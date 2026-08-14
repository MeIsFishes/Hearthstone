using System.Collections.ObjectModel;
using System.IO;
using BbxEditor.Application;
using BbxEditor.Contracts;
using BbxEditor.Diagnostics;
using BbxEditor.Domain;
using BbxEditor.Infrastructure;
using BbxEditor.Wpf.Presentation;
using BbxEditor.Wpf.Services;

namespace BbxEditor.Wpf.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly IDialogService _dialogs;
    private readonly WorkspaceDocumentService _documents = new();
    private readonly SettingsService _settingsService;
    private readonly BbxCommonSettingsService _commonSettingsService = new();
    private readonly string _settingsDirectory;
    private readonly ProjectFileIndexService _projectFileIndex = new();
    private readonly VectorSearchCoordinator _vectorSearch;
    private readonly Dictionary<DocumentViewModel, OpenDocumentWatchState> _openDocumentWatches = [];
    private DesignPlanDirectoryWatch? _designPlanDirectoryWatch;
    private AppSettings _settings;
    private BbxCommonSettings _commonSettings;
    private IReadOnlyList<IndexedProjectFile> _indexedProjectFiles = [];
    private IReadOnlyList<IndexedDesignPlanDocument> _indexedDesignPlanDocuments = [];
    private TaskCatalog _taskCatalog = new([], [], []);
    private BbxMetadataCatalog _metadataCatalog = BbxMetadataCatalog.Empty;
    private DocumentViewModel? _currentDocument;
    private DocumentViewModel? _previewDocument;
    private TaskInstance? _selectedTask;
    private TimelineItem? _selectedTimelineItem;
    private IndexedProjectFile? _selectedExplorerFile;
    private bool _synchronizingExplorerSelection;
    private DesignPlanFileViewModel? _selectedDesignPlanFile;
    private readonly HashSet<string> _knownExplorerMods = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _selectedExplorerMods = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<ProjectFileKind> _selectedExplorerFileTypes = new(Enum.GetValues<ProjectFileKind>());
    private string _explorerStatusText = "Select a game project directory to browse files.";
    private string _explorerSearchText = string.Empty;
    private string _designPlanSearchText = string.Empty;
    private string _designPlanStatusText = "Select the Design Plan tab to browse documents.";
    private int _explorerTabIndex;
    private int _indexRequestVersion;
    private int _searchRequestVersion;
    private int _designPlanSearchRequestVersion;
    private bool _vectorSearchEnabledDraft;
    private string _modelDirectoryDraft = string.Empty;
    private string _vectorSearchStatusText = "Vector search is disabled.";
    private bool _projectFileIndexReady;
    private bool _initializationStarted;
    private bool _disposed;

    public MainViewModel(IDialogService dialogs)
    {
        _dialogs = dialogs;
        var settingsFilePath = ResolveSettingsFilePath();
        _settingsDirectory = Path.GetDirectoryName(settingsFilePath) ?? AppContext.BaseDirectory;
        _settingsService = new SettingsService(settingsFilePath);
        _settings = _settingsService.Load();
        _commonSettings = _commonSettingsService.Load();
        _vectorSearchEnabledDraft = _settings.VectorSearchEnabled;
        _modelDirectoryDraft = _commonSettings.ModelDirectory;
        _vectorSearch = new VectorSearchCoordinator(
            Path.Combine(_settingsDirectory, "vector-index.json"),
            csvCacheFile: Path.Combine(_settingsDirectory, "csv-vector-index.tmp.json"));
        _vectorSearch.StatusChanged += OnVectorSearchStatusChanged;
        _projectFileIndex.IndexChanged += OnProjectFileIndexChanged;

        NewTimelineCommand = new RelayCommand(NewTimeline);
        NewBehaviorTreeCommand = new RelayCommand(NewBehaviorTree);
        NewCsvCommand = new RelayCommand(NewCsv);
        OpenCommand = new RelayCommand(Open);
        SaveCommand = new RelayCommand(() => Save(false), () => CurrentDocument?.CanSave == true);
        SaveAsCommand = new RelayCommand(() => Save(true), () => CurrentDocument?.CanSave == true);
        ReloadCatalogCommand = new RelayCommand(ReloadCatalog);
        BrowseCatalogCommand = new RelayCommand(BrowseCatalog);
        BrowseProjectCommand = new RelayCommand(BrowseProject);
        BrowseModelDirectoryCommand = new RelayCommand(BrowseModelDirectory);
        ApplyVectorSearchSettingsCommand = new RelayCommand(() => _ = ApplyVectorSearchSettingsAsync());
        RefreshExplorerCommand = new RelayCommand(() => _ = RefreshExplorerAsync());
        OpenExplorerFileCommand = new RelayCommand(OpenSelectedExplorerFile, () => SelectedExplorerFile is not null);
        OpenDesignPlanCommand = new RelayCommand(OpenSelectedDesignPlan, () => SelectedDesignPlanFile is not null);
        BuildExplorerFileTypeFilters();

        RefreshRecentFileMenu();
    }

    public async Task InitializeAsync()
    {
        if (_initializationStarted || _disposed) return;
        _initializationStarted = true;
        try
        {
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            if (_disposed) return;

            ReloadCatalogCore(false);
            if (_disposed) return;

            await RestartExplorerIndexAsync();
            if (_disposed) return;

            RestartDesignPlanIndex();
            if (_disposed) return;

            await ApplyVectorSearchConfigurationAsync();
        }
        catch (OperationCanceledException) when (_disposed)
        {
        }
        catch (ObjectDisposedException) when (_disposed)
        {
        }
        catch (Exception exception)
        {
            SetStatus("Startup initialization failed: " + exception.Message, true);
        }
    }

    public ObservableCollection<DocumentViewModel> Documents { get; } = [];
    public ObservableCollection<RecentFileMenuItemViewModel> RecentFileMenuItems { get; } = [];
    public ObservableCollection<IndexedProjectFile> ExplorerFiles { get; } = [];
    public ObservableCollection<ExplorerModFilterViewModel> ExplorerModFilters { get; } = [];
    public ObservableCollection<ExplorerFileTypeFilterViewModel> ExplorerFileTypeFilters { get; } = [];
    public ObservableCollection<DesignPlanDateViewModel> DesignPlanDates { get; } = [];
    public ObservableCollection<DesignPlanFileViewModel> DesignPlanSearchResults { get; } = [];
    public ApplicationLog Log { get; } = new();
    public RelayCommand NewTimelineCommand { get; }
    public RelayCommand NewBehaviorTreeCommand { get; }
    public RelayCommand NewCsvCommand { get; }
    public RelayCommand OpenCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand SaveAsCommand { get; }
    public RelayCommand ReloadCatalogCommand { get; }
    public RelayCommand BrowseCatalogCommand { get; }
    public RelayCommand BrowseProjectCommand { get; }
    public RelayCommand BrowseModelDirectoryCommand { get; }
    public RelayCommand ApplyVectorSearchSettingsCommand { get; }
    public RelayCommand RefreshExplorerCommand { get; }
    public RelayCommand OpenExplorerFileCommand { get; }
    public RelayCommand OpenDesignPlanCommand { get; }

    public TaskCatalog Catalog
    {
        get => _taskCatalog;
        private set
        {
            if (SetProperty(ref _taskCatalog, value))
            {
                RaisePropertyChanged(nameof(Contexts));
                RaisePropertyChanged(nameof(SelectedContext));
            }
        }
    }

    public BbxMetadataCatalog MetadataCatalog
    {
        get => _metadataCatalog;
        private set => SetProperty(ref _metadataCatalog, value);
    }

    private EditorCatalog EditorCatalog => new(Catalog, MetadataCatalog);
    public IReadOnlyList<TaskContextDefinition> Contexts => Catalog.Contexts;

    public string MetadataPath
    {
        get => _settings.MetadataPath;
        set
        {
            if (_settings.MetadataPath == value) return;
            _settings.MetadataPath = value;
            _settingsService.Save(_settings);
            RaisePropertyChanged();
        }
    }

    public string GameProjectPath
    {
        get => _settings.GameProjectPath;
        set
        {
            if (_settings.GameProjectPath == value) return;
            _settings.GameProjectPath = value;
            _settingsService.Save(_settings);
            RaisePropertyChanged();
            _ = RestartExplorerIndexAsync();
            RestartDesignPlanIndex();
        }
    }

    public string ExplorerDirectoriesText
    {
        get => string.Join(Environment.NewLine, _settings.ExplorerDirectories);
        set
        {
            var directories = (value ?? string.Empty)
                .Split(['\r', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (_settings.ExplorerDirectories.SequenceEqual(directories, StringComparer.OrdinalIgnoreCase)) return;
            _settings.ExplorerDirectories = directories;
            _settingsService.Save(_settings);
            RaisePropertyChanged();
            _ = RestartExplorerIndexAsync();
        }
    }

    public bool VectorSearchEnabled
    {
        get => _vectorSearchEnabledDraft;
        set => SetProperty(ref _vectorSearchEnabledDraft, value);
    }

    public string ModelDirectory
    {
        get => _modelDirectoryDraft;
        set => SetProperty(ref _modelDirectoryDraft, value ?? string.Empty);
    }

    public string VectorSearchStatusText
    {
        get => _vectorSearchStatusText;
        private set => SetProperty(ref _vectorSearchStatusText, value);
    }

    public IndexedProjectFile? SelectedExplorerFile
    {
        get => _selectedExplorerFile;
        set
        {
            if (!SetProperty(ref _selectedExplorerFile, value)) return;
            OpenExplorerFileCommand.RaiseCanExecuteChanged();
            if (value is not null && !_synchronizingExplorerSelection) PreviewExplorerFile(value);
        }
    }

    public DesignPlanFileViewModel? SelectedDesignPlanFile
    {
        get => _selectedDesignPlanFile;
        set
        {
            if (!SetProperty(ref _selectedDesignPlanFile, value)) return;
            OpenDesignPlanCommand.RaiseCanExecuteChanged();
            if (value is not null) PreviewDesignPlan(value.Document);
        }
    }

    public int ExplorerTabIndex
    {
        get => _explorerTabIndex;
        set
        {
            if (!SetProperty(ref _explorerTabIndex, value)) return;
            if (value == 1) RefreshDesignPlanView();
        }
    }

    public string DesignPlanSearchText
    {
        get => _designPlanSearchText;
        set
        {
            if (!SetProperty(ref _designPlanSearchText, value ?? string.Empty)) return;
            RaisePropertyChanged(nameof(IsDesignPlanSearchActive));
            RefreshDesignPlanView();
        }
    }

    public bool IsDesignPlanSearchActive => !string.IsNullOrWhiteSpace(DesignPlanSearchText);

    public string DesignPlanStatusText
    {
        get => _designPlanStatusText;
        private set => SetProperty(ref _designPlanStatusText, value);
    }

    public string ExplorerStatusText { get => _explorerStatusText; private set => SetProperty(ref _explorerStatusText, value); }
    public string ExplorerModFilterText => _selectedExplorerMods.Count == _knownExplorerMods.Count
        ? "All Mods"
        : _selectedExplorerMods.Count switch
        {
            0 => "No Mods",
            1 => _selectedExplorerMods.First(),
            _ => $"{_selectedExplorerMods.Count} Mods",
        };
    public string ExplorerFileTypeFilterText => _selectedExplorerFileTypes.Count == Enum.GetValues<ProjectFileKind>().Length
        ? "All Types"
        : _selectedExplorerFileTypes.Count switch
        {
            0 => "No Types",
            1 => GetFileTypeLabel(_selectedExplorerFileTypes.First()),
            _ => $"{_selectedExplorerFileTypes.Count} Types",
        };
    public string ExplorerFilterText => $"{ExplorerModFilterText} · {ExplorerFileTypeFilterText}";
    public string ExplorerSearchText
    {
        get => _explorerSearchText;
        set
        {
            if (!SetProperty(ref _explorerSearchText, value ?? string.Empty)) return;
            RefreshExplorerView();
            ExplorerStatusText = $"{ExplorerFiles.Count} files in {ExplorerFilterText}.";
        }
    }

    public DocumentViewModel? CurrentDocument
    {
        get => _currentDocument;
        set
        {
            if (SetProperty(ref _currentDocument, value))
            {
                SelectedTask = null;
                SelectedTimelineItem = null;
                RaisePropertyChanged(nameof(SelectedContext));
                SaveCommand.RaiseCanExecuteChanged();
                SaveAsCommand.RaiseCanExecuteChanged();
                SynchronizeExplorerSelection();
            }
        }
    }

    public TaskContextDefinition? SelectedContext
    {
        get => CurrentDocument?.TaskDocument is { } task ? Catalog.FindContext(task.BindingContextType) : null;
        set
        {
            if (CurrentDocument?.TaskDocument is not { } task || value is null) return;
            task.BindingContextType = value.TypeName;
            RaisePropertyChanged();
        }
    }

    public TaskInstance? SelectedTask { get => _selectedTask; set => SetProperty(ref _selectedTask, value); }
    public TimelineItem? SelectedTimelineItem { get => _selectedTimelineItem; set => SetProperty(ref _selectedTimelineItem, value); }
    public void SetStatus(string text, bool error = false)
    {
        Log.Add(text, error ? DiagnosticSeverity.Error : DiagnosticSeverity.Info);
    }

    internal void RecordDiagnostics(IEnumerable<Diagnostic> diagnostics) => Log.AddRange(diagnostics);

    public TaskDefinition? SelectTask(string title, string description, Func<TaskDefinition, bool> predicate)
    {
        var candidates = Catalog.Tasks.Where(predicate).OrderBy(task => task.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToArray();
        if (candidates.Length > 0) return _dialogs.SelectTask(candidates, title, description, RankVectorNamesAsync);
        SetStatus($"No task types are available for \"{title}\". Check the metadata directory.", true);
        return null;
    }

    private Task<IReadOnlyList<string>> RankVectorNamesAsync(
        string query, IReadOnlyCollection<string> candidateNames, CancellationToken cancellationToken) =>
        _vectorSearch.RankNamesAsync(query, candidateNames, cancellationToken);

    internal Task<IReadOnlyList<string>> RankCsvColumnValuesAsync(
        string columnKey,
        string query,
        IReadOnlyCollection<string> candidateValues,
        CancellationToken cancellationToken) =>
        _vectorSearch.RankCsvColumnValuesAsync(columnKey, query, candidateValues, cancellationToken);

    internal Task<TransientVectorIndex?> BuildTransientVectorIndexAsync(
        IReadOnlyCollection<string> candidateTexts,
        CancellationToken cancellationToken) =>
        _vectorSearch.BuildTransientIndexAsync(candidateTexts, cancellationToken);

    internal Task<IReadOnlyList<string>> RankTransientVectorIndexAsync(
        string query,
        TransientVectorIndex index,
        CancellationToken cancellationToken) =>
        _vectorSearch.RankTransientIndexAsync(query, index, cancellationToken);

    internal IReadOnlyList<CsvAssociationTarget> ResolveAssociatedCsvTargets(CsvDocument document) =>
        CsvAssociationTargetResolver.Resolve(document, MetadataCatalog, _indexedProjectFiles, _projectFileIndexReady);

    internal void OpenAssociatedCsv(CsvAssociationTarget target)
    {
        if (!target.CanOpen || target.File is null)
        {
            SetStatus(target.UnavailableReason ?? "The associated CSV target is unavailable.", true);
            return;
        }
        OpenDocument(target.File.FullPath);
    }

    public void CloseDocument(DocumentViewModel document)
    {
        if (document.Document.IsDirty && !_dialogs.Confirm("Unsaved Changes", $"{document.Header} has unsaved changes. Close it anyway?")) return;
        var index = Documents.IndexOf(document);
        if (ReferenceEquals(_previewDocument, document)) _previewDocument = null;
        DetachDocumentWatch(document);
        document.Dispose();
        Documents.Remove(document);
        if (CurrentDocument == document) CurrentDocument = Documents.Count == 0 ? null : Documents[Math.Clamp(index - 1, 0, Documents.Count - 1)];
    }

    public void PinCurrentPreviewDocument()
    {
        if (CurrentDocument is { } document) PinPreviewDocument(document);
    }

    public void PinPreviewDocument(DocumentViewModel document)
    {
        if (!document.IsPreview || !ReferenceEquals(document, _previewDocument)) return;
        PinDocument(document);
    }

    private void NewTimeline() => CreateTaskDocument(new TimelineDocument(), "NewTimeline.editor.json");
    private void NewBehaviorTree() => CreateTaskDocument(new BehaviorTreeDocument(), "NewBehaviorTree.editor.json");

    private void NewCsv()
    {
        var path = _dialogs.SaveDocumentFile(_settings.LastDocumentPath, ".csv", "NewData.csv");
        if (path is null) return;
        var document = new CsvDocument { FilePath = path };
        document.Columns.Add("Id");
        document.EnableChangeTracking();
        document.IsDirty = true;
        AddDocument(document);
        SetStatus($"Created a new CSV document. Save target: {path}");
    }

    private void CreateTaskDocument(TaskDocument document, string suggestedFileName)
    {
        var path = _dialogs.SaveDocumentFile(_settings.LastDocumentPath, ".editor.json", suggestedFileName);
        if (path is null) return;
        document.FilePath = path;
        AddDocument(document);
        document.IsDirty = true;
        _settings.LastDocumentPath = path;
        _settingsService.Save(_settings);
        SetStatus($"Created a new {document.Kind}. Save target: {path}");
    }

    private DocumentViewModel AddDocument(EditorDocument document, bool isPreview = false)
    {
        if (document is TaskDocument task && string.IsNullOrWhiteSpace(task.BindingContextType) && Contexts.Count > 0)
        {
            task.BindingContextType = Contexts[0].TypeName;
            task.IsDirty = false;
        }
        var viewModel = CreateDocumentViewModel(document);
        viewModel.IsPreview = isPreview;
        if (!isPreview) viewModel.OnPinned();
        Documents.Add(viewModel);
        if (isPreview) _previewDocument = viewModel;
        CurrentDocument = viewModel;
        AttachDocumentWatch(viewModel);
        return viewModel;
    }

    private DocumentViewModel CreateDocumentViewModel(EditorDocument document) => document switch
    {
        TimelineDocument timeline => new TimelineDocumentViewModel(timeline, this),
        BehaviorTreeDocument tree => new BehaviorTreeDocumentViewModel(tree, this),
        CsvDocument csv => new CsvDocumentViewModel(csv, this),
        ScriptableObjectDocument scriptableObject => new ScriptableObjectDocumentViewModel(scriptableObject, this),
        DesignPlanDocument designPlan => new DesignPlanDocumentViewModel(designPlan, this),
        _ => throw new NotSupportedException(document.GetType().Name),
    };

    private void Open()
    {
        var path = _dialogs.OpenDocumentFile(_settings.LastDocumentPath);
        if (path is not null) OpenDocument(path);
    }

    private void OpenRecentDocument(string path)
    {
        if (!File.Exists(path))
        {
            _settings.RemoveRecentDocument(path);
            _settingsService.Save(_settings);
            RefreshRecentFileMenu();
            SetStatus($"The recent file does not exist and was removed from the list: {path}", true);
            _dialogs.Show("File Not Found", $"The recent file could not be found and was removed from the list:{Environment.NewLine}{path}", true);
            return;
        }
        OpenDocument(path);
    }

    private void OpenDocument(string path, bool preview = false)
    {
        var fullPath = Path.GetFullPath(path);
        var existing = Documents.FirstOrDefault(document =>
            !string.IsNullOrWhiteSpace(document.Document.FilePath) &&
            Path.GetFullPath(document.Document.FilePath).Equals(fullPath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            if (!preview) PinDocument(existing);
            CurrentDocument = existing;
            SetStatus($"Already open: {fullPath}");
            return;
        }
        var result = _documents.Open(path, EditorCatalog);
        if (result.Value is null) { ShowDiagnostics("Open Failed", result.Diagnostics); return; }
        if (preview) ReplacePreviewDocument();
        AddDocument(result.Value, preview);
        if (!preview) RecordRecentDocument(path);
        SetStatus(preview ? $"Previewing: {path}" : $"Opened: {path}");
        RecordDiagnostics(result.Diagnostics);
    }

    private void PreviewExplorerFile(IndexedProjectFile file)
    {
        if (!File.Exists(file.FullPath))
        {
            SetStatus($"The indexed file no longer exists: {file.FullPath}", true);
            _ = RefreshExplorerAsync();
            return;
        }
        OpenDocument(file.FullPath, true);
    }

    private void PreviewDesignPlan(IndexedDesignPlanDocument document) => OpenDesignPlan(document, true);

    internal bool TryOpenDesignPlanLink(Uri linkUri)
    {
        var document = DesignPlanIndexService.FindLinkedDocument(_indexedDesignPlanDocuments, linkUri);
        if (document is null) return false;
        OpenDesignPlan(document, false);
        return true;
    }

    internal void OpenAssociatedDesignPlan(string path, string tabTitle) =>
        OpenDesignPlan(path, false, false, tabTitle);

    private void OpenDesignPlan(IndexedDesignPlanDocument document, bool preview) =>
        OpenDesignPlan(document.FullPath, preview, true, null);

    private void OpenDesignPlan(string path, bool preview, bool reloadIndexWhenMissing, string? tabTitleOverride)
    {
        if (!File.Exists(path))
        {
            SetStatus($"The design plan no longer exists: {path}", true);
            if (reloadIndexWhenMissing) ReloadDesignPlanIndex();
            return;
        }

        var fullPath = Path.GetFullPath(path);
        var existing = Documents.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(item.Document.FilePath) &&
            Path.GetFullPath(item.Document.FilePath).Equals(fullPath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            if (!preview) PinDocument(existing);
            CurrentDocument = existing;
            SetStatus(preview ? $"Previewing design plan: {fullPath}" : $"Opened design plan: {fullPath}");
            return;
        }

        try
        {
            var content = DesignPlanIndexService.LoadContent(fullPath);
            var designPlan = new DesignPlanDocument
            {
                FilePath = fullPath,
                Title = string.IsNullOrWhiteSpace(tabTitleOverride) ? content.Title : tabTitleOverride,
                State = content.State,
                Priority = content.Priority,
                PlanPath = content.PlanPath,
                ReviewPath = content.ReviewPath,
                TabTitleOverride = tabTitleOverride,
                Markdown = content.MarkdownBody,
            };
            if (preview) ReplacePreviewDocument();
            AddDocument(designPlan, preview);
            SetStatus(preview ? $"Previewing design plan: {fullPath}" : $"Opened design plan: {fullPath}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SetStatus("Could not read the design plan: " + exception.Message, true);
        }
    }

    private void ReplacePreviewDocument()
    {
        if (_previewDocument is null) return;
        if (_previewDocument.Document.IsDirty)
        {
            PinDocument(_previewDocument);
            return;
        }
        var preview = _previewDocument;
        _previewDocument = null;
        DetachDocumentWatch(preview);
        preview.Dispose();
        Documents.Remove(preview);
    }

    private void PinDocument(DocumentViewModel document)
    {
        if (!document.IsPreview) return;
        document.IsPreview = false;
        document.OnPinned();
        if (ReferenceEquals(_previewDocument, document)) _previewDocument = null;
        if (document.TracksRecentFiles && !string.IsNullOrWhiteSpace(document.Document.FilePath))
            RecordRecentDocument(document.Document.FilePath);
        SetStatus($"Pinned: {document.Document.FilePath}");
    }

    private void Save(bool saveAs)
    {
        if (CurrentDocument is null) return;
        PinDocument(CurrentDocument);
        var document = CurrentDocument.Document;
        var path = document.FilePath;
        if (saveAs || string.IsNullOrWhiteSpace(path))
        {
            path = _dialogs.SaveDocumentFile(path ?? _settings.LastDocumentPath, GetExtension(document));
            if (path is null) return;
        }
        var result = _documents.Save(document, EditorCatalog, path);
        if (!result.Success) { ShowDiagnostics("Save Failed", result.Diagnostics); return; }
        AttachDocumentWatch(CurrentDocument);
        RecordRecentDocument(result.Value!);
        SetStatus($"Saved: {result.Value}");
    }

    private void AttachDocumentWatch(DocumentViewModel document)
    {
        DetachDocumentWatch(document);
        var path = document.Document.FilePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

        var fullPath = Path.GetFullPath(path);
        var fingerprint = OpenDocumentFileWatch.ReadFingerprint(fullPath);
        var registration = new OpenDocumentFileWatch(fullPath, changedPath => _ = CheckWatchedDocumentAsync(document, changedPath));
        _openDocumentWatches[document] = new OpenDocumentWatchState(fullPath, fingerprint, registration);
    }

    private void DetachDocumentWatch(DocumentViewModel document)
    {
        if (_openDocumentWatches.Remove(document, out var state)) state.Dispose();
    }

    private async Task CheckWatchedDocumentAsync(DocumentViewModel document, string path)
    {
        string? fingerprint = null;
        for (var attempt = 0; attempt < 4 && fingerprint is null; attempt++)
        {
            fingerprint = await Task.Run(() => OpenDocumentFileWatch.ReadFingerprint(path)).ConfigureAwait(false);
            if (fingerprint is null) await Task.Delay(150).ConfigureAwait(false);
        }
        if (fingerprint is null) return;

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            await dispatcher.InvokeAsync(() => ApplyExternalFileChange(document, path, fingerprint));
            return;
        }
        ApplyExternalFileChange(document, path, fingerprint);
    }

    private void ApplyExternalFileChange(DocumentViewModel document, string path, string fingerprint)
    {
        if (!_openDocumentWatches.TryGetValue(document, out var state) ||
            !state.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase) ||
            state.Fingerprint == fingerprint)
        {
            return;
        }

        if (fingerprint == "<missing>")
        {
            state.Fingerprint = fingerprint;
            SetStatus($"File deleted on disk; the open tab is keeping its in-memory content: {path}", true);
            _dialogs.Show("File Deleted on Disk",
                $"The file was deleted by another program. Its tab remains open with the in-memory content:{Environment.NewLine}{path}", true);
            return;
        }

        if (document.Document.IsDirty && _dialogs.ResolveExternalFileChange(path) == ExternalFileChangeChoice.KeepLocal)
        {
            state.Fingerprint = fingerprint;
            SetStatus($"Kept local edits after an external file change: {path}", true);
            return;
        }

        ReloadDocumentFromDisk(document, path);
    }

    private void ReloadDocumentFromDisk(DocumentViewModel document, string path)
    {
        if (document is DesignPlanDocumentViewModel)
        {
            try
            {
                var content = DesignPlanIndexService.LoadContent(path);
                var tabTitleOverride = ((DesignPlanDocumentViewModel)document).DesignPlan.TabTitleOverride;
                ReplaceReloadedDocument(document, new DesignPlanDocument
                {
                    FilePath = path,
                    Title = string.IsNullOrWhiteSpace(tabTitleOverride) ? content.Title : tabTitleOverride,
                    State = content.State,
                    Priority = content.Priority,
                    PlanPath = content.PlanPath,
                    ReviewPath = content.ReviewPath,
                    TabTitleOverride = tabTitleOverride,
                    Markdown = content.MarkdownBody,
                });
                SetStatus($"Reloaded design plan after an external file change: {path}");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                SetStatus("Could not reload the externally changed design plan: " + exception.Message, true);
            }
            return;
        }

        var result = _documents.Open(path, EditorCatalog);
        if (result.Value is null)
        {
            SetStatus($"Could not reload the externally changed file: {path}", true);
            RecordDiagnostics(result.Diagnostics);
            return;
        }

        ReplaceReloadedDocument(document, result.Value);
        SetStatus($"Reloaded after an external file change: {path}");
        RecordDiagnostics(result.Diagnostics);
    }

    private void ReplaceReloadedDocument(DocumentViewModel document, EditorDocument reloadedDocument)
    {
        var index = Documents.IndexOf(document);
        if (index < 0) return;
        var wasCurrent = ReferenceEquals(CurrentDocument, document);
        var wasPreview = document.IsPreview;
        var wasTrackedPreview = ReferenceEquals(_previewDocument, document);
        DetachDocumentWatch(document);

        var replacement = CreateDocumentViewModel(reloadedDocument);
        replacement.IsPreview = wasPreview;
        if (!wasPreview) replacement.OnPinned();
        Documents[index] = replacement;
        document.Dispose();
        if (wasTrackedPreview) _previewDocument = replacement;
        if (wasCurrent) CurrentDocument = replacement;
        AttachDocumentWatch(replacement);
    }

    private static string GetExtension(EditorDocument document) => document switch
    {
        TaskDocument => ".editor.json",
        CsvDocument => ".csv",
        ScriptableObjectDocument => ".asset",
        _ => string.Empty,
    };

    private void RecordRecentDocument(string path)
    {
        _settings.RecordRecentDocument(path, 10);
        _settingsService.Save(_settings);
        RefreshRecentFileMenu();
    }

    private void RefreshRecentFileMenu()
    {
        RecentFileMenuItems.Clear();
        foreach (var path in _settings.RecentDocumentPaths.Take(10)) RecentFileMenuItems.Add(new RecentFileMenuItemViewModel(path, OpenRecentDocument));
        if (RecentFileMenuItems.Count == 0) RecentFileMenuItems.Add(RecentFileMenuItemViewModel.Empty);
    }

    private void ReloadCatalog() => ReloadCatalogCore(true);

    private void ReloadCatalogCore(bool restartExplorer)
    {
        var metadataPath = ResolveConfiguredPath(MetadataPath);
        if (!Directory.Exists(metadataPath))
        {
            Catalog = new TaskCatalog([], [], []);
            MetadataCatalog = BbxMetadataCatalog.Empty;
            SetStatus("Select a BbxEditor metadata directory to enable typed Task, CSV, and BbxScriptableObject editing.", true);
            if (restartExplorer) _ = RestartExplorerIndexAsync();
            return;
        }

        var taskDirectory = TaskMetadataDirectoryResolver.Resolve(metadataPath);
        var taskResult = TaskCatalog.LoadFromDirectory(taskDirectory);
        var metadataResult = BbxMetadataCatalog.LoadFromDirectory(metadataPath);
        Catalog = taskResult.Value ?? new TaskCatalog([], [], []);
        MetadataCatalog = metadataResult.Value ?? BbxMetadataCatalog.Empty;
        foreach (var document in Documents.Select(item => item.TaskDocument).Where(item => item is not null)) _ = TaskReconciler.Reconcile(document!, Catalog);

        var diagnostics = taskResult.Diagnostics.Concat(metadataResult.Diagnostics).ToArray();
        var summary = $"Imported {Catalog.Tasks.Count} tasks, {Catalog.Contexts.Count} contexts, {MetadataCatalog.CsvTypes.Count} CSV types, and {MetadataCatalog.ScriptableObjectTypes.Count} BbxScriptableObject types.";
        SetStatus(summary);
        RecordDiagnostics(diagnostics);
        if (restartExplorer) _ = RestartExplorerIndexAsync();
    }

    private void BrowseCatalog()
    {
        var path = _dialogs.SelectFolder(ResolveConfiguredPath(MetadataPath));
        if (path is null) return;
        MetadataPath = MakeSettingsRelativePath(path);
        ReloadCatalog();
    }

    private void BrowseProject()
    {
        var path = _dialogs.SelectFolder(ResolveConfiguredPath(GameProjectPath), "Select the Unity Game Project Directory");
        if (path is not null) GameProjectPath = MakeSettingsRelativePath(path);
    }

    private void BrowseModelDirectory()
    {
        var path = _dialogs.SelectFolder(ModelDirectory, "Select the Shared Embedding Model Directory");
        if (path is not null) ModelDirectory = path;
    }

    private async Task ApplyVectorSearchSettingsAsync()
    {
        _settings.VectorSearchEnabled = VectorSearchEnabled;
        _settingsService.Save(_settings);
        _commonSettings.ModelDirectory = ModelDirectory.Trim();
        _commonSettingsService.Save(_commonSettings);
        await ApplyVectorSearchConfigurationAsync();
    }

    private async Task ApplyVectorSearchConfigurationAsync()
    {
        await _vectorSearch.ApplyConfigurationAsync(_settings.VectorSearchEnabled, _commonSettings.ModelDirectory);
        foreach (var document in Documents.OfType<BehaviorTreeDocumentViewModel>().Where(document => !document.IsPreview))
            document.RebuildNodeSearchIndex();
        SynchronizeVectorCorpus();
    }

    private async Task RestartExplorerIndexAsync()
    {
        var requestVersion = ++_indexRequestVersion;
        _projectFileIndex.Stop();
        _indexedProjectFiles = [];
        _projectFileIndexReady = false;
        RefreshExplorerView();
        var gameProjectPath = ResolveConfiguredPath(GameProjectPath);
        if (!Directory.Exists(gameProjectPath))
        {
            SynchronizeVectorCorpus();
            ExplorerStatusText = "Select a valid game project directory to browse files.";
            return;
        }

        ExplorerStatusText = "Indexing project files…";
        var catalog = EditorCatalog;
        var searchDirectories = _settings.ExplorerDirectories.ToArray();
        try
        {
            await _projectFileIndex.StartAsync(gameProjectPath, searchDirectories, path => ClassifyExplorerFile(path, catalog));
        }
        catch (Exception exception) when (requestVersion == _indexRequestVersion)
        {
            ExplorerStatusText = "Index failed: " + exception.Message;
            SetStatus(ExplorerStatusText, true);
        }
    }

    private void RestartDesignPlanIndex()
    {
        _designPlanDirectoryWatch?.Dispose();
        _designPlanDirectoryWatch = null;
        _indexedDesignPlanDocuments = [];
        var gameProjectPath = ResolveConfiguredPath(GameProjectPath);
        if (!Directory.Exists(gameProjectPath))
        {
            RefreshDesignPlanView();
            SynchronizeVectorCorpus();
            DesignPlanStatusText = "Select a valid game project directory to browse design plans.";
            return;
        }

        _designPlanDirectoryWatch = new DesignPlanDirectoryWatch(gameProjectPath, OnDesignPlanDirectoryChanged);
        ReloadDesignPlanIndex();
    }

    private void OnDesignPlanDirectoryChanged()
    {
        if (_disposed) return;
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            _ = dispatcher.InvokeAsync(ReloadDesignPlanIndex);
            return;
        }
        ReloadDesignPlanIndex();
    }

    private void ReloadDesignPlanIndex()
    {
        if (_disposed) return;
        var gameProjectPath = ResolveConfiguredPath(GameProjectPath);
        try
        {
            _indexedDesignPlanDocuments = DesignPlanIndexService.Scan(gameProjectPath)
                .SelectMany(date => date.Documents)
                .ToArray();
            SynchronizeVectorCorpus();
            RefreshDesignPlanView();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _indexedDesignPlanDocuments = [];
            SynchronizeVectorCorpus();
            RefreshDesignPlanView();
            DesignPlanStatusText = "Design plan scan failed: " + exception.Message;
            SetStatus(DesignPlanStatusText, true);
        }
    }

    private async Task RefreshExplorerAsync()
    {
        ExplorerStatusText = "Refreshing project files…";
        await _projectFileIndex.RefreshAsync();
    }

    private ProjectFileClassification? ClassifyExplorerFile(string path, EditorCatalog catalog)
    {
        var result = _documents.Open(path, catalog);
        return result.Value switch
        {
            TimelineDocument => new ProjectFileClassification(ProjectFileKind.Task, TaskFileEditorKind.Timeline),
            BehaviorTreeDocument => new ProjectFileClassification(ProjectFileKind.Task, TaskFileEditorKind.BehaviorTree),
            CsvDocument => new ProjectFileClassification(ProjectFileKind.Csv),
            ScriptableObjectDocument => new ProjectFileClassification(ProjectFileKind.ScriptableObject),
            _ => null,
        };
    }

    private void OnProjectFileIndexChanged(object? sender, ProjectFileIndexChangedEventArgs args)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            _ = dispatcher.InvokeAsync(() => ApplyProjectFileIndex(args));
            return;
        }
        ApplyProjectFileIndex(args);
    }

    private void ApplyProjectFileIndex(ProjectFileIndexChangedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.Error))
        {
            _projectFileIndexReady = false;
            ExplorerStatusText = "Index failed: " + args.Error;
            return;
        }
        _indexedProjectFiles = args.Files;
        _projectFileIndexReady = true;
        SynchronizeVectorCorpus();
        RebuildExplorerModFilters();
        RefreshExplorerView();
        ExplorerStatusText = $"{ExplorerFiles.Count} files in {ExplorerFilterText}.";
    }

    private void SynchronizeVectorCorpus()
    {
        _vectorSearch.SynchronizeNames(DesignPlanSearchService.BuildVectorCorpus(
            _indexedProjectFiles,
            Catalog.Tasks.Select(task => task.TypeName),
            _indexedDesignPlanDocuments));
    }

    private void RebuildExplorerModFilters()
    {
        var mods = _indexedProjectFiles.Select(file => file.ModName).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(mod => mod.Equals("Native", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(mod => mod, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var previouslyAll = _knownExplorerMods.Count == 0 || _selectedExplorerMods.SetEquals(_knownExplorerMods);
        _knownExplorerMods.Clear();
        _knownExplorerMods.UnionWith(mods);
        _selectedExplorerMods.IntersectWith(_knownExplorerMods);
        if (previouslyAll) _selectedExplorerMods.UnionWith(_knownExplorerMods);
        ExplorerModFilters.Clear();
        ExplorerModFilters.Add(new ExplorerModFilterViewModel("All Mods", null, SelectExplorerMod));
        foreach (var mod in mods) ExplorerModFilters.Add(new ExplorerModFilterViewModel(mod, mod, SelectExplorerMod));
        UpdateExplorerFilterSelection();
        RaisePropertyChanged(nameof(ExplorerModFilterText));
        RaisePropertyChanged(nameof(ExplorerFilterText));
    }

    private void SelectExplorerMod(string? modName)
    {
        if (modName is null)
        {
            if (_selectedExplorerMods.SetEquals(_knownExplorerMods)) _selectedExplorerMods.Clear();
            else _selectedExplorerMods.UnionWith(_knownExplorerMods);
        }
        else if (!_selectedExplorerMods.Add(modName))
        {
            _selectedExplorerMods.Remove(modName);
        }
        RaisePropertyChanged(nameof(ExplorerModFilterText));
        RaisePropertyChanged(nameof(ExplorerFilterText));
        UpdateExplorerFilterSelection();
        RefreshExplorerView();
        ExplorerStatusText = $"{ExplorerFiles.Count} files in {ExplorerFilterText}.";
    }

    private void UpdateExplorerFilterSelection()
    {
        foreach (var filter in ExplorerModFilters)
            filter.IsSelected = filter.ModName is null
                ? _selectedExplorerMods.SetEquals(_knownExplorerMods)
                : _selectedExplorerMods.Contains(filter.ModName);
    }

    private void BuildExplorerFileTypeFilters()
    {
        ExplorerFileTypeFilters.Add(new ExplorerFileTypeFilterViewModel("All Types", null, SelectExplorerFileType));
        ExplorerFileTypeFilters.Add(new ExplorerFileTypeFilterViewModel("Task", ProjectFileKind.Task, SelectExplorerFileType));
        ExplorerFileTypeFilters.Add(new ExplorerFileTypeFilterViewModel("CSV", ProjectFileKind.Csv, SelectExplorerFileType));
        ExplorerFileTypeFilters.Add(new ExplorerFileTypeFilterViewModel("ScriptableObject", ProjectFileKind.ScriptableObject, SelectExplorerFileType));
        UpdateExplorerFileTypeFilterSelection();
    }

    private void SelectExplorerFileType(ProjectFileKind? fileType)
    {
        if (fileType is null)
        {
            var allTypes = Enum.GetValues<ProjectFileKind>();
            if (_selectedExplorerFileTypes.Count == allTypes.Length) _selectedExplorerFileTypes.Clear();
            else _selectedExplorerFileTypes.UnionWith(allTypes);
        }
        else if (!_selectedExplorerFileTypes.Add(fileType.Value))
        {
            _selectedExplorerFileTypes.Remove(fileType.Value);
        }
        RaisePropertyChanged(nameof(ExplorerFileTypeFilterText));
        RaisePropertyChanged(nameof(ExplorerFilterText));
        UpdateExplorerFileTypeFilterSelection();
        RefreshExplorerView();
        ExplorerStatusText = $"{ExplorerFiles.Count} files in {ExplorerFilterText}.";
    }

    private void UpdateExplorerFileTypeFilterSelection()
    {
        foreach (var filter in ExplorerFileTypeFilters)
            filter.IsSelected = filter.FileType is null
                ? _selectedExplorerFileTypes.Count == Enum.GetValues<ProjectFileKind>().Length
                : _selectedExplorerFileTypes.Contains(filter.FileType.Value);
    }

    private static string GetFileTypeLabel(ProjectFileKind fileType) => fileType switch
    {
        ProjectFileKind.Csv => "CSV",
        ProjectFileKind.ScriptableObject => "ScriptableObject",
        _ => "Task",
    };

    private void RefreshExplorerView()
    {
        var requestVersion = ++_searchRequestVersion;
        var candidates = _indexedProjectFiles.Where(file =>
                _selectedExplorerMods.Contains(file.ModName) && _selectedExplorerFileTypes.Contains(file.Kind))
            .ToArray();
        var query = ExplorerSearchText.Trim();
        if (query.Length == 0)
        {
            ReplaceExplorerFiles(candidates);
            return;
        }

        var normalizedQuery = VectorSearchNameNormalizer.NormalizeQuery(query);
        var exact = candidates.Where(file =>
                VectorSearchNameNormalizer.NormalizeFileName(file.FileName).Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                file.FileName.Equals(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var exactPaths = exact.Select(file => file.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var lexical = candidates.Where(file => !exactPaths.Contains(file.FullPath) &&
                (file.FileName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                 VectorSearchNameNormalizer.NormalizeFileName(file.FileName).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var literalResults = exact.Concat(lexical).ToArray();
        ReplaceExplorerFiles(literalResults);
        if (_vectorSearch.IsReady)
            _ = AppendVectorResultsAsync(query, candidates, literalResults, requestVersion);
    }

    private async Task AppendVectorResultsAsync(
        string query,
        IReadOnlyList<IndexedProjectFile> candidates,
        IReadOnlyList<IndexedProjectFile> literalResults,
        int requestVersion)
    {
        try
        {
            var candidateNames = candidates.Select(file => VectorSearchNameNormalizer.NormalizeFileName(file.FileName))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var rankedNames = await _vectorSearch.RankNamesAsync(query, candidateNames);
            if (requestVersion != _searchRequestVersion || !ExplorerSearchText.Trim().Equals(query, StringComparison.Ordinal)) return;

            var literalPaths = literalResults.Select(file => file.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var ranks = rankedNames.Select((name, rank) => (name, rank))
                .ToDictionary(item => item.name, item => item.rank, StringComparer.OrdinalIgnoreCase);
            var vectorResults = candidates.Where(file => !literalPaths.Contains(file.FullPath) &&
                    ranks.ContainsKey(VectorSearchNameNormalizer.NormalizeFileName(file.FileName)))
                .OrderBy(file => ranks[VectorSearchNameNormalizer.NormalizeFileName(file.FileName)])
                .ThenBy(file => file.FileName, StringComparer.OrdinalIgnoreCase);
            ReplaceExplorerFiles(literalResults.Concat(vectorResults));
            ExplorerStatusText = $"{ExplorerFiles.Count} files in {ExplorerFilterText}. Literal matches are listed before vector matches.";
        }
        catch (Exception exception)
        {
            VectorSearchStatusText = "Vector query failed: " + exception.Message;
        }
    }

    private void ReplaceExplorerFiles(IEnumerable<IndexedProjectFile> files)
    {
        ExplorerFiles.Clear();
        foreach (var file in files) ExplorerFiles.Add(file);
        SynchronizeExplorerSelection();
    }

    private void SynchronizeExplorerSelection()
    {
        _synchronizingExplorerSelection = true;
        try
        {
            SelectedExplorerFile = ResolveCurrentExplorerFile(ExplorerFiles, CurrentDocument?.Document);
        }
        finally
        {
            _synchronizingExplorerSelection = false;
        }
    }

    internal static IndexedProjectFile? ResolveCurrentExplorerFile(
        IEnumerable<IndexedProjectFile> files,
        EditorDocument? currentDocument)
    {
        if (currentDocument is null or DesignPlanDocument || string.IsNullOrWhiteSpace(currentDocument.FilePath))
            return null;

        var currentPath = Path.GetFullPath(currentDocument.FilePath);
        return files.FirstOrDefault(file =>
            Path.GetFullPath(file.FullPath).Equals(currentPath, StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshDesignPlanView()
    {
        var requestVersion = ++_designPlanSearchRequestVersion;
        var query = DesignPlanSearchText.Trim();
        if (query.Length == 0)
        {
            DesignPlanSearchResults.Clear();
            ReplaceDesignPlanFiles(_indexedDesignPlanDocuments);
            DesignPlanStatusText = _indexedDesignPlanDocuments.Count == 0
                ? "No design plan documents found."
                : $"{_indexedDesignPlanDocuments.Count} design plans.";
            return;
        }

        var literalResults = DesignPlanSearchService.FindLiteralMatches(_indexedDesignPlanDocuments, query);
        ReplaceDesignPlanSearchResults(literalResults);
        DesignPlanStatusText = $"{literalResults.Count} literal design plan matches.";
        if (_vectorSearch.IsReady)
            _ = AppendDesignPlanVectorResultsAsync(query, literalResults, requestVersion);
    }

    private async Task AppendDesignPlanVectorResultsAsync(
        string query,
        IReadOnlyList<IndexedDesignPlanDocument> literalResults,
        int requestVersion)
    {
        try
        {
            var candidateNames = _indexedDesignPlanDocuments
                .Select(DesignPlanSearchService.GetVectorName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var rankedNames = await _vectorSearch.RankNamesAsync(query, candidateNames);
            if (requestVersion != _designPlanSearchRequestVersion ||
                !DesignPlanSearchText.Trim().Equals(query, StringComparison.Ordinal)) return;

            var searchResults = DesignPlanSearchService.MergeVectorMatches(
                _indexedDesignPlanDocuments,
                literalResults,
                rankedNames);
            ReplaceDesignPlanSearchResults(searchResults);
            DesignPlanStatusText = $"{DesignPlanSearchResults.Count} design plan matches.";
        }
        catch (Exception exception)
        {
            VectorSearchStatusText = "Vector query failed: " + exception.Message;
        }
    }

    private void ReplaceDesignPlanFiles(IEnumerable<IndexedDesignPlanDocument> documents)
    {
        SelectedDesignPlanFile = null;
        DesignPlanDates.Clear();
        foreach (var date in documents.GroupBy(document => document.Date)
                     .OrderByDescending(group => group.Key, StringComparer.Ordinal))
        {
            DesignPlanDates.Add(new DesignPlanDateViewModel(new IndexedDesignPlanDate(
                date.Key,
                DesignPlanIndexService.OrderDocuments(date))));
        }
    }

    private void ReplaceDesignPlanSearchResults(IEnumerable<IndexedDesignPlanDocument> documents)
    {
        SelectedDesignPlanFile = null;
        DesignPlanSearchResults.Clear();
        foreach (var document in documents)
            DesignPlanSearchResults.Add(new DesignPlanFileViewModel(document));
    }

    private void OnVectorSearchStatusChanged(object? sender, VectorSearchStatusChangedEventArgs args)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            _ = dispatcher.InvokeAsync(() => ApplyVectorSearchStatus(args));
            return;
        }
        ApplyVectorSearchStatus(args);
    }

    private void ApplyVectorSearchStatus(VectorSearchStatusChangedEventArgs args)
    {
        VectorSearchStatusText = args.Status;
        if (args.Ready && !string.IsNullOrWhiteSpace(ExplorerSearchText)) RefreshExplorerView();
        if (args.Ready && !string.IsNullOrWhiteSpace(DesignPlanSearchText)) RefreshDesignPlanView();
    }

    private void OpenSelectedExplorerFile()
    {
        if (SelectedExplorerFile is null) return;
        if (!File.Exists(SelectedExplorerFile.FullPath))
        {
            SetStatus($"The indexed file no longer exists: {SelectedExplorerFile.FullPath}", true);
            _ = RefreshExplorerAsync();
            return;
        }
        OpenDocument(SelectedExplorerFile.FullPath);
    }

    private void OpenSelectedDesignPlan()
    {
        if (SelectedDesignPlanFile is not null) OpenDesignPlan(SelectedDesignPlanFile.Document, false);
    }

    private void ShowDiagnostics(string title, IEnumerable<Diagnostic> diagnostics)
    {
        var diagnosticList = diagnostics.ToArray();
        var message = JoinDiagnostics(diagnosticList);
        RecordDiagnostics(diagnosticList);
        _dialogs.Show(title, message, true);
    }

    private static string ResolveSettingsFilePath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BbxEditor.Net.sln")))
                return Path.Combine(directory.FullName, "settings.json");
        }
        return Path.Combine(AppContext.BaseDirectory, "settings.json");
    }

    private string ResolveConfiguredPath(string configuredPath)
        => PortablePath.Resolve(_settingsDirectory, configuredPath);

    private string MakeSettingsRelativePath(string path) =>
        PortablePath.MakeRelative(_settingsDirectory, path);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var state in _openDocumentWatches.Values) state.Dispose();
        _openDocumentWatches.Clear();
        foreach (var document in Documents) document.Dispose();
        _designPlanDirectoryWatch?.Dispose();
        _designPlanDirectoryWatch = null;
        _projectFileIndex.IndexChanged -= OnProjectFileIndexChanged;
        _projectFileIndex.Dispose();
        _vectorSearch.StatusChanged -= OnVectorSearchStatusChanged;
        Task.Run(async () => await _vectorSearch.DisposeAsync()).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    private static string JoinDiagnostics(IEnumerable<Diagnostic> diagnostics) => string.Join(Environment.NewLine, diagnostics.Select(item => $"[{item.Severity}] {item.Message}"));
}

public sealed class RecentFileMenuItemViewModel
{
    private RecentFileMenuItemViewModel(string displayName)
    {
        DisplayName = displayName;
        FullPath = string.Empty;
        OpenCommand = new RelayCommand(() => { }, () => false);
    }

    public RecentFileMenuItemViewModel(string path, Action<string> open)
    {
        FullPath = path;
        DisplayName = Path.GetFileName(path);
        OpenCommand = new RelayCommand(() => open(path));
    }

    public static RecentFileMenuItemViewModel Empty { get; } = new("No Recent Files");
    public string DisplayName { get; }
    public string FullPath { get; }
    public RelayCommand OpenCommand { get; }
}

public sealed class DesignPlanDateViewModel
{
    public DesignPlanDateViewModel(IndexedDesignPlanDate date)
    {
        Date = date.Date;
        Documents = new ObservableCollection<DesignPlanFileViewModel>(
            date.Documents.Select(document => new DesignPlanFileViewModel(document)));
    }

    public string Date { get; }
    public ObservableCollection<DesignPlanFileViewModel> Documents { get; }
}

public sealed class DesignPlanFileViewModel
{
    public DesignPlanFileViewModel(IndexedDesignPlanDocument document) => Document = document;

    public IndexedDesignPlanDocument Document { get; }
    public string DisplayName => Document.Title;
    public string FileName => Document.FileName;
    public string? Priority => Document.Priority;
    public string? PriorityColor => DesignPlanMetadataPresentation.GetPriorityColor(Priority);
    public bool HasPriority => PriorityColor is not null;
    public string? State => Document.State;
    public string? StateColor => DesignPlanMetadataPresentation.GetStateColor(State);
    public bool HasState => StateColor is not null;
    public string FullPath => Document.FullPath;
}

public sealed class ExplorerModFilterViewModel : ObservableObject
{
    private bool _isSelected;

    public ExplorerModFilterViewModel(string displayName, string? modName, Action<string?> select)
    {
        DisplayName = displayName;
        ModName = modName;
        SelectCommand = new RelayCommand(() => select(ModName));
    }

    public string DisplayName { get; }
    public string? ModName { get; }
    public RelayCommand SelectCommand { get; }
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!SetProperty(ref _isSelected, value)) return;
            RaisePropertyChanged(nameof(Header));
        }
    }
    public string Header => IsSelected ? "✓ " + DisplayName : DisplayName;
}

public sealed class ExplorerFileTypeFilterViewModel : ObservableObject
{
    private bool _isSelected;

    public ExplorerFileTypeFilterViewModel(string displayName, ProjectFileKind? fileType, Action<ProjectFileKind?> select)
    {
        DisplayName = displayName;
        FileType = fileType;
        SelectCommand = new RelayCommand(() => select(FileType));
    }

    public string DisplayName { get; }
    public ProjectFileKind? FileType { get; }
    public RelayCommand SelectCommand { get; }
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!SetProperty(ref _isSelected, value)) return;
            RaisePropertyChanged(nameof(Header));
        }
    }
    public string Header => IsSelected ? "✓ " + DisplayName : DisplayName;
}
