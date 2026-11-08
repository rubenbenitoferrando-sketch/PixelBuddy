# PixelBuddy — pixel art buddies for macOS and Windows that react to Claude Code

Native macOS app (Swift, no dependencies) that shows a small pixel art character above the
Dock (bottom-left). It reacts to what Claude Code is doing and shows the current calendar
event when clicked. 10 characters to choose from. A Windows version (C#, see "Windows" below)
does the same above the taskbar, with the same assets, states and rules.

## Privacy rule (both layers are required)
The buddy reacts ONLY to Claude Code sessions opened in the watched project folder (and its
subfolders). Everything else is ignored.
1. Hooks are only enabled for that project, never in `~/.claude/settings.json` nor in the
   project's shared `.claude/settings.json`. Two ways:
   - The `pixelbuddy` Claude Code plugin (`plugins/pixelbuddy/`, catalog in
     `.claude-plugin/marketplace.json`) installed with **local** scope.
   - The app's "Connect Claude Code" / `install.sh <folder>` writes the hooks into the
     personal `.claude/settings.local.json` Claude Code actually reads: the folder's own, or
     the git repository root's when the folder is inside a repo (Claude Code reads the local
     file from the repo root, also from subfolders and worktrees; not when the root is $HOME).
     Backed up first; the file and its backups go to `.git/info/exclude`. Skipped when the
     plugin is already enabled.
   There is a single hook script: `plugins/pixelbuddy/scripts/pixelbuddy-hook.sh`.
2. The app stores the watched folder and drops any event whose `cwd` is outside it. A `cwd`
   inside a git worktree is first mapped to the same path in the main checkout (read from the
   worktree's `.git` file → `commondir`), so worktree sessions count as the original repo.
   `PixelBuddy --debug-paths <folder> <cwd>` prints how a path is classified.
The hook writes only session id, cwd, event, tool name / notification type and time to
`~/Library/Application Support/PixelBuddy/sessions/<session_id>.json` (dir 700, files 600).
It must be instant, print nothing (UserPromptSubmit stdout goes into Claude's context) and
always exit 0. No network access anywhere, with one exception approved by the owner: the
Windows version talks to Google (OAuth + Calendar API, read-only) for Google Calendar.

## Decisions already made (don't change without asking)
- Characters live in `assets/avatars/<id>/`: `<prefix>_<variant>_<state>.png` (32×32 frames
  in a horizontal strip) + `manifest.json` (id, prefix, name es/en, variants, and per state
  frames, fps, loopFrom, frameSize, anchorX). All characters use the same state keys.
  Scale ×2 with nearest-neighbour (no smoothing). Always align by `anchorX` when switching
  states. `run` uses 48×32 frames (trail behind); signs and props use 40×32.
  - `ninja`: `tools/make_sprites.py`; `astronauta`: `tools/make_astronaut.py`.
  - The other 8 (`tools/make_characters.py`, code in `tools/chars/`, shared base
    `tools/chars/base.py`): programador (laptop), futbolista (shirt), baloncestista
    (shirt), piloto F1 (car), perro (breed: labrador, dálmata, husky, beagle, salchicha,
    carlino; collar in the session color), bombero (uniform), estudiante (glasses),
    tenista (racket). In brackets: what changes per session. No real brands, crests or teams.
  - A ninja is placed by its color *index*; the variant name is looked up in the current
    character at render time (variants aren't always colors).
- Facing right by default; flip horizontally when running left.
- States (Claude Code hooks):
  - `run`: UserPromptSubmit, PreToolUse, PostToolUse → working; moves across the strip.
  - `katana` (celebrate): Stop. Plays once, then loops from `loopFrom`. 30 s → `train`.
  - `ask` (blue ?): PreToolUse of `AskUserQuestion`, PermissionRequest, Notification of type
    `permission_prompt` / `elicitation_dialog` / `agent_needs_input`. Never sleeps.
  - `alert` (red !): StopFailure. Never sleeps.
  - `train` (waiting for your message): after the 30 s of `katana`, or any other
    Notification (the desktop app doesn't send `idle_prompt`, so don't rely on it).
    Doesn't override `ask`/`alert`. After 1 min → `sleep`.
  - `idle`: no activity (mostly the main buddy). `sleep`: 10 min without events.
  - SessionEnd → that buddy disappears. Sessions without events for 2 h are removed.
    When the Claude desktop app quits, all session buddies are removed.
- One buddy per session (`session_id`). Colour order: azul, rojo, verde, morado, naranja,
  amarillo (then repeat). With no sessions a main buddy stays (sleeps after 2 min without
  Claude activity, remembered across restarts) and can't be removed. "✕ Remove" in a
  session buddy's bubble deletes its session file; it comes back if the session works again.
- Click → pixel bubble (2 px border, monospaced font): NOW (title + end time, or "You can
  relax") and NEXT (only today). Closes on another click or after 8 s.
- Calendar: EventKit (full access). iCloud/Google accounts from System Settings work.
- Welcome window on first launch (character, folder + connect, calendar), then the animated
  "How it works" guide. Both also in the menu bar menu. UI strings es/en in `Strings.swift`.
- Starts at login (SMAppService). Menu bar icon. Adaptive redraw (60 Hz only while running).
- Name PixelBuddy (formerly NinjaDock: settings are migrated, `install.sh` removes the old
  app). Data in `~/Library/Application Support/PixelBuddy/`.
- Signing: local certificate "PixelBuddy Local" (or "NinjaDock Local") if present, else
  ad-hoc. Zero cost, no paid services, no external dependencies.

## Windows (`windows/`)
- C# / .NET 10 / WinForms, no NuGet packages. `windows/build.sh` builds self-contained
  single-file zips (x64 + ARM) into `windows/dist/` (also from the Mac: .NET SDK in `~/.dotnet`).
  Keep it behaviour-identical to the Mac app (same states, timers, colours, bubble, privacy).
- Layered per-pixel-alpha window above the taskbar (`StageWindow`, `UpdateLayeredWindow`);
  clicks pass through except on buddies (alpha-1 hit area) and the bubble. Tray icon menu.
  Sprites scaled by an integer factor (≈2 × display scale) with nearest-neighbour.
- Data in `%LOCALAPPDATA%\PixelBuddy\` (settings.json, sessions\, google-token.bin,
  pixelbuddy-hook.exe copy). Open at sign-in via HKCU Run.
- Hook: `windows/PixelBuddyHook` (Native AOT console exe, same JSON as the Mac hook).
  "Connect Claude Code" writes **exec-form** hooks (`command` = hook exe path, `args` = event,
  `onFailure: continue`) so no Git Bash/PowerShell is needed and a failure never blocks Claude.
  The shared plugin script detects Windows (`$WINDIR`) and execs the hook exe from
  `%LOCALAPPDATA%\PixelBuddy\`; all plugin hooks have `onFailure: continue`.
- Google Calendar: OAuth desktop flow with PKCE and a 127.0.0.1 loopback listener, scope
  `calendar.readonly`. Events only in memory; refresh token encrypted with DPAPI. The OAuth
  client id/secret are injected at build time (`windows/google-oauth.env`, gitignored, or the
  repo secrets in `.github/workflows/release.yml`) — never commit them. Privacy policy:
  `docs/privacy.html` (GitHub Pages); keep it in sync with what the app does.
- Releases: a `v…` tag runs `.github/workflows/release.yml` (Mac + Windows zips → draft
  release). Every release must include `PixelBuddy.zip` (Mac): `scripts/get.sh` downloads it
  from the latest release.

@privado/CLAUDE-personal.md
