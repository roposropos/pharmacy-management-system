-- Initial PostgreSQL schema for the Apteka desktop application.
-- Run this script while connected to the target database, e.g. "Apteka".

DO $$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'apteka_app') THEN
		CREATE ROLE apteka_app LOGIN PASSWORD 'apteka_app';
	END IF;

	IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'apteka_farmaceuta') THEN
		CREATE ROLE apteka_farmaceuta LOGIN PASSWORD 'farmaceuta';
	END IF;

	IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'apteka_kierownik') THEN
		CREATE ROLE apteka_kierownik LOGIN PASSWORD 'kierownik';
	END IF;
END
$$;

ALTER ROLE apteka_app PASSWORD 'apteka_app';
ALTER ROLE apteka_farmaceuta PASSWORD 'farmaceuta';
ALTER ROLE apteka_kierownik PASSWORD 'kierownik';

CREATE SCHEMA IF NOT EXISTS apteka;
CREATE SCHEMA IF NOT EXISTS magazyn;
CREATE SCHEMA IF NOT EXISTS uzytkownicy;

CREATE TABLE IF NOT EXISTS apteka.adresy (
	id_adresu serial PRIMARY KEY,
	ulica varchar(120),
	nr_domu varchar(20) NOT NULL,
	nr_lokalu varchar(20),
	kod_pocztowy varchar(12) NOT NULL,
	miasto varchar(80) NOT NULL,
	kraj varchar(80) NOT NULL DEFAULT 'Polska'
);

CREATE TABLE IF NOT EXISTS apteka.osoby (
	id_osoby serial PRIMARY KEY,
	imie varchar(80) NOT NULL,
	nazwisko varchar(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS apteka.numery_telefonu (
	id_telefonu serial PRIMARY KEY,
	numer varchar(30) NOT NULL,
	opis varchar(80)
);

CREATE TABLE IF NOT EXISTS apteka.pozycja_numeru (
	id_osoby_osoby integer NOT NULL REFERENCES apteka.osoby(id_osoby) ON DELETE CASCADE,
	id_telefonu_numery_telefonu integer NOT NULL REFERENCES apteka.numery_telefonu(id_telefonu) ON DELETE CASCADE,
	PRIMARY KEY (id_osoby_osoby, id_telefonu_numery_telefonu)
);

CREATE TABLE IF NOT EXISTS apteka.klienci (
	id_klienta serial PRIMARY KEY,
	pesel varchar(512) NOT NULL,
	pesel_hash char(64) CHECK (pesel_hash IS NULL OR pesel_hash ~ '^[0-9a-f]{64}$'),
	id_adresu_adresy integer NOT NULL REFERENCES apteka.adresy(id_adresu),
	id_osoby_osoby integer NOT NULL UNIQUE REFERENCES apteka.osoby(id_osoby) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_klienci_pesel_hash
	ON apteka.klienci (pesel_hash)
	WHERE pesel_hash IS NOT NULL;

CREATE TABLE IF NOT EXISTS apteka.lekarze (
	id_lekarza serial PRIMARY KEY,
	"numer_PWZ" integer NOT NULL UNIQUE CHECK ("numer_PWZ" > 0),
	id_osoby_osoby integer NOT NULL UNIQUE REFERENCES apteka.osoby(id_osoby) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS apteka.producenci (
	id_producenta serial PRIMARY KEY,
	nazwa varchar(160) NOT NULL UNIQUE,
	id_adresu_adresy integer NOT NULL REFERENCES apteka.adresy(id_adresu)
);

CREATE TABLE IF NOT EXISTS apteka.leki (
	id_leku serial PRIMARY KEY,
	nazwa varchar(160) NOT NULL,
	bez_recepty boolean NOT NULL DEFAULT false,
	substancja_czynna varchar(160) NOT NULL,
	id_producenta_producenci integer NOT NULL REFERENCES apteka.producenci(id_producenta),
	CONSTRAINT uq_leki_nazwa_producent UNIQUE (nazwa, id_producenta_producenci)
);

CREATE TABLE IF NOT EXISTS apteka.warianty_lekow (
	id_wariantu serial PRIMARY KEY,
	kod_ean bigint NOT NULL UNIQUE CHECK (kod_ean > 0),
	postac smallint NOT NULL CHECK (postac BETWEEN 0 AND 5),
	dawkowanie varchar(80) NOT NULL,
	ilosc integer NOT NULL CHECK (ilosc > 0),
	id_leku_leki integer NOT NULL REFERENCES apteka.leki(id_leku) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS magazyn.partie_lekow (
	id_partii serial PRIMARY KEY,
	numer_partii varchar(80) NOT NULL,
	data_waznosci date NOT NULL,
	ilosc_dostepna integer NOT NULL DEFAULT 0 CHECK (ilosc_dostepna >= 0),
	ilosc_zarezerwowana integer NOT NULL DEFAULT 0 CHECK (ilosc_zarezerwowana >= 0),
	id_wariantu_warianty_lekow integer NOT NULL REFERENCES apteka.warianty_lekow(id_wariantu),
	CONSTRAINT uq_partie_lekow UNIQUE (numer_partii, id_wariantu_warianty_lekow),
	CONSTRAINT ck_partie_lekow_rezerwacje CHECK (ilosc_zarezerwowana <= ilosc_dostepna)
);

CREATE TABLE IF NOT EXISTS magazyn.dostawcy (
	id_dostawcy serial PRIMARY KEY,
	nazwa varchar(180) NOT NULL UNIQUE,
	"NIP" varchar(20) NOT NULL UNIQUE,
	id_adresu_adresy integer NOT NULL REFERENCES apteka.adresy(id_adresu)
);

CREATE TABLE IF NOT EXISTS magazyn.zamowienia (
	id_zamowienia serial PRIMARY KEY,
	data_utworzenia timestamp NOT NULL DEFAULT now(),
	status varchar(30) NOT NULL DEFAULT 'Nowe' CHECK (status IN ('Nowe', 'Zatwierdzone', 'Zrealizowane', 'Anulowane', 'Archiwalne')),
	typ varchar(30) NOT NULL DEFAULT 'Lek' CHECK (typ IN ('Lek', 'Surowiec', 'Mieszane')),
	id_dostawcy_dostawcy integer NOT NULL REFERENCES magazyn.dostawcy(id_dostawcy)
);

CREATE TABLE IF NOT EXISTS magazyn.pozycje_zamowien (
	id_pozycji_zamowienia serial PRIMARY KEY,
	id_zamowienia_zamowienia integer NOT NULL REFERENCES magazyn.zamowienia(id_zamowienia) ON DELETE CASCADE,
	id_wariantu_warianty_lekow integer REFERENCES apteka.warianty_lekow(id_wariantu),
	id_surowca_surowce integer,
	ilosc numeric(12, 3) NOT NULL CHECK (ilosc > 0),
	cena_szacowana numeric(12, 2) NOT NULL DEFAULT 0 CHECK (cena_szacowana >= 0),
	CONSTRAINT ck_pozycje_zamowien_produkt CHECK (
		(id_wariantu_warianty_lekow IS NOT NULL AND id_surowca_surowce IS NULL)
		OR (id_wariantu_warianty_lekow IS NULL AND id_surowca_surowce IS NOT NULL)
	)
);

CREATE TABLE IF NOT EXISTS magazyn.dostawy (
	id_dostawy serial PRIMARY KEY,
	data_dostawy timestamp NOT NULL DEFAULT now(),
	numer_dokumentu varchar(80),
	id_dostawcy_dostawcy integer NOT NULL REFERENCES magazyn.dostawcy(id_dostawcy)
);

CREATE TABLE IF NOT EXISTS magazyn.pozycje_dostaw (
	id_pozycji_dostawy serial PRIMARY KEY,
	id_dostawy_dostawy integer NOT NULL REFERENCES magazyn.dostawy(id_dostawy) ON DELETE CASCADE,
	id_partii_partie_lekow integer REFERENCES magazyn.partie_lekow(id_partii),
	id_partii_surowca_partie_surowcow integer,
	ilosc numeric(12, 3) NOT NULL CHECK (ilosc > 0),
	cena_zakupu numeric(12, 2) NOT NULL DEFAULT 0 CHECK (cena_zakupu >= 0),
	CONSTRAINT ck_pozycje_dostaw_partia CHECK (
		(id_partii_partie_lekow IS NOT NULL AND id_partii_surowca_partie_surowcow IS NULL)
		OR (id_partii_partie_lekow IS NULL AND id_partii_surowca_partie_surowcow IS NOT NULL)
	)
);

CREATE TABLE IF NOT EXISTS apteka.sprzedaze (
	id_sprzedazy serial PRIMARY KEY,
	typ_dokumentu varchar(20) NOT NULL CHECK (typ_dokumentu IN ('Faktura', 'Paragon')),
	data_sprzedazy timestamp NOT NULL DEFAULT now(),
	kwota numeric(12, 2) NOT NULL DEFAULT 0 CHECK (kwota >= 0)
);

CREATE TABLE IF NOT EXISTS apteka.pozycja_sprzedazy (
	id_pozycji serial PRIMARY KEY,
	ilosc integer NOT NULL CHECK (ilosc > 0),
	cena_jednostkowa numeric(12, 2) NOT NULL CHECK (cena_jednostkowa >= 0),
	typ_produktu varchar(20) NOT NULL CHECK (typ_produktu IN ('Lek', 'Receptura')),
	id_sprzedazy_sprzedaze integer NOT NULL REFERENCES apteka.sprzedaze(id_sprzedazy) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS apteka.lek_w_pozycji_sprzedazy (
	id_partii_partie_lekow integer NOT NULL REFERENCES magazyn.partie_lekow(id_partii),
	id_pozycji_pozycja_sprzedazy integer NOT NULL REFERENCES apteka.pozycja_sprzedazy(id_pozycji) ON DELETE CASCADE,
	PRIMARY KEY (id_partii_partie_lekow, id_pozycji_pozycja_sprzedazy)
);

CREATE TABLE IF NOT EXISTS apteka.recepta (
	id_recepty serial PRIMARY KEY,
	data_wystawienia date NOT NULL,
	data_realizacji timestamp,
	data_waznosci date NOT NULL,
	kod integer NOT NULL UNIQUE CHECK (kod BETWEEN 0 AND 65535),
	id_sprzedazy_sprzedaze integer REFERENCES apteka.sprzedaze(id_sprzedazy),
	id_klienta_klienci integer REFERENCES apteka.klienci(id_klienta),
	id_lekarza_lekarze integer NOT NULL REFERENCES apteka.lekarze(id_lekarza),
	id_recepty_recepta integer REFERENCES apteka.recepta(id_recepty)
);

CREATE TABLE IF NOT EXISTS apteka.leki_w_recepcie (
	id_recepty_recepta integer NOT NULL REFERENCES apteka.recepta(id_recepty) ON DELETE CASCADE,
	id_wariantu_warianty_lekow integer NOT NULL REFERENCES apteka.warianty_lekow(id_wariantu),
	PRIMARY KEY (id_recepty_recepta, id_wariantu_warianty_lekow)
);

CREATE TABLE IF NOT EXISTS magazyn.surowce (
	id_surowca serial PRIMARY KEY,
	nazwa_surowca varchar(160) NOT NULL UNIQUE,
	typ varchar(30) NOT NULL CHECK (typ IN ('czynny', 'pomocniczy')),
	jednostka varchar(20) NOT NULL DEFAULT 'g'
);

CREATE TABLE IF NOT EXISTS magazyn.partie_surowcow (
	id_partii_surowca serial PRIMARY KEY,
	numer_partii varchar(80) NOT NULL,
	data_waznosci date NOT NULL,
	ilosc_dostepna numeric(12, 3) NOT NULL DEFAULT 0 CHECK (ilosc_dostepna >= 0),
	ilosc_zarezerwowana numeric(12, 3) NOT NULL DEFAULT 0 CHECK (ilosc_zarezerwowana >= 0),
	id_surowca_surowce integer NOT NULL REFERENCES magazyn.surowce(id_surowca),
	CONSTRAINT uq_partie_surowcow UNIQUE (numer_partii, id_surowca_surowce),
	CONSTRAINT ck_partie_surowcow_rezerwacje CHECK (ilosc_zarezerwowana <= ilosc_dostepna)
);

DO $$
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM pg_constraint WHERE conname = 'fk_pozycje_zamowien_surowce'
	) THEN
		ALTER TABLE magazyn.pozycje_zamowien
			ADD CONSTRAINT fk_pozycje_zamowien_surowce
			FOREIGN KEY (id_surowca_surowce) REFERENCES magazyn.surowce(id_surowca);
	END IF;

	IF NOT EXISTS (
		SELECT 1 FROM pg_constraint WHERE conname = 'fk_pozycje_dostaw_partie_surowcow'
	) THEN
		ALTER TABLE magazyn.pozycje_dostaw
			ADD CONSTRAINT fk_pozycje_dostaw_partie_surowcow
			FOREIGN KEY (id_partii_surowca_partie_surowcow) REFERENCES magazyn.partie_surowcow(id_partii_surowca);
	END IF;
END
$$;

CREATE TABLE IF NOT EXISTS apteka.receptury (
	id_receptury serial PRIMARY KEY,
	nazwa varchar(160) NOT NULL UNIQUE,
	opis text,
	zatwierdzona boolean NOT NULL DEFAULT false,
	koszt_przygotowania numeric(12, 2) NOT NULL DEFAULT 0 CHECK (koszt_przygotowania >= 0)
);

CREATE TABLE IF NOT EXISTS apteka.receptury_surowce (
	id_receptury_receptury integer NOT NULL REFERENCES apteka.receptury(id_receptury) ON DELETE CASCADE,
	id_surowca_surowce integer NOT NULL REFERENCES magazyn.surowce(id_surowca),
	ilosc numeric(12, 3) NOT NULL CHECK (ilosc > 0),
	PRIMARY KEY (id_receptury_receptury, id_surowca_surowce)
);

CREATE TABLE IF NOT EXISTS uzytkownicy.role (
	id_roli serial PRIMARY KEY,
	nazwa_roli varchar(40) NOT NULL UNIQUE CHECK (nazwa_roli IN ('kierownik', 'farmaceuta'))
);

CREATE TABLE IF NOT EXISTS uzytkownicy.uzytkownicy (
	id_uzytkownika serial PRIMARY KEY,
	login varchar(80) NOT NULL UNIQUE,
	haslo_hash varchar(120) NOT NULL,
	ostatnie_logowanie date,
	id_roli_role integer NOT NULL REFERENCES uzytkownicy.role(id_roli),
	id_osoby_osoby integer NOT NULL UNIQUE REFERENCES apteka.osoby(id_osoby)
);

CREATE TABLE IF NOT EXISTS uzytkownicy.log_operacji (
	id_operacji serial PRIMARY KEY,
	data_operacji timestamp NOT NULL DEFAULT now(),
	typ_operacji varchar(20) NOT NULL,
	encja varchar(160) NOT NULL,
	klucz_rekordu varchar(80),
	opis text,
	id_uzytkownika_uzytkownicy integer REFERENCES uzytkownicy.uzytkownicy(id_uzytkownika) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_leki_nazwa ON apteka.leki(nazwa);
CREATE INDEX IF NOT EXISTS idx_surowce_nazwa ON magazyn.surowce(nazwa_surowca);
CREATE INDEX IF NOT EXISTS idx_recepta_data_realizacji ON apteka.recepta(data_realizacji);
CREATE INDEX IF NOT EXISTS idx_partie_lekow_waznosc ON magazyn.partie_lekow(data_waznosci);
CREATE INDEX IF NOT EXISTS idx_partie_surowcow_waznosc ON magazyn.partie_surowcow(data_waznosci);

CREATE OR REPLACE VIEW magazyn.v_stan_magazynu_lekow AS
SELECT
	l.nazwa || ' ' || w.dawkowanie || ' x' || w.ilosc AS pelna_nazwa,
	COALESCE(SUM(pl.ilosc_dostepna - pl.ilosc_zarezerwowana), 0)::integer AS ilosc_dostepna,
	MIN(pl.data_waznosci) FILTER (WHERE (pl.ilosc_dostepna - pl.ilosc_zarezerwowana) > 0) AS najblizsza_data_waznosci
FROM apteka.leki l
JOIN apteka.warianty_lekow w ON w.id_leku_leki = l.id_leku
LEFT JOIN magazyn.partie_lekow pl ON pl.id_wariantu_warianty_lekow = w.id_wariantu
GROUP BY l.id_leku, l.nazwa, w.id_wariantu, w.dawkowanie, w.ilosc;

CREATE OR REPLACE VIEW magazyn.v_stan_magazynu_surowcow AS
SELECT
	s.nazwa_surowca,
	COALESCE(SUM(ps.ilosc_dostepna - ps.ilosc_zarezerwowana), 0)::integer AS ilosc_dostepna,
	MIN(ps.data_waznosci) FILTER (WHERE (ps.ilosc_dostepna - ps.ilosc_zarezerwowana) > 0) AS najblizsza_data_waznosci
FROM magazyn.surowce s
LEFT JOIN magazyn.partie_surowcow ps ON ps.id_surowca_surowce = s.id_surowca
GROUP BY s.id_surowca, s.nazwa_surowca;

CREATE OR REPLACE VIEW apteka.v_sprzedaz_dzienna AS
SELECT
	s.data_sprzedazy::date AS data_sprzedazy,
	COUNT(*) AS liczba_dokumentow,
	COALESCE(SUM(s.kwota), 0)::numeric(12, 2) AS obrot
FROM apteka.sprzedaze s
GROUP BY s.data_sprzedazy::date;

CREATE OR REPLACE FUNCTION uzytkownicy.fn_zapisz_log_operacji()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = uzytkownicy, apteka, magazyn, public
AS $$
DECLARE
	v_user_id integer;
	v_record_id text;
	v_row jsonb;
BEGIN
	v_user_id := NULLIF(current_setting('app.user_id', true), '')::integer;
	v_row := CASE WHEN TG_OP = 'DELETE' THEN to_jsonb(OLD) ELSE to_jsonb(NEW) END;
	v_record_id := v_row ->> TG_ARGV[0];

	INSERT INTO uzytkownicy.log_operacji (
		typ_operacji,
		encja,
		klucz_rekordu,
		opis,
		id_uzytkownika_uzytkownicy
	)
	VALUES (
		TG_OP,
		TG_TABLE_SCHEMA || '.' || TG_TABLE_NAME,
		v_record_id,
		TG_OP || ' on ' || TG_TABLE_SCHEMA || '.' || TG_TABLE_NAME,
		v_user_id
	);

	IF TG_OP = 'DELETE' THEN
		RETURN OLD;
	END IF;

	RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_audit_klienci ON apteka.klienci;
CREATE TRIGGER trg_audit_klienci AFTER INSERT OR UPDATE OR DELETE ON apteka.klienci
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_klienta');

DROP TRIGGER IF EXISTS trg_audit_adresy ON apteka.adresy;
CREATE TRIGGER trg_audit_adresy AFTER INSERT OR UPDATE OR DELETE ON apteka.adresy
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_adresu');

DROP TRIGGER IF EXISTS trg_audit_osoby ON apteka.osoby;
CREATE TRIGGER trg_audit_osoby AFTER INSERT OR UPDATE OR DELETE ON apteka.osoby
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_osoby');

DROP TRIGGER IF EXISTS trg_audit_numery_telefonu ON apteka.numery_telefonu;
CREATE TRIGGER trg_audit_numery_telefonu AFTER INSERT OR UPDATE OR DELETE ON apteka.numery_telefonu
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_telefonu');

DROP TRIGGER IF EXISTS trg_audit_pozycja_numeru ON apteka.pozycja_numeru;
CREATE TRIGGER trg_audit_pozycja_numeru AFTER INSERT OR UPDATE OR DELETE ON apteka.pozycja_numeru
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_osoby_osoby');

DROP TRIGGER IF EXISTS trg_audit_lekarze ON apteka.lekarze;
CREATE TRIGGER trg_audit_lekarze AFTER INSERT OR UPDATE OR DELETE ON apteka.lekarze
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_lekarza');

DROP TRIGGER IF EXISTS trg_audit_producenci ON apteka.producenci;
CREATE TRIGGER trg_audit_producenci AFTER INSERT OR UPDATE OR DELETE ON apteka.producenci
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_producenta');

DROP TRIGGER IF EXISTS trg_audit_leki ON apteka.leki;
CREATE TRIGGER trg_audit_leki AFTER INSERT OR UPDATE OR DELETE ON apteka.leki
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_leku');

DROP TRIGGER IF EXISTS trg_audit_warianty_lekow ON apteka.warianty_lekow;
CREATE TRIGGER trg_audit_warianty_lekow AFTER INSERT OR UPDATE OR DELETE ON apteka.warianty_lekow
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_wariantu');

DROP TRIGGER IF EXISTS trg_audit_recepta ON apteka.recepta;
CREATE TRIGGER trg_audit_recepta AFTER INSERT OR UPDATE OR DELETE ON apteka.recepta
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_recepty');

DROP TRIGGER IF EXISTS trg_audit_leki_w_recepcie ON apteka.leki_w_recepcie;
CREATE TRIGGER trg_audit_leki_w_recepcie AFTER INSERT OR UPDATE OR DELETE ON apteka.leki_w_recepcie
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_recepty_recepta');

DROP TRIGGER IF EXISTS trg_audit_receptury ON apteka.receptury;
CREATE TRIGGER trg_audit_receptury AFTER INSERT OR UPDATE OR DELETE ON apteka.receptury
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_receptury');

DROP TRIGGER IF EXISTS trg_audit_receptury_surowce ON apteka.receptury_surowce;
CREATE TRIGGER trg_audit_receptury_surowce AFTER INSERT OR UPDATE OR DELETE ON apteka.receptury_surowce
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_receptury_receptury');

DROP TRIGGER IF EXISTS trg_audit_sprzedaze ON apteka.sprzedaze;
CREATE TRIGGER trg_audit_sprzedaze AFTER INSERT OR UPDATE OR DELETE ON apteka.sprzedaze
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_sprzedazy');

DROP TRIGGER IF EXISTS trg_audit_pozycja_sprzedazy ON apteka.pozycja_sprzedazy;
CREATE TRIGGER trg_audit_pozycja_sprzedazy AFTER INSERT OR UPDATE OR DELETE ON apteka.pozycja_sprzedazy
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_pozycji');

DROP TRIGGER IF EXISTS trg_audit_lek_w_pozycji_sprzedazy ON apteka.lek_w_pozycji_sprzedazy;
CREATE TRIGGER trg_audit_lek_w_pozycji_sprzedazy AFTER INSERT OR UPDATE OR DELETE ON apteka.lek_w_pozycji_sprzedazy
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_pozycji_pozycja_sprzedazy');

DROP TRIGGER IF EXISTS trg_audit_dostawy ON magazyn.dostawy;
CREATE TRIGGER trg_audit_dostawy AFTER INSERT OR UPDATE OR DELETE ON magazyn.dostawy
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_dostawy');

DROP TRIGGER IF EXISTS trg_audit_dostawcy ON magazyn.dostawcy;
CREATE TRIGGER trg_audit_dostawcy AFTER INSERT OR UPDATE OR DELETE ON magazyn.dostawcy
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_dostawcy');

DROP TRIGGER IF EXISTS trg_audit_pozycje_dostaw ON magazyn.pozycje_dostaw;
CREATE TRIGGER trg_audit_pozycje_dostaw AFTER INSERT OR UPDATE OR DELETE ON magazyn.pozycje_dostaw
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_pozycji_dostawy');

DROP TRIGGER IF EXISTS trg_audit_partie_lekow ON magazyn.partie_lekow;
CREATE TRIGGER trg_audit_partie_lekow AFTER INSERT OR UPDATE OR DELETE ON magazyn.partie_lekow
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_partii');

DROP TRIGGER IF EXISTS trg_audit_surowce ON magazyn.surowce;
CREATE TRIGGER trg_audit_surowce AFTER INSERT OR UPDATE OR DELETE ON magazyn.surowce
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_surowca');

DROP TRIGGER IF EXISTS trg_audit_partie_surowcow ON magazyn.partie_surowcow;
CREATE TRIGGER trg_audit_partie_surowcow AFTER INSERT OR UPDATE OR DELETE ON magazyn.partie_surowcow
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_partii_surowca');

DROP TRIGGER IF EXISTS trg_audit_zamowienia ON magazyn.zamowienia;
CREATE TRIGGER trg_audit_zamowienia AFTER INSERT OR UPDATE OR DELETE ON magazyn.zamowienia
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_zamowienia');

DROP TRIGGER IF EXISTS trg_audit_pozycje_zamowien ON magazyn.pozycje_zamowien;
CREATE TRIGGER trg_audit_pozycje_zamowien AFTER INSERT OR UPDATE OR DELETE ON magazyn.pozycje_zamowien
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_pozycji_zamowienia');

DROP TRIGGER IF EXISTS trg_audit_role ON uzytkownicy.role;
CREATE TRIGGER trg_audit_role AFTER INSERT OR UPDATE OR DELETE ON uzytkownicy.role
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_roli');

DROP TRIGGER IF EXISTS trg_audit_uzytkownicy ON uzytkownicy.uzytkownicy;
CREATE TRIGGER trg_audit_uzytkownicy AFTER INSERT OR UPDATE OR DELETE ON uzytkownicy.uzytkownicy
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_uzytkownika');

DO $$
DECLARE
	db_name text := current_database();
BEGIN
	EXECUTE format('GRANT CONNECT ON DATABASE %I TO apteka_app, apteka_farmaceuta, apteka_kierownik', db_name);
END
$$;

GRANT USAGE ON SCHEMA apteka, magazyn, uzytkownicy TO apteka_app, apteka_farmaceuta, apteka_kierownik;

GRANT SELECT ON uzytkownicy.role, uzytkownicy.uzytkownicy, apteka.osoby TO apteka_app;
GRANT UPDATE (ostatnie_logowanie) ON uzytkownicy.uzytkownicy TO apteka_app;

GRANT SELECT ON
	apteka.adresy,
	apteka.osoby,
	apteka.numery_telefonu,
	apteka.pozycja_numeru,
	apteka.klienci,
	apteka.lekarze,
	apteka.producenci,
	apteka.leki,
	apteka.warianty_lekow,
	apteka.recepta,
	apteka.leki_w_recepcie,
	apteka.receptury,
	apteka.receptury_surowce,
	apteka.sprzedaze,
	apteka.pozycja_sprzedazy,
	apteka.lek_w_pozycji_sprzedazy,
	apteka.wykonania_receptur,
	apteka.surowce_w_wykonaniu
TO apteka_farmaceuta;

GRANT SELECT ON
	magazyn.dostawcy,
	magazyn.zamowienia,
	magazyn.pozycje_zamowien,
	magazyn.dostawy,
	magazyn.pozycje_dostaw,
	magazyn.partie_lekow,
	magazyn.surowce,
	magazyn.partie_surowcow,
	magazyn.v_stan_magazynu_lekow,
	magazyn.v_stan_magazynu_surowcow
TO apteka_farmaceuta;

GRANT SELECT ON apteka.v_sprzedaz_dzienna TO apteka_farmaceuta;

GRANT INSERT ON
	apteka.adresy,
	apteka.osoby,
	apteka.numery_telefonu,
	apteka.pozycja_numeru,
	apteka.klienci,
	apteka.sprzedaze,
	apteka.pozycja_sprzedazy,
	apteka.lek_w_pozycji_sprzedazy,
	apteka.wykonania_receptur,
	apteka.surowce_w_wykonaniu,
	magazyn.zamowienia,
	magazyn.pozycje_zamowien,
	magazyn.dostawy,
	magazyn.pozycje_dostaw,
	magazyn.partie_lekow,
	magazyn.partie_surowcow
TO apteka_farmaceuta;

GRANT UPDATE ON
	apteka.recepta,
	apteka.sprzedaze,
	magazyn.partie_lekow,
	magazyn.partie_surowcow
TO apteka_farmaceuta;

REVOKE ALL PRIVILEGES ON uzytkownicy.log_operacji FROM apteka_farmaceuta;
REVOKE SELECT, INSERT, UPDATE, DELETE ON uzytkownicy.uzytkownicy FROM apteka_farmaceuta;

GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA apteka TO apteka_kierownik;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA magazyn TO apteka_kierownik;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA uzytkownicy TO apteka_kierownik;
GRANT SELECT ON uzytkownicy.log_operacji TO apteka_kierownik;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA apteka, magazyn, uzytkownicy TO apteka_app, apteka_farmaceuta, apteka_kierownik;
GRANT EXECUTE ON FUNCTION uzytkownicy.fn_zapisz_log_operacji() TO apteka_app, apteka_farmaceuta, apteka_kierownik;
