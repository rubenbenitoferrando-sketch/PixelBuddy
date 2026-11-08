using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PixelBuddy;

/// Inicio de sesión con Google para apps de escritorio (OAuth 2.0 con PKCE):
/// se abre el navegador, el usuario acepta y Google devuelve un código a un pequeño
/// servidor en 127.0.0.1 (solo accesible desde este PC). Permiso pedido: leer el calendario.
///
/// Es lo ÚNICO de PixelBuddy que usa internet, y solo habla con Google.
static class GoogleAuth
{
    public const string Scope = "https://www.googleapis.com/auth/calendar.readonly";
    const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    const string RevokeEndpoint = "https://oauth2.googleapis.com/revoke";

    public static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    // El identificador de la app en Google se pone al compilar (no está en el repositorio).
    // En apps de escritorio Google no lo considera secreto, pero así no se reutiliza fácilmente.
    static string Metadata(string key) =>
        typeof(GoogleAuth).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key)?.Value ?? "";

    static string ClientId => Metadata("GoogleClientId");
    static string ClientSecret => Metadata("GoogleClientSecret");

    /// false si esta versión se compiló sin identificador de Google.
    public static bool Configured => ClientId.Length > 0;

    public sealed record Tokens(string AccessToken, DateTime ExpiresUtc, string? RefreshToken);

    public sealed class AuthException(string message, bool revoked = false) : Exception(message)
    {
        /// true si Google dice que el permiso ya no vale (lo quitaste en tu cuenta, caducó…).
        public bool Revoked { get; } = revoked;
    }

    /// Abre el navegador para que el usuario dé permiso y espera la respuesta (máx. 5 min).
    public static async Task<Tokens> AuthorizeAsync(CancellationToken cancel)
    {
        var verifier = RandomUrlSafe(32);
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = RandomUrlSafe(16);

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var redirect = $"http://127.0.0.1:{port}/";

        var url = AuthEndpoint + "?" + Form(new()
        {
            ["client_id"] = ClientId,
            ["redirect_uri"] = redirect,
            ["response_type"] = "code",
            ["scope"] = Scope,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
        });
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        var code = await WaitForCodeAsync(listener, state, timeout.Token);

        var json = await PostAsync(TokenEndpoint, new()
        {
            ["code"] = code,
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["redirect_uri"] = redirect,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = verifier,
        }, cancel);
        return ParseTokens(json, previousRefresh: null);
    }

    /// Pide un acceso nuevo (dura ~1 h) con el permiso guardado.
    public static async Task<Tokens> RefreshAsync(string refreshToken, CancellationToken cancel)
    {
        var json = await PostAsync(TokenEndpoint, new()
        {
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
        }, cancel);
        return ParseTokens(json, refreshToken);
    }

    /// Avisa a Google de que ya no queremos el permiso (al desconectar).
    public static async Task RevokeAsync(string token)
    {
        try
        {
            using var r = await Http.PostAsync(RevokeEndpoint,
                new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token }));
        }
        catch { }
    }

    // MARK: Servidor local para la respuesta de Google

    static async Task<string> WaitForCodeAsync(TcpListener listener, string state, CancellationToken cancel)
    {
        while (true)
        {
            using var client = await listener.AcceptTcpClientAsync(cancel);
            using var stream = client.GetStream();
            // Los navegadores abren a veces conexiones "por si acaso" sin enviar nada.
            string requestLine;
            using (var wait = CancellationTokenSource.CreateLinkedTokenSource(cancel))
            {
                wait.CancelAfter(TimeSpan.FromSeconds(3));
                try { requestLine = await ReadLineAsync(stream, wait.Token); }
                catch (OperationCanceledException) when (!cancel.IsCancellationRequested) { continue; }
                catch (IOException) { continue; }
            }
            // "GET /?state=…&code=… HTTP/1.1"
            var parts = requestLine.Split(' ');
            var target = parts.Length >= 2 ? parts[1] : "";
            if (!target.StartsWith("/?"))
            {
                await RespondAsync(stream, 404, "", cancel);   // p. ej. /favicon.ico
                continue;
            }
            var query = ParseQuery(target[2..]);
            if (query.GetValueOrDefault("state") != state)
            {
                await RespondAsync(stream, 400, Page(L.T("google.page.error")), cancel);
                continue;
            }
            if (query.TryGetValue("code", out var code) && code.Length > 0)
            {
                await RespondAsync(stream, 200, Page(L.T("google.page.ok")), cancel);
                return code;
            }
            await RespondAsync(stream, 200, Page(L.T("google.page.denied")), cancel);
            throw new AuthException(L.T("google.denied"));
        }
    }

    static async Task<string> ReadLineAsync(NetworkStream s, CancellationToken cancel)
    {
        var sb = new StringBuilder();
        var buf = new byte[1];
        while (sb.Length < 8192 && await s.ReadAsync(buf, cancel) == 1)
        {
            if (buf[0] == '\n') break;
            if (buf[0] != '\r') sb.Append((char)buf[0]);
        }
        return sb.ToString();
    }

    static async Task RespondAsync(NetworkStream s, int status, string html, CancellationToken cancel)
    {
        var body = Encoding.UTF8.GetBytes(html);
        var head = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {status} {(status == 200 ? "OK" : "Error")}\r\n" +
            "Content-Type: text/html; charset=utf-8\r\n" +
            $"Content-Length: {body.Length}\r\nConnection: close\r\nCache-Control: no-store\r\n\r\n");
        await s.WriteAsync(head, cancel);
        await s.WriteAsync(body, cancel);
    }

    static string Page(string message) =>
        "<!doctype html><meta charset=utf-8><title>PixelBuddy</title>" +
        "<body style=\"font:16px Consolas,monospace;background:#fffcf2;color:#12121e;" +
        "display:flex;align-items:center;justify-content:center;height:90vh;text-align:center\">" +
        $"<div><p style=\"font-size:22px;font-weight:bold\">PixelBuddy</p><p>{WebUtility.HtmlEncode(message)}</p></div>";

    // MARK: Ayudantes

    static async Task<JsonDocument> PostAsync(string url, Dictionary<string, string> form, CancellationToken cancel)
    {
        HttpResponseMessage r;
        try
        {
            r = await Http.PostAsync(url, new FormUrlEncodedContent(form), cancel);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !cancel.IsCancellationRequested)
        {
            throw new AuthException(L.T("google.offline"));
        }
        using (r)
        {
            var text = await r.Content.ReadAsStringAsync(cancel);
            JsonDocument doc;
            try { doc = JsonDocument.Parse(text); }
            catch { throw new AuthException(L.T("google.failed", (int)r.StatusCode)); }
            if (!r.IsSuccessStatusCode)
            {
                var error = doc.RootElement.TryGetProperty("error", out var e) ? e.GetString() : null;
                doc.Dispose();
                throw new AuthException(L.T("google.failed", (int)r.StatusCode), revoked: error == "invalid_grant");
            }
            return doc;
        }
    }

    static Tokens ParseTokens(JsonDocument json, string? previousRefresh)
    {
        using (json)
        {
            var o = json.RootElement;
            var access = o.GetProperty("access_token").GetString() ?? "";
            var expires = o.TryGetProperty("expires_in", out var ex) ? ex.GetInt32() : 3600;
            var refresh = o.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : previousRefresh;
            return new Tokens(access, DateTime.UtcNow.AddSeconds(expires), refresh);
        }
    }

    static string Form(Dictionary<string, string> values) =>
        string.Join("&", values.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

    static Dictionary<string, string> ParseQuery(string q)
    {
        var d = new Dictionary<string, string>();
        foreach (var part in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = part.IndexOf('=');
            var k = Uri.UnescapeDataString((i < 0 ? part : part[..i]).Replace('+', ' '));
            var v = i < 0 ? "" : Uri.UnescapeDataString(part[(i + 1)..].Replace('+', ' '));
            d.TryAdd(k, v);
        }
        return d;
    }

    static string RandomUrlSafe(int bytes) => Base64Url(RandomNumberGenerator.GetBytes(bytes));

    static string Base64Url(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// Guarda el permiso de Google cifrado con tu usuario de Windows (DPAPI): solo tu cuenta
/// de este PC puede leerlo.
static class TokenStore
{
    static readonly string FilePath = Path.Combine(Paths.SupportDir, "google-token.bin");
    static readonly byte[] Entropy = Encoding.UTF8.GetBytes("PixelBuddy Google Calendar");

    public static string? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var plain = Native.Unprotect(File.ReadAllBytes(FilePath), Entropy);
            return plain == null ? null : Encoding.UTF8.GetString(plain);
        }
        catch { return null; }
    }

    public static void Save(string refreshToken)
    {
        Directory.CreateDirectory(Paths.SupportDir);
        var data = Native.Protect(Encoding.UTF8.GetBytes(refreshToken), Entropy)
                   ?? throw new IOException("DPAPI");
        var tmp = FilePath + ".tmp";
        File.WriteAllBytes(tmp, data);
        File.Move(tmp, FilePath, overwrite: true);
    }

    public static void Delete()
    {
        try { File.Delete(FilePath); } catch { }
    }
}
