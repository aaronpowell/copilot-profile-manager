using System.Runtime.InteropServices.Marshalling;
using CopilotProfileManager.Shell.Com;
using CopilotProfileManager.Shell.Services;

namespace CopilotProfileManager.Shell.Commands;

[GeneratedComClass]
internal sealed partial class CopilotRootCommand : IExplorerCommand, IObjectWithSite
{
    private readonly ExplorerCommandMenuBuilder menuBuilder;
    private readonly ShellSiteContext siteContext = new();

    public CopilotRootCommand()
        : this(new ExplorerCommandMenuBuilder())
    {
    }

    public CopilotRootCommand(ExplorerCommandMenuBuilder menuBuilder)
    {
        this.menuBuilder = menuBuilder;
    }

    public int GetTitle(IShellItemArray? psiItemArray, out string ppszName)
    {
        ppszName = "Copilot";
        return ShellConstants.S_OK;
    }

    public int GetIcon(IShellItemArray? psiItemArray, out string ppszIcon)
    {
        var firstIcon = menuBuilder.Build().Items.FirstOrDefault()?.IconPath;
        ppszIcon = firstIcon ?? string.Empty;
        return string.IsNullOrWhiteSpace(ppszIcon) ? ShellConstants.E_NOTIMPL : ShellConstants.S_OK;
    }

    public int GetToolTip(IShellItemArray? psiItemArray, out string ppszInfotip)
    {
        ppszInfotip = string.Empty;
        return ShellConstants.E_NOTIMPL;
    }

    public int GetCanonicalName(out Guid pguidCommandName)
    {
        pguidCommandName = ShellConstants.RootCommandGuid;
        return ShellConstants.S_OK;
    }

    public int GetState(IShellItemArray? psiItemArray, bool fOkToBeSlow, out uint pCmdState)
    {
        pCmdState = menuBuilder.Build().Items.Count > 0
            ? ShellConstants.EcsEnabled
            : ShellConstants.EcsHidden;
        return ShellConstants.S_OK;
    }

    public int Invoke(IShellItemArray? psiItemArray, IBindCtx? pbc) => ShellConstants.S_OK;

    public int GetFlags(out uint pFlags)
    {
        pFlags = ShellConstants.EcfHasSubCommands;
        return ShellConstants.S_OK;
    }

    public int EnumSubCommands(out IEnumExplorerCommand? ppEnum)
    {
        var commands = menuBuilder.Build().Items
            .Select(item => (IExplorerCommand)new CopilotProfileSubCommand(item, siteContext))
            .ToList();

        ppEnum = new EnumExplorerCommand(commands);
        return ShellConstants.S_OK;
    }

    public int SetSite(nint punkSite)
    {
        siteContext.SetSite(punkSite);
        return ShellConstants.S_OK;
    }

    public int GetSite(in Guid riid, out nint ppvSite)
    {
        ppvSite = siteContext.SitePointer;
        return ppvSite == nint.Zero ? ShellConstants.E_FAIL : ShellConstants.S_OK;
    }
}
