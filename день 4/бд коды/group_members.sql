-- Table: public.group_members

-- DROP TABLE IF EXISTS public.group_members;

CREATE TABLE IF NOT EXISTS public.group_members
(
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    request_id uuid,
    guest_id uuid,
    CONSTRAINT group_members_pkey PRIMARY KEY (id),
    CONSTRAINT group_members_guest_id_fkey FOREIGN KEY (guest_id)
        REFERENCES public.guests (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT group_members_request_id_fkey FOREIGN KEY (request_id)
        REFERENCES public.requests (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.group_members
    OWNER to postgres;