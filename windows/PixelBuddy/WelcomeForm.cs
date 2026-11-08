namespace PixelBuddy;

/// Ventana de bienvenida: personaje, carpeta de proyecto (+ conectar Claude Code) y
/// Google Calendar. Se abre sola la primera vez y después desde el menú "Configurar PixelBuddy…".
sealed class WelcomeForm : Form
{
    const int W = 520;   // ancho del contenido (a escala 100 %)

    readonly PixelBuddyApp app;
    string? folder;
    readonly List<RadioButton> avatarButtons = new();
    readonly Label folderLabel;
    readonly CheckBox connectCheck;
    readonly Button calButton;
    readonly Label calStatus;
    readonly Label errorLabel;

    public WelcomeForm(PixelBuddyApp app)
    {
        this.app = app;
        folder = app.Settings.WatchedFolder;
        SuspendLayout();
        Ui.SetupDialog(this);
        var col = Ui.Column(W);

        col.Controls.Add(Ui.Text(L.T("welcome.title"), W, 15F, bold: true));
        col.Controls.Add(Ui.Text(L.T("welcome.intro"), W, secondary: true, bottom: 14));

        // 1. Personaje (5 por fila)
        col.Controls.Add(Ui.Text(L.T("welcome.step1"), W, 10F, bold: true));
        var grid = new FlowLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true,
            MaximumSize = new Size(W, 0), Margin = new Padding(0, 2, 0, 12),
        };
        var scale = Ui.PixelScale(this, 1.5);
        foreach (var lib in app.Avatars)
        {
            var b = new RadioButton
            {
                Appearance = Appearance.Button,
                Text = lib.DisplayName,
                Tag = lib.Id,
                TextImageRelation = TextImageRelation.ImageAboveText,
                ImageAlign = ContentAlignment.BottomCenter,
                TextAlign = ContentAlignment.TopCenter,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(W / 5 - 6, 86),
                Margin = new Padding(0, 0, 6, 6),
                UseMnemonic = false,
            };
            b.FlatAppearance.BorderColor = SystemColors.ControlLight;
            b.FlatAppearance.CheckedBackColor = Color.FromArgb(214, 228, 250);
            var frames = lib.Frames(lib.Variants[0], "idle");
            if (frames.Length > 0 && lib.Anims.TryGetValue("idle", out var info))
                b.Image = Ui.PixelFrame(frames[0], scale, 32, 32, info.AnchorX, 16);
            b.CheckedChanged += (_, _) =>
            {
                if (b.Checked && b.Tag is string id && id != app.Stage.Sprites.Id) app.SelectAvatar(id);
            };
            avatarButtons.Add(b);
            grid.Controls.Add(b);
        }
        col.Controls.Add(grid);

        // 2. Carpeta + conectar Claude Code
        col.Controls.Add(Ui.Text(L.T("welcome.step2"), W, 10F, bold: true));
        col.Controls.Add(Ui.Text(L.T("welcome.step2.help"), W, secondary: true));
        folderLabel = new Label { AutoSize = true, AutoEllipsis = true, MaximumSize = new Size(W - 160, 0),
                                  ForeColor = Ui.Secondary, Margin = new Padding(8, 7, 0, 0), UseMnemonic = false };
        col.Controls.Add(Ui.Row(Ui.Button(L.T("welcome.chooseFolder"), (_, _) => ChooseFolder()), folderLabel));
        connectCheck = new CheckBox { Text = L.T("welcome.connect"), AutoSize = true, Checked = true,
                                      Margin = new Padding(0, 4, 0, 0), UseMnemonic = false };
        col.Controls.Add(connectCheck);
        col.Controls.Add(Ui.Text(L.T("welcome.connect.help"), W, 8F, secondary: true, bottom: 14));

        // 3. Google Calendar
        col.Controls.Add(Ui.Text(L.T("welcome.step3"), W, 10F, bold: true));
        col.Controls.Add(Ui.Text(L.T("welcome.step3.help"), W, secondary: true));
        calButton = Ui.Button(L.T("welcome.grant"), (_, _) => ConnectCalendar());
        calStatus = new Label { AutoSize = true, MaximumSize = new Size(W - 200, 0),
                                Margin = new Padding(8, 7, 0, 0), UseMnemonic = false };
        col.Controls.Add(Ui.Row(calButton, calStatus));

        // Errores y botón final
        errorLabel = Ui.Text("", W, top: 10);
        errorLabel.ForeColor = Color.Firebrick;
        errorLabel.Visible = false;
        col.Controls.Add(errorLabel);
        var start = Ui.Button(L.T("welcome.start"), (_, _) => Start());
        start.Margin = new Padding(W - 110, 14, 0, 0);
        col.Controls.Add(start);
        AcceptButton = start;

        Controls.Add(col);
        ResumeLayout(true);

        app.Calendar.Changed += Refresh;
        FormClosed += (_, _) => app.Calendar.Changed -= Refresh;
        Activated += (_, _) => Refresh();
        Refresh();
    }

    new void Refresh()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(Refresh); return; }
        foreach (var b in avatarButtons) b.Checked = (string?)b.Tag == app.Stage.Sprites.Id;
        folderLabel.Text = folder ?? "—";
        var cal = app.Calendar;
        (calButton.Enabled, calButton.Visible, calStatus.Text, calStatus.ForeColor) = cal.State switch
        {
            GoogleCalendar.Status.NotConfigured => (false, false, L.T("menu.calNotConfigured"), Ui.Secondary),
            GoogleCalendar.Status.Connecting => (false, true, L.T("menu.calConnecting"), Ui.Secondary),
            GoogleCalendar.Status.Connected => (false, false,
                cal.Account != null ? L.T("welcome.connectedAs", cal.Account) : L.T("welcome.granted"),
                Color.ForestGreen),
            GoogleCalendar.Status.NeedsReconnect => (true, true, L.T("menu.calExpired"), Ui.Secondary),
            _ => (true, true, "", Ui.Secondary),
        };
    }

    void ChooseFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = L.T("panel.message"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
        };
        if (folder != null && Directory.Exists(folder)) dlg.InitialDirectory = folder;
        if (dlg.ShowDialog(this) != DialogResult.OK || string.IsNullOrEmpty(dlg.SelectedPath)) return;
        var f = WatchedPaths.Normalize(dlg.SelectedPath);
        if (WatchedPaths.IsHome(f))
        {
            ShowError(L.T("err.home"));
            return;
        }
        folder = f;
        errorLabel.Visible = false;
        // Si ya usa el plugin, no hacen falta los hooks.
        connectCheck.Checked = !HookInstaller.PluginEnabled(f);
        Refresh();
    }

    void ConnectCalendar() => _ = app.Calendar.ConnectAsync();

    void Start()
    {
        if (folder == null)
        {
            ShowError(L.T("welcome.needFolder"));
            return;
        }
        try
        {
            app.ApplyFolder(folder, connect: connectCheck.Checked);
        }
        catch (Exception e)
        {
            ShowError(e.Message);
            return;
        }
        Close();
        app.ShowGuide();   // después de la bienvenida, cómo funciona cada cosa
    }

    void ShowError(string message)
    {
        errorLabel.Text = message;
        errorLabel.Visible = true;
    }
}
