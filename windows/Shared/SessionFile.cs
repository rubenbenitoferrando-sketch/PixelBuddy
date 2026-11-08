using System.Text;
using System.Text.Json;

namespace PixelBuddy;

/// Archivo de una sesión de Claude Code: sessions\<session_id>.json con
/// {"session_id","cwd","event","detail","ts"} (el mismo formato que en Mac).
public static class SessionFile
{
    /// Solo letras, números y unos pocos signos: nada de rutas ni caracteres raros.
    public static string Clean(string? s, string extra)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(Math.Min(s.Length, 128));
        foreach (var c in s)
        {
            if (sb.Length >= 128) break;
            if (c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z')
                || (extra.Length > 0 && (char.IsAsciiDigit(c) || extra.Contains(c))))
                sb.Append(c);
        }
        return sb.ToString();
    }

    /// Escribe el archivo de forma atómica (archivo temporal + renombrar).
    public static void Write(string sessionId, string cwd, string ev, string detail, long ts)
    {
        Directory.CreateDirectory(Paths.SessionsDir);
        var final = Path.Combine(Paths.SessionsDir, sessionId + ".json");
        var tmp = Path.Combine(Paths.SessionsDir, $".{sessionId}.{Environment.ProcessId}.tmp");
        try
        {
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var w = new Utf8JsonWriter(fs))
            {
                w.WriteStartObject();
                w.WriteString("session_id", sessionId);
                w.WriteString("cwd", cwd);
                w.WriteString("event", ev);
                w.WriteString("detail", detail);
                w.WriteNumber("ts", ts);
                w.WriteEndObject();
            }
            // Si la app está leyendo justo ese archivo, se reintenta un momento.
            for (var i = 0; ; i++)
            {
                try { File.Move(tmp, final, overwrite: true); break; }
                catch (IOException) when (i < 5) { Thread.Sleep(15); }
            }
        }
        finally
        {
            try { File.Delete(tmp); } catch { }
        }
    }
}
