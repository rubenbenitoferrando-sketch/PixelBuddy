import AppKit
import EventKit

/// Bocadillo pixel art: borde de 2 px, esquinas en escalón, pico hacia el ninja
/// y fuente monoespaciada.
final class BubbleView: NSView, BubbleTail {
    static let px: CGFloat = 2          // un "píxel" del bocadillo
    static let pad: CGFloat = 10
    static let tailH: CGFloat = 8
    static let ink = NSColor(srgbRed: 18 / 255, green: 18 / 255, blue: 30 / 255, alpha: 1)
    static let paper = NSColor(srgbRed: 1, green: 0.99, blue: 0.95, alpha: 1)
    static let shade = NSColor(srgbRed: 0, green: 0, blue: 0, alpha: 0.25)

    static let buttonH: CGFloat = 20
    static let buttonGap: CGFloat = 8

    private let text: NSAttributedString
    private var tailX: CGFloat = 20
    private let removable: Bool

    /// Botón "Quitar ninja" (solo en ninjas de sesión).
    var onRemove: (() -> Void)?
    /// Clic en el resto del bocadillo.
    var onClose: (() -> Void)?

    private static let buttonLabel = NSAttributedString(string: L.t("bubble.remove"), attributes: [
        .font: NSFont.monospacedSystemFont(ofSize: 10, weight: .bold),
        .foregroundColor: paper])

    init(text: NSAttributedString, removable: Bool) {
        self.text = text
        self.removable = removable
        let size = text.boundingRect(with: NSSize(width: 240, height: 400),
                                     options: [.usesLineFragmentOrigin]).size
        var w = ceil(size.width) + 2 * BubbleView.pad + 4
        var h = ceil(size.height) + 2 * BubbleView.pad + BubbleView.tailH + BubbleView.px
        if removable {
            w = max(w, ceil(BubbleView.buttonLabel.size().width) + 2 * BubbleView.pad + 20)
            h += BubbleView.buttonH + BubbleView.buttonGap
        }
        super.init(frame: NSRect(x: 0, y: 0, width: max(w.rounded(.up), 60), height: h))
    }
    required init?(coder: NSCoder) { fatalError() }

    override var isFlipped: Bool { false }
    override func acceptsFirstMouse(for event: NSEvent?) -> Bool { true }

    /// Zona del botón, en coordenadas del bocadillo.
    private var buttonRect: NSRect {
        let p = BubbleView.px
        return NSRect(x: BubbleView.pad, y: BubbleView.tailH + BubbleView.pad - p,
                      width: bounds.width - p - 2 * BubbleView.pad, height: BubbleView.buttonH)
    }

    override func mouseDown(with event: NSEvent) {
        let pt = convert(event.locationInWindow, from: nil)
        if removable && buttonRect.insetBy(dx: -2, dy: -2).contains(pt) { onRemove?() } else { onClose?() }
    }

    func setTailX(_ x: CGFloat) {
        let p = BubbleView.px
        let clamped = min(max(x, 6 * p + 2), bounds.width - 6 * p - 2)
        let snapped = (clamped / p).rounded() * p
        if snapped != tailX { tailX = snapped; needsDisplay = true }
    }

    override func draw(_ dirtyRect: NSRect) {
        let p = BubbleView.px
        let w = bounds.width - p          // deja sitio para la sombra
        let bodyY = BubbleView.tailH
        let bodyH = bounds.height - bodyY

        func stepped(_ r: NSRect, _ c: NSColor) {
            c.setFill()
            NSRect(x: r.minX + p, y: r.minY, width: r.width - 2 * p, height: r.height).fill()
            NSRect(x: r.minX, y: r.minY + p, width: r.width, height: r.height - 2 * p).fill()
        }
        // Sombra, borde y relleno del cuerpo.
        stepped(NSRect(x: p, y: bodyY - p, width: w, height: bodyH), BubbleView.shade)
        stepped(NSRect(x: 0, y: bodyY, width: w, height: bodyH), BubbleView.ink)
        stepped(NSRect(x: p, y: bodyY + p, width: w - 2 * p, height: bodyH - 2 * p), BubbleView.paper)

        // Pico en escalones (4 filas de 2 px).
        for i in 0..<4 {
            let y = bodyY - CGFloat(i + 1) * p
            let outer = CGFloat(4 - i) * 2 * p
            BubbleView.ink.setFill()
            NSRect(x: tailX - outer / 2, y: y, width: outer, height: p).fill()
            let inner = outer - 2 * p
            if inner > 0 {
                BubbleView.paper.setFill()
                NSRect(x: tailX - inner / 2, y: y, width: inner, height: p).fill()
            }
        }
        BubbleView.paper.setFill()
        NSRect(x: tailX - 3 * p, y: bodyY, width: 6 * p, height: p).fill()

        var textY = bodyY + BubbleView.pad
        if removable {
            let r = buttonRect
            stepped(r, BubbleView.ink)
            let label = BubbleView.buttonLabel
            let ls = label.size()
            label.draw(at: NSPoint(x: (r.midX - ls.width / 2).rounded(),
                                   y: (r.midY - ls.height / 2).rounded()))
            textY = r.maxY + BubbleView.buttonGap
        }
        text.draw(with: NSRect(x: BubbleView.pad + 2, y: textY,
                               width: w - 2 * BubbleView.pad, height: bounds.height - textY - BubbleView.pad),
                  options: [.usesLineFragmentOrigin])
    }

    // MARK: - Texto

    /// Color del texto de cabecera para cada cinta (tonos oscuros legibles).
    static let accents: [NSColor] = [
        NSColor(srgbRed: 28 / 255, green: 79 / 255, blue: 160 / 255, alpha: 1),   // azul
        NSColor(srgbRed: 150 / 255, green: 25 / 255, blue: 35 / 255, alpha: 1),   // rojo
        NSColor(srgbRed: 30 / 255, green: 115 / 255, blue: 50 / 255, alpha: 1),   // verde
        NSColor(srgbRed: 95 / 255, green: 40 / 255, blue: 150 / 255, alpha: 1),   // morado
        NSColor(srgbRed: 170 / 255, green: 90 / 255, blue: 15 / 255, alpha: 1),   // naranja
        NSColor(srgbRed: 140 / 255, green: 110 / 255, blue: 10 / 255, alpha: 1),  // amarillo
    ]

    static func makeText(current: EKEvent?, next: EKEvent?, hasAccess: Bool,
                         colorIndex: Int) -> NSAttributedString {
        let body = NSFont.monospacedSystemFont(ofSize: 11, weight: .regular)
        let bold = NSFont.monospacedSystemFont(ofSize: 11, weight: .bold)
        let accent = accents[colorIndex % accents.count]
        let para = NSMutableParagraphStyle()
        para.lineSpacing = 2
        let out = NSMutableAttributedString()
        func add(_ s: String, _ font: NSFont, _ color: NSColor = ink) {
            out.append(NSAttributedString(string: s, attributes: [
                .font: font, .foregroundColor: color, .paragraphStyle: para]))
        }
        guard hasAccess else {
            add(L.t("bubble.noCalTitle") + "\n", bold, accent)
            add(L.t("bubble.noCalBody"), body)
            return out
        }
        if let c = current {
            add(L.t("bubble.now") + "\n", bold, accent)
            add(short(c.title) + "\n", bold)
            add(L.t("bubble.until", hour(c.endDate)), body)
        } else {
            add(L.t("bubble.now") + "\n", bold, accent)   // mismo formato que con un evento
            add(L.t("bubble.rest"), bold)
        }
        if let n = next {
            add("\n\n" + L.t("bubble.next") + "\n", bold, accent)
            add(short(n.title) + "\n", bold)
            add(when(n.startDate), body)
        }
        return out
    }

    private static func short(_ s: String?) -> String {
        let t = (s ?? "").trimmingCharacters(in: .whitespacesAndNewlines)
        if t.isEmpty { return L.t("bubble.untitled") }
        return t.count > 30 ? String(t.prefix(29)) + "…" : t
    }

    private static func hour(_ d: Date) -> String {
        let f = DateFormatter()
        f.locale = L.locale
        f.dateFormat = "HH:mm"
        return f.string(from: d)
    }

    private static func when(_ d: Date) -> String {
        let cal = Calendar.current
        if cal.isDateInToday(d) { return L.t("bubble.today", hour(d)) }
        if cal.isDateInTomorrow(d) { return L.t("bubble.tomorrow", hour(d)) }
        let f = DateFormatter()
        f.locale = L.locale
        f.dateFormat = "EEEE d"
        return L.t("bubble.dayAt", f.string(from: d), hour(d))
    }
}
