#!/bin/bash
# Installs PixelBuddy on this Mac / Instala PixelBuddy en este Mac.
#   scripts/install.sh              → la app te pide la carpeta en su bienvenida
#   scripts/install.sh <carpeta>    → vigila esa carpeta y le pone los hooks
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BUNDLE_ID="com.rubenbenito.PixelBuddy"
APP="$HOME/Applications/PixelBuddy.app"
SUPPORT="$HOME/Library/Application Support/PixelBuddy"

echo "== 1/5 Checking tools / Comprobando herramientas"
if ! xcode-select -p >/dev/null 2>&1; then
  echo "Xcode Command Line Tools are missing. Run / Faltan las Command Line Tools. Ejecuta:"
  echo "  xcode-select --install"
  exit 1
fi

echo "== 2/5 Building the app / Compilando la app"
pkill -x PixelBuddy 2>/dev/null || true
OLD="$HOME/Applications/NinjaDock.app"
if [ -d "$OLD" ]; then   # versión antigua: quita sus hooks y su arranque, conserva sus ajustes
  echo "   Retirando la versión antigua (NinjaDock)…"
  pkill -x NinjaDock 2>/dev/null || true
  "$OLD/Contents/MacOS/NinjaDock" --uninstall >/dev/null 2>&1 || true
  rm -rf "$OLD" "$HOME/Library/Application Support/NinjaDock"
fi
"$ROOT/scripts/build_app.sh"

echo "== 3/5 Copying the hook / Copiando el hook"
mkdir -p "$SUPPORT/sessions" && chmod 700 "$SUPPORT" "$SUPPORT/sessions"
cp "$ROOT/plugins/pixelbuddy/scripts/pixelbuddy-hook.sh" "$SUPPORT/pixelbuddy-hook.sh"
chmod +x "$SUPPORT/pixelbuddy-hook.sh"

echo "== 4/5 Project folder / Carpeta de proyecto"
FOLDER="${1:-}"
if [ -n "$FOLDER" ]; then
  FOLDER="${FOLDER%/}"
  [ -d "$FOLDER" ] || { echo "Folder not found / No existe la carpeta: $FOLDER"; exit 1; }
  if [ "$(cd "$FOLDER" && pwd -P)" = "$(cd "$ROOT" && pwd -P)" ]; then
    echo "That's PixelBuddy's own code folder. Pick your project folder."
    echo "Esa es la carpeta del código de PixelBuddy. Elige la carpeta de tu proyecto."; exit 1
  fi
  "$APP/Contents/MacOS/PixelBuddy" --install-hooks "$FOLDER"
else
  echo "   PixelBuddy will ask for it in its welcome window."
  echo "   PixelBuddy te la pedirá en su ventana de bienvenida."
fi

echo "== 5/5 Opening PixelBuddy / Abriendo PixelBuddy"
open "$APP"
echo
echo "✓ Done / Listo."
echo "  Close and reopen your Claude Code sessions in your project folder."
echo "  Cierra y vuelve a abrir tus sesiones de Claude Code en tu carpeta de proyecto."
