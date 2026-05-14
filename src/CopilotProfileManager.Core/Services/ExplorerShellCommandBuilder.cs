using CopilotProfileManager.App.Models;

namespace CopilotProfileManager.App.Services;

public static class ExplorerShellCommandBuilder
{
    public static string BuildWindowsTerminalArguments(CopilotProfile profile, string directoryToken) =>
        $"--profile \"{profile.Guid:B}\" -d \"{directoryToken}\"";

    public static string BuildWindowsTerminalCommand(CopilotProfile profile, string directoryToken) =>
        $"wt.exe {BuildWindowsTerminalArguments(profile, directoryToken)}";

    public static string ResolveExplorerIcon(CopilotProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.IconPath))
        {
            var iconPath = profile.IconPath.Trim();
            if (Path.GetExtension(iconPath).Equals(".ico", StringComparison.OrdinalIgnoreCase))
            {
                return iconPath;
            }

            var icoPath = Path.ChangeExtension(iconPath, ".ico");
            if (File.Exists(icoPath))
            {
                return icoPath;
            }
        }

        return "wt.exe";
    }
}
