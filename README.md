<p align="center"><img src="docs/icon.png" width="160" alt="PixelBuddy icon"></p>

# PixelBuddy

A tiny pixel art buddy that lives above your Dock on **macOS** or your taskbar on **Windows**,
reacts to what **Claude Code** is doing, and shows your calendar with one click. Pick one of
**10 characters**: ninja, astronaut, developer, footballer, basketball player, F1 driver, dog,
firefighter, student or tennis player.

> **On Windows?** Jump to [Windows](#windows).

*[Leer en español ↓](#español)*

<p align="center"><img src="docs/scene_en.png" alt="PixelBuddy above the Dock: one buddy per Claude Code session and the calendar bubble"></p>

<p align="center"><img src="docs/characters.gif" alt="All ten characters in every state"></p>

| Claude Code… | Your buddy |
|---|---|
| is working | runs, flies, dribbles or drives back and forth |
| finished | celebrates for 30 s (katana, flag, goal, basket, A+…) |
| asks you something or needs permission | blue **?** |
| stopped on an error | red **!** |
| waits for your next message | trains, stretches, juggles, reads… (1 min) |
| has been idle | sleeps, Zzz |

- **Several sessions at once.** You can have several Claude Code sessions open in the same
  project folder at the same time: each one gets its own buddy, and they all work
  independently — one can be running while another celebrates and a third asks you
  something. The color tells you which session is which: the first session you open is
  blue, the second red, then green, purple, orange and yellow (the dog changes breed
  instead). When they are idle they stand side by side without overlapping.
- Click a buddy to see your current event and your next one today, from the macOS Calendar
  (iCloud, Google… any account added in *System Settings › Internet Accounts*).
- It only reacts to sessions opened in **your project folder** (and its subfolders). Git
  repositories and worktrees are supported: a session in a worktree counts as the original
  repository.
- Native app (Swift on macOS, C# on Windows), no dependencies, free. The macOS version never
  connects to the internet; the Windows version connects only to Google, to read your calendar.

## Characters

Pick yours in the welcome window or in the menu bar (*Character ▸*). Each Claude Code session
gets the next color in order; the dog changes breed instead (labrador, dalmatian, husky,
beagle, dachshund, pug), with a collar in the session color.

<p align="center"><img src="docs/gallery.png" width="640" alt="The ten characters with their six colors"></p>

## Requirements

- macOS 14 Sonoma or later (Apple Silicon or Intel), or Windows 10/11 ([see Windows](#windows))
- Claude Code (desktop app or CLI)

## Install (macOS)

Pick one of these three ways. The first two need nothing else.

### A. One command (easiest)

Open the **Terminal** app and paste:

```bash
curl -fsSL https://raw.githubusercontent.com/rubenbenitoferrando-sketch/PixelBuddy/main/scripts/get.sh | bash
```

It downloads the ready-made app from [Releases](https://github.com/rubenbenitoferrando-sketch/PixelBuddy/releases/latest),
installs it in `~/Applications` and opens it.

### B. Download the app

1. Download **PixelBuddy.zip** from [Releases](https://github.com/rubenbenitoferrando-sketch/PixelBuddy/releases/latest)
   and double-click it.
2. Drag **PixelBuddy.app** to your Applications folder and open it.
3. The first time, macOS says it can't verify the developer (the app isn't notarized by Apple,
   which costs money). Click **Done**, then go to *System Settings › Privacy & Security*,
   scroll down and click **Open Anyway**. You only do this once.

### C. Build it yourself

Needs the Xcode Command Line Tools (`xcode-select --install`; full Xcode is not needed).

```bash
git clone https://github.com/rubenbenitoferrando-sketch/PixelBuddy.git
cd PixelBuddy
./scripts/install.sh
```

The script builds the app on your Mac and installs it in `~/Applications`.

> With only the Command Line Tools installed (no full Xcode), the build may print
> `warning: could not determine XCTest paths … unable to lookup item 'PlatformPath'`.
> It's harmless (XCTest is only used for tests, which PixelBuddy doesn't need): just let the
> script finish.

### First launch

PixelBuddy opens a welcome window where you:

1. pick your character,
2. choose your project folder — and tick **Connect Claude Code**. It adds PixelBuddy's hooks
   to your personal `.claude/settings.local.json` (backing it up first): the folder's own, or,
   if the folder is inside a git repository, the repository root's, which is where Claude Code
   reads it. The file and its backups are kept out of git,
3. optionally allow Calendar access.

Close and reopen your Claude Code sessions in that folder, and your buddy comes to life.

### Prefer the Claude Code plugin?

Instead of ticking *Connect Claude Code*, you can install the hooks as a plugin. Inside a
Claude Code session opened in your project folder:

```
/plugin marketplace add rubenbenitoferrando-sketch/PixelBuddy
/plugin install pixelbuddy@pixelbuddy
```

Choose the **local** scope (only this project, only you). You still need the app: the plugin
just tells it what Claude Code is doing.

## Using it

- **Click a buddy** → speech bubble with your current event (and when it ends) and your next
  one today, or "You can relax". It closes on the next click or after 8 seconds.
- **✕ Remove** in the bubble removes a buddy whose session you no longer use. If that session
  starts working again, its buddy comes back. When there are no sessions, a main buddy stays
  so you can always check your calendar.
- **Menu bar icon**: character, show/hide, calendars, watched folder, set up, **How It Works…** (a short animated guide that also opens right after the welcome window), open at login, quit.

## Privacy

On macOS everything stays on your Mac. The hook writes one small file per session to
`~/Library/Application Support/PixelBuddy/sessions/` with the session id, folder, event name,
tool name or notification type, and time — nothing from your prompts or code. Events from
other folders are ignored. The calendar is read locally through EventKit. The macOS version
makes no network requests. On Windows, see [Windows › Privacy](#privacy-on-windows). Full
details: [privacy policy](https://rubenbenitoferrando-sketch.github.io/PixelBuddy/privacy.html).

## Uninstall

```bash
./scripts/uninstall.sh
```

It removes the hooks from your project folder, the login item, the app, its data and its
settings (backups of `settings.local.json` are kept). If you used the plugin:
`/plugin uninstall pixelbuddy@pixelbuddy`.

## Windows

The same buddies, characters and states, above the Windows taskbar (bottom left), with your
**Google Calendar** in the bubble.

**Requirements:** Windows 10 or 11 (Intel/AMD or ARM) and Claude Code. Nothing else to install.

**Install**

1. Download from [Releases](https://github.com/rubenbenitoferrando-sketch/PixelBuddy/releases/latest)
   **PixelBuddy-Windows-x64.zip** (most PCs) or **PixelBuddy-Windows-ARM.zip** (Snapdragon and
   other ARM PCs).
2. Unzip it wherever you like (for example in `Documents`) and open **PixelBuddy.exe**.
3. The first time, Windows says *"Windows protected your PC"* (the app isn't signed, which costs
   money): click **More info → Run anyway**.
4. The welcome window asks you to pick a character, choose your project folder (tick
   **Connect Claude Code**) and, optionally, connect Google Calendar. It also turns on *Open at
   sign-in*.

Close and reopen your Claude Code sessions in that folder, and your buddy comes to life. The
PixelBuddy icon sits next to the clock (maybe inside the **^** arrow): character, show/hide,
calendars, folder, set up, *How it works…*, open at sign-in, uninstall and quit.

**Google Calendar.** *Calendars → Connect Google Calendar…* opens your browser: sign in and allow
read-only access. Google may warn that the app isn't verified yet; click **Continue**. Your
current and next event appear when you click a buddy. All-day events aren't shown, nor
cancelled or declined ones. Choose which calendars to show in the same menu.

**Claude Code.** *Connect Claude Code* adds the hooks to your personal
`.claude\settings.local.json` (backed up first, kept out of git) using Claude Code's *exec form*:
they launch `pixelbuddy-hook.exe` directly, with no Git Bash or PowerShell, and with
`onFailure: continue`, so a hook problem can never block Claude. The
[plugin](#prefer-the-claude-code-plugin) works too if Git for Windows is installed.

<a id="privacy-on-windows"></a>**Privacy on Windows.** Session files are kept in
`%LOCALAPPDATA%\PixelBuddy\sessions\`, as on macOS. The only network access is to Google
(`accounts.google.com`, `oauth2.googleapis.com`, `www.googleapis.com`), and only to read
your calendar. Events stay in memory and are never saved or sent anywhere. The Google permission
is stored encrypted with your Windows account (DPAPI). Disconnecting deletes it and revokes it.
[Privacy policy](https://rubenbenitoferrando-sketch.github.io/PixelBuddy/privacy.html).

**Uninstall.** Use *Uninstall PixelBuddy…* in the menu (removes the hooks, *Open at sign-in*,
the Google permission and the settings), then delete the app folder. If you used the plugin:
`/plugin uninstall pixelbuddy@pixelbuddy`.

**Build it yourself.** Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download), on
Windows, macOS or Linux: run `windows/build.sh`. It leaves the zips in `windows/dist/`.
Google Calendar needs your own OAuth client ("Desktop app") in `windows/google-oauth.env`.
Without it, the app works without the calendar. On Windows the hook is compiled to native code.
From macOS/Linux it's a slower test build.

## Add your own character

Each character is a folder in `assets/avatars/<id>/` with a `manifest.json` and one sprite
strip per color and state (`<prefix>_<color>_<state>.png`, 32×32 frames; states `idle`, `run`,
`katana`, `train`, `sleep`, `alert`, `ask`). The app finds new folders automatically.
The ninja and astronaut are generated with `tools/make_sprites.py` and
`tools/make_astronaut.py`; the other eight with `tools/make_characters.py` from
`tools/chars/` (they all need Pillow). `tools/preview_characters.py` makes a preview GIF.

## Development

- Swift Package (macOS 14+) in `Sources/PixelBuddy/`. `scripts/build_app.sh` builds with
  `swift build -c release`, assembles `PixelBuddy.app`, signs it and installs it.
  `scripts/build_app.sh --release` builds a universal (Apple Silicon + Intel), ad-hoc signed
  app and leaves `build/PixelBuddy.zip` to attach to a GitHub release (`scripts/get.sh`
  downloads the latest one).
- `scripts/simulate.sh` plays fake sessions through every state, without real work.
- README images: `swift tools/make_readme_images.swift` (scenes and gallery) and
  `python3 tools/make_readme_gif.py` (animation).
- Optional: `scripts/create_signing_cert.sh` creates a local signing certificate so macOS
  keeps the Calendar permission across rebuilds (asks for your Mac password once).
- The hook script is `plugins/pixelbuddy/scripts/pixelbuddy-hook.sh`; the plugin catalog is
  `.claude-plugin/marketplace.json`.
- Windows version (C#, .NET 10, WinForms, no NuGet packages) in `windows/`: the app in
  `windows/PixelBuddy/`, the native hook in `windows/PixelBuddyHook/`. `PixelBuddy.exe --demo`
  plays fake sessions through every state.
- Releases: pushing a `v…` tag runs `.github/workflows/release.yml`, which builds the Mac and
  Windows zips on GitHub and creates a draft release.

PixelBuddy is an independent project, not affiliated with or endorsed by Anthropic.
Claude and Claude Code are trademarks of Anthropic.

---

## Español

Un pequeño compañero pixel art que vive encima del Dock de tu **Mac** o de la barra de tareas
de **Windows**. Te muestra de un vistazo qué está haciendo **Claude Code** y, si haces clic en
él, te enseña tu calendario. Puedes elegir entre **10 personajes**: ninja, astronauta,
programador, futbolista, baloncestista, piloto de F1, perro, bombero, estudiante y tenista.

> **¿Usas Windows?** Ve directamente a [En Windows](#en-windows).

<p align="center"><img src="docs/scene_es.png" alt="PixelBuddy encima del Dock: un personaje por cada sesión de Claude Code y el bocadillo del calendario"></p>

### Qué significa cada animación

| Si Claude Code… | Tu personaje… |
|---|---|
| está trabajando | corre, vuela, bota el balón o conduce de un lado a otro |
| ha terminado | lo celebra durante 30 segundos (katana, bandera, gol, canasta, A+…) |
| te hace una pregunta o necesita tu permiso | muestra una **?** azul |
| se ha parado por un error | muestra una **!** roja |
| espera tu siguiente mensaje | entrena, se estira, hace toques, lee… (1 minuto) |
| lleva un rato sin actividad | se duerme (Zzz) |

- **Varias sesiones a la vez.** Puedes tener abiertas varias sesiones de Claude Code en la
  misma carpeta de proyecto al mismo tiempo: cada una tiene su propio personaje y funcionan
  por separado. Uno puede estar trabajando mientras otro celebra y un tercero te pregunta
  algo. El color te dice qué sesión es cuál: la primera que abres es azul, la segunda roja y
  después verde, morada, naranja y amarilla (en el perro cambia la raza). Cuando están
  quietos se colocan uno al lado del otro, sin taparse.
- Al hacer clic en un personaje verás tu evento actual y el siguiente de hoy, sacados del
  Calendario de macOS. Sirve cualquier cuenta que tengas añadida en
  *Ajustes del Sistema › Cuentas de internet* (iCloud, Google…).
- Solo reacciona a las sesiones que abras en **tu carpeta de proyecto** (y sus subcarpetas).
  Funciona con repositorios git y con *worktrees*: una sesión en una worktree cuenta como si
  estuviera en el repositorio original.
- Es una app nativa (Swift en Mac, C# en Windows), gratuita y sin dependencias externas. La de
  Mac nunca se conecta a internet; la de Windows solo se conecta a Google, para leer tu
  calendario.

### Personajes

Elige el tuyo en la ventana de bienvenida o en la barra de menús (*Personaje ▸*). Cada sesión
nueva recibe el siguiente color; el perro cambia de raza (labrador, dálmata, husky, beagle,
salchicha y carlino) y lleva un collar del color de su sesión.

<p align="center"><img src="docs/gallery.png" width="640" alt="Los diez personajes con sus seis colores"></p>

### Qué necesitas

- macOS 14 Sonoma o posterior (Mac con Apple Silicon o Intel), o Windows 10/11
  ([mira En Windows](#en-windows)).
- Claude Code (la app de escritorio o la versión de terminal).

### Instalación en Mac

Elige una de estas tres formas. Las dos primeras no necesitan nada más.

**A. Con un solo comando (la más fácil).** Abre la app **Terminal** (está en
*Aplicaciones › Utilidades*), pega esto y pulsa Intro:

```bash
curl -fsSL https://raw.githubusercontent.com/rubenbenitoferrando-sketch/PixelBuddy/main/scripts/get.sh | bash
```

Descarga la app ya hecha desde [Releases](https://github.com/rubenbenitoferrando-sketch/PixelBuddy/releases/latest),
la instala en tu carpeta `~/Applications` y la abre.

**B. Descargando la app.**

1. Descarga **PixelBuddy.zip** desde [Releases](https://github.com/rubenbenitoferrando-sketch/PixelBuddy/releases/latest)
   y haz doble clic en él.
2. Arrastra **PixelBuddy.app** a tu carpeta Aplicaciones y ábrela.
3. La primera vez, macOS dice que no puede verificar al desarrollador (la app no está
   revisada por Apple, porque eso cuesta dinero). Pulsa **OK**, ve a
   *Ajustes del Sistema › Privacidad y seguridad*, baja hasta el final y pulsa
   **Abrir igualmente**. Solo hay que hacerlo una vez.

**C. Compilándola tú.** Necesitas las herramientas de línea de comandos de Xcode
(*Command Line Tools*): si no las tienes, escribe `xcode-select --install` en la Terminal;
no hace falta Xcode completo. Después copia estos tres comandos, uno detrás de otro:

```bash
git clone https://github.com/rubenbenitoferrando-sketch/PixelBuddy.git
cd PixelBuddy
./scripts/install.sh
```

- El primero descarga PixelBuddy en tu Mac.
- El segundo entra en la carpeta que se acaba de descargar.
- El tercero compila la app y la instala en tu carpeta `~/Applications`.

> Si solo tienes las *Command Line Tools* (sin Xcode completo), al compilar puede salir
> `warning: could not determine XCTest paths … unable to lookup item 'PlatformPath'`.
> Es solo un aviso y no pasa nada (XCTest sirve para pruebas que PixelBuddy no necesita):
> deja que el script termine.

Al abrir PixelBuddy por primera vez aparece la **ventana de bienvenida**, donde:

1. eliges tu personaje;
2. eliges tu carpeta de proyecto (la que abres en Claude Code) y marcas
   **Conectar Claude Code**. Así PixelBuddy añade sus avisos a tu archivo personal
   `.claude/settings.local.json`, después de hacer una copia de seguridad. Es el de esa
   carpeta o, si está dentro de un repositorio git, el de la raíz del repositorio, que es donde
   lo lee Claude Code. Ese archivo y sus copias quedan fuera de git;
3. si quieres, das acceso al Calendario.

Después se abre una guía corta, **Cómo funciona**, con todas las animaciones explicadas.
Por último, cierra y vuelve a abrir tus sesiones de Claude Code en esa carpeta para que tu
personaje empiece a reaccionar.

#### Alternativa: el plugin de Claude Code

En lugar de marcar *Conectar Claude Code*, puedes instalar los avisos como un plugin. Abre una
sesión de Claude Code en tu carpeta de proyecto y escribe:

```
/plugin marketplace add rubenbenitoferrando-sketch/PixelBuddy
/plugin install pixelbuddy@pixelbuddy
```

Cuando te pregunte dónde instalarlo, elige el alcance **local** (solo este proyecto y solo
para ti). La app sigue siendo necesaria: el plugin solo le cuenta lo que hace Claude Code.

### Uso

- **Clic en un personaje:** aparece un bocadillo con tu evento actual (y hasta qué hora dura)
  y el siguiente de hoy. Si no tienes nada, pone *Puedes descansar*. Se cierra con otro clic
  o a los 8 segundos.
- **✕ Quitar** (dentro del bocadillo): retira el personaje de una sesión que ya no usas. Si
  esa sesión vuelve a trabajar, el personaje reaparece. Cuando no hay ninguna sesión, queda
  un personaje principal para que siempre puedas consultar el calendario.
- **Icono de la barra de menús** (arriba a la derecha): cambiar de personaje, mostrarlos u
  ocultarlos, elegir calendarios, cambiar la carpeta vigilada, volver a la bienvenida, abrir
  la guía *Cómo funciona*, activar el arranque al iniciar sesión y salir.

### Privacidad

En Mac, todo se queda en tu Mac. Por cada sesión se guarda un archivo pequeño en
`~/Library/Application Support/PixelBuddy/sessions/` con el identificador de la sesión, la
carpeta, el tipo de aviso, el nombre de la herramienta y la hora. **Nunca** se guarda lo que
escribes ni tu código. Los avisos de otras carpetas se ignoran, el calendario se lee
directamente en tu Mac y la versión de Mac no se conecta a internet. Para Windows, mira
[En Windows](#en-windows). Todos los detalles en la
[política de privacidad](https://rubenbenitoferrando-sketch.github.io/PixelBuddy/privacy.html).

### Desinstalar

Desde la carpeta de PixelBuddy, en Terminal:

```bash
./scripts/uninstall.sh
```

Quita los avisos de tu carpeta de proyecto, el arranque al iniciar sesión, la app, sus datos y
sus ajustes (las copias de seguridad de `settings.local.json` se conservan). Si usaste el
plugin, escribe también en Claude Code: `/plugin uninstall pixelbuddy@pixelbuddy`.

### En Windows

Los mismos personajes y estados, encima de la barra de tareas de Windows (abajo a la
izquierda), y con tu **Google Calendar** en el bocadillo.

**Qué necesitas:** Windows 10 u 11 (Intel/AMD o ARM) y Claude Code. Nada más.

**Instalación**

1. Descarga desde [Releases](https://github.com/rubenbenitoferrando-sketch/PixelBuddy/releases/latest)
   **PixelBuddy-Windows-x64.zip** (la mayoría de PCs) o **PixelBuddy-Windows-ARM.zip** (PCs con
   chip Snapdragon u otro ARM).
2. Descomprímelo donde quieras (por ejemplo, en `Documentos`) y abre **PixelBuddy.exe**.
3. La primera vez, Windows dice *"Windows protegió tu PC"* (la app no está firmada, porque eso
   cuesta dinero): pulsa **Más información → Ejecutar de todos modos**.
4. En la ventana de bienvenida eliges tu personaje, tu carpeta de proyecto (marca
   **Conectar Claude Code**) y, si quieres, conectas Google Calendar. También se activa
   *Abrir al iniciar Windows*.

Cierra y vuelve a abrir tus sesiones de Claude Code en esa carpeta para que tu personaje
empiece a reaccionar. El icono de PixelBuddy está junto al reloj (quizá dentro de la flecha
**^**). Desde él puedes:
- cambiar de personaje y mostrarlos u ocultarlos;
- elegir los calendarios y la carpeta;
- volver a la bienvenida o abrir *Cómo funciona…*;
- activar o desactivar *Abrir al iniciar Windows*;
- desinstalar PixelBuddy o salir.

**Google Calendar.** *Calendarios → Conectar Google Calendar…* abre tu navegador: entra en tu
cuenta y acepta el permiso de solo lectura. Puede que Google avise de que la app aún no está
verificada; pulsa **Continuar**. Al hacer clic en un personaje verás tu evento actual y el
siguiente. No se muestran los eventos de todo el día, ni los cancelados, ni los que rechazaste.
En ese mismo menú eliges qué calendarios mostrar.

**Privacidad en Windows.** Los archivos de sesión se guardan en
`%LOCALAPPDATA%\PixelBuddy\sessions\`, como en Mac. La app solo se conecta a internet para
hablar con Google, y solo para leer tu calendario. Los eventos se quedan en memoria: nunca se
guardan ni se envían a ningún sitio. El permiso de Google se guarda cifrado con tu usuario de
Windows. Al desconectar se borra y se anula.

**Desinstalar.** En el menú, *Desinstalar PixelBuddy…* quita los avisos, el arranque con
Windows, el permiso de Google y los ajustes. Después borra la carpeta de la app. Si usaste el
plugin: `/plugin uninstall pixelbuddy@pixelbuddy`.

---

Licencia MIT. PixelBuddy es un proyecto independiente, sin relación con Anthropic ni
respaldado por ella. Claude y Claude Code son marcas de Anthropic.
