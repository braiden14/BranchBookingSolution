-- V14 - Paginated offered-service catalogue lookup
-- Apply after V1 through V13. This is separate from V5 so existing databases
-- can add the paginated lookup without re-running earlier scripts.

CREATE OR REPLACE FUNCTION public.get_offered_services_page(
	p_page_number INT DEFAULT 1,
	p_page_size   INT DEFAULT 10,
	p_search_term VARCHAR DEFAULT NULL,
	p_is_active   BOOLEAN DEFAULT NULL
)
RETURNS TABLE (
	id                       BIGINT,
	code                     VARCHAR,
	name                     VARCHAR,
	service_duration_minutes INTEGER,
	is_active                BOOLEAN,
	created_at_utc           TIMESTAMPTZ,
	total_count              BIGINT
)
LANGUAGE sql
STABLE
AS $$
	SELECT
		s.id,
		s.code,
		s.name,
		s.service_duration_minutes,
		s.is_active,
		s.created_at_utc,
		COUNT(*) OVER () AS total_count
	FROM public.offered_services s
	WHERE (NULLIF(BTRIM(p_search_term), '') IS NULL
		   OR s.code ILIKE '%' || BTRIM(p_search_term) || '%'
		   OR s.name ILIKE '%' || BTRIM(p_search_term) || '%')
	  AND (p_is_active IS NULL OR s.is_active = p_is_active)
	ORDER BY s.name, s.id
	LIMIT GREATEST(LEAST(COALESCE(p_page_size, 10), 200), 1)
	OFFSET (GREATEST(COALESCE(p_page_number, 1), 1) - 1)
		   * GREATEST(LEAST(COALESCE(p_page_size, 10), 200), 1);
$$;
