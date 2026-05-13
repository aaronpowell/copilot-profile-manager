# Copilot Profile Manager

Copilot Profile Manager is a Windows desktop app for managing **GitHub Copilot CLI** profiles stored in **Windows Terminal** settings, syncing them to **Windows Terminal Preview** when installed, and publishing matching **Explorer context-menu** entries.

## What it does

- discovers Windows Terminal stable and Preview `settings.json` files
- loads Copilot-related Terminal profiles and merges them by **GUID**
- lets you create, clone, edit, and delete profiles in a WinForms UI
- splits the profile command into a **shell command prefix** and **Copilot CLI flags**
- reads `copilot --help` and exposes the CLI options in the UI for easier flag insertion
- syncs managed profiles back to Windows Terminal and/or Windows Terminal Preview
- writes a per-user `Copilot` submenu to Explorer for:
  - right-clicking a folder
  - right-clicking the background inside a folder

## UX notes

The app now makes the sync targets explicit:

- **Windows Terminal (stable)** means the profile is written to the regular Windows Terminal `settings.json`
- **Windows Terminal Preview** means the profile is written to the Preview build's `settings.json`
- **Explorer submenu** means the classic right-click `Copilot` submenu is created or removed

The app also shows the actual settings-file paths so it is obvious which install each checkbox maps to.

The Explorer entries launch:

```powershell
wt.exe --profile "{profile-guid}" -d "<selected directory>"
```

Using the profile **GUID** instead of the profile name means renames do not break the shell integration.

## Why C# WinForms?

F# would absolutely be a fun choice for the domain logic, but for a practical first pass this app is built in **C# WinForms** so the Windows UI and registry integration stay straightforward. If the project grows, it would be reasonable to move the sync/domain code into a separate class library and revisit F# there.

## Current architecture

- `src/CopilotProfileManager.App/MainForm.cs` — WinForms UI
- `src/CopilotProfileManager.App/Models` — profile, CLI option, metadata, and Terminal location models
- `src/CopilotProfileManager.App/Services/WindowsTerminalSettingsService.cs` — Windows Terminal discovery and DOM-based JSON merge/upsert
- `src/CopilotProfileManager.App/Services/RegistrySyncService.cs` — per-user Explorer submenu writer
- `src/CopilotProfileManager.App/Services/CopilotCliService.cs` — `copilot --help` inspection and flag parsing
- `src/CopilotProfileManager.App/Services/AppMetadataService.cs` — app-managed profile tracking for safe removals

## Explorer integration notes

This MVP writes classic registry-backed context menu entries under:

- `HKCU\Software\Classes\Directory\shell\CopilotProfileManager`
- `HKCU\Software\Classes\Directory\Background\shell\CopilotProfileManager`

That means it works without elevation, but on Windows 11 it still lands under **Show more options**.

## Removing the Explorer menu

You have two ways to remove the registry integration:

1. In the app, click **Remove Explorer menu now**
2. Or uncheck **Explorer submenu** for every profile and save

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

## Roadmap ideas

- add browse pickers for icon and background image paths
- support more Windows Terminal installation layouts
- show a diff preview before syncing
- manage new-tab menu groups as well as profiles
- add export/import for profile sets
- optionally add a COM shell extension companion for top-level Windows 11 menu support

## License

MIT
