-- V15 - API correctness fixes
-- Apply after V1 through V14.

CREATE OR REPLACE FUNCTION public.create_weekly_operating_hours(
	p_schedule_id BIGINT,
	p_day_of_week SMALLINT,
	p_opens_at    TIME DEFAULT NULL,
	p_closes_at   TIME DEFAULT NULL,
	p_is_closed   BOOLEAN DEFAULT FALSE,
	p_capacity    INTEGER DEFAULT NULL
)
RETURNS BIGINT
LANGUAGE sql
AS $$
	INSERT INTO public.branch_weekly_operating_hours (
		schedule_id, day_of_week, opens_at, closes_at, is_closed, capacity
	)
	VALUES (
		p_schedule_id,
		p_day_of_week,
		CASE WHEN COALESCE(p_is_closed, FALSE) THEN NULL ELSE p_opens_at END,
		CASE WHEN COALESCE(p_is_closed, FALSE) THEN NULL ELSE p_closes_at END,
		COALESCE(p_is_closed, FALSE),
		CASE WHEN COALESCE(p_is_closed, FALSE) THEN 0 ELSE p_capacity END
	)
	RETURNING weekly_hours_id;
$$;
