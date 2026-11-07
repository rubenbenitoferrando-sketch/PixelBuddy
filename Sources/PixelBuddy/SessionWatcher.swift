import Foundation

/// Vigila `~/Library/Application Support/PixelBuddy/sessions/` y avisa de cada
/// archivo nuevo o modificado. Borra los de sesiones terminadas o muy antiguas.
final class SessionWatcher {
    let dir: URL
    var onEvent: (SessionEvent) -> Void

    private var source: DispatchSourceFileSystemObject?
    private var timer: Timer?
    private var seen: [String: Date] = [:]   // archivo → fecha de modificación

    init(dir: URL, onEvent: @escaping (SessionEvent) -> Void) {
        self.dir = dir
        self.onEvent = onEvent
    }

    func start() {
        try? FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true,
                                                 attributes: [.posixPermissions: 0o700])
        try? FileManager.default.setAttributes([.posixPermissions: 0o700], ofItemAtPath: dir.path)
        startSource()
        // Respaldo por si se pierde algún aviso del sistema de archivos.
        let t = Timer(timeInterval: 3, target: self, selector: #selector(timerFired),
                      userInfo: nil, repeats: true)
        RunLoop.main.add(t, forMode: .common)
        timer = t
        scan()
    }

    private func startSource() {
        source?.cancel()
        let fd = open(dir.path, O_EVTONLY)
        guard fd >= 0 else { return }
        let src = DispatchSource.makeFileSystemObjectSource(
            fileDescriptor: fd, eventMask: [.write, .rename, .delete], queue: .main)
        src.setEventHandler { [weak self] in
            MainActor.assumeIsolated { self?.scan() }
        }
        src.setCancelHandler { close(fd) }
        src.resume()
        source = src
    }

    @objc private func timerFired() {
        if !FileManager.default.fileExists(atPath: dir.path) {
            try? FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
            startSource()
        }
        scan()
    }

    /// Olvida lo leído y vuelve a procesar todos los archivos.
    func rescanAll() {
        seen.removeAll()
        scan()
    }

    func scan() {
        let fm = FileManager.default
        guard let files = try? fm.contentsOfDirectory(
            at: dir, includingPropertiesForKeys: [.contentModificationDateKey],
            options: [.skipsHiddenFiles]) else { return }
        let now = Date()
        var present = Set<String>()
        // Orden por fecha para aplicar los eventos en el orden en que llegaron.
        let dated = files.filter { $0.pathExtension == "json" }.map { url -> (URL, Date) in
            let d = (try? url.resourceValues(forKeys: [.contentModificationDateKey]))?
                .contentModificationDate ?? .distantPast
            return (url, d)
        }.sorted { $0.1 < $1.1 }

        for (url, mtime) in dated {
            let name = url.lastPathComponent
            present.insert(name)
            if now.timeIntervalSince(mtime) >= Ninja.forgetAfter {
                try? fm.removeItem(at: url)
                // Con ts antiguo, el escenario elimina ese ninja.
                if let e = parse(url) { onEvent(e) }
                seen.removeValue(forKey: name)
                present.remove(name)
                continue
            }
            if seen[name] == mtime { continue }
            seen[name] = mtime
            guard let e = parse(url) else { continue }
            if e.event == "SessionEnd" {
                try? fm.removeItem(at: url)
                present.remove(name)
                seen.removeValue(forKey: name)
            }
            onEvent(e)
        }
        for k in seen.keys where !present.contains(k) { seen.removeValue(forKey: k) }
    }

    private func parse(_ url: URL) -> SessionEvent? {
        // Un archivo de sesión ocupa ~200 bytes: se ignora cualquiera anormalmente grande.
        let size = (try? url.resourceValues(forKeys: [.fileSizeKey]))?.fileSize ?? 0
        guard size < 64_000, let data = try? Data(contentsOf: url),
              let obj = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
              let sid = obj["session_id"] as? String, !sid.isEmpty,
              let event = obj["event"] as? String else { return nil }
        let ts = (obj["ts"] as? NSNumber)?.doubleValue ?? Date().timeIntervalSince1970
        return SessionEvent(sessionId: sid, cwd: (obj["cwd"] as? String) ?? "",
                            event: event, detail: (obj["detail"] as? String) ?? "", ts: ts)
    }
}
