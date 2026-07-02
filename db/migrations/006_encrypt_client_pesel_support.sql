-- Allow the desktop app to store encrypted PESEL values and enforce uniqueness by hash.
-- Existing plaintext demo/client rows remain readable by the app and are encrypted on next save.

ALTER TABLE apteka.klienci
	ADD COLUMN IF NOT EXISTS pesel_hash char(64);

ALTER TABLE apteka.klienci
	ALTER COLUMN pesel TYPE varchar(512);

ALTER TABLE apteka.klienci
	DROP CONSTRAINT IF EXISTS klienci_pesel_check;

ALTER TABLE apteka.klienci
	DROP CONSTRAINT IF EXISTS klienci_pesel_key;

ALTER TABLE apteka.klienci
	DROP CONSTRAINT IF EXISTS klienci_pesel_hash_check;

ALTER TABLE apteka.klienci
	ADD CONSTRAINT klienci_pesel_hash_check
	CHECK (pesel_hash IS NULL OR pesel_hash ~ '^[0-9a-f]{64}$');

CREATE UNIQUE INDEX IF NOT EXISTS ux_klienci_pesel_hash
	ON apteka.klienci (pesel_hash)
	WHERE pesel_hash IS NOT NULL;
