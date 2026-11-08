using System.Text.Json;

namespace PixelBuddy;

/// Evento de sesión leído de `sessions\<id>.json`.
sealed record SessionEvent(string SessionId, string Cwd, string Event, string Detail, double Ts);

/// Vigila %LOCALAPPDATA%\PixelBuddy\sessions\ y avisa de cada archivo nuevo o
/// modificado. Borra los de sesiones terminadas o muy antiguas.
sealed class SessionWatcher : IDisposable
{
    readonly string dir;
    readonly Control ui;
    readonly Action<SessionEvent> onEvent;
    FileSystemWatcher? fsw;
    readonly System.Windows.Forms.Timer timer = new() { Interval = 3000 };
    readonly Dictionary<string, DateTime> seen = new();   // archivo → fecha de modificación
    bool scanQueued;

    public SessionWatcher(string dir, Control ui, Action<SessionEvent> onEvent)
    {
        this.dir = dir; this.ui = ui; this.onEvent = onEvent;
    }

    public void Start()
    {
        StartWatcher();
        // Respaldo por si se pierde algún aviso del sistema de archivos.
        timer.Tick += (_, _) =>
        {
            if (fsw == null || !Directory.Exists(dir)) StartWatcher();
            Scan();
        };
        timer.Start();
        Scan();
    }

    void StartWatcher()
    {
        fsw?.Dispose();
        fsw = null;
        try
        {
            Directory.CreateDirectory(dir);
            var w = new FileSystemWatcher(dir, "*.json")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            };
            w.Changed += (_, _) => QueueScan();
            w.Created += (_, _) => QueueScan();
            w.Renamed += (_, _) => QueueScan();
            w.Deleted += (_, _) => QueueScan();
            w.Error += (_, _) => ui.BeginInvoke(StartWatcher);
            w.EnableRaisingEvents = true;
            fsw = w;
        }
        catch { }
    }

    /// Agrupa los avisos que llegan seguidos en una sola lectura.
    void QueueScan()
    {
        if (scanQueued || !ui.IsHandleCreated) return;
        scanQueued = true;
        ui.BeginInvoke(() => { scanQueued = false; Scan(); });
    }

    /// Olvida lo leído y vuelve a procesar todos los archivos.
    public void RescanAll()
    {
        seen.Clear();
        Scan();
    }

    public void Scan()
    {
        string[] files;
        try { files = Directory.GetFiles(dir, "*.json"); } catch { return; }
        var now = DateTime.UtcNow;
        var present = new HashSet<string>();
        // Orden por fecha para aplicar los eventos en el orden en que llegaron.
        var dated = files
            .Select(f => (file: f, mtime: SafeMtime(f)))
            .OrderBy(t => t.mtime);

        foreach (var (file, mtime) in dated)
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith('.')) continue;
            present.Add(name);
            if ((now - mtime).TotalSeconds >= Buddy.ForgetAfter)
            {
                // Con ts antiguo, el escenario elimina ese personaje.
                if (Parse(file) is { } old) onEvent(old);
                TryDelete(file);
                seen.Remove(name);
                present.Remove(name);
                continue;
            }
            if (seen.TryGetValue(name, out var s) && s == mtime) continue;
            // Si no se puede leer ahora (se está escribiendo), se reintenta en la próxima vuelta.
            if (Parse(file) is not { } e) continue;
            seen[name] = mtime;
            if (e.Event == "SessionEnd")
            {
                TryDelete(file);
                present.Remove(name);
                seen.Remove(name);
            }
            onEvent(e);
        }
        foreach (var k in seen.Keys.Where(k => !present.Contains(k)).ToList()) seen.Remove(k);
    }

    static DateTime SafeMtime(string f)
    {
        try { return File.GetLastWriteTimeUtc(f); } catch { return DateTime.MinValue; }
    }

    static void TryDelete(string f)
    {
        try { File.Delete(f); } catch { }
    }

    static SessionEvent? Parse(string file)
    {
        try
        {
            // Un archivo de sesión ocupa ~200 bytes: se ignora cualquiera anormalmente grande.
            if (new FileInfo(file).Length >= 64_000) return null;
            using var doc = JsonDocument.Parse(File.ReadAllBytes(file));
            var o = doc.RootElement;
            var sid = Str(o, "session_id");
            var ev = Str(o, "event");
            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(ev)) return null;
            var ts = o.TryGetProperty("ts", out var t) && t.ValueKind == JsonValueKind.Number
                ? t.GetDouble() : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return new SessionEvent(sid, Str(o, "cwd") ?? "", ev, Str(o, "detail") ?? "", ts);
        }
        catch { return null; }

        static string? Str(JsonElement o, string k) =>
            o.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }

    public void Dispose()
    {
        timer.Dispose();
        fsw?.Dispose();
    }
}
