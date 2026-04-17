-- Table: public.requests

-- DROP TABLE IF EXISTS public.requests;

CREATE TABLE IF NOT EXISTS public.requests
(
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    guest_id uuid NOT NULL,
    employee_id uuid,
    start_date date NOT NULL,
    end_date date NOT NULL,
    status character varying(30) COLLATE pg_catalog."default" DEFAULT 'new'::character varying,
    CONSTRAINT requests_pkey PRIMARY KEY (id),
    CONSTRAINT requests_employee_id_fkey FOREIGN KEY (employee_id)
        REFERENCES public.employees (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT requests_guest_id_fkey FOREIGN KEY (guest_id)
        REFERENCES public.guests (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.requests
    OWNER to postgres;