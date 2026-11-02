-- ============================================================
-- V7 - Branch lifecycle functions
--
-- These functions only persist branch data and state. Business
-- validation remains in the application layer.
-- ============================================================

CREATE OR REPLACE FUNCTION public.update_branch(
	p_branch_id       BIGINT,
	p_code            VARCHAR,
	p_name            VARCHAR,
	p_address_line_1  VARCHAR,
	p_city            VARCHAR,
	p_province        VARCHAR,
	p_postal_code     VARCHAR
)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
	WITH updated AS (
		UPDATE public.branches
		SET code          = p_code,
			name          = p_name,
			address_line_1 = p_address_line_1,
			city          = p_city,
			province      = p_province,
			postal_code   = p_postal_code
		WHERE id = p_branch_id
		RETURNING 1
	)
	SELECT EXISTS (SELECT 1 FROM updated);
$$;


CREATE OR REPLACE FUNCTION public.deactivate_branch(p_branch_id BIGINT)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
	WITH updated AS (
		UPDATE public.branches
		SET status = 0,
			accepts_bookings = FALSE
		WHERE id = p_branch_id
		RETURNING 1
	)
	SELECT EXISTS (SELECT 1 FROM updated);
$$;
