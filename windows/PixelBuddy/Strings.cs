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
        ["menu.setup"] = ("Configurar PixelBuddy…", "Set up PixelBuddy…"),
        ["menu.guide"] = ("Cómo funciona…", "How it works…"),
        ["menu.uninstall"] = ("Desinstalar PixelBuddy…", "Uninstall PixelBuddy…"),
        ["uninstall.title"] = ("Desinstalar PixelBuddy", "Uninstall PixelBuddy"),
        ["uninstall.ask"] = (
            "Se quitarán los avisos de Claude Code de tu carpeta, el arranque con Windows, el permiso de Google Calendar y los ajustes de PixelBuddy, y la app se cerrará.\n\n¿Continuar?",
            "This removes the Claude Code hooks from your folder, Open at sign-in, the Google Calendar access and PixelBuddy's settings, and quits the app.\n\nContinue?"),
        ["uninstall.done"] = (
            "Listo. Ahora borra la carpeta de la app:\n{0}\n\nSi usabas el plugin, quítalo en Claude Code con:\n/plugin uninstall pixelbuddy@pixelbuddy",
            "Done. Now delete the app folder:\n{0}\n\nIf you used the plugin, remove it in Claude Code with:\n/plugin uninstall pixelbuddy@pixelbuddy"),

        // Bienvenida
        ["welcome.title"] = ("Bienvenido a PixelBuddy", "Welcome to PixelBuddy"),
        ["welcome.intro"] = (
            "Un compañero pixel art que vive encima de la barra de tareas, reacciona a lo que hace Claude Code y te enseña tu calendario con un clic.",
            "A pixel art buddy that lives above your taskbar, reacts to what Claude Code is doing and shows your calendar with one click."),
        ["welcome.step1"] = ("1. Elige tu personaje", "1. Pick your character"),
        ["welcome.step2"] = ("2. Elige tu carpeta de proyecto", "2. Choose your project folder"),
        ["welcome.step2.help"] = (
            "Solo reaccionará a las sesiones de Claude Code abiertas en esta carpeta (y sus subcarpetas).",
            "It only reacts to Claude Code sessions opened in this folder (and its subfolders)."),
        ["welcome.chooseFolder"] = ("Elegir carpeta…", "Choose folder…"),
        ["welcome.connect"] = ("Conectar Claude Code en esta carpeta (recomendado)",
                               "Connect Claude Code in this folder (recommended)"),
        ["welcome.connect.help"] = (
            "Añade los avisos de PixelBuddy a .claude\\settings.local.json de la carpeta (archivo personal, con copia de seguridad). ¿Prefieres el plugin? En Claude Code: /plugin install pixelbuddy@pixelbuddy",
            "Adds PixelBuddy's hooks to the folder's .claude\\settings.local.json (a personal file, backed up first). Prefer the plugin? In Claude Code: /plugin install pixelbuddy@pixelbuddy"),
        ["welcome.step3"] = ("3. Google Calendar (opcional)", "3. Google Calendar (optional)"),
        ["welcome.step3.help"] = (
            "Al hacer clic en tu personaje verás el evento actual y el siguiente. PixelBuddy solo lee tu calendario: los eventos no se guardan ni salen de tu PC.",
            "Click your buddy to see your current and next event. PixelBuddy only reads your calendar: events aren't saved and never leave your PC."),
        ["welcome.grant"] = ("Conectar Google Calendar", "Connect Google Calendar"),
        ["welcome.granted"] = ("✓ Conectado", "✓ Connected"),
        ["welcome.connectedAs"] = ("✓ Conectado: {0}", "✓ Connected: {0}"),
        ["welcome.start"] = ("Empezar", "Start"),
        ["welcome.needFolder"] = ("Elige primero una carpeta de proyecto.", "Choose a project folder first."),

        // Cómo funciona
        ["guide.title"] = ("Cómo funciona PixelBuddy", "How PixelBuddy works"),
        ["guide.intro"] = (
            "Tu personaje vive encima de la barra de tareas y te cuenta de un vistazo qué hace Claude Code.",
            "Your buddy lives above the taskbar and tells you at a glance what Claude Code is doing."),
        ["guide.run.t"] = ("Trabaja", "Working"),
        ["guide.run.d"] = ("Claude está trabajando. Va de un lado a otro hasta que acaba.",
                           "Claude is working. It moves back and forth until it's done."),
        ["guide.katana.t"] = ("Ha terminado", "Done"),
        ["guide.katana.d"] = ("Claude ha acabado. Lo celebra durante 30 segundos: ya puedes revisar su trabajo.",
                              "Claude has finished. It celebrates for 30 seconds: time to check the result."),
        ["guide.ask.t"] = ("Te pregunta", "Needs you"),
        ["guide.ask.d"] = ("Claude te hace una pregunta o necesita tu permiso. Ve a Claude Code a contestar.",
                           "Claude has a question or needs your permission. Go to Claude Code to answer."),
        ["guide.alert.t"] = ("Error", "Error"),
        ["guide.alert.d"] = ("Claude se ha parado por un error (límite de uso, conexión…) y necesita tu ayuda.",
                             "Claude stopped on an error (usage limit, connection…) and needs your help."),
        ["guide.train.t"] = ("Te espera", "Waiting"),
        ["guide.train.d"] = ("Claude espera tu siguiente mensaje. Al cabo de 1 minuto se duerme.",
                             "Claude is waiting for your next message. After 1 minute it falls asleep."),
        ["guide.sleep.t"] = ("Duerme", "Sleeping"),
        ["guide.sleep.d"] = ("No hay actividad. Se despierta en cuanto vuelvas a escribir a Claude.",
                             "Nothing is happening. It wakes up as soon as you write to Claude again."),
        ["guide.more"] = ("Más cosas", "Good to know"),
        ["guide.tip1"] = (
            "Puedes tener varias sesiones de Claude Code a la vez en tu carpeta: cada una tiene su personaje y su color (la 1.ª azul, la 2.ª roja, luego verde, morada…) y funcionan por separado. Si no hay sesiones, queda el principal.",
            "You can run several Claude Code sessions at once in your folder: each gets its own buddy and color (1st blue, 2nd red, then green, purple…) and they work independently. With no sessions, the main buddy stays."),
        ["guide.tip2"] = ("Haz clic en un personaje para ver tu evento actual y el siguiente de hoy.",
                          "Click a buddy to see your current event and your next one today."),
        ["guide.tip3"] = ("Con ✕ Quitar, en el bocadillo, retiras el personaje de una sesión que ya no usas.",
                          "Use ✕ Remove in the bubble to dismiss the buddy of a session you no longer use."),
        ["guide.tip4"] = (
            "Desde el icono de PixelBuddy junto al reloj (quizá dentro de la flecha ^) cambias de personaje, los ocultas, eliges calendarios o la carpeta.",
            "From the PixelBuddy icon next to the clock (maybe inside the ^ arrow) you can switch character, hide buddies, pick calendars or the folder."),
        ["guide.tip5"] = ("Si una sesión no hace reaccionar al personaje, ciérrala y vuelve a abrirla en Claude Code.",
                          "If a session doesn't make your buddy react, close it and reopen it in Claude Code."),
        ["guide.ok"] = ("Entendido", "Got it"),

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
