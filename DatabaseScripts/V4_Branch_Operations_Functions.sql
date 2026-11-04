-- ============================================================
-- SCHEDULES (public.branch_operational_schedules)
-- ============================================================

-- CreateSchedule
CREATE OR REPLACE FUNCTION public.create_schedule(
    p_branch_id      BIGINT,
    p_effective_from DATE,
    p_effective_to   DATE    DEFAULT NULL,
    p_is_active      BOOLEAN DEFAULT TRUE
)
RETURNS BIGINT
LANGUAGE sql
AS $$
    INSERT INTO public.branch_operational_schedules (
        branch_id, effective_from, effective_to, is_active
    )
    VALUES (
        p_branch_id, p_effective_from, p_effective_to, COALESCE(p_is_active, TRUE)
    )
    RETURNING id;
$$;


-- GetScheduleByBranchId
-- Returns the single active schedule in effect for the branch on a given date (default: today).
CREATE OR REPLACE FUNCTION public.get_schedule_by_branch_id(
    p_branch_id  BIGINT,
    p_as_of_date DATE DEFAULT CURRENT_DATE
)
RETURNS TABLE (
    id             BIGINT,
    branch_id      BIGINT,
    effective_from DATE,
    effective_to   DATE,
    is_active      BOOLEAN
)
LANGUAGE sql
STABLE
AS $$
    SELECT s.id, s.branch_id, s.effective_from, s.effective_to, s.is_active
    FROM public.branch_operational_schedules s
    WHERE s.branch_id = p_branch_id
      AND s.is_active
      AND s.effective_from <= COALESCE(p_as_of_date, CURRENT_DATE)
      AND (s.effective_to IS NULL OR s.effective_to >= COALESCE(p_as_of_date, CURRENT_DATE))
    ORDER BY s.effective_from DESC
    LIMIT 1;
$$;


-- ============================================================
-- WEEKLY OPERATING HOURS (public.branch_weekly_operating_hours)
-- ============================================================

-- CreateWeeklyOperatingHours
CREATE OR REPLACE FUNCTION public.create_weekly_operating_hours(
    p_schedule_id BIGINT,
    p_day_of_week SMALLINT,
    p_opens_at    TIME    DEFAULT NULL,
    p_closes_at   TIME    DEFAULT NULL,
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
        p_schedule_id, p_day_of_week, p_opens_at, p_closes_at,
        COALESCE(p_is_closed, FALSE), p_capacity
    )
    RETURNING weekly_hours_id;
$$;


-- GetWeeklyOperatingHoursByScheduleId
CREATE OR REPLACE FUNCTION public.get_weekly_operating_hours_by_schedule_id(p_schedule_id BIGINT)
RETURNS TABLE (
    weekly_hours_id BIGINT,
    schedule_id     BIGINT,
    day_of_week     SMALLINT,
    opens_at        TIME,
    closes_at       TIME,
    is_closed       BOOLEAN,
    capacity        INTEGER
)
LANGUAGE sql
STABLE
AS $$
    SELECT w.weekly_hours_id, w.schedule_id, w.day_of_week,
           w.opens_at, w.closes_at, w.is_closed, w.capacity
    FROM public.branch_weekly_operating_hours w
    WHERE w.schedule_id = p_schedule_id
    ORDER BY w.day_of_week;
$$;


