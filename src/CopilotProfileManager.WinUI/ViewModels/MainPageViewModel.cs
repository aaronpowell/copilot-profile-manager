using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotProfileManager.App.Models;
using CopilotProfileManager.App.Services;

namespace CopilotProfileManager.WinUI.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly AppMetadataService metadataService = new();
    private readonly WindowsTerminalSettingsService terminalSettingsService = new();
    private readonly RegistrySyncService registrySyncService = new();
    private readonly CopilotCliService copilotCliService = new();

    private ProfileMetadata metadata = new();
    private List<TerminalSettingsLocation> locations = [];
    private HashSet<Guid> deletedProfileGuids = [];
    private bool stableInstalled;
    private bool previewInstalled;
    private bool isApplyingSelection;
    private string defaultShellPrefix = "\"C:\\Program Files\\PowerShell\\7-preview\\pwsh.exe\" -c";

    [ObservableProperty]
    public partial ObservableCollection<CopilotProfile> Profiles { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<CopilotCliOption> CliOptions { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<string> ColorSchemes { get; set; } = [];

    [ObservableProperty]
    public partial CopilotProfile? SelectedProfile { get; set; }

    [ObservableProperty]
    public partial CopilotCliOption? SelectedCliOption { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GuidText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ShellCommandPrefix { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CopilotArguments { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ColorScheme { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StartingDirectory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string IconPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BackgroundImagePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double BackgroundOpacity { get; set; }

    [ObservableProperty]
    public partial double WindowOpacity { get; set; }

    [ObservableProperty]
    public partial string TabTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool Hidden { get; set; }

    [ObservableProperty]
    public partial bool SyncStable { get; set; }

    [ObservableProperty]
    public partial bool SyncPreview { get; set; }

    [ObservableProperty]
    public partial bool SyncRegistry { get; set; }

    [ObservableProperty]
    public partial string CommandPreview { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ActivityLog { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StableSettingsPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PreviewSettingsPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedCliOptionDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string ExplorerIntegrationDescription { get; set; } =
        "Adds or removes the classic Explorer submenu for folders and folder backgrounds. On Windows 11 it appears under Show more options.";

    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            var hasPackageIdentity = RuntimeEnvironmentService.HasPackageIdentity();
            metadata = metadataService.Load();
            if (metadata.ExplorerProfileGuids.Count == 0 && metadata.ManagedProfileGuids.Count > 0)
            {
                metadata.ExplorerProfileGuids = [.. metadata.ManagedProfileGuids];
            }

            locations = terminalSettingsService.DiscoverLocations().ToList();
            stableInstalled = locations.Any(location => location.Key == "stable" && location.IsInstalled);
            previewInstalled = locations.Any(location => location.Key == "preview" && location.IsInstalled);
            ExplorerIntegrationDescription = hasPackageIdentity
                ? "Adds or removes the classic Explorer submenu for folders and folder backgrounds. This run has package identity, so Explorer registry writes are redirected into the app package and will not appear in the real Explorer menu or under HKCU\\Software\\Classes in RegEdit. Use the published unpackaged build to test Explorer integration."
                : "Adds or removes the classic Explorer submenu for folders and folder backgrounds. On Windows 11 it appears under Show more options.";

            var snapshots = locations
                .Where(location => location.IsInstalled)
                .Select(terminalSettingsService.LoadSnapshot)
                .ToList();

            var mergedProfiles = MergeProfiles(snapshots);
            defaultShellPrefix = terminalSettingsService.DetermineDefaultShellPrefix(mergedProfiles);
            deletedProfileGuids = [];

            Profiles = new ObservableCollection<CopilotProfile>(mergedProfiles);
            ColorSchemes = new ObservableCollection<string>(snapshots
                .SelectMany(snapshot => snapshot.ColorSchemes)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase));

            var cliOptionsResult = await CopilotCliService.GetOptionsAsync();
            CliOptions = new ObservableCollection<CopilotCliOption>(cliOptionsResult.Options);
            foreach (var diagnostic in cliOptionsResult.Diagnostics)
            {
                Log(diagnostic);
            }

            StableSettingsPath = DescribeLocation("stable", stableInstalled);
            PreviewSettingsPath = DescribeLocation("preview", previewInstalled);

            SelectedProfile = Profiles.FirstOrDefault();
            if (SelectedProfile is null)
            {
                ClearEditor();
            }

            if (hasPackageIdentity)
            {
                Log("Detected package identity. Explorer registry writes are redirected into the app package, so the classic menu will not appear in the real Explorer context menu during this run.");
            }

            Log($"Loaded {Profiles.Count} Copilot profile(s).");
        }
        catch (Exception ex)
        {
            Log($"Failed to load data: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public bool IsStableAvailable => stableInstalled;

    public bool IsPreviewAvailable => previewInstalled;

    partial void OnSelectedProfileChanged(CopilotProfile? value)
    {
        isApplyingSelection = true;

        if (value is null)
        {
            ClearEditor();
            isApplyingSelection = false;
            DuplicateProfileCommand.NotifyCanExecuteChanged();
            DeleteProfileCommand.NotifyCanExecuteChanged();
            return;
        }

        Name = value.Name;
        GuidText = value.Guid.ToString("B");
        ShellCommandPrefix = value.ShellCommandPrefix;
        CopilotArguments = value.CopilotArguments;
        ColorScheme = value.ColorScheme;
        StartingDirectory = value.StartingDirectory;
        IconPath = value.IconPath;
        BackgroundImagePath = value.BackgroundImagePath;
        BackgroundOpacity = (double)(value.BackgroundImageOpacity ?? 0);
        WindowOpacity = value.Opacity ?? 0;
        TabTitle = value.TabTitle;
        Hidden = value.Hidden;
        SyncStable = value.SyncStable;
        SyncPreview = value.SyncPreview;
        SyncRegistry = value.SyncRegistry;
        CommandPreview = value.BuildCommandLine();

        isApplyingSelection = false;
        DuplicateProfileCommand.NotifyCanExecuteChanged();
        DeleteProfileCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedCliOptionChanged(CopilotCliOption? value)
    {
        SelectedCliOptionDescription = value is null
            ? string.Empty
            : $"{value.Syntax}{Environment.NewLine}{Environment.NewLine}{value.Description}";
    }

    partial void OnNameChanged(string value) => ApplyEditorChanges();
    partial void OnShellCommandPrefixChanged(string value) => ApplyEditorChanges();
    partial void OnCopilotArgumentsChanged(string value) => ApplyEditorChanges();
    partial void OnColorSchemeChanged(string value) => ApplyEditorChanges();
    partial void OnStartingDirectoryChanged(string value) => ApplyEditorChanges();
    partial void OnIconPathChanged(string value) => ApplyEditorChanges();
    partial void OnBackgroundImagePathChanged(string value) => ApplyEditorChanges();
    partial void OnBackgroundOpacityChanged(double value) => ApplyEditorChanges();
    partial void OnWindowOpacityChanged(double value) => ApplyEditorChanges();
    partial void OnTabTitleChanged(string value) => ApplyEditorChanges();
    partial void OnHiddenChanged(bool value) => ApplyEditorChanges();
    partial void OnSyncStableChanged(bool value) => ApplyEditorChanges();
    partial void OnSyncPreviewChanged(bool value) => ApplyEditorChanges();
    partial void OnSyncRegistryChanged(bool value) => ApplyEditorChanges();

    [RelayCommand]
    private void CreateProfile()
    {
        ApplyEditorChanges();

        var profile = new CopilotProfile
        {
            Name = $"GitHub Copilot {Profiles.Count + 1}",
            ShellCommandPrefix = defaultShellPrefix,
            TabTitle = "GitHub Copilot",
            SyncStable = stableInstalled,
            SyncPreview = previewInstalled,
            SyncRegistry = true,
        };

        Profiles.Add(profile);
        SortProfiles(profile.Guid);
        SelectedProfile = Profiles.First(item => item.Guid == profile.Guid);
        Log($"Created profile '{profile.Name}'.");
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private void DuplicateProfile()
    {
        ApplyEditorChanges();

        if (SelectedProfile is not { } profile)
        {
            return;
        }

        var clone = profile.Clone();
        Profiles.Add(clone);
        SortProfiles(clone.Guid);
        SelectedProfile = Profiles.First(item => item.Guid == clone.Guid);
        Log($"Cloned profile '{profile.Name}'.");
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private void DeleteProfile()
    {
        if (SelectedProfile is not { } profile)
        {
            return;
        }

        deletedProfileGuids.Add(profile.Guid);
        Profiles.Remove(profile);
        SelectedProfile = Profiles.FirstOrDefault();
        Log($"Deleted profile '{profile.Name}'. Changes will be applied on save.");
    }

    [RelayCommand]
    private async Task ReloadAsync() => await LoadAsync();

    [RelayCommand]
    private void InsertSelectedFlag()
    {
        if (SelectedCliOption is not { } option)
        {
            return;
        }

        var syntax = option.Syntax.Replace(", ", " ", StringComparison.Ordinal);
        CopilotArguments = string.IsNullOrWhiteSpace(CopilotArguments)
            ? syntax
            : $"{CopilotArguments.Trim()} {syntax}";
    }

    [RelayCommand]
    private void RemoveExplorerMenu()
    {
        ApplyEditorChanges();

        foreach (var profile in Profiles)
        {
            profile.SyncRegistry = false;
        }

        SyncRegistry = false;
        registrySyncService.RemoveMenu();
        Log("Removed the Explorer Copilot submenu and turned off Explorer sync for all profiles. Save to persist that state.");
    }

    [RelayCommand]
    private void ClearActivityLog() => ActivityLog = string.Empty;

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            ApplyEditorChanges();

            var managedGuids = metadata.ManagedProfileGuids
                .Concat(deletedProfileGuids)
                .ToHashSet();

            foreach (var location in locations.Where(location => location.IsInstalled))
            {
                var desiredProfiles = location.Key switch
                {
                    "stable" => Profiles.Where(profile => profile.SyncStable).ToList(),
                    "preview" => Profiles.Where(profile => profile.SyncPreview).ToList(),
                    _ => [],
                };

                terminalSettingsService.SyncProfiles(location, desiredProfiles, managedGuids);
                Log($"Synced {desiredProfiles.Count} profile(s) to {location.DisplayName}.");
            }

            if (RuntimeEnvironmentService.HasPackageIdentity())
            {
                Log("Detected package identity before Explorer sync. Registry writes will be virtualized and won't show up in the real Explorer menu or the standard HKCU\\Software\\Classes view.");
            }

            try
            {
                registrySyncService.SyncProfiles(Profiles);
            }
            catch (Exception ex)
            {
                Log($"Explorer registry sync failed: {ex.GetType().Name}: {ex.Message}");
                throw;
            }

            Log(Profiles.Any(profile => profile.SyncRegistry)
                ? "Updated Explorer registry menu."
                : "Explorer registry menu removed.");

            metadata.ManagedProfileGuids = Profiles
                .Where(profile => profile.HasAnySyncTarget())
                .Select(profile => profile.Guid)
                .ToHashSet();
            metadata.ExplorerProfileGuids = Profiles
                .Where(profile => profile.SyncRegistry)
                .Select(profile => profile.Guid)
                .ToHashSet();

            metadataService.Save(metadata);
            deletedProfileGuids.Clear();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Log($"Save failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool HasSelectedProfile() => SelectedProfile is not null;

    private void ApplyEditorChanges()
    {
        if (isApplyingSelection || SelectedProfile is not { } profile)
        {
            return;
        }

        profile.Name = Name.Trim();
        profile.ShellCommandPrefix = string.IsNullOrWhiteSpace(ShellCommandPrefix)
            ? defaultShellPrefix
            : ShellCommandPrefix.Trim();
        profile.CopilotArguments = CopilotArguments.Trim();
        profile.ColorScheme = ColorScheme.Trim();
        profile.StartingDirectory = StartingDirectory.Trim();
        profile.IconPath = IconPath.Trim();
        profile.BackgroundImagePath = BackgroundImagePath.Trim();
        profile.BackgroundImageOpacity = BackgroundOpacity <= 0 ? null : (decimal)BackgroundOpacity;
        profile.Opacity = WindowOpacity <= 0 ? null : (int)WindowOpacity;
        profile.TabTitle = TabTitle.Trim();
        profile.Hidden = Hidden;
        profile.SyncStable = stableInstalled && SyncStable;
        profile.SyncPreview = previewInstalled && SyncPreview;
        profile.SyncRegistry = SyncRegistry;

        CommandPreview = profile.BuildCommandLine();
        SortProfiles(profile.Guid);
    }

    private void SortProfiles(Guid selectedGuid)
    {
        var ordered = Profiles.OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase).ToList();
        Profiles = new ObservableCollection<CopilotProfile>(ordered);
        SelectedProfile = Profiles.FirstOrDefault(profile => profile.Guid == selectedGuid);
    }

    private List<CopilotProfile> MergeProfiles(IEnumerable<TerminalSettingsSnapshot> snapshots)
    {
        var byGuid = new Dictionary<Guid, CopilotProfile>();

        foreach (var snapshot in snapshots)
        {
            foreach (var profile in snapshot.Profiles)
            {
                if (byGuid.TryGetValue(profile.Guid, out var existing))
                {
                    existing.SyncStable |= profile.SyncStable;
                    existing.SyncPreview |= profile.SyncPreview;
                    existing.SyncRegistry |= metadata.ExplorerProfileGuids.Contains(profile.Guid);
                    continue;
                }

                profile.SyncRegistry = metadata.ExplorerProfileGuids.Contains(profile.Guid);
                byGuid[profile.Guid] = profile;
            }
        }

        return byGuid.Values
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string DescribeLocation(string key, bool isInstalled)
    {
        var location = locations.FirstOrDefault(item => item.Key == key);
        if (location is null)
        {
            return string.Empty;
        }

        return isInstalled
            ? $"Settings file: {location.SettingsPath}"
            : $"Not found: {location.SettingsPath}";
    }

    private void ClearEditor()
    {
        Name = string.Empty;
        GuidText = string.Empty;
        ShellCommandPrefix = string.Empty;
        CopilotArguments = string.Empty;
        ColorScheme = string.Empty;
        StartingDirectory = string.Empty;
        IconPath = string.Empty;
        BackgroundImagePath = string.Empty;
        BackgroundOpacity = 0;
        WindowOpacity = 0;
        TabTitle = string.Empty;
        Hidden = false;
        SyncStable = false;
        SyncPreview = false;
        SyncRegistry = false;
        CommandPreview = string.Empty;
    }

    private void Log(string message)
    {
        var builder = new StringBuilder(ActivityLog);
        if (builder.Length > 0)
        {
            builder.AppendLine();
        }

        builder.Append($"[{DateTime.Now:HH:mm:ss}] {message}");
        ActivityLog = builder.ToString();
    }
}
