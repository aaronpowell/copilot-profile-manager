namespace CopilotProfileManager.Shell.Models;

public sealed record ExplorerCommandMenu(
    string Title,
    IReadOnlyList<ExplorerCommandMenuItem> Items);
