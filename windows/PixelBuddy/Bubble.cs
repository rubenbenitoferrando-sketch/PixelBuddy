using System.Drawing.Text;

namespace PixelBuddy;

/// Evento de calendario (de Google Calendar), en hora local.
sealed record CalEvent(string Title, DateTime Start, DateTime End);

/// Bocadillo pixel art: borde de 2 px, esquinas en escalón, pico hacia el personaje
/// y fuente monoespaciada. Todas las medidas van en "píxeles de sprite" (p).
sealed class Bubble : IDisposable
{
    public sealed record Line(string Text, bool Bold, Color Color);

    static readonly Color Ink = Color.FromArgb(18, 18, 30);
    static readonly Color Paper = Color.FromArgb(255, 252, 242);
    static readonly Color Shade = Color.FromArgb(64, 0, 0, 0);

    /// Color de la cabecera para cada variante (tonos oscuros legibles).
    static readonly Color[] Accents =
    {
        Color.FromArgb(28, 79, 160),    // azul
        Color.FromArgb(150, 25, 35),    // rojo
        Color.FromArgb(30, 115, 50),    // verde
        Color.FromArgb(95, 40, 150),    // morado
        Color.FromArgb(170, 90, 15),    // naranja
        Color.FromArgb(140, 110, 10),   // amarillo
    };

    readonly List<Line> lines;
    public bool Removable { get; }
    public Buddy Owner { get; }

    // Medidas (en píxeles de pantalla), calculadas en Layout.
    int p, pad, tailH, buttonH, gap;
    Font? body, bold, button;
    float lineH;
    public Size Size { get; private set; }
    /// Botón "✕ Quitar", en coordenadas del bocadillo.
    public Rectangle ButtonRect { get; private set; }

    public Bubble(List<Line> lines, bool removable, Buddy owner)
    {
        this.lines = lines; Removable = removable; Owner = owner;
    }

    public void Layout(int spritePx, Graphics g)
    {
        if (p == spritePx && body != null) return;
        DisposeFonts();
        p = spritePx;
        pad = 5 * p; tailH = 4 * p; buttonH = 10 * p; gap = 4 * p;
        body = new Font("Consolas", 6f * p, FontStyle.Regular, GraphicsUnit.Pixel);
        bold = new Font("Consolas", 6f * p, FontStyle.Bold, GraphicsUnit.Pixel);
        button = new Font("Consolas", 5f * p, FontStyle.Bold, GraphicsUnit.Pixel);
        lineH = bold.GetHeight(g) + p;

        float textW = 0;
        foreach (var l in lines)
            textW = Math.Max(textW, g.MeasureString(l.Text, l.Bold ? bold : body, int.MaxValue,
                StringFormat.GenericTypographic).Width);
        var textH = lineH * lines.Count;
        var w = (int)Math.Ceiling(textW) + 2 * pad + 2 * p;
        var h = (int)Math.Ceiling(textH) + 2 * pad + tailH + p;
        if (Removable)
        {
            var bw = g.MeasureString(L.T("bubble.remove"), button, int.MaxValue,
                StringFormat.GenericTypographic).Width;
            w = Math.Max(w, (int)Math.Ceiling(bw) + 2 * pad + 10 * p);
            h += buttonH + gap;
        }
        w = Math.Max(w, 30 * p);
        Size = new Size(w, h);
        // El botón queda abajo, encima del pico.
        ButtonRect = new Rectangle(pad, h - tailH - pad + p - buttonH, w - p - 2 * pad, buttonH);
    }

    /// Dibuja el bocadillo con su esquina superior izquierda en (left, top) y el pico en `tailX`.
    public void Draw(Graphics g, int left, int top, int tailX)
    {
        if (body == null || bold == null || button == null) return;
        var w = Size.Width - p;            // deja sitio para la sombra
        var bodyH = Size.Height - tailH;

        void Stepped(Rectangle r, Color c)
        {
            using var b = new SolidBrush(c);
            g.FillRectangle(b, r.X + p, r.Y, r.Width - 2 * p, r.Height);
            g.FillRectangle(b, r.X, r.Y + p, r.Width, r.Height - 2 * p);
        }

        // Sombra, borde y relleno del cuerpo.
        Stepped(new Rectangle(left + p, top + p, w, bodyH), Shade);
        Stepped(new Rectangle(left, top, w, bodyH), Ink);
        Stepped(new Rectangle(left + p, top + p, w - 2 * p, bodyH - 2 * p), Paper);

        // Pico en escalones (4 filas de 1 px de sprite), alineado a la rejilla.
        var tx = Math.Clamp(tailX, 6 * p + 2, Size.Width - 6 * p - 2);
        tx = (int)Math.Round(tx / (double)p) * p;
        using (var ink = new SolidBrush(Ink))
        using (var paper = new SolidBrush(Paper))
        {
            for (var i = 0; i < 4; i++)
            {
                var y = top + bodyH + i * p;
                var outer = (4 - i) * 2 * p;
                g.FillRectangle(ink, left + tx - outer / 2, y, outer, p);
                var inner = outer - 2 * p;
                if (inner > 0) g.FillRectangle(paper, left + tx - inner / 2, y, inner, p);
            }
            g.FillRectangle(paper, left + tx - 3 * p, top + bodyH - p, 6 * p, p);
        }

        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        if (Removable)
        {
            var r = ButtonRect;
            r.Offset(left, top);
            Stepped(r, Ink);
            var label = L.T("bubble.remove");
            var ls = g.MeasureString(label, button, int.MaxValue, StringFormat.GenericTypographic);
            using var pb = new SolidBrush(Paper);
            g.DrawString(label, button, pb,
                MathF.Round(r.X + (r.Width - ls.Width) / 2), MathF.Round(r.Y + (r.Height - ls.Height) / 2),
                StringFormat.GenericTypographic);
        }
        float ty = top + pad;
        foreach (var l in lines)
        {
            if (l.Text.Length > 0)
            {
                using var b = new SolidBrush(l.Color);
                g.DrawString(l.Text, l.Bold ? bold : body, b, left + pad + p, MathF.Round(ty),
                    StringFormat.GenericTypographic);
            }
            ty += lineH;
        }
    }

    void DisposeFonts()
    {
        body?.Dispose(); bold?.Dispose(); button?.Dispose();
        body = bold = button = null;
    }

    public void Dispose() => DisposeFonts();

    // MARK: Texto

    /// Texto del bocadillo. Con `notice` (título, texto) se muestra ese aviso en lugar del calendario.
    public static List<Line> MakeText(CalEvent? current, CalEvent? next, (string title, string body)? notice,
                                      int colorIndex)
    {
        var accent = Accents[colorIndex % Accents.Length];
        var outLines = new List<Line>();
        void Add(string s, bool isBold, Color? c = null)
        {
            foreach (var part in s.Split('\n')) outLines.Add(new Line(part, isBold, c ?? Ink));
        }
        if (notice is { } n)
        {
            Add(n.title, true, accent);
            Add(n.body, false);
            return outLines;
        }
        Add(L.T("bubble.now"), true, accent);   // mismo formato con o sin evento
        if (current != null)
        {
            Add(Short(current.Title), true);
            Add(L.T("bubble.until", Hour(current.End)), false);
        }
        else
        {
            Add(L.T("bubble.rest"), true);
        }
        if (next != null)
        {
            Add("", false);
            Add(L.T("bubble.next"), true, accent);
            Add(Short(next.Title), true);
            Add(When(next.Start), false);
        }
        return outLines;
    }

    static string Short(string? s)
    {
        var t = (s ?? "").Trim();
        if (t.Length == 0) return L.T("bubble.untitled");
        return t.Length > 30 ? t[..29] + "…" : t;
    }

    static string Hour(DateTime d) => d.ToString("HH:mm", L.Culture);

    static string When(DateTime d)
    {
        var today = DateTime.Today;
        if (d.Date == today) return L.T("bubble.today", Hour(d));
        if (d.Date == today.AddDays(1)) return L.T("bubble.tomorrow", Hour(d));
        return L.T("bubble.dayAt", d.ToString("dddd d", L.Culture), Hour(d));
    }
}
