import AppKit
import ImageIO

/// Metadatos de una animación (de `manifest.json`).
struct AnimInfo {
    let frames: Int
    let fps: Double
    let loopFrom: Int
    let frameW: Int
    let frameH: Int
    let anchorX: Int

    /// Frame a mostrar tras `elapsed` segundos: se reproduce entera una vez
    /// y después repite en bucle desde `loopFrom`.
    func frameIndex(elapsed: Double) -> Int {
        let k = max(0, Int(elapsed * fps))
        if k < frames { return k }
        let loopLen = max(1, frames - loopFrom)
        return loopFrom + (k - frames) % loopLen
    }
}

/// Un personaje (ninja, astronauta…): carga sus tiras de sprites y las corta en frames.
/// Cada personaje vive en `avatars/<id>/` con `manifest.json` y `<prefijo>_<color>_<estado>.png`.
final class SpriteLibrary {
    let dir: URL
    let id: String
    let prefix: String
    private let names: [String: String]
    let variants: [String]
    let anims: [String: AnimInfo]
    private var cache: [String: [CGImage]] = [:]

    init?(dir: URL) {
        guard let data = try? Data(contentsOf: dir.appendingPathComponent("manifest.json")),
              let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
              let variants = json["variants"] as? [String],
              let states = json["states"] as? [String: [String: Any]] else { return nil }
        let defSize = (json["frameSize"] as? [Int]) ?? [32, 32]
        var anims: [String: AnimInfo] = [:]
        for (name, s) in states {
            let size = (s["frameSize"] as? [Int]) ?? defSize
            anims[name] = AnimInfo(
                frames: (s["frames"] as? Int) ?? 1,
                fps: (s["fps"] as? NSNumber)?.doubleValue ?? 4,
                loopFrom: (s["loopFrom"] as? Int) ?? 0,
                frameW: size[0], frameH: size[1],
                anchorX: (s["anchorX"] as? Int) ?? size[0] / 2)
        }
        self.dir = dir
        self.id = (json["id"] as? String) ?? dir.lastPathComponent
        self.prefix = (json["prefix"] as? String) ?? dir.lastPathComponent
        self.names = (json["name"] as? [String: String]) ?? [:]
        self.variants = variants
        self.anims = anims
    }

    /// Nombre para mostrar en el idioma del sistema.
    var displayName: String {
        let lang = Locale.current.language.languageCode?.identifier ?? "es"
        return names[lang] ?? names["en"] ?? names["es"] ?? id.capitalized
    }

    /// Carpeta con todos los personajes: dentro del .app o, en desarrollo, `assets/avatars`.
    static func avatarsRoot() -> URL? {
        let fm = FileManager.default
        if let u = Bundle.main.resourceURL?.appendingPathComponent("avatars"),
           fm.fileExists(atPath: u.path) { return u }
        var d = URL(fileURLWithPath: CommandLine.arguments[0]).resolvingSymlinksInPath()
            .deletingLastPathComponent()
        for _ in 0..<8 {
            let c = d.appendingPathComponent("assets/avatars")
            if fm.fileExists(atPath: c.path) { return c }
            d = d.deletingLastPathComponent()
        }
        return nil
    }

    /// Todos los personajes instalados (el ninja primero).
    static func all() -> [SpriteLibrary] {
        guard let root = avatarsRoot(),
              let dirs = try? FileManager.default.contentsOfDirectory(
                at: root, includingPropertiesForKeys: nil, options: [.skipsHiddenFiles]) else { return [] }
        return dirs.compactMap { SpriteLibrary(dir: $0) }
            .sorted { ($0.id == "ninja" ? 0 : 1, $0.displayName) < ($1.id == "ninja" ? 0 : 1, $1.displayName) }
    }

    func frames(variant: String, state: String) -> [CGImage] {
        let key = "\(variant)_\(state)"
        if let f = cache[key] { return f }
        let url = dir.appendingPathComponent("\(prefix)_\(key).png")
        guard let info = anims[state],
              let src = CGImageSourceCreateWithURL(url as CFURL, nil),
              let img = CGImageSourceCreateImageAtIndex(src, 0, nil) else { return [] }
        var out: [CGImage] = []
        for i in 0..<info.frames {
            let r = CGRect(x: i * info.frameW, y: 0, width: info.frameW, height: info.frameH)
            if let f = img.cropping(to: r) { out.append(f) }
        }
        cache[key] = out
        return out
    }
}
