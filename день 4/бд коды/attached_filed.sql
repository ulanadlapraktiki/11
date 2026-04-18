-- Table: public.attached_files

-- DROP TABLE IF EXISTS public.attached_files;

CREATE TABLE IF NOT EXISTS public.attached_files
(
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    request_id uuid,
    file_type character varying(50) COLLATE pg_catalog."default",
    file_path character varying(500) COLLATE pg_catalog."default",
    file_name character varying(200) COLLATE pg_catalog."default",
    uploaded_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT attached_files_pkey PRIMARY KEY (id),
    CONSTRAINT attached_files_request_id_fkey FOREIGN KEY (request_id)
        REFERENCES public.requests (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.attached_files
    OWNER to postgres;