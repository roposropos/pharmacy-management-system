-- Expand audit coverage to all operational tables used by the desktop workflow.

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

DROP TRIGGER IF EXISTS trg_audit_pozycja_sprzedazy ON apteka.pozycja_sprzedazy;
CREATE TRIGGER trg_audit_pozycja_sprzedazy AFTER INSERT OR UPDATE OR DELETE ON apteka.pozycja_sprzedazy
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_pozycji');

DROP TRIGGER IF EXISTS trg_audit_lek_w_pozycji_sprzedazy ON apteka.lek_w_pozycji_sprzedazy;
CREATE TRIGGER trg_audit_lek_w_pozycji_sprzedazy AFTER INSERT OR UPDATE OR DELETE ON apteka.lek_w_pozycji_sprzedazy
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_pozycji_pozycja_sprzedazy');

DROP TRIGGER IF EXISTS trg_audit_leki_w_recepcie ON apteka.leki_w_recepcie;
CREATE TRIGGER trg_audit_leki_w_recepcie AFTER INSERT OR UPDATE OR DELETE ON apteka.leki_w_recepcie
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_recepty_recepta');

DROP TRIGGER IF EXISTS trg_audit_receptury ON apteka.receptury;
CREATE TRIGGER trg_audit_receptury AFTER INSERT OR UPDATE OR DELETE ON apteka.receptury
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_receptury');

DROP TRIGGER IF EXISTS trg_audit_receptury_surowce ON apteka.receptury_surowce;
CREATE TRIGGER trg_audit_receptury_surowce AFTER INSERT OR UPDATE OR DELETE ON apteka.receptury_surowce
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_receptury_receptury');

DROP TRIGGER IF EXISTS trg_audit_surowce_w_wykonaniu ON apteka.surowce_w_wykonaniu;
CREATE TRIGGER trg_audit_surowce_w_wykonaniu AFTER INSERT OR UPDATE OR DELETE ON apteka.surowce_w_wykonaniu
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_wykonania_wykonania_receptur');

DROP TRIGGER IF EXISTS trg_audit_dostawcy ON magazyn.dostawcy;
CREATE TRIGGER trg_audit_dostawcy AFTER INSERT OR UPDATE OR DELETE ON magazyn.dostawcy
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_dostawcy');

DROP TRIGGER IF EXISTS trg_audit_pozycje_zamowien ON magazyn.pozycje_zamowien;
CREATE TRIGGER trg_audit_pozycje_zamowien AFTER INSERT OR UPDATE OR DELETE ON magazyn.pozycje_zamowien
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_pozycji_zamowienia');

DROP TRIGGER IF EXISTS trg_audit_pozycje_dostaw ON magazyn.pozycje_dostaw;
CREATE TRIGGER trg_audit_pozycje_dostaw AFTER INSERT OR UPDATE OR DELETE ON magazyn.pozycje_dostaw
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_pozycji_dostawy');

DROP TRIGGER IF EXISTS trg_audit_surowce ON magazyn.surowce;
CREATE TRIGGER trg_audit_surowce AFTER INSERT OR UPDATE OR DELETE ON magazyn.surowce
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_surowca');

DROP TRIGGER IF EXISTS trg_audit_partie_surowcow ON magazyn.partie_surowcow;
CREATE TRIGGER trg_audit_partie_surowcow AFTER INSERT OR UPDATE OR DELETE ON magazyn.partie_surowcow
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_partii_surowca');

DROP TRIGGER IF EXISTS trg_audit_role ON uzytkownicy.role;
CREATE TRIGGER trg_audit_role AFTER INSERT OR UPDATE OR DELETE ON uzytkownicy.role
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_roli');

DROP TRIGGER IF EXISTS trg_audit_uzytkownicy ON uzytkownicy.uzytkownicy;
CREATE TRIGGER trg_audit_uzytkownicy AFTER INSERT OR UPDATE OR DELETE ON uzytkownicy.uzytkownicy
FOR EACH ROW EXECUTE FUNCTION uzytkownicy.fn_zapisz_log_operacji('id_uzytkownika');
