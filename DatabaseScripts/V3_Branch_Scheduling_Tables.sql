-- 1. Service catalogue
CREATE TABLE public.offered_services (
    id                       BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code                     VARCHAR(50)  NOT NULL,
    name                     VARCHAR(200) NOT NULL,
    service_duration_minutes INTEGER      NOT NULL,
    created_at_utc           TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT uq_offered_services_code UNIQUE (code),
    CONSTRAINT ck_offered_services_duration CHECK (service_duration_minutes > 0)
);

-- 2. Which services each branch offers
CREATE TABLE public.branch_services (
    branch_service_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    branch_id         BIGINT      NOT NULL,
    service_id        BIGINT      NOT NULL,
    is_active         BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at_utc    TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_branch_services_branch
        FOREIGN KEY (branch_id)  REFERENCES public.branches (id),
    CONSTRAINT fk_branch_services_service
        FOREIGN KEY (service_id) REFERENCES public.offered_services (id),
    CONSTRAINT uq_branch_services_branch_service UNIQUE (branch_id, service_id)
);

CREATE INDEX ix_branch_services_service_id ON public.branch_services (service_id);

-- 3. Operating schedules (date-ranged versions of a branch's hours)
CREATE TABLE public.branch_operational_schedules (
    id             BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    branch_id      BIGINT  NOT NULL,
    effective_from DATE    NOT NULL,
    effective_to   DATE    NULL,  -- NULL = open-ended
    is_active      BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT fk_branch_schedules_branch
        FOREIGN KEY (branch_id) REFERENCES public.branches (id),
    CONSTRAINT ck_branch_schedules_dates
        CHECK (effective_to IS NULL OR effective_to >= effective_from)
);

CREATE INDEX ix_branch_schedules_branch_dates
    ON public.branch_operational_schedules (branch_id, effective_from, effective_to);

-- 4. Standard weekly hours for a schedule
CREATE TABLE public.branch_weekly_operating_hours (
    weekly_hours_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    schedule_id     BIGINT   NOT NULL,
    day_of_week     SMALLINT NOT NULL,  -- 1 = Sunday ... 7 = Saturday (matches .NET DayOfWeek)
    opens_at        TIME     NULL,
    closes_at       TIME     NULL,
    is_closed       BOOLEAN  NOT NULL DEFAULT FALSE,
    capacity        INTEGER  NULL,

    CONSTRAINT fk_weekly_hours_schedule
        FOREIGN KEY (schedule_id) REFERENCES public.branch_operational_schedules (id)
        ON DELETE CASCADE,
    CONSTRAINT uq_weekly_hours_schedule_day UNIQUE (schedule_id, day_of_week),
    CONSTRAINT ck_weekly_hours_day CHECK (day_of_week BETWEEN 1 AND 7),
    CONSTRAINT ck_weekly_hours_times CHECK (
        is_closed OR (opens_at IS NOT NULL AND closes_at IS NOT NULL AND opens_at < closes_at)
    ),
    CONSTRAINT ck_weekly_hours_capacity CHECK (capacity IS NULL OR capacity >= 0)
);

-- 5. Specific-date overrides (holidays, special days)
CREATE TABLE public.branch_date_overrides (
    override_id   BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    schedule_id   BIGINT       NOT NULL,
    override_date DATE         NOT NULL,
    reason        VARCHAR(255) NULL,
    is_closed     BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT fk_date_overrides_schedule
        FOREIGN KEY (schedule_id) REFERENCES public.branch_operational_schedules (id)
        ON DELETE CASCADE,
    CONSTRAINT uq_date_overrides_schedule_date UNIQUE (schedule_id, override_date)
);

-- 6. Custom hours for an override date (when not fully closed)
CREATE TABLE public.branch_date_operating_hours (
    date_hours_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    override_id   BIGINT  NOT NULL,
    opens_at      TIME    NOT NULL,
    closes_at     TIME    NOT NULL,
    capacity      INTEGER NULL,

    CONSTRAINT fk_date_hours_override
        FOREIGN KEY (override_id) REFERENCES public.branch_date_overrides (override_id)
        ON DELETE CASCADE,
    CONSTRAINT ck_date_hours_times CHECK (opens_at < closes_at),
    CONSTRAINT ck_date_hours_capacity CHECK (capacity IS NULL OR capacity >= 0)
);

CREATE INDEX ix_date_hours_override_id ON public.branch_date_operating_hours (override_id);

-- 7. Per-day capacity overrides (independent of schedules)
CREATE TABLE public.branch_daily_capacity_overrides (
    capacity_override_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    branch_id            BIGINT  NOT NULL,
    override_date        DATE    NOT NULL,
    capacity             INTEGER NOT NULL,

    CONSTRAINT fk_capacity_overrides_branch
        FOREIGN KEY (branch_id) REFERENCES public.branches (id),
    CONSTRAINT uq_capacity_overrides_branch_date UNIQUE (branch_id, override_date),
    CONSTRAINT ck_capacity_overrides_capacity CHECK (capacity >= 0)
);