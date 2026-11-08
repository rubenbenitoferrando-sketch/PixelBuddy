// PixelBuddy · puente entre Claude Code y el personaje (versión Windows).
//
// Uso: pixelbuddy-hook.exe <Evento>     (Claude Code pasa el JSON del evento por stdin)
//
// Escribe %LOCALAPPDATA%\PixelBuddy\sessions\<session_id>.json con
// {"session_id","cwd","event","detail","ts"}. "detail" es la herramienta (tool_name)
// o el tipo de notificación (notification_type), si los hay.
//
// Reglas: instantáneo (se compila a código nativo), nada por la salida estándar
// (en UserPromptSubmit se añadiría al contexto de Claude) y SIEMPRE termina con 0.

using System.Text.Json;
using PixelBuddy;

try
{
    Run(args);
}
catch
{
    // Nunca molestar a Claude Code.
}
return 0;

static void Run(string[] args)
{
    var stdin = Console.OpenStandardInput();

    // session_id, cwd, hook_event_name y tool_name van antes que tool_input:
    // basta con los primeros 64 KB.
    var buf = new byte[64 * 1024];
    var len = 0;
    int n;
    while (len < buf.Length && (n = stdin.Read(buf, len, buf.Length - len)) > 0) len += n;
    var complete = len < buf.Length;

    string? sid = null, cwd = null, hookEvent = null, tool = null, notification = null;
    var reader = new Utf8JsonReader(buf.AsSpan(0, len), isFinalBlock: complete, state: default);
    try
    {
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 1) continue;
            var name = reader.GetString();
            if (!reader.Read()) break;
            if (reader.TokenType == JsonTokenType.String)
            {
                var v = reader.GetString();
                switch (name)
                {
                    case "session_id": sid ??= v; break;
                    case "cwd": cwd ??= v; break;
                    case "hook_event_name": hookEvent ??= v; break;
                    case "tool_name": tool ??= v; break;
                    case "notification_type": notification ??= v; break;
                }
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                if (!reader.TrySkip()) break;   // se acaba lo leído: ya tenemos lo que hace falta
            }
        }
    }
    catch (JsonException)
    {
        // JSON cortado o raro: usamos lo que se haya podido leer.
    }

    // Se descarta el resto para no dejar a Claude Code escribiendo en una tubería cerrada.
    if (!complete) stdin.CopyTo(Stream.Null);

    var id = SessionFile.Clean(sid, "_-");
    if (id.Length == 0) return;
    var ev = SessionFile.Clean(args.Length > 0 ? args[0] : "", "");
    if (ev.Length == 0) ev = SessionFile.Clean(hookEvent, "");
    var detail = SessionFile.Clean(tool, "_.:-");
    if (detail.Length == 0) detail = SessionFile.Clean(notification, "_.:-");

    SessionFile.Write(id, cwd ?? "", ev, detail, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
}
