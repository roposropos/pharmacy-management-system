# Baza danych

Ten katalog zawiera powtarzalny setup lokalnego PostgreSQL dla aplikacji Apteka.

## Szybki start

1. Utworz baze `Apteka` w PostgreSQL.
2. Uruchom migracje:

```bash
for migration in db/migrations/*.sql; do psql -d Apteka -f "$migration"; done
```

3. Wgraj dane demonstracyjne:

```bash
psql -d Apteka -f db/seeds/001_demo_data.sql
```

4. Uruchom test dymny bazy:

```bash
psql -d Apteka -v ON_ERROR_STOP=1 -f db/tests/001_smoke_regression.sql
```

5. Na Windows te same kroki wykonuje skrypt:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup-db-windows.ps1
```

6. Sprawdz lub zmien dane polaczenia w `appsettings.json`.

7. Dla instalacji produkcyjnej ustaw w `appsettings.local.json` albo zmiennej srodowiskowej
   `APTEKA_SENSITIVE_DATA_KEY` wlasny klucz ochrony danych wrazliwych. Demo ma jawny
   klucz tylko po to, zeby projekt uruchamial sie od razu lokalnie.

W `appsettings.json` mozesz zostawic:

```text
auto
```

Wtedy aplikacja sprobuje sama dobrac driver. Na macOS/Homebrew sterownik ODBC zwykle znajduje sie pod:

```text
/opt/homebrew/lib/psqlodbcw.so
```

Na Windows po instalacji psqlODBC zwykle uzyj nazwy:

```text
PostgreSQL Unicode
```

## Konta testowe

- Kierownik: login `kierownik`, haslo `kierownik123`
- Farmaceuta: login `farmaceuta`, haslo `farmaceuta123`

## Role PostgreSQL tworzone przez migracje

- `apteka_app` - konto do logowania i odczytu danych uzytkownikow
- `apteka_farmaceuta` - konto operacyjne bez uprawnien DELETE
- `apteka_kierownik` - konto kierownika z pelniejszym CRUD i dostepem do logow
