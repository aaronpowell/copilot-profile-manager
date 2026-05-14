# Copilot Profile Manager

Copilot Profile Manager is a **WinUI 3** Windows desktop app for managing **GitHub Copilot CLI** profiles stored in **Windows Terminal** settings, syncing them to **Windows Terminal Preview** when installed, and publishing matching **classic Explorer context-menu** entries.

## What it does

- discovers Windows Terminal stable and Preview `settings.json` files
- loads Copilot-related Terminal profiles and merges them by **GUID**
- lets you create, clone, edit, and delete profiles in a WinUI 3 desktop UI
- splits the profile command into a **shell command prefix** and **Copilot CLI flags**
- reads `copilot --help` and exposes the CLI options in the UI for easier flag insertion
- syncs managed profiles back to Windows Terminal and/or Windows Terminal Preview
- writes a per-user `Copilot` classic submenu to Explorer for:
  - right-clicking a folder
  - right-clicking the background inside a folder

## UX notes

The app now makes the sync targets explicit:

- **Windows Terminal (stable)** means the profile is written to the regular Windows Terminal `settings.json`
- **Windows Terminal Preview** means the profile is written to the Preview build's `settings.json`
- **Classic Explorer submenu** means the classic right-click `Copilot` submenu is created or removed

The app also shows the actual settings-file paths so it is obvious which install each checkbox maps to.

The Explorer entries launch:

```powershell
wt.exe --profile "{profile-guid}" -d "<selected directory>"
```

Using the profile **GUID** instead of the profile name means renames do not break the shell integration.

## Why C# and WinUI 3?

F# would still be a fun option for the domain layer, but the current app is built in **C# with WinUI 3** so it can use a modern Windows desktop UI without losing the existing Terminal and registry integration. If the project grows, it would still be reasonable to move the sync/domain code into a separate class library and revisit F# there.

## Current architecture

- `src/CopilotProfileManager.WinUI\App.xaml`, `MainWindow.xaml`, and `MainPage.xaml` — WinUI 3 shell and main application UI
- `src/CopilotProfileManager.WinUI\ViewModels\MainPageViewModel.cs` — orchestration layer for loading, editing, saving, and syncing profiles
- `src/CopilotProfileManager.Core` — reusable profile models plus Terminal, metadata, runtime, and Explorer shell services shared by the app and future shell-extension work
- `src/CopilotProfileManager.Shell` — shell-extension-facing menu models, COM contracts, root/leaf command implementations, and NativeAOT export scaffolding for a future packaged `IExplorerCommand` submenu
- `src/CopilotProfileManager.WinUI\Services\CopilotCliService.cs` — `copilot --help` inspection and flag parsing

## Explorer integration notes

This MVP writes classic registry-backed context menu entries under:

- `HKCU\Software\Classes\Directory\shell\CopilotProfileManager`
- `HKCU\Software\Classes\Directory\Background\shell\CopilotProfileManager`

That means it works without elevation, but on Windows 11 it still lands under **Show more options**.

If you run the app with **package identity** during development, the classic `HKCU\Software\Classes` writes can be redirected into the app package's private registry view instead of the real Explorer hive. In that case you will not see the keys under the usual RegEdit path and Explorer will not pick them up. Use the published unpackaged build (`dotnet publish ... -p:WindowsPackageType=None`) to test the classic Explorer integration end to end.

### Modern Explorer shell-extension status

The repository already contains the core command implementation for a future modern Windows 11 menu:

- explorer-enabled profiles are persisted explicitly
- the shell-launch logic is shared in the core library
- `CopilotProfileManager.Shell` contains the dynamic submenu model, COM interfaces, root/leaf `IExplorerCommand` implementations, class factory, and NativeAOT DLL export scaffolding

Pull requests publish the shell project with NativeAOT on `windows-latest`, sign an installable MSIX test layout with a self-signed certificate, and validate that the generated package manifest includes the COM server and `Directory` / `Directory\Background` Explorer registrations.

Tagged releases now ship two Windows artifacts:

- an unpackaged `win-x64` zip for the desktop app alone
- a packaged `win-x64` MSIX sideload zip that includes the Explorer shell extension, install scripts, and the public certificate needed for sideloading

### NativeAOT prerequisites for shell work on Windows

To publish the shell project successfully on Windows, install:

- the **.NET 10 SDK**
- **Visual Studio 2022** or **Build Tools for Visual Studio 2022**
- the **Desktop development with C++** workload (MSVC toolchain + Windows SDK), which NativeAOT uses at publish time

### Shell publish

```powershell
dotnet publish .\src\CopilotProfileManager.Shell\CopilotProfileManager.Shell.csproj --configuration Release --runtime win-x64 -o .\artifacts\shell\win-x64
```

If you are packaging the shell output into an Explorer-integrated package and then update that package, Explorer may continue holding onto the previous shell extension until it refreshes. If the updated command does not appear immediately, restart Explorer (or sign out/in) after the package update.

To install the packaged release or PR artifact, extract the layout zip/artifact and run `Install.ps1`. The package uses a self-signed certificate, so Windows may prompt to trust the included `.cer` file during installation.

## Removing the Explorer menu

You have two ways to remove the registry integration:

1. In the app, click **Remove Explorer menu now**
2. Or uncheck **Classic Explorer submenu** for every profile and save

The app writes the menu under `HKCU`, so removal is per-user and does not require elevation.

## What is the Flag helper page for?

It is not a separate feature area. It is just a reference/insertion helper for the **Copilot CLI flags** field on the main editor tab:

- pick a flag from the list
- read its description
- insert it into the currently selected profile

## Could this become a “real” Windows 11 top-level menu integration?

**Yes**, but not with registry keys alone.

To appear in the modern Windows 11 top-level context menu, the app would need a proper **COM shell extension**, typically implemented with `IExplorerCommand` (and related registration/plumbing) rather than the classic `Directory\shell` approach. That would mean:

1. shipping and registering a COM server
2. implementing shell command handlers for selected folders and folder backgrounds
3. handling registration, updates, uninstall, and architecture compatibility cleanly
4. likely moving the current registry sync logic behind a more formal install/registration layer

That is feasible in a production application, but it is a meaningfully bigger step than this MVP because Explorer shell extensions have a much higher bar for reliability, deployment, and compatibility testing.

## Build

```powershell
dotnet build .\CopilotProfileManager.slnx
```

## Release publish

```powershell
dotnet publish .\src\CopilotProfileManager.WinUI\CopilotProfileManager.WinUI.csproj --configuration Release --runtime win-x64 --self-contained false -p:WindowsPackageType=None -p:PublishSingleFile=false -o .\artifacts\publish\win-x64
```

## Bump version

```powershell
.\bump-version.ps1 0.3.0
```

This updates both `Directory.Build.props` and `src\CopilotProfileManager.WinUI\Package.appxmanifest`, normalizing three-part versions like `0.3.0` to `0.3.0.0`.

## Roadmap ideas

- add browse pickers for icon and background image paths
- support more Windows Terminal installation layouts
- show a diff preview before syncing
- manage new-tab menu groups as well as profiles
- add export/import for profile sets
- optionally add a COM shell extension companion for top-level Windows 11 menu support

## License

MIT
