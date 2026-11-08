using System.Diagnostics;

namespace PixelBuddy;

/// Ventana "Cómo funciona": cada estado con su animación real (del personaje elegido)
/// y qué significa, más unos consejos. Se abre al terminar la bienvenida y desde el menú.
sealed class GuideForm : Form
{
    const int W = 470;

    static readonly (string state, string key)[] States =
    {
        ("run", "guide.run"), ("katana", "guide.katana"), ("ask", "guide.ask"),
        ("alert", "guide.alert"), ("train", "guide.train"), ("sleep", "guide.sleep"),
    };

    readonly List<(PictureBox box, Bitmap[] frames, AnimInfo info)> rows = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 / 12 };
    readonly Stopwatch clock = Stopwatch.StartNew();

    public GuideForm(SpriteLibrary lib)
    {
        SuspendLayout();
        Ui.SetupDialog(this);
        var col = Ui.Column(W);
        col.Controls.Add(Ui.Text(L.T("guide.title"), W, 15F, bold: true));
        col.Controls.Add(Ui.Text(L.T("guide.intro"), W, secondary: true, bottom: 12));

        var scale = Ui.PixelScale(this, 1.5);
        var variant = lib.Variants[0];
        const int cellW = 56, cellH = 32;
        var textW = W - cellW * 3 / 2 - 16;
        foreach (var (state, key) in States)
        {
            if (!lib.Anims.TryGetValue(state, out var info)) continue;
            var frames = lib.Frames(variant, state)
                .Select(f => Ui.PixelFrame(f, scale, cellW, cellH, info.AnchorX, 32)).ToArray();
            var box = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.AutoSize,
                Image = frames.FirstOrDefault(),
                Margin = new Padding(0, 0, 12, 0),
            };
            rows.Add((box, frames, info));
            var text = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 4, 0, 0),
            };
            text.Controls.Add(Ui.Text(L.T(key + ".t"), textW, 10F, bold: true, bottom: 0));
            text.Controls.Add(Ui.Text(L.T(key + ".d"), textW, secondary: true, bottom: 0));
            var row = Ui.Row(box, text);
            row.Margin = new Padding(0, 0, 0, 10);
            col.Controls.Add(row);
        }

        col.Controls.Add(Ui.Text(L.T("guide.more"), W, 10F, bold: true, top: 6));
        for (var k = 1; k <= 5; k++)
            col.Controls.Add(Ui.Text("•  " + L.T($"guide.tip{k}"), W, bottom: 6));

        var ok = Ui.Button(L.T("guide.ok"), (_, _) => Close());
        ok.Margin = new Padding(W - 100, 12, 0, 0);
        col.Controls.Add(ok);
        AcceptButton = ok;
        CancelButton = ok;

        Controls.Add(col);
        ResumeLayout(true);

        timer.Tick += (_, _) => Animate();
        timer.Start();
        FormClosed += (_, _) =>
        {
            timer.Dispose();
            foreach (var (_, frames, _) in rows) foreach (var f in frames) f.Dispose();
        };
    }

    void Animate()
    {
        var elapsed = clock.Elapsed.TotalSeconds;
        foreach (var (box, frames, info) in rows)
        {
            if (frames.Length == 0) continue;
            var img = frames[Math.Min(info.FrameIndex(elapsed), frames.Length - 1)];
            if (!ReferenceEquals(box.Image, img)) box.Image = img;
        }
    }
}
