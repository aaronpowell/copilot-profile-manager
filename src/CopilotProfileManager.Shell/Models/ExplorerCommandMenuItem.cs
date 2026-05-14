using CopilotProfileManager.App.Models;

namespace CopilotProfileManager.Shell.Models;

public sealed record ExplorerCommandMenuItem(
    CopilotProfile Profile,
    string IconPath)
{
    public Guid ProfileGuid => Profile.Guid;

    public string Title => Profile.Name;
}
