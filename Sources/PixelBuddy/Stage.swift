import AppKit
import QuartzCore

/// Panel transparente, sin bordes y siempre encima, que no roba el foco.
final class NinjaPanel: NSPanel {
    init() {
        super.init(contentRect: NSRect(x: 0, y: 0, width: 400, height: 200),
                   styleMask: [.borderless, .nonactivatingPanel],
                   backing: .buffered, defer: false)
        isOpaque = false
        backgroundColor = .clear
        hasShadow = false
        level = .statusBar
        collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .stationary, .ignoresCycle]
        isFloatingPanel = true
        hidesOnDeactivate = false
        becomesKeyOnlyIfNeeded = true
        isMovable = false
        isReleasedWhenClosed = false
        ignoresMouseEvents = true
        animationBehavior = .none
    }
    override var canBecomeKey: Bool { false }
    override var canBecomeMain: Bool { false }
}

final class StageView: NSView {
    var onClick: ((NSPoint) -> Void)?
    override var isFlipped: Bool { false }
    override func acceptsFirstMouse(for event: NSEvent?) -> Bool { true }
    override func mouseDown(with event: NSEvent) {
        onClick?(convert(event.locationInWindow, from: nil))
    }
}

/// Evento de sesión leído de `sessions/<id>.json`.
struct SessionEvent {
    let sessionId: String
    let cwd: String
    let event: String
    /// Herramienta (tool_name) o tipo de notificación (notification_type).
    let detail: String
    let ts: TimeInterval
}

/// Escenario: la franja inferior izquierda con todos los ninjas.
final class Stage: NSObject {
    static let scale: CGFloat = 2
    static let windowHeight: CGFloat = 260
    static let guardianID = "__guardian__"
    /// Notificaciones que significan "Claude te pregunta o te pide permiso".
    static let askNotifications: Set<String> = [
        "permission_prompt", "elicitation_dialog", "elicitation_url_dialog", "agent_needs_input"]

    let panel = NinjaPanel()
    let view = StageView()
    private(set) var sprites: SpriteLibrary
    private(set) var ninjas: [Ninja] = []   // por orden de aparición

    /// Se llama al hacer clic en un ninja.
    var onNinjaClicked: ((Ninja) -> Void)?

    private var timer: Timer?
    private var lastTick = CACurrentMediaTime()
    /// Última actividad de Claude en la carpeta vigilada. Se guarda para que el
    /// ninja principal no se "despierte" cada vez que se reinicia la app.
    private var lastActivity: TimeInterval = {
        let saved = UserDefaults.standard.double(forKey: Config.keyLastActivity)
        return saved > 0 ? saved : Date().timeIntervalSince1970
    }() {
        didSet {
            if lastActivity != oldValue {
                UserDefaults.standard.set(lastActivity, forKey: Config.keyLastActivity)
            }
        }
    }
    private var lastLayoutCheck: CFTimeInterval = 0
    private(set) var isHidden = false

    // Bocadillo
    private(set) var bubble: NSView?
    private(set) weak var bubbleNinja: Ninja?
    private var bubbleTimer: Timer?

    init(sprites: SpriteLibrary) {
        self.sprites = sprites
        super.init()
        UserDefaults.standard.set(lastActivity, forKey: Config.keyLastActivity)
        view.wantsLayer = true
        view.layer?.backgroundColor = .clear
        panel.contentView = view
        view.onClick = { [weak self] p in self?.handleClick(p) }
        layoutWindow()
        updateGuardian()
        NotificationCenter.default.addObserver(self, selector: #selector(screensChanged),
            name: NSApplication.didChangeScreenParametersNotification, object: nil)
        scheduleTimer(interval: Stage.fastInterval)
        panel.orderFrontRegardless()
    }

    // Refresco adaptativo: fluido solo cuando alguien corre, para ahorrar batería.
    static let fastInterval: TimeInterval = 1.0 / 60.0   // algún ninja corriendo
    static let slowInterval: TimeInterval = 1.0 / 15.0   // todos quietos (sprites a ≤ 8 fps)
    static let hiddenInterval: TimeInterval = 1.0        // ninjas ocultos

    private func scheduleTimer(interval: TimeInterval) {
        if let t = timer, t.timeInterval == interval { return }
        timer?.invalidate()
        let t = Timer(timeInterval: interval, target: self, selector: #selector(tick),
                      userInfo: nil, repeats: true)
        t.tolerance = interval * 0.1
        RunLoop.main.add(t, forMode: .common)
        timer = t
    }

    // MARK: - Ventana

    private func desiredFrame() -> NSRect {
        guard let screen = NSScreen.screens.first else { return panel.frame }
        let vis = screen.visibleFrame   // excluye el Dock y la barra de menús
        let width = floor(screen.frame.width / 3)
        return NSRect(x: vis.minX, y: vis.minY, width: width, height: Stage.windowHeight)
    }

    func layoutWindow() {
        let f = desiredFrame()
        if f != panel.frame {
            panel.setFrame(f, display: true)
            layoutHomes()
        }
    }

    @objc private func screensChanged() { layoutWindow() }

    func setHidden(_ hidden: Bool) {
        isHidden = hidden
        if hidden { closeBubble(); panel.orderOut(nil) } else { panel.orderFrontRegardless() }
        scheduleTimer(interval: hidden ? Stage.hiddenInterval : Stage.fastInterval)
    }

    /// Cambia de personaje: los ninjas siguen en su estado, solo cambia el dibujo.
    func setSprites(_ lib: SpriteLibrary) {
        sprites = lib
        for n in ninjas { n.renderKey = "" }
    }

    // MARK: - Ninjas

    private var sessionNinjas: [Ninja] { ninjas.filter { !$0.isGuardian } }

    /// Sitio fijo de cada ninja cuando está quieto (sin solaparse).
    private func layoutHomes() {
        let w = view.bounds.width
        let margin: CGFloat = 40
        let n = ninjas.count
        let spacing: CGFloat = n > 1 ? min(76, max(20, (w - 2 * margin) / CGFloat(n - 1))) : 0
        for (i, nj) in ninjas.enumerated() { nj.homeX = margin + CGFloat(i) * spacing }
    }

    private func addNinja(id: String, guardian: Bool = false, now: TimeInterval) -> Ninja {
        let variants = sprites.variants
        let index: Int
        if guardian {
            index = 0
        } else {
            let used = Set(sessionNinjas.map { $0.colorIndex % variants.count })
            index = (0..<variants.count).first { !used.contains($0) } ?? sessionNinjas.count
        }
        let n = Ninja(id: id, colorIndex: index,
                      isGuardian: guardian, now: now)
        ninjas.append(n)
        view.layer?.addSublayer(n.layer)
        layoutHomes()
        n.x = n.homeX
        return n
    }

    private func removeNinja(_ n: Ninja) {
        if bubbleNinja === n { closeBubble() }
        n.layer.removeFromSuperlayer()
        ninjas.removeAll { $0 === n }
        layoutHomes()
    }

    /// Quita un ninja de sesión a mano (vuelve si esa sesión manda nuevos eventos).
    func dismiss(_ n: Ninja) {
        guard !n.isGuardian else { return }
        removeNinja(n)
        updateGuardian()
    }

    func removeAllSessions() {
        for n in sessionNinjas { removeNinja(n) }
        updateGuardian()
    }

    /// Si no hay sesiones, queda un ninja guardián para poder ver el calendario.
    private func updateGuardian() {
        let now = Date().timeIntervalSince1970
        let guardian = ninjas.first { $0.isGuardian }
        if sessionNinjas.isEmpty {
            if guardian == nil {
                let g = addNinja(id: Stage.guardianID, guardian: true, now: now)
                g.lastEvent = lastActivity
            }
        } else if let g = guardian {
            removeNinja(g)
        }
    }

    /// Aplica un evento de Claude Code (ya filtrado por carpeta).
    func apply(_ e: SessionEvent) {
        let now = Date().timeIntervalSince1970
        lastActivity = max(lastActivity, min(e.ts, now))
        defer { if !isHidden { scheduleTimer(interval: Stage.fastInterval) } }
        let existing = ninjas.first { $0.id == e.sessionId && !$0.isGuardian }
        if e.event == "SessionEnd" || now - e.ts >= Ninja.forgetAfter {
            if let n = existing { removeNinja(n) }
            updateGuardian()
            return
        }
        let isNew = existing == nil
        let n = existing ?? addNinja(id: e.sessionId, now: e.ts)
        // El archivo de cada sesión siempre contiene su último evento: su hora manda.
        n.lastEvent = e.ts
        switch e.event {
        case "PreToolUse" where e.detail == "AskUserQuestion":
            n.setState(.ask, since: e.ts)          // Claude te hace una pregunta
        case "UserPromptSubmit", "PreToolUse", "PostToolUse": n.setState(.run, since: e.ts)
        case "PermissionRequest": n.setState(.ask, since: e.ts)
        case "Notification" where Stage.askNotifications.contains(e.detail):
            n.setState(.ask, since: e.ts)
        case "Notification":
            // Otros avisos ("lleva un rato esperando") no tapan una pregunta ni un error.
            if n.state != .ask && n.state != .alert { n.setState(.train, since: e.ts) }
        case "Stop": n.setState(.katana, since: e.ts)
        case "StopFailure": n.setState(.alert, since: e.ts)
        default: break
        }
        n.applyTimers(now: now)
        if isNew && n.state == .run { n.dir = Bool.random() ? 1 : -1 }
        updateGuardian()
    }

    // MARK: - Animación

    @objc private func tick() {
        let now = CACurrentMediaTime()
        let dt = CGFloat(min(now - lastTick, 0.1))
        lastTick = now
        let wall = Date().timeIntervalSince1970

        if now - lastLayoutCheck > 2 { lastLayoutCheck = now; layoutWindow() }

        for n in ninjas where !n.isGuardian && wall - n.lastEvent >= Ninja.forgetAfter {
            removeNinja(n)
        }
        updateGuardian()

        let width = view.bounds.width
        let minX: CGFloat = 64, maxX = max(minX, width - 64)

        CATransaction.begin()
        CATransaction.setDisableActions(true)
        for n in ninjas {
            if n.isGuardian {
                n.state = wall - lastActivity >= Ninja.guardianSleepAfter ? .sleep : .idle
            } else {
                n.applyTimers(now: wall)
            }
            var target = n.state
            if n.state == .run {
                n.x += n.dir * n.speed * dt
                if n.x >= maxX { n.x = maxX; n.dir = -1 }
                if n.x <= minX { n.x = minX; n.dir = 1 }
            } else if abs(n.x - n.homeX) > 0.5 {
                // Vuelve corriendo a su sitio antes de cambiar de animación.
                target = .run
                n.dir = n.homeX > n.x ? 1 : -1
                let step = n.speed * 1.3 * dt
                n.x = abs(n.homeX - n.x) <= step ? n.homeX : n.x + n.dir * step
            } else {
                n.x = n.homeX
                n.dir = 1
            }
            if target != n.anim { n.anim = target; n.animStart = now }
            render(n, now: now)
        }
        CATransaction.commit()

        updateBubblePosition()
        updateMousePassthrough()

        let moving = ninjas.contains { $0.anim == .run }
        scheduleTimer(interval: isHidden ? Stage.hiddenInterval
                                         : (moving ? Stage.fastInterval : Stage.slowInterval))
    }

    private func render(_ n: Ninja, now: CFTimeInterval) {
        guard let info = sprites.anims[n.anim.rawValue] else { return }
        // La variante se busca en el personaje actual por su número de orden: así cambiar de
        // personaje funciona aunque las variantes tengan otros nombres (razas del perro…).
        let variant = sprites.variants[n.colorIndex % max(1, sprites.variants.count)]
        let frames = sprites.frames(variant: variant, state: n.anim.rawValue)
        guard !frames.isEmpty else { return }
        let idx = min(info.frameIndex(elapsed: now - n.animStart), frames.count - 1)
        let px = (n.x * 2).rounded() / 2
        let key = "\(n.anim.rawValue)|\(idx)|\(px)|\(n.dir)"
        if key == n.renderKey { return }
        n.renderKey = key
        let s = Stage.scale
        n.layer.contents = frames[idx]
        n.layer.bounds = CGRect(x: 0, y: 0, width: CGFloat(info.frameW) * s,
                                height: CGFloat(info.frameH) * s)
        // Alineado por anchorX: el centro del ninja no "salta" al cambiar de estado.
        n.layer.anchorPoint = CGPoint(x: CGFloat(info.anchorX) / CGFloat(info.frameW), y: 0)
        n.layer.position = CGPoint(x: px, y: 0)
        n.layer.transform = n.dir < 0 ? CATransform3DMakeScale(-1, 1, 1) : CATransform3DIdentity
    }

    // MARK: - Ratón

    /// Zona clicable de un ninja (su cuerpo, sin la estela).
    private func hitRect(_ n: Ninja) -> NSRect {
        NSRect(x: n.x - 24, y: 0, width: 48, height: 60)
    }

    private func ninja(at p: NSPoint) -> Ninja? {
        ninjas.reversed().first { hitRect($0).contains(p) }
    }

    /// Los clics atraviesan la ventana salvo encima de un ninja o del bocadillo.
    private func updateMousePassthrough() {
        guard !isHidden else { return }
        let m = NSEvent.mouseLocation
        let p = NSPoint(x: m.x - panel.frame.minX, y: m.y - panel.frame.minY)
        let interactive = ninja(at: p) != nil || (bubble?.frame.contains(p) ?? false)
        if panel.ignoresMouseEvents == interactive { panel.ignoresMouseEvents = !interactive }
    }

    private func handleClick(_ p: NSPoint) {
        if let b = bubble, b.frame.contains(p) { closeBubble(); return }
        guard let n = ninja(at: p) else { return }
        if bubbleNinja === n { closeBubble(); return }
        onNinjaClicked?(n)
    }

    // MARK: - Bocadillo

    func showBubble(_ b: NSView, for n: Ninja) {
        closeBubble()
        bubble = b
        bubbleNinja = n
        b.wantsLayer = true
        b.layer?.zPosition = 100
        view.addSubview(b)
        updateBubblePosition()
        let t = Timer(timeInterval: 8, target: self, selector: #selector(bubbleTimeout),
                      userInfo: nil, repeats: false)
        RunLoop.main.add(t, forMode: .common)
        bubbleTimer = t
    }

    @objc private func bubbleTimeout() { closeBubble() }

    func closeBubble() {
        bubbleTimer?.invalidate()
        bubbleTimer = nil
        bubble?.removeFromSuperview()
        bubble = nil
        bubbleNinja = nil
    }

    private func updateBubblePosition() {
        guard let b = bubble, let n = bubbleNinja else { return }
        let w = b.frame.width
        let x = min(max(0, n.x - w / 2), max(0, view.bounds.width - w))
        b.setFrameOrigin(NSPoint(x: x.rounded(), y: 62))
        (b as? BubbleTail)?.setTailX(n.x - x)
    }
}

/// Un bocadillo cuyo pico apunta al ninja.
protocol BubbleTail: AnyObject {
    func setTailX(_ x: CGFloat)
}
