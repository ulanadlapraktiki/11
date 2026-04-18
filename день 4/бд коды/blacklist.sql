-- Table: public.blacklist

-- DROP TABLE IF EXISTS public.blacklist;

CREATE TABLE IF NOT EXISTS public.blacklist
(
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    guest_id uuid,
    reason text COLLATE pg_catalog."default" NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT blacklist_pkey PRIMARY KEY (id),
    CONSTRAINT blacklist_guest_id_fkey FOREIGN KEY (guest_id)
        REFERENCES public.guests (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.blacklist
    OWNER to postgres;