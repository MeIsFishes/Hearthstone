using System.Collections.ObjectModel;
using System.Windows;
using BbxDeployer.Core;
using BbxDeployer.Infrastructure;
using BbxDeployer.Services;

namespace BbxDeployer.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ProjectLocator _projectLocator = new();
    private readonly UnityEditorLocator _unityEditorLocator = new();
    private readonly SettingsRepository _settingsRepository = new();
    private readonly DialogService _dialogService = new();
    private readonly SyncPlanner _syncPlanner;
    private readonly SyncExecutor _syncExecutor;
    private string _sourceRepositoryRoot = string.Empty;
    private string _sourceUnityProjectRoot = string.Empty;
    private string _sourceStatus = "Not configured";
    private string _statusText = "Configure a source and at least one destination.";
    private string _summaryText = "Ready for validation.";
    private bool _isBusy;
    private bool _isPreviewing;
    private bool _isProgressIndeterminate;
    private double _progressValue;
    private SyncItemViewModel? _selectedSyncItem;
    private TargetProjectViewModel? _selectedTarget;
    private SyncPreview? _preview;
    private CancellationTokenSource? _operationCancellation;
    private string? _lastLogPath;

    public MainViewModel()
    {
        var ignoreLoader = new IgnoreRuleLoader();
        var validator = new ProjectValidator(_projectLocator);
        _syncPlanner = new SyncPlanner(
            ignoreLoader,
            new PathInclusionEvaluator(),
            _projectLocator,
            validator,
            _unityEditorLocator);
        _syncExecutor = new SyncExecutor(ignoreLoader);

        BrowseRepositoryCommand = new RelayCommand(_ => BrowseRepository(), _ => !IsBusy);
        BrowseGameProjectCommand = new RelayCommand(_ => BrowseGameProject(), _ => !IsBusy);
        AddSyncItemCommand = new RelayCommand(_ => AddSyncItem(), _ => !IsBusy);
        EditSyncItemCommand = new RelayCommand(
            _ => EditSyncItem(),
            _ => !IsBusy && SelectedSyncItem is not null);
        RemoveSyncItemCommand = new RelayCommand(
            _ => RemoveSyncItem(),
            _ => !IsBusy && SelectedSyncItem is not null);
        AddTargetCommand = new RelayCommand(_ => AddTarget(), _ => !IsBusy);
        EditTargetCommand = new RelayCommand(
            parameter => EditTarget(parameter as TargetProjectViewModel),
            parameter => !IsBusy && parameter is TargetProjectViewModel);
        RemoveTargetCommand = new RelayCommand(
            parameter => RemoveTarget(parameter as TargetProjectViewModel),
            parameter => !IsBusy && parameter is TargetProjectViewModel);
        PreviewCommand = new AsyncRelayCommand(_ => PreviewAsync(), _ => !IsBusy);
        SyncCommand = new AsyncRelayCommand(_ => SyncAsync(), _ => CanSync);
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings(), _ => !IsBusy);
        SelectAsSourceCommand = new RelayCommand(
            parameter => SelectAsSource(parameter as TargetProjectViewModel),
            parameter => !IsBusy
                && parameter is TargetProjectViewModel target
                && _projectLocator.IsUnityProject(target.UnityProjectRoot));
    }

    public ObservableCollection<SyncItemViewModel> SyncItems { get; } = [];

    public ObservableCollection<TargetProjectViewModel> Targets { get; } = [];

    public ObservableCollection<string> Messages { get; } = [];

    public Window? Owner { get; set; }

    public RelayCommand BrowseRepositoryCommand { get; }
    public RelayCommand BrowseGameProjectCommand { get; }
    public RelayCommand AddSyncItemCommand { get; }
    public RelayCommand EditSyncItemCommand { get; }
    public RelayCommand RemoveSyncItemCommand { get; }
    public RelayCommand AddTargetCommand { get; }
    public RelayCommand EditTargetCommand { get; }
    public RelayCommand RemoveTargetCommand { get; }
    public AsyncRelayCommand PreviewCommand { get; }
    public AsyncRelayCommand SyncCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand SelectAsSourceCommand { get; }

    public string MainProjectName => string.IsNullOrWhiteSpace(SourceUnityProjectRoot)
        ? "Not configured"
        : new DirectoryInfo(SourceUnityProjectRoot).Name;

    public string MainProjectRoot => string.IsNullOrWhiteSpace(SourceRepositoryRoot)
        ? "Choose the folder that provides files to all other projects."
        : SourceRepositoryRoot;

    public string MainGameDirectory => string.IsNullOrWhiteSpace(SourceUnityProjectRoot)
        ? string.Empty
        : SourceUnityProjectRoot;

    public string SourceRepositoryRoot
    {
        get => _sourceRepositoryRoot;
        set
        {
            if (SetProperty(ref _sourceRepositoryRoot, value))
            {
                NotifyMainProject();
                UpdateSourceStatus();
                InvalidatePreview();
            }
        }
    }

    public string SourceUnityProjectRoot
    {
        get => _sourceUnityProjectRoot;
        set
        {
            if (SetProperty(ref _sourceUnityProjectRoot, value))
            {
                NotifyMainProject();
                UpdateSourceStatus();
                InvalidatePreview();
            }
        }
    }

    public string SourceStatus
    {
        get => _sourceStatus;
        private set => SetProperty(ref _sourceStatus, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool IsPreviewing
    {
        get => _isPreviewing;
        private set => SetProperty(ref _isPreviewing, value);
    }

    public bool IsProgressIndeterminate
    {
        get => _isProgressIndeterminate;
        private set => SetProperty(ref _isProgressIndeterminate, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public SyncItemViewModel? SelectedSyncItem
    {
        get => _selectedSyncItem;
        set
        {
            if (SetProperty(ref _selectedSyncItem, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public TargetProjectViewModel? SelectedTarget
    {
        get => _selectedTarget;
        set
        {
            if (SetProperty(ref _selectedTarget, value))
            {
                RaiseCommandStates();
            }
        }
    }

    private bool CanPreview =>
        !IsBusy
        && Directory.Exists(SourceRepositoryRoot)
        && _projectLocator.IsUnityProject(SourceUnityProjectRoot)
        && SyncItems.Count > 0
        && Targets.Count > 0;

    private bool CanSync => !IsBusy && _preview is { HasBlockingErrors: false };

    public async Task InitializeAsync()
    {
        try
        {
            var settings = await _settingsRepository.LoadAsync();
            if (settings is not null)
            {
                ApplySettings(settings);
            }
            else
            {
                ApplyDefaults();
            }

            StatusText = CanPreview
                ? "Ready to preview."
                : "Configure a source and at least one destination.";
        }
        catch (Exception exception)
        {
            ApplyDefaults();
            Messages.Add($"Settings could not be loaded: {exception.Message}");
        }

        RaiseCommandStates();
    }

    public async Task SaveAsync()
    {
        try
        {
            await _settingsRepository.SaveAsync(CaptureSettings());
        }
        catch (Exception exception)
        {
            Messages.Add($"Settings could not be saved: {exception.Message}");
        }
    }

    private void ApplyDefaults()
    {
        ReplaceSyncItems(_projectLocator.CreateDefaultSyncItems());
        var repositoryRoot = _projectLocator.InferRepositoryRootFromExecutable(
            AppContext.BaseDirectory);
        if (repositoryRoot is null)
        {
            return;
        }

        SourceRepositoryRoot = repositoryRoot;
        var candidates = _projectLocator.DiscoverUnityProjects(repositoryRoot);
        if (candidates.Count == 1)
        {
            SourceUnityProjectRoot = candidates[0];
        }
    }

    private void ApplySettings(AppSettings settings)
    {
        ReplaceSyncItems(settings.SyncItems.Count > 0
            ? settings.SyncItems
            : _projectLocator.CreateDefaultSyncItems());

        if (settings.Source is not null)
        {
            _sourceRepositoryRoot = settings.Source.RepositoryRoot;
            _sourceUnityProjectRoot = settings.Source.UnityProjectRoot;
            OnPropertyChanged(nameof(SourceRepositoryRoot));
            OnPropertyChanged(nameof(SourceUnityProjectRoot));
            NotifyMainProject();
            UpdateSourceStatus();
        }

        Targets.Clear();
        foreach (var target in settings.Targets)
        {
            Targets.Add(CreateTargetViewModel(target));
        }
    }

    private AppSettings CaptureSettings()
    {
        return new AppSettings
        {
            Source = CreateSourceContext(),
            Targets = Targets.Select(target => target.Model.Clone()).ToList(),
            SyncItems = SyncItems.Select(item =>
            {
                var model = item.Model.Clone();
                model.Enabled = true;
                return model;
            }).ToList()
        };
    }

    private ProjectContext CreateSourceContext()
    {
        return new ProjectContext
        {
            DisplayName = string.IsNullOrWhiteSpace(SourceUnityProjectRoot)
                ? "Source"
                : new DirectoryInfo(SourceUnityProjectRoot).Name,
            RepositoryRoot = SourceRepositoryRoot,
            UnityProjectRoot = SourceUnityProjectRoot,
            UnityEditorVersion =
                _projectLocator.ReadUnityVersion(SourceUnityProjectRoot) ?? string.Empty
        };
    }

    private void ReplaceSyncItems(IEnumerable<SyncItem> items)
    {
        SyncItems.Clear();
        foreach (var item in items)
        {
            var model = item.Clone();
            model.Enabled = true;
            var viewModel = new SyncItemViewModel(model);
            viewModel.Changed += (_, _) => InvalidatePreview();
            SyncItems.Add(viewModel);
        }
    }

    private void BrowseRepository()
    {
        var selected = _dialogService.SelectFolder(
            "Select Main Project Root",
            SourceRepositoryRoot);
        if (selected is null)
        {
            return;
        }

        try
        {
            var context = _projectLocator.CreateContextFromProjectRoot(selected);
            SourceRepositoryRoot = context.RepositoryRoot;
            SourceUnityProjectRoot = context.UnityProjectRoot;
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message);
        }
    }

    private void BrowseGameProject()
    {
        var selected = _dialogService.SelectFolder(
            "Select Source Unity Project",
            SourceUnityProjectRoot);
        if (selected is null)
        {
            return;
        }

        if (!_projectLocator.IsUnityProject(selected))
        {
            _dialogService.ShowError("The selected folder is not a Unity project.");
            return;
        }

        SourceUnityProjectRoot = selected;
        if (string.IsNullOrWhiteSpace(SourceRepositoryRoot))
        {
            SourceRepositoryRoot = Directory.GetParent(selected)?.FullName ?? string.Empty;
        }
    }

    private void AddSyncItem()
    {
        var result = _dialogService.EditSyncItem(
            new SyncItem
            {
                DisplayName = "Custom Folder",
                SourceBase = PathBaseKind.RepositoryRoot,
                TargetBase = PathBaseKind.RepositoryRoot,
                UseGitIgnoreFiles = true
            },
            CreateSourceContext(),
            Owner);
        if (result is null)
        {
            return;
        }

        var viewModel = new SyncItemViewModel(result);
        viewModel.Changed += (_, _) => InvalidatePreview();
        SyncItems.Add(viewModel);
        SelectedSyncItem = viewModel;
        InvalidatePreview();
    }

    private void EditSyncItem()
    {
        if (SelectedSyncItem is null)
        {
            return;
        }

        var result = _dialogService.EditSyncItem(
            SelectedSyncItem.Model,
            CreateSourceContext(),
            Owner);
        if (result is not null)
        {
            SelectedSyncItem.Replace(result);
        }
    }

    private void RemoveSyncItem()
    {
        if (SelectedSyncItem is null)
        {
            return;
        }

        SyncItems.Remove(SelectedSyncItem);
        SelectedSyncItem = null;
        InvalidatePreview();
    }

    private void AddTarget()
    {
        var selected = _dialogService.SelectFolder("Select Destination Project Root");
        if (selected is null)
        {
            return;
        }

        ProjectContext? context;
        try
        {
            context = CreateDestinationContext(selected);
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message);
            return;
        }

        if (context is null)
        {
            return;
        }

        if (Targets.Any(target => target.RepositoryRoot.Equals(
                context.RepositoryRoot,
                StringComparison.OrdinalIgnoreCase)))
        {
            _dialogService.ShowError("This project root is already in the list.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SourceRepositoryRoot)
            || !_projectLocator.IsUnityProject(SourceUnityProjectRoot))
        {
            if (!_projectLocator.IsUnityProject(context.UnityProjectRoot))
            {
                _dialogService.ShowError(
                    "Configure a valid Main Project before adding a new empty destination.");
                return;
            }

            SourceRepositoryRoot = context.RepositoryRoot;
            SourceUnityProjectRoot = context.UnityProjectRoot;
        }
        else
        {
            var viewModel = CreateTargetViewModel(context);
            Targets.Add(viewModel);
            SelectedTarget = viewModel;
        }

        InvalidatePreview();
    }

    private void EditTarget(TargetProjectViewModel? target)
    {
        if (target is null)
        {
            return;
        }

        var selected = _dialogService.SelectFolder(
            "Select Destination Project Root",
            target.RepositoryRoot);
        if (selected is null)
        {
            return;
        }

        try
        {
            var context = CreateDestinationContext(selected);
            if (context is null)
            {
                return;
            }

            target.Replace(context);
            InvalidatePreview();
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message);
        }
    }

    private ProjectContext? CreateDestinationContext(string selectedRoot)
    {
        var context = _projectLocator.CreateDestinationContextFromProjectRoot(selectedRoot);
        if (_projectLocator.IsUnityProject(context.UnityProjectRoot))
        {
            return context;
        }

        var edited = _dialogService.EditProject(context, Owner);
        if (edited is null)
        {
            return null;
        }

        if (!_projectLocator.CanBootstrapUnityProject(edited))
        {
            throw new InvalidOperationException(
                "For a new project, the Game Project folder must be a direct child "
                + "of an existing Repository Root.");
        }

        return edited;
    }

    private void RemoveTarget(TargetProjectViewModel? target)
    {
        if (target is null)
        {
            return;
        }

        Targets.Remove(target);
        if (ReferenceEquals(SelectedTarget, target))
        {
            SelectedTarget = null;
        }

        InvalidatePreview();
    }

    private void SelectAsSource(TargetProjectViewModel? target)
    {
        if (target is null)
        {
            return;
        }

        var previousSource = CreateSourceContext();
        var nextSource = target.Model.Clone();
        Targets.Remove(target);

        SourceRepositoryRoot = nextSource.RepositoryRoot;
        SourceUnityProjectRoot = nextSource.UnityProjectRoot;

        if (Directory.Exists(previousSource.RepositoryRoot)
            && _projectLocator.IsUnityProject(previousSource.UnityProjectRoot)
            && !Targets.Any(item => item.RepositoryRoot.Equals(
                previousSource.RepositoryRoot,
                StringComparison.OrdinalIgnoreCase)))
        {
            Targets.Add(CreateTargetViewModel(previousSource));
        }

        SelectedTarget = null;
        InvalidatePreview();
    }

    private async Task PreviewAsync()
    {
        var readinessErrors = GetPreviewReadinessErrors();
        if (readinessErrors.Count > 0)
        {
            _preview = null;
            ProgressValue = 0;
            Messages.Clear();
            foreach (var error in readinessErrors)
            {
                Messages.Add($"ERROR: {error}");
            }

            SummaryText = "Preview cannot start. Check the configuration details below.";
            StatusText = "Preview configuration incomplete.";
            RaiseCommandStates();
            return;
        }

        IsBusy = true;
        IsPreviewing = true;
        IsProgressIndeterminate = false;
        ProgressValue = 0;
        Messages.Clear();
        SummaryText = "Preview in progress. Scanning paths and comparing project files...";
        _operationCancellation = new CancellationTokenSource();

        try
        {
            _preview = null;
            await SaveAsync();
            StatusText = "Checking selected directories...";
            var statusProgress = new Progress<PreviewProgress>(value =>
            {
                StatusText = value.Message;
                IsProgressIndeterminate = value.IsIndeterminate;
                ProgressValue = value.Percentage;
            });
            _preview = await _syncPlanner.CreatePreviewAsync(
                CreateSourceContext(),
                Targets.Select(target => target.Model.Clone()).ToList(),
                SyncItems.Select(item => item.Model.Clone()).ToList(),
                statusProgress,
                _operationCancellation.Token);

            foreach (var error in _preview.Errors)
            {
                Messages.Add($"ERROR: {error}");
            }

            foreach (var target in _preview.Targets)
            {
                foreach (var error in target.Errors)
                {
                    Messages.Add($"ERROR [{target.Target.DisplayName}]: {error}");
                }

                foreach (var warning in target.Warnings)
                {
                    Messages.Add($"WARNING [{target.Target.DisplayName}]: {warning}");
                }

                if (target.RequiresUnityBootstrap)
                {
                    Messages.Add(
                        $"INFO [{target.Target.DisplayName}]: A new Unity project shell will create "
                        + $"{target.UnityBootstrapFiles.Count:N0} missing files without overwriting "
                        + "existing files.");
                }

                if (target.RequiresUnityProjectCreation)
                {
                    Messages.Add(
                        $"INFO [{target.Target.DisplayName}]: Sync Now will create a Unity "
                        + $"{target.Target.UnityEditorVersion} project, then overwrite "
                        + "Packages/manifest.json and packages-lock.json from the main project.");
                }
            }

            foreach (var warning in _preview.Warnings)
            {
                Messages.Add($"WARNING: {warning}");
            }

            if (_preview.RuleFiles.Count > 0)
            {
                Messages.Add(
                    $"INFO: Loaded {_preview.RuleFiles.Count:N0} .gitignore file(s).");
            }

            foreach (var targetViewModel in Targets)
            {
                var targetPreview = _preview.Targets.FirstOrDefault(target =>
                    target.Target.UnityProjectRoot.Equals(
                        targetViewModel.UnityProjectRoot,
                        StringComparison.OrdinalIgnoreCase));
                targetViewModel.ApplyPreview(targetPreview, _preview.UnityEditors);
            }

            var newFiles = _preview.Targets.Sum(
                target => target.NewFileCount + target.UnityBootstrapFiles.Count);
            var overwrittenFiles = _preview.Targets.Sum(target => target.OverwriteFileCount);
            SummaryText =
                $"Included {_preview.Files.Count:N0} files ({FormatBytes(_preview.IncludedBytes)}), "
                + $"excluded {_preview.ExcludedFileCount:N0} files ({FormatBytes(_preview.ExcludedBytes)}), "
                + $"{newFiles:N0} new and {overwrittenFiles:N0} overwrite operations "
                + $"across {_preview.Targets.Count:N0} destinations.";
            ProgressValue = 100;
            StatusText = _preview.HasBlockingErrors ? "Blocked" : "Preview complete";
            await SaveAsync();
        }
        catch (OperationCanceledException)
        {
            StatusText = "Preview cancelled.";
        }
        catch (Exception exception)
        {
            _preview = null;
            StatusText = "Preview failed.";
            Messages.Add($"ERROR: {exception.Message}");
        }
        finally
        {
            _operationCancellation.Dispose();
            _operationCancellation = null;
            IsPreviewing = false;
            IsProgressIndeterminate = false;
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    private List<string> GetPreviewReadinessErrors()
    {
        var errors = new List<string>();
        if (!Directory.Exists(SourceRepositoryRoot))
        {
            errors.Add("The main project root does not exist.");
        }

        if (!_projectLocator.IsUnityProject(SourceUnityProjectRoot))
        {
            errors.Add("The main project does not contain a valid Unity game project.");
        }

        if (SyncItems.Count == 0)
        {
            errors.Add("Select at least one transfer directory in Settings.");
        }

        if (Targets.Count == 0)
        {
            errors.Add("Add at least one destination project.");
        }

        return errors;
    }

    private async Task SyncAsync()
    {
        if (_preview is null)
        {
            return;
        }

        if (!_dialogService.Confirm(
                $"Overwrite existing files in {_preview.Targets.Count} destination projects?\n\n"
                + "Destination-only files will not be deleted. New Unity projects will be created "
                + "with the selected Editor, and package manifest files will be overwritten. "
                + "This operation does not create a backup.",
                "Confirm Sync",
                Owner))
        {
            IsProgressIndeterminate = false;
            ProgressValue = 0;
            StatusText = "Sync cancelled.";
            SummaryText = "Sync was not started. No files were changed.";
            Messages.Clear();
            Messages.Add("CANCELLED: Sync was not started. No files were changed.");
            return;
        }

        IsBusy = true;
        IsProgressIndeterminate = false;
        ProgressValue = 0;
        Messages.Clear();
        SummaryText = "Sync in progress. Validating the preview snapshot...";
        _operationCancellation = new CancellationTokenSource();

        try
        {
            StatusText = "Validating preview snapshot...";
            var progress = new Progress<SyncProgress>(value =>
            {
                IsProgressIndeterminate = value.IsIndeterminate;
                ProgressValue = value.Percentage;
                StatusText = value.SyncItemName switch
                {
                    "Preview Validation" => value.CurrentFile,
                    "Unity Project Creation" =>
                        $"Creating {value.TargetName}: {value.CurrentFile}",
                    "Sync Complete" => "Finalizing sync...",
                    _ =>
                        $"Copying {value.SyncItemName} to {value.TargetName}: {value.CurrentFile}"
                };
            });

            var result = await _syncExecutor.ExecuteAsync(
                _preview,
                progress,
                _operationCancellation.Token);
            _lastLogPath = result.LogPath;

            foreach (var target in result.Targets)
            {
                if (target.Succeeded)
                {
                    Messages.Add(
                        $"COMPLETED [{target.TargetName}]: {target.CopiedFiles:N0} files, "
                        + $"{FormatBytes(target.CopiedBytes)}.");
                }
                else if (target.Cancelled)
                {
                    Messages.Add($"CANCELLED [{target.TargetName}]: partial changes applied.");
                }
                else
                {
                    Messages.Add($"ERROR [{target.TargetName}]: {target.Error}");
                }
            }

            StatusText = result.Cancelled
                ? "Cancelled (partial changes applied)"
                : result.Targets.All(target => target.Succeeded)
                    ? "Completed"
                    : "Completed with errors";
            IsProgressIndeterminate = false;
            if (!result.Cancelled && result.Targets.All(target => target.Succeeded))
            {
                ProgressValue = 100;
            }
            SummaryText = result.LogPath is null
                ? StatusText
                : $"{StatusText}. Log: {result.LogPath}";
            _preview = null;
        }
        catch (OperationCanceledException)
        {
            IsProgressIndeterminate = false;
            StatusText = "Cancelled (partial changes applied)";
            SummaryText = "Sync cancelled. Files already copied were not rolled back.";
            Messages.Add("CANCELLED: Partial changes may have been applied.");
            _preview = null;
        }
        catch (Exception exception)
        {
            IsProgressIndeterminate = false;
            StatusText = "Sync failed.";
            SummaryText = "Sync failed before file copying. No files were changed.";
            Messages.Add($"ERROR: {exception.Message}");
            _preview = null;
        }
        finally
        {
            _operationCancellation.Dispose();
            _operationCancellation = null;
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    private void OpenSettings()
    {
        _dialogService.ShowSettings(this, Owner);
    }

    private TargetProjectViewModel CreateTargetViewModel(ProjectContext context)
    {
        var viewModel = new TargetProjectViewModel(context);
        viewModel.UnityEditorVersionChanged += TargetUnityEditorVersionChanged;
        return viewModel;
    }

    private async void TargetUnityEditorVersionChanged(object? sender, EventArgs e)
    {
        if (sender is not TargetProjectViewModel targetViewModel
            || targetViewModel.SelectedUnityEditor is not { } editor
            || _preview is null)
        {
            return;
        }

        var targetPreview = _preview.Targets.FirstOrDefault(target =>
            target.Target.UnityProjectRoot.Equals(
                targetViewModel.UnityProjectRoot,
                StringComparison.OrdinalIgnoreCase));
        if (targetPreview?.RequiresUnityProjectCreation != true)
        {
            return;
        }

        targetPreview.Target.UnityEditorVersion = editor.Version;
        targetPreview.UnityEditorExecutablePath = editor.ExecutablePath;
        StatusText =
            $"Unity {editor.Version} selected for {targetViewModel.DisplayName}.";
        await SaveAsync();
    }

    private void NotifyMainProject()
    {
        OnPropertyChanged(nameof(MainProjectName));
        OnPropertyChanged(nameof(MainProjectRoot));
        OnPropertyChanged(nameof(MainGameDirectory));
    }

    private void UpdateSourceStatus()
    {
        SourceStatus = _projectLocator.IsUnityProject(SourceUnityProjectRoot)
            ? "Unity project detected"
            : "Not configured";
        RaiseCommandStates();
    }

    private void InvalidatePreview()
    {
        _preview = null;
        foreach (var target in Targets)
        {
            target.InvalidatePreview();
        }

        ProgressValue = 0;
        IsProgressIndeterminate = false;
        SummaryText = "Preview required.";
        if (!IsBusy)
        {
            StatusText = CanPreview ? "Ready to preview." : "Configuration incomplete.";
        }

        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        BrowseRepositoryCommand.RaiseCanExecuteChanged();
        BrowseGameProjectCommand.RaiseCanExecuteChanged();
        AddSyncItemCommand.RaiseCanExecuteChanged();
        EditSyncItemCommand.RaiseCanExecuteChanged();
        RemoveSyncItemCommand.RaiseCanExecuteChanged();
        AddTargetCommand.RaiseCanExecuteChanged();
        EditTargetCommand.RaiseCanExecuteChanged();
        RemoveTargetCommand.RaiseCanExecuteChanged();
        PreviewCommand.RaiseCanExecuteChanged();
        SyncCommand.RaiseCanExecuteChanged();
        OpenSettingsCommand.RaiseCanExecuteChanged();
        SelectAsSourceCommand.RaiseCanExecuteChanged();
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }
}
