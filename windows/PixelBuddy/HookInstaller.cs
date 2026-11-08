using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PixelBuddy;

/// Añade o quita los hooks de PixelBuddy en `<carpeta>\.claude\settings.local.json`.
/// Nunca toca `~\.claude\settings.json` ni `.claude\settings.json`.
///
/// En Windows los hooks usan la "forma exec" de Claude Code: lanzan directamente
/// pixelbuddy-hook.exe, sin Git Bash ni PowerShell (más rápido y sin dependencias), y
/// con `onFailure: continue` para que un fallo del hook nunca bloquee a Claude.
static class HookInstaller
{
    public static readonly string[] Events =
    {
        "UserPromptSubmit", "PreToolUse", "PostToolUse", "PermissionRequest",
        "Stop", "StopFailure", "Notification", "SessionEnd",
    };
    static readonly HashSet<string> WithMatcher = new() { "PreToolUse", "PostToolUse", "PermissionRequest" };

    public const string HookExeName = "pixelbuddy-hook.exe";
    /// El hook que usan los ajustes: una copia fija en %LOCALAPPDATA%\PixelBuddy\, así sigue
    /// funcionando aunque muevas la carpeta de la app.
    public static string HookPath => Path.Combine(Paths.SupportDir, HookExeName);

    static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,   // tildes legibles
    };

    public sealed class Failure(string message) : Exception(message);

    /// Copia el hook de la carpeta de la app a %LOCALAPPDATA%\PixelBuddy\ si falta o ha cambiado.
    public static void EnsureHookInstalled()
    {
        try
        {
            var src = Path.Combine(AppContext.BaseDirectory, HookExeName);
            if (!File.Exists(src)) return;
            Directory.CreateDirectory(Paths.SupportDir);
            var dst = HookPath;
            if (File.Exists(dst) && SameFile(src, dst)) return;
            var tmp = dst + ".new";
            File.Copy(src, tmp, overwrite: true);
            File.Move(tmp, dst, overwrite: true);
        }
        catch { }   // p. ej. Claude Code lo está usando justo ahora: se reintenta al próximo arranque

        static bool SameFile(string a, string b)
        {
            var fa = new FileInfo(a);
            var fb = new FileInfo(b);
            return fa.Length == fb.Length && File.ReadAllBytes(a).AsSpan().SequenceEqual(File.ReadAllBytes(b));
        }
    }

    /// Carpeta cuyo `.claude\settings.local.json` lee Claude Code para sesiones abiertas en
    /// `folder`: la raíz del repositorio git que la contiene (también desde subcarpetas y
    /// worktrees) o, fuera de git, la propia carpeta.
    public static string SettingsFolder(string folder) =>
        WatchedPaths.RepositoryRoot(folder) ?? WatchedPaths.Normalize(folder);

    public static string SettingsFile(string folder) => LocalFile(SettingsFolder(folder));

    static string LocalFile(string dir) => Path.Combine(dir, ".claude", "settings.local.json");

    /// true si el plugin `pixelbuddy` ya está activado para esa carpeta (en sus ajustes locales
    /// o de proyecto, o en los del usuario) y puede funcionar aquí. En Windows el plugin
    /// necesita Git Bash; sin él, se usan los hooks propios.
    public static bool PluginEnabled(string folder)
    {
        if (!GitBashAvailable()) return false;
        var root = SettingsFolder(folder);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var files = new[]
        {
            SettingsFile(folder), LocalFile(folder),
            Path.Combine(folder, ".claude", "settings.json"),
            Path.Combine(root, ".claude", "settings.json"),
            Path.Combine(home, ".claude", "settings.json"),
        };
        foreach (var f in files.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (!File.Exists(f)) continue;
                if (JsonNode.Parse(File.ReadAllText(f))?["enabledPlugins"] is not JsonObject enabled) continue;
                foreach (var (k, v) in enabled)
                    if (k.StartsWith("pixelbuddy@") && v is JsonValue jv && jv.TryGetValue<bool>(out var on) && on)
                        return true;
            }
            catch { }
        }
        return false;
    }

    /// Claude Code ejecuta los hooks del plugin con Git Bash (Git para Windows).
    static bool GitBashAvailable()
    {
        var env = Environment.GetEnvironmentVariable("CLAUDE_CODE_GIT_BASH_PATH");
        if (!string.IsNullOrEmpty(env) && File.Exists(env)) return true;
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "bash.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "bin", "bash.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Git", "bin", "bash.exe"),
        };
        return candidates.Any(File.Exists);
    }

    public static void Install(string folder)
    {
        folder = WatchedPaths.Normalize(folder);
        if (WatchedPaths.IsHome(folder)) throw new Failure(L.T("err.home"));
        EnsureHookInstalled();
        if (!File.Exists(HookPath)) throw new Failure(L.T("err.noHook"));
        var file = SettingsFile(folder);
        // Si antes se instalaron en la subcarpeta, se quitan de ahí para no duplicarlos.
        if (!SamePath(folder, SettingsFolder(folder)))
            try { RemoveHooks(LocalFile(folder)); } catch { }
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var root = Load(file);
        if (root["hooks"] is not JsonObject hooks)
        {
            hooks = new JsonObject();
            root["hooks"] = hooks;
        }
        foreach (var ev in Events)
        {
            var groups = Strip(hooks[ev] as JsonArray);
            var group = new JsonObject();
            if (WithMatcher.Contains(ev)) group["matcher"] = "*";
            group["hooks"] = new JsonArray(new JsonObject
            {
                ["type"] = "command",
                ["command"] = HookPath,
                ["args"] = new JsonArray(ev),
                ["timeout"] = 5,
                ["onFailure"] = "continue",
            });
            groups.Add(group);
            hooks[ev] = groups;
        }
        Save(root, file);
        EnsureGitIgnored(SettingsFolder(folder));
    }

    /// Quita nuestros hooks de la raíz del repositorio y, por si acaso, de la propia carpeta.
    public static void Uninstall(string folder)
    {
        folder = WatchedPaths.Normalize(folder);
        RemoveHooks(SettingsFile(folder));
        if (!SamePath(folder, SettingsFolder(folder)))
            try { RemoveHooks(LocalFile(folder)); } catch { }
    }

    static void RemoveHooks(string file)
    {
        if (!File.Exists(file)) return;
        var root = Load(file, backup: false);
        if (root["hooks"] is not JsonObject hooks) return;
        var changed = false;
        foreach (var ev in hooks.Select(kv => kv.Key).ToList())
        {
            if (hooks[ev] is not JsonArray groups) continue;
            var before = groups.Count + CountHooks(groups);
            var cleaned = Strip(groups);
            if (cleaned.Count + CountHooks(cleaned) == before) continue;
            changed = true;
            if (cleaned.Count == 0) hooks.Remove(ev); else hooks[ev] = cleaned;
        }
        if (!changed) return;   // no había nada nuestro
        Backup(file);
        if (hooks.Count == 0) root.Remove("hooks");
        Save(root, file);

        static int CountHooks(JsonArray groups) =>
            groups.Sum(g => (g?["hooks"] as JsonArray)?.Count ?? 0);
    }

    // MARK: Ayudantes

    static bool SamePath(string a, string b) =>
        string.Equals(WatchedPaths.Normalize(a), WatchedPaths.Normalize(b), StringComparison.OrdinalIgnoreCase);

    /// Lee el JSON existente (o vacío) y deja una copia de seguridad.
    static JsonObject Load(string file, bool backup = true)
    {
        if (!File.Exists(file)) return new JsonObject();
        var text = File.ReadAllText(file).Trim();
        if (text.Length == 0) return new JsonObject();
        JsonObject? obj;
        try { obj = JsonNode.Parse(text, documentOptions: new JsonDocumentOptions
            { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject; }
        catch { obj = null; }
        if (obj == null) throw new Failure(L.T("err.json", file));
        if (backup) Backup(file);
        return obj;
    }

    static void Backup(string file)
    {
        var b = $"{file}.backup-{DateTime.Now:yyyyMMdd-HHmmss}";
        if (!File.Exists(b)) File.Copy(file, b);
    }

    static void Save(JsonObject obj, string file)
    {
        var tmp = file + ".tmp";
        File.WriteAllText(tmp, obj.ToJsonString(WriteOptions) + "\n");
        File.Move(tmp, file, overwrite: true);
    }

    /// Nuestras entradas: las que lanzan pixelbuddy-hook (de la app de Windows o del plugin).
    static bool IsOurs(JsonNode? h) =>
        h?["command"] is JsonValue v && v.TryGetValue<string>(out var c)
        && c.Contains("PixelBuddy", StringComparison.OrdinalIgnoreCase)
        && c.Contains("pixelbuddy-hook", StringComparison.OrdinalIgnoreCase);

    /// Quita nuestras entradas y deja intactas las demás.
    static JsonArray Strip(JsonArray? groups)
    {
        var result = new JsonArray();
        if (groups == null) return result;
        foreach (var g in groups)
        {
            var copy = g?.DeepClone();
            if (copy?["hooks"] is JsonArray hs)
            {
                foreach (var h in hs.Where(IsOurs).ToList()) hs.Remove(h);
                if (hs.Count == 0) continue;
            }
            result.Add(copy);
        }
        return result;
    }

    /// Si la carpeta está en un repo git, añade el archivo a `.git\info\exclude`
    /// (exclusión local: no se comparte ni se sube).
    static void EnsureGitIgnored(string folder)
    {
        try
        {
            var target = WatchedPaths.Normalize(folder);
            var dir = new DirectoryInfo(target);
            while (dir != null)
            {
                var git = Path.Combine(dir.FullName, ".git");
                if (File.Exists(git)) return;            // worktree: ya se usa la raíz principal
                if (Directory.Exists(git))
                {
                    var rel = target[WatchedPaths.Normalize(dir.FullName).Length..]
                        .TrimStart('\\').Replace('\\', '/');
                    var b = "/" + (rel.Length == 0 ? "" : rel + "/") + ".claude/settings.local.json";
                    // el archivo personal y sus copias de seguridad
                    var lines = new[] { b, b + ".backup-*" };
                    var exclude = Path.Combine(git, "info", "exclude");
                    var current = File.Exists(exclude) ? File.ReadAllText(exclude) : "";
                    var existing = current.Split('\n').Select(l => l.TrimEnd('\r')).ToHashSet();
                    var missing = lines.Where(l => !existing.Contains(l)).ToList();
                    if (missing.Count == 0) return;
                    Directory.CreateDirectory(Path.GetDirectoryName(exclude)!);
                    var sep = current.Length == 0 || current.EndsWith('\n') ? "" : "\n";
                    File.WriteAllText(exclude, current + sep + "# PixelBuddy (archivo personal)\n"
                                               + string.Join("\n", missing) + "\n");
                    return;
                }
                dir = dir.Parent;
            }
        }
        catch { }
    }
}
