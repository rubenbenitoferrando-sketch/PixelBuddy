using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PixelBuddy;

/// Escenario: la franja encima de la barra de tareas (abajo a la izquierda) con todos los
/// personajes. Las posiciones van en puntos (como en Mac) y se multiplican por la escala
/// de la pantalla; los sprites se amplían ×2 (×3, ×4… en pantallas con más escala)
/// sin suavizar.
sealed class Stage : IDisposable
{
    const double WindowHeightPt = 260;
    const string GuardianId = "__guardian__";
    /// Notificaciones que significan "Claude te pregunta o te pide permiso".
    static readonly HashSet<string> AskNotifications = new()
    {
        "permission_prompt", "elicitation_dialog", "elicitation_url_dialog", "agent_needs_input",
    };

    // Refresco adaptativo: fluido solo cuando alguien corre, para ahorrar batería.
    const int FastInterval = 16;     // algún personaje corriendo
    const int SlowInterval = 66;     // todos quietos (sprites a ≤ 8 fps)
    const int HiddenInterval = 1000; // personajes ocultos

    public StageWindow Window { get; } = new();
    public SpriteLibrary Sprites { get; private set; }
    readonly List<Buddy> buddies = new();   // por orden de aparición
    readonly Settings settings;

    /// Se llama al hacer clic en un personaje.
    public Action<Buddy>? BuddyClicked;
    /// Se llama al pulsar "✕ Quitar" en el bocadillo.
    public Action<Buddy>? RemoveClicked;

    readonly System.Windows.Forms.Timer timer = new();
    readonly Stopwatch clock = Stopwatch.StartNew();
    double Mono => clock.Elapsed.TotalSeconds;
    static double Wall => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    double lastTick, lastLayoutCheck = -10;
    double lastActivity;

    public bool IsHidden { get; private set; }
    bool fullscreenHidden;

    // Geometría (en píxeles de pantalla).
    Rectangle frame;
    double u = 1;        // píxeles por punto
    int spritePx = 2;    // píxeles de pantalla por píxel de sprite
    double WidthPt => frame.Width / u;
    bool dirty = true;

    // Bocadillo
    Bubble? bubble;
    double bubbleOpened;
    Rectangle bubbleRect;    // en coordenadas de la ventana
    bool mouseWasDown;

    public Stage(SpriteLibrary sprites, Settings settings)
    {
        Sprites = sprites;
        this.settings = settings;
        lastActivity = settings.LastActivity > 0 ? settings.LastActivity : Wall;
        SaveActivity();
        Window.MouseDown += (_, e) => HandleClick(e.Location);
        LayoutWindow();
        UpdateGuardian();
        timer.Tick += (_, _) => Tick();
        SetInterval(FastInterval);
        timer.Start();
        Window.Show();
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
    }

    void OnDisplayChanged(object? s, EventArgs e) => Window.BeginInvoke(LayoutWindow);

    void SetInterval(int ms)
    {
        if (timer.Interval != ms) timer.Interval = ms;
    }

    void SaveActivity()
    {
        if (settings.LastActivity == lastActivity) return;
        settings.LastActivity = lastActivity;
        settings.Save();
    }

    // MARK: Ventana

    void LayoutWindow()
    {
        var screen = Screen.PrimaryScreen;
        if (screen == null) return;
        var scale = Native.PrimaryScale();
        var work = screen.WorkingArea;   // excluye la barra de tareas
        var h = (int)Math.Round(WindowHeightPt * scale);
        var f = new Rectangle(work.Left, work.Bottom - h, screen.Bounds.Width / 3, h);
        if (f == frame && scale == u) return;
        frame = f;
        u = scale;
        spritePx = Math.Max(1, (int)Math.Round(2 * scale));
        LayoutHomes();
        foreach (var b in buddies) b.RenderKey = "";
        dirty = true;
    }

    public void SetHidden(bool hidden)
    {
        IsHidden = hidden;
        if (hidden) CloseBubble();
        UpdateVisibility();
        SetInterval(hidden ? HiddenInterval : FastInterval);
    }

    void UpdateVisibility()
    {
        var visible = !IsHidden && !fullscreenHidden;
        if (Window.Visible != visible)
        {
            if (visible) { Window.Show(); dirty = true; } else Window.Hide();
        }
    }

    /// Cambia de personaje: siguen en su estado, solo cambia el dibujo.
    public void SetSprites(SpriteLibrary lib)
    {
        Sprites = lib;
        foreach (var b in buddies) b.RenderKey = "";
        dirty = true;
    }

    // MARK: Personajes

    IEnumerable<Buddy> SessionBuddies => buddies.Where(b => !b.IsGuardian);

    /// Sitio fijo de cada personaje cuando está quieto (sin solaparse).
    void LayoutHomes()
    {
        const double margin = 40;
        var n = buddies.Count;
        var spacing = n > 1 ? Math.Min(76, Math.Max(20, (WidthPt - 2 * margin) / (n - 1))) : 0;
        for (var i = 0; i < n; i++) buddies[i].HomeX = margin + i * spacing;
    }

    Buddy AddBuddy(string id, double now, bool guardian = false)
    {
        var count = Sprites.Variants.Count;
        int index;
        if (guardian)
        {
            index = 0;
        }
        else
        {
            var used = SessionBuddies.Select(b => b.ColorIndex % count).ToHashSet();
            index = Enumerable.Range(0, count).Where(i => !used.Contains(i)).DefaultIfEmpty(-1).First();
            if (index < 0) index = SessionBuddies.Count();
        }
        var buddy = new Buddy(id, index, guardian, now, Mono);
        buddies.Add(buddy);
        LayoutHomes();
        buddy.X = buddy.HomeX;
        dirty = true;
        return buddy;
    }

    void RemoveBuddy(Buddy b)
    {
        if (bubble?.Owner == b) CloseBubble();
        buddies.Remove(b);
        LayoutHomes();
        dirty = true;
    }

    /// Quita un personaje de sesión a mano (vuelve si esa sesión manda nuevos eventos).
    public void Dismiss(Buddy b)
    {
        if (b.IsGuardian) return;
        RemoveBuddy(b);
        UpdateGuardian();
    }

    public void RemoveAllSessions()
    {
        foreach (var b in SessionBuddies.ToList()) RemoveBuddy(b);
        UpdateGuardian();
    }

    /// Si no hay sesiones, queda un personaje principal para poder ver el calendario.
    void UpdateGuardian()
    {
        var guardian = buddies.FirstOrDefault(b => b.IsGuardian);
        if (!SessionBuddies.Any())
        {
            if (guardian == null)
            {
                var g = AddBuddy(GuardianId, Wall, guardian: true);
                g.LastEvent = lastActivity;
            }
        }
        else if (guardian != null)
        {
            RemoveBuddy(guardian);
        }
    }

    /// Aplica un evento de Claude Code (ya filtrado por carpeta).
    public void Apply(SessionEvent e)
    {
        var now = Wall;
        lastActivity = Math.Max(lastActivity, Math.Min(e.Ts, now));
        SaveActivity();
        if (!IsHidden) SetInterval(FastInterval);
        var existing = buddies.FirstOrDefault(b => b.Id == e.SessionId && !b.IsGuardian);
        if (e.Event == "SessionEnd" || now - e.Ts >= Buddy.ForgetAfter)
        {
            if (existing != null) RemoveBuddy(existing);
            UpdateGuardian();
            return;
        }
        var isNew = existing == null;
        var n = existing ?? AddBuddy(e.SessionId, e.Ts);
        // El archivo de cada sesión siempre contiene su último evento: su hora manda.
        n.LastEvent = e.Ts;
        switch (e.Event)
        {
            case "PreToolUse" when e.Detail == "AskUserQuestion":
                n.SetState(BuddyState.Ask, e.Ts);          // Claude te hace una pregunta
                break;
            case "UserPromptSubmit" or "PreToolUse" or "PostToolUse":
                n.SetState(BuddyState.Run, e.Ts);
                break;
            case "PermissionRequest":
                n.SetState(BuddyState.Ask, e.Ts);
                break;
            case "Notification" when AskNotifications.Contains(e.Detail):
                n.SetState(BuddyState.Ask, e.Ts);
                break;
            case "Notification":
                // Otros avisos ("lleva un rato esperando") no tapan una pregunta ni un error.
                if (n.State is not (BuddyState.Ask or BuddyState.Alert)) n.SetState(BuddyState.Train, e.Ts);
                break;
            case "Stop":
                n.SetState(BuddyState.Katana, e.Ts);
                break;
            case "StopFailure":
                n.SetState(BuddyState.Alert, e.Ts);
                break;
        }
        n.ApplyTimers(now);
        if (isNew && n.State == BuddyState.Run) n.Dir = Random.Shared.Next(2) == 0 ? 1 : -1;
        UpdateGuardian();
    }

    // MARK: Animación

    void Tick()
    {
        var now = Mono;
        var dt = Math.Min(now - lastTick, 0.1);
        lastTick = now;
        var wall = Wall;

        if (now - lastLayoutCheck > 2)
        {
            lastLayoutCheck = now;
            LayoutWindow();
            fullscreenHidden = Native.FullscreenAppRunning();
            UpdateVisibility();
            if (Window.Visible) Window.KeepOnTop();
        }

        foreach (var b in buddies.Where(b => !b.IsGuardian && wall - b.LastEvent >= Buddy.ForgetAfter).ToList())
            RemoveBuddy(b);
        UpdateGuardian();

        const double minX = 64;
        var maxX = Math.Max(minX, WidthPt - 64);

        foreach (var n in buddies)
        {
            if (n.IsGuardian)
                n.State = wall - lastActivity >= Buddy.GuardianSleepAfter ? BuddyState.Sleep : BuddyState.Idle;
            else
                n.ApplyTimers(wall);
            var target = n.State;
            if (n.State == BuddyState.Run)
            {
                n.X += n.Dir * n.Speed * dt;
                if (n.X >= maxX) { n.X = maxX; n.Dir = -1; }
                if (n.X <= minX) { n.X = minX; n.Dir = 1; }
            }
            else if (Math.Abs(n.X - n.HomeX) > 0.5)
            {
                // Vuelve corriendo a su sitio antes de cambiar de animación.
                target = BuddyState.Run;
                n.Dir = n.HomeX > n.X ? 1 : -1;
                var step = n.Speed * 1.3 * dt;
                n.X = Math.Abs(n.HomeX - n.X) <= step ? n.HomeX : n.X + n.Dir * step;
            }
            else
            {
                n.X = n.HomeX;
                n.Dir = 1;
            }
            if (target != n.Anim) { n.Anim = target; n.AnimStart = now; }
        }

        if (bubble != null)
        {
            if (now - bubbleOpened >= 8) CloseBubble();
            else CheckClickOutside();
        }

        if (Window.Visible) Render(now);

        var moving = buddies.Any(b => b.Anim == BuddyState.Run);
        SetInterval(IsHidden || fullscreenHidden ? HiddenInterval : moving ? FastInterval : SlowInterval);
    }

    int PxX(double x) => (int)Math.Round(x * u);

    /// Dibuja solo si algo ha cambiado (frame, posición, bocadillo…).
    void Render(double now)
    {
        var changed = dirty;
        foreach (var n in buddies)
        {
            var info = Sprites.Anims.GetValueOrDefault(n.Anim.Key());
            var idx = info?.FrameIndex(now - n.AnimStart) ?? 0;
            var key = $"{n.Anim}|{idx}|{PxX(n.X)}|{n.Dir}";
            if (key != n.RenderKey) { n.RenderKey = key; changed = true; }
        }
        if (!changed) return;
        dirty = false;

        using var bmp = new Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.SmoothingMode = SmoothingMode.None;
            using var attrs = new ImageAttributes();
            attrs.SetWrapMode(WrapMode.TileFlipXY);
            // Casi invisible (alfa 1): hace clicable todo el cuerpo, no solo sus píxeles.
            using var hit = new SolidBrush(Color.FromArgb(1, 0, 0, 0));

            foreach (var n in buddies)
            {
                g.FillRectangle(hit, HitRect(n));
                DrawBuddy(g, n, now, attrs);
            }

            if (bubble != null)
            {
                bubble.Layout(spritePx, g);
                var x = PxX(bubble.Owner.X);
                var left = Math.Clamp(x - bubble.Size.Width / 2, 0, Math.Max(0, frame.Width - bubble.Size.Width));
                var top = frame.Height - (int)Math.Round(62 * u) - bubble.Size.Height;
                bubbleRect = new Rectangle(left, Math.Max(0, top), bubble.Size.Width, bubble.Size.Height);
                bubble.Draw(g, bubbleRect.X, bubbleRect.Y, x - left);
            }
        }
        Window.Present(bmp, frame.X, frame.Y);
    }

    void DrawBuddy(Graphics g, Buddy n, double now, ImageAttributes attrs)
    {
        if (!Sprites.Anims.TryGetValue(n.Anim.Key(), out var info)) return;
        // La variante se busca en el personaje actual por su número de orden: así cambiar de
        // personaje funciona aunque las variantes tengan otros nombres (razas del perro…).
        var variant = Sprites.Variants[n.ColorIndex % Math.Max(1, Sprites.Variants.Count)];
        var frames = Sprites.Frames(variant, n.Anim.Key());
        if (frames.Length == 0) return;
        var f = frames[Math.Min(info.FrameIndex(now - n.AnimStart), frames.Length - 1)];
        int s = spritePx, fw = info.FrameW * s, fh = info.FrameH * s;
        var x = PxX(n.X);
        var top = frame.Height - fh;
        // Alineado por anchorX: el centro no "salta" al cambiar de estado.
        var state = g.Save();
        if (n.Dir < 0)
        {
            g.TranslateTransform(x, 0);
            g.ScaleTransform(-1, 1);
            g.DrawImage(f, new Rectangle(-info.AnchorX * s, top, fw, fh), 0, 0, f.Width, f.Height,
                GraphicsUnit.Pixel, attrs);
        }
        else
        {
            g.DrawImage(f, new Rectangle(x - info.AnchorX * s, top, fw, fh), 0, 0, f.Width, f.Height,
                GraphicsUnit.Pixel, attrs);
        }
        g.Restore(state);
    }

    // MARK: Ratón

    /// Zona clicable de un personaje (su cuerpo, sin la estela), en píxeles de la ventana.
    Rectangle HitRect(Buddy n)
    {
        var h = (int)Math.Round(60 * u);
        return new Rectangle(PxX(n.X - 24), frame.Height - h, (int)Math.Round(48 * u), h);
    }

    Buddy? BuddyAt(Point p) => buddies.AsEnumerable().Reverse().FirstOrDefault(b => HitRect(b).Contains(p));

    void HandleClick(Point p)
    {
        if (bubble != null && bubbleRect.Contains(p))
        {
            var local = new Point(p.X - bubbleRect.X, p.Y - bubbleRect.Y);
            var owner = bubble.Owner;
            var removed = bubble.Removable && Rectangle.Inflate(bubble.ButtonRect, 2, 2).Contains(local);
            CloseBubble();
            if (removed) RemoveClicked?.Invoke(owner);
            return;
        }
        var n = BuddyAt(p);
        if (n == null) return;
        if (bubble?.Owner == n) { CloseBubble(); return; }
        BuddyClicked?.Invoke(n);
    }

    /// Un clic en cualquier otro sitio de la pantalla cierra el bocadillo.
    void CheckClickOutside()
    {
        var down = (Native.GetAsyncKeyState(Native.VK_LBUTTON) & 0x8001) != 0
                   || (Native.GetAsyncKeyState(Native.VK_RBUTTON) & 0x8001) != 0;
        if (down && !mouseWasDown)
        {
            var c = Cursor.Position;
            var p = new Point(c.X - frame.X, c.Y - frame.Y);
            var inside = frame.Contains(c) && (bubbleRect.Contains(p) || BuddyAt(p) != null);
            if (!inside) CloseBubble();
        }
        mouseWasDown = down;
    }

    // MARK: Bocadillo

    /// Personaje cuyo bocadillo está abierto (o null).
    public Buddy? BubbleOwner => bubble?.Owner;

    public void ShowBubble(Bubble b)
    {
        // Si se rehace el del mismo personaje (datos nuevos), no se reinician los 8 s.
        var keepTime = bubble?.Owner == b.Owner;
        CloseBubble();
        bubble = b;
        if (!keepTime) bubbleOpened = Mono;
        // El clic que lo abre no debe cerrarlo.
        mouseWasDown = true;
        dirty = true;
        SetInterval(SlowInterval);
    }

    public void CloseBubble()
    {
        if (bubble == null) return;
        bubble.Dispose();
        bubble = null;
        bubbleRect = Rectangle.Empty;
        dirty = true;
    }

    public void Dispose()
    {
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
        timer.Dispose();
        CloseBubble();
        Window.Dispose();
    }
}
