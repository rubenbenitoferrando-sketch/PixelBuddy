using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PixelBuddy;

/// Piezas comunes de las ventanas de bienvenida y "Cómo funciona".
static class Ui
{
    public static readonly Color Secondary = SystemColors.GrayText;

    /// Ventana fija, centrada, que crece con su contenido y respeta la escala de la pantalla.
    public static void SetupDialog(Form f)
    {
        f.Text = "PixelBuddy";
        f.AutoScaleDimensions = new SizeF(96F, 96F);
        f.AutoScaleMode = AutoScaleMode.Dpi;
        f.Font = SystemFonts.MessageBoxFont ?? new Font("Segoe UI", 9F);
        f.FormBorderStyle = FormBorderStyle.FixedDialog;
        f.MaximizeBox = false;
        f.MinimizeBox = false;
        f.StartPosition = FormStartPosition.CenterScreen;
        f.AutoSize = true;
        f.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        f.ShowInTaskbar = true;
        try { f.Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? ""); } catch { }
        // En pantallas pequeñas, si no cabe, se vuelve desplazable.
        f.Load += (_, _) =>
        {
            var area = Screen.FromControl(f).WorkingArea;
            if (f.Height <= area.Height * 0.92) return;
            f.AutoSize = false;
            f.AutoScroll = true;
            f.Height = (int)(area.Height * 0.9);
            f.Width += SystemInformation.VerticalScrollBarWidth;
            f.Location = new Point(area.Left + (area.Width - f.Width) / 2, area.Top + (area.Height - f.Height) / 2);
        };
    }

    /// Columna vertical donde se apilan los elementos.
    public static FlowLayoutPanel Column(int width) => new()
    {
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(22, 18, 22, 16),
        MaximumSize = new Size(width + 44, 0),
    };

    public static Label Text(string s, int width, float size = 9F, bool bold = false, bool secondary = false,
                             int top = 0, int bottom = 4)
    {
        var baseFont = SystemFonts.MessageBoxFont ?? new Font("Segoe UI", 9F);
        return new Label
        {
            Text = s,
            AutoSize = true,
            MaximumSize = new Size(width, 0),
            Font = new Font(baseFont.FontFamily, size, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = secondary ? Secondary : SystemColors.ControlText,
            Margin = new Padding(0, top, 0, bottom),
            UseMnemonic = false,
        };
    }

    /// Píxeles de pantalla por píxel de sprite para un tamaño base (×1.5, ×2…), redondeado
    /// a un número entero para que el pixel art se vea nítido.
    public static int PixelScale(Control c, double baseScale) =>
        Math.Max(1, (int)Math.Round(baseScale * c.DeviceDpi / 96.0));

    /// Frame ampliado sin suavizar dentro de una celda de `cellW`×`cellH` píxeles de sprite,
    /// con el centro del personaje (anchorX) en `centerX`, como en pantalla.
    public static Bitmap PixelFrame(Bitmap frame, int scale, int cellW, int cellH, int anchorX, int centerX)
    {
        var bmp = new Bitmap(cellW * scale, cellH * scale, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        using var attrs = new ImageAttributes();
        attrs.SetWrapMode(WrapMode.TileFlipXY);
        var x = (centerX - anchorX) * scale;
        var y = (cellH - frame.Height) * scale;
        g.DrawImage(frame, new Rectangle(x, y, frame.Width * scale, frame.Height * scale),
            0, 0, frame.Width, frame.Height, GraphicsUnit.Pixel, attrs);
        return bmp;
    }

    /// Fila con los controles uno al lado del otro.
    public static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 2, 0, 4),
        };
        row.Controls.AddRange(controls);
        return row;
    }

    public static Button Button(string text, EventHandler onClick)
    {
        var b = new Button { Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                             Padding = new Padding(8, 2, 8, 2), UseMnemonic = false };
        b.Click += onClick;
        return b;
    }
}
