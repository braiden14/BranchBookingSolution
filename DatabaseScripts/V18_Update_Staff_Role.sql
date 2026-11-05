-- ============================================================
-- V18: Add staff role update function
-- ============================================================
-- Replaces the active staff role assignment while preserving the
-- historical user_roles rows through revoked_at_utc.

CREATE OR REPLACE FUNCTION public.update_staff_role(
	p_user_id BIGINT,
	p_role_code VARCHAR)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
DECLARE
	v_role_id BIGINT;
BEGIN
	IF p_role_code NOT IN ('Admin', 'Staff', 'BranchManager') THEN
		RAISE EXCEPTION 'Staff role must be Admin, Staff, or BranchManager.';
	END IF;

	SELECT id INTO v_role_id
	FROM public.roles
	WHERE code = p_role_code;

	IF v_role_id IS NULL THEN
		RAISE EXCEPTION 'Staff role % does not exist.', p_role_code;
	END IF;

	IF NOT EXISTS (
		SELECT 1
		FROM public.users u
		JOIN public.user_roles ur ON ur.user_id = u.id AND ur.revoked_at_utc IS NULL
		JOIN public.roles existing_role ON existing_role.id = ur.role_id
		WHERE u.id = p_user_id
			AND existing_role.code IN ('Admin', 'Staff', 'BranchManager')) THEN
		RETURN FALSE;
	END IF;

	UPDATE public.user_roles
	SET revoked_at_utc = now()
	WHERE user_id = p_user_id
	  AND revoked_at_utc IS NULL
	  AND role_id <> v_role_id;

	INSERT INTO public.user_roles (user_id, role_id, assigned_at_utc, revoked_at_utc)
	VALUES (p_user_id, v_role_id, now(), NULL)
	ON CONFLICT (user_id, role_id) DO UPDATE
	SET assigned_at_utc = EXCLUDED.assigned_at_utc,
		revoked_at_utc = NULL;

	UPDATE public.users
	SET authorization_version = authorization_version + 1,
		updated_at_utc = now()
	WHERE id = p_user_id;

	RETURN TRUE;
END;
$$;
