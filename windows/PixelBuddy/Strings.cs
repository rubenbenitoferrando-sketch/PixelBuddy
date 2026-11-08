using System.Globalization;

namespace PixelBuddy;

/// Textos de la app en español e inglés (español si Windows está en español).
static class L
{
    public static readonly bool IsSpanish =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es";

    public static CultureInfo Culture { get; } = new(IsSpanish ? "es-ES" : "en-US");

    /// Texto traducido; {0}, {1}… se sustituyen por los argumentos.
    public static string T(string key, params object[] args)
    {
        if (!Table.TryGetValue(key, out var pair)) return key;
        var s = IsSpanish ? pair.es : pair.en;
        return args.Length == 0 ? s : string.Format(Culture, s, args);
    }

    static readonly Dictionary<string, (string es, string en)> Table = new()
    {
        // Menú del icono (junto al reloj)
        ["menu.show"] = ("Mostrar personajes", "Show buddies"),
        ["menu.hide"] = ("Ocultar personajes", "Hide buddies"),
        ["menu.avatar"] = ("Personaje", "Character"),
        ["menu.calendars"] = ("Calendarios", "Calendars"),
        ["menu.calNotConfigured"] = ("Esta versión no incluye Google Calendar",
                                     "This build doesn't include Google Calendar"),
        ["menu.calConnect"] = ("Conectar Google Calendar…", "Connect Google Calendar…"),
        ["menu.calConnecting"] = ("Conectando… (mira tu navegador)", "Connecting… (check your browser)"),
        ["menu.calCancel"] = ("Cancelar", "Cancel"),
        ["menu.calExpired"] = ("El permiso de Google ha caducado", "Google access has expired"),
        ["menu.calAccount"] = ("Cuenta: {0}", "Account: {0}"),
        ["menu.calLoading"] = ("Leyendo calendarios…", "Loading calendars…"),
        ["menu.calOffline"] = ("Sin conexión con Google", "Can't reach Google"),
        ["menu.calRefresh"] = ("Actualizar ahora", "Refresh now"),
        ["menu.calDisconnect"] = ("Desconectar Google Calendar", "Disconnect Google Calendar"),
        ["menu.calDisconnectAsk"] = (
            "PixelBuddy dejará de leer tu calendario y borrará el permiso guardado en este PC.",
            "PixelBuddy will stop reading your calendar and delete the access saved on this PC."),

        // Google
        ["google.page.ok"] = ("¡Listo! Ya puedes cerrar esta pestaña y volver a PixelBuddy.",
                              "All set! You can close this tab and go back to PixelBuddy."),
        ["google.page.denied"] = ("No has dado permiso. Puedes cerrar esta pestaña.",
                                  "You didn't grant access. You can close this tab."),
        ["google.page.error"] = ("Esta respuesta no es de PixelBuddy. Vuelve a intentarlo desde el menú.",
                                 "This response isn't from PixelBuddy. Please try again from the menu."),
        ["google.denied"] = ("No has dado permiso para leer tu calendario.", "You didn't grant access to your calendar."),
        ["google.offline"] = ("No he podido conectar con Google. ¿Tienes internet?",
                              "Couldn't reach Google. Are you online?"),
        ["google.failed"] = ("Google ha respondido con un error ({0}). Vuelve a intentarlo.",
                             "Google returned an error ({0}). Please try again."),
        ["google.noRefresh"] = ("Google no ha guardado el permiso. Vuelve a intentarlo.",
                                "Google didn't keep the access. Please try again."),
        ["google.connectFail"] = ("No he podido conectar Google Calendar", "Couldn't connect Google Calendar"),
        ["menu.watching"] = ("Vigilando: {0}", "Watching: {0}"),
        ["menu.noFolder"] = ("ninguna carpeta", "no folder"),
        ["menu.changeFolder"] = ("Cambiar carpeta vigilada…", "Change watched folder…"),
        ["menu.login"] = ("Abrir al iniciar Windows", "Open at sign-in"),
        ["menu.quit"] = ("Salir de PixelBuddy", "Quit PixelBuddy"),

        // Avisos
        ["alert.noSprites"] = ("PixelBuddy no encuentra los personajes", "PixelBuddy can't find its characters"),
        ["alert.reinstall"] = ("Vuelve a instalar la app.", "Please reinstall the app."),
        ["alert.nowWatching"] = ("Ahora vigilo {0}", "Now watching {0}"),
        ["alert.restartSessions"] = (
            "Cierra y vuelve a abrir las sesiones de Claude Code de esa carpeta para que tu personaje reaccione.",
            "Close and reopen your Claude Code sessions in that folder so your buddy can react."),
        ["alert.loginFail"] = ("No he podido cambiar el arranque al iniciar Windows",
                               "Couldn't change Open at sign-in"),
        ["panel.message"] = ("Elige tu carpeta de proyecto (la que abres en Claude Code)",
                             "Choose your project folder (the one you open in Claude Code)"),
        ["err.home"] = ("Elige la carpeta del proyecto, no tu carpeta de usuario.",
                        "Choose a project folder, not your user folder."),
        ["err.json"] = ("{0} no es un JSON válido. No lo he tocado.", "{0} isn't valid JSON. I left it untouched."),
        ["err.noHook"] = ("Falta pixelbuddy-hook.exe junto a PixelBuddy.exe. Vuelve a descomprimir la app.",
                          "pixelbuddy-hook.exe is missing next to PixelBuddy.exe. Please unzip the app again."),
        ["alert.folderFail"] = ("No he podido conectar Claude Code en esa carpeta", "Couldn't connect Claude Code in that folder"),
        ["first.title"] = ("Bienvenido a PixelBuddy", "Welcome to PixelBuddy"),
        ["first.body"] = (
            "Tu personaje vive encima de la barra de tareas y reacciona a lo que hace Claude Code.\n\nAhora elige tu carpeta de proyecto: solo reaccionará a las sesiones de Claude Code abiertas en ella (y sus subcarpetas).\n\nPuedes cambiarla cuando quieras desde el icono de PixelBuddy, junto al reloj.",
            "Your buddy lives above the taskbar and reacts to what Claude Code is doing.\n\nNow choose your project folder: it only reacts to Claude Code sessions opened in it (and its subfolders).\n\nYou can change it anytime from the PixelBuddy icon next to the clock."),

        // Bocadillo
        ["bubble.noCalTitle"] = ("SIN CALENDARIO", "NO CALENDAR"),
        ["bubble.noCalBody"] = ("Conecta tu Google\nCalendar desde el\nicono de PixelBuddy,\njunto al reloj.",
                                "Connect Google\nCalendar from the\nPixelBuddy icon\nnext to the clock."),
        ["bubble.notConfigured"] = ("Esta versión no\nincluye Google\nCalendar.", "This build doesn't\ninclude Google\nCalendar."),
        ["bubble.connectingTitle"] = ("CONECTANDO…", "CONNECTING…"),
        ["bubble.connectingBody"] = ("Acepta el permiso\nen tu navegador.", "Allow access in\nyour browser."),
        ["bubble.reconnectTitle"] = ("RECONECTA GOOGLE", "RECONNECT GOOGLE"),
        ["bubble.reconnectBody"] = ("El permiso ha\ncaducado. Conéctalo\notra vez desde el\nicono del reloj.",
                                    "Access expired.\nConnect again from\nthe icon next to\nthe clock."),
        ["bubble.offlineTitle"] = ("SIN CONEXIÓN", "OFFLINE"),
        ["bubble.offlineBody"] = ("No he podido leer\ntu calendario.", "Couldn't read your\ncalendar."),
        ["bubble.loadingTitle"] = ("UN MOMENTO…", "ONE MOMENT…"),
        ["bubble.loadingBody"] = ("Estoy leyendo tu\ncalendario.", "Reading your\ncalendar."),
        ["bubble.now"] = ("AHORA", "NOW"),
        ["bubble.until"] = ("hasta las {0}", "until {0}"),
        ["bubble.rest"] = ("Puedes descansar", "You can relax"),
        ["bubble.next"] = ("DESPUÉS", "NEXT"),
        ["bubble.untitled"] = ("(sin título)", "(untitled)"),
        ["bubble.today"] = ("hoy a las {0}", "today at {0}"),
        ["bubble.tomorrow"] = ("mañana a las {0}", "tomorrow at {0}"),
        ["bubble.dayAt"] = ("{0} a las {1}", "{0} at {1}"),
        ["bubble.remove"] = ("✕ QUITAR", "✕ REMOVE"),
    };
}
