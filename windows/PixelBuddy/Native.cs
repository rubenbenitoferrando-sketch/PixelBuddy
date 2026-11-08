using System.Runtime.InteropServices;

namespace PixelBuddy;

/// Funciones de Windows que .NET no trae de serie.
static class Native
{
    public const int WS_EX_TOPMOST = 0x8;
    public const int WS_EX_TOOLWINDOW = 0x80;
    public const int WS_EX_LAYERED = 0x80000;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WM_MOUSEACTIVATE = 0x21;
    public const int MA_NOACTIVATE = 3;
    public const int ULW_ALPHA = 2;
    public const int VK_LBUTTON = 0x01, VK_RBUTTON = 0x02;
    public const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOACTIVATE = 0x10;
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public const int ATTACH_PARENT_PROCESS = -1;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; public POINT(int x, int y) { X = x; Y = y; } }

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE { public int cx, cy; public SIZE(int w, int h) { cx = w; cy = h; } }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst,
        ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

    [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr hIcon);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(POINT pt, uint flags);
    [DllImport("shcore.dll")] public static extern int GetDpiForMonitor(IntPtr hmon, int type, out uint x, out uint y);

    /// 2 = app a pantalla completa, 3 = juego/vídeo a pantalla completa, 4 = presentación.
    [DllImport("shell32.dll")] public static extern int SHQueryUserNotificationState(out int state);

    [DllImport("kernel32.dll")] public static extern bool AttachConsole(int pid);

    // MARK: DPAPI (cifrado ligado a tu usuario de Windows)

    [StructLayout(LayoutKind.Sequential)]
    struct DATA_BLOB { public int cbData; public IntPtr pbData; }

    const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CryptProtectData(ref DATA_BLOB dataIn, string? description, ref DATA_BLOB entropy,
        IntPtr reserved, IntPtr prompt, int flags, out DATA_BLOB dataOut);

    [DllImport("crypt32.dll", SetLastError = true)]
    static extern bool CryptUnprotectData(ref DATA_BLOB dataIn, IntPtr description, ref DATA_BLOB entropy,
        IntPtr reserved, IntPtr prompt, int flags, out DATA_BLOB dataOut);

    [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr mem);

    public static byte[]? Protect(byte[] data, byte[] entropy) => Crypt(data, entropy, protect: true);
    public static byte[]? Unprotect(byte[] data, byte[] entropy) => Crypt(data, entropy, protect: false);

    static byte[]? Crypt(byte[] data, byte[] entropy, bool protect)
    {
        var hIn = GCHandle.Alloc(data, GCHandleType.Pinned);
        var hEnt = GCHandle.Alloc(entropy, GCHandleType.Pinned);
        try
        {
            var input = new DATA_BLOB { cbData = data.Length, pbData = hIn.AddrOfPinnedObject() };
            var ent = new DATA_BLOB { cbData = entropy.Length, pbData = hEnt.AddrOfPinnedObject() };
            var ok = protect
                ? CryptProtectData(ref input, "PixelBuddy", ref ent, IntPtr.Zero, IntPtr.Zero,
                    CRYPTPROTECT_UI_FORBIDDEN, out var output)
                : CryptUnprotectData(ref input, IntPtr.Zero, ref ent, IntPtr.Zero, IntPtr.Zero,
                    CRYPTPROTECT_UI_FORBIDDEN, out output);
            if (!ok) return null;
            try
            {
                var result = new byte[output.cbData];
                Marshal.Copy(output.pbData, result, 0, output.cbData);
                return result;
            }
            finally
            {
                LocalFree(output.pbData);
            }
        }
        finally
        {
            hIn.Free();
            hEnt.Free();
        }
    }

    /// Escala de la pantalla principal (1 = 100 %, 1.5 = 150 %…).
    public static double PrimaryScale()
    {
        try
        {
            var b = Screen.PrimaryScreen!.Bounds;
            var mon = MonitorFromPoint(new POINT(b.Left + b.Width / 2, b.Top + b.Height / 2), 1);
            if (GetDpiForMonitor(mon, 0, out var dpi, out _) == 0 && dpi > 0) return dpi / 96.0;
        }
        catch { }
        return 1;
    }

    /// true si hay algo a pantalla completa (un vídeo, un juego, una presentación).
    public static bool FullscreenAppRunning()
    {
        try { return SHQueryUserNotificationState(out var s) == 0 && s is 2 or 3 or 4; }
        catch { return false; }
    }
}
