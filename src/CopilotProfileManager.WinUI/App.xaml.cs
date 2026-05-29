using Microsoft.UI.Xaml;
using CopilotProfileManager.App.Services;

namespace CopilotProfileManager.WinUI;

public partial class App : Application
{
    private readonly AppLogService appLogService = AppLogService.Instance;
    public static Window Window { get; private set; } = null!;

    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    public static nint WindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(Window);

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        appLogService.Write("App", "Application initialized.");
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Window = new MainWindow();
        DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        appLogService.Write("App", "Main window created.");
        Window.Activate();
        appLogService.Write("App", "Main window activated.");
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        appLogService.Write("App", $"WinUI unhandled exception. Handled={e.Handled}{Environment.NewLine}{e.Exception}");
    }

    private void OnCurrentDomainUnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
    {
        appLogService.Write(
            "App",
            $"AppDomain unhandled exception. IsTerminating={e.IsTerminating}{Environment.NewLine}{e.ExceptionObject}");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        appLogService.Write("App", $"Unobserved task exception:{Environment.NewLine}{e.Exception}");
    }
}
