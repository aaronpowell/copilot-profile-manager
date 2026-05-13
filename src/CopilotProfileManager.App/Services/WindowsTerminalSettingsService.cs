using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CopilotProfileManager.App.Models;

namespace CopilotProfileManager.App.Services;

public sealed class WindowsTerminalSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    private static readonly Regex CopilotRegex = new(@"\bcopilot\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public IReadOnlyList<TerminalSettingsLocation> DiscoverLocations()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var stablePath = Path.Combine(
            localAppData,
            "Packages",
            "Microsoft.WindowsTerminal_8wekyb3d8bbwe",
            "LocalState",
            "settings.json");

        var previewPath = Path.Combine(
            localAppData,
            "Packages",
            "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe",
            "LocalState",
            "settings.json");

        return
        [
            new TerminalSettingsLocation("stable", "Windows Terminal", stablePath, File.Exists(stablePath)),
            new TerminalSettingsLocation("preview", "Windows Terminal Preview", previewPath, File.Exists(previewPath)),
        ];
    }

    public TerminalSettingsSnapshot LoadSnapshot(TerminalSettingsLocation location)
    {
        if (!location.IsInstalled)
        {
            return new TerminalSettingsSnapshot(location, [], []);
        }

        var root = LoadDocument(location.SettingsPath);
        var profiles = new List<CopilotProfile>();

        foreach (var node in GetProfilesArray(root))
        {
            if (node is not JsonObject profileObject || !IsCopilotProfile(profileObject))
            {
                continue;
            }

            profiles.Add(ToProfile(profileObject, location));
        }

        var colorSchemes = GetColorSchemes(root);
        return new TerminalSettingsSnapshot(location, profiles, colorSchemes);
    }

    public void SyncProfiles(
        TerminalSettingsLocation location,
        IEnumerable<CopilotProfile> desiredProfiles,
        IEnumerable<Guid> managedProfileGuids)
    {
        if (!location.IsInstalled)
        {
            return;
        }

        var root = LoadDocument(location.SettingsPath);
        var profilesArray = EnsureProfilesArray(root);
        var desiredByGuid = desiredProfiles.ToDictionary(profile => NormalizeGuid(profile.Guid), StringComparer.OrdinalIgnoreCase);
        var managedGuidSet = managedProfileGuids
            .Select(NormalizeGuid)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = profilesArray.Count - 1; i >= 0; i--)
        {
            if (profilesArray[i] is not JsonObject profileObject)
            {
                continue;
            }

            var guid = profileObject["guid"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(guid))
            {
                continue;
            }

            if (managedGuidSet.Contains(guid) && !desiredByGuid.ContainsKey(guid))
            {
                profilesArray.RemoveAt(i);
            }
        }

        foreach (var entry in desiredByGuid)
        {
            var existing = profilesArray
                .OfType<JsonObject>()
                .FirstOrDefault(profile => string.Equals(profile["guid"]?.GetValue<string>(), entry.Key, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                existing = new JsonObject();
                profilesArray.Add(existing);
            }

            ApplyProfile(existing, entry.Value);
        }

        SaveDocument(location.SettingsPath, root);
    }

    public string DetermineDefaultShellPrefix(IEnumerable<CopilotProfile> profiles)
    {
        var prefix = profiles
            .Select(profile => profile.ShellCommandPrefix)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return string.IsNullOrWhiteSpace(prefix)
            ? "\"C:\\Program Files\\PowerShell\\7-preview\\pwsh.exe\" -c"
            : prefix.Trim();
    }

    private static JsonObject LoadDocument(string settingsPath)
    {
        var json = File.ReadAllText(settingsPath);
        return JsonNode.Parse(json, documentOptions: DocumentOptions)?.AsObject()
            ?? throw new InvalidOperationException($"Could not parse Windows Terminal settings file at '{settingsPath}'.");
    }

    private static JsonArray GetProfilesArray(JsonObject root) =>
        root["profiles"]?["list"]?.AsArray() ?? [];

    private static JsonArray EnsureProfilesArray(JsonObject root)
    {
        var profilesObject = root["profiles"] as JsonObject;
        if (profilesObject is null)
        {
            profilesObject = new JsonObject();
            root["profiles"] = profilesObject;
        }

        var profilesArray = profilesObject["list"] as JsonArray;
        if (profilesArray is null)
        {
            profilesArray = new JsonArray();
            profilesObject["list"] = profilesArray;
        }

        return profilesArray;
    }

    private static IReadOnlyList<string> GetColorSchemes(JsonObject root)
    {
        var schemes = root["schemes"] as JsonArray;
        if (schemes is null)
        {
            return [];
        }

        return schemes
            .OfType<JsonObject>()
            .Select(scheme => scheme["name"]?.GetValue<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsCopilotProfile(JsonObject profileObject)
    {
        var commandline = profileObject["commandline"]?.GetValue<string>() ?? string.Empty;
        var name = profileObject["name"]?.GetValue<string>() ?? string.Empty;
        return CopilotRegex.IsMatch(commandline) || name.Contains("Copilot", StringComparison.OrdinalIgnoreCase);
    }

    private static CopilotProfile ToProfile(JsonObject profileObject, TerminalSettingsLocation location)
    {
        var commandline = profileObject["commandline"]?.GetValue<string>() ?? string.Empty;
        var (shellCommandPrefix, copilotArguments) = SplitCommandline(commandline);

        return new CopilotProfile
        {
            Guid = ParseGuid(profileObject["guid"]?.GetValue<string>()),
            Name = profileObject["name"]?.GetValue<string>() ?? "GitHub Copilot",
            ShellCommandPrefix = shellCommandPrefix,
            CopilotArguments = copilotArguments,
            ColorScheme = profileObject["colorScheme"]?.GetValue<string>() ?? string.Empty,
            StartingDirectory = profileObject["startingDirectory"]?.GetValue<string>() ?? string.Empty,
            IconPath = profileObject["icon"]?.GetValue<string>() ?? string.Empty,
            BackgroundImagePath = profileObject["backgroundImage"]?.GetValue<string>() ?? string.Empty,
            BackgroundImageOpacity = TryGetDecimal(profileObject["backgroundImageOpacity"]),
            Opacity = profileObject["opacity"]?.GetValue<int>(),
            TabTitle = profileObject["tabTitle"]?.GetValue<string>() ?? string.Empty,
            Hidden = profileObject["hidden"]?.GetValue<bool>() ?? false,
            SyncStable = location.Key == "stable",
            SyncPreview = location.Key == "preview",
            LastLoadedFrom = location.DisplayName,
        };
    }

    private static void ApplyProfile(JsonObject profileObject, CopilotProfile profile)
    {
        profileObject["guid"] = NormalizeGuid(profile.Guid);
        profileObject["name"] = profile.Name.Trim();
        profileObject["commandline"] = profile.BuildCommandLine();
        profileObject["hidden"] = profile.Hidden;

        SetOrRemove(profileObject, "colorScheme", profile.ColorScheme);
        SetOrRemove(profileObject, "startingDirectory", profile.StartingDirectory);
        SetOrRemove(profileObject, "icon", profile.IconPath);
        SetOrRemove(profileObject, "backgroundImage", profile.BackgroundImagePath);
        SetOrRemove(profileObject, "tabTitle", profile.TabTitle);

        if (profile.BackgroundImageOpacity.HasValue)
        {
            profileObject["backgroundImageOpacity"] = profile.BackgroundImageOpacity.Value;
        }
        else
        {
            profileObject.Remove("backgroundImageOpacity");
        }

        if (profile.Opacity.HasValue)
        {
            profileObject["opacity"] = profile.Opacity.Value;
        }
        else
        {
            profileObject.Remove("opacity");
        }
    }

    private static void SetOrRemove(JsonObject profileObject, string propertyName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            profileObject.Remove(propertyName);
            return;
        }

        profileObject[propertyName] = value.Trim();
    }

    private static Guid ParseGuid(string? value) =>
        Guid.TryParse(value, out var guid) ? guid : Guid.NewGuid();

    private static string NormalizeGuid(Guid guid) => guid.ToString("B");

    private static (string ShellCommandPrefix, string CopilotArguments) SplitCommandline(string commandline)
    {
        var match = CopilotRegex.Match(commandline);
        if (!match.Success)
        {
            return (commandline.Trim(), string.Empty);
        }

        var prefix = commandline[..match.Index].Trim();
        var args = commandline[(match.Index + match.Length)..].Trim();
        return (prefix, args);
    }

    private static decimal? TryGetDecimal(JsonNode? node)
    {
        if (node is not JsonValue value)
        {
            return null;
        }

        if (value.TryGetValue<decimal>(out var decimalValue))
        {
            return decimalValue;
        }

        if (value.TryGetValue<double>(out var doubleValue))
        {
            return (decimal)doubleValue;
        }

        return null;
    }

    private static void SaveDocument(string settingsPath, JsonObject root)
    {
        var tempPath = $"{settingsPath}.tmp";
        var backupPath = $"{settingsPath}.bak";

        var json = root.ToJsonString(JsonOptions);
        File.WriteAllText(tempPath, json);

        if (File.Exists(settingsPath))
        {
            File.Copy(settingsPath, backupPath, true);
        }

        File.Move(tempPath, settingsPath, true);
    }
}
