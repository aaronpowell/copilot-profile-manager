namespace CopilotProfileManager.App.Models;

public sealed class CopilotProfile
{
    public Guid Guid { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "GitHub Copilot";

    public string ShellCommandPrefix { get; set; } = "pwsh.exe -c";

    public string CopilotArguments { get; set; } = string.Empty;

    public string ColorScheme { get; set; } = string.Empty;

    public string StartingDirectory { get; set; } = string.Empty;

    public string IconPath { get; set; } = string.Empty;

    public string BackgroundImagePath { get; set; } = string.Empty;

    public decimal? BackgroundImageOpacity { get; set; }

    public int? Opacity { get; set; }

    public string TabTitle { get; set; } = "GitHub Copilot";

    public bool Hidden { get; set; }

    public bool SyncStable { get; set; }

    public bool SyncPreview { get; set; }

    public bool SyncRegistry { get; set; }

    public string LastLoadedFrom { get; set; } = string.Empty;

    public string BuildCommandLine()
    {
        var prefix = ShellCommandPrefix.Trim();
        var args = CopilotArguments.Trim();

        return string.IsNullOrWhiteSpace(args)
            ? $"{prefix} copilot".Trim()
            : $"{prefix} copilot {args}".Trim();
    }

    public bool HasAnySyncTarget() => SyncStable || SyncPreview || SyncRegistry;

    public CopilotProfile Clone() =>
        new()
        {
            Guid = Guid.NewGuid(),
            Name = $"{Name} Copy",
            ShellCommandPrefix = ShellCommandPrefix,
            CopilotArguments = CopilotArguments,
            ColorScheme = ColorScheme,
            StartingDirectory = StartingDirectory,
            IconPath = IconPath,
            BackgroundImagePath = BackgroundImagePath,
            BackgroundImageOpacity = BackgroundImageOpacity,
            Opacity = Opacity,
            TabTitle = TabTitle,
            Hidden = Hidden,
            SyncStable = SyncStable,
            SyncPreview = SyncPreview,
            SyncRegistry = SyncRegistry,
            LastLoadedFrom = LastLoadedFrom,
        };

    public override string ToString() => Name;
}
