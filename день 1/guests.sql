-- Table: public.guests

-- DROP TABLE IF EXISTS public.guests;

CREATE TABLE IF NOT EXISTS public.guests
(
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    last_name character varying(100) COLLATE pg_catalog."default" NOT NULL,
    first_name character varying(100) COLLATE pg_catalog."default" NOT NULL,
    middle_name character varying(100) COLLATE pg_catalog."default",
    passport_series character varying(10) COLLATE pg_catalog."default",
    passport_number character varying(20) COLLATE pg_catalog."default",
    birth_date date,
    phone character varying(20) COLLATE pg_catalog."default" NOT NULL,
    email character varying(100) COLLATE pg_catalog."default",
    CONSTRAINT guests_pkey PRIMARY KEY (id),
    CONSTRAINT guests_passport_number_key UNIQUE (passport_number)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.guests
    OWNER to postgres;