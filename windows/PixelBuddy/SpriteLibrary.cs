using System.Drawing.Imaging;
using System.Text.Json;

namespace PixelBuddy;

/// Metadatos de una animación (de `manifest.json`).
sealed record AnimInfo(int Frames, double Fps, int LoopFrom, int FrameW, int FrameH, int AnchorX)
{
    /// Frame a mostrar tras `elapsed` segundos: se reproduce entera una vez
    /// y después repite en bucle desde `LoopFrom`.
    public int FrameIndex(double elapsed)
    {
        var k = Math.Max(0, (int)(elapsed * Fps));
        if (k < Frames) return k;
        var loopLen = Math.Max(1, Frames - LoopFrom);
        return LoopFrom + (k - Frames) % loopLen;
    }
}

/// Un personaje (ninja, astronauta…): carga sus tiras de sprites y las corta en frames.
/// Cada personaje vive en `avatars\<id>\` con `manifest.json` y `<prefijo>_<variante>_<estado>.png`.
sealed class SpriteLibrary
{
    public string Dir { get; }
    public string Id { get; }
    public string Prefix { get; }
    public IReadOnlyList<string> Variants { get; }
    public IReadOnlyDictionary<string, AnimInfo> Anims { get; }
    readonly Dictionary<string, string> names;
    readonly Dictionary<string, Bitmap[]> cache = new();

    SpriteLibrary(string dir, string id, string prefix, Dictionary<string, string> names,
                  List<string> variants, Dictionary<string, AnimInfo> anims)
    {
        Dir = dir; Id = id; Prefix = prefix; this.names = names; Variants = variants; Anims = anims;
    }

    public static SpriteLibrary? Load(string dir)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "manifest.json")));
            var root = doc.RootElement;
            var variants = root.GetProperty("variants").EnumerateArray().Select(v => v.GetString()!).ToList();
            if (variants.Count == 0) return null;
            var defSize = root.TryGetProperty("frameSize", out var fs) ? Size2(fs) : (w: 32, h: 32);
            var anims = new Dictionary<string, AnimInfo>();
            foreach (var s in root.GetProperty("states").EnumerateObject())
            {
                var v = s.Value;
                var size = v.TryGetProperty("frameSize", out var f) ? Size2(f) : defSize;
                anims[s.Name] = new AnimInfo(
                    v.TryGetProperty("frames", out var fr) ? fr.GetInt32() : 1,
                    v.TryGetProperty("fps", out var fps) ? fps.GetDouble() : 4,
                    v.TryGetProperty("loopFrom", out var lf) ? lf.GetInt32() : 0,
                    size.w, size.h,
                    v.TryGetProperty("anchorX", out var ax) ? ax.GetInt32() : size.w / 2);
            }
            var names = new Dictionary<string, string>();
            if (root.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.Object)
                foreach (var p in n.EnumerateObject()) names[p.Name] = p.Value.GetString() ?? "";
            var folder = Path.GetFileName(dir);
            return new SpriteLibrary(dir,
                root.TryGetProperty("id", out var id) ? id.GetString() ?? folder : folder,
                root.TryGetProperty("prefix", out var pre) ? pre.GetString() ?? folder : folder,
                names, variants, anims);
        }
        catch { return null; }

        static (int w, int h) Size2(JsonElement e) => (e[0].GetInt32(), e[1].GetInt32());
    }

    /// Nombre para mostrar en el idioma de Windows.
    public string DisplayName =>
        names.GetValueOrDefault(L.IsSpanish ? "es" : "en")
        ?? names.GetValueOrDefault("en") ?? names.GetValueOrDefault("es") ?? Id;

    /// Carpeta con todos los personajes: junto al .exe o, en desarrollo, `assets\avatars`.
    public static string? AvatarsRoot()
    {
        var here = Path.Combine(AppContext.BaseDirectory, "avatars");
        if (Directory.Exists(here)) return here;
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && d != null; i++, d = d.Parent)
        {
            var c = Path.Combine(d.FullName, "assets", "avatars");
            if (Directory.Exists(c)) return c;
        }
        return null;
    }

    /// Todos los personajes instalados (el ninja primero).
    public static List<SpriteLibrary> All()
    {
        var root = AvatarsRoot();
        if (root == null) return new();
        return Directory.GetDirectories(root)
            .Select(Load).OfType<SpriteLibrary>()
            .OrderBy(l => l.Id == "ninja" ? 0 : 1)
            .ThenBy(l => l.DisplayName, StringComparer.Create(L.Culture, true))
            .ToList();
    }

    public Bitmap[] Frames(string variant, string state)
    {
        var key = $"{variant}_{state}";
        if (cache.TryGetValue(key, out var f)) return f;
        var result = Array.Empty<Bitmap>();
        var file = Path.Combine(Dir, $"{Prefix}_{key}.png");
        if (Anims.TryGetValue(state, out var info) && File.Exists(file))
        {
            try
            {
                using var strip = new Bitmap(file);
                var list = new List<Bitmap>();
                for (var i = 0; i < info.Frames; i++)
                {
                    var r = new Rectangle(i * info.FrameW, 0, info.FrameW, info.FrameH);
                    if (r.Right > strip.Width || r.Bottom > strip.Height) break;
                    list.Add(strip.Clone(r, PixelFormat.Format32bppArgb));
                }
                result = list.ToArray();
            }
            catch { }
        }
        cache[key] = result;
        return result;
    }

    /// Primer frame de `idle` de la primera variante, ampliado sin suavizar (para iconos).
    public Bitmap? IconFrame(int size)
    {
        var frames = Frames(Variants[0], "idle");
        if (frames.Length == 0) return null;
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        g.DrawImage(frames[0], new Rectangle(0, 0, size, size));
        return bmp;
    }
}
