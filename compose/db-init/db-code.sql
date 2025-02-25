--
-- PostgreSQL database dump
--

-- Dumped from database version 17.0 (Debian 17.0-1.pgdg120+1)
-- Dumped by pg_dump version 17.0 (Debian 17.0-1.pgdg120+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: poems_search_vector_update(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.generate_search_vector(author text, title text, content text)
RETURNS tsvector AS $$
BEGIN
    RETURN setweight(to_tsvector('russian', author), 'A') ||
           setweight(to_tsvector('russian', title), 'B') ||
           setweight(to_tsvector('russian', content), 'C');
END;
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION public.poems_search_vector_update()
RETURNS trigger AS $$
DECLARE
	author_name text;
BEGIN
    SELECT name
	INTO author_name
	FROM public.authors
	WHERE id = NEW.author_id;
    
	NEW.searchvector := public.generate_search_vector(author_name, NEW.title, NEW.content);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;



SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: authors; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.authors (
    id serial NOT NULL,
    name text NOT NULL
);

--
-- Name: poems; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.poems (
    id serial NOT NULL,
    title text NOT NULL,
    content text NOT NULL,
    author_id integer NOT NULL,
    searchvector tsvector
);

--
-- Name: authors authors_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.authors
    ADD CONSTRAINT authors_pk PRIMARY KEY (id);

--
-- Name: poems poems_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.poems
    ADD CONSTRAINT poems_pk PRIMARY KEY (id);


--
-- Name: poems_search_vector_idx; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX poems_search_vector_idx ON public.poems USING gin (searchvector);


--
-- Name: poems poems_search_vector_update; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER poems_search_vector_update BEFORE INSERT OR UPDATE ON public.poems FOR EACH ROW EXECUTE FUNCTION public.poems_search_vector_update();


--
-- Name: poems poems_authors_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.poems
    ADD CONSTRAINT poems_authors_fk FOREIGN KEY (author_id) REFERENCES public.authors(id);


--
-- PostgreSQL database dump complete
--
