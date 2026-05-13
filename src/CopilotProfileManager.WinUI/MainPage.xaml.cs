using Microsoft.UI.Xaml.Controls;
using CopilotProfileManager.WinUI.ViewModels;

namespace CopilotProfileManager.WinUI;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; } = new();

    public MainPage()
    {
        DataContext = ViewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await ViewModel.LoadAsync();
    }
}
