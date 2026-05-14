using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace CopilotProfileManager.Shell.Com;

[GeneratedComInterface]
[Guid("A88826F8-186F-4987-AADE-EA0CEF8FBFE8")]
internal partial interface IEnumExplorerCommand
{
    [PreserveSig]
    int Next(
        uint celt,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IExplorerCommand[] rgelt,
        out uint pceltFetched);

    [PreserveSig]
    int Skip(uint celt);

    [PreserveSig]
    int Reset();

    [PreserveSig]
    int Clone(out IEnumExplorerCommand? ppenum);
}
