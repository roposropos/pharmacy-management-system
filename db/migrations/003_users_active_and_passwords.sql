-- Account lifecycle and password hash format support.

ALTER TABLE uzytkownicy.uzytkownicy
	ADD COLUMN IF NOT EXISTS aktywny boolean NOT NULL DEFAULT true;

UPDATE uzytkownicy.uzytkownicy SET aktywny = true WHERE aktywny IS NULL;

GRANT UPDATE (haslo_hash, aktywny, ostatnie_logowanie, login, id_roli_role) ON uzytkownicy.uzytkownicy TO apteka_kierownik;
