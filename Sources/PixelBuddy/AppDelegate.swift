import AppKit

final class AppDelegate: NSObject, NSApplicationDelegate {
    var stage: Stage!
    var watcher: SessionWatcher!
    let calendar = CalendarService()
    private var clickMonitor: Any?
    private var menuBar: MenuBar?
    /// Personajes disponibles (ninja, astronauta…).
    private(set) var avatars: [SpriteLibrary] = []
    var currentAvatarID: String { stage.sprites.id }

    func selectAvatar(_ id: String) {
        guard let lib = avatars.first(where: { $0.id == id }) else { return }
        UserDefaults.standard.set(id, forKey: Config.keyAvatar)
        stage.setSprites(lib)
        menuBar?.updateIcon(lib)
    }

    private var welcome: WelcomeWindowController?
    private var guide: GuideWindowController?

    /// Abre la ventana "Cómo funciona".
    func showGuide() {
        if guide == nil { guide = GuideWindowController(app: self) }
        guide?.present()
    }

    /// Abre la ventana de bienvenida / configuración.
    func showWelcome() {
        if welcome == nil { welcome = WelcomeWindowController(app: self) }
        welcome?.present()
    }

    /// Vigila `folder` a partir de ahora. Si `connect`, pone los hooks en esa carpeta
    /// (salvo que ya use el plugin) y los quita de la anterior.
    func applyFolder(_ path: String, connect: Bool) throws {
        let folder = Config.normalize(path)
        if connect && !HookInstaller.pluginEnabled(folder: folder) {
            try HookInstaller.install(folder: folder)
        }
        if let old = Config.watchedFolder, Config.normalize(old) != folder {
            try? HookInstaller.uninstall(folder: old)
        }
        Config.watchedFolder = folder
        stage.removeAllSessions()
        watcher.rescanAll()
    }

    func applicationDidFinishLaunching(_ notification: Notification) {
        avatars = SpriteLibrary.all()
        let saved = UserDefaults.standard.string(forKey: Config.keyAvatar)
        guard let lib = avatars.first(where: { $0.id == saved }) ?? avatars.first else {
            let a = NSAlert()
            a.messageText = L.t("alert.noSprites")
            a.informativeText = L.t("alert.reinstall")
            a.runModal()
            NSApp.terminate(nil)
            return
        }
        ensureHookInstalled()
        stage = Stage(sprites: lib)
        watcher = SessionWatcher(dir: Config.sessionsDir) { [weak self] e in
            self?.handle(e)
        }
        watcher.start()

        stage.onNinjaClicked = { [weak self] n in self?.showBubble(for: n) }
        // Un clic en cualquier otro sitio cierra el bocadillo.
        clickMonitor = NSEvent.addGlobalMonitorForEvents(matching: [.leftMouseDown, .rightMouseDown]) {
            [weak self] _ in MainActor.assumeIsolated { self?.stage.closeBubble() }
        }
        // Primera vez (sin carpeta): la bienvenida se encarga de todo, Calendario incluido.
        if Config.watchedFolder == nil {
            DispatchQueue.main.async { [weak self] in self?.showWelcome() }
        } else if calendar.status == .notDetermined {
            calendar.requestAccess { _ in }
        }

        // Si la app de Claude se cierra, sus sesiones mueren: quitamos sus ninjas.
        NSWorkspace.shared.notificationCenter.addObserver(
            self, selector: #selector(appTerminated(_:)),
            name: NSWorkspace.didTerminateApplicationNotification, object: nil)
        if !claudeIsRunning { clearSessions() }   // p. ej. al encender el Mac

        stage.setHidden(UserDefaults.standard.bool(forKey: Config.keyHidden))
        menuBar = MenuBar(app: self, sprites: lib)
        MenuBar.configureLoginItemFirstTime()
    }

    private func showBubble(for n: Ninja) {
        let (cur, next) = calendar.hasAccess ? calendar.currentAndNext() : (nil, nil)
        let text = BubbleView.makeText(current: cur, next: next, hasAccess: calendar.hasAccess,
                                       colorIndex: n.colorIndex)
        let bubble = BubbleView(text: text, removable: !n.isGuardian)
        bubble.onClose = { [weak self] in self?.stage.closeBubble() }
        bubble.onRemove = { [weak self, weak n] in
            guard let self, let n else { return }
            // Borra su archivo: si esa sesión vuelve a trabajar, el ninja reaparece.
            try? FileManager.default.removeItem(
                at: Config.sessionsDir.appendingPathComponent("\(n.id).json"))
            self.stage.dismiss(n)
        }
        stage.showBubble(bubble, for: n)
    }

    static let claudeBundleID = "com.anthropic.claudefordesktop"

    private var claudeIsRunning: Bool {
        !NSRunningApplication.runningApplications(withBundleIdentifier: AppDelegate.claudeBundleID).isEmpty
    }

    @objc private func appTerminated(_ note: Notification) {
        let app = note.userInfo?[NSWorkspace.applicationUserInfoKey] as? NSRunningApplication
        guard app?.bundleIdentifier == AppDelegate.claudeBundleID else { return }
        clearSessions()
    }

    /// Quita todos los ninjas de sesión y sus archivos; queda el ninja principal.
    private func clearSessions() {
        let fm = FileManager.default
        if let files = try? fm.contentsOfDirectory(at: Config.sessionsDir, includingPropertiesForKeys: nil) {
            for f in files where f.pathExtension == "json" { try? fm.removeItem(at: f) }
        }
        stage.removeAllSessions()
        watcher.rescanAll()
    }

    /// Solo cuentan las sesiones cuyo `cwd` está dentro de la carpeta vigilada.
    private func handle(_ e: SessionEvent) {
        guard Config.isWatched(cwd: e.cwd) else { return }
        stage.apply(e)
    }

    /// Si falta el hook en Application Support, lo copia desde la app.
    private func ensureHookInstalled() {
        let fm = FileManager.default
        guard let src = Bundle.main.url(forResource: "pixelbuddy-hook", withExtension: "sh") else { return }
        let dst = Config.hookURL
        try? fm.createDirectory(at: Config.supportDir, withIntermediateDirectories: true)
        if !fm.contentsEqual(atPath: src.path, andPath: dst.path) {
            try? fm.removeItem(at: dst)
            try? fm.copyItem(at: src, to: dst)
            try? fm.setAttributes([.posixPermissions: 0o755], ofItemAtPath: dst.path)
        }
    }
}
