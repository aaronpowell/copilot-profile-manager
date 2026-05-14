using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace CopilotProfileManager.Shell.Com;

[GeneratedComInterface]
[Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
internal partial interface IShellItemArray
{
    [PreserveSig]
    int BindToHandler(nint pbc, in Guid bhid, in Guid riid, out nint ppv);

    [PreserveSig]
    int GetPropertyStore(int flags, in Guid riid, out nint ppv);

    [PreserveSig]
    int GetPropertyDescriptionList(nint keyType, in Guid riid, out nint ppv);

    [PreserveSig]
    int GetAttributes(uint attribFlags, uint sfgaoMask, out uint psfgaoAttribs);

    [PreserveSig]
    int GetCount(out uint pdwNumItems);

    [PreserveSig]
    int GetItemAt(uint dwIndex, out IShellItem ppsi);

    [PreserveSig]
    int EnumItems(out nint ppenumShellItems);
}
