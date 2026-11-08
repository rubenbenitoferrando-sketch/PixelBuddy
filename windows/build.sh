#!/bin/bash
# Compila PixelBuddy para Windows (desde un Mac, Linux o Windows con Git Bash) y deja
# los paquetes listos en windows/dist/:
#   PixelBuddy-Windows-x64.zip    PCs normales (Intel / AMD)
#   PixelBuddy-Windows-ARM.zip    PCs con chip ARM (Snapdragon…)
#
# Cada zip lleva PixelBuddy.exe (con todo dentro: no hace falta instalar .NET),
# pixelbuddy-hook.exe y los personajes.
#
# En Windows, el hook se compila a código nativo (arranca en milisegundos). Desde Mac o
# Linux no se puede: se usa una versión normal, algo más lenta al arrancar (para pruebas).
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
DIST="$HERE/dist"
BUILD="$HERE/obj/package"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
[ -x "$HOME/.dotnet/dotnet" ] && export PATH="$HOME/.dotnet:$PATH"
command -v dotnet >/dev/null || { echo "Falta .NET 10: https://dotnet.microsoft.com/download"; exit 1; }

# Identificador de PixelBuddy en Google Calendar (opcional; nunca se sube a GitHub):
# windows/google-oauth.env con PIXELBUDDY_GOOGLE_CLIENT_ID=… y PIXELBUDDY_GOOGLE_CLIENT_SECRET=…
# (o esas variables de entorno, como hace GitHub Actions con sus "secrets").
if [ -f "$HERE/google-oauth.env" ]; then
  set -a; . "$HERE/google-oauth.env"; set +a
fi
GOOGLE_ID="${PIXELBUDDY_GOOGLE_CLIENT_ID:-}"
GOOGLE_SECRET="${PIXELBUDDY_GOOGLE_CLIENT_SECRET:-}"
if [ -n "$GOOGLE_ID" ]; then echo "✓ Con Google Calendar"; else echo "· Sin Google Calendar (falta google-oauth.env)"; fi

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) NATIVE_HOOK=true ;;
  *) NATIVE_HOOK=false ;;
esac

rm -rf "$BUILD"
mkdir -p "$DIST"
for rid in win-x64 win-arm64; do
  out="$BUILD/$rid/PixelBuddy"
  echo "▶ $rid"
  dotnet publish "$HERE/PixelBuddy/PixelBuddy.csproj" -c Release -r "$rid" --self-contained \
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none \
    "-p:GoogleClientId=$GOOGLE_ID" "-p:GoogleClientSecret=$GOOGLE_SECRET" -o "$out" -v quiet
  if $NATIVE_HOOK; then
    dotnet publish "$HERE/PixelBuddyHook/PixelBuddyHook.csproj" -c Release -r "$rid" \
      -p:DebugType=none -o "$BUILD/$rid/hook" -v quiet
  else
    dotnet publish "$HERE/PixelBuddyHook/PixelBuddyHook.csproj" -c Release -r "$rid" --self-contained \
      -p:PublishAot=false -p:PublishTrimmed=true -p:PublishSingleFile=true \
      -p:DebugType=none -o "$BUILD/$rid/hook" -v quiet
  fi
  cp "$BUILD/$rid/hook/pixelbuddy-hook.exe" "$out/"
  name="PixelBuddy-Windows-$([ "$rid" = win-x64 ] && echo x64 || echo ARM).zip"
  rm -f "$DIST/$name"
  if command -v ditto >/dev/null; then
    (cd "$BUILD/$rid" && ditto -c -k --norsrc --noextattr --noacl --keepParent PixelBuddy "$DIST/$name")
  else
    (cd "$BUILD/$rid" && zip -qr "$DIST/$name" PixelBuddy)
  fi
done
ls -lh "$DIST"
