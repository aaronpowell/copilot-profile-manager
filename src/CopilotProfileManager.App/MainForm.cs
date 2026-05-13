using CopilotProfileManager.App.Models;
using CopilotProfileManager.App.Services;

namespace CopilotProfileManager.App;

public sealed class MainForm : Form
{
    private readonly AppMetadataService metadataService = new();
    private readonly WindowsTerminalSettingsService terminalSettingsService = new();
    private readonly RegistrySyncService registrySyncService = new();
    private readonly CopilotCliService copilotCliService = new();
    private readonly ToolTip toolTip = new();

    private readonly ComboBox profileSelector = new();
    private readonly TextBox nameTextBox = new();
    private readonly TextBox guidTextBox = new();
    private readonly TextBox shellPrefixTextBox = new();
    private readonly TextBox copilotArgsTextBox = new();
    private readonly ComboBox colorSchemeComboBox = new();
    private readonly TextBox startingDirectoryTextBox = new();
    private readonly TextBox iconPathTextBox = new();
    private readonly TextBox backgroundImagePathTextBox = new();
    private readonly NumericUpDown backgroundOpacityInput = new();
    private readonly NumericUpDown opacityInput = new();
    private readonly TextBox tabTitleTextBox = new();
    private readonly CheckBox hiddenCheckBox = new();
    private readonly CheckBox syncStableCheckBox = new();
    private readonly CheckBox syncPreviewCheckBox = new();
    private readonly CheckBox syncRegistryCheckBox = new();
    private readonly Label syncStablePathLabel = new();
    private readonly Label syncPreviewPathLabel = new();
    private readonly Label syncRegistryHelpLabel = new();
    private readonly TextBox commandPreviewTextBox = new();
    private readonly ListBox cliOptionsList = new();
    private readonly TextBox cliOptionDescriptionTextBox = new();
    private readonly Button insertFlagButton = new();
    private readonly Button newProfileButton = new();
    private readonly Button cloneProfileButton = new();
    private readonly Button deleteProfileButton = new();
    private readonly Button reloadButton = new();
    private readonly Button saveButton = new();
    private readonly Button removeRegistryButton = new();
    private readonly TextBox logTextBox = new();

    private bool isLoadingProfile;
    private bool stableInstalled;
    private bool previewInstalled;
    private string defaultShellPrefix = "\"C:\\Program Files\\PowerShell\\7-preview\\pwsh.exe\" -c";
    private List<CopilotProfile> profiles = [];
    private HashSet<Guid> deletedProfileGuids = [];
    private ProfileMetadata metadata = new();
    private List<TerminalSettingsLocation> locations = [];

    public MainForm()
    {
        InitializeComponent();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await LoadDataAsync();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        WireEditorEvents();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            metadata = metadataService.Load();
            locations = terminalSettingsService.DiscoverLocations().ToList();
            stableInstalled = locations.Any(location => location.Key == "stable" && location.IsInstalled);
            previewInstalled = locations.Any(location => location.Key == "preview" && location.IsInstalled);

            var snapshots = locations
                .Where(location => location.IsInstalled)
                .Select(terminalSettingsService.LoadSnapshot)
                .ToList();

            profiles = MergeProfiles(snapshots);
            defaultShellPrefix = terminalSettingsService.DetermineDefaultShellPrefix(profiles);
            deletedProfileGuids = [];

            colorSchemeComboBox.Items.Clear();
            foreach (var colorScheme in snapshots
                .SelectMany(snapshot => snapshot.ColorSchemes)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                colorSchemeComboBox.Items.Add(colorScheme);
            }

            var cliOptions = await copilotCliService.GetOptionsAsync();
            cliOptionsList.Items.Clear();
            cliOptionsList.Items.AddRange(cliOptions.ToArray());

            RebindProfileSelector();
            SetSyncAvailability();
            Log($"Loaded {profiles.Count} Copilot profile(s).");
        }
        catch (Exception ex)
        {
            Log($"Failed to load data: {ex.Message}");
        }
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "Copilot Profile Manager";
        MinimumSize = new Size(1280, 860);
        StartPosition = FormStartPosition.CenterScreen;

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12),
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));

        rootLayout.Controls.Add(BuildHeaderPanel(), 0, 0);
        rootLayout.Controls.Add(BuildToolbarPanel(), 0, 1);
        rootLayout.Controls.Add(BuildEditorTabs(), 0, 2);
        rootLayout.Controls.Add(BuildActivityPanel(), 0, 3);

        Controls.Add(rootLayout);
        ResumeLayout(false);
    }

    private Control BuildHeaderPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(16, 12, 16, 12),
        };

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Text = "Copilot Profile Manager",
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
            Height = 34,
        };

        var subtitleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Manage GitHub Copilot Windows Terminal profiles, choose where they sync, and control the Explorer submenu without editing JSON or registry keys by hand.",
            TextAlign = ContentAlignment.MiddleLeft,
        };

        panel.Controls.Add(subtitleLabel);
        panel.Controls.Add(titleLabel);
        return panel;
    }

    private Control BuildToolbarPanel()
    {
        var group = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "Current profile",
            Padding = new Padding(12),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 560F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var selectorLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Profile",
            TextAlign = ContentAlignment.MiddleLeft,
        };

        profileSelector.Dock = DockStyle.Fill;
        profileSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        profileSelector.SelectedIndexChanged += (_, _) => DisplaySelectedProfile();

        var actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
        };

        newProfileButton.Text = "New profile";
        newProfileButton.AutoSize = true;
        newProfileButton.Click += (_, _) => CreateProfile();

        cloneProfileButton.Text = "Duplicate";
        cloneProfileButton.AutoSize = true;
        cloneProfileButton.Click += (_, _) => CloneSelectedProfile();

        deleteProfileButton.Text = "Delete";
        deleteProfileButton.AutoSize = true;
        deleteProfileButton.Click += (_, _) => DeleteSelectedProfile();

        reloadButton.Text = "Reload from disk";
        reloadButton.AutoSize = true;
        reloadButton.Click += async (_, _) => await LoadDataAsync();

        saveButton.Text = "Save all changes";
        saveButton.AutoSize = true;
        saveButton.Click += async (_, _) => await SaveAsync();

        actionsPanel.Controls.Add(newProfileButton);
        actionsPanel.Controls.Add(cloneProfileButton);
        actionsPanel.Controls.Add(deleteProfileButton);
        actionsPanel.Controls.Add(reloadButton);
        actionsPanel.Controls.Add(saveButton);

        var helpLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Profiles are merged by GUID across Windows Terminal and Windows Terminal Preview. Use the selector above to switch profiles, then edit them below.",
        };

        layout.Controls.Add(selectorLabel, 0, 0);
        layout.Controls.Add(profileSelector, 1, 0);
        layout.Controls.Add(actionsPanel, 2, 0);
        layout.Controls.Add(helpLabel, 0, 1);
        layout.SetColumnSpan(helpLabel, 3);

        group.Controls.Add(layout);
        return group;
    }

    private Control BuildEditorTabs()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
        };

        tabs.TabPages.Add(BuildProfileEditorPage());
        tabs.TabPages.Add(BuildFlagHelperPage());
        return tabs;
    }

    private TabPage BuildProfileEditorPage()
    {
        var page = new TabPage("Profile editor");

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
        };

        var sectionLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Padding = new Padding(12),
        };

        sectionLayout.Controls.Add(BuildIdentitySection());
        sectionLayout.Controls.Add(BuildLaunchSection());
        sectionLayout.Controls.Add(BuildAppearanceSection());
        sectionLayout.Controls.Add(BuildSyncSection());

        scrollPanel.Controls.Add(sectionLayout);
        page.Controls.Add(scrollPanel);
        return page;
    }

    private Control BuildIdentitySection()
    {
        var group = CreateSection("Identity");
        var layout = CreateFormGrid();

        guidTextBox.ReadOnly = true;
        AddFormRow(layout, "Profile name", nameTextBox);
        AddFormRow(layout, "Profile GUID", guidTextBox);

        group.Controls.Add(layout);
        return group;
    }

    private Control BuildLaunchSection()
    {
        var group = CreateSection("Launch command");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoSize = true,
        };

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Text = "The final Terminal command is assembled as: <shell prefix> copilot <flags>. The flags field below is only the part after `copilot`.",
            Padding = new Padding(0, 0, 0, 10),
        });

        var formGrid = CreateFormGrid();
        AddFormRow(formGrid, "Shell command prefix", shellPrefixTextBox);
        AddFormRow(formGrid, "Copilot CLI flags", copilotArgsTextBox);
        AddFormRow(formGrid, "Command preview", commandPreviewTextBox);
        commandPreviewTextBox.ReadOnly = true;
        commandPreviewTextBox.Font = new Font(FontFamily.GenericMonospace, 9F);

        layout.Controls.Add(formGrid);
        group.Controls.Add(layout);

        toolTip.SetToolTip(shellPrefixTextBox, "Everything before the word `copilot`, for example: \"pwsh.exe -c\"");
        toolTip.SetToolTip(copilotArgsTextBox, "Only the flags and values after `copilot`, for example: --allow-all --model gpt-5.4");

        return group;
    }

    private Control BuildAppearanceSection()
    {
        var group = CreateSection("Appearance and Terminal behaviour");
        var layout = CreateFormGrid();

        colorSchemeComboBox.DropDownStyle = ComboBoxStyle.DropDown;
        backgroundOpacityInput.DecimalPlaces = 2;
        backgroundOpacityInput.Increment = 0.05M;
        backgroundOpacityInput.Minimum = 0;
        backgroundOpacityInput.Maximum = 1;
        opacityInput.Minimum = 0;
        opacityInput.Maximum = 100;
        hiddenCheckBox.Text = "Hide this profile from the Terminal profile list";

        AddFormRow(layout, "Color scheme", colorSchemeComboBox);
        AddFormRow(layout, "Starting directory", startingDirectoryTextBox);
        AddFormRow(layout, "Icon path", iconPathTextBox);
        AddFormRow(layout, "Background image", backgroundImagePathTextBox);
        AddFormRow(layout, "Background opacity", backgroundOpacityInput);
        AddFormRow(layout, "Window opacity", opacityInput);
        AddFormRow(layout, "Tab title", tabTitleTextBox);
        AddFormRow(layout, "Visibility", hiddenCheckBox);

        group.Controls.Add(layout);
        return group;
    }

    private Control BuildSyncSection()
    {
        var group = CreateSection("Where to sync this profile");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoSize = true,
        };

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Text = "Choose which Terminal settings files should receive this profile, and whether the Explorer 'Copilot' submenu should be published.",
            Padding = new Padding(0, 0, 0, 10),
        });

        var syncLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
        };

        syncStableCheckBox.Text = "Windows Terminal (stable)";
        syncPreviewCheckBox.Text = "Windows Terminal Preview";
        syncRegistryCheckBox.Text = "Explorer submenu";
        syncStablePathLabel.AutoSize = true;
        syncPreviewPathLabel.AutoSize = true;
        syncRegistryHelpLabel.AutoSize = true;
        syncRegistryHelpLabel.Text = "Adds or removes the classic Explorer submenu for folders and folder backgrounds.";

        syncLayout.Controls.Add(syncStableCheckBox);
        syncLayout.Controls.Add(syncStablePathLabel);
        syncLayout.Controls.Add(syncPreviewCheckBox);
        syncLayout.Controls.Add(syncPreviewPathLabel);
        syncLayout.Controls.Add(syncRegistryCheckBox);
        syncLayout.Controls.Add(syncRegistryHelpLabel);

        removeRegistryButton.Text = "Remove Explorer menu now";
        removeRegistryButton.AutoSize = true;
        removeRegistryButton.Click += (_, _) => RemoveRegistryMenu();

        layout.Controls.Add(syncLayout);
        layout.Controls.Add(removeRegistryButton);
        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 0),
            Text = "Want to uninstall the registry changes? Click 'Remove Explorer menu now'. That clears the current submenu immediately and also turns off Explorer sync for every profile in the editor.",
        });

        group.Controls.Add(layout);
        return group;
    }

    private TabPage BuildFlagHelperPage()
    {
        var page = new TabPage("Flag helper");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "This page is just a helper for the 'Copilot CLI flags' field on the Profile editor tab. Pick a flag, read what it does, then insert it into the selected profile's flags field.",
        }, 0, 0);

        cliOptionsList.Dock = DockStyle.Fill;
        cliOptionsList.SelectedIndexChanged += (_, _) =>
        {
            cliOptionDescriptionTextBox.Text = cliOptionsList.SelectedItem is CopilotCliOption option
                ? $"{option.Syntax}{Environment.NewLine}{Environment.NewLine}{option.Description}"
                : string.Empty;
        };

        cliOptionDescriptionTextBox.Dock = DockStyle.Fill;
        cliOptionDescriptionTextBox.Multiline = true;
        cliOptionDescriptionTextBox.ReadOnly = true;

        insertFlagButton.Text = "Insert into selected profile";
        insertFlagButton.Dock = DockStyle.Right;
        insertFlagButton.Click += (_, _) => InsertSelectedFlag();

        layout.Controls.Add(cliOptionsList, 0, 1);
        layout.Controls.Add(cliOptionDescriptionTextBox, 0, 2);
        layout.Controls.Add(insertFlagButton, 0, 3);

        page.Controls.Add(layout);
        return page;
    }

    private Control BuildActivityPanel()
    {
        var group = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "Activity",
            Padding = new Padding(10),
        };

        logTextBox.Dock = DockStyle.Fill;
        logTextBox.Multiline = true;
        logTextBox.ReadOnly = true;
        logTextBox.ScrollBars = ScrollBars.Vertical;
        logTextBox.Font = new Font(FontFamily.GenericMonospace, 9F);

        group.Controls.Add(logTextBox);
        return group;
    }

    private static GroupBox CreateSection(string title) =>
        new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Text = title,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 12),
        };

    private static TableLayoutPanel CreateFormGrid()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        return layout;
    }

    private static void AddFormRow(TableLayoutPanel layout, string labelText, Control control)
    {
        var rowIndex = layout.RowStyles.Count;
        layout.RowCount = rowIndex + 1;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, control is CheckBox ? 34F : 36F));

        layout.Controls.Add(new Label
        {
            Text = labelText,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
        }, 0, rowIndex);

        control.Dock = DockStyle.Fill;
        layout.Controls.Add(control, 1, rowIndex);
    }

    private IEnumerable<Control> EnumerateEditableControls()
    {
        yield return nameTextBox;
        yield return shellPrefixTextBox;
        yield return copilotArgsTextBox;
        yield return colorSchemeComboBox;
        yield return startingDirectoryTextBox;
        yield return iconPathTextBox;
        yield return backgroundImagePathTextBox;
        yield return backgroundOpacityInput;
        yield return opacityInput;
        yield return tabTitleTextBox;
        yield return hiddenCheckBox;
        yield return syncStableCheckBox;
        yield return syncPreviewCheckBox;
        yield return syncRegistryCheckBox;
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
                    existing.SyncRegistry |= metadata.ManagedProfileGuids.Contains(profile.Guid);
                    continue;
                }

                profile.SyncRegistry = metadata.ManagedProfileGuids.Contains(profile.Guid);
                byGuid[profile.Guid] = profile;
            }
        }

        return byGuid.Values
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void RebindProfileSelector()
    {
        var selectedGuid = SelectedProfile?.Guid;
        profileSelector.BeginUpdate();
        profileSelector.Items.Clear();
        profileSelector.Items.AddRange(profiles.Cast<object>().ToArray());
        profileSelector.EndUpdate();

        if (selectedGuid.HasValue)
        {
            var index = profiles.FindIndex(profile => profile.Guid == selectedGuid.Value);
            profileSelector.SelectedIndex = index >= 0 ? index : (profiles.Count > 0 ? 0 : -1);
        }
        else if (profiles.Count > 0)
        {
            profileSelector.SelectedIndex = 0;
        }
        else
        {
            ClearEditor();
        }
    }

    private CopilotProfile? SelectedProfile => profileSelector.SelectedItem as CopilotProfile;

    private void DisplaySelectedProfile()
    {
        isLoadingProfile = true;

        try
        {
            if (SelectedProfile is not { } profile)
            {
                ClearEditor();
                return;
            }

            nameTextBox.Text = profile.Name;
            guidTextBox.Text = profile.Guid.ToString("B");
            shellPrefixTextBox.Text = profile.ShellCommandPrefix;
            copilotArgsTextBox.Text = profile.CopilotArguments;
            colorSchemeComboBox.Text = profile.ColorScheme;
            startingDirectoryTextBox.Text = profile.StartingDirectory;
            iconPathTextBox.Text = profile.IconPath;
            backgroundImagePathTextBox.Text = profile.BackgroundImagePath;
            backgroundOpacityInput.Value = Math.Clamp(profile.BackgroundImageOpacity ?? 0, 0, 1);
            opacityInput.Value = Math.Clamp(profile.Opacity ?? 0, 0, 100);
            tabTitleTextBox.Text = profile.TabTitle;
            hiddenCheckBox.Checked = profile.Hidden;
            syncStableCheckBox.Checked = profile.SyncStable;
            syncPreviewCheckBox.Checked = profile.SyncPreview;
            syncRegistryCheckBox.Checked = profile.SyncRegistry;
            commandPreviewTextBox.Text = profile.BuildCommandLine();
        }
        finally
        {
            isLoadingProfile = false;
        }

        SetSyncAvailability();
    }

    private void CaptureCurrentProfile()
    {
        if (isLoadingProfile || SelectedProfile is not { } profile)
        {
            return;
        }

        profile.Name = nameTextBox.Text.Trim();
        profile.ShellCommandPrefix = string.IsNullOrWhiteSpace(shellPrefixTextBox.Text)
            ? defaultShellPrefix
            : shellPrefixTextBox.Text.Trim();
        profile.CopilotArguments = copilotArgsTextBox.Text.Trim();
        profile.ColorScheme = colorSchemeComboBox.Text.Trim();
        profile.StartingDirectory = startingDirectoryTextBox.Text.Trim();
        profile.IconPath = iconPathTextBox.Text.Trim();
        profile.BackgroundImagePath = backgroundImagePathTextBox.Text.Trim();
        profile.BackgroundImageOpacity = backgroundOpacityInput.Value == 0 ? null : backgroundOpacityInput.Value;
        profile.Opacity = opacityInput.Value == 0 ? null : (int)opacityInput.Value;
        profile.TabTitle = tabTitleTextBox.Text.Trim();
        profile.Hidden = hiddenCheckBox.Checked;
        profile.SyncStable = stableInstalled && syncStableCheckBox.Checked;
        profile.SyncPreview = previewInstalled && syncPreviewCheckBox.Checked;
        profile.SyncRegistry = syncRegistryCheckBox.Checked;

        commandPreviewTextBox.Text = profile.BuildCommandLine();
        RefreshProfileNames();
    }

    private void RefreshProfileNames()
    {
        var index = profileSelector.SelectedIndex;
        profileSelector.BeginUpdate();
        profileSelector.Items.Clear();
        profileSelector.Items.AddRange(profiles.Cast<object>().ToArray());
        profileSelector.EndUpdate();
        if (index >= 0 && index < profileSelector.Items.Count)
        {
            profileSelector.SelectedIndex = index;
        }
    }

    private void ClearEditor()
    {
        nameTextBox.Clear();
        guidTextBox.Clear();
        shellPrefixTextBox.Clear();
        copilotArgsTextBox.Clear();
        colorSchemeComboBox.Text = string.Empty;
        startingDirectoryTextBox.Clear();
        iconPathTextBox.Clear();
        backgroundImagePathTextBox.Clear();
        backgroundOpacityInput.Value = 0;
        opacityInput.Value = 0;
        tabTitleTextBox.Clear();
        hiddenCheckBox.Checked = false;
        syncStableCheckBox.Checked = false;
        syncPreviewCheckBox.Checked = false;
        syncRegistryCheckBox.Checked = false;
        commandPreviewTextBox.Clear();
    }

    private void SetSyncAvailability()
    {
        var stableLocation = locations.FirstOrDefault(location => location.Key == "stable");
        var previewLocation = locations.FirstOrDefault(location => location.Key == "preview");

        syncStableCheckBox.Enabled = stableInstalled;
        syncPreviewCheckBox.Enabled = previewInstalled;

        syncStablePathLabel.Text = stableInstalled
            ? $"Settings file: {stableLocation?.SettingsPath}"
            : $"Not found: {stableLocation?.SettingsPath}";

        syncPreviewPathLabel.Text = previewInstalled
            ? $"Settings file: {previewLocation?.SettingsPath}"
            : $"Not found: {previewLocation?.SettingsPath}";

        toolTip.SetToolTip(syncStableCheckBox, "Writes this profile into the stable Windows Terminal settings file.");
        toolTip.SetToolTip(syncPreviewCheckBox, "Writes this profile into the Windows Terminal Preview settings file.");
        toolTip.SetToolTip(syncRegistryCheckBox, "Publishes the Explorer Copilot submenu for this profile.");
    }

    private void CreateProfile()
    {
        CaptureCurrentProfile();

        var profile = new CopilotProfile
        {
            Name = $"GitHub Copilot {(profiles.Count + 1)}",
            ShellCommandPrefix = defaultShellPrefix,
            TabTitle = "GitHub Copilot",
            SyncStable = stableInstalled,
            SyncPreview = previewInstalled,
            SyncRegistry = true,
        };

        profiles.Add(profile);
        profiles = profiles
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        RebindProfileSelector();
        profileSelector.SelectedItem = profiles.First(item => item.Guid == profile.Guid);
        Log($"Created profile '{profile.Name}'.");
    }

    private void CloneSelectedProfile()
    {
        CaptureCurrentProfile();

        if (SelectedProfile is not { } profile)
        {
            return;
        }

        var clone = profile.Clone();
        profiles.Add(clone);
        profiles = profiles
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        RebindProfileSelector();
        profileSelector.SelectedItem = profiles.First(item => item.Guid == clone.Guid);
        Log($"Cloned profile '{profile.Name}'.");
    }

    private void DeleteSelectedProfile()
    {
        if (SelectedProfile is not { } profile)
        {
            return;
        }

        deletedProfileGuids.Add(profile.Guid);
        profiles.Remove(profile);
        RebindProfileSelector();
        Log($"Deleted profile '{profile.Name}'. Changes will be applied on save.");
    }

    private void InsertSelectedFlag()
    {
        if (cliOptionsList.SelectedItem is not CopilotCliOption option)
        {
            return;
        }

        var syntax = option.Syntax.Replace(", ", " ", StringComparison.Ordinal);
        copilotArgsTextBox.Text = string.IsNullOrWhiteSpace(copilotArgsTextBox.Text)
            ? syntax
            : $"{copilotArgsTextBox.Text.Trim()} {syntax}";
    }

    private void RemoveRegistryMenu()
    {
        CaptureCurrentProfile();

        foreach (var profile in profiles)
        {
            profile.SyncRegistry = false;
        }

        syncRegistryCheckBox.Checked = false;
        registrySyncService.RemoveMenu();
        Log("Removed the Explorer Copilot submenu and turned off Explorer sync for all profiles. Save to persist that state.");
    }

    private async Task SaveAsync()
    {
        try
        {
            CaptureCurrentProfile();

            var managedGuids = metadata.ManagedProfileGuids
                .Concat(deletedProfileGuids)
                .ToHashSet();

            foreach (var location in locations.Where(location => location.IsInstalled))
            {
                var desiredProfiles = location.Key switch
                {
                    "stable" => profiles.Where(profile => profile.SyncStable).ToList(),
                    "preview" => profiles.Where(profile => profile.SyncPreview).ToList(),
                    _ => [],
                };

                terminalSettingsService.SyncProfiles(location, desiredProfiles, managedGuids);
                Log($"Synced {desiredProfiles.Count} profile(s) to {location.DisplayName}.");
            }

            registrySyncService.SyncProfiles(profiles);
            Log(profiles.Any(profile => profile.SyncRegistry)
                ? "Updated Explorer registry menu."
                : "Explorer registry menu removed.");

            metadata.ManagedProfileGuids = profiles
                .Where(profile => profile.HasAnySyncTarget())
                .Select(profile => profile.Guid)
                .ToHashSet();

            metadataService.Save(metadata);
            deletedProfileGuids.Clear();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            Log($"Save failed: {ex.Message}");
        }
    }

    private void Log(string message)
    {
        logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void WireEditorEvents()
    {
        foreach (var control in EnumerateEditableControls())
        {
            switch (control)
            {
                case TextBox textBox:
                    textBox.TextChanged += (_, _) => CaptureCurrentProfile();
                    break;
                case ComboBox comboBox:
                    comboBox.TextChanged += (_, _) => CaptureCurrentProfile();
                    break;
                case NumericUpDown numericUpDown:
                    numericUpDown.ValueChanged += (_, _) => CaptureCurrentProfile();
                    break;
                case CheckBox checkBox:
                    checkBox.CheckedChanged += (_, _) => CaptureCurrentProfile();
                    break;
            }
        }
    }
}
