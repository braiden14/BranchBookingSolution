CREATE OR REPLACE FUNCTION public.get_all_branches(
    p_page_number INT DEFAULT 1,
    p_page_size   INT DEFAULT 5,
    p_search_term VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id               BIGINT,
    code             VARCHAR,
    name             VARCHAR,
    address_line_1   VARCHAR,
    city             VARCHAR,
    province         VARCHAR,
    postal_code      VARCHAR,
    status           SMALLINT,
    accepts_bookings BOOLEAN,
    created_at_utc   TIMESTAMPTZ,
    total_count      BIGINT
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        b.id,
        b.code,
        b.name,
        b.address_line_1,
        b.city,
        b.province,
        b.postal_code,
        b.status,
        b.accepts_bookings,
        b.created_at_utc,
        COUNT(*) OVER () AS total_count
    FROM public.branches b
    WHERE NULLIF(BTRIM(p_search_term), '') IS NULL
       OR CONCAT_WS(' ', b.code, b.name, b.address_line_1, b.city, b.province, b.postal_code)
          ILIKE '%' || BTRIM(p_search_term) || '%'
    ORDER BY b.id
    LIMIT  GREATEST(LEAST(COALESCE(p_page_size, 5), 200), 1)
    OFFSET (GREATEST(COALESCE(p_page_number, 1), 1) - 1)
           * GREATEST(LEAST(COALESCE(p_page_size, 5), 200), 1);
$$;

CREATE OR REPLACE FUNCTION public.create_branch(
    p_code             VARCHAR,
    p_name             VARCHAR,
    p_address_line_1   VARCHAR,
    p_city             VARCHAR,
    p_province         VARCHAR,
    p_postal_code      VARCHAR,
    p_status           SMALLINT DEFAULT 1,
    p_accepts_bookings BOOLEAN  DEFAULT TRUE
)
RETURNS BIGINT
LANGUAGE sql
AS $$
    INSERT INTO public.branches (
        code,
        name,
        address_line_1,
        city,
        province,
        postal_code,
        status,
        accepts_bookings
    )
    VALUES (
        p_code,
        p_name,
        p_address_line_1,
        p_city,
        p_province,
        p_postal_code,
        COALESCE(p_status, 1),
        COALESCE(p_accepts_bookings, TRUE)
    )
    RETURNING id;
$$;