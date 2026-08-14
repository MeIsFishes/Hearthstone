namespace BbxEditor.Infrastructure;

public enum ProjectFileKind
{
    Task,
    Csv,
    ScriptableObject,
}

public enum TaskFileEditorKind
{
    Timeline,
    BehaviorTree,
}

public readonly record struct ProjectFileClassification(ProjectFileKind Kind, TaskFileEditorKind? TaskEditorKind = null);

public sealed record IndexedProjectFile(
    string FullPath,
    string RelativePath,
    ProjectFileKind Kind,
    string ModName,
    TaskFileEditorKind? TaskEditorKind = null)
{
    public string FileName => Path.GetFileName(FullPath);
    public string DisplayName => FileName.EndsWith(".editor.json", StringComparison.OrdinalIgnoreCase)
        ? FileName[..^".editor.json".Length]
        : Path.GetFileNameWithoutExtension(FileName);
    public string FileTypeLabel => Kind switch
    {
        ProjectFileKind.Csv => "CSV",
        ProjectFileKind.ScriptableObject => "ScriptableObject",
        _ => TaskEditorKind switch
        {
            TaskFileEditorKind.Timeline => "Task · Timeline",
            TaskFileEditorKind.BehaviorTree => "Task · Behavior Tree",
            _ => "Task",
        },
    };
}

public sealed class ProjectFileIndexChangedEventArgs(IReadOnlyList<IndexedProjectFile> files, string? error = null) : EventArgs
{
    public IReadOnlyList<IndexedProjectFile> Files { get; } = files;
    public string? Error { get; } = error;
}

public sealed class ProjectFileIndexService : IDisposable
{
    private static readonly string[] SupportedSearchPatterns = ["*.editor.json", "*.csv", "*.asset"];
    private readonly object _gate = new();
    private readonly SemaphoreSlim _scanGate = new(1, 1);
    private readonly List<FileSystemWatcher> _contentWatchers = [];
    private FileSystemWatcher? _directoryWatcher;
    private CancellationTokenSource? _lifetime;
    private CancellationTokenSource? _debounce;
    private string _projectRoot = string.Empty;
    private string[] _searchRoots = [];
    private Func<string, ProjectFileClassification?>? _classifier;
    private int _generation;
    private bool _disposed;

    public event EventHandler<ProjectFileIndexChangedEventArgs>? IndexChanged;

    public async Task StartAsync(
        string projectRoot,
        IEnumerable<string> searchDirectories,
        Func<string, ProjectFileClassification?> classifier,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var normalizedProjectRoot = Path.GetFullPath(projectRoot);
        if (!Directory.Exists(normalizedProjectRoot))
            throw new DirectoryNotFoundException($"The game project directory does not exist: {normalizedProjectRoot}");

        var normalizedSearchRoots = NormalizeSearchRoots(normalizedProjectRoot, searchDirectories);
        CancellationTokenSource lifetime;
        int generation;
        lock (_gate)
        {
            StopCore();
            _projectRoot = normalizedProjectRoot;
            _searchRoots = normalizedSearchRoots;
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            lifetime = _lifetime;
            generation = ++_generation;
            ConfigureWatchers();
        }

        await RebuildAsync(generation, lifetime.Token).ConfigureAwait(false);
    }

    public Task RefreshAsync()
    {
        lock (_gate)
        {
            if (_lifetime is null) return Task.CompletedTask;
            return RebuildAsync(_generation, _lifetime.Token);
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            StopCore();
            _generation++;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task RebuildAsync(int generation, CancellationToken cancellationToken)
    {
        try
        {
            await _scanGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var projectRoot = _projectRoot;
                var searchRoots = _searchRoots;
                var classifier = _classifier;
                if (classifier is null) return;
                var files = await Task.Run(() => BuildIndex(projectRoot, searchRoots, classifier, cancellationToken), cancellationToken).ConfigureAwait(false);
                lock (_gate)
                {
                    if (generation != _generation || cancellationToken.IsCancellationRequested) return;
                }
                IndexChanged?.Invoke(this, new ProjectFileIndexChangedEventArgs(files));
            }
            finally
            {
                _scanGate.Release();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            lock (_gate)
            {
                if (generation != _generation) return;
            }
            IndexChanged?.Invoke(this, new ProjectFileIndexChangedEventArgs([], exception.Message));
        }
    }

    private static IReadOnlyList<IndexedProjectFile> BuildIndex(
        string projectRoot,
        IEnumerable<string> searchRoots,
        Func<string, ProjectFileClassification?> classifier,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, IndexedProjectFile>(StringComparer.OrdinalIgnoreCase);
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };
        foreach (var searchRoot in searchRoots.Where(Directory.Exists))
        {
            foreach (var searchPattern in SupportedSearchPatterns)
            {
                foreach (var path in Directory.EnumerateFiles(searchRoot, searchPattern, options))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ProjectFileClassification? classification;
                    try
                    {
                        classification = classifier(path);
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }
                    if (classification is null) continue;
                    var relativePath = Path.GetRelativePath(projectRoot, path).Replace('\\', '/');
                    result[path] = new IndexedProjectFile(path, relativePath, classification.Value.Kind, ResolveModName(relativePath),
                        classification.Value.TaskEditorKind);
                }
            }
        }
        return result.Values
            .OrderBy(file => file.ModName.Equals("Native", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(file => file.ModName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(file => file.Kind)
            .ThenBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] NormalizeSearchRoots(string projectRoot, IEnumerable<string> searchDirectories)
    {
        var result = new List<string>();
        foreach (var configuredPath in searchDirectories.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var fullPath = Path.GetFullPath(Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(projectRoot, configuredPath));
            if (!IsWithin(fullPath, projectRoot)) continue;
            if (!result.Contains(fullPath, StringComparer.OrdinalIgnoreCase)) result.Add(fullPath);
        }
        return result.ToArray();
    }

    private void ConfigureWatchers()
    {
        DisposeWatchers();
        foreach (var searchRoot in _searchRoots.Where(Directory.Exists))
        {
            var watcher = CreateWatcher(searchRoot, NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size);
            watcher.Changed += OnContentChanged;
            watcher.Created += OnContentChanged;
            watcher.Deleted += OnContentChanged;
            watcher.Renamed += OnContentRenamed;
            watcher.Error += OnWatcherError;
            watcher.EnableRaisingEvents = true;
            _contentWatchers.Add(watcher);
        }

        _directoryWatcher = CreateWatcher(_projectRoot, NotifyFilters.DirectoryName);
        _directoryWatcher.Created += OnDirectoryChanged;
        _directoryWatcher.Deleted += OnDirectoryChanged;
        _directoryWatcher.Renamed += OnDirectoryRenamed;
        _directoryWatcher.Error += OnWatcherError;
        _directoryWatcher.EnableRaisingEvents = true;
    }

    private static FileSystemWatcher CreateWatcher(string path, NotifyFilters notifyFilter) => new(path)
    {
        IncludeSubdirectories = true,
        NotifyFilter = notifyFilter,
        InternalBufferSize = 64 * 1024,
    };

    private void OnContentChanged(object sender, FileSystemEventArgs args)
    {
        if (IsSupportedPath(args.FullPath) || Directory.Exists(args.FullPath)) ScheduleRefresh();
    }

    private void OnContentRenamed(object sender, RenamedEventArgs args)
    {
        if (IsSupportedPath(args.FullPath) || IsSupportedPath(args.OldFullPath) || Directory.Exists(args.FullPath)) ScheduleRefresh();
    }

    private void OnDirectoryChanged(object sender, FileSystemEventArgs args)
    {
        if (!IsRelevantDirectory(args.FullPath)) return;
        lock (_gate)
        {
            if (_lifetime is null) return;
            ConfigureWatchers();
        }
        ScheduleRefresh();
    }

    private void OnDirectoryRenamed(object sender, RenamedEventArgs args)
    {
        if (IsRelevantDirectory(args.FullPath) || IsRelevantDirectory(args.OldFullPath)) OnDirectoryChanged(sender, args);
    }

    private void OnWatcherError(object sender, ErrorEventArgs args) => ScheduleRefresh();

    private void ScheduleRefresh()
    {
        CancellationTokenSource debounce;
        int generation;
        lock (_gate)
        {
            if (_lifetime is null) return;
            _debounce?.Cancel();
            _debounce?.Dispose();
            _debounce = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            debounce = _debounce;
            generation = _generation;
        }
        _ = DebouncedRefreshAsync(generation, debounce.Token);
    }

    private async Task DebouncedRefreshAsync(int generation, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            await RebuildAsync(generation, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private bool IsRelevantDirectory(string path) => _searchRoots.Any(root => IsWithin(path, root) || IsWithin(root, path));

    private static bool IsSupportedPath(string path) =>
        path.EndsWith(".editor.json", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase);

    private static string ResolveModName(string relativePath)
    {
        var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var modsIndex = Array.FindIndex(parts, part => part.Equals("Mods", StringComparison.OrdinalIgnoreCase));
        if (modsIndex >= 0 && modsIndex + 1 < parts.Length)
            return parts[modsIndex + 1].Equals("Native", StringComparison.OrdinalIgnoreCase) ? "Native" : parts[modsIndex + 1];
        return "Native";
    }

    private static bool IsWithin(string path, string root)
    {
        var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath.Equals(fullRoot, StringComparison.OrdinalIgnoreCase) ||
               fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private void StopCore()
    {
        _debounce?.Cancel();
        _debounce?.Dispose();
        _debounce = null;
        _lifetime?.Cancel();
        _lifetime?.Dispose();
        _lifetime = null;
        DisposeWatchers();
    }

    private void DisposeWatchers()
    {
        foreach (var watcher in _contentWatchers) watcher.Dispose();
        _contentWatchers.Clear();
        _directoryWatcher?.Dispose();
        _directoryWatcher = null;
    }
}
