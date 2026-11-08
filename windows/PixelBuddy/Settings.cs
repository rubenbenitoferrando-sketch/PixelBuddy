using System.Text.Json;

namespace PixelBuddy;

/// Ajustes de PixelBuddy, guardados en %LOCALAPPDATA%\PixelBuddy\settings.json.
sealed class Settings
{
    /// Carpeta de proyecto vigilada. Sin carpeta, se ignoran todos los eventos.
    public string? WatchedFolder { get; set; }
    public bool Hidden { get; set; }
    public string? Avatar { get; set; }
    /// Última actividad de Claude (epoch), para que el principal no se despierte al reiniciar.
    public double LastActivity { get; set; }
    public bool LoginConfigured { get; set; }
    /// Calendarios de Google que no se muestran (por su id).
    public List<string> ExcludedCalendars { get; set; } = new();
    /// false hasta la primera lectura tras conectar Google (entonces se copian los que tienes marcados).
    public bool CalendarsInitialized { get; set; }

    static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static Settings Load()
    {
        try
        {
            if (File.Exists(Paths.SettingsFile))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(Paths.SettingsFile)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Paths.SupportDir);
            var tmp = Paths.SettingsFile + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(this, Options));
            File.Move(tmp, Paths.SettingsFile, overwrite: true);
        }
        catch { }
    }
}
