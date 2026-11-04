-- V13 - Realistic development/test data
-- Run after V1 through V12 have been applied.
-- This script is intended for local development and test environments only.
-- It is safe to run repeatedly: branch and service codes are stable.

BEGIN;

-- -----------------------------------------------------------------------------
-- Offered service catalogue
-- -----------------------------------------------------------------------------
INSERT INTO public.offered_services (code, name, service_duration_minutes, is_active)
VALUES
	('ID_RENEWAL', 'Identity document renewal', 30, TRUE),
	('PASSPORT', 'Passport application', 45, TRUE),
	('DRIVERS_LICENCE', 'Driving licence renewal', 30, TRUE),
	('VEHICLE_LICENCE', 'Motor vehicle licence renewal', 20, TRUE),
	('BIRTH_CERTIFICATE', 'Birth certificate application', 30, TRUE),
	('BUSINESS_REGISTRATION', 'Business registration support', 60, TRUE),
	('DOCUMENT_CERTIFICATION', 'Document certification', 20, TRUE)
ON CONFLICT (code) DO UPDATE
SET name = EXCLUDED.name,
	service_duration_minutes = EXCLUDED.service_duration_minutes,
	is_active = EXCLUDED.is_active;

-- -----------------------------------------------------------------------------
-- Branches
-- -----------------------------------------------------------------------------
INSERT INTO public.branches
	(code, name, address_line_1, city, province, postal_code, status, accepts_bookings)
VALUES
	('CPT_CBD', 'Cape Town Central', '12 Long Street', 'Cape Town', 'Western Cape', '8001', 1, TRUE),
	('JHB_SANDTON', 'Sandton City', '83 Rivonia Road', 'Sandton', 'Gauteng', '2196', 1, TRUE),
	('DBN_MARINE', 'Durban Marine Parade', '45 Marine Parade', 'Durban', 'KwaZulu-Natal', '4001', 1, TRUE)
ON CONFLICT (code) DO UPDATE
SET name = EXCLUDED.name,
	address_line_1 = EXCLUDED.address_line_1,
	city = EXCLUDED.city,
	province = EXCLUDED.province,
	postal_code = EXCLUDED.postal_code,
	status = EXCLUDED.status,
	accepts_bookings = EXCLUDED.accepts_bookings;

-- -----------------------------------------------------------------------------
-- Branch service offerings
-- -----------------------------------------------------------------------------
INSERT INTO public.branch_services (branch_id, service_id, is_active)
SELECT b.id, s.id, TRUE
FROM (VALUES
	('CPT_CBD', 'ID_RENEWAL'),
	('CPT_CBD', 'PASSPORT'),
	('CPT_CBD', 'DRIVERS_LICENCE'),
	('CPT_CBD', 'BIRTH_CERTIFICATE'),
	('CPT_CBD', 'DOCUMENT_CERTIFICATION'),
	('JHB_SANDTON', 'ID_RENEWAL'),
	('JHB_SANDTON', 'PASSPORT'),
	('JHB_SANDTON', 'DRIVERS_LICENCE'),
	('JHB_SANDTON', 'VEHICLE_LICENCE'),
	('JHB_SANDTON', 'BUSINESS_REGISTRATION'),
	('DBN_MARINE', 'ID_RENEWAL'),
	('DBN_MARINE', 'PASSPORT'),
	('DBN_MARINE', 'VEHICLE_LICENCE'),
	('DBN_MARINE', 'BIRTH_CERTIFICATE'),
	('DBN_MARINE', 'DOCUMENT_CERTIFICATION')
) AS offerings(branch_code, service_code)
JOIN public.branches b ON b.code = offerings.branch_code
JOIN public.offered_services s ON s.code = offerings.service_code
ON CONFLICT (branch_id, service_id) DO UPDATE
SET is_active = EXCLUDED.is_active;

-- -----------------------------------------------------------------------------
-- One active operating schedule per branch
-- -----------------------------------------------------------------------------
INSERT INTO public.branch_operational_schedules (branch_id, effective_from, effective_to, is_active)
SELECT b.id, DATE '2025-01-01', NULL, TRUE
FROM public.branches b
WHERE b.code IN ('CPT_CBD', 'JHB_SANDTON', 'DBN_MARINE')
  AND NOT EXISTS (
	  SELECT 1
	  FROM public.branch_operational_schedules existing
	  WHERE existing.branch_id = b.id
		AND existing.effective_from = DATE '2025-01-01'
  );

-- Keep this as a separate statement. PostgreSQL data-modifying CTEs use the
-- statement snapshot, so rows inserted by a schedule CTE are not visible to a
-- SELECT in the same statement.
INSERT INTO public.branch_weekly_operating_hours
	(schedule_id, day_of_week, opens_at, closes_at, is_closed, capacity)
SELECT
	schedule_id,
	day_of_week,
	CASE WHEN day_of_week = 1 THEN NULL WHEN day_of_week = 7 THEN TIME '09:00' ELSE TIME '08:00' END,
	CASE WHEN day_of_week = 1 THEN NULL WHEN day_of_week = 7 THEN TIME '13:00' ELSE TIME '16:30' END,
	day_of_week = 1,
	CASE WHEN day_of_week = 1 THEN NULL WHEN day_of_week = 7 THEN 4 ELSE 8 END
FROM (
	SELECT s.id AS schedule_id, d.day_of_week
	FROM public.branch_operational_schedules s
	CROSS JOIN generate_series(1, 7) AS d(day_of_week)
	WHERE s.effective_from = DATE '2025-01-01'
	  AND s.branch_id IN (
		  SELECT id FROM public.branches WHERE code IN ('CPT_CBD', 'JHB_SANDTON', 'DBN_MARINE')
	  )
) AS schedule_days
ON CONFLICT (schedule_id, day_of_week) DO UPDATE
SET opens_at = EXCLUDED.opens_at,
	closes_at = EXCLUDED.closes_at,
	is_closed = EXCLUDED.is_closed,
	capacity = EXCLUDED.capacity;

COMMIT;

-- Quick verification:
-- SELECT code, name, is_active FROM public.offered_services ORDER BY code;
-- SELECT code, name, city FROM public.branches WHERE code IN ('CPT_CBD', 'JHB_SANDTON', 'DBN_MARINE');
-- SELECT b.code, s.code, bs.is_active
-- FROM public.branch_services bs
-- JOIN public.branches b ON b.id = bs.branch_id
-- JOIN public.offered_services s ON s.id = bs.service_id
-- ORDER BY b.code, s.code;
