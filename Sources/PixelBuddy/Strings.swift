import Foundation

/// Textos de la app en español e inglés. Se usa el idioma preferido del Mac
/// (español si empieza por "es"; inglés en cualquier otro caso).
enum L {
    static let isSpanish: Bool = (Locale.preferredLanguages.first ?? "en").hasPrefix("es")
    static var locale: Locale { Locale(identifier: isSpanish ? "es_ES" : "en_US") }

    /// Texto traducido; `%@` se sustituye por los argumentos, en orden.
    static func t(_ key: String, _ args: CVarArg...) -> String {
        guard let pair = table[key] else { return key }
        let s = isSpanish ? pair.es : pair.en
        return args.isEmpty ? s : String(format: s, arguments: args)
    }

    private static let table: [String: (es: String, en: String)] = [
        // Barra de menús
        "menu.show": ("Mostrar personajes", "Show Buddies"),
        "menu.hide": ("Ocultar personajes", "Hide Buddies"),
        "menu.avatar": ("Personaje", "Character"),
        "menu.calendars": ("Calendarios", "Calendars"),
        "menu.noCalendars": ("No hay calendarios", "No calendars"),
        "menu.grantCalendar": ("Dar acceso al Calendario…", "Allow Calendar Access…"),
        "menu.watching": ("Vigilando: %@", "Watching: %@"),
        "menu.noFolder": ("ninguna carpeta", "no folder"),
        "menu.changeFolder": ("Cambiar carpeta vigilada…", "Change Watched Folder…"),
        "menu.setup": ("Configurar PixelBuddy…", "Set Up PixelBuddy…"),
        "menu.login": ("Abrir al iniciar sesión", "Open at Login"),
        "menu.quit": ("Salir de PixelBuddy", "Quit PixelBuddy"),

        // Avisos
        "alert.loginFail": ("No he podido cambiar el arranque al iniciar sesión",
                            "Couldn't change Open at Login"),
        "alert.nowWatching": ("Ahora vigilo %@", "Now watching %@"),
        "alert.restartSessions": (
            "Cierra y vuelve a abrir las sesiones de Claude Code de esa carpeta para que tu personaje reaccione.",
            "Close and reopen your Claude Code sessions in that folder so your buddy can react."),
        "alert.folderFail": ("No he podido cambiar la carpeta", "Couldn't change the folder"),
        "alert.noSprites": ("PixelBuddy no encuentra los personajes", "PixelBuddy can't find its characters"),
        "alert.reinstall": ("Vuelve a instalar la app.", "Please reinstall the app."),
        "panel.prompt": ("Vigilar esta carpeta", "Watch This Folder"),
        "panel.message": ("Elige tu carpeta de proyecto (la que abres en Claude Code)",
                          "Choose your project folder (the one you open in Claude Code)"),
        "err.home": ("Elige la carpeta del proyecto, no tu carpeta de inicio.",
                     "Choose a project folder, not your home folder."),
        "err.json": ("%@ no es un JSON válido. No lo he tocado.", "%@ isn't valid JSON. I left it untouched."),

        // Bocadillo
        "bubble.noCalTitle": ("SIN CALENDARIO", "NO CALENDAR"),
        "bubble.noCalBody": ("Dale acceso en Ajustes del\nSistema > Privacidad y\nseguridad > Calendarios.",
                             "Allow access in System\nSettings > Privacy &\nSecurity > Calendars."),
        "bubble.now": ("AHORA", "NOW"),
        "bubble.until": ("hasta las %@", "until %@"),
        "bubble.rest": ("Puedes descansar", "You can relax"),
        "bubble.next": ("DESPUÉS", "NEXT"),
        "bubble.untitled": ("(sin título)", "(untitled)"),
        "bubble.today": ("hoy a las %@", "today at %@"),
        "bubble.tomorrow": ("mañana a las %@", "tomorrow at %@"),
        "bubble.dayAt": ("%@ a las %@", "%@ at %@"),
        "bubble.remove": ("✕ QUITAR", "✕ REMOVE"),

        // Bienvenida
        "welcome.title": ("Bienvenido a PixelBuddy", "Welcome to PixelBuddy"),
        "welcome.intro": (
            "Un compañero pixel art que vive encima del Dock, reacciona a lo que hace Claude Code y te enseña tu calendario con un clic.",
            "A pixel art buddy that lives above your Dock, reacts to what Claude Code is doing and shows your calendar with one click."),
        "welcome.step1": ("1. Elige tu personaje", "1. Pick your character"),
        "welcome.step2": ("2. Elige tu carpeta de proyecto", "2. Choose your project folder"),
        "welcome.step2.help": (
            "Solo reaccionará a las sesiones de Claude Code abiertas en esta carpeta (y sus subcarpetas).",
            "It only reacts to Claude Code sessions opened in this folder (and its subfolders)."),
        "welcome.chooseFolder": ("Elegir carpeta…", "Choose Folder…"),
        "welcome.connect": ("Conectar Claude Code en esta carpeta (recomendado)",
                            "Connect Claude Code in this folder (recommended)"),
        "welcome.connect.help": (
            "Añade los avisos de PixelBuddy a .claude/settings.local.json de la carpeta (archivo personal, con copia de seguridad). ¿Prefieres el plugin? En Claude Code: /plugin install pixelbuddy@pixelbuddy",
            "Adds PixelBuddy's hooks to the folder's .claude/settings.local.json (a personal file, backed up first). Prefer the plugin? In Claude Code: /plugin install pixelbuddy@pixelbuddy"),
        "welcome.step3": ("3. Calendario (opcional)", "3. Calendar (optional)"),
        "welcome.step3.help": (
            "Al hacer clic en tu personaje verás el evento actual y el siguiente. Todo se queda en tu Mac.",
            "Click your buddy to see your current and next event. Everything stays on your Mac."),
        "welcome.grant": ("Dar acceso al Calendario", "Allow Calendar Access"),
        "welcome.granted": ("✓ Acceso concedido", "✓ Access granted"),
        "welcome.denied": ("Sin acceso. Puedes darlo en Ajustes del Sistema > Privacidad y seguridad > Calendarios.",
                           "No access. You can allow it in System Settings > Privacy & Security > Calendars."),
        "welcome.start": ("Empezar", "Start"),
        "welcome.needFolder": ("Elige primero una carpeta de proyecto.", "Choose a project folder first."),
        // Cómo funciona
        "menu.guide": ("Cómo funciona…", "How It Works…"),
        "guide.title": ("Cómo funciona PixelBuddy", "How PixelBuddy works"),
        "guide.intro": (
            "Tu personaje vive encima del Dock y te cuenta de un vistazo qué hace Claude Code.",
            "Your buddy lives above the Dock and tells you at a glance what Claude Code is doing."),
        "guide.run.t": ("Trabaja", "Working"),
        "guide.run.d": ("Claude está trabajando. Va de un lado a otro hasta que acaba.",
                        "Claude is working. It moves back and forth until it's done."),
        "guide.katana.t": ("Ha terminado", "Done"),
        "guide.katana.d": ("Claude ha acabado. Lo celebra durante 30 segundos: ya puedes revisar su trabajo.",
                           "Claude has finished. It celebrates for 30 seconds: time to check the result."),
        "guide.ask.t": ("Te pregunta", "Needs you"),
        "guide.ask.d": ("Claude te hace una pregunta o necesita tu permiso. Ve a Claude Code a contestar.",
                        "Claude has a question or needs your permission. Go to Claude Code to answer."),
        "guide.alert.t": ("Error", "Error"),
        "guide.alert.d": ("Claude se ha parado por un error (límite de uso, conexión…) y necesita tu ayuda.",
                          "Claude stopped on an error (usage limit, connection…) and needs your help."),
        "guide.train.t": ("Te espera", "Waiting"),
        "guide.train.d": ("Claude espera tu siguiente mensaje. Al cabo de 1 minuto se duerme.",
                          "Claude is waiting for your next message. After 1 minute it falls asleep."),
        "guide.sleep.t": ("Duerme", "Sleeping"),
        "guide.sleep.d": ("No hay actividad. Se despierta en cuanto vuelvas a escribir a Claude.",
                          "Nothing is happening. It wakes up as soon as you write to Claude again."),
        "guide.more": ("Más cosas", "Good to know"),
        "guide.tip1": ("Puedes tener varias sesiones de Claude Code a la vez en tu carpeta: cada una tiene su personaje y su color (la 1.ª azul, la 2.ª roja, luego verde, morada…) y funcionan por separado. Si no hay sesiones, queda el principal.",
                       "You can run several Claude Code sessions at once in your folder: each gets its own buddy and color (1st blue, 2nd red, then green, purple…) and they work independently. With no sessions, the main buddy stays."),
        "guide.tip2": ("Haz clic en un personaje para ver tu evento actual y el siguiente de hoy.",
                       "Click a buddy to see your current event and your next one today."),
        "guide.tip3": ("Con ✕ Quitar, en el bocadillo, retiras el personaje de una sesión que ya no usas.",
                       "Use ✕ Remove in the bubble to dismiss the buddy of a session you no longer use."),
        "guide.tip4": ("En la barra de menús (arriba a la derecha) cambias de personaje, los ocultas, eliges calendarios o la carpeta.",
                       "From the menu bar (top right) you can switch character, hide buddies, pick calendars or the folder."),
        "guide.tip5": ("Si una sesión no hace reaccionar al personaje, ciérrala y vuelve a abrirla en Claude Code.",
                       "If a session doesn't make your buddy react, close it and reopen it in Claude Code."),
        "guide.ok": ("Entendido", "Got it"),
        "welcome.done": ("Listo. Cierra y vuelve a abrir tus sesiones de Claude Code en esa carpeta.",
                         "All set. Close and reopen your Claude Code sessions in that folder."),
    ]
}
