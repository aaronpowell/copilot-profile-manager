using System.Runtime.InteropServices;

namespace CopilotProfileManager.App.Services;

public static class RuntimeEnvironmentService
{
    private const int ErrorInsufficientBuffer = 122;
    private const int AppModelErrorNoPackage = 15700;

    public static bool HasPackageIdentity()
    {
        uint packageFullNameLength = 0;
        var result = GetCurrentPackageFullName(ref packageFullNameLength, nint.Zero);

        return result switch
        {
            0 => true,
            ErrorInsufficientBuffer => true,
            AppModelErrorNoPackage => false,
            _ => false,
        };
    }

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, nint packageFullName);
}
