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
    static func isWatched(cwd: String) -> Bool {
        guard let root = watchedFolder, !cwd.isEmpty else { return false }
        let r = comparable(root), c = comparable(cwd)
        if r == "/" { return true }
        return c == r || c.hasPrefix(r + "/")
    }
}
