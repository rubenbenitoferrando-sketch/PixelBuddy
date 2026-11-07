#!/bin/bash
# Desinstala PixelBuddy: quita los hooks, el arranque al iniciar sesión, la app y sus datos.
# Las copias de seguridad settings.local.json.backup-* de tu proyecto se conservan.
set -u

BUNDLE_ID="com.rubenbenito.PixelBuddy"
APP="$HOME/Applications/PixelBuddy.app"
SUPPORT="$HOME/Library/Application Support/PixelBuddy"

echo "→ Cerrando PixelBuddy…"
pkill -x PixelBuddy 2>/dev/null || true
sleep 1

if [ -x "$APP/Contents/MacOS/PixelBuddy" ]; then
  echo "→ Quitando hooks y arranque al iniciar sesión…"
  "$APP/Contents/MacOS/PixelBuddy" --uninstall || true
else
  FOLDER=$(defaults read "$BUNDLE_ID" watchedFolder 2>/dev/null || true)
  [ -n "$FOLDER" ] && echo "⚠ No encuentro la app: quita a mano las entradas 'PixelBuddy' de $FOLDER/.claude/settings.local.json"
fi

echo "→ Borrando la app y sus datos…"
rm -rf "$APP" "$SUPPORT"
defaults delete "$BUNDLE_ID" 2>/dev/null || true
for CERT in "PixelBuddy Local" "NinjaDock Local"; do
  if security find-certificate -c "$CERT" >/dev/null 2>&1; then
    echo "→ Quitando el certificado \"$CERT\" del llavero…"
    security delete-identity -c "$CERT" >/dev/null 2>&1 \
      || security delete-certificate -c "$CERT" >/dev/null 2>&1 || true
  fi
done
# Restos de la versión antigua (NinjaDock)
rm -rf "$HOME/Applications/NinjaDock.app" "$HOME/Library/Application Support/NinjaDock"
defaults delete com.rubenbenito.NinjaDock 2>/dev/null || true
echo "✓ PixelBuddy desinstalado."
