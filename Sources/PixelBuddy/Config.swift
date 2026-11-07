import Foundation

/// Rutas y ajustes de PixelBuddy (guardados en UserDefaults del bundle
/// `com.rubenbenito.PixelBuddy`, editables con `defaults`).
enum Config {
    static let bundleID = "com.rubenbenito.PixelBuddy"

    static let keyWatched = "watchedFolder"
    static let keyHidden = "ninjasHidden"
    static let keyExcludedCalendars = "excludedCalendars"
    static let keyLoginConfigured = "loginItemConfigured"
    static let keyLastActivity = "lastActivity"
    static let keyAvatar = "avatar"

    static var supportDir: URL {
        FileManager.default.homeDirectoryForCurrentUser
            .appendingPathComponent("Library/Application Support/PixelBuddy", isDirectory: true)
    }
    static var sessionsDir: URL { supportDir.appendingPathComponent("sessions", isDirectory: true) }
    static var hookURL: URL { supportDir.appendingPathComponent("pixelbuddy-hook.sh") }

    /// Carpeta de proyecto vigilada. Sin carpeta, se ignoran todos los eventos.
    static var watchedFolder: String? {
        get {
            let v = UserDefaults.standard.string(forKey: keyWatched)
            return (v?.isEmpty ?? true) ? nil : v
        }
        set {
            UserDefaults.standard.set(newValue, forKey: keyWatched)
            UserDefaults.standard.synchronize()
        }
    }

    /// La primera vez, hereda los ajustes de la versión antigua (NinjaDock).
    static func migrateFromNinjaDock() {
        let d = UserDefaults.standard
        guard d.string(forKey: keyWatched) == nil,
              let old = UserDefaults(suiteName: "com.rubenbenito.NinjaDock"),
              let folder = old.string(forKey: keyWatched) else { return }
        d.set(folder, forKey: keyWatched)
        for k in [keyHidden, keyExcludedCalendars, keyAvatar, keyLastActivity] {
            if let v = old.object(forKey: k) { d.set(v, forKey: k) }
        }
        d.synchronize()
    }

    /// Ruta absoluta, sin enlaces simbólicos ni barra final, en forma Unicode canónica.
    static func normalize(_ path: String) -> String {
        let expanded = (path as NSString).expandingTildeInPath
        var p = URL(fileURLWithPath: expanded).resolvingSymlinksInPath().standardizedFileURL.path
        while p.count > 1 && p.hasSuffix("/") { p.removeLast() }
        return p
    }

    private static func comparable(_ path: String) -> String {
        normalize(path).precomposedStringWithCanonicalMapping.lowercased()
    }

    /// true si `cwd` es la carpeta vigilada o está dentro de ella.
    /// Una sesión en una worktree de git cuenta como si estuviera en el repositorio original.
    static func isWatched(cwd: String) -> Bool {
        guard let root = watchedFolder, !cwd.isEmpty else { return false }
        return isInside(cwd, root: root)
            || (worktreeEquivalent(of: cwd).map { isInside($0, root: root) } ?? false)
    }

    static func isInside(_ path: String, root: String) -> Bool {
        let r = comparable(root), c = comparable(path)
        if r == "/" { return true }
        return c == r || c.hasPrefix(r + "/")
    }

    // MARK: - Git

    /// Carpeta de git más cercana a `path` (subiendo): (carpeta, es_worktree).
    private static func enclosingGit(_ path: String) -> (dir: URL, worktreeGitFile: URL?)? {
        let fm = FileManager.default
        var dir = URL(fileURLWithPath: normalize(path))
        while true {
            let dotgit = dir.appendingPathComponent(".git")
            var isDir: ObjCBool = false
            if fm.fileExists(atPath: dotgit.path, isDirectory: &isDir) {
                return (dir, isDir.boolValue ? nil : dotgit)
            }
            if dir.path == "/" { return nil }
            dir = dir.deletingLastPathComponent()
        }
    }

    /// Raíz del repositorio principal a partir del archivo `.git` de una worktree.
    private static func mainRoot(fromWorktreeGitFile file: URL) -> URL? {
        guard let text = try? String(contentsOf: file, encoding: .utf8),
              let line = text.split(separator: "\n").first(where: { $0.hasPrefix("gitdir:") }) else { return nil }
        var gitdir = line.dropFirst("gitdir:".count).trimmingCharacters(in: .whitespaces)
        if !gitdir.hasPrefix("/") {
            gitdir = file.deletingLastPathComponent().appendingPathComponent(gitdir).standardizedFileURL.path
        }
        // .git/worktrees/<nombre>/commondir apunta al .git del repositorio principal
        let gitdirURL = URL(fileURLWithPath: gitdir)
        guard let common = (try? String(contentsOf: gitdirURL.appendingPathComponent("commondir"),
                                         encoding: .utf8))?.trimmingCharacters(in: .whitespacesAndNewlines),
              !common.isEmpty else { return nil }   // p. ej. un submódulo: no es una worktree
        let commonURL = common.hasPrefix("/") ? URL(fileURLWithPath: common)
                                              : gitdirURL.appendingPathComponent(common).standardizedFileURL
        guard commonURL.lastPathComponent == ".git" else { return nil }
        return commonURL.deletingLastPathComponent()
    }

    /// Si `cwd` está dentro de una worktree de git, la ruta equivalente en el repositorio
    /// principal (misma subcarpeta). nil si no es una worktree.
    static func worktreeEquivalent(of cwd: String) -> String? {
        guard let (dir, file) = enclosingGit(cwd), let gitFile = file,
              let main = mainRoot(fromWorktreeGitFile: gitFile) else { return nil }
        let rel = String(normalize(cwd).dropFirst(dir.path.count))
        return normalize(main.path) + rel
    }

    /// Raíz del repositorio principal que contiene `folder`, o nil si no está en uno.
    /// Claude Code lee `.claude/settings.local.json` de ahí (salvo si es tu carpeta de inicio).
    static func repositoryRoot(of folder: String) -> String? {
        guard let (dir, file) = enclosingGit(folder) else { return nil }
        let root = file.flatMap(mainRoot(fromWorktreeGitFile:)) ?? (file == nil ? dir : nil)
        guard let r = root.map({ normalize($0.path) }), r != normalize(NSHomeDirectory()) else { return nil }
        return r
    }
}
