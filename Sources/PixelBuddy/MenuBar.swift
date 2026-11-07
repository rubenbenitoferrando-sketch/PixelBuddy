import AppKit
import ServiceManagement

/// Icono de mini ninja en la barra de menús y su menú.
final class MenuBar: NSObject, NSMenuDelegate {
    private let statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
    private unowned let app: AppDelegate

    init(app: AppDelegate, sprites: SpriteLibrary) {
        self.app = app
        super.init()
        updateIcon(sprites)
        statusItem.button?.toolTip = "PixelBuddy"
        let menu = NSMenu()
        menu.delegate = self
        statusItem.menu = menu
    }

    /// Icono de la barra de menús: el personaje elegido, en idle.
    func updateIcon(_ sprites: SpriteLibrary) {
        if let frame = sprites.frames(variant: sprites.variants.first ?? "azul", state: "idle").first {
            // 32 px en 16 pt: un píxel del sprite por píxel de pantalla Retina.
            statusItem.button?.image = NSImage(cgImage: frame, size: NSSize(width: 16, height: 16))
            statusItem.button?.title = ""
        } else {
            statusItem.button?.image = nil
            statusItem.button?.title = "◆"
        }
    }

    // MARK: - Arranque al iniciar sesión

    static var loginEnabled: Bool { SMAppService.mainApp.status == .enabled }

    /// La primera vez, activa "Abrir al iniciar sesión" (activado por defecto).
    static func configureLoginItemFirstTime() {
        let d = UserDefaults.standard
        guard !d.bool(forKey: Config.keyLoginConfigured) else { return }
        try? SMAppService.mainApp.register()
        d.set(true, forKey: Config.keyLoginConfigured)
    }

    // MARK: - Menú

    func menuNeedsUpdate(_ menu: NSMenu) {
        menu.removeAllItems()
        menu.addItem(item(app.stage.isHidden ? L.t("menu.show") : L.t("menu.hide"),
                          #selector(toggleHidden)))
        menu.addItem(.separator())

        let avatarItem = NSMenuItem(title: L.t("menu.avatar"), action: nil, keyEquivalent: "")
        let avatarMenu = NSMenu()
        for lib in app.avatars {
            let i = item(lib.displayName, #selector(chooseAvatar(_:)))
            i.representedObject = lib.id
            i.state = lib.id == app.currentAvatarID ? .on : .off
            if let f = lib.frames(variant: lib.variants.first ?? "azul", state: "idle").first {
                i.image = NSImage(cgImage: f, size: NSSize(width: 16, height: 16))
            }
            avatarMenu.addItem(i)
        }
        avatarItem.submenu = avatarMenu
        menu.addItem(avatarItem)

        let cals = NSMenuItem(title: L.t("menu.calendars"), action: nil, keyEquivalent: "")
        cals.submenu = calendarsMenu()
        menu.addItem(cals)
        menu.addItem(.separator())

        let folder = Config.watchedFolder
        let info = NSMenuItem(title: L.t("menu.watching", folder.map { ($0 as NSString).lastPathComponent }
                                                     ?? L.t("menu.noFolder")),
                              action: nil, keyEquivalent: "")
        info.toolTip = folder
        info.isEnabled = false
        menu.addItem(info)
        menu.addItem(item(L.t("menu.changeFolder"), #selector(changeFolder)))
        menu.addItem(item(L.t("menu.setup"), #selector(openSetup)))
        menu.addItem(item(L.t("menu.guide"), #selector(openGuide)))
        menu.addItem(.separator())

        let login = item(L.t("menu.login"), #selector(toggleLogin))
        login.state = MenuBar.loginEnabled ? .on : .off
        menu.addItem(login)
        menu.addItem(.separator())
        menu.addItem(item(L.t("menu.quit"), #selector(quit), key: "q"))
    }

    private func item(_ title: String, _ action: Selector, key: String = "") -> NSMenuItem {
        let i = NSMenuItem(title: title, action: action, keyEquivalent: key)
        i.target = self
        return i
    }

    private func calendarsMenu() -> NSMenu {
        let m = NSMenu()
        let cal = app.calendar
        guard cal.hasAccess else {
            m.addItem(item(L.t("menu.grantCalendar"), #selector(requestCalendar)))
            return m
        }
        let all = cal.allCalendars()
        if all.isEmpty {
            let i = NSMenuItem(title: L.t("menu.noCalendars"), action: nil, keyEquivalent: "")
            i.isEnabled = false
            m.addItem(i)
        }
        var lastSource = ""
        for c in all {
            if c.source.title != lastSource {
                if !lastSource.isEmpty { m.addItem(.separator()) }
                lastSource = c.source.title
                let h = NSMenuItem(title: lastSource, action: nil, keyEquivalent: "")
                h.isEnabled = false
                m.addItem(h)
            }
            let i = item(c.title, #selector(toggleCalendar(_:)))
            i.representedObject = c.calendarIdentifier
            i.state = cal.isEnabled(c) ? .on : .off
            i.indentationLevel = 1
            m.addItem(i)
        }
        return m
    }

    // MARK: - Acciones

    @objc private func toggleHidden() {
        let hide = !app.stage.isHidden
        app.stage.setHidden(hide)
        UserDefaults.standard.set(hide, forKey: Config.keyHidden)
    }

    @objc private func chooseAvatar(_ sender: NSMenuItem) {
        if let id = sender.representedObject as? String { app.selectAvatar(id) }
    }

    @objc private func toggleCalendar(_ sender: NSMenuItem) {
        if let id = sender.representedObject as? String { app.calendar.toggle(id) }
    }

    @objc private func requestCalendar() {
        if app.calendar.status == .notDetermined {
            app.calendar.requestAccess { _ in }
        } else if let url = URL(string:
            "x-apple.systempreferences:com.apple.preference.security?Privacy_Calendars") {
            NSWorkspace.shared.open(url)
        }
    }

    @objc private func toggleLogin() {
        do {
            if MenuBar.loginEnabled {
                try SMAppService.mainApp.unregister()
            } else {
                try SMAppService.mainApp.register()
                if SMAppService.mainApp.status == .requiresApproval {
                    SMAppService.openSystemSettingsLoginItems()
                }
            }
        } catch {
            alert(L.t("alert.loginFail"), error.localizedDescription)
        }
    }

    @objc private func changeFolder() {
        let panel = NSOpenPanel()
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.allowsMultipleSelection = false
        panel.prompt = L.t("panel.prompt")
        panel.message = L.t("panel.message")
        if let f = Config.watchedFolder { panel.directoryURL = URL(fileURLWithPath: f) }
        NSApp.activate()
        guard panel.runModal() == .OK, let url = panel.url else { return }
        let folder = Config.normalize(url.path)
        do {
            try app.applyFolder(folder, connect: true)
            alert(L.t("alert.nowWatching", (folder as NSString).lastPathComponent),
                  L.t("alert.restartSessions"))
        } catch {
            alert(L.t("alert.folderFail"), error.localizedDescription)
        }
    }

    @objc private func openSetup() { app.showWelcome() }
    @objc private func openGuide() { app.showGuide() }

    @objc private func quit() { NSApp.terminate(nil) }

    private func alert(_ title: String, _ text: String) {
        NSApp.activate()
        let a = NSAlert()
        a.messageText = title
        a.informativeText = text
        a.runModal()
    }
}
