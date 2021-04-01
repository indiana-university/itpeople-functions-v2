-- Add EF migrations table to existing DBA PSQL database.
-- Pretend as though the initial migration has already run.

-- Table: public.__EFMigrationsHistory
CREATE TABLE public."__EFMigrationsHistory"
(
    migration_id character varying(150) COLLATE pg_catalog."default" NOT NULL,
    product_version character varying(32) COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

-- Change the owner to the correct DB user
ALTER TABLE public."__EFMigrationsHistory" OWNER TO itpeople_owner;

-- Pretend as though the initial migration has already run.
INSERT INTO public."__EFMigrationsHistory"(
	migration_id, product_version)
	VALUES ('20210119163805_ImportExistingSchema', '3.1.11');