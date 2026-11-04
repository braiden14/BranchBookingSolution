-- ============================================================
-- V16: Remove database objects no longer used by the application
-- ============================================================
-- Scope: objects confirmed unreferenced by the BranchBooking API,
-- application tests, and subsequent repository migrations.
--
-- This is intentionally append-only. Do not edit V1-V15 because those
-- scripts may already have been applied to existing databases.
--
-- WARNING: Apply only when no external reports, scripts, or integrations
-- depend on these objects.

DROP FUNCTION IF EXISTS public.get_schedule_by_id(BIGINT);
DROP FUNCTION IF EXISTS public.get_schedules_for_branch(BIGINT, BOOLEAN);
DROP FUNCTION IF EXISTS public.update_schedule(BIGINT, DATE, DATE, BOOLEAN);
DROP FUNCTION IF EXISTS public.deactivate_schedule(BIGINT);
DROP FUNCTION IF EXISTS public.update_weekly_operating_hours(BIGINT, TIME, TIME, BOOLEAN, INTEGER);
DROP FUNCTION IF EXISTS public.delete_weekly_operating_hours(BIGINT);

DROP TABLE IF EXISTS public.role_permissions;
DROP TABLE IF EXISTS public.permissions;
DROP TABLE IF EXISTS public.email_verification_tokens;
DROP TABLE IF EXISTS public.password_reset_tokens;
DROP TABLE IF EXISTS public.auth_events;
