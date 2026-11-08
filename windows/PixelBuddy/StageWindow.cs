using System.Drawing.Imaging;

namespace PixelBuddy;

/// Ventana transparente, sin bordes y siempre encima, que no roba el foco ni sale en
/// la barra de tareas. Se dibuja con transparencia por píxel (UpdateLayeredWindow): los
/// clics atraviesan las zonas vacías y solo se reciben encima de un personaje o del bocadillo.
sealed class StageWindow : Form
{
    public StageWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "PixelBuddy";
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW
                          | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOPMOST;
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override void WndProc(ref Message m)
    {
        // Un clic no activa la ventana (no le quita el foco a lo que estés usando).
        if (m.Msg == Native.WM_MOUSEACTIVATE)
        {
            m.Result = Native.MA_NOACTIVATE;
            return;
        }
        base.WndProc(ref m);
    }

    /// Vuelve a ponerla por encima de todo (otras ventanas "siempre encima" pueden taparla).
    public void KeepOnTop() =>
        Native.SetWindowPos(Handle, Native.HWND_TOPMOST, 0, 0, 0, 0,
            Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);

    /// Muestra `bmp` (32 bits con transparencia) en la posición de pantalla `x`, `y`.
    public void Present(Bitmap bmp, int x, int y)
    {
        if (bmp.PixelFormat != PixelFormat.Format32bppArgb) return;
        var screenDc = Native.GetDC(IntPtr.Zero);
        var memDc = Native.CreateCompatibleDC(screenDc);
        var hBitmap = IntPtr.Zero;
        var old = IntPtr.Zero;
        try
        {
            hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
            old = Native.SelectObject(memDc, hBitmap);
            var size = new Native.SIZE(bmp.Width, bmp.Height);
            var src = new Native.POINT(0, 0);
            var pos = new Native.POINT(x, y);
            var blend = new Native.BLENDFUNCTION
            {
                BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = 1,
            };
            Native.UpdateLayeredWindow(Handle, screenDc, ref pos, ref size, memDc, ref src, 0,
                ref blend, Native.ULW_ALPHA);
        }
        finally
        {
            if (old != IntPtr.Zero) Native.SelectObject(memDc, old);
            if (hBitmap != IntPtr.Zero) Native.DeleteObject(hBitmap);
            Native.DeleteDC(memDc);
            Native.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }
}
