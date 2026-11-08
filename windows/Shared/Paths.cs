namespace PixelBuddy;

/// Carpetas de datos de PixelBuddy en Windows: %LOCALAPPDATA%\PixelBuddy\
/// (la app y el hook usan las mismas rutas).
public static class Paths
{
    public static string SupportDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PixelBuddy");

    public static string SessionsDir { get; } = Path.Combine(SupportDir, "sessions");
    public static string SettingsFile { get; } = Path.Combine(SupportDir, "settings.json");
}
