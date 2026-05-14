using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace CopilotProfileManager.Shell.Com;

[GeneratedComInterface]
[Guid("1AF3A467-214F-4298-908E-06B03E0B39F9")]
internal partial interface IFolderView2
{
    [PreserveSig]
    int GetCurrentViewMode(out uint pViewMode);

    [PreserveSig]
    int SetCurrentViewMode(uint viewMode);

    [PreserveSig]
    int GetFolder(in Guid riid, out nint ppv);
}
