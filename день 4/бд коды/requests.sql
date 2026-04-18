-- Table: public.requests

-- DROP TABLE IF EXISTS public.requests;

CREATE TABLE IF NOT EXISTS public.requests
(
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    guest_id uuid,
    employee_id uuid,
    requesttype character varying(20) COLLATE pg_catalog."default" DEFAULT 'личная'::character varying,
    status character varying(30) COLLATE pg_catalog."default" DEFAULT 'проверка'::character varying,
    reject_reason text COLLATE pg_catalog."default",
    start_date date NOT NULL,
    end_date date NOT NULL,
    visit_purpose text COLLATE pg_catalog."default",
    target_department character varying(200) COLLATE pg_catalog."default",
    note text COLLATE pg_catalog."default",
    entry_time timestamp without time zone,
    exit_time timestamp without time zone,
    arrival_time timestamp without time zone,
    travel_time_minutes integer DEFAULT 30,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT requests_pkey PRIMARY KEY (id),
    CONSTRAINT requests_employee_id_fkey FOREIGN KEY (employee_id)
        REFERENCES public.employees (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT requests_guest_id_fkey FOREIGN KEY (guest_id)
        REFERENCES public.guests (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.requests
    OWNER to postgres;