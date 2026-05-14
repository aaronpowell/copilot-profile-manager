namespace CopilotProfileManager.Shell.Commands;

internal static class ShellConstants
{
    public const int S_OK = 0;
    public const int S_FALSE = 1;
    public const int E_FAIL = unchecked((int)0x80004005);
    public const int E_NOTIMPL = unchecked((int)0x80004001);
    public const int ClassE_NOAGGREGATION = unchecked((int)0x80040110);
    public const int RegdbE_CLASSNOTREG = unchecked((int)0x80040154);

    public const uint EcsEnabled = 0x0;
    public const uint EcsHidden = 0x2;

    public const uint EcfDefault = 0x0;
    public const uint EcfHasSubCommands = 0x1;

    public const uint SigdnFileSysPath = 0x80058000u;

    public static readonly Guid SidSFolderView = new("C3C1446D-BB26-46FA-B14B-E20A8DCFD19F");
    public static readonly Guid SidSFolderViewLegacy = new("CDE725B0-CCC9-4519-917E-325D72FAB4CE");
    public static readonly Guid RootCommandGuid = new("3AE4F373-B68B-470D-9A11-1814A6A595E1");
}
