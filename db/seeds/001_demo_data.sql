-- Demo data for local development and manual tests.

INSERT INTO uzytkownicy.role (id_roli, nazwa_roli) VALUES
	(1, 'kierownik'),
	(2, 'farmaceuta')
ON CONFLICT (id_roli) DO UPDATE SET nazwa_roli = EXCLUDED.nazwa_roli;

INSERT INTO apteka.adresy (id_adresu, ulica, nr_domu, nr_lokalu, kod_pocztowy, miasto, kraj) VALUES
	(1, 'Rynek', '1', NULL, '50-101', 'Wroclaw', 'Polska'),
	(2, 'Grabiszynska', '12', '4', '53-501', 'Wroclaw', 'Polska'),
	(3, 'Dluga', '8', NULL, '50-260', 'Wroclaw', 'Polska'),
	(4, 'Hurtowa', '20', NULL, '55-040', 'Bielany Wroclawskie', 'Polska'),
	(5, 'Produkcyjna', '7', NULL, '60-001', 'Poznan', 'Polska')
ON CONFLICT (id_adresu) DO UPDATE SET
	ulica = EXCLUDED.ulica,
	nr_domu = EXCLUDED.nr_domu,
	nr_lokalu = EXCLUDED.nr_lokalu,
	kod_pocztowy = EXCLUDED.kod_pocztowy,
	miasto = EXCLUDED.miasto,
	kraj = EXCLUDED.kraj;

INSERT INTO apteka.osoby (id_osoby, imie, nazwisko) VALUES
	(1, 'Anna', 'Kowalska'),
	(2, 'Piotr', 'Nowak'),
	(3, 'Maria', 'Zielinska'),
	(4, 'Jan', 'Wisniewski'),
	(5, 'Tomasz', 'Lekarski')
ON CONFLICT (id_osoby) DO UPDATE SET imie = EXCLUDED.imie, nazwisko = EXCLUDED.nazwisko;

INSERT INTO uzytkownicy.uzytkownicy (id_uzytkownika, login, haslo_hash, id_roli_role, id_osoby_osoby, aktywny) VALUES
	(1, 'kierownik', 'YcenThwvKBg9XxRWnNGYGeYwgVOuQB2X56EBi1XFKKc=', 1, 1, true),
	(2, 'farmaceuta', 'f+nDC45Rwvmuv8YHpE4D8+OeNzMWg3CaOszIpWzs6gk=', 2, 2, true)
ON CONFLICT (id_uzytkownika) DO UPDATE SET
	login = EXCLUDED.login,
	haslo_hash = EXCLUDED.haslo_hash,
	id_roli_role = EXCLUDED.id_roli_role,
	id_osoby_osoby = EXCLUDED.id_osoby_osoby,
	aktywny = EXCLUDED.aktywny;

INSERT INTO apteka.klienci (id_klienta, pesel, pesel_hash, id_adresu_adresy, id_osoby_osoby) VALUES
	(1, 'enc:v1:NwdZiSaZVhsJoyZg:oLhWAVBrAO4XgUXTeS9wvA==:mtjobNKpjG6sTYw=', 'e35023289b8bd6a4050f45b9b054488b74cde1b2092da8300420dda8d435914f', 2, 3),
	(2, 'enc:v1:th8GXgZq2Ac/4a/V:Lj8RnNAxpbyuuyoHnZo+Eg==:UEy7Q6Zs3nE4sXc=', '7dfb7884a33be03349fbecf1ac0e1980422ceb340af1a9c35eaf9dd4330f1428', 3, 4)
ON CONFLICT (id_klienta) DO UPDATE SET
	pesel = EXCLUDED.pesel,
	pesel_hash = EXCLUDED.pesel_hash,
	id_adresu_adresy = EXCLUDED.id_adresu_adresy,
	id_osoby_osoby = EXCLUDED.id_osoby_osoby;

INSERT INTO apteka.numery_telefonu (id_telefonu, numer, opis) VALUES
	(1, '501-100-200', 'Komorkowy'),
	(2, '502-200-300', 'Domowy')
ON CONFLICT (id_telefonu) DO UPDATE SET numer = EXCLUDED.numer, opis = EXCLUDED.opis;

INSERT INTO apteka.pozycja_numeru (id_osoby_osoby, id_telefonu_numery_telefonu) VALUES
	(3, 1),
	(4, 2)
ON CONFLICT DO NOTHING;

INSERT INTO apteka.lekarze (id_lekarza, "numer_PWZ", id_osoby_osoby) VALUES
	(1, 1234567, 5)
ON CONFLICT (id_lekarza) DO UPDATE SET
	"numer_PWZ" = EXCLUDED."numer_PWZ",
	id_osoby_osoby = EXCLUDED.id_osoby_osoby;

INSERT INTO apteka.producenci (id_producenta, nazwa, id_adresu_adresy) VALUES
	(1, 'Polpharma', 5),
	(2, 'Aflofarm', 5)
ON CONFLICT (id_producenta) DO UPDATE SET
	nazwa = EXCLUDED.nazwa,
	id_adresu_adresy = EXCLUDED.id_adresu_adresy;

INSERT INTO apteka.leki (id_leku, nazwa, bez_recepty, substancja_czynna, id_producenta_producenci) VALUES
	(1, 'Apap', true, 'Paracetamol', 1),
	(2, 'Ibuprofen Forte', true, 'Ibuprofen', 2),
	(3, 'Amotaks', false, 'Amoksycylina', 1)
ON CONFLICT (id_leku) DO UPDATE SET
	nazwa = EXCLUDED.nazwa,
	bez_recepty = EXCLUDED.bez_recepty,
	substancja_czynna = EXCLUDED.substancja_czynna,
	id_producenta_producenci = EXCLUDED.id_producenta_producenci;

INSERT INTO apteka.warianty_lekow (id_wariantu, kod_ean, postac, dawkowanie, ilosc, id_leku_leki) VALUES
	(1, 5900000000011, 0, '500 mg', 24, 1),
	(2, 5900000000028, 0, '400 mg', 12, 2),
	(3, 5900000000035, 1, '500 mg', 16, 3)
ON CONFLICT (id_wariantu) DO UPDATE SET
	kod_ean = EXCLUDED.kod_ean,
	postac = EXCLUDED.postac,
	dawkowanie = EXCLUDED.dawkowanie,
	ilosc = EXCLUDED.ilosc,
	id_leku_leki = EXCLUDED.id_leku_leki;

INSERT INTO magazyn.partie_lekow (id_partii, numer_partii, data_waznosci, ilosc_dostepna, ilosc_zarezerwowana, id_wariantu_warianty_lekow) VALUES
	(1, 'APAP-2026-01', '2027-06-30', 40, 0, 1),
	(2, 'IBU-2026-01', '2027-03-31', 25, 0, 2),
	(3, 'AMO-2026-01', '2026-12-31', 15, 0, 3)
ON CONFLICT (id_partii) DO UPDATE SET
	numer_partii = EXCLUDED.numer_partii,
	data_waznosci = EXCLUDED.data_waznosci,
	ilosc_dostepna = EXCLUDED.ilosc_dostepna,
	ilosc_zarezerwowana = EXCLUDED.ilosc_zarezerwowana,
	id_wariantu_warianty_lekow = EXCLUDED.id_wariantu_warianty_lekow;

INSERT INTO magazyn.dostawcy (id_dostawcy, nazwa, "NIP", id_adresu_adresy) VALUES
	(1, 'Hurtownia Medyczna Zdrowie', '8990000001', 4)
ON CONFLICT (id_dostawcy) DO UPDATE SET
	nazwa = EXCLUDED.nazwa,
	"NIP" = EXCLUDED."NIP",
	id_adresu_adresy = EXCLUDED.id_adresu_adresy;

INSERT INTO magazyn.surowce (id_surowca, nazwa_surowca, typ, jednostka) VALUES
	(1, 'Lanolina', 'pomocniczy', 'g'),
	(2, 'Mentol', 'czynny', 'g')
ON CONFLICT (id_surowca) DO UPDATE SET
	nazwa_surowca = EXCLUDED.nazwa_surowca,
	typ = EXCLUDED.typ,
	jednostka = EXCLUDED.jednostka;

INSERT INTO magazyn.partie_surowcow (id_partii_surowca, numer_partii, data_waznosci, ilosc_dostepna, ilosc_zarezerwowana, id_surowca_surowce) VALUES
	(1, 'LAN-2026-01', '2028-01-31', 500.000, 0, 1),
	(2, 'MEN-2026-01', '2027-09-30', 100.000, 0, 2)
ON CONFLICT (id_partii_surowca) DO UPDATE SET
	numer_partii = EXCLUDED.numer_partii,
	data_waznosci = EXCLUDED.data_waznosci,
	ilosc_dostepna = EXCLUDED.ilosc_dostepna,
	ilosc_zarezerwowana = EXCLUDED.ilosc_zarezerwowana,
	id_surowca_surowce = EXCLUDED.id_surowca_surowce;

INSERT INTO apteka.receptury (id_receptury, nazwa, opis, zatwierdzona, koszt_przygotowania) VALUES
	(1, 'Masc mentolowa 1%', 'Przykladowa receptura demonstracyjna', true, 12.00)
ON CONFLICT (id_receptury) DO UPDATE SET
	nazwa = EXCLUDED.nazwa,
	opis = EXCLUDED.opis,
	zatwierdzona = EXCLUDED.zatwierdzona,
	koszt_przygotowania = EXCLUDED.koszt_przygotowania;

INSERT INTO apteka.receptury_surowce (id_receptury_receptury, id_surowca_surowce, ilosc) VALUES
	(1, 1, 99.000),
	(1, 2, 1.000)
ON CONFLICT DO NOTHING;

INSERT INTO apteka.recepta (id_recepty, data_wystawienia, data_waznosci, kod, id_klienta_klienci, id_lekarza_lekarze) VALUES
	(1, CURRENT_DATE - INTERVAL '2 days', CURRENT_DATE + INTERVAL '28 days', 12001, 1, 1)
ON CONFLICT (id_recepty) DO UPDATE SET
	data_wystawienia = EXCLUDED.data_wystawienia,
	data_waznosci = EXCLUDED.data_waznosci,
	kod = EXCLUDED.kod,
	id_klienta_klienci = EXCLUDED.id_klienta_klienci,
	id_lekarza_lekarze = EXCLUDED.id_lekarza_lekarze;

INSERT INTO apteka.leki_w_recepcie (id_recepty_recepta, id_wariantu_warianty_lekow) VALUES
	(1, 3)
ON CONFLICT DO NOTHING;

SELECT setval('uzytkownicy.role_id_roli_seq', COALESCE((SELECT MAX(id_roli) FROM uzytkownicy.role), 1));
SELECT setval('apteka.adresy_id_adresu_seq', COALESCE((SELECT MAX(id_adresu) FROM apteka.adresy), 1));
SELECT setval('apteka.osoby_id_osoby_seq', COALESCE((SELECT MAX(id_osoby) FROM apteka.osoby), 1));
SELECT setval('uzytkownicy.uzytkownicy_id_uzytkownika_seq', COALESCE((SELECT MAX(id_uzytkownika) FROM uzytkownicy.uzytkownicy), 1));
SELECT setval('apteka.klienci_id_klienta_seq', COALESCE((SELECT MAX(id_klienta) FROM apteka.klienci), 1));
SELECT setval('apteka.numery_telefonu_id_telefonu_seq', COALESCE((SELECT MAX(id_telefonu) FROM apteka.numery_telefonu), 1));
SELECT setval('apteka.lekarze_id_lekarza_seq', COALESCE((SELECT MAX(id_lekarza) FROM apteka.lekarze), 1));
SELECT setval('apteka.producenci_id_producenta_seq', COALESCE((SELECT MAX(id_producenta) FROM apteka.producenci), 1));
SELECT setval('apteka.leki_id_leku_seq', COALESCE((SELECT MAX(id_leku) FROM apteka.leki), 1));
SELECT setval('apteka.warianty_lekow_id_wariantu_seq', COALESCE((SELECT MAX(id_wariantu) FROM apteka.warianty_lekow), 1));
SELECT setval('magazyn.partie_lekow_id_partii_seq', COALESCE((SELECT MAX(id_partii) FROM magazyn.partie_lekow), 1));
SELECT setval('magazyn.dostawcy_id_dostawcy_seq', COALESCE((SELECT MAX(id_dostawcy) FROM magazyn.dostawcy), 1));
SELECT setval('magazyn.surowce_id_surowca_seq', COALESCE((SELECT MAX(id_surowca) FROM magazyn.surowce), 1));
SELECT setval('magazyn.partie_surowcow_id_partii_surowca_seq', COALESCE((SELECT MAX(id_partii_surowca) FROM magazyn.partie_surowcow), 1));
SELECT setval('apteka.receptury_id_receptury_seq', COALESCE((SELECT MAX(id_receptury) FROM apteka.receptury), 1));
SELECT setval('apteka.recepta_id_recepty_seq', COALESCE((SELECT MAX(id_recepty) FROM apteka.recepta), 1));
