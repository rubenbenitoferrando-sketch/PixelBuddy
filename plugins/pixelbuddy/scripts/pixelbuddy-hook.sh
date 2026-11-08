#!/bin/sh
# PixelBuddy · puente entre Claude Code y el ninja.
#
# Uso: pixelbuddy-hook.sh <Evento>     (Claude Code pasa el JSON del evento por stdin)
#
# Escribe de forma atómica
#   ~/Library/Application Support/PixelBuddy/sessions/<session_id>.json
# con {"session_id","cwd","event","detail","ts"}. "detail" es la herramienta
# (tool_name) o el tipo de notificación (notification_type), si los hay.
#
# Reglas: instantáneo, solo herramientas de macOS, nada por la salida estándar
# (en UserPromptSubmit se añadiría al contexto de Claude) y SIEMPRE termina con 0.
#
# En Windows (Claude Code lo ejecuta con Git Bash) pasa el evento al hook de la app
# de Windows, %LOCALAPPDATA%\PixelBuddy\pixelbuddy-hook.exe, que hace lo mismo.

if [ -n "${WINDIR:-}${windir:-}" ]; then
  EXE="${LOCALAPPDATA:-}\\PixelBuddy\\pixelbuddy-hook.exe"
  command -v cygpath >/dev/null 2>&1 && EXE=$(cygpath -u "$EXE" 2>/dev/null)
  [ -f "$EXE" ] && exec "$EXE" "${1:-}" >/dev/null 2>&1
  cat >/dev/null 2>&1
  exit 0
fi

(
  umask 077   # archivos y carpeta solo legibles por ti
  DIR="$HOME/Library/Application Support/PixelBuddy/sessions"
  EVENT=$(printf '%s' "${1:-}" | tr -cd 'A-Za-z')

  # session_id y cwd van al principio del JSON: basta con los primeros 16 KB.
  # El resto se descarta para no dejar a Claude Code escribiendo en una tubería cerrada.
  INPUT=$(head -c 16384 | tr -d '\r\n')
  cat >/dev/null

  field() {
    printf '%s' "$INPUT" \
      | grep -o "\"$1\"[[:space:]]*:[[:space:]]*\"[^\"]*\"" \
      | head -n 1 \
      | sed "s/^\"$1\"[[:space:]]*:[[:space:]]*\"//; s/\"\$//; s/\\\\\$//"
  }

  SID=$(field session_id | tr -cd 'A-Za-z0-9_-')
  [ -n "$SID" ] || exit 0
  CWD=$(field cwd)
  [ -n "$EVENT" ] || EVENT=$(field hook_event_name | tr -cd 'A-Za-z')
  DETAIL=$(field tool_name | tr -cd 'A-Za-z0-9_.:-')
  [ -n "$DETAIL" ] || DETAIL=$(field notification_type | tr -cd 'A-Za-z0-9_.:-')
  TS=$(date +%s)

  mkdir -p "$DIR" && chmod 700 "$DIR" || exit 0
  TMP="$DIR/.$SID.$$.tmp"
  printf '{"session_id":"%s","cwd":"%s","event":"%s","detail":"%s","ts":%s}\n' \
    "$SID" "$CWD" "$EVENT" "$DETAIL" "$TS" > "$TMP" \
    && mv -f "$TMP" "$DIR/$SID.json"
  rm -f "$TMP"
) >/dev/null 2>&1

exit 0
