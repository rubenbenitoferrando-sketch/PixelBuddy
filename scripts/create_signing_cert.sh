#!/bin/bash
# Crea un certificado de firma propio ("PixelBuddy Local") en tu llavero de inicio de sesión.
# Con él, la firma de PixelBuddy es la misma en cada versión y macOS no vuelve a pedir
# el permiso del Calendario al recompilar. Gratis y solo válido en este Mac.
#
# macOS te pedirá tu contraseña UNA vez para dar por bueno el certificado.
set -euo pipefail

NAME="PixelBuddy Local"
KEYCHAIN="$HOME/Library/Keychains/login.keychain-db"

if security find-identity -v -p codesigning | grep -qE "\"(PixelBuddy|NinjaDock) Local\""; then
  echo "✓ Ya tienes un certificado de PixelBuddy. No hay nada que hacer."
  exit 0
fi

TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

cat > "$TMP/cert.cnf" <<EOF
[req]
distinguished_name = dn
x509_extensions = ext
prompt = no
[dn]
CN = $NAME
[ext]
basicConstraints = critical, CA:false
keyUsage = critical, digitalSignature
extendedKeyUsage = critical, codeSigning
EOF

echo "→ Generando el certificado (válido 10 años)…"
/usr/bin/openssl req -x509 -newkey rsa:2048 -nodes -days 3650 \
  -keyout "$TMP/key.pem" -out "$TMP/cert.pem" -config "$TMP/cert.cnf" 2>/dev/null

# Contraseña temporal solo para empaquetar; el archivo se borra al terminar.
P12PASS=$(/usr/bin/openssl rand -hex 16)
/usr/bin/openssl pkcs12 -export -inkey "$TMP/key.pem" -in "$TMP/cert.pem" \
  -name "$NAME" -out "$TMP/cert.p12" -passout "pass:$P12PASS" 2>/dev/null

echo "→ Guardándolo en tu llavero…"
security import "$TMP/cert.p12" -k "$KEYCHAIN" -P "$P12PASS" -T /usr/bin/codesign >/dev/null

echo "→ Marcándolo como de confianza para firmar código."
echo "   macOS te pedirá tu contraseña del Mac en una ventana."
security add-trusted-cert -r trustRoot -p codeSign -k "$KEYCHAIN" "$TMP/cert.pem"

if security find-identity -v -p codesigning | grep -q "\"$NAME\""; then
  echo "✓ Certificado \"$NAME\" listo. Vuelve a compilar con scripts/build_app.sh."
else
  echo "✗ El certificado no aparece como válido. Revisa Acceso a Llaveros."
  exit 1
fi
