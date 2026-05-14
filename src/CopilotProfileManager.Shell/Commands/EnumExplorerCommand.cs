using System.Runtime.InteropServices.Marshalling;
using CopilotProfileManager.Shell.Com;

namespace CopilotProfileManager.Shell.Commands;

[GeneratedComClass]
internal sealed partial class EnumExplorerCommand : IEnumExplorerCommand
{
    private readonly IReadOnlyList<IExplorerCommand> commands;
    private int currentIndex;

    public EnumExplorerCommand(IReadOnlyList<IExplorerCommand> commands)
    {
        this.commands = commands;
    }

    public int Next(uint celt, IExplorerCommand[] rgelt, out uint pceltFetched)
    {
        pceltFetched = 0;

        while (currentIndex < commands.Count && pceltFetched < celt && pceltFetched < rgelt.Length)
        {
            rgelt[pceltFetched] = commands[currentIndex];
            pceltFetched++;
            currentIndex++;
        }

        return pceltFetched == celt ? ShellConstants.S_OK : ShellConstants.S_FALSE;
    }

    public int Skip(uint celt)
    {
        currentIndex = Math.Min(currentIndex + (int)celt, commands.Count);
        return ShellConstants.S_OK;
    }

    public int Reset()
    {
        currentIndex = 0;
        return ShellConstants.S_OK;
    }

    public int Clone(out IEnumExplorerCommand? ppenum)
    {
        var clone = new EnumExplorerCommand(commands)
        {
            currentIndex = currentIndex,
        };

        ppenum = clone;
        return ShellConstants.S_OK;
    }
}
