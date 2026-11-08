using System.Reflection;
using Microsoft.Win32;

namespace PixelBuddy;

/// Icono del personaje junto al reloj (área de notificación) y su menú.
sealed class Tray : IDisposable
{
    readonly PixelBuddyApp app;
    readonly NotifyIcon icon = new() { Text = "PixelBuddy" };
    readonly ContextMenuStrip menu = new();
    Icon? current;
    readonly Dictionary<string, Bitmap?> menuImages = new();

    public Tray(PixelBuddyApp app, SpriteLibrary sprites)
    {
        this.app = app;
        icon.ContextMenuStrip = menu;
        menu.Opening += (_, e) => { Build(); e.Cancel = false; };
        // Con un clic normal también se abre el menú (como en la barra de menús del Mac).
        icon.MouseClick += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(icon, null);
        };
        UpdateIcon(sprites);
        Build();
        icon.Visible = true;
    }

    /// El icono es el personaje elegido, en idle.
    public void UpdateIcon(SpriteLibrary sprites)
    {
        using var bmp = sprites.IconFrame(32);
        if (bmp == null) return;
        var h = bmp.GetHicon();
        var next = (Icon)Icon.FromHandle(h).Clone();
        Native.DestroyIcon(h);
        icon.Icon = next;
        current?.Dispose();
        current = next;
    }

    void Build()
    {
        menu.Items.Clear();
        var stage = app.Stage;
        Add(menu.Items, stage.IsHidden ? L.T("menu.show") : L.T("menu.hide"), app.ToggleHidden);
        menu.Items.Add(new ToolStripSeparator());

        var avatar = new ToolStripMenuItem(L.T("menu.avatar"));
        foreach (var lib in app.Avatars)
        {
            var id = lib.Id;
            var i = Add(avatar.DropDownItems, lib.DisplayName, () => app.SelectAvatar(id));
            i.Checked = lib.Id == stage.Sprites.Id;
            if (!menuImages.TryGetValue(id, out var img)) menuImages[id] = img = lib.IconFrame(16);
            i.Image = img;
        }
        menu.Items.Add(avatar);

        var cals = new ToolStripMenuItem(L.T("menu.calendars"));
        BuildCalendars(cals.DropDownItems);
        menu.Items.Add(cals);
        menu.Items.Add(new ToolStripSeparator());

        var folder = app.Settings.WatchedFolder;
        menu.Items.Add(new ToolStripMenuItem(
            L.T("menu.watching", folder != null ? Path.GetFileName(folder) : L.T("menu.noFolder")))
        {
            Enabled = false,
            ToolTipText = folder,
        });
        Add(menu.Items, L.T("menu.changeFolder"), app.ChooseFolder);
        Add(menu.Items, L.T("menu.setup"), app.ShowWelcome);
        Add(menu.Items, L.T("menu.guide"), app.ShowGuide);
        menu.Items.Add(new ToolStripSeparator());

        var login = Add(menu.Items, L.T("menu.login"), () => Login.Set(!Login.Enabled));
        login.Checked = Login.Enabled;
        Add(menu.Items, L.T("menu.uninstall"), app.Uninstall);
        menu.Items.Add(new ToolStripSeparator());
        Add(menu.Items, L.T("menu.quit"), app.Quit);
    }

    void BuildCalendars(ToolStripItemCollection items)
    {
        var cal = app.Calendar;
        static ToolStripMenuItem Info(string text) => new(text) { Enabled = false };
        switch (cal.State)
        {
            case GoogleCalendar.Status.NotConfigured:
                items.Add(Info(L.T("menu.calNotConfigured")));
                return;
            case GoogleCalendar.Status.Connecting:
                items.Add(Info(L.T("menu.calConnecting")));
                Add(items, L.T("menu.calCancel"), cal.CancelConnect);
                return;
            case GoogleCalendar.Status.Disconnected:
            case GoogleCalendar.Status.NeedsReconnect:
                if (cal.State == GoogleCalendar.Status.NeedsReconnect) items.Add(Info(L.T("menu.calExpired")));
                Add(items, L.T("menu.calConnect"), () => _ = cal.ConnectAsync());
                return;
        }
        items.Add(Info(cal.Account != null ? L.T("menu.calAccount", cal.Account) : "Google Calendar"));
        if (cal.Calendars.Count == 0)
            items.Add(Info(cal.LastFailed ? L.T("menu.calOffline") : L.T("menu.calLoading")));
        foreach (var c in cal.Calendars)
        {
            var id = c.Id;
            var i = Add(items, c.Name, () => cal.Toggle(id));
            i.Checked = cal.IsEnabled(id);
        }
        items.Add(new ToolStripSeparator());
        Add(items, L.T("menu.calRefresh"), () => _ = cal.RefreshAsync(force: true));
        Add(items, L.T("menu.calDisconnect"), () =>
        {
            var ok = MessageBox.Show(L.T("menu.calDisconnectAsk"), "PixelBuddy",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (ok == DialogResult.OK) cal.Disconnect();
        });
    }

    static ToolStripMenuItem Add(ToolStripItemCollection items, string text, Action action)
    {
        var i = new ToolStripMenuItem(text);
        i.Click += (_, _) => action();
        items.Add(i);
        return i;
    }

    public void Dispose()
    {
        icon.Visible = false;
        icon.Dispose();
        menu.Dispose();
        current?.Dispose();
    }
}

/// "Abrir al iniciar Windows": una entrada en HKCU\…\Run (solo para tu usuario).
static class Login
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Name = "PixelBuddy";

    static string Command => $"\"{Environment.ProcessPath}\"";

    public static bool Enabled
    {
        get
        {
            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(RunKey);
                return k?.GetValue(Name) is string;
            }
            catch { return false; }
        }
    }

    public static void Set(bool on)
    {
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(RunKey);
            if (on) k.SetValue(Name, Command); else k.DeleteValue(Name, false);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, L.T("alert.loginFail"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public static void Remove()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            k?.DeleteValue(Name, false);
        }
        catch { }
    }

    /// Si la app se ha movido de carpeta, actualiza la ruta guardada.
    public static void Refresh()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (k?.GetValue(Name) is string v && v != Command) k.SetValue(Name, Command);
        }
        catch { }
    }
}
