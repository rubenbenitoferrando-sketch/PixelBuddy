#!/bin/bash
# Installs the ready-made PixelBuddy from GitHub Releases (no Xcode needed).
# Instala PixelBuddy ya compilado desde GitHub Releases (sin Xcode).
#   curl -fsSL https://raw.githubusercontent.com/rubenbenitoferrando-sketch/PixelBuddy/main/scripts/get.sh | bash
set -euo pipefail

URL="https://github.com/rubenbenitoferrando-sketch/PixelBuddy/releases/latest/download/PixelBuddy.zip"
DEST="$HOME/Applications"
APP="$DEST/PixelBuddy.app"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

echo "== 1/3 Downloading / Descargando PixelBuddy"
curl -fL --progress-bar -o "$TMP/PixelBuddy.zip" "$URL"

echo "== 2/3 Installing / Instalando en $DEST"
pkill -x PixelBuddy 2>/dev/null || true
mkdir -p "$DEST"
ditto -x -k "$TMP/PixelBuddy.zip" "$TMP/app"
[ -d "$TMP/app/PixelBuddy.app" ] || { echo "Download failed / La descarga ha fallado"; exit 1; }
rm -rf "$APP"
ditto "$TMP/app/PixelBuddy.app" "$APP"
xattr -dr com.apple.quarantine "$APP" 2>/dev/null || true

echo "== 3/3 Opening / Abriendo PixelBuddy"
open "$APP"
echo
echo "✓ Done / Listo. Follow the welcome window / Sigue la ventana de bienvenida."
