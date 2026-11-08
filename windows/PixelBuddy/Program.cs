namespace PixelBuddy;

// Órdenes de línea de comandos:
//   PixelBuddy.exe --install-hooks <carpeta>       vigila esa carpeta (y quita los hooks de la anterior)
//   PixelBuddy.exe --uninstall-hooks <carpeta>     quita solo los hooks de esa carpeta
//   PixelBuddy.exe --uninstall                     quita los hooks y el arranque al iniciar Windows
//   PixelBuddy.exe --debug-paths <carpeta> <cwd>   cómo se clasifica una ruta
//   PixelBuddy.exe --demo                          simula sesiones para ver todos los estados
static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0].StartsWith("--"))
        {
            Native.AttachConsole(Native.ATTACH_PARENT_PROCESS);   // para ver lo que se imprime
            return Cli.Run(args);
        }

        using var mutex = new Mutex(true, @"Local\PixelBuddy", out var first);
        if (!first) return 0;   // ya está abierta

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new PixelBuddyApp());
        return 0;
    }
}

static class Cli
{
    public static int Run(string[] args)
    {
        switch (args[0])
        {
            case "--debug-paths" when args.Length >= 3:
            {
                var (root, cwd) = (args[1], args[2]);
                var mapped = WatchedPaths.WorktreeEquivalent(cwd);
                var watched = WatchedPaths.IsWatched(root, cwd);
                Console.WriteLine($"cuenta={(watched ? "sí" : "no")}  worktree→{mapped ?? "-"}  " +
                                  $"repositorio={WatchedPaths.RepositoryRoot(root) ?? "-"}");
                return 0;
            }
            case "--demo":
                return Demo.Run();
            case "--install-hooks" when args.Length >= 2:
                try
                {
                    var folder = WatchedPaths.Normalize(args[1]);
                    var settings = Settings.Load();
                    if (settings.WatchedFolder is { } old && !string.Equals(
                            WatchedPaths.Normalize(old), folder, StringComparison.OrdinalIgnoreCase))
                        try { HookInstaller.Uninstall(old); } catch { }
                    if (HookInstaller.PluginEnabled(folder))
                    {
                        Console.WriteLine("✓ Esa carpeta ya usa el plugin pixelbuddy: no hacen falta hooks.");
                    }
                    else
                    {
                        HookInstaller.Install(folder);
                        Console.WriteLine($"✓ Hooks añadidos en {HookInstaller.SettingsFile(folder)}");
                    }
                    settings.WatchedFolder = folder;
                    settings.Save();
                    return 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"✗ {e.Message}");
                    return 1;
                }
            case "--uninstall-hooks" when args.Length >= 2:
                try
                {
                    HookInstaller.Uninstall(args[1]);
                    Console.WriteLine($"✓ Hooks quitados de {HookInstaller.SettingsFile(args[1])}");
                    return 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"✗ {e.Message}");
                    return 1;
                }
            case "--uninstall":
                // Quita los hooks y el arranque al iniciar Windows (para desinstalar).
                if (Settings.Load().WatchedFolder is { } watchedFolder)
                    try { HookInstaller.Uninstall(watchedFolder); } catch (Exception e) { Console.WriteLine($"✗ {e.Message}"); }
                Login.Remove();
                Console.WriteLine("✓ PixelBuddy desconectado. Ya puedes borrar su carpeta.");
                return 0;
            default:
                Console.WriteLine("Uso: PixelBuddy.exe --install-hooks <carpeta> | --uninstall-hooks <carpeta> | " +
                                  "--uninstall | --debug-paths <carpeta> <cwd> | --demo");
                return 2;
        }
    }
}

/// Simula 2 sesiones dentro de la carpeta vigilada y 1 fuera (que debe ignorarse),
/// pasando por todos los estados. Dura ~1 min. PixelBuddy tiene que estar abierta.
static class Demo
{
    public static int Run()
    {
        var watched = Settings.Load().WatchedFolder;
        if (watched == null)
        {
            Console.WriteLine("No hay carpeta vigilada: abre PixelBuddy y elige una.");
            return 1;
        }
        string a = "demo-dentro-1", b = "demo-dentro-2", c = "demo-fuera";
        var cwdA = watched;
        var cwdB = Path.Combine(watched, "subcarpeta");
        var cwdC = Path.Combine(Path.GetTempPath(), "fuera-del-proyecto");

        void Send(string sid, string cwd, string ev, string detail = "") =>
            SessionFile.Write(sid, cwd, ev, detail, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        void Step(string title, string text) => Console.WriteLine($"\n▶ {title}\n   {text}");

        Step("1/6 Empiezan 3 sesiones", "Verás 2 personajes corriendo (azul y rojo). La de fuera NO debe aparecer.");
        foreach (var (s, cwd) in new[] { (a, cwdA), (b, cwdB), (c, cwdC) }) Send(s, cwd, "UserPromptSubmit");
        Thread.Sleep(8000);

        Step("2/6 El azul termina y el rojo te hace una pregunta", "Azul: vuelve a su sitio y celebra. Rojo: interrogación.");
        Send(a, cwdA, "Stop");
        Send(b, cwdB, "PreToolUse", "AskUserQuestion");
        Send(c, cwdC, "Stop");
        Thread.Sleep(6000);

        Step("3/6 Le respondes y el rojo vuelve a trabajar", "Rojo: corre otra vez.");
        Send(b, cwdB, "PostToolUse", "AskUserQuestion");
        Thread.Sleep(5000);

        Step("4/6 El rojo te pide permiso", "Rojo: interrogación.");
        Send(b, cwdB, "PermissionRequest", "Bash");
        Thread.Sleep(5000);

        Step("5/6 El rojo se para por un error", "Rojo: exclamación roja.");
        Send(b, cwdB, "StopFailure");
        Thread.Sleep(6000);

        Step("6/6 Las sesiones terminan", "Desaparecen y vuelve el personaje principal.");
        foreach (var (s, cwd) in new[] { (a, cwdA), (b, cwdB), (c, cwdC) }) Send(s, cwd, "SessionEnd");
        Console.WriteLine("\n✓ Fin de la demostración.");
        return 0;
    }
}
