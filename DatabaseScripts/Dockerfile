FROM flyway/flyway:10-alpine

COPY V*.sql /flyway/sql/

# Scripts use a single underscore (V1_Name.sql) rather than Flyway's default double underscore
ENV FLYWAY_SQL_MIGRATION_SEPARATOR=_

CMD ["migrate"]
