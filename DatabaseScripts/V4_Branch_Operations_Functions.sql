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


-- GetScheduleById
CREATE OR REPLACE FUNCTION public.get_schedule_by_id(p_schedule_id BIGINT)
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
    WHERE s.id = p_schedule_id;
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


-- GetSchedulesForBranch
-- Returns all schedules for a branch, newest first. Inactive ones are excluded unless requested.
CREATE OR REPLACE FUNCTION public.get_schedules_for_branch(
    p_branch_id        BIGINT,
    p_include_inactive BOOLEAN DEFAULT FALSE
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
      AND (COALESCE(p_include_inactive, FALSE) OR s.is_active)
    ORDER BY s.effective_from DESC, s.id DESC;
$$;


-- UpdateSchedule
-- Replaces effective_from, effective_to and is_active. Returns FALSE if the schedule doesn't exist.
-- p_effective_to = NULL means open-ended.
CREATE OR REPLACE FUNCTION public.update_schedule(
    p_schedule_id    BIGINT,
    p_effective_from DATE,
    p_effective_to   DATE,
    p_is_active      BOOLEAN
)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
    WITH updated AS (
        UPDATE public.branch_operational_schedules
        SET effective_from = p_effective_from,
            effective_to   = p_effective_to,
            is_active      = COALESCE(p_is_active, TRUE)
        WHERE id = p_schedule_id
        RETURNING 1
    )
    SELECT EXISTS (SELECT 1 FROM updated);
$$;


-- DeactivateSchedule
CREATE OR REPLACE FUNCTION public.deactivate_schedule(p_schedule_id BIGINT)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
    WITH updated AS (
        UPDATE public.branch_operational_schedules
        SET is_active = FALSE
        WHERE id = p_schedule_id
        RETURNING 1
    )
    SELECT EXISTS (SELECT 1 FROM updated);
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


-- UpdateWeeklyOperatingHours
-- Updates times, closed flag and capacity. The day and schedule of a row are fixed;
-- to move hours to another day, delete and recreate.
CREATE OR REPLACE FUNCTION public.update_weekly_operating_hours(
    p_weekly_hours_id BIGINT,
    p_opens_at        TIME,
    p_closes_at       TIME,
    p_is_closed       BOOLEAN,
    p_capacity        INTEGER
)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
    WITH updated AS (
        UPDATE public.branch_weekly_operating_hours
        SET opens_at   = p_opens_at,
            closes_at  = p_closes_at,
            is_closed  = COALESCE(p_is_closed, FALSE),
            capacity   = p_capacity
        WHERE weekly_hours_id = p_weekly_hours_id
        RETURNING 1
    )
    SELECT EXISTS (SELECT 1 FROM updated);
$$;


-- DeleteWeeklyOperatingHours
CREATE OR REPLACE FUNCTION public.delete_weekly_operating_hours(p_weekly_hours_id BIGINT)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
    WITH deleted AS (
        DELETE FROM public.branch_weekly_operating_hours
        WHERE weekly_hours_id = p_weekly_hours_id
        RETURNING 1
    )
    SELECT EXISTS (SELECT 1 FROM deleted);
$$;