import AppKit

/// Ventana de bienvenida: personaje, carpeta de proyecto (+ conectar Claude Code) y Calendario.
/// Se abre sola la primera vez y después desde el menú "Configurar PixelBuddy…".
final class WelcomeWindowController: NSWindowController, NSWindowDelegate {
    private unowned let app: AppDelegate
    private var folder: String? = Config.watchedFolder

    private let folderLabel = NSTextField(labelWithString: "")
    private let connectCheck = NSButton(checkboxWithTitle: L.t("welcome.connect"), target: nil, action: nil)
    private let calButton = NSButton(title: L.t("welcome.grant"), target: nil, action: nil)
    private let calStatus = NSTextField(wrappingLabelWithString: "")
    private let errorLabel = NSTextField(wrappingLabelWithString: "")
    private var avatarButtons: [NSButton] = []

    private static let width: CGFloat = 500

    init(app: AppDelegate) {
        self.app = app
        let w = NSWindow(contentRect: NSRect(x: 0, y: 0, width: Self.width + 48, height: 600),
                         styleMask: [.titled, .closable], backing: .buffered, defer: false)
        w.title = "PixelBuddy"
        w.isReleasedWhenClosed = false
        super.init(window: w)
        w.delegate = self
        build()
    }
    required init?(coder: NSCoder) { fatalError() }

    func present() {
        refresh()
        NSApp.activate()
        window?.center()
        window?.makeKeyAndOrderFront(nil)
    }

    // MARK: - Construcción

    private func label(_ s: String, size: CGFloat = 13, bold: Bool = false,
                       secondary: Bool = false) -> NSTextField {
        let l = NSTextField(wrappingLabelWithString: s)
        l.font = bold ? .boldSystemFont(ofSize: size) : .systemFont(ofSize: size)
        if secondary { l.textColor = .secondaryLabelColor }
        l.preferredMaxLayoutWidth = Self.width
        return l
    }

    /// Imagen pixel art nítida (sin suavizado) a partir de un frame del sprite.
    private func pixelImage(_ cg: CGImage, side: CGFloat) -> NSImage {
        NSImage(size: NSSize(width: side, height: side), flipped: false) { rect in
            NSGraphicsContext.current?.imageInterpolation = .none
            NSGraphicsContext.current?.cgContext.interpolationQuality = .none
            NSGraphicsContext.current?.cgContext.draw(cg, in: rect)
            return true
        }
    }

    private func build() {
        let stack = NSStackView()
        stack.orientation = .vertical
        stack.alignment = .leading
        stack.spacing = 10
        stack.edgeInsets = NSEdgeInsets(top: 24, left: 24, bottom: 20, right: 24)

        stack.addArrangedSubview(label(L.t("welcome.title"), size: 20, bold: true))
        stack.addArrangedSubview(label(L.t("welcome.intro"), secondary: true))
        stack.setCustomSpacing(18, after: stack.arrangedSubviews.last!)

        // 1. Personaje
        stack.addArrangedSubview(label(L.t("welcome.step1"), bold: true))
        // Cuadrícula de 5 por fila (con 10 personajes no caben en una)
        let avatars = NSStackView()
        avatars.orientation = .vertical
        avatars.alignment = .leading
        avatars.spacing = 8
        var avatarRow: NSStackView?
        for (i, lib) in app.avatars.enumerated() {
            if i % 5 == 0 {
                let r = NSStackView()
                r.spacing = 6
                r.distribution = .fillEqually
                r.widthAnchor.constraint(equalToConstant: Self.width).isActive = true
                avatars.addArrangedSubview(r)
                avatarRow = r
            }
            let b = NSButton(radioButtonWithTitle: lib.displayName, target: self,
                             action: #selector(chooseAvatar(_:)))
            b.identifier = NSUserInterfaceItemIdentifier(lib.id)
            b.font = .systemFont(ofSize: 11)
            if let f = lib.frames(variant: lib.variants.first ?? "azul", state: "idle").first {
                b.image = pixelImage(f, side: 56)
                b.imagePosition = .imageAbove
            }
            avatarButtons.append(b)
            avatarRow?.addArrangedSubview(b)
        }
        stack.addArrangedSubview(avatars)
        stack.setCustomSpacing(18, after: avatars)

        // 2. Carpeta + conectar Claude Code
        stack.addArrangedSubview(label(L.t("welcome.step2"), bold: true))
        stack.addArrangedSubview(label(L.t("welcome.step2.help"), size: 12, secondary: true))
        let choose = NSButton(title: L.t("welcome.chooseFolder"), target: self, action: #selector(chooseFolder))
        folderLabel.lineBreakMode = .byTruncatingMiddle
        folderLabel.textColor = .secondaryLabelColor
        folderLabel.setContentCompressionResistancePriority(.defaultLow, for: .horizontal)
        let row = NSStackView(views: [choose, folderLabel])
        row.spacing = 10
        row.widthAnchor.constraint(lessThanOrEqualToConstant: Self.width).isActive = true
        stack.addArrangedSubview(row)
        connectCheck.state = .on
        stack.addArrangedSubview(connectCheck)
        stack.addArrangedSubview(label(L.t("welcome.connect.help"), size: 11, secondary: true))
        stack.setCustomSpacing(18, after: stack.arrangedSubviews.last!)

        // 3. Calendario
        stack.addArrangedSubview(label(L.t("welcome.step3"), bold: true))
        stack.addArrangedSubview(label(L.t("welcome.step3.help"), size: 12, secondary: true))
        calButton.target = self
        calButton.action = #selector(grantCalendar)
        calStatus.preferredMaxLayoutWidth = Self.width - 200
        calStatus.font = .systemFont(ofSize: 12)
        let calRow = NSStackView(views: [calButton, calStatus])
        calRow.spacing = 10
        stack.addArrangedSubview(calRow)
        stack.setCustomSpacing(20, after: calRow)

        // Errores y botón final
        errorLabel.textColor = .systemRed
        errorLabel.preferredMaxLayoutWidth = Self.width
        errorLabel.isHidden = true
        stack.addArrangedSubview(errorLabel)
        let start = NSButton(title: L.t("welcome.start"), target: self, action: #selector(start))
        start.keyEquivalent = "\r"
        let spacer = NSView()
        let bottom = NSStackView(views: [spacer, start])
        bottom.widthAnchor.constraint(equalToConstant: Self.width).isActive = true
        stack.addArrangedSubview(bottom)

        window?.contentView = stack
        refresh()
    }

    private func refresh() {
        for b in avatarButtons { b.state = b.identifier?.rawValue == app.currentAvatarID ? .on : .off }
        folderLabel.stringValue = folder.map { ($0 as NSString).abbreviatingWithTildeInPath } ?? "—"
        if let f = folder, HookInstaller.pluginEnabled(folder: f) {
            connectCheck.state = .off   // ya usa el plugin: no hacen falta los hooks
        }
        switch app.calendar.status {
        case .fullAccess:
            calStatus.stringValue = L.t("welcome.granted")
            calStatus.textColor = .systemGreen
            calButton.isEnabled = false
        case .notDetermined:
            calStatus.stringValue = ""
            calButton.isEnabled = true
        default:
            calStatus.stringValue = L.t("welcome.denied")
            calStatus.textColor = .secondaryLabelColor
            calButton.isEnabled = true
        }
        window?.layoutIfNeeded()
    }

    // MARK: - Acciones

    @objc private func chooseAvatar(_ sender: NSButton) {
        if let id = sender.identifier?.rawValue { app.selectAvatar(id) }
        refresh()
    }

    @objc private func chooseFolder() {
        guard let w = window else { return }
        let panel = NSOpenPanel()
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.allowsMultipleSelection = false
        panel.prompt = L.t("panel.prompt")
        panel.message = L.t("panel.message")
        if let f = folder { panel.directoryURL = URL(fileURLWithPath: f) }
        panel.beginSheetModal(for: w) { [weak self] resp in
            guard resp == .OK, let url = panel.url else { return }
            self?.folder = Config.normalize(url.path)
            self?.errorLabel.isHidden = true
            self?.connectCheck.state = .on
            self?.refresh()
        }
    }

    @objc private func grantCalendar() {
        if app.calendar.status == .notDetermined {
            app.calendar.requestAccess { [weak self] _ in self?.refresh() }
        } else if let url = URL(string:
            "x-apple.systempreferences:com.apple.preference.security?Privacy_Calendars") {
            NSWorkspace.shared.open(url)
        }
    }

    @objc private func start() {
        guard let f = folder else {
            errorLabel.stringValue = L.t("welcome.needFolder")
            errorLabel.isHidden = false
            return
        }
        do {
            try app.applyFolder(f, connect: connectCheck.state == .on)
            close()
            app.showGuide()   // después de la bienvenida, cómo funciona cada cosa
        } catch {
            errorLabel.stringValue = error.localizedDescription
            errorLabel.isHidden = false
        }
    }

    func windowDidBecomeKey(_ notification: Notification) { refresh() }
}
