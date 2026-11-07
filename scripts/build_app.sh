#!/bin/bash
# Compila PixelBuddy, crea PixelBuddy.app, la firma (certificado propio o ad-hoc) y la instala en ~/Applications.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
NAME="PixelBuddy"
BUNDLE_ID="com.rubenbenito.PixelBuddy"
APP="$ROOT/build/$NAME.app"
DEST="$HOME/Applications"

cd "$ROOT"
echo "→ Compilando (swift build -c release)…"
swift build -c release
BIN="$(swift build -c release --show-bin-path)/$NAME"

echo "→ Creando $NAME.app…"
rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp "$BIN" "$APP/Contents/MacOS/$NAME"
cp -R "$ROOT/assets/avatars" "$APP/Contents/Resources/avatars"
cp "$ROOT/assets/AppIcon.icns" "$APP/Contents/Resources/AppIcon.icns"
# Texto del permiso del Calendario en cada idioma
CAL_EN="PixelBuddy reads your calendars to show your current and next event when you click your buddy."
CAL_ES="PixelBuddy lee tus calendarios para enseñarte el evento actual y el siguiente cuando haces clic en tu personaje."
mkdir -p "$APP/Contents/Resources/en.lproj" "$APP/Contents/Resources/es.lproj"
printf '"NSCalendarsFullAccessUsageDescription" = "%s";\n"NSCalendarsUsageDescription" = "%s";\n' \
  "$CAL_EN" "$CAL_EN" > "$APP/Contents/Resources/en.lproj/InfoPlist.strings"
printf '"NSCalendarsFullAccessUsageDescription" = "%s";\n"NSCalendarsUsageDescription" = "%s";\n' \
  "$CAL_ES" "$CAL_ES" > "$APP/Contents/Resources/es.lproj/InfoPlist.strings"
if [ -f "$ROOT/plugins/pixelbuddy/scripts/pixelbuddy-hook.sh" ]; then
  cp "$ROOT/plugins/pixelbuddy/scripts/pixelbuddy-hook.sh" "$APP/Contents/Resources/pixelbuddy-hook.sh"
fi

cat > "$APP/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleIdentifier</key><string>$BUNDLE_ID</string>
  <key>CFBundleName</key><string>$NAME</string>
  <key>CFBundleDisplayName</key><string>$NAME</string>
  <key>CFBundleExecutable</key><string>$NAME</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>1.0</string>
  <key>CFBundleVersion</key><string>1</string>
  <key>CFBundleDevelopmentRegion</key><string>en</string>
  <key>CFBundleLocalizations</key><array><string>en</string><string>es</string></array>
  <key>CFBundleIconFile</key><string>AppIcon</string>
  <key>LSMinimumSystemVersion</key><string>14.0</string>
  <key>LSUIElement</key><true/>
  <key>NSHighResolutionCapable</key><true/>
  <key>NSCalendarsFullAccessUsageDescription</key>
  <string>$CAL_EN</string>
  <key>NSCalendarsUsageDescription</key>
  <string>$CAL_EN</string>
</dict>
</plist>
EOF

# Con el certificado propio (scripts/create_signing_cert.sh) la firma no cambia entre
# versiones y macOS recuerda el permiso del Calendario. Si no existe, firma ad-hoc.
CERT=""
for c in "PixelBuddy Local" "NinjaDock Local"; do
  if security find-identity -v -p codesigning 2>/dev/null | grep -q "\"$c\""; then CERT="$c"; break; fi
done
if [ -n "$CERT" ]; then
  echo "→ Firmando con el certificado \"$CERT\"…"
  codesign --force --sign "$CERT" --identifier "$BUNDLE_ID" "$APP"
else
  echo "→ Firmando (ad-hoc)…"
  codesign --force --sign - --identifier "$BUNDLE_ID" "$APP"
fi

echo "→ Instalando en $DEST…"
mkdir -p "$DEST"
rm -rf "$DEST/$NAME.app"
cp -R "$APP" "$DEST/$NAME.app"
echo "✓ Instalada: $DEST/$NAME.app"
