#!/bin/bash
# Simula 2 sesiones de Claude Code DENTRO de la carpeta vigilada y 1 FUERA
# (que debe ignorarse), pasando por todos los estados. Dura ~1 min 30 s.
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BUNDLE_ID="com.rubenbenito.PixelBuddy"
SESSIONS="$HOME/Library/Application Support/PixelBuddy/sessions"
HOOK="$HOME/Library/Application Support/PixelBuddy/pixelbuddy-hook.sh"
[ -x "$HOOK" ] || HOOK="$ROOT/plugins/pixelbuddy/scripts/pixelbuddy-hook.sh"

WATCHED=$(defaults read "$BUNDLE_ID" watchedFolder 2>/dev/null) || {
  echo "No hay carpeta vigilada. Ejecuta antes scripts/install.sh"; exit 1; }
pgrep -x PixelBuddy >/dev/null || { echo "PixelBuddy no está abierta: open ~/Applications/PixelBuddy.app"; exit 1; }

A="sim-dentro-1"; CWD_A="$WATCHED"
B="sim-dentro-2"; CWD_B="$WATCHED/subcarpeta"
C="sim-fuera";    CWD_C="/private/tmp/fuera-del-proyecto"

# Envía un evento a través del hook real, igual que Claude Code.
send() {  # send <session> <cwd> <Evento> [campos JSON extra]
  printf '{"session_id":"%s","transcript_path":"/dev/null","cwd":"%s","hook_event_name":"%s"%s}' \
    "$1" "$2" "$3" "${4:+,$4}" | "$HOOK" "$3"
}
step() { printf '\n\033[1m▶ %s\033[0m\n   %s\n' "$1" "$2"; }

step "1/9 Empiezan 3 sesiones" \
     "Verás 2 ninjas corriendo (azul y rojo). La sesión de fuera NO debe aparecer."
for s in "$A|$CWD_A" "$B|$CWD_B" "$C|$CWD_C"; do send "${s%%|*}" "${s#*|}" UserPromptSubmit; done
for i in 1 2 3 4 5 6 7 8; do
  for s in "$A|$CWD_A" "$B|$CWD_B" "$C|$CWD_C"; do
    send "${s%%|*}" "${s#*|}" PreToolUse; send "${s%%|*}" "${s#*|}" PostToolUse
  done
  sleep 1
done

step "2/9 El azul termina (Stop) y el rojo te hace una pregunta" \
     "Azul: vuelve a su sitio y saca la katana. Rojo: interrogación azul."
send "$A" "$CWD_A" Stop; A_STOP=$(date +%s)
send "$B" "$CWD_B" PreToolUse '"tool_name":"AskUserQuestion","tool_input":{}'
send "$C" "$CWD_C" Stop
sleep 6

step "3/9 Le respondes y el rojo vuelve a trabajar" "Rojo: corre otra vez."
send "$B" "$CWD_B" PostToolUse '"tool_name":"AskUserQuestion"'
for i in 1 2 3 4; do send "$B" "$CWD_B" PreToolUse '"tool_name":"Bash"'; sleep 1; done

step "4/9 El rojo te pide permiso" "Rojo: interrogación azul."
send "$B" "$CWD_B" PermissionRequest '"tool_name":"Bash"'
send "$B" "$CWD_B" Notification '"message":"Claude needs your permission","notification_type":"permission_prompt"'
sleep 5

step "5/9 Lo apruebas, y después el rojo se para por un error (StopFailure)" \
     "Rojo: corre un momento y luego exclamación roja."
send "$B" "$CWD_B" PostToolUse '"tool_name":"Bash"'
sleep 2
send "$B" "$CWD_B" StopFailure
sleep 5

step "6/9 Le ayudas y el rojo termina bien" "Rojo: katana."
send "$B" "$CWD_B" UserPromptSubmit
send "$B" "$CWD_B" Stop
sleep 7

WAIT=$(( A_STOP + 31 - $(date +%s) ))
step "7/9 Esperando a que pasen los 30 s de katana del azul (${WAIT} s)" \
     "Azul: pasa a entrenar (espera tu siguiente mensaje)."
[ "$WAIT" -gt 0 ] && sleep "$WAIT"
sleep 3

step "8/9 Simulamos que el azul lleva más de 2 min entrenando" "Azul: se duerme (Zzz)."
TS=$(( $(date +%s) - 601 ))
printf '{"session_id":"%s","cwd":"%s","event":"Stop","ts":%s}\n' "$A" "$CWD_A" "$TS" \
  > "$SESSIONS/.$A.tmp" && mv -f "$SESSIONS/.$A.tmp" "$SESSIONS/$A.json"
sleep 8

step "9/9 Fin de las sesiones (SessionEnd)" \
     "Los dos ninjas desaparecen y vuelve el ninja guardián."
send "$A" "$CWD_A" SessionEnd; sleep 2
send "$B" "$CWD_B" SessionEnd
send "$C" "$CWD_C" SessionEnd
sleep 2
rm -f "$SESSIONS/$C.json"
printf '\n✓ Simulación terminada.\n'
