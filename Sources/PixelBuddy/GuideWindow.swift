import AppKit

/// Ventana "Cómo funciona": cada estado con su animación real (del personaje elegido)
/// y qué significa, más unos consejos. Se abre al terminar la bienvenida y desde el menú.
final class GuideWindowController: NSWindowController, NSWindowDelegate {
    private unowned let app: AppDelegate
    private var rows: [(view: NSImageView, frames: [NSImage], info: AnimInfo)] = []
    private var timer: Timer?
    private var start = CACurrentMediaTime()
    private var builtFor = ""

    private static let width: CGFloat = 440
    private static let scale: CGFloat = 1.5          // 32 px → 48 pt
    private static let cell = NSSize(width: 56 * 1.5, height: 32 * 1.5)

    private static let states: [(state: String, key: String)] = [
        ("run", "guide.run"), ("katana", "guide.katana"), ("ask", "guide.ask"),
        ("alert", "guide.alert"), ("train", "guide.train"), ("sleep", "guide.sleep"),
    ]

    init(app: AppDelegate) {
        self.app = app
        let w = NSWindow(contentRect: NSRect(x: 0, y: 0, width: Self.width + 48, height: 600),
                         styleMask: [.titled, .closable], backing: .buffered, defer: false)
        w.title = "PixelBuddy"
        w.isReleasedWhenClosed = false
        super.init(window: w)
        w.delegate = self
    }
    required init?(coder: NSCoder) { fatalError() }

    func present() {
        if builtFor != app.currentAvatarID { build() }
        start = CACurrentMediaTime()
        let t = Timer(timeInterval: 1.0 / 12.0, target: self, selector: #selector(tick),
                      userInfo: nil, repeats: true)
        RunLoop.main.add(t, forMode: .common)
        timer?.invalidate()
        timer = t
        NSApp.activate()
        window?.center()
        window?.makeKeyAndOrderFront(nil)
    }

    func windowWillClose(_ notification: Notification) {
        timer?.invalidate()
        timer = nil
    }

    // MARK: - Construcción

    private func label(_ s: String, size: CGFloat = 13, bold: Bool = false,
                       secondary: Bool = false, width: CGFloat = GuideWindowController.width) -> NSTextField {
        let l = NSTextField(wrappingLabelWithString: s)
        l.font = bold ? .boldSystemFont(ofSize: size) : .systemFont(ofSize: size)
        if secondary { l.textColor = .secondaryLabelColor }
        l.preferredMaxLayoutWidth = width
        return l
    }

    /// Frame pixel art nítido, colocado para que el centro del personaje (anchorX) quede
    /// siempre en el mismo sitio, como en pantalla.
    private func pixelFrame(_ cg: CGImage, anchorX: Int) -> NSImage {
        let s = Self.scale
        return NSImage(size: Self.cell, flipped: false) { _ in
            guard let ctx = NSGraphicsContext.current?.cgContext else { return false }
            ctx.interpolationQuality = .none
            let x = (32 - CGFloat(anchorX)) * s
            ctx.draw(cg, in: CGRect(x: x, y: 0, width: CGFloat(cg.width) * s, height: CGFloat(cg.height) * s))
            return true
        }
    }

    private func build() {
        builtFor = app.currentAvatarID
        rows.removeAll()
        let lib = app.avatars.first { $0.id == app.currentAvatarID } ?? app.avatars[0]
        let variant = lib.variants.first ?? "azul"

        let stack = NSStackView()
        stack.orientation = .vertical
        stack.alignment = .leading
        stack.spacing = 8
        stack.edgeInsets = NSEdgeInsets(top: 22, left: 24, bottom: 20, right: 24)

        stack.addArrangedSubview(label(L.t("guide.title"), size: 20, bold: true))
        stack.addArrangedSubview(label(L.t("guide.intro"), secondary: true))
        stack.setCustomSpacing(14, after: stack.arrangedSubviews.last!)

        let textWidth = Self.width - Self.cell.width - 12
        for (state, key) in Self.states {
            guard let info = lib.anims[state] else { continue }
            let frames = lib.frames(variant: variant, state: state)
                .map { pixelFrame($0, anchorX: info.anchorX) }
            let iv = NSImageView()
            iv.imageScaling = .scaleNone
            iv.image = frames.first
            iv.widthAnchor.constraint(equalToConstant: Self.cell.width).isActive = true
            iv.heightAnchor.constraint(equalToConstant: Self.cell.height).isActive = true
            rows.append((iv, frames, info))

            let text = NSStackView(views: [label(L.t(key + ".t"), bold: true, width: textWidth),
                                           label(L.t(key + ".d"), size: 12, secondary: true, width: textWidth)])
            text.orientation = .vertical
            text.alignment = .leading
            text.spacing = 2
            let row = NSStackView(views: [iv, text])
            row.alignment = .centerY
            row.spacing = 12
            stack.addArrangedSubview(row)
        }
        stack.setCustomSpacing(16, after: stack.arrangedSubviews.last!)

        stack.addArrangedSubview(label(L.t("guide.more"), bold: true))
        for k in 1...5 {
            stack.addArrangedSubview(label("•  " + L.t("guide.tip\(k)"), size: 12))
        }
        stack.setCustomSpacing(16, after: stack.arrangedSubviews.last!)

        let ok = NSButton(title: L.t("guide.ok"), target: self, action: #selector(done))
        ok.keyEquivalent = "\r"
        let bottom = NSStackView(views: [NSView(), ok])
        bottom.widthAnchor.constraint(equalToConstant: Self.width).isActive = true
        stack.addArrangedSubview(bottom)

        window?.contentView = stack
    }

    @objc private func tick() {
        let elapsed = CACurrentMediaTime() - start
        for r in rows where !r.frames.isEmpty {
            let i = min(r.info.frameIndex(elapsed: elapsed), r.frames.count - 1)
            if r.view.image !== r.frames[i] { r.view.image = r.frames[i] }
        }
    }

    @objc private func done() { close() }
}
