-- Table: public.employees

-- DROP TABLE IF EXISTS public.employees;

CREATE TABLE IF NOT EXISTS public.employees
(
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    login character varying(50) COLLATE pg_catalog."default" NOT NULL,
    password_hash character varying(255) COLLATE pg_catalog."default" NOT NULL,
    role character varying(30) COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT employees_pkey PRIMARY KEY (id),
    CONSTRAINT employees_login_key UNIQUE (login),
    CONSTRAINT employees_role_check CHECK (role::text = ANY (ARRAY['general'::character varying, 'security'::character varying, 'head'::character varying]::text[]))
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.employees
    OWNER to postgres;