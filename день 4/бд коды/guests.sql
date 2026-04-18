-- Table: public.guests

-- DROP TABLE IF EXISTS public.guests;

CREATE TABLE IF NOT EXISTS public.guests
(
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    last_name character varying(100) COLLATE pg_catalog."default" NOT NULL,
    first_name character varying(100) COLLATE pg_catalog."default" NOT NULL,
    middle_name character varying(100) COLLATE pg_catalog."default",
    passport_series character varying(10) COLLATE pg_catalog."default",
    passport_number character varying(10) COLLATE pg_catalog."default",
    birth_date date NOT NULL,
    phone character varying(20) COLLATE pg_catalog."default",
    email character varying(100) COLLATE pg_catalog."default" NOT NULL,
    organization character varying(200) COLLATE pg_catalog."default",
    login character varying(50) COLLATE pg_catalog."default",
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT guests_pkey PRIMARY KEY (id),
    CONSTRAINT guests_email_key UNIQUE (email),
    CONSTRAINT guests_login_key UNIQUE (login)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.guests
    OWNER to postgres;

-- Trigger: trg_visitor_login

-- DROP TRIGGER IF EXISTS trg_visitor_login ON public.guests;

CREATE OR REPLACE TRIGGER trg_visitor_login
    BEFORE INSERT
    ON public.guests
    FOR EACH ROW
    EXECUTE FUNCTION public.generate_visitor_login();