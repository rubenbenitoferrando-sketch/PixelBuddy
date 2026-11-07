import Foundation

/// Añade o quita los hooks de PixelBuddy en `<carpeta>/.claude/settings.local.json`.
/// Nunca toca `~/.claude/settings.json` ni `.claude/settings.json`.
enum HookInstaller {
    static let events = ["UserPromptSubmit", "PreToolUse", "PostToolUse", "PermissionRequest",
                         "Stop", "StopFailure", "Notification", "SessionEnd"]
    /// Así reconocemos nuestras entradas (y las de la versión antigua, NinjaDock).
    static let markers = ["PixelBuddy/pixelbuddy-hook.sh", "NinjaDock/ninja-hook.sh"]

    struct Failure: LocalizedError {
        let message: String
        var errorDescription: String? { message }
    }

    static func settingsURL(_ folder: String) -> URL {
        URL(fileURLWithPath: folder).appendingPathComponent(".claude/settings.local.json")
    }

    static func command(_ event: String) -> String {
        // Comillas simples para el shell, escapando las que tenga la ruta.
        let quoted = Config.hookURL.path.replacingOccurrences(of: "'", with: "'\\''")
        return "'\(quoted)' \(event)"
    }

    /// true si el plugin `pixelbuddy` de Claude Code ya está activado para esa carpeta
    /// (en sus ajustes locales o de proyecto, o en los del usuario): entonces sobran los hooks.
    static func pluginEnabled(folder: String) -> Bool {
        let files = [settingsURL(folder),
                     URL(fileURLWithPath: folder).appendingPathComponent(".claude/settings.json"),
                     FileManager.default.homeDirectoryForCurrentUser
                        .appendingPathComponent(".claude/settings.json")]
        for f in files {
            guard let data = try? Data(contentsOf: f),
                  let obj = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
                  let enabled = obj["enabledPlugins"] as? [String: Any] else { continue }
            if enabled.contains(where: { $0.key.hasPrefix("pixelbuddy@") && ($0.value as? Bool) == true }) {
                return true
            }
        }
        return false
    }

    static func install(folder: String) throws {
        let home = Config.normalize(NSHomeDirectory())
        guard Config.normalize(folder) != home else {
            throw Failure(message: L.t("err.home"))
        }
        let url = settingsURL(folder)
        try FileManager.default.createDirectory(at: url.deletingLastPathComponent(),
                                                withIntermediateDirectories: true)
        var root = try load(url)
        var hooks = (root["hooks"] as? [String: Any]) ?? [:]
        for ev in events {
            var groups = strip((hooks[ev] as? [[String: Any]]) ?? [])
            var group: [String: Any] = [
                "hooks": [["type": "command", "command": command(ev), "timeout": 5]]
            ]
            if ["PreToolUse", "PostToolUse", "PermissionRequest"].contains(ev) { group["matcher"] = "*" }
            groups.append(group)
            hooks[ev] = groups
        }
        root["hooks"] = hooks
        try save(root, to: url)
        ensureGitIgnored(folder: folder)
    }

    static func uninstall(folder: String) throws {
        let url = settingsURL(folder)
        guard FileManager.default.fileExists(atPath: url.path) else { return }
        var root = try load(url)
        guard var hooks = root["hooks"] as? [String: Any] else { return }
        let before = hooks.description
        for (ev, value) in hooks {
            guard let groups = value as? [[String: Any]] else { continue }
            let cleaned = strip(groups)
            if cleaned.isEmpty { hooks.removeValue(forKey: ev) } else { hooks[ev] = cleaned }
        }
        guard hooks.description != before else { return }   // no había nada nuestro
        if hooks.isEmpty { root.removeValue(forKey: "hooks") } else { root["hooks"] = hooks }
        try save(root, to: url)
    }

    // MARK: - Ayudantes

    /// Lee el JSON existente (o vacío) y deja una copia de seguridad.
    private static func load(_ url: URL) throws -> [String: Any] {
        guard FileManager.default.fileExists(atPath: url.path) else { return [:] }
        let data = try Data(contentsOf: url)
        let text = String(data: data, encoding: .utf8)?
            .trimmingCharacters(in: .whitespacesAndNewlines) ?? ""
        if text.isEmpty { return [:] }
        guard let obj = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else {
            throw Failure(message: L.t("err.json", url.path))
        }
        let f = DateFormatter()
        f.dateFormat = "yyyyMMdd-HHmmss"
        let backup = url.deletingLastPathComponent()
            .appendingPathComponent("settings.local.json.backup-\(f.string(from: Date()))")
        if !FileManager.default.fileExists(atPath: backup.path) {
            try FileManager.default.copyItem(at: url, to: backup)
        }
        return obj
    }

    private static func save(_ obj: [String: Any], to url: URL) throws {
        let data = try JSONSerialization.data(
            withJSONObject: obj, options: [.prettyPrinted, .sortedKeys, .withoutEscapingSlashes])
        try data.write(to: url, options: .atomic)
    }

    /// Quita nuestras entradas y deja intactas las demás.
    private static func strip(_ groups: [[String: Any]]) -> [[String: Any]] {
        groups.compactMap { g in
            guard var hs = g["hooks"] as? [[String: Any]] else { return g }
            hs.removeAll { h in
                guard let c = h["command"] as? String else { return false }
                return markers.contains { c.contains($0) }
            }
            if hs.isEmpty { return nil }
            var copy = g
            copy["hooks"] = hs
            return copy
        }
    }

    /// Si la carpeta está en un repo git, añade el archivo a `.git/info/exclude`
    /// (exclusión local: no se comparte ni se sube).
    private static func ensureGitIgnored(folder: String) {
        let fm = FileManager.default
        let target = URL(fileURLWithPath: Config.normalize(folder))
        var dir = target
        while true {
            let git = dir.appendingPathComponent(".git")
            var isDir: ObjCBool = false
            if fm.fileExists(atPath: git.path, isDirectory: &isDir) {
                guard isDir.boolValue else { return }
                guard !target.path.contains("\n") else { return }
                var rel = String(target.path.dropFirst(dir.path.count))
                if rel.hasPrefix("/") { rel.removeFirst() }
                let line = "/" + (rel.isEmpty ? "" : rel + "/") + ".claude/settings.local.json"
                let exclude = git.appendingPathComponent("info/exclude")
                let current = (try? String(contentsOf: exclude, encoding: .utf8)) ?? ""
                if current.split(separator: "\n").contains(where: { $0 == line }) { return }
                try? fm.createDirectory(at: exclude.deletingLastPathComponent(),
                                        withIntermediateDirectories: true)
                let sep = current.isEmpty || current.hasSuffix("\n") ? "" : "\n"
                let added = current + sep + "# PixelBuddy (archivo personal)\n" + line + "\n"
                try? added.write(to: exclude, atomically: true, encoding: .utf8)
                return
            }
            if dir.path == "/" { return }
            dir = dir.deletingLastPathComponent()
        }
    }
}
