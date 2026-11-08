using System.Net.Http.Headers;
using System.Text.Json;

namespace PixelBuddy;

/// Lee tu Google Calendar (solo lectura): la lista de calendarios y los eventos de hoy.
/// Los eventos se guardan solo en memoria; en disco queda únicamente el permiso cifrado.
sealed class GoogleCalendar : IDisposable
{
    public enum Status { NotConfigured, Disconnected, Connecting, Connected, NeedsReconnect }

    public sealed record CalInfo(string Id, string Name, bool Primary);

    const string Api = "https://www.googleapis.com/calendar/v3";
    static readonly TimeSpan RefreshEvery = TimeSpan.FromMinutes(5);
    static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(1);

    readonly Settings settings;
    readonly System.Windows.Forms.Timer timer = new() { Interval = (int)RefreshEvery.TotalMilliseconds };
    string? refreshToken;
    string? accessToken;
    DateTime accessExpiresUtc;
    CancellationTokenSource? connecting;
    bool refreshing;

    List<CalInfo> calendars = new();
    List<CalEvent> events = new();
    DateTime? lastFetchUtc;

    public Status State { get; private set; }
    /// Correo de la cuenta (el id del calendario principal).
    public string? Account { get; private set; }
    public IReadOnlyList<CalInfo> Calendars => calendars;
    /// true si ya hay eventos leídos (aunque la última vez fallara la conexión).
    public bool HasData => lastFetchUtc != null;
    /// true si la última lectura falló (sin internet…).
    public bool LastFailed { get; private set; }

    /// Cambió algo (estado, calendarios o eventos).
    public event Action? Changed;

    public GoogleCalendar(Settings settings)
    {
        this.settings = settings;
        if (!GoogleAuth.Configured)
        {
            State = Status.NotConfigured;
            return;
        }
        refreshToken = TokenStore.Load();
        State = refreshToken != null ? Status.Connected : Status.Disconnected;
        timer.Tick += (_, _) => _ = RefreshAsync();
        timer.Start();
        if (State == Status.Connected) _ = RefreshAsync();
    }

    // MARK: Conectar y desconectar

    public async Task ConnectAsync()
    {
        if (State is Status.NotConfigured or Status.Connecting) return;
        connecting = new CancellationTokenSource();
        var previous = State;
        SetState(Status.Connecting);
        try
        {
            var tokens = await GoogleAuth.AuthorizeAsync(connecting.Token);
            if (tokens.RefreshToken == null) throw new GoogleAuth.AuthException(L.T("google.noRefresh"));
            TokenStore.Save(tokens.RefreshToken);
            refreshToken = tokens.RefreshToken;
            accessToken = tokens.AccessToken;
            accessExpiresUtc = tokens.ExpiresUtc;
            settings.CalendarsInitialized = false;   // la primera vez se usan los que ves en Google
            settings.Save();
            SetState(Status.Connected);
            await RefreshAsync(force: true);
        }
        catch (OperationCanceledException)
        {
            SetState(previous == Status.NeedsReconnect ? Status.NeedsReconnect : Status.Disconnected);
        }
        catch (Exception e)
        {
            SetState(previous == Status.NeedsReconnect ? Status.NeedsReconnect : Status.Disconnected);
            MessageBox.Show(e.Message, L.T("google.connectFail"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            connecting?.Dispose();
            connecting = null;
        }
    }

    public void CancelConnect() => connecting?.Cancel();

    public void Disconnect()
    {
        var token = refreshToken;
        TokenStore.Delete();
        refreshToken = accessToken = null;
        calendars = new();
        events = new();
        lastFetchUtc = null;
        Account = null;
        settings.ExcludedCalendars.Clear();
        settings.CalendarsInitialized = false;
        settings.Save();
        SetState(Status.Disconnected);
        if (token != null) _ = GoogleAuth.RevokeAsync(token);
    }

    void SetState(Status s)
    {
        State = s;
        Changed?.Invoke();
    }

    // MARK: Calendarios elegidos

    public bool IsEnabled(string id) => !settings.ExcludedCalendars.Contains(id);

    public void Toggle(string id)
    {
        if (!settings.ExcludedCalendars.Remove(id)) settings.ExcludedCalendars.Add(id);
        settings.Save();
        _ = RefreshAsync(force: true);
    }

    // MARK: Eventos

    /// Si los datos tienen más de 1 minuto, los vuelve a leer (en segundo plano).
    public void RefreshIfStale()
    {
        if (lastFetchUtc == null || DateTime.UtcNow - lastFetchUtc > StaleAfter) _ = RefreshAsync();
    }

    public async Task RefreshAsync(bool force = false)
    {
        if (State != Status.Connected || refreshing) return;
        if (!force && lastFetchUtc != null && DateTime.UtcNow - lastFetchUtc < StaleAfter) return;
        refreshing = true;
        try
        {
            var token = await AccessTokenAsync();
            var cals = await FetchCalendarsAsync(token);
            if (!settings.CalendarsInitialized)
            {
                // Al conectar, se usan los calendarios que tienes marcados en Google Calendar.
                settings.ExcludedCalendars = cals.Where(c => !c.selected).Select(c => c.info.Id).ToList();
                settings.CalendarsInitialized = true;
                settings.Save();
            }
            calendars = cals.Select(c => c.info).ToList();
            Account = calendars.FirstOrDefault(c => c.Primary)?.Id;
            var enabled = calendars.Where(c => IsEnabled(c.Id)).ToList();
            var lists = await Task.WhenAll(enabled.Select(c => FetchEventsAsync(token, c.Id)));
            events = lists.SelectMany(l => l).ToList();
            lastFetchUtc = DateTime.UtcNow;
            LastFailed = false;
        }
        catch (GoogleAuth.AuthException e) when (e.Revoked)
        {
            // El permiso ya no vale (lo quitaste en tu cuenta de Google, o caducó).
            TokenStore.Delete();
            refreshToken = accessToken = null;
            State = Status.NeedsReconnect;
        }
        catch
        {
            LastFailed = true;   // sin internet, Google no responde…: se reintenta más tarde
            accessToken = null;
        }
        finally
        {
            refreshing = false;
            Changed?.Invoke();
        }
    }

    /// Evento en curso (el que antes termina) y el siguiente, solo de hoy.
    public (CalEvent? current, CalEvent? next) CurrentAndNext()
    {
        var now = DateTime.Now;
        var endOfToday = DateTime.Today.AddDays(1);
        var current = events.Where(e => e.Start <= now && e.End > now).MinBy(e => e.End);
        var next = events.Where(e => e.Start > now && e.Start < endOfToday).MinBy(e => e.Start);
        return (current, next);
    }

    // MARK: Google Calendar API

    async Task<string> AccessTokenAsync()
    {
        if (accessToken != null && DateTime.UtcNow < accessExpiresUtc.AddMinutes(-1)) return accessToken;
        if (refreshToken == null) throw new GoogleAuth.AuthException("no token", revoked: true);
        var t = await GoogleAuth.RefreshAsync(refreshToken, CancellationToken.None);
        accessToken = t.AccessToken;
        accessExpiresUtc = t.ExpiresUtc;
        if (t.RefreshToken != null && t.RefreshToken != refreshToken)
        {
            refreshToken = t.RefreshToken;
            TokenStore.Save(refreshToken);
        }
        return accessToken;
    }

    static async Task<JsonDocument> GetAsync(string url, string token)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var r = await GoogleAuth.Http.SendAsync(req);
        if (r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new GoogleAuth.AuthException("unauthorized");   // se pide otro acceso al reintentar
        r.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await r.Content.ReadAsStreamAsync());
    }

    static async Task<List<(CalInfo info, bool selected)>> FetchCalendarsAsync(string token)
    {
        var result = new List<(CalInfo, bool)>();
        string? page = null;
        do
        {
            var url = $"{Api}/users/me/calendarList?minAccessRole=reader&maxResults=250" +
                      "&fields=items(id,summary,summaryOverride,primary,selected),nextPageToken" +
                      (page != null ? "&pageToken=" + Uri.EscapeDataString(page) : "");
            using var doc = await GetAsync(url, token);
            var root = doc.RootElement;
            if (root.TryGetProperty("items", out var items))
                foreach (var c in items.EnumerateArray())
                {
                    var id = Str(c, "id");
                    if (id == null) continue;
                    var name = Str(c, "summaryOverride") ?? Str(c, "summary") ?? id;
                    result.Add((new CalInfo(id, name, Bool(c, "primary")), Bool(c, "selected")));
                }
            page = Str(root, "nextPageToken");
        } while (page != null);
        // El principal primero, luego por nombre.
        return result.OrderBy(c => c.Item1.Primary ? 0 : 1)
            .ThenBy(c => c.Item1.Name, StringComparer.Create(L.Culture, true)).ToList();
    }

    static async Task<List<CalEvent>> FetchEventsAsync(string token, string calendarId)
    {
        var now = DateTime.UtcNow;
        var timeMin = now.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var timeMax = DateTime.Today.AddDays(1).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"{Api}/calendars/{Uri.EscapeDataString(calendarId)}/events" +
                  $"?timeMin={timeMin}&timeMax={timeMax}&singleEvents=true&orderBy=startTime&maxResults=100" +
                  "&fields=items(summary,status,eventType,start,end,attendees(self,responseStatus))";
        using var doc = await GetAsync(url, token);
        var list = new List<CalEvent>();
        if (!doc.RootElement.TryGetProperty("items", out var items)) return list;
        foreach (var e in items.EnumerateArray())
        {
            if (Str(e, "status") == "cancelled") continue;
            if (Str(e, "eventType") is "workingLocation" or "birthday") continue;
            // Los de todo el día (sin hora) no se muestran, como en Mac.
            if (!e.TryGetProperty("start", out var s) || Str(s, "dateTime") is not { } start) continue;
            if (!e.TryGetProperty("end", out var en) || Str(en, "dateTime") is not { } end) continue;
            if (e.TryGetProperty("attendees", out var att) && att.EnumerateArray()
                    .Any(a => Bool(a, "self") && Str(a, "responseStatus") == "declined")) continue;
            if (!DateTimeOffset.TryParse(start, out var st) || !DateTimeOffset.TryParse(end, out var et)) continue;
            list.Add(new CalEvent(Str(e, "summary") ?? "", st.LocalDateTime, et.LocalDateTime));
        }
        return list;
    }

    static string? Str(JsonElement o, string k) =>
        o.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    static bool Bool(JsonElement o, string k) =>
        o.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.True;

    public void Dispose()
    {
        timer.Dispose();
        connecting?.Cancel();
    }
}
