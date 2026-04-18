-- Table: public.employees

-- DROP TABLE IF EXISTS public.employees;

CREATE TABLE IF NOT EXISTS public.employees
(
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    code character varying(20) COLLATE pg_catalog."default" NOT NULL,
    login character varying(50) COLLATE pg_catalog."default" NOT NULL,
    password_hash character varying(32) COLLATE pg_catalog."default" NOT NULL,
    role character varying(30) COLLATE pg_catalog."default" DEFAULT 'user'::character varying,
    department character varying(200) COLLATE pg_catalog."default",
    CONSTRAINT employees_pkey PRIMARY KEY (id),
    CONSTRAINT employees_code_key UNIQUE (code),
    CONSTRAINT employees_login_key UNIQUE (login)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.employees
    OWNER to postgres;