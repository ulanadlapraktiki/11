-- Table: public.access_logs

-- DROP TABLE IF EXISTS public.access_logs;

CREATE TABLE IF NOT EXISTS public.access_logs
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    request_id integer NOT NULL,
    access_time date,
    access_type text COLLATE pg_catalog."default",
    CONSTRAINT access_logs_pkey PRIMARY KEY (id, request_id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.access_logs
    OWNER to postgres;