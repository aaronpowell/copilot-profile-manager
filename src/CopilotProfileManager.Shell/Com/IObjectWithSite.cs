using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace CopilotProfileManager.Shell.Com;

[GeneratedComInterface]
[Guid("FC4801A3-2BA9-11CF-A229-00AA003D7352")]
internal partial interface IObjectWithSite
{
    [PreserveSig]
    int SetSite(nint punkSite);

    [PreserveSig]
    int GetSite(in Guid riid, out nint ppvSite);
}
