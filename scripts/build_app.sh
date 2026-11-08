#!/bin/bash
# Compila PixelBuddy, crea PixelBuddy.app, la firma (certificado propio o ad-hoc) y la instala en ~/Applications.
#   scripts/build_app.sh            → compila para este Mac y la instala
#   scripts/build_app.sh --release  → app universal (Intel + Apple Silicon), firma ad-hoc,
#                                     sin instalar; deja build/PixelBuddy.zip para GitHub Releases
set -euo pipefail
RELEASE=0
[ "${1:-}" = "--release" ] && RELEASE=1

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
NAME="PixelBuddy"
BUNDLE_ID="com.rubenbenito.PixelBuddy"
APP="$ROOT/build/$NAME.app"
DEST="$HOME/Applications"

cd "$ROOT"
if [ "$RELEASE" = 1 ]; then
  for ARCH in arm64 x86_64; do
    echo "→ Compilando para $ARCH…"
    rm -rf "$ROOT/build/release-$ARCH"
    swift build -c release --triple "$ARCH-apple-macosx14.0" \
      --scratch-path "$ROOT/build/release-$ARCH"
  done
  BIN="$ROOT/build/$NAME-universal"
  lipo -create -output "$BIN" \
    "$(swift build -c release --triple arm64-apple-macosx14.0 --scratch-path "$ROOT/build/release-arm64" --show-bin-path)/$NAME" \
    "$(swift build -c release --triple x86_64-apple-macosx14.0 --scratch-path "$ROOT/build/release-x86_64" --show-bin-path)/$NAME"
  # Nada de rutas de este Mac dentro de lo que se publica
  if strings -a "$BIN" | grep -q "$HOME"; then
    echo "✗ El binario contiene rutas de $HOME; no se publica."; exit 1
  fi
else
  echo "→ Compilando (swift build -c release)…"
  swift build -c release
  BIN="$(swift build -c release --show-bin-path)/$NAME"
fi

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
  <key>CFBundleShortVersionString</key><string>1.1</string>
  <key>CFBundleVersion</key><string>2</string>
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
[ "$RELEASE" = 1 ] || for c in "PixelBuddy Local" "NinjaDock Local"; do
  if security find-identity -v -p codesigning 2>/dev/null | grep -q "\"$c\""; then CERT="$c"; break; fi
done
if [ -n "$CERT" ]; then
  echo "→ Firmando con el certificado \"$CERT\"…"
  codesign --force --sign "$CERT" --identifier "$BUNDLE_ID" "$APP"
else
  echo "→ Firmando (ad-hoc)…"
  codesign --force --sign - --identifier "$BUNDLE_ID" "$APP"
fi

if [ "$RELEASE" = 1 ]; then
  ZIP="$ROOT/build/$NAME.zip"
  rm -f "$ZIP"
  ditto -c -k --norsrc --noextattr --noacl --keepParent "$APP" "$ZIP"
  echo "✓ Lista para subir a GitHub Releases: $ZIP"
  exit 0
fi

echo "→ Instalando en $DEST…"
mkdir -p "$DEST"
rm -rf "$DEST/$NAME.app"
cp -R "$APP" "$DEST/$NAME.app"
echo "✓ Instalada: $DEST/$NAME.app"
