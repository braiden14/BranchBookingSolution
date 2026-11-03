-- V12 - Staff directory and account lifecycle management

INSERT INTO public.roles (code, name, description)
VALUES
	('BranchManager', 'Branch Manager', 'Manages operations for an assigned branch'),
	('Admin', 'Administrator', 'Full system access')
ON CONFLICT (code) DO NOTHING;

CREATE OR REPLACE FUNCTION public.get_auth_user_by_email(p_email CITEXT)
RETURNS TABLE (id BIGINT, email CITEXT, password_hash VARCHAR, first_name VARCHAR, last_name VARCHAR, phone_number VARCHAR, status SMALLINT, email_confirmed BOOLEAN, failed_login_count INTEGER, locked_until_utc TIMESTAMPTZ, authorization_version INTEGER, roles VARCHAR[], assigned_branch_id BIGINT)
LANGUAGE SQL AS $$
	SELECT u.id, u.email, u.password_hash, u.first_name, u.last_name, u.phone_number, u.status, u.email_confirmed, u.failed_login_count, u.locked_until_utc, u.authorization_version,
		   COALESCE(array_agg(r.code) FILTER (WHERE r.code IS NOT NULL), ARRAY[]::VARCHAR[]), sp.branch_id
	FROM public.users u
	LEFT JOIN public.user_roles ur ON ur.user_id = u.id AND ur.revoked_at_utc IS NULL
	LEFT JOIN public.roles r ON r.id = ur.role_id
	LEFT JOIN public.staff_profiles sp ON sp.user_id = u.id
	WHERE u.email = p_email GROUP BY u.id, sp.branch_id;
$$;

CREATE OR REPLACE FUNCTION public.get_auth_user_by_id(p_user_id BIGINT)
RETURNS TABLE (id BIGINT, email CITEXT, password_hash VARCHAR, first_name VARCHAR, last_name VARCHAR, phone_number VARCHAR, status SMALLINT, email_confirmed BOOLEAN, failed_login_count INTEGER, locked_until_utc TIMESTAMPTZ, authorization_version INTEGER, roles VARCHAR[], assigned_branch_id BIGINT)
LANGUAGE SQL AS $$
	SELECT u.id, u.email, u.password_hash, u.first_name, u.last_name, u.phone_number, u.status, u.email_confirmed, u.failed_login_count, u.locked_until_utc, u.authorization_version,
		   COALESCE(array_agg(r.code) FILTER (WHERE r.code IS NOT NULL), ARRAY[]::VARCHAR[]), sp.branch_id
	FROM public.users u
	LEFT JOIN public.user_roles ur ON ur.user_id = u.id AND ur.revoked_at_utc IS NULL
	LEFT JOIN public.roles r ON r.id = ur.role_id
	LEFT JOIN public.staff_profiles sp ON sp.user_id = u.id
	WHERE u.id = p_user_id GROUP BY u.id, sp.branch_id;
$$;

CREATE OR REPLACE FUNCTION public.create_staff_invitation(
	p_email CITEXT,
	p_first_name VARCHAR,
	p_last_name VARCHAR,
	p_phone_number VARCHAR,
	p_branch_id BIGINT,
	p_assigned_by_user_id BIGINT,
	p_role_code VARCHAR,
	p_token_hash VARCHAR,
	p_expires_at_utc TIMESTAMPTZ
)
RETURNS BIGINT
LANGUAGE plpgsql
AS $$
DECLARE
	v_user_id BIGINT;
	v_role_id BIGINT;
BEGIN
	IF p_role_code NOT IN ('Admin', 'Staff', 'BranchManager') THEN
		RAISE EXCEPTION 'Unsupported invitation role.';
	END IF;

	IF p_role_code <> 'Admin' AND p_branch_id IS NULL THEN
		RAISE EXCEPTION 'A branch is required for this invitation role.';
	END IF;

	INSERT INTO public.users (email, password_hash, first_name, last_name, phone_number, status, email_confirmed)
	VALUES (p_email, '', p_first_name, p_last_name, p_phone_number, 4, FALSE)
	RETURNING id INTO v_user_id;

	SELECT id INTO v_role_id
	FROM public.roles
	WHERE code = p_role_code;

	IF v_role_id IS NULL THEN
		RAISE EXCEPTION 'Invitation role % does not exist.', p_role_code;
	END IF;

	IF p_branch_id IS NOT NULL THEN
		INSERT INTO public.staff_profiles (user_id, branch_id)
		VALUES (v_user_id, p_branch_id);
	END IF;

	INSERT INTO public.user_roles (user_id, role_id, assigned_by_user_id)
	VALUES (v_user_id, v_role_id, p_assigned_by_user_id);

	INSERT INTO public.staff_invitations (user_id, token_hash, role_id, invited_by_user_id, expires_at_utc)
	VALUES (v_user_id, p_token_hash, v_role_id, p_assigned_by_user_id, p_expires_at_utc);

	RETURN v_user_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.get_staff_directory(p_page_number INTEGER, p_page_size INTEGER)
RETURNS TABLE (
	user_id BIGINT,
	email CITEXT,
	first_name VARCHAR,
	last_name VARCHAR,
	phone_number VARCHAR,
	status SMALLINT,
	roles VARCHAR[],
	branch_id BIGINT,
	branch_name VARCHAR,
	created_at_utc TIMESTAMPTZ,
	last_login_at_utc TIMESTAMPTZ,
	invitation_expires_at_utc TIMESTAMPTZ,
	total_count BIGINT
)
LANGUAGE SQL
AS $$
	WITH staff_users AS (
		SELECT DISTINCT u.id
		FROM public.users u
		JOIN public.user_roles ur ON ur.user_id = u.id AND ur.revoked_at_utc IS NULL
		JOIN public.roles r ON r.id = ur.role_id
		WHERE r.code IN ('Admin', 'Staff', 'BranchManager')
	), counted AS (
		SELECT COUNT(*) OVER () AS total_count, u.*
		FROM staff_users su
		JOIN public.users u ON u.id = su.id
		ORDER BY u.created_at_utc DESC, u.id DESC
		OFFSET GREATEST(p_page_number - 1, 0) * p_page_size
		LIMIT p_page_size
	)
	SELECT c.id, c.email, c.first_name, c.last_name, c.phone_number, c.status,
		   COALESCE((SELECT array_agg(r.code ORDER BY r.code)
					 FROM public.user_roles ur JOIN public.roles r ON r.id = ur.role_id
					 WHERE ur.user_id = c.id AND ur.revoked_at_utc IS NULL), ARRAY[]::VARCHAR[]),
		   sp.branch_id, b.name, c.created_at_utc, c.last_login_at_utc,
		   (SELECT si.expires_at_utc FROM public.staff_invitations si
			WHERE si.user_id = c.id AND si.accepted_at_utc IS NULL
			ORDER BY si.created_at_utc DESC LIMIT 1), c.total_count
	FROM counted c
	LEFT JOIN public.staff_profiles sp ON sp.user_id = c.id
	LEFT JOIN public.branches b ON b.id = sp.branch_id
	ORDER BY c.created_at_utc DESC, c.id DESC;
$$;

CREATE OR REPLACE FUNCTION public.update_staff_status(p_user_id BIGINT, p_status SMALLINT)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
BEGIN
	IF p_status NOT IN (1, 3) THEN
		RAISE EXCEPTION 'Staff status must be active or disabled.';
	END IF;

	UPDATE public.users u
	SET status = p_status, updated_at_utc = now(), authorization_version = authorization_version + 1
	WHERE u.id = p_user_id
	  AND EXISTS (
		  SELECT 1 FROM public.user_roles ur JOIN public.roles r ON r.id = ur.role_id
			WHERE ur.user_id = u.id AND ur.revoked_at_utc IS NULL AND r.code IN ('Admin', 'Staff', 'BranchManager'));
	RETURN FOUND;
END;
$$;

CREATE OR REPLACE FUNCTION public.revoke_user_sessions(p_user_id BIGINT, p_reason VARCHAR)
RETURNS INTEGER
LANGUAGE SQL
AS $$
	WITH revoked AS (
		UPDATE public.user_sessions
		SET revoked_at_utc = COALESCE(revoked_at_utc, now()), revoke_reason = p_reason
		WHERE user_id = p_user_id AND revoked_at_utc IS NULL
		RETURNING 1
	)
	SELECT COUNT(*)::INTEGER FROM revoked;
$$;
