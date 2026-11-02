CREATE TABLE branches (
    id               BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code             VARCHAR(50)  NOT NULL,
    name             VARCHAR(200) NOT NULL,
    address_line_1   VARCHAR(255) NOT NULL,
    city             VARCHAR(100) NOT NULL,
    province         VARCHAR(100) NOT NULL,
    postal_code      VARCHAR(20)  NOT NULL,
    status           SMALLINT     NOT NULL DEFAULT 1,
    accepts_bookings BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at_utc   TIMESTAMPTZ  NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),

    CONSTRAINT uq_branches_code UNIQUE (code)
);