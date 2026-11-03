CREATE TABLE IF NOT EXISTS public.appointments (
	id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	customer_id BIGINT NOT NULL REFERENCES public.users(id),
	branch_id BIGINT NOT NULL REFERENCES public.branches(id),
	service_id BIGINT NOT NULL REFERENCES public.offered_services(id),
	starts_at_utc TIMESTAMPTZ NOT NULL,
	ends_at_utc TIMESTAMPTZ NOT NULL,
	status VARCHAR(20) NOT NULL DEFAULT 'Confirmed',
	created_at_utc TIMESTAMPTZ NOT NULL DEFAULT now(),
	CONSTRAINT ck_appointments_range CHECK (ends_at_utc > starts_at_utc),
	CONSTRAINT ck_appointments_status CHECK (status IN ('Confirmed', 'Cancelled'))
);
CREATE INDEX IF NOT EXISTS ix_appointments_customer_time ON public.appointments(customer_id, starts_at_utc);
CREATE INDEX IF NOT EXISTS ix_appointments_branch_time ON public.appointments(branch_id, starts_at_utc) WHERE status = 'Confirmed';
