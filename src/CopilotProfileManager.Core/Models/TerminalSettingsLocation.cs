namespace CopilotProfileManager.App.Models;

public sealed record TerminalSettingsLocation(
    string Key,
    string DisplayName,
    string SettingsPath,
    bool IsInstalled);
