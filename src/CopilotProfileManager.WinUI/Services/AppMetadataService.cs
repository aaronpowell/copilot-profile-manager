using System.Text.Json;
using CopilotProfileManager.App.Models;

namespace CopilotProfileManager.App.Services;

public sealed class AppMetadataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

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
            return new ProfileMetadata();
        }

        var json = File.ReadAllText(metadataPath);
        return JsonSerializer.Deserialize<ProfileMetadata>(json, JsonOptions) ?? new ProfileMetadata();
    }

    public void Save(ProfileMetadata metadata)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, JsonOptions));
    }
}
