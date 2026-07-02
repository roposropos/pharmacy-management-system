#!/usr/bin/env bash
set -euo pipefail

APP_NAME="${APP_NAME:-Apteka}"
APP_IDENTIFIER="${APP_IDENTIFIER:-pl.apteka.desktop}"
PUBLISH_DIR="${PUBLISH_DIR:-dist/macos-arm64}"
PACKAGE_ROOT="${PACKAGE_ROOT:-dist/packages}"
STAMP="${STAMP:-$(date +%Y%m%d_%H%M%S)}"
APP_DIR="${PACKAGE_ROOT}/${APP_NAME}-${STAMP}.app"
DMG_PATH="${PACKAGE_ROOT}/${APP_NAME}-macos-arm64-${STAMP}.dmg"
ZIP_PATH="${PACKAGE_ROOT}/${APP_NAME}-macos-arm64-${STAMP}.zip"
DMG_STAGE="${PACKAGE_ROOT}/${APP_NAME}-macos-arm64-${STAMP}"

"$(dirname "$0")/publish-macos-arm64.sh"

mkdir -p "${APP_DIR}/Contents/MacOS" "${APP_DIR}/Contents/Resources"
cp -R "${PUBLISH_DIR}/." "${APP_DIR}/Contents/MacOS/"
chmod +x "${APP_DIR}/Contents/MacOS/${APP_NAME}"

bundle_macos_odbc_runtime() {
  local bin_dir="$1"
  local queue=()
  local processed=()

  copy_library() {
    local source_path="$1"
    if [ ! -f "${source_path}" ]; then
      return
    fi

    local target_path="${bin_dir}/$(basename "${source_path}")"
    if [ ! -f "${target_path}" ]; then
      cp -fL "${source_path}" "${target_path}"
      chmod u+w "${target_path}" 2>/dev/null || true
    fi
    queue+=("${target_path}")
  }

  copy_library "/opt/homebrew/lib/libodbc.2.dylib"
  copy_library "/opt/homebrew/lib/libodbcinst.2.dylib"
  copy_library "/opt/homebrew/lib/psqlodbcw.so"
  copy_library "/usr/local/lib/libodbc.2.dylib"
  copy_library "/usr/local/lib/libodbcinst.2.dylib"
  copy_library "/usr/local/lib/psqlodbcw.so"

  if [ "${#queue[@]}" -eq 0 ]; then
    echo "Uwaga: nie znaleziono lokalnych bibliotek unixODBC/psqlODBC do dolaczenia do paczki macOS."
    return
  fi

  if ! command -v otool >/dev/null 2>&1 || ! command -v install_name_tool >/dev/null 2>&1; then
    echo "Uwaga: brak otool/install_name_tool; biblioteki ODBC skopiowano bez przepinania zaleznosci."
    return
  fi

  local index=0
  while [ "${index}" -lt "${#queue[@]}" ]; do
    local binary="${queue[$index]}"
    index=$((index + 1))

    if [ "${#processed[@]}" -gt 0 ]; then
      case " ${processed[*]} " in
        *" ${binary} "*) continue ;;
      esac
    fi
    processed+=("${binary}")

    while IFS= read -r dependency; do
      if [ ! -f "${dependency}" ]; then
        continue
      fi

      local dependency_name
      dependency_name="$(basename "${dependency}")"
      local bundled_dependency="${bin_dir}/${dependency_name}"
      if [ ! -f "${bundled_dependency}" ]; then
        cp -fL "${dependency}" "${bundled_dependency}"
        chmod u+w "${bundled_dependency}" 2>/dev/null || true
        queue+=("${bundled_dependency}")
      fi

      install_name_tool -change "${dependency}" "@loader_path/${dependency_name}" "${binary}" 2>/dev/null || true
    done < <(otool -L "${binary}" | awk '/^\t\/opt\/homebrew\// || /^\t\/usr\/local\// { print $1 }')

    if [[ "${binary}" == *.dylib ]]; then
      install_name_tool -id "@loader_path/$(basename "${binary}")" "${binary}" 2>/dev/null || true
    fi
  done
}

bundle_macos_odbc_runtime "${APP_DIR}/Contents/MacOS"

sign_macos_app() {
  local app_dir="$1"

  if ! command -v codesign >/dev/null 2>&1; then
    echo "Uwaga: brak codesign; paczka macOS pozostaje niepodpisana."
    return
  fi

  while IFS= read -r -d '' binary; do
    codesign --force --sign - --timestamp=none "${binary}" >/dev/null
  done < <(find "${app_dir}/Contents/MacOS" -maxdepth 1 -type f \( \
    -name "psqlodbcw.so" \
    -o -name "libodbc*.dylib" \
    -o -name "libpq*.dylib" \
    -o -name "libssl*.dylib" \
    -o -name "libcrypto*.dylib" \
    -o -name "libgssapi*.dylib" \
    -o -name "libkrb5*.dylib" \
    -o -name "libk5crypto*.dylib" \
    -o -name "libcom_err*.dylib" \
    -o -name "libkrb5support*.dylib" \
    -o -name "libltdl*.dylib" \
    -o -name "libintl*.dylib" \
  \) -print0)

  # The bundle is intentionally not signed as a notarized Apple app. Signing the
  # native files is enough to keep patched dylibs loadable after install_name_tool.
}

cat > "${APP_DIR}/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
  "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>
  <string>${APP_NAME}</string>
  <key>CFBundleDisplayName</key>
  <string>${APP_NAME}</string>
  <key>CFBundleIdentifier</key>
  <string>${APP_IDENTIFIER}</string>
  <key>CFBundleVersion</key>
  <string>1.0.0</string>
  <key>CFBundleShortVersionString</key>
  <string>1.0.0</string>
  <key>CFBundleExecutable</key>
  <string>${APP_NAME}</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>LSMinimumSystemVersion</key>
  <string>12.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

cat > "${APP_DIR}/Contents/MacOS/README-URUCHOMIENIE.txt" <<'README'
Apteka - uruchomienie

Wymagania:
- PostgreSQL
- sterownik ODBC PostgreSQL. Paczka macOS dolacza lokalne biblioteki ODBC, jesli byly dostepne podczas budowania, ale na innym komputerze nadal moze byc potrzebne: brew install unixodbc psqlodbc

DMG nie uruchamia programu sam z siebie. Po dwukliku DMG otwiera sie dysk z aplikacja.
Uruchom Apteka.app albo plik Uruchom_Apteka.command.

Jesli macOS blokuje aplikacje jako niepodpisana:
1. Kliknij Apteka.app prawym przyciskiem.
2. Wybierz Otworz.
3. Potwierdz Otworz.

Przygotowanie bazy:
- w DMG uruchom Setup_Bazy_macOS.command
- albo w katalogu aplikacji uruchom scripts/setup-db-macos.sh

Po pierwszym uruchomieniu użyj sekcji "Konfiguracja połączenia" na ekranie logowania,
jeśli lokalna baza lub sterownik ODBC mają inne parametry niż domyślne.

Przycisk "Test połączenia" na ekranie logowania jest opcjonalny. Sprawdza tylko, czy aplikacja
widzi PostgreSQL, ODBC i schemat bazy. Logowanie wykonuje taki test automatycznie.

Dla instalacji produkcyjnej ustaw własny stały klucz danych wrażliwych w:
- appsettings.local.json, sekcja Security:SensitiveDataKey
- albo zmiennej środowiskowej APTEKA_SENSITIVE_DATA_KEY
README

sign_macos_app "${APP_DIR}"

mkdir -p "${DMG_STAGE}"
cp -R "${APP_DIR}" "${DMG_STAGE}/${APP_NAME}.app"
cp "${APP_DIR}/Contents/MacOS/README-URUCHOMIENIE.txt" "${DMG_STAGE}/README-URUCHOMIENIE.txt"

cat > "${DMG_STAGE}/Uruchom_Apteka.command" <<'COMMAND'
#!/usr/bin/env bash
set -e

DIR="$(cd "$(dirname "$0")" && pwd)"
APP="$DIR/Apteka.app"

if [ ! -d "$APP" ]; then
  echo "Nie znaleziono Apteka.app obok tego pliku."
  echo "Rozpakuj paczke lub otworz DMG ponownie."
  read -n 1 -s -r -p "Nacisnij dowolny klawisz..."
  exit 1
fi

xattr -dr com.apple.quarantine "$APP" 2>/dev/null || true
chmod +x "$APP/Contents/MacOS/Apteka" 2>/dev/null || true
cd "$APP/Contents/MacOS"
./Apteka
COMMAND

cat > "${DMG_STAGE}/Setup_Bazy_macOS.command" <<'COMMAND'
#!/usr/bin/env bash
set -e

DIR="$(cd "$(dirname "$0")" && pwd)"
APP="$DIR/Apteka.app"
SCRIPT="$APP/Contents/MacOS/scripts/setup-db-macos.sh"

if [ ! -f "$SCRIPT" ]; then
  echo "Nie znaleziono skryptu setup-db-macos.sh w Apteka.app."
  read -n 1 -s -r -p "Nacisnij dowolny klawisz..."
  exit 1
fi

chmod +x "$SCRIPT"
cd "$APP/Contents/MacOS"
"$SCRIPT"
read -n 1 -s -r -p "Gotowe. Nacisnij dowolny klawisz..."
COMMAND

chmod +x "${DMG_STAGE}/Uruchom_Apteka.command" "${DMG_STAGE}/Setup_Bazy_macOS.command"

if command -v hdiutil >/dev/null 2>&1 && hdiutil create -volname "${APP_NAME}" -srcfolder "${DMG_STAGE}" -ov -format UDZO "${DMG_PATH}" >/dev/null; then
  echo "macOS app: ${APP_DIR}"
  echo "macOS dmg: ${DMG_PATH}"
else
  (cd "${PACKAGE_ROOT}" && zip -qr "$(basename "${ZIP_PATH}")" "$(basename "${DMG_STAGE}")")
  echo "macOS app: ${APP_DIR}"
  echo "macOS zip: ${ZIP_PATH}"
fi
