using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using CopilotProfileManager.Shell.Commands;

namespace CopilotProfileManager.Shell;

internal static class DllExports
{
    private static readonly StrategyBasedComWrappers ComWrappers = new();

    [UnmanagedCallersOnly(EntryPoint = "DllGetClassObject")]
    public static unsafe int DllGetClassObject(Guid* rclsid, Guid* riid, nint* ppv)
    {
        if (rclsid is null || riid is null || ppv is null)
        {
            return ShellConstants.E_FAIL;
        }

        *ppv = nint.Zero;
        if (*rclsid != ShellConstants.RootCommandGuid)
        {
            return ShellConstants.RegdbE_CLASSNOTREG;
        }

        var factoryPtr = ComWrappers.GetOrCreateComInterfaceForObject(
            new CopilotRootCommandFactory(),
            CreateComInterfaceFlags.None);

        var requestedInterface = *riid;
        var hr = Marshal.QueryInterface(factoryPtr, in requestedInterface, out var interfacePtr);
        Marshal.Release(factoryPtr);
        *ppv = interfacePtr;
        return hr;
    }

    [UnmanagedCallersOnly(EntryPoint = "DllCanUnloadNow")]
    public static int DllCanUnloadNow() => ShellConstants.S_OK;
}
