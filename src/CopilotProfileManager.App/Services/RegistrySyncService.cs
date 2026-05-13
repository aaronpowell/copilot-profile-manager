using Microsoft.Win32;
using CopilotProfileManager.App.Models;

namespace CopilotProfileManager.App.Services;

public sealed class RegistrySyncService
{
    private const string DirectoryMenuKey = @"Software\Classes\Directory\shell\CopilotProfileManager";
    private const string BackgroundMenuKey = @"Software\Classes\Directory\Background\shell\CopilotProfileManager";

    public void SyncProfiles(IEnumerable<CopilotProfile> profiles)
    {
        var orderedProfiles = profiles
            .Where(profile => profile.SyncRegistry)
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        RemoveMenu();

        if (orderedProfiles.Count == 0)
        {
            return;
        }

        CreateMenu(DirectoryMenuKey, orderedProfiles, "%1");
        CreateMenu(BackgroundMenuKey, orderedProfiles, "%V");
    }

    public void RemoveMenu()
    {
        Registry.CurrentUser.DeleteSubKeyTree(DirectoryMenuKey, false);
        Registry.CurrentUser.DeleteSubKeyTree(BackgroundMenuKey, false);
    }

    private static void CreateMenu(string rootKeyPath, IReadOnlyList<CopilotProfile> profiles, string directoryToken)
    {
        using var rootKey = Registry.CurrentUser.CreateSubKey(rootKeyPath);
        ArgumentNullException.ThrowIfNull(rootKey);

        rootKey.SetValue("MUIVerb", "Copilot");
        rootKey.SetValue("SubCommands", string.Empty);
        rootKey.SetValue("Icon", ResolveExplorerIcon(profiles[0]));

        using var shellKey = rootKey.CreateSubKey("shell");
        ArgumentNullException.ThrowIfNull(shellKey);

        foreach (var profile in profiles)
        {
            using var profileKey = shellKey.CreateSubKey(profile.Guid.ToString("N"));
            ArgumentNullException.ThrowIfNull(profileKey);

            profileKey.SetValue("MUIVerb", profile.Name);
            profileKey.SetValue("Icon", ResolveExplorerIcon(profile));

            using var commandKey = profileKey.CreateSubKey("command");
            ArgumentNullException.ThrowIfNull(commandKey);
            commandKey.SetValue(string.Empty, BuildCommand(profile, directoryToken));
        }
    }

    private static string BuildCommand(CopilotProfile profile, string directoryToken) =>
        $"wt.exe --profile \"{profile.Guid:B}\" -d \"{directoryToken}\"";

    private static string ResolveExplorerIcon(CopilotProfile profile)
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
