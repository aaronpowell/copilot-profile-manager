namespace CopilotProfileManager.App.Models;

public sealed record TerminalSettingsSnapshot(
    TerminalSettingsLocation Location,
    IReadOnlyList<CopilotProfile> Profiles,
    IReadOnlyList<string> ColorSchemes);
