// Genera las imágenes del README a partir de los sprites reales (sin dependencias):
//   docs/scene_en.png, docs/scene_es.png  — escritorio con varios personajes y el bocadillo
//   docs/gallery.png                      — los 10 personajes con sus 6 variantes
// Uso: swift tools/make_readme_images.swift
import AppKit
import ImageIO

let root = URL(fileURLWithPath: CommandLine.arguments[0]).deletingLastPathComponent()
    .deletingLastPathComponent()
let avatars = root.appendingPathComponent("assets/avatars")
let docs = root.appendingPathComponent("docs")

// MARK: - Sprites

struct Manifest: Decodable {
    struct State: Decodable { let frames: Int; let frameSize: [Int]; let anchorX: Int }
    let id: String; let prefix: String; let name: [String: String]
    let variants: [String]; let states: [String: State]
}

func manifest(_ id: String) -> Manifest {
    let data = try! Data(contentsOf: avatars.appendingPathComponent("\(id)/manifest.json"))
    return try! JSONDecoder().decode(Manifest.self, from: data)
}

/// Un frame del sprite y su anchorX.
func sprite(_ id: String, _ variant: String, _ state: String, _ index: Int) -> (CGImage, Int) {
    let m = manifest(id)
    let s = m.states[state]!
    let url = avatars.appendingPathComponent("\(id)/\(m.prefix)_\(variant)_\(state).png")
    let src = CGImageSourceCreateWithURL(url as CFURL, nil)!
    let sheet = CGImageSourceCreateImageAtIndex(src, 0, nil)!
    let w = s.frameSize[0]
    let i = min(index, s.frames - 1)
    return (sheet.cropping(to: CGRect(x: i * w, y: 0, width: w, height: 32))!, s.anchorX)
}

/// Dibuja un sprite con los pies en `baseY` y su centro (anchorX) en `cx`, ampliado sin suavizado.
func draw(_ img: (CGImage, Int), cx: CGFloat, baseY: CGFloat, scale: CGFloat, in ctx: CGContext) {
    let (cg, anchor) = img
    ctx.interpolationQuality = .none
    let w = CGFloat(cg.width) * scale, h = CGFloat(cg.height) * scale
    ctx.draw(cg, in: CGRect(x: cx - CGFloat(anchor) * scale, y: baseY, width: w, height: h))
}

func render(width: Int, height: Int, _ body: (CGContext) -> Void) -> NSBitmapImageRep {
    let rep = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: width, pixelsHigh: height,
                               bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
                               colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0)!
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
    body(NSGraphicsContext.current!.cgContext)
    NSGraphicsContext.restoreGraphicsState()
    return rep
}

func save(_ rep: NSBitmapImageRep, _ name: String) {
    let url = docs.appendingPathComponent(name)
    try! rep.representation(using: .png, properties: [:])!.write(to: url)
    print("✓ \(url.path)")
}

func rgb(_ r: Int, _ g: Int, _ b: Int, _ a: CGFloat = 1) -> NSColor {
    NSColor(srgbRed: CGFloat(r) / 255, green: CGFloat(g) / 255, blue: CGFloat(b) / 255, alpha: a)
}

// MARK: - Bocadillo (mismo estilo que la app: borde de "píxel", esquinas en escalón, pico)

func bubble(_ lines: [(String, Bool, NSColor)], tailX: CGFloat, bottom: CGFloat, left: CGFloat,
            p: CGFloat, in ctx: CGContext) {
    let ink = rgb(18, 18, 30), paper = rgb(255, 253, 242), shade = rgb(0, 0, 0, 0.25)
    let text = NSMutableAttributedString()
    let para = NSMutableParagraphStyle(); para.lineSpacing = 3
    for (i, (s, bold, color)) in lines.enumerated() {
        let font = NSFont.monospacedSystemFont(ofSize: 7 * p, weight: bold ? .bold : .regular)
        text.append(NSAttributedString(string: (i > 0 ? "\n" : "") + s,
                                       attributes: [.font: font, .foregroundColor: color, .paragraphStyle: para]))
    }
    let size = text.boundingRect(with: NSSize(width: 2000, height: 2000), options: .usesLineFragmentOrigin).size
    let pad = 6 * p, tailH = 4 * p
    let w = ceil(size.width) + 2 * pad, h = ceil(size.height) + 2 * pad
    let y0 = bottom + tailH
    func stepped(_ r: CGRect, _ c: NSColor) {
        c.setFill()
        CGRect(x: r.minX + p, y: r.minY, width: r.width - 2 * p, height: r.height).fill()
        CGRect(x: r.minX, y: r.minY + p, width: r.width, height: r.height - 2 * p).fill()
    }
    stepped(CGRect(x: left + p, y: y0 - p, width: w, height: h), shade)
    stepped(CGRect(x: left, y: y0, width: w, height: h), ink)
    stepped(CGRect(x: left + p, y: y0 + p, width: w - 2 * p, height: h - 2 * p), paper)
    for i in 0..<4 {   // pico en escalones
        let y = y0 - CGFloat(i + 1) * p
        let outer = CGFloat(4 - i) * 2 * p
        ink.setFill(); CGRect(x: tailX - outer / 2, y: y, width: outer, height: p).fill()
        let inner = outer - 2 * p
        if inner > 0 { paper.setFill(); CGRect(x: tailX - inner / 2, y: y, width: inner, height: p).fill() }
    }
    paper.setFill(); CGRect(x: tailX - 3 * p, y: y0, width: 6 * p, height: p).fill()
    text.draw(with: CGRect(x: left + pad, y: y0 + pad, width: size.width + 4, height: size.height),
              options: .usesLineFragmentOrigin)
}

// MARK: - Escena de escritorio

func scene(lang: String) -> NSBitmapImageRep {
    let W = 1200, H = 560
    return render(width: W, height: H) { ctx in
        // fondo tipo fondo de pantalla
        let grad = NSGradient(colors: [rgb(255, 196, 160), rgb(198, 150, 226), rgb(92, 112, 210)])!
        grad.draw(in: CGRect(x: 0, y: 0, width: W, height: H), angle: -90)
        // ventanas difuminadas de fondo
        for (x, y, w, h) in [(640, 210, 460, 300), (520, 260, 330, 230)] {
            let r = NSBezierPath(roundedRect: CGRect(x: x, y: y, width: w, height: h), xRadius: 14, yRadius: 14)
            rgb(255, 255, 255, 0.55).setFill(); r.fill()
            rgb(255, 255, 255, 0.75).setFill()
            NSBezierPath(roundedRect: CGRect(x: x, y: y + h - 34, width: w, height: 34), xRadius: 14, yRadius: 14).fill()
            for (i, c) in [rgb(255, 95, 87), rgb(254, 188, 46), rgb(40, 200, 64)].enumerated() {
                c.setFill(); NSBezierPath(ovalIn: CGRect(x: x + 16 + i * 22, y: y + h - 23, width: 12, height: 12)).fill()
            }
        }
        // Dock
        let dock = CGRect(x: 230, y: 14, width: 740, height: 76)
        rgb(255, 255, 255, 0.38).setFill()
        NSBezierPath(roundedRect: dock, xRadius: 22, yRadius: 22).fill()
        let icons = [rgb(80, 140, 250), rgb(250, 90, 90), rgb(90, 200, 120), rgb(250, 180, 60),
                     rgb(160, 110, 240), rgb(60, 190, 220), rgb(240, 120, 180), rgb(120, 130, 150), rgb(30, 30, 40)]
        for (i, c) in icons.enumerated() {
            c.setFill()
            NSBezierPath(roundedRect: CGRect(x: 252 + i * 78, y: 24, width: 58, height: 58), xRadius: 14, yRadius: 14).fill()
        }
        // personajes encima del Dock (franja inferior izquierda)
        let base: CGFloat = 128, s: CGFloat = 3
        let xs: [CGFloat] = [120, 300, 470]
        draw(sprite("astronauta", "azul", "run", 2), cx: xs[0], baseY: base, scale: s, in: ctx)
        draw(sprite("ninja", "rojo", "katana", 4), cx: xs[1], baseY: base, scale: s, in: ctx)
        draw(sprite("perro", "dalmata", "ask", 1), cx: xs[2], baseY: base, scale: s, in: ctx)
        let es = lang == "es"
        let accent = rgb(150, 25, 35), ink = rgb(18, 18, 30)
        bubble([(es ? "AHORA" : "NOW", true, accent),
                (es ? "Reunión de equipo" : "Team meeting", true, ink),
                (es ? "hasta las 12:30" : "until 12:30", false, ink),
                ("", false, ink),
                (es ? "DESPUÉS" : "NEXT", true, accent),
                (es ? "Comida con Ana" : "Lunch with Ana", true, ink),
                (es ? "hoy a las 14:00" : "today at 14:00", false, ink)],
               tailX: xs[1] + 2, bottom: base + 92, left: xs[1] - 110, p: 3, in: ctx)
        // etiquetas centradas debajo de cada personaje, entre los pies y el Dock
        let shadow = NSShadow(); shadow.shadowColor = rgb(0, 0, 0, 0.5); shadow.shadowBlurRadius = 4
        let attrs: [NSAttributedString.Key: Any] = [
            .font: NSFont.systemFont(ofSize: 16, weight: .semibold),
            .foregroundColor: rgb(255, 255, 255), .shadow: shadow]
        let notes = es ? ["Claude trabaja", "Ha terminado", "Te pregunta"]
                       : ["Claude is working", "Done", "Needs you"]
        for (t, x) in zip(notes, xs) {
            let str = NSAttributedString(string: t, attributes: attrs)
            str.draw(at: NSPoint(x: x - str.size().width / 2, y: 100))
        }
    }
}

// MARK: - Galería

func gallery() -> NSBitmapImageRep {
    let order = ["ninja", "astronauta", "programador", "futbolista", "baloncestista", "piloto",
                 "perro", "bombero", "estudiante", "tenista"]
    let s: CGFloat = 2, cell: CGFloat = 78, labelW: CGFloat = 230, rowH: CGFloat = 74
    let W = Int(labelW + 6 * cell + 20), H = Int(CGFloat(order.count) * rowH + 24)
    return render(width: W, height: H) { ctx in
        rgb(240, 243, 248).setFill(); CGRect(x: 0, y: 0, width: W, height: H).fill()
        for (r, id) in order.enumerated() {
            let m = manifest(id)
            let y = CGFloat(H) - 12 - CGFloat(r + 1) * rowH
            if r % 2 == 0 {
                rgb(228, 233, 242).setFill(); CGRect(x: 0, y: y, width: CGFloat(W), height: rowH).fill()
            }
            let en = m.name["en"] ?? id, es = m.name["es"] ?? id
            let title = NSMutableAttributedString(string: en, attributes: [
                .font: NSFont.systemFont(ofSize: 17, weight: .semibold), .foregroundColor: rgb(30, 32, 48)])
            if es != en {
                title.append(NSAttributedString(string: "\n" + es, attributes: [
                    .font: NSFont.systemFont(ofSize: 13), .foregroundColor: rgb(100, 104, 120)]))
            }
            title.draw(with: CGRect(x: 18, y: y + 14, width: labelW - 24, height: rowH - 20),
                       options: .usesLineFragmentOrigin)
            for (c, v) in m.variants.enumerated() {
                draw(sprite(id, v, "idle", 0), cx: labelW + CGFloat(c) * cell + cell / 2,
                     baseY: y + 5, scale: s, in: ctx)
            }
        }
    }
}

// MARK: - Imagen para compartir (GitHub › Settings › Social preview, 1280×640)

func socialPreview() -> NSBitmapImageRep {
    let W = 1280, H = 640
    return render(width: W, height: H) { ctx in
        // cielo nocturno en franjas de "píxel", como el icono
        for y in stride(from: 0, to: H, by: 16) {
            let t = CGFloat(y) / CGFloat(H)
            rgb(Int(52 - 28 * t), Int(58 - 30 * t), Int(120 - 54 * t)).setFill()
            CGRect(x: 0, y: y, width: W, height: 16).fill()
        }
        rgb(235, 240, 255).setFill()
        for (x, y, sz) in [(80, 560, 12), (260, 600, 8), (470, 520, 12), (720, 590, 8), (930, 540, 12),
                           (1160, 600, 8), (1210, 470, 12), (60, 420, 8), (1100, 380, 8), (640, 470, 8)] {
            CGRect(x: x, y: y, width: sz, height: sz).fill()
        }
        // suelo
        rgb(30, 34, 70).setFill(); CGRect(x: 0, y: 0, width: W, height: 120).fill()
        rgb(70, 78, 140).setFill(); CGRect(x: 0, y: 112, width: W, height: 8).fill()
        // título
        let shadow = NSShadow(); shadow.shadowColor = rgb(0, 0, 0, 0.5); shadow.shadowBlurRadius = 6
        let title = NSAttributedString(string: "PixelBuddy", attributes: [
            .font: NSFont.systemFont(ofSize: 96, weight: .heavy), .foregroundColor: rgb(255, 255, 255),
            .shadow: shadow])
        title.draw(at: NSPoint(x: (CGFloat(W) - title.size().width) / 2, y: 430))
        let sub = NSAttributedString(
            string: "Pixel art buddies for your Mac that react to Claude Code",
            attributes: [.font: NSFont.systemFont(ofSize: 34, weight: .medium),
                         .foregroundColor: rgb(214, 222, 255), .shadow: shadow])
        sub.draw(at: NSPoint(x: (CGFloat(W) - sub.size().width) / 2, y: 370))
        // los 10 personajes en fila, de pie sobre el suelo
        let row: [(String, String, String, Int)] = [
            ("ninja", "azul", "katana", 4), ("astronauta", "rojo", "idle", 0),
            ("programador", "verde", "idle", 0), ("futbolista", "morado", "idle", 0),
            ("baloncestista", "naranja", "idle", 0), ("piloto", "amarillo", "idle", 0),
            ("perro", "husky", "idle", 0), ("bombero", "rojo", "idle", 0),
            ("estudiante", "azul", "idle", 0), ("tenista", "verde", "idle", 0)]
        let s: CGFloat = 3.5, step: CGFloat = 118
        let x0 = (CGFloat(W) - step * CGFloat(row.count - 1)) / 2
        for (i, (id, v, st, f)) in row.enumerated() {
            draw(sprite(id, v, st, f), cx: x0 + CGFloat(i) * step, baseY: 96, scale: s, in: ctx)
        }
    }
}

try? FileManager.default.createDirectory(at: docs, withIntermediateDirectories: true)
save(socialPreview(), "social_preview.png")
save(scene(lang: "en"), "scene_en.png")
save(scene(lang: "es"), "scene_es.png")
save(gallery(), "gallery.png")
