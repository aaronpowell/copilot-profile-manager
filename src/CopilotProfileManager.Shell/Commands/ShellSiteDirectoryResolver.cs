using System.Runtime.InteropServices;
using CopilotProfileManager.Shell.Com;

namespace CopilotProfileManager.Shell.Commands;

internal static class ShellSiteDirectoryResolver
{
    public static string? TryResolveDirectoryPath(nint sitePointer)
    {
        if (sitePointer == nint.Zero)
        {
            return null;
        }

        var serviceProviderPointer = QueryInterface(sitePointer, typeof(CopilotProfileManager.Shell.Com.IServiceProvider).GUID);
        if (serviceProviderPointer == nint.Zero)
        {
            return null;
        }

        try
        {
            foreach (var serviceId in GetFolderViewServiceIds())
            {
                var folderViewPointer = QueryService(serviceProviderPointer, serviceId, typeof(IFolderView2).GUID);
                if (folderViewPointer == nint.Zero)
                {
                    continue;
                }

                try
                {
                    var shellItemPointer = GetFolder(folderViewPointer, typeof(IShellItem).GUID);
                    if (shellItemPointer == nint.Zero)
                    {
                        continue;
                    }

                    try
                    {
                        var path = GetDisplayName(shellItemPointer, ShellConstants.SigdnFileSysPath);
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            return path;
                        }
                    }
                    finally
                    {
                        Marshal.Release(shellItemPointer);
                    }
                }
                finally
                {
                    Marshal.Release(folderViewPointer);
                }
            }
        }
        finally
        {
            Marshal.Release(serviceProviderPointer);
        }

        return null;
    }

    private static IEnumerable<Guid> GetFolderViewServiceIds()
    {
        yield return ShellConstants.SidSFolderView;
        yield return ShellConstants.SidSFolderViewLegacy;
    }

    private static nint QueryService(nint serviceProviderPointer, in Guid serviceId, in Guid interfaceId)
    {
        unsafe
        {
            var vtable = *(nint**)serviceProviderPointer;
            var queryService = (delegate* unmanaged[Stdcall]<nint, Guid*, Guid*, nint*, int>)vtable[3];
            nint interfacePointer = nint.Zero;

            fixed (Guid* serviceIdPointer = &serviceId)
            fixed (Guid* interfaceIdPointer = &interfaceId)
            {
                nint* interfacePointerAddress = &interfacePointer;
                return queryService(serviceProviderPointer, serviceIdPointer, interfaceIdPointer, interfacePointerAddress) == ShellConstants.S_OK
                    ? interfacePointer
                    : nint.Zero;
            }
        }
    }

    private static nint GetFolder(nint folderViewPointer, in Guid interfaceId)
    {
        unsafe
        {
            var vtable = *(nint**)folderViewPointer;
            var getFolder = (delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)vtable[5];
            nint interfacePointer = nint.Zero;

            fixed (Guid* interfaceIdPointer = &interfaceId)
            {
                nint* interfacePointerAddress = &interfacePointer;
                return getFolder(folderViewPointer, interfaceIdPointer, interfacePointerAddress) == ShellConstants.S_OK
                    ? interfacePointer
                    : nint.Zero;
            }
        }
    }

    private static string? GetDisplayName(nint shellItemPointer, uint sigdnName)
    {
        unsafe
        {
            var vtable = *(nint**)shellItemPointer;
            var getDisplayName = (delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)vtable[5];
            nint namePointer = nint.Zero;

            try
            {
                return getDisplayName(shellItemPointer, sigdnName, &namePointer) == ShellConstants.S_OK && namePointer != nint.Zero
                    ? Marshal.PtrToStringUni(namePointer)
                    : null;
            }
            finally
            {
                if (namePointer != nint.Zero)
                {
                    Marshal.FreeCoTaskMem(namePointer);
                }
            }
        }
    }

    private static nint QueryInterface(nint unknownPointer, in Guid interfaceId)
    {
        var queryInterfaceId = interfaceId;
        return Marshal.QueryInterface(unknownPointer, in queryInterfaceId, out var interfacePointer) == ShellConstants.S_OK
            ? interfacePointer
            : nint.Zero;
    }
}
