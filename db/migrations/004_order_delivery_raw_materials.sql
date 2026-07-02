-- Allow orders and deliveries to contain both ready medicines and compounding raw materials.

ALTER TABLE magazyn.pozycje_zamowien
	ADD COLUMN IF NOT EXISTS id_surowca_surowce integer;

ALTER TABLE magazyn.pozycje_zamowien
	ALTER COLUMN ilosc TYPE numeric(12, 3) USING ilosc::numeric;

ALTER TABLE magazyn.pozycje_zamowien
	DROP CONSTRAINT IF EXISTS ck_pozycje_zamowien_produkt;

ALTER TABLE magazyn.pozycje_zamowien
	ADD CONSTRAINT ck_pozycje_zamowien_produkt CHECK (
		(id_wariantu_warianty_lekow IS NOT NULL AND id_surowca_surowce IS NULL)
		OR (id_wariantu_warianty_lekow IS NULL AND id_surowca_surowce IS NOT NULL)
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
END
$$;

ALTER TABLE magazyn.pozycje_dostaw
	ADD COLUMN IF NOT EXISTS id_partii_surowca_partie_surowcow integer;

ALTER TABLE magazyn.pozycje_dostaw
	ALTER COLUMN ilosc TYPE numeric(12, 3) USING ilosc::numeric;

ALTER TABLE magazyn.pozycje_dostaw
	DROP CONSTRAINT IF EXISTS ck_pozycje_dostaw_partia;

ALTER TABLE magazyn.pozycje_dostaw
	ADD CONSTRAINT ck_pozycje_dostaw_partia CHECK (
		(id_partii_partie_lekow IS NOT NULL AND id_partii_surowca_partie_surowcow IS NULL)
		OR (id_partii_partie_lekow IS NULL AND id_partii_surowca_partie_surowcow IS NOT NULL)
	);

DO $$
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM pg_constraint WHERE conname = 'fk_pozycje_dostaw_partie_surowcow'
	) THEN
		ALTER TABLE magazyn.pozycje_dostaw
			ADD CONSTRAINT fk_pozycje_dostaw_partie_surowcow
			FOREIGN KEY (id_partii_surowca_partie_surowcow) REFERENCES magazyn.partie_surowcow(id_partii_surowca);
	END IF;
END
$$;
