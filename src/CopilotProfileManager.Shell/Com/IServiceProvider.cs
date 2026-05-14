using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace CopilotProfileManager.Shell.Com;

[GeneratedComInterface]
[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
internal partial interface IServiceProvider
{
    [PreserveSig]
    int QueryService(in Guid guidService, in Guid riid, out nint ppvObject);
}
