# Copilot instructions

## Build, test, and lint commands

### Build

```powershell
dotnet build .\CopilotProfileManager.slnx
```

### Restore

```powershell
dotnet restore .\CopilotProfileManager.slnx
```

### Release publish

This is the same shape used by the tag-release workflow:

```powershell
dotnet publish .\src\CopilotProfileManager.WinUI\CopilotProfileManager.WinUI.csproj --configuration Release --runtime win-x64 --self-contained false -p:WindowsPackageType=None -p:PublishSingleFile=false -o .\artifacts\publish\win-x64
```

### Tests / single-test execution

There is currently **no test project** in this repository, so there is no single-test command yet.

### Lint

There is currently **no dedicated lint command** or analyzer workflow beyond the normal .NET build.

## High-level architecture

- The repository is currently a **single WinUI 3 desktop app** targeting `net10.0-windows10.0.26100.0`.
- `MainPageViewModel` is the orchestration layer for the desktop UI. It loads profiles on startup, binds the current profile into the editor, and coordinates all save/sync actions.
- `App.xaml`, `MainWindow.xaml`, and `MainPage.xaml` provide the WinUI app shell and editor surface.
- Domain logic is intentionally pushed into `Services`:
  - `WindowsTerminalSettingsService` discovers stable and Preview Windows Terminal `settings.json` locations, loads Copilot-related profiles, and syncs profile changes back into Terminal settings.
  - `RegistrySyncService` owns Explorer submenu creation/removal under per-user registry keys.
  - `CopilotCliService` shells out to `copilot --help` and parses the option list for the in-app flag helper.
  - `AppMetadataService` persists app-managed profile GUIDs in `%APPDATA%\CopilotProfileManager\managed-profiles.json` so the app can safely remove profiles it previously managed.
- `Models` contains the internal data shape used across the UI and services:
  - `CopilotProfile` is the main editable model.
  - `TerminalSettingsLocation` and `TerminalSettingsSnapshot` describe Terminal installs and loaded state.
  - `ProfileMetadata` tracks managed GUIDs.
- GitHub Actions are already wired:
  - `.github/workflows/pull-request.yml` restores and builds the solution on PRs.
  - `.github/workflows/release.yml` publishes the WinUI app for `win-x64`, zips the output, and creates a GitHub Release on tag push.

## Key conventions

- **Profile GUID is the source of truth.** Treat the Windows Terminal profile GUID as the stable identity. Do not key logic off profile names; renames should not break sync or Explorer launches.
- **Explorer commands launch by GUID, not name.** Registry entries use `wt.exe --profile "{guid}" -d ...` so profile renames stay safe.
- **`commandline` is edited indirectly.** The UI and model split Terminal commands into:
  - `ShellCommandPrefix` = everything before `copilot`
  - `CopilotArguments` = everything after `copilot`
  
  `CopilotProfile.BuildCommandLine()` is the canonical way to reassemble the final Terminal command string.
- **Terminal settings are merged, not replaced with POCO round-trips.** `WindowsTerminalSettingsService` uses `JsonNode`/DOM-style updates against the existing `settings.json` and only mutates the relevant `profiles.list` entries plus managed properties.
- **Only remove profiles the app owns.** The metadata file tracks managed GUIDs; sync logic uses that set so unchecked profiles can be removed safely without deleting unrelated user profiles.
- **Registry integration is per-user.** Explorer menu keys live under `HKCU\Software\Classes\...`, so changes should not require elevation and should stay scoped to the current user.
- **Stable and Preview Terminal are separate sync targets.** The UI and sync logic treat regular Windows Terminal and Windows Terminal Preview as distinct outputs with distinct `settings.json` files.
- **Copilot CLI option discovery is best-effort.** If `copilot --help` fails, the UI should still function; the flag helper is an assistive feature, not a hard dependency.
