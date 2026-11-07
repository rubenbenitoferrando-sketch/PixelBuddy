import QuartzCore

enum NinjaState: String {
    case idle, run, katana, train, sleep, alert, ask
}

/// Un ninja en pantalla (uno por sesión de Claude Code, más el guardián).
final class Ninja {
    static let katanaDuration: TimeInterval = 30
    static let sleepAfter: TimeInterval = 10 * 60
    static let trainDuration: TimeInterval = 60
    /// El ninja principal se duerme a los 2 min sin actividad de Claude.
    static let guardianSleepAfter: TimeInterval = 2 * 60
    static let forgetAfter: TimeInterval = 2 * 60 * 60

    let id: String
    let colorIndex: Int
    let isGuardian: Bool

    /// Estado lógico (lo que dicen los eventos).
    var state: NinjaState = .idle
    /// Hora (epoch) en que empezó el estado lógico.
    var stateSince: TimeInterval
    /// Hora (epoch) del último evento recibido.
    var lastEvent: TimeInterval

    /// Animación que se ve (puede ser `run` mientras vuelve a su sitio).
    var anim: NinjaState = .idle
    var animStart: CFTimeInterval = CACurrentMediaTime()

    var x: CGFloat = 0          // centro del ninja, en puntos de la ventana
    var dir: CGFloat = 1        // 1 = derecha, -1 = izquierda
    var homeX: CGFloat = 0      // sitio donde se queda quieto
    let speed: CGFloat

    let layer = CALayer()
    var renderKey = ""

    init(id: String, colorIndex: Int, isGuardian: Bool = false, now: TimeInterval) {
        self.id = id
        self.colorIndex = colorIndex
        self.isGuardian = isGuardian
        self.stateSince = now
        self.lastEvent = now
        self.speed = CGFloat.random(in: 130...170)
        layer.magnificationFilter = .nearest
        layer.minificationFilter = .nearest
        layer.contentsGravity = .resize
    }

    func setState(_ s: NinjaState, since: TimeInterval) {
        if s != state || s == .katana { stateSince = since }
        state = s
    }

    /// Transiciones por tiempo: katana (30 s) → train (1 min, Claude espera tu
    /// mensaje) → sleep. Cualquier otro estado (salvo alert/ask) → sleep a los 10 min.
    func applyTimers(now: TimeInterval) {
        if state == .katana && now - stateSince >= Ninja.katanaDuration {
            setState(.train, since: stateSince + Ninja.katanaDuration)
        }
        if state == .train && now - stateSince >= Ninja.trainDuration {
            setState(.sleep, since: stateSince + Ninja.trainDuration)
        }
        // Un error o una pregunta no se duermen: siguen avisando hasta que respondas.
        if state != .sleep && state != .alert && state != .ask
            && now - lastEvent >= Ninja.sleepAfter {
            setState(.sleep, since: lastEvent + Ninja.sleepAfter)
        }
    }
}
