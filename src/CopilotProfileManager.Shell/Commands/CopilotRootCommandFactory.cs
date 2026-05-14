using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using CopilotProfileManager.Shell.Com;

namespace CopilotProfileManager.Shell.Commands;

[GeneratedComClass]
internal sealed partial class CopilotRootCommandFactory : IClassFactory
{
    private static readonly StrategyBasedComWrappers ComWrappers = new();

    public int CreateInstance(nint pUnkOuter, in Guid riid, out nint ppvObject)
    {
        ppvObject = nint.Zero;

        if (pUnkOuter != nint.Zero)
        {
            return ShellConstants.ClassE_NOAGGREGATION;
        }

        var commandPtr = ComWrappers.GetOrCreateComInterfaceForObject(
            new CopilotRootCommand(),
            CreateComInterfaceFlags.None);

        var interfaceId = riid;
        var hr = Marshal.QueryInterface(commandPtr, in interfaceId, out ppvObject);
        Marshal.Release(commandPtr);
        return hr;
    }

    public int LockServer(bool fLock) => ShellConstants.S_OK;
}
