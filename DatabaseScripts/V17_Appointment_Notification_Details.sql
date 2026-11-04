-- ============================================================
-- V17: Add a targeted appointment notification lookup
-- ============================================================
-- Returns all data required to publish a booking confirmation in one query.

CREATE OR REPLACE FUNCTION public.get_appointment_notification_details(
	p_customer_id BIGINT,
	p_appointment_id BIGINT)
RETURNS TABLE (
	appointment_id BIGINT,
	customer_id BIGINT,
	customer_email CITEXT,
	customer_first_name VARCHAR,
	customer_last_name VARCHAR,
	branch_id BIGINT,
	branch_name VARCHAR,
	branch_address_line1 VARCHAR,
	branch_city VARCHAR,
	branch_province VARCHAR,
	branch_postal_code VARCHAR,
	service_id BIGINT,
	service_name VARCHAR,
	start_at TIMESTAMP,
	end_at TIMESTAMP)
LANGUAGE SQL
AS $$
	SELECT a.id,
		   a.customer_id,
		   u.email,
		   u.first_name,
		   u.last_name,
		   a.branch_id,
		   b.name,
		   b.address_line_1,
		   b.city,
		   b.province,
		   b.postal_code,
		   a.service_id,
		   s.name,
		   a.starts_at_utc AT TIME ZONE 'Africa/Johannesburg',
		   a.ends_at_utc AT TIME ZONE 'Africa/Johannesburg'
	FROM public.appointments a
	JOIN public.users u ON u.id = a.customer_id
	JOIN public.branches b ON b.id = a.branch_id
	JOIN public.offered_services s ON s.id = a.service_id
	WHERE a.customer_id = p_customer_id
	  AND a.id = p_appointment_id;
$$;
