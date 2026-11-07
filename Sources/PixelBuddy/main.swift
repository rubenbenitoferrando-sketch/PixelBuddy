import AppKit
import ServiceManagement

// Órdenes de línea de comandos que usan install.sh y uninstall.sh:
//   PixelBuddy --install-hooks <carpeta>   vigila esa carpeta (y quita los hooks de la anterior)
//   PixelBuddy --uninstall-hooks <carpeta> quita solo los hooks de esa carpeta (p. ej. al usar el plugin)
//   PixelBuddy --uninstall                 quita los hooks y el arranque al iniciar sesión
Config.migrateFromNinjaDock()

let args = CommandLine.arguments
if args.count >= 2 {
    switch args[1] {
    case "--install-hooks":
        guard args.count >= 3 else { print("Falta la carpeta"); exit(2) }
        let folder = Config.normalize(args[2])
        do {
            if let old = Config.watchedFolder, Config.normalize(old) != folder {
                try? HookInstaller.uninstall(folder: old)
            }
            if HookInstaller.pluginEnabled(folder: folder) {
                print("✓ Esa carpeta ya usa el plugin pixelbuddy: no hacen falta hooks.")
            } else {
                try HookInstaller.install(folder: folder)
                print("✓ Hooks añadidos en \(HookInstaller.settingsURL(folder).path)")
            }
            Config.watchedFolder = folder
            exit(0)
        } catch {
            print("✗ \(error.localizedDescription)")
            exit(1)
        }
    case "--debug-paths":   // diagnóstico: --debug-paths <carpeta_vigilada> <cwd>
        guard args.count >= 4 else { print("Uso: --debug-paths <carpeta> <cwd>"); exit(2) }
        let (root, cwd) = (args[2], args[3])
        let mapped = Config.worktreeEquivalent(of: cwd)
        let watched = Config.isInside(cwd, root: root) || (mapped.map { Config.isInside($0, root: root) } ?? false)
        print("cuenta=\(watched ? "sí" : "no")  worktree→\(mapped ?? "-")  ajustes=\(HookInstaller.settingsURL(root).path)")
        exit(0)
    case "--uninstall-hooks":
        guard args.count >= 3 else { print("Falta la carpeta"); exit(2) }
        do {
            try HookInstaller.uninstall(folder: Config.normalize(args[2]))
            print("✓ Hooks quitados de \(HookInstaller.settingsURL(args[2]).path)")
            exit(0)
        } catch {
            print("✗ \(error.localizedDescription)")
            exit(1)
        }
    case "--uninstall":
        if let folder = Config.watchedFolder {
            do {
                try HookInstaller.uninstall(folder: folder)
                print("✓ Hooks quitados de \(HookInstaller.settingsURL(folder).path)")
            } catch {
                print("✗ \(error.localizedDescription)")
            }
        }
        try? SMAppService.mainApp.unregister()
        exit(0)
    default:
        break
    }
}

MainActor.assumeIsolated {
    let app = NSApplication.shared
    let delegate = AppDelegate()
    app.delegate = delegate
    app.setActivationPolicy(.accessory)
    withExtendedLifetime(delegate) { app.run() }
}
