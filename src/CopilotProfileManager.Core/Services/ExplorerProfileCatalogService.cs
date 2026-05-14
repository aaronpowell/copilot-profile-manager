using CopilotProfileManager.App.Models;

namespace CopilotProfileManager.App.Services;

public sealed class ExplorerProfileCatalogService
{
    private readonly AppMetadataService metadataService;
    private readonly WindowsTerminalSettingsService terminalSettingsService;

    public ExplorerProfileCatalogService()
        : this(new AppMetadataService(), new WindowsTerminalSettingsService())
    {
    }

    public ExplorerProfileCatalogService(
        AppMetadataService metadataService,
        WindowsTerminalSettingsService terminalSettingsService)
    {
        this.metadataService = metadataService;
        this.terminalSettingsService = terminalSettingsService;
    }

    public IReadOnlyList<CopilotProfile> LoadExplorerProfiles()
    {
        var metadata = metadataService.Load();
        var explorerGuids = GetExplorerProfileGuids(metadata);
        if (explorerGuids.Count == 0)
        {
            return [];
        }

        var mergedProfiles = new Dictionary<Guid, CopilotProfile>();
        var locations = terminalSettingsService
            .DiscoverLocations()
            .Where(location => location.IsInstalled);

        foreach (var snapshot in locations.Select(terminalSettingsService.LoadSnapshot))
        {
            foreach (var profile in snapshot.Profiles)
            {
                if (!explorerGuids.Contains(profile.Guid))
                {
                    continue;
                }

                if (mergedProfiles.TryGetValue(profile.Guid, out var existing))
                {
                    existing.SyncStable |= profile.SyncStable;
                    existing.SyncPreview |= profile.SyncPreview;
                    existing.SyncRegistry = true;
                    continue;
                }

                profile.SyncRegistry = true;
                mergedProfiles[profile.Guid] = profile;
            }
        }

        return mergedProfiles.Values
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static HashSet<Guid> GetExplorerProfileGuids(ProfileMetadata metadata) =>
        metadata.ExplorerProfileGuids.Count > 0
            ? [.. metadata.ExplorerProfileGuids]
            : [.. metadata.ManagedProfileGuids];
}
