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
        : this(null)
    {
    }

    public AppMetadataService(string? metadataPath)
    {
        this.metadataPath = string.IsNullOrWhiteSpace(metadataPath)
            ? GetDefaultMetadataPath()
            : metadataPath;
    }

    public string MetadataPath => metadataPath;

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

    private static string GetDefaultMetadataPath()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CopilotProfileManager");

        return Path.Combine(appDataPath, "managed-profiles.json");
    }
}
