CREATE EXTENSION IF NOT EXISTS citext;

CREATE TABLE IF NOT EXISTS public.users (
	id                   BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	email                CITEXT NOT NULL,
	password_hash        VARCHAR(500) NOT NULL,
	first_name           VARCHAR(100) NOT NULL,
	last_name            VARCHAR(100) NOT NULL,
	phone_number         VARCHAR(30),
	status               SMALLINT NOT NULL DEFAULT 0,
	email_confirmed      BOOLEAN NOT NULL DEFAULT FALSE,
	failed_login_count   INTEGER NOT NULL DEFAULT 0,
	locked_until_utc     TIMESTAMPTZ,
	last_login_at_utc    TIMESTAMPTZ,
	authorization_version INTEGER NOT NULL DEFAULT 1,
	created_at_utc       TIMESTAMPTZ NOT NULL DEFAULT now(),
	updated_at_utc       TIMESTAMPTZ NOT NULL DEFAULT now(),
	CONSTRAINT ck_users_status CHECK (status IN (0, 1, 2, 3, 4)),
	CONSTRAINT ck_users_failed_login_count CHECK (failed_login_count >= 0)
);

CREATE TABLE IF NOT EXISTS public.roles (
	id                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	code              VARCHAR(50) NOT NULL UNIQUE,
	name              VARCHAR(100) NOT NULL,
	description       VARCHAR(500),
	created_at_utc    TIMESTAMPTZ NOT NULL DEFAULT now(),
	updated_at_utc    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.user_roles (
	user_id              BIGINT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
	role_id              BIGINT NOT NULL REFERENCES public.roles(id) ON DELETE RESTRICT,
	assigned_by_user_id  BIGINT REFERENCES public.users(id) ON DELETE SET NULL,
	assigned_at_utc      TIMESTAMPTZ NOT NULL DEFAULT now(),
	revoked_at_utc       TIMESTAMPTZ,
	PRIMARY KEY (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS public.customer_profiles (
	user_id                    BIGINT PRIMARY KEY REFERENCES public.users(id) ON DELETE CASCADE,
	preferred_contact_method   VARCHAR(30),
	marketing_opt_in           BOOLEAN NOT NULL DEFAULT FALSE,
	created_at_utc             TIMESTAMPTZ NOT NULL DEFAULT now(),
	updated_at_utc             TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.staff_profiles (
	user_id             BIGINT PRIMARY KEY REFERENCES public.users(id) ON DELETE CASCADE,
	employee_number     VARCHAR(50) UNIQUE,
	job_title           VARCHAR(150),
	hired_on            DATE,
	created_at_utc      TIMESTAMPTZ NOT NULL DEFAULT now(),
	updated_at_utc      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.user_sessions (
	id                       BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	user_id                  BIGINT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
	family_id                UUID NOT NULL,
	refresh_token_hash       VARCHAR(128) NOT NULL UNIQUE,
	replaced_by_session_id   BIGINT REFERENCES public.user_sessions(id) ON DELETE SET NULL,
	expires_at_utc           TIMESTAMPTZ NOT NULL,
	last_used_at_utc         TIMESTAMPTZ,
	revoked_at_utc           TIMESTAMPTZ,
	revoke_reason            VARCHAR(100),
	ip_address               VARCHAR(64),
	user_agent               VARCHAR(500),
	created_at_utc           TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.staff_invitations (
	id                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	user_id           BIGINT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
	token_hash        VARCHAR(128) NOT NULL UNIQUE,
	role_id           BIGINT NOT NULL REFERENCES public.roles(id) ON DELETE RESTRICT,
	invited_by_user_id BIGINT NOT NULL REFERENCES public.users(id) ON DELETE RESTRICT,
	expires_at_utc    TIMESTAMPTZ NOT NULL,
	accepted_at_utc   TIMESTAMPTZ,
	created_at_utc    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_user_sessions_user_id ON public.user_sessions(user_id);
CREATE INDEX IF NOT EXISTS ix_user_sessions_family_id ON public.user_sessions(family_id);
CREATE INDEX IF NOT EXISTS ix_staff_invitations_user_id ON public.staff_invitations(user_id);

INSERT INTO public.roles (code, name, description)
VALUES
	('Admin', 'Administrator', 'Full system access'),
	('Staff', 'Staff', 'Staff access to branch operations'),
	('Customer', 'Customer', 'Customer booking access')
ON CONFLICT (code) DO NOTHING;
