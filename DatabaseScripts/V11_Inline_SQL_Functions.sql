-- V11 - Replace application inline SQL with database functions

CREATE OR REPLACE FUNCTION public.get_auth_user_by_email(p_email TEXT)
RETURNS TABLE (
	id BIGINT,
	email CITEXT,
	password_hash VARCHAR,
	first_name VARCHAR,
	last_name VARCHAR,
	phone_number VARCHAR,
	status SMALLINT,
	email_confirmed BOOLEAN,
	failed_login_count INTEGER,
	locked_until_utc TIMESTAMPTZ,
	authorization_version INTEGER,
	roles VARCHAR[]
)
LANGUAGE SQL
AS $$
	SELECT u.id, u.email, u.password_hash, u.first_name, u.last_name, u.phone_number,
		   u.status, u.email_confirmed, u.failed_login_count, u.locked_until_utc,
		   u.authorization_version,
		   COALESCE(array_agg(r.code) FILTER (WHERE r.code IS NOT NULL), ARRAY[]::VARCHAR[]) AS roles
	FROM public.users u
	LEFT JOIN public.user_roles ur ON ur.user_id = u.id AND ur.revoked_at_utc IS NULL
	LEFT JOIN public.roles r ON r.id = ur.role_id
	WHERE u.email = p_email
	GROUP BY u.id;
$$;

CREATE OR REPLACE FUNCTION public.get_auth_user_by_id(p_user_id BIGINT)
RETURNS TABLE (
	id BIGINT,
	email CITEXT,
	password_hash VARCHAR,
	first_name VARCHAR,
	last_name VARCHAR,
	phone_number VARCHAR,
	status SMALLINT,
	email_confirmed BOOLEAN,
	failed_login_count INTEGER,
	locked_until_utc TIMESTAMPTZ,
	authorization_version INTEGER,
	roles VARCHAR[]
)
LANGUAGE SQL
AS $$
	SELECT u.id, u.email, u.password_hash, u.first_name, u.last_name, u.phone_number,
		   u.status, u.email_confirmed, u.failed_login_count, u.locked_until_utc,
		   u.authorization_version,
		   COALESCE(array_agg(r.code) FILTER (WHERE r.code IS NOT NULL), ARRAY[]::VARCHAR[]) AS roles
	FROM public.users u
	LEFT JOIN public.user_roles ur ON ur.user_id = u.id AND ur.revoked_at_utc IS NULL
	LEFT JOIN public.roles r ON r.id = ur.role_id
	WHERE u.id = p_user_id
	GROUP BY u.id;
$$;

CREATE OR REPLACE FUNCTION public.get_staff_branch_id(p_user_id BIGINT)
RETURNS BIGINT
LANGUAGE SQL
AS $$
	SELECT branch_id FROM public.staff_profiles WHERE user_id = p_user_id;
$$;

CREATE OR REPLACE FUNCTION public.create_customer(
	p_email CITEXT,
	p_password_hash VARCHAR,
	p_first_name VARCHAR,
	p_last_name VARCHAR,
	p_phone_number VARCHAR
)
RETURNS BIGINT
LANGUAGE plpgsql
AS $$
DECLARE
	v_user_id BIGINT;
BEGIN
	INSERT INTO public.users (email, password_hash, first_name, last_name, phone_number, status, email_confirmed)
	VALUES (p_email, p_password_hash, p_first_name, p_last_name, p_phone_number, 1, FALSE)
	RETURNING id INTO v_user_id;

	INSERT INTO public.customer_profiles (user_id) VALUES (v_user_id);
	INSERT INTO public.user_roles (user_id, role_id)
	SELECT v_user_id, id FROM public.roles WHERE code = 'Customer';

	RETURN v_user_id;
END;
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
	v_invitation_id BIGINT;
BEGIN
	INSERT INTO public.users (email, password_hash, first_name, last_name, phone_number, status, email_confirmed)
	VALUES (p_email, '', p_first_name, p_last_name, p_phone_number, 4, FALSE)
	RETURNING id INTO v_user_id;

	SELECT id INTO v_role_id FROM public.roles WHERE code = p_role_code;

	INSERT INTO public.staff_profiles (user_id, branch_id) VALUES (v_user_id, p_branch_id);
	INSERT INTO public.user_roles (user_id, role_id, assigned_by_user_id)
	VALUES (v_user_id, v_role_id, p_assigned_by_user_id);
	INSERT INTO public.staff_invitations (user_id, token_hash, role_id, invited_by_user_id, expires_at_utc)
	VALUES (v_user_id, p_token_hash, v_role_id, p_assigned_by_user_id, p_expires_at_utc)
	RETURNING id INTO v_invitation_id;

	RETURN v_user_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.get_staff_invitation(p_token_hash VARCHAR)
RETURNS TABLE (id BIGINT, user_id BIGINT, token_hash VARCHAR, expires_at_utc TIMESTAMPTZ, accepted_at_utc TIMESTAMPTZ)
LANGUAGE SQL
AS $$
	SELECT id, user_id, token_hash, expires_at_utc, accepted_at_utc
	FROM public.staff_invitations
	WHERE token_hash = p_token_hash;
$$;

CREATE OR REPLACE FUNCTION public.accept_staff_invitation(p_invitation_id BIGINT, p_user_id BIGINT, p_password_hash VARCHAR)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
	UPDATE public.users
	SET password_hash = p_password_hash, status = 1, email_confirmed = TRUE, updated_at_utc = now()
	WHERE id = p_user_id;
	UPDATE public.staff_invitations
	SET accepted_at_utc = now()
	WHERE id = p_invitation_id AND accepted_at_utc IS NULL;
END;
$$;

CREATE OR REPLACE FUNCTION public.record_failed_login(p_user_id BIGINT, p_failed_login_count INTEGER, p_locked_until_utc TIMESTAMPTZ)
RETURNS BOOLEAN
LANGUAGE SQL
AS $$
	UPDATE public.users
	SET failed_login_count = p_failed_login_count, locked_until_utc = p_locked_until_utc, updated_at_utc = now()
	WHERE id = p_user_id
	RETURNING TRUE;
$$;

CREATE OR REPLACE FUNCTION public.record_successful_login(p_user_id BIGINT)
RETURNS BOOLEAN
LANGUAGE SQL
AS $$
	UPDATE public.users
	SET failed_login_count = 0, locked_until_utc = NULL, last_login_at_utc = now(), updated_at_utc = now()
	WHERE id = p_user_id
	RETURNING TRUE;
$$;

CREATE OR REPLACE FUNCTION public.get_refresh_session(p_refresh_token_hash VARCHAR)
RETURNS TABLE (id BIGINT, user_id BIGINT, family_id UUID, refresh_token_hash VARCHAR, expires_at_utc TIMESTAMPTZ, revoked_at_utc TIMESTAMPTZ, replaced_by_session_id BIGINT)
LANGUAGE SQL
AS $$
	SELECT id, user_id, family_id, refresh_token_hash, expires_at_utc, revoked_at_utc, replaced_by_session_id
	FROM public.user_sessions
	WHERE refresh_token_hash = p_refresh_token_hash;
$$;

CREATE OR REPLACE FUNCTION public.create_refresh_session(p_user_id BIGINT, p_family_id UUID, p_refresh_token_hash VARCHAR, p_expires_at_utc TIMESTAMPTZ)
RETURNS BIGINT
LANGUAGE SQL
AS $$
	INSERT INTO public.user_sessions (user_id, family_id, refresh_token_hash, expires_at_utc)
	VALUES (p_user_id, p_family_id, p_refresh_token_hash, p_expires_at_utc)
	RETURNING id;
$$;

CREATE OR REPLACE FUNCTION public.rotate_refresh_session(p_session_id BIGINT, p_replacement_session_id BIGINT)
RETURNS BOOLEAN
LANGUAGE SQL
AS $$
	UPDATE public.user_sessions
	SET revoked_at_utc = now(), revoke_reason = 'Rotated', replaced_by_session_id = p_replacement_session_id, last_used_at_utc = now()
	WHERE id = p_session_id AND revoked_at_utc IS NULL
	RETURNING TRUE;
$$;

CREATE OR REPLACE FUNCTION public.revoke_refresh_session(p_session_id BIGINT, p_reason VARCHAR)
RETURNS BOOLEAN
LANGUAGE SQL
AS $$
	UPDATE public.user_sessions
	SET revoked_at_utc = COALESCE(revoked_at_utc, now()), revoke_reason = p_reason
	WHERE id = p_session_id
	RETURNING TRUE;
$$;

CREATE OR REPLACE FUNCTION public.revoke_refresh_session_family(p_family_id UUID, p_reason VARCHAR)
RETURNS BOOLEAN
LANGUAGE SQL
AS $$
	UPDATE public.user_sessions
	SET revoked_at_utc = COALESCE(revoked_at_utc, now()), revoke_reason = p_reason
	WHERE family_id = p_family_id
	RETURNING TRUE;
$$;

CREATE OR REPLACE FUNCTION public.get_customer_page(p_page_size INTEGER, p_offset INTEGER)
RETURNS TABLE (customer_id BIGINT, first_name VARCHAR, last_name VARCHAR, email CITEXT, phone_number VARCHAR, status SMALLINT, created_at_utc TIMESTAMPTZ, total_count BIGINT)
LANGUAGE SQL
AS $$
	SELECT u.id, u.first_name, u.last_name, u.email, u.phone_number, u.status, u.created_at_utc,
		   COUNT(*) OVER()
	FROM public.users u
	JOIN public.customer_profiles cp ON cp.user_id = u.id
	ORDER BY u.last_name, u.first_name, u.id
	LIMIT p_page_size OFFSET p_offset;
$$;

CREATE OR REPLACE FUNCTION public.get_branch_by_id(p_branch_id BIGINT)
RETURNS TABLE (id BIGINT, code VARCHAR, name VARCHAR, address_line_1 VARCHAR, city VARCHAR, province VARCHAR, postal_code VARCHAR, status SMALLINT, accepts_bookings BOOLEAN, created_at_utc TIMESTAMPTZ, total_count BIGINT)
LANGUAGE SQL
AS $$
	SELECT id, code, name, address_line_1, city, province, postal_code, status, accepts_bookings, created_at_utc, 1::BIGINT
	FROM public.branches
	WHERE id = p_branch_id;
$$;

CREATE OR REPLACE FUNCTION public.get_bookable_branches()
RETURNS TABLE (branch_id BIGINT, code VARCHAR, name VARCHAR, city VARCHAR)
LANGUAGE SQL
AS $$
	SELECT id, code, name, city
	FROM public.branches
	WHERE status = 1 AND accepts_bookings
	ORDER BY name, id;
$$;

CREATE OR REPLACE FUNCTION public.get_bookable_services(p_branch_id BIGINT)
RETURNS TABLE (service_id BIGINT, code VARCHAR, name VARCHAR, duration_minutes INTEGER)
LANGUAGE SQL
AS $$
	SELECT s.id, s.code, s.name, s.service_duration_minutes
	FROM public.branch_services bs
	JOIN public.offered_services s ON s.id = bs.service_id
	JOIN public.branches b ON b.id = bs.branch_id
	WHERE bs.branch_id = p_branch_id AND bs.is_active AND b.status = 1 AND b.accepts_bookings
	ORDER BY s.name, s.id;
$$;

CREATE OR REPLACE FUNCTION public.get_customer_appointments(p_customer_id BIGINT, p_history BOOLEAN)
RETURNS TABLE (appointment_id BIGINT, customer_id BIGINT, branch_id BIGINT, branch_name VARCHAR, service_id BIGINT, service_name VARCHAR, start_at TIMESTAMP, end_at TIMESTAMP, status VARCHAR)
LANGUAGE SQL
AS $$
	SELECT a.id, a.customer_id, a.branch_id, b.name, a.service_id, s.name,
		   a.starts_at_utc AT TIME ZONE 'Africa/Johannesburg', a.ends_at_utc AT TIME ZONE 'Africa/Johannesburg', a.status
	FROM public.appointments a
	JOIN public.branches b ON b.id = a.branch_id
	JOIN public.offered_services s ON s.id = a.service_id
	WHERE a.customer_id = p_customer_id
	  AND ((p_history AND (a.starts_at_utc < now() OR a.status = 'Cancelled'))
		OR (NOT p_history AND a.status = 'Confirmed' AND a.starts_at_utc >= now()))
	ORDER BY a.starts_at_utc;
$$;

CREATE OR REPLACE FUNCTION public.get_operational_appointments(p_branch_id BIGINT, p_history BOOLEAN, p_page_size INTEGER, p_offset INTEGER)
RETURNS TABLE (appointment_id BIGINT, customer_id BIGINT, customer_name TEXT, customer_email CITEXT, branch_id BIGINT, branch_name VARCHAR, service_id BIGINT, service_name VARCHAR, start_at TIMESTAMP, end_at TIMESTAMP, status VARCHAR, total_count BIGINT)
LANGUAGE SQL
AS $$
	SELECT a.id, a.customer_id, concat(u.first_name, ' ', u.last_name), u.email, a.branch_id, b.name, a.service_id, s.name,
		   a.starts_at_utc AT TIME ZONE 'Africa/Johannesburg', a.ends_at_utc AT TIME ZONE 'Africa/Johannesburg', a.status, COUNT(*) OVER()
	FROM public.appointments a
	JOIN public.users u ON u.id = a.customer_id
	JOIN public.branches b ON b.id = a.branch_id
	JOIN public.offered_services s ON s.id = a.service_id
	WHERE (p_branch_id IS NULL OR a.branch_id = p_branch_id)
	  AND ((p_history AND (a.starts_at_utc < now() OR a.status = 'Cancelled'))
		OR (NOT p_history AND a.status = 'Confirmed' AND a.starts_at_utc >= now()))
	ORDER BY a.starts_at_utc, a.id
	LIMIT p_page_size OFFSET p_offset;
$$;

CREATE OR REPLACE FUNCTION public.get_available_slots(p_customer_id BIGINT, p_branch_id BIGINT, p_service_id BIGINT, p_date DATE, p_exclude_id BIGINT, p_now_sast TIMESTAMP)
RETURNS TABLE (start_at TIMESTAMP, end_at TIMESTAMP)
LANGUAGE SQL
AS $$
	WITH selected_service AS (
		SELECT s.service_duration_minutes AS duration
		FROM public.offered_services s
		JOIN public.branch_services bs ON bs.service_id = s.id
		WHERE s.id = p_service_id AND bs.branch_id = p_branch_id AND bs.is_active
	), selected_schedule AS (
		SELECT id
		FROM public.branch_operational_schedules
		WHERE branch_id = p_branch_id AND is_active AND effective_from <= p_date
		  AND (effective_to IS NULL OR effective_to >= p_date)
		ORDER BY effective_from DESC LIMIT 1
	), hours AS (
		SELECT COALESCE(dh.opens_at, wh.opens_at) AS opens_at,
			   COALESCE(dh.closes_at, wh.closes_at) AS closes_at,
			   COALESCE(co.capacity, dh.capacity, wh.capacity, 1) AS capacity,
			   COALESCE(dover.is_closed, false) AS is_closed
		FROM selected_schedule ss
		JOIN public.branch_weekly_operating_hours wh
		  ON wh.schedule_id = ss.id AND wh.day_of_week = EXTRACT(DOW FROM p_date)::smallint + 1
		LEFT JOIN public.branch_date_overrides dover
		  ON dover.schedule_id = ss.id AND dover.override_date = p_date
		LEFT JOIN public.branch_date_operating_hours dh ON dh.override_id = dover.override_id
		LEFT JOIN public.branch_daily_capacity_overrides co
		  ON co.branch_id = p_branch_id AND co.override_date = p_date
	), candidates AS (
		SELECT gs AS local_start,
			   gs + make_interval(mins => selected_service.duration) AS local_end,
			   hours.capacity
		FROM hours CROSS JOIN selected_service
		CROSS JOIN LATERAL generate_series(
			p_date + hours.opens_at,
			p_date + hours.closes_at - make_interval(mins => selected_service.duration),
			interval '15 minutes') gs
		WHERE NOT hours.is_closed AND hours.opens_at < hours.closes_at
	)
	SELECT local_start, local_end
	FROM candidates c
	WHERE local_start > p_now_sast
	  AND (SELECT COUNT(*) FROM public.appointments a
		   WHERE a.branch_id = p_branch_id AND a.status = 'Confirmed'
			 AND (p_exclude_id IS NULL OR a.id <> p_exclude_id)
			 AND a.starts_at_utc < (c.local_end AT TIME ZONE 'Africa/Johannesburg')
			 AND a.ends_at_utc > (c.local_start AT TIME ZONE 'Africa/Johannesburg')) < c.capacity
	  AND NOT EXISTS (SELECT 1 FROM public.appointments customer_appointment
					  WHERE customer_appointment.customer_id = p_customer_id
						AND customer_appointment.status = 'Confirmed'
						AND (p_exclude_id IS NULL OR customer_appointment.id <> p_exclude_id)
						AND customer_appointment.starts_at_utc < (c.local_end AT TIME ZONE 'Africa/Johannesburg')
						AND customer_appointment.ends_at_utc > (c.local_start AT TIME ZONE 'Africa/Johannesburg'))
	ORDER BY local_start;
$$;

CREATE OR REPLACE FUNCTION public.create_appointment(p_customer_id BIGINT, p_branch_id BIGINT, p_service_id BIGINT, p_start_at TIMESTAMP)
RETURNS BIGINT
LANGUAGE plpgsql
AS $$
DECLARE
	v_duration INTEGER;
	v_id BIGINT;
BEGIN
	PERFORM pg_advisory_xact_lock(hashtextextended(format('branch:%s:date:%s', p_branch_id, p_start_at::date), 0));

	SELECT s.service_duration_minutes INTO v_duration
	FROM public.offered_services s
	JOIN public.branch_services bs ON bs.service_id = s.id
	JOIN public.branches b ON b.id = bs.branch_id
	WHERE s.id = p_service_id AND bs.branch_id = p_branch_id AND bs.is_active
	  AND b.status = 1 AND b.accepts_bookings;

	IF v_duration IS NULL THEN
		RAISE EXCEPTION 'The selected service is not available at this branch.';
	END IF;

	INSERT INTO public.appointments(customer_id, branch_id, service_id, starts_at_utc, ends_at_utc)
	SELECT p_customer_id, p_branch_id, p_service_id,
		   p_start_at AT TIME ZONE 'Africa/Johannesburg',
		   (p_start_at + make_interval(mins => v_duration)) AT TIME ZONE 'Africa/Johannesburg'
	WHERE NOT EXISTS (
		SELECT 1 FROM public.appointments
		WHERE customer_id = p_customer_id AND branch_id = p_branch_id
		  AND starts_at_utc = p_start_at AT TIME ZONE 'Africa/Johannesburg'
		  AND status = 'Confirmed')
	RETURNING id INTO v_id;

	RETURN v_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.cancel_appointment(p_customer_id BIGINT, p_appointment_id BIGINT)
RETURNS BOOLEAN
LANGUAGE SQL
AS $$
	UPDATE public.appointments
	SET status = 'Cancelled'
	WHERE id = p_appointment_id AND customer_id = p_customer_id AND status = 'Confirmed' AND starts_at_utc > now()
	RETURNING TRUE;
$$;

CREATE OR REPLACE FUNCTION public.reschedule_appointment(p_customer_id BIGINT, p_appointment_id BIGINT, p_start_at TIMESTAMP)
RETURNS BOOLEAN
LANGUAGE SQL
AS $$
	UPDATE public.appointments a
	SET starts_at_utc = p_start_at AT TIME ZONE 'Africa/Johannesburg',
		ends_at_utc = (p_start_at + (a.ends_at_utc - a.starts_at_utc)) AT TIME ZONE 'Africa/Johannesburg'
	WHERE a.id = p_appointment_id AND a.customer_id = p_customer_id AND a.status = 'Confirmed'
	  AND a.starts_at_utc > now()
	  AND NOT EXISTS (
		  SELECT 1 FROM public.appointments other
		  WHERE other.customer_id = a.customer_id AND other.id <> a.id AND other.status = 'Confirmed'
			AND other.starts_at_utc < (p_start_at + (a.ends_at_utc - a.starts_at_utc)) AT TIME ZONE 'Africa/Johannesburg'
			AND other.ends_at_utc > p_start_at AT TIME ZONE 'Africa/Johannesburg')
	RETURNING TRUE;
$$;
