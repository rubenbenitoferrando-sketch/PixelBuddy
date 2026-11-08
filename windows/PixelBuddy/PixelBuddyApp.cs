using System.Diagnostics;

namespace PixelBuddy;

/// La app: escenario, vigilante de sesiones e icono junto al reloj.
sealed class PixelBuddyApp : ApplicationContext
{
    public Settings Settings { get; } = Settings.Load();
    public List<SpriteLibrary> Avatars { get; }
    public Stage Stage { get; }
    public GoogleCalendar Calendar { get; }
    readonly SessionWatcher watcher;
    readonly Tray tray;
    readonly System.Windows.Forms.Timer claudeTimer = new() { Interval = 5000 };
    bool claudeWasRunning;

    public PixelBuddyApp()
    {
        Avatars = SpriteLibrary.All();
        var lib = Avatars.FirstOrDefault(a => a.Id == Settings.Avatar) ?? Avatars.FirstOrDefault();
        if (lib == null)
        {
            MessageBox.Show(L.T("alert.reinstall"), L.T("alert.noSprites"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }

        HookInstaller.EnsureHookInstalled();
        Stage = new Stage(lib!, Settings);
        Stage.BuddyClicked = ShowBubble;
        Stage.RemoveClicked = RemoveSession;
        Calendar = new GoogleCalendar(Settings);
        Calendar.Changed += CalendarChanged;
        watcher = new SessionWatcher(Paths.SessionsDir, Stage.Window, Handle);

        // Si Claude no está abierto (p. ej. al encender el PC), no queda ninguna sesión viva.
        claudeWasRunning = ClaudeIsRunning();
        if (!claudeWasRunning) DeleteSessionFiles();
        watcher.Start();
        claudeTimer.Tick += (_, _) => CheckClaude();
        claudeTimer.Start();

        Stage.SetHidden(Settings.Hidden);
        tray = new Tray(this, lib!);

        // La primera vez se activa "Abrir al iniciar Windows" (activado por defecto).
        if (!Settings.LoginConfigured)
        {
            Login.Set(true);
            Settings.LoginConfigured = true;
            Settings.Save();
        }
        else
        {
            Login.Refresh();
        }

        // Primera vez (sin carpeta): se pide la carpeta del proyecto.
        if (Settings.WatchedFolder == null)
            Stage.Window.BeginInvoke(() => ChooseFolder(firstTime: true));
    }

    /// Solo cuentan las sesiones cuyo `cwd` está dentro de la carpeta vigilada.
    void Handle(SessionEvent e)
    {
        if (!WatchedPaths.IsWatched(Settings.WatchedFolder, e.Cwd)) return;
        Stage.Apply(e);
    }

    void ShowBubble(Buddy n)
    {
        Calendar.RefreshIfStale();   // si cambia algo, el bocadillo abierto se actualiza
        ShowBubbleText(n);
    }

    void ShowBubbleText(Buddy n)
    {
        var cal = Calendar;
        (string, string)? notice = cal.State switch
        {
            GoogleCalendar.Status.NotConfigured => (L.T("bubble.noCalTitle"), L.T("bubble.notConfigured")),
            GoogleCalendar.Status.Disconnected => (L.T("bubble.noCalTitle"), L.T("bubble.noCalBody")),
            GoogleCalendar.Status.Connecting => (L.T("bubble.connectingTitle"), L.T("bubble.connectingBody")),
            GoogleCalendar.Status.NeedsReconnect => (L.T("bubble.reconnectTitle"), L.T("bubble.reconnectBody")),
            _ when !cal.HasData => cal.LastFailed
                ? (L.T("bubble.offlineTitle"), L.T("bubble.offlineBody"))
                : (L.T("bubble.loadingTitle"), L.T("bubble.loadingBody")),
            _ => null,
        };
        var (cur, next) = notice == null ? cal.CurrentAndNext() : (null, null);
        var text = Bubble.MakeText(cur, next, notice, n.ColorIndex);
        Stage.ShowBubble(new Bubble(text, removable: !n.IsGuardian, n));
    }

    /// El calendario ha cambiado: si hay un bocadillo abierto, se rehace con los datos nuevos.
    void CalendarChanged()
    {
        if (Stage.BubbleOwner is { } owner) Stage.Window.BeginInvoke(() =>
        {
            if (Stage.BubbleOwner == owner) ShowBubbleText(owner);
        });
    }

    /// Borra su archivo: si esa sesión vuelve a trabajar, el personaje reaparece.
    void RemoveSession(Buddy n)
    {
        try { File.Delete(Path.Combine(Paths.SessionsDir, n.Id + ".json")); } catch { }
        Stage.Dismiss(n);
    }

    // MARK: Claude abierto o cerrado

    /// La app de escritorio de Claude y Claude Code se llaman claude.exe.
    static bool ClaudeIsRunning()
    {
        var ps = Process.GetProcessesByName("claude");
        foreach (var p in ps) p.Dispose();
        return ps.Length > 0;
    }

    /// Al cerrarse todo Claude, sus sesiones mueren: se quitan sus personajes.
    void CheckClaude()
    {
        var running = ClaudeIsRunning();
        if (claudeWasRunning && !running)
        {
            DeleteSessionFiles();
            Stage.RemoveAllSessions();
            watcher.RescanAll();
        }
        claudeWasRunning = running;
    }

    static void DeleteSessionFiles()
    {
        try
        {
            foreach (var f in Directory.GetFiles(Paths.SessionsDir, "*.json"))
                try { File.Delete(f); } catch { }
        }
        catch { }
    }

    // MARK: Acciones del menú

    public void ToggleHidden()
    {
        var hide = !Stage.IsHidden;
        Stage.SetHidden(hide);
        Settings.Hidden = hide;
        Settings.Save();
    }

    public void SelectAvatar(string id)
    {
        var lib = Avatars.FirstOrDefault(a => a.Id == id);
        if (lib == null) return;
        Settings.Avatar = id;
        Settings.Save();
        Stage.SetSprites(lib);
        tray.UpdateIcon(lib);
    }

    public void ChooseFolder(bool firstTime)
    {
        if (firstTime)
            MessageBox.Show(L.T("first.body"), L.T("first.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        while (true)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = L.T("panel.message"),
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false,
            };
            if (Settings.WatchedFolder != null && Directory.Exists(Settings.WatchedFolder))
                dlg.InitialDirectory = Settings.WatchedFolder;
            if (dlg.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(dlg.SelectedPath)) return;
            var folder = WatchedPaths.Normalize(dlg.SelectedPath);
            if (WatchedPaths.IsHome(folder))
            {
                MessageBox.Show(L.T("err.home"), "PixelBuddy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                continue;
            }
            try
            {
                ApplyFolder(folder, connect: true);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, L.T("alert.folderFail"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                continue;
            }
            MessageBox.Show(L.T("alert.restartSessions"), L.T("alert.nowWatching", Path.GetFileName(folder)),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
    }

    /// Vigila `folder` a partir de ahora. Si `connect`, pone los hooks en esa carpeta
    /// (salvo que ya use el plugin) y los quita de la anterior.
    public void ApplyFolder(string folder, bool connect)
    {
        folder = WatchedPaths.Normalize(folder);
        if (connect && !HookInstaller.PluginEnabled(folder)) HookInstaller.Install(folder);
        var old = Settings.WatchedFolder;
        if (old != null && !string.Equals(WatchedPaths.Normalize(old), folder, StringComparison.OrdinalIgnoreCase))
            try { HookInstaller.Uninstall(old); } catch { }
        Settings.WatchedFolder = folder;
        Settings.Save();
        Stage.RemoveAllSessions();
        watcher.RescanAll();
    }

    public void Quit() => ExitThread();

    protected override void ExitThreadCore()
    {
        claudeTimer.Dispose();
        Calendar.Dispose();
        watcher.Dispose();
        tray.Dispose();
        Stage.Dispose();
        base.ExitThreadCore();
    }
}
