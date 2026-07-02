-- Tighten the operational database role so it matches the application role model.

REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA apteka FROM apteka_farmaceuta;
REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA magazyn FROM apteka_farmaceuta;
REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA uzytkownicy FROM apteka_farmaceuta;

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

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA apteka, magazyn, uzytkownicy TO apteka_farmaceuta;
GRANT EXECUTE ON FUNCTION uzytkownicy.fn_zapisz_log_operacji() TO apteka_farmaceuta;
