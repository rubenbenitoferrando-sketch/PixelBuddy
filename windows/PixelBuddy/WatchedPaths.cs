using System.Text;

namespace PixelBuddy;

/// ¿Está una sesión de Claude Code dentro de la carpeta vigilada? (regla de privacidad)
static class WatchedPaths
{
    static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// Ruta absoluta, con "\" y sin barra final, en forma Unicode canónica.
    /// Acepta también rutas de Git Bash (/c/Users/…) y con "/".
    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        var p = path.Trim();
        if (p.Length >= 2 && p[0] == '/' && char.IsAsciiLetter(p[1]) && (p.Length == 2 || p[2] == '/'))
            p = char.ToUpperInvariant(p[1]) + ":\\" + (p.Length > 3 ? p[3..] : "");
        if (p == "~" || p.StartsWith("~/") || p.StartsWith("~\\")) p = Home + p[1..];
        p = p.Replace('/', '\\');
        try { p = Path.GetFullPath(p); } catch { }
        while (p.Length > 3 && p.EndsWith('\\')) p = p[..^1];
        return p.Normalize(NormalizationForm.FormC);
    }

    static string Comparable(string path) => Normalize(path).ToLowerInvariant();

    /// true si `cwd` es la carpeta vigilada o está dentro de ella.
    /// Una sesión en una worktree de git cuenta como si estuviera en el repositorio original.
    public static bool IsWatched(string? root, string cwd)
    {
        if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(cwd)) return false;
        if (IsInside(cwd, root)) return true;
        var mapped = WorktreeEquivalent(cwd);
        return mapped != null && IsInside(mapped, root);
    }

    public static bool IsInside(string path, string root)
    {
        var r = Comparable(root);
        var c = Comparable(path);
        if (r.Length == 0 || c.Length == 0) return false;
        var prefix = r.EndsWith('\\') ? r : r + "\\";
        return c == r || c.StartsWith(prefix, StringComparison.Ordinal);
    }

    public static bool IsHome(string path) => Comparable(path) == Comparable(Home);

    // MARK: Git

    /// Carpeta de git más cercana a `path` (subiendo) y, si es una worktree, su archivo .git.
    static (string dir, string? worktreeGitFile)? EnclosingGit(string path)
    {
        try
        {
            var dir = new DirectoryInfo(Normalize(path));
            while (dir != null)
            {
                var dotgit = Path.Combine(dir.FullName, ".git");
                if (Directory.Exists(dotgit)) return (dir.FullName, null);
                if (File.Exists(dotgit)) return (dir.FullName, dotgit);
                dir = dir.Parent;
            }
        }
        catch { }
        return null;
    }

    /// Raíz del repositorio principal a partir del archivo `.git` de una worktree.
    static string? MainRoot(string worktreeGitFile)
    {
        try
        {
            var line = File.ReadLines(worktreeGitFile).FirstOrDefault(l => l.StartsWith("gitdir:"));
            if (line == null) return null;
            var gitdir = line["gitdir:".Length..].Trim();
            if (!Path.IsPathRooted(gitdir))
                gitdir = Path.Combine(Path.GetDirectoryName(worktreeGitFile)!, gitdir);
            gitdir = Path.GetFullPath(gitdir);
            // .git\worktrees\<nombre>\commondir apunta al .git del repositorio principal
            var commondirFile = Path.Combine(gitdir, "commondir");
            if (!File.Exists(commondirFile)) return null;   // p. ej. un submódulo: no es una worktree
            var common = File.ReadAllText(commondirFile).Trim();
            if (common.Length == 0) return null;
            var commonPath = Path.GetFullPath(Path.IsPathRooted(common) ? common : Path.Combine(gitdir, common))
                .TrimEnd('\\', '/');
            if (!string.Equals(Path.GetFileName(commonPath), ".git", StringComparison.OrdinalIgnoreCase))
                return null;
            return Path.GetDirectoryName(commonPath);
        }
        catch { return null; }
    }

    /// Si `cwd` está dentro de una worktree de git, la ruta equivalente en el repositorio
    /// principal (misma subcarpeta). null si no es una worktree.
    public static string? WorktreeEquivalent(string cwd)
    {
        if (EnclosingGit(cwd) is not { worktreeGitFile: { } file } found) return null;
        var main = MainRoot(file);
        if (main == null) return null;
        var rel = Normalize(cwd)[Normalize(found.dir).Length..];
        return Normalize(main) + rel;
    }

    /// Raíz del repositorio principal que contiene `folder`, o null si no está en uno.
    /// Claude Code lee `.claude\settings.local.json` de ahí (salvo si es tu carpeta de usuario).
    public static string? RepositoryRoot(string folder)
    {
        if (EnclosingGit(folder) is not { } found) return null;
        var root = found.worktreeGitFile == null ? found.dir : MainRoot(found.worktreeGitFile);
        if (root == null) return null;
        root = Normalize(root);
        return IsHome(root) ? null : root;
    }
}
