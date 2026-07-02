-- Regression smoke tests for the local Apteka database.
-- Run after migrations and seed data:
-- psql -d Apteka -v ON_ERROR_STOP=1 -f db/tests/001_smoke_regression.sql

DO $$
BEGIN
	IF has_table_privilege('apteka_farmaceuta', 'uzytkownicy.uzytkownicy', 'SELECT') THEN
		RAISE EXCEPTION 'Farmaceuta must not read uzytkownicy.uzytkownicy';
	END IF;

	IF has_table_privilege('apteka_farmaceuta', 'uzytkownicy.log_operacji', 'SELECT') THEN
		RAISE EXCEPTION 'Farmaceuta must not read audit log';
	END IF;

	IF has_table_privilege('apteka_farmaceuta', 'apteka.leki', 'UPDATE') THEN
		RAISE EXCEPTION 'Farmaceuta must not update medicine catalog';
	END IF;

	IF NOT has_table_privilege('apteka_farmaceuta', 'magazyn.zamowienia', 'INSERT') THEN
		RAISE EXCEPTION 'Farmaceuta must create order proposals';
	END IF;

	IF NOT has_table_privilege('apteka_farmaceuta', 'magazyn.partie_lekow', 'UPDATE') THEN
		RAISE EXCEPTION 'Farmaceuta must update medicine batches during sales/deliveries';
	END IF;
END
$$;

DO $$
DECLARE
	v_count integer;
BEGIN
	SELECT COUNT(*) INTO v_count
	FROM information_schema.columns
	WHERE table_schema = 'apteka'
	  AND table_name = 'klienci'
	  AND column_name = 'pesel_hash';

	IF v_count <> 1 THEN
		RAISE EXCEPTION 'Client PESEL hash column is required for sensitive data protection';
	END IF;

	SELECT COUNT(*) INTO v_count FROM magazyn.v_stan_magazynu_lekow;
	IF v_count = 0 THEN
		RAISE EXCEPTION 'Medicine stock view should not be empty after seed data';
	END IF;

	SELECT COUNT(*) INTO v_count FROM magazyn.v_stan_magazynu_surowcow;
	IF v_count = 0 THEN
		RAISE EXCEPTION 'Raw material stock view should not be empty after seed data';
	END IF;
END
$$;

DO $$
DECLARE
	v_missing text;
BEGIN
	WITH expected(trigger_name) AS (
		VALUES
			('trg_audit_klienci'),
			('trg_audit_leki'),
			('trg_audit_warianty_lekow'),
			('trg_audit_receptury'),
			('trg_audit_wykonania_receptur'),
			('trg_audit_surowce_w_wykonaniu'),
			('trg_audit_pozycje_zamowien'),
			('trg_audit_pozycje_dostaw'),
			('trg_audit_partie_surowcow'),
			('trg_audit_uzytkownicy')
	)
	SELECT string_agg(e.trigger_name, ', ' ORDER BY e.trigger_name)
	INTO v_missing
	FROM expected e
	WHERE NOT EXISTS (
		SELECT 1
		FROM pg_trigger t
		WHERE t.tgname = e.trigger_name
		  AND NOT t.tgisinternal
	);

	IF v_missing IS NOT NULL THEN
		RAISE EXCEPTION 'Missing audit triggers: %', v_missing;
	END IF;
END
$$;

BEGIN;
SET LOCAL ROLE apteka_farmaceuta;
WITH a AS (
	INSERT INTO apteka.adresy (nr_domu, kod_pocztowy, miasto, kraj)
	VALUES ('1', '00-001', 'Testowo', 'Polska')
	RETURNING id_adresu
),
o AS (
	INSERT INTO apteka.osoby (imie, nazwisko)
	VALUES ('Test', 'Pacjent')
	RETURNING id_osoby
)
INSERT INTO apteka.klienci (pesel, pesel_hash, id_adresu_adresy, id_osoby_osoby)
SELECT 'enc:v1:smoke', repeat('a', 64), a.id_adresu, o.id_osoby
FROM a, o;
ROLLBACK;

BEGIN;
SET LOCAL ROLE apteka_farmaceuta;
INSERT INTO magazyn.zamowienia (status, typ, id_dostawcy_dostawcy)
VALUES ('Nowe', 'Surowiec', 1);
INSERT INTO magazyn.pozycje_zamowien
	(id_zamowienia_zamowienia, id_surowca_surowce, ilosc, cena_szacowana)
SELECT currval('magazyn.zamowienia_id_zamowienia_seq'), 1, 10.500, 25.00;
ROLLBACK;

BEGIN;
WITH d AS (
	INSERT INTO magazyn.dostawy (id_dostawcy_dostawcy)
	VALUES (1)
	RETURNING id_dostawy
),
p AS (
	INSERT INTO magazyn.partie_surowcow
		(numer_partii, data_waznosci, ilosc_dostepna, ilosc_zarezerwowana, id_surowca_surowce)
	VALUES ('SMOKE-RAW-001', CURRENT_DATE + 365, 5.250, 0, 1)
	ON CONFLICT (numer_partii, id_surowca_surowce)
	DO UPDATE SET ilosc_dostepna = magazyn.partie_surowcow.ilosc_dostepna + EXCLUDED.ilosc_dostepna
	RETURNING id_partii_surowca
)
INSERT INTO magazyn.pozycje_dostaw
	(id_dostawy_dostawy, id_partii_surowca_partie_surowcow, ilosc, cena_zakupu)
SELECT d.id_dostawy, p.id_partii_surowca, 5.250, 15.00
FROM d, p;
ROLLBACK;

SELECT 'OK' AS smoke_result;
