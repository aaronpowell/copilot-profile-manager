using System.Runtime.InteropServices;
using System.Threading;

namespace CopilotProfileManager.Shell.Commands;

internal sealed class ShellSiteContext
{
    private nint sitePointer;

    public nint SitePointer => Interlocked.CompareExchange(ref sitePointer, nint.Zero, nint.Zero);

    public void SetSite(nint punkSite)
    {
        if (punkSite != nint.Zero)
        {
            Marshal.AddRef(punkSite);
        }

        var previousPointer = Interlocked.Exchange(ref sitePointer, punkSite);
        if (previousPointer != nint.Zero)
        {
            Marshal.Release(previousPointer);
        }
    }

    ~ShellSiteContext()
    {
        SetSite(nint.Zero);
    }
}
