using System.Diagnostics;
using System.Runtime.InteropServices.Marshalling;
using CopilotProfileManager.App.Services;
using CopilotProfileManager.Shell.Com;
using CopilotProfileManager.Shell.Models;

namespace CopilotProfileManager.Shell.Commands;

[GeneratedComClass]
internal sealed partial class CopilotProfileSubCommand : IExplorerCommand
{
    private readonly ExplorerCommandMenuItem item;
    private readonly ShellSiteContext siteContext;

    public CopilotProfileSubCommand(ExplorerCommandMenuItem item, ShellSiteContext siteContext)
    {
        this.item = item;
        this.siteContext = siteContext;
    }

    public int GetTitle(IShellItemArray? psiItemArray, out string ppszName)
    {
        ppszName = item.Title;
        return ShellConstants.S_OK;
    }

    public int GetIcon(IShellItemArray? psiItemArray, out string ppszIcon)
    {
        ppszIcon = item.IconPath;
        return string.IsNullOrWhiteSpace(ppszIcon) ? ShellConstants.E_NOTIMPL : ShellConstants.S_OK;
    }

    public int GetToolTip(IShellItemArray? psiItemArray, out string ppszInfotip)
    {
        ppszInfotip = string.Empty;
        return ShellConstants.E_NOTIMPL;
    }

    public int GetCanonicalName(out Guid pguidCommandName)
    {
        pguidCommandName = item.ProfileGuid;
        return ShellConstants.S_OK;
    }

    public int GetState(IShellItemArray? psiItemArray, bool fOkToBeSlow, out uint pCmdState)
    {
        pCmdState = ShellConstants.EcsEnabled;
        return ShellConstants.S_OK;
    }

    public int Invoke(IShellItemArray? psiItemArray, IBindCtx? pbc)
    {
        var directoryPath = ResolveDirectoryPath(psiItemArray);
        var arguments = ExplorerShellCommandBuilder.BuildWindowsTerminalArguments(item.Profile, directoryPath);

        Process.Start(new ProcessStartInfo("wt.exe", arguments)
        {
            UseShellExecute = true,
        });

        return ShellConstants.S_OK;
    }

    public int GetFlags(out uint pFlags)
    {
        pFlags = ShellConstants.EcfDefault;
        return ShellConstants.S_OK;
    }

    public int EnumSubCommands(out IEnumExplorerCommand? ppEnum)
    {
        ppEnum = null;
        return ShellConstants.E_NOTIMPL;
    }

    private string ResolveDirectoryPath(IShellItemArray? psiItemArray)
    {
        if (psiItemArray is not null &&
            psiItemArray.GetCount(out var count) == ShellConstants.S_OK &&
            count > 0 &&
            psiItemArray.GetItemAt(0, out var itemAt) == ShellConstants.S_OK &&
            itemAt.GetDisplayName(ShellConstants.SigdnFileSysPath, out var path) == ShellConstants.S_OK &&
            !string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        return ShellSiteDirectoryResolver.TryResolveDirectoryPath(siteContext.SitePointer)
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}
