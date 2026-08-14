using System.IO;
using BbxEditor.Infrastructure;

namespace BbxEditor.Wpf.Services;

internal sealed class DesignPlanDirectoryWatch : IDisposable
{
    private readonly object _gate = new();
    private readonly string _designPlanRoot;
    private readonly Action _changed;
    private readonly FileSystemWatcher _watcher;
    private CancellationTokenSource? _debounce;
    private bool _disposed;

    public DesignPlanDirectoryWatch(string gameProjectPath, Action changed)
    {
        var projectRoot = Path.GetFullPath(gameProjectPath);
        if (!Directory.Exists(projectRoot))
            throw new DirectoryNotFoundException($"The game project directory does not exist: {projectRoot}");
        _designPlanRoot = DesignPlanIndexService.ResolveDirectory(projectRoot);
        _changed = changed;
        _watcher = new FileSystemWatcher(projectRoot)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite |
                           NotifyFilters.Size | NotifyFilters.CreationTime,
            InternalBufferSize = 64 * 1024,
        };
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnChanged(object sender, FileSystemEventArgs args)
    {
        if (IsRelevant(args.FullPath)) ScheduleNotification();
    }

    private void OnRenamed(object sender, RenamedEventArgs args)
    {
        if (IsRelevant(args.FullPath) || IsRelevant(args.OldFullPath)) ScheduleNotification();
    }

    private void OnError(object sender, ErrorEventArgs args) => ScheduleNotification();

    private bool IsRelevant(string path) => IsWithin(path, _designPlanRoot) || IsWithin(_designPlanRoot, path);

    private void ScheduleNotification()
    {
        CancellationToken token;
        lock (_gate)
        {
            if (_disposed) return;
            _debounce?.Cancel();
            _debounce?.Dispose();
            _debounce = new CancellationTokenSource();
            token = _debounce.Token;
        }
        _ = NotifyAfterSettleAsync(token);
    }

    private async Task NotifyAfterSettleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            _changed();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static bool IsWithin(string path, string root)
    {
        var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath.Equals(fullRoot, StringComparison.OrdinalIgnoreCase) ||
               fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _debounce?.Cancel();
            _debounce?.Dispose();
            _debounce = null;
        }
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
    }
}
