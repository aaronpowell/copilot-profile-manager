using Microsoft.Win32;
using CopilotProfileManager.App.Models;

namespace CopilotProfileManager.App.Services;

public sealed class RegistrySyncService
{
    private const string DirectoryMenuKey = @"Software\Classes\Directory\shell\Copilot";
    private const string BackgroundMenuKey = @"Software\Classes\Directory\Background\shell\Copilot";
    private readonly AppLogService appLogService = AppLogService.Instance;

    public void SyncProfiles(IEnumerable<CopilotProfile> profiles)
    {
        var orderedProfiles = profiles
            .Where(profile => profile.SyncRegistry)
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        appLogService.Write("Registry", $"Starting Explorer menu sync with {orderedProfiles.Count} profile(s).");
        RemoveMenu();

        if (orderedProfiles.Count == 0)
        {
            appLogService.Write("Registry", "No profiles are marked for Explorer sync. Existing Explorer menu entries were removed.");
            return;
        }

        CreateMenu(DirectoryMenuKey, orderedProfiles, "%1");
        CreateMenu(BackgroundMenuKey, orderedProfiles, "%V");
        appLogService.Write("Registry", $"Finished Explorer menu sync with {orderedProfiles.Count} profile(s).");
    }

    public void RemoveMenu()
    {
        appLogService.Write("Registry", $"Removing Explorer menu key '{DirectoryMenuKey}'.");
        Registry.CurrentUser.DeleteSubKeyTree(DirectoryMenuKey, false);
        appLogService.Write("Registry", $"Removing Explorer menu key '{BackgroundMenuKey}'.");
        Registry.CurrentUser.DeleteSubKeyTree(BackgroundMenuKey, false);
    }

    private void CreateMenu(string rootKeyPath, IReadOnlyList<CopilotProfile> profiles, string directoryToken)
    {
        appLogService.Write("Registry", $"Creating Explorer menu root '{rootKeyPath}' for token '{directoryToken}'.");
        using var rootKey = Registry.CurrentUser.CreateSubKey(rootKeyPath);
        ArgumentNullException.ThrowIfNull(rootKey);

        rootKey.SetValue("MUIVerb", "Copilot");
        rootKey.SetValue("SubCommands", string.Empty);
        rootKey.SetValue("Icon", ResolveExplorerIcon(profiles[0]));

        using var shellKey = rootKey.CreateSubKey("shell");
        ArgumentNullException.ThrowIfNull(shellKey);

        foreach (var profile in profiles)
        {
            using var profileKey = shellKey.CreateSubKey(GetMenuItemKey(profile));
            ArgumentNullException.ThrowIfNull(profileKey);

            profileKey.SetValue("MUIVerb", profile.Name);
            profileKey.SetValue("Icon", ResolveExplorerIcon(profile));

            using var commandKey = profileKey.CreateSubKey("command");
            ArgumentNullException.ThrowIfNull(commandKey);
            var command = BuildCommand(profile, directoryToken);
            commandKey.SetValue(string.Empty, command);
            appLogService.Write("Registry", $"Registered profile '{profile.Name}' ({profile.Guid:B}) with command '{command}'.");
        }
    }

    private static string GetMenuItemKey(CopilotProfile profile)
    {
        var key = profile.Name.Trim();
        return string.IsNullOrWhiteSpace(key) ? profile.Guid.ToString("N") : key;
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
