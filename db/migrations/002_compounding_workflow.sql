-- Workflow for prepared/compounded medicines.

CREATE TABLE IF NOT EXISTS apteka.wykonania_receptur (
	id_wykonania serial PRIMARY KEY,
	id_receptury_receptury integer NOT NULL REFERENCES apteka.receptury(id_receptury),
	id_recepty_recepta integer REFERENCES apteka.recepta(id_recepty),
	id_sprzedazy_sprzedaze integer NOT NULL REFERENCES apteka.sprzedaze(id_sprzedazy),
	id_pozycji_pozycja_sprzedazy integer NOT NULL REFERENCES apteka.pozycja_sprzedazy(id_pozycji),
	data_wykonania timestamp NOT NULL DEFAULT now(),
	ilosc integer NOT NULL CHECK (ilosc > 0),
	koszt_jednostkowy numeric(12, 2) NOT NULL DEFAULT 0 CHECK (koszt_jednostkowy >= 0)
);

CREATE TABLE IF NOT EXISTS apteka.surowce_w_wykonaniu (
	id_wykonania_wykonania_receptur integer NOT NULL REFERENCES apteka.wykonania_receptur(id_wykonania) ON DELETE CASCADE,
	id_partii_surowca_partie_surowcow integer NOT NULL REFERENCES magazyn.partie_surowcow(id_partii_surowca),
	ilosc numeric(12, 3) NOT NULL CHECK (ilosc > 0),
	PRIMARY KEY (id_wykonania_wykonania_receptur, id_partii_surowca_partie_surowcow)
);

CREATE INDEX IF NOT EXISTS idx_wykonania_receptur_data ON apteka.wykonania_receptur(data_wykonania);
CREATE INDEX IF NOT EXISTS idx_wykonania_receptur_receptura ON apteka.wykonania_receptur(id_receptury_receptury);

DROP TRIGGER IF EXISTS trg_audit_wykonania_receptur ON apteka.wykonania_receptur;
CREATE TRIGGER trg_audit_wykonania_receptur AFTER INSERT OR UPDATE OR DELETE ON apteka.wykonania_receptur
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_wykonania');

DROP TRIGGER IF EXISTS trg_audit_surowce_w_wykonaniu ON apteka.surowce_w_wykonaniu;
CREATE TRIGGER trg_audit_surowce_w_wykonaniu AFTER INSERT OR UPDATE OR DELETE ON apteka.surowce_w_wykonaniu
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_wykonania_wykonania_receptur');

GRANT SELECT, INSERT, UPDATE ON apteka.wykonania_receptur, apteka.surowce_w_wykonaniu TO apteka_farmaceuta;
GRANT ALL PRIVILEGES ON apteka.wykonania_receptur, apteka.surowce_w_wykonaniu TO apteka_kierownik;
GRANT USAGE, SELECT ON SEQUENCE apteka.wykonania_receptur_id_wykonania_seq TO apteka_farmaceuta, apteka_kierownik;
