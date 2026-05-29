using System.Text.Json;
using CopilotProfileManager.App.Models;

namespace CopilotProfileManager.App.Services;

public sealed class AppMetadataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly AppLogService appLogService = AppLogService.Instance;
    private readonly string metadataPath;

    public AppMetadataService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CopilotProfileManager");

        metadataPath = Path.Combine(appDataPath, "managed-profiles.json");
    }

    public ProfileMetadata Load()
    {
        if (!File.Exists(metadataPath))
        {
            appLogService.Write("Metadata", $"No metadata file found at '{metadataPath}'. Starting with an empty managed profile set.");
            return new ProfileMetadata();
        }

        var json = File.ReadAllText(metadataPath);
        var metadata = JsonSerializer.Deserialize<ProfileMetadata>(json, JsonOptions) ?? new ProfileMetadata();
        appLogService.Write("Metadata", $"Loaded {metadata.ManagedProfileGuids.Count} managed profile GUID(s) from '{metadataPath}'.");
        return metadata;
    }

    public void Save(ProfileMetadata metadata)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, JsonOptions));
        appLogService.Write("Metadata", $"Saved {metadata.ManagedProfileGuids.Count} managed profile GUID(s) to '{metadataPath}'.");
    }
}
