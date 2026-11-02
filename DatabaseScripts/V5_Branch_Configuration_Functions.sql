-- ============================================================
-- V5 - Branch configuration functions
--
-- These functions only persist and retrieve branch configuration.
-- Schedule resolution, validation and booking rules belong in the API.
-- ============================================================

-- ============================================================
-- OFFERED SERVICES AND BRANCH SERVICES
-- ============================================================

CREATE OR REPLACE FUNCTION public.get_all_offered_services()
RETURNS TABLE (
	id                       BIGINT,
	code                     VARCHAR,
	name                     VARCHAR,
	service_duration_minutes INTEGER,
	created_at_utc           TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
	SELECT
		s.id,
		s.code,
		s.name,
		s.service_duration_minutes,
		s.created_at_utc
	FROM public.offered_services s
	ORDER BY s.name, s.id;
$$;


CREATE OR REPLACE FUNCTION public.get_branch_services(p_branch_id BIGINT)
RETURNS TABLE (
	branch_service_id        BIGINT,
	branch_id                BIGINT,
	service_id               BIGINT,
	service_code             VARCHAR,
	service_name             VARCHAR,
	service_duration_minutes INTEGER,
	is_active                BOOLEAN,
	created_at_utc           TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
	SELECT
		bs.branch_service_id,
		bs.branch_id,
		bs.service_id,
		s.code,
		s.name,
		s.service_duration_minutes,
		bs.is_active,
		bs.created_at_utc
	FROM public.branch_services bs
	INNER JOIN public.offered_services s ON s.id = bs.service_id
	WHERE bs.branch_id = p_branch_id
	ORDER BY s.name, s.id;
$$;


CREATE OR REPLACE FUNCTION public.upsert_branch_service(
	p_branch_id  BIGINT,
	p_service_id BIGINT,
	p_is_active  BOOLEAN DEFAULT TRUE
)
RETURNS BIGINT
LANGUAGE sql
AS $$
	INSERT INTO public.branch_services (
		branch_id,
		service_id,
		is_active
	)
	VALUES (
		p_branch_id,
		p_service_id,
		COALESCE(p_is_active, TRUE)
	)
	ON CONFLICT (branch_id, service_id)
	DO UPDATE SET is_active = EXCLUDED.is_active
	RETURNING branch_service_id;
$$;


-- ============================================================
-- DATE OVERRIDES
-- ============================================================

CREATE OR REPLACE FUNCTION public.get_branch_date_overrides(
	p_branch_id BIGINT,
	p_from_date DATE DEFAULT NULL,
	p_to_date   DATE DEFAULT NULL
)
RETURNS TABLE (
	override_id   BIGINT,
	schedule_id   BIGINT,
	branch_id     BIGINT,
	override_date DATE,
	reason        VARCHAR,
	is_closed     BOOLEAN,
	date_hours_id BIGINT,
	opens_at      TIME,
	closes_at     TIME,
	capacity      INTEGER
)
LANGUAGE sql
STABLE
AS $$
	SELECT
		o.override_id,
		o.schedule_id,
		s.branch_id,
		o.override_date,
		o.reason,
		o.is_closed,
		h.date_hours_id,
		h.opens_at,
		h.closes_at,
		h.capacity
	FROM public.branch_date_overrides o
	INNER JOIN public.branch_operational_schedules s ON s.id = o.schedule_id
	LEFT JOIN public.branch_date_operating_hours h ON h.override_id = o.override_id
	WHERE s.branch_id = p_branch_id
	  AND (p_from_date IS NULL OR o.override_date >= p_from_date)
	  AND (p_to_date IS NULL OR o.override_date <= p_to_date)
	ORDER BY o.override_date, o.override_id;
$$;


CREATE OR REPLACE FUNCTION public.create_branch_date_override(
	p_schedule_id   BIGINT,
	p_override_date DATE,
	p_reason        VARCHAR DEFAULT NULL,
	p_is_closed     BOOLEAN DEFAULT FALSE
)
RETURNS BIGINT
LANGUAGE sql
AS $$
	INSERT INTO public.branch_date_overrides (
		schedule_id,
		override_date,
		reason,
		is_closed
	)
	VALUES (
		p_schedule_id,
		p_override_date,
		p_reason,
		COALESCE(p_is_closed, FALSE)
	)
	RETURNING override_id;
$$;


CREATE OR REPLACE FUNCTION public.update_branch_date_override(
	p_override_id   BIGINT,
	p_override_date DATE,
	p_reason        VARCHAR,
	p_is_closed     BOOLEAN
)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
	WITH updated AS (
		UPDATE public.branch_date_overrides
		SET override_date = p_override_date,
			reason        = p_reason,
			is_closed     = COALESCE(p_is_closed, FALSE)
		WHERE override_id = p_override_id
		RETURNING 1
	)
	SELECT EXISTS (SELECT 1 FROM updated);
$$;


CREATE OR REPLACE FUNCTION public.create_branch_date_operating_hours(
	p_override_id BIGINT,
	p_opens_at    TIME,
	p_closes_at   TIME,
	p_capacity    INTEGER DEFAULT NULL
)
RETURNS BIGINT
LANGUAGE sql
AS $$
	INSERT INTO public.branch_date_operating_hours (
		override_id,
		opens_at,
		closes_at,
		capacity
	)
	VALUES (
		p_override_id,
		p_opens_at,
		p_closes_at,
		p_capacity
	)
	RETURNING date_hours_id;
$$;


CREATE OR REPLACE FUNCTION public.update_branch_date_operating_hours(
	p_date_hours_id BIGINT,
	p_opens_at      TIME,
	p_closes_at     TIME,
	p_capacity      INTEGER DEFAULT NULL
)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
	WITH updated AS (
		UPDATE public.branch_date_operating_hours
		SET opens_at  = p_opens_at,
			closes_at = p_closes_at,
			capacity  = p_capacity
		WHERE date_hours_id = p_date_hours_id
		RETURNING 1
	)
	SELECT EXISTS (SELECT 1 FROM updated);
$$;


CREATE OR REPLACE FUNCTION public.delete_branch_date_override(p_override_id BIGINT)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
	WITH deleted AS (
		DELETE FROM public.branch_date_overrides
		WHERE override_id = p_override_id
		RETURNING 1
	)
	SELECT EXISTS (SELECT 1 FROM deleted);
$$;


-- ============================================================
-- DAILY CAPACITY OVERRIDES
-- ============================================================

CREATE OR REPLACE FUNCTION public.get_branch_capacity_overrides(
	p_branch_id BIGINT,
	p_from_date DATE DEFAULT NULL,
	p_to_date   DATE DEFAULT NULL
)
RETURNS TABLE (
	capacity_override_id BIGINT,
	branch_id            BIGINT,
	override_date        DATE,
	capacity             INTEGER
)
LANGUAGE sql
STABLE
AS $$
	SELECT
		c.capacity_override_id,
		c.branch_id,
		c.override_date,
		c.capacity
	FROM public.branch_daily_capacity_overrides c
	WHERE c.branch_id = p_branch_id
	  AND (p_from_date IS NULL OR c.override_date >= p_from_date)
	  AND (p_to_date IS NULL OR c.override_date <= p_to_date)
	ORDER BY c.override_date, c.capacity_override_id;
$$;


CREATE OR REPLACE FUNCTION public.upsert_branch_daily_capacity_override(
	p_branch_id    BIGINT,
	p_override_date DATE,
	p_capacity     INTEGER
)
RETURNS BIGINT
LANGUAGE sql
AS $$
	INSERT INTO public.branch_daily_capacity_overrides (
		branch_id,
		override_date,
		capacity
	)
	VALUES (
		p_branch_id,
		p_override_date,
		p_capacity
	)
	ON CONFLICT (branch_id, override_date)
	DO UPDATE SET capacity = EXCLUDED.capacity
	RETURNING capacity_override_id;
$$;


CREATE OR REPLACE FUNCTION public.delete_branch_daily_capacity_override(
	p_capacity_override_id BIGINT
)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
	WITH deleted AS (
		DELETE FROM public.branch_daily_capacity_overrides
		WHERE capacity_override_id = p_capacity_override_id
		RETURNING 1
	)
	SELECT EXISTS (SELECT 1 FROM deleted);
$$;
