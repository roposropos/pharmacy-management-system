#!/usr/bin/env bash
set -euo pipefail

APP_NAME="${APP_NAME:-Apteka}"
PUBLISH_DIR="${PUBLISH_DIR:-dist/win-x64}"
PACKAGE_ROOT="${PACKAGE_ROOT:-dist/packages}"
STAMP="${STAMP:-$(date +%Y%m%d_%H%M%S)}"
ZIP_PATH="${PACKAGE_ROOT}/${APP_NAME}-win-x64-${STAMP}.zip"

"$(dirname "$0")/publish-windows-x64.sh"

mkdir -p "${PACKAGE_ROOT}"

cat > "${PUBLISH_DIR}/README-URUCHOMIENIE.txt" <<'README'
Apteka - uruchomienie

Wymagania:
- PostgreSQL
- psqlODBC dla Windows

Przygotowanie bazy:
1. Zainstaluj PostgreSQL i dodaj katalog bin do PATH.
2. W katalogu aplikacji uruchom:
   Setup_Bazy_Windows.bat
   albo:
   powershell -ExecutionPolicy Bypass -File .\scripts\setup-db-windows.ps1

Uruchom:
- Uruchom_Apteka.bat
- albo bezposrednio Apteka.exe

Po pierwszym uruchomieniu użyj sekcji "Konfiguracja połączenia" na ekranie logowania,
jeśli lokalna baza lub sterownik ODBC mają inne parametry niż domyślne.

Dla instalacji produkcyjnej ustaw własny stały klucz danych wrażliwych w:
- appsettings.local.json, sekcja Security:SensitiveDataKey
- albo zmiennej środowiskowej APTEKA_SENSITIVE_DATA_KEY
README

cat > "${PUBLISH_DIR}/Uruchom_Apteka.bat" <<'BAT'
@echo off
cd /d "%~dp0"
start "" "%~dp0Apteka.exe"
BAT

cat > "${PUBLISH_DIR}/Setup_Bazy_Windows.bat" <<'BAT'
@echo off
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0scripts\setup-db-windows.ps1"
pause
BAT

(cd "${PUBLISH_DIR}" && zip -qr "../packages/$(basename "${ZIP_PATH}")" . -x "runtimes/unix/*" "*.dylib")

echo "Windows zip: ${ZIP_PATH}"
