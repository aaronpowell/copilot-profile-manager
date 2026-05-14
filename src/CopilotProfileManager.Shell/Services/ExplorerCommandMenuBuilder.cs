using CopilotProfileManager.App.Services;
using CopilotProfileManager.Shell.Models;

namespace CopilotProfileManager.Shell.Services;

public sealed class ExplorerCommandMenuBuilder
{
    private readonly ExplorerProfileCatalogService explorerProfileCatalogService;

    public ExplorerCommandMenuBuilder()
        : this(new ExplorerProfileCatalogService())
    {
    }

    public ExplorerCommandMenuBuilder(ExplorerProfileCatalogService explorerProfileCatalogService)
    {
        this.explorerProfileCatalogService = explorerProfileCatalogService;
    }

    public ExplorerCommandMenu Build()
    {
        var items = explorerProfileCatalogService
            .LoadExplorerProfiles()
            .Select(profile => new ExplorerCommandMenuItem(
                profile,
                ExplorerShellCommandBuilder.ResolveExplorerIcon(profile)))
            .ToList();

        return new ExplorerCommandMenu("Copilot", items);
    }
}
