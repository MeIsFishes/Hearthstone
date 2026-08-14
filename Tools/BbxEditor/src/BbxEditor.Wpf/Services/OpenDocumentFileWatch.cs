using System.IO;
using System.Security.Cryptography;

namespace BbxEditor.Wpf.Services;

internal sealed class OpenDocumentFileWatch : IDisposable
{
    private readonly object _gate = new();
    private readonly string _filePath;
    private readonly Action<string> _changed;
    private readonly FileSystemWatcher _watcher;
    private CancellationTokenSource? _debounce;
    private bool _disposed;

    public OpenDocumentFileWatch(string filePath, Action<string> changed)
    {
        _filePath = Path.GetFullPath(filePath);
        _changed = changed;
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(_filePath) ?? ".", Path.GetFileName(_filePath))
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
        };
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;
        _watcher.EnableRaisingEvents = true;
    }

    public static string? ReadFingerprint(string filePath)
    {
        if (!File.Exists(filePath)) return "<missing>";
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 64 * 1024, FileOptions.SequentialScan);
            var hash = SHA256.HashData(stream);
            return $"{stream.Length}:{Convert.ToHexString(hash)}";
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs args) => ScheduleNotification();
    private void OnRenamed(object sender, RenamedEventArgs args) => ScheduleNotification();
    private void OnError(object sender, ErrorEventArgs args) => ScheduleNotification();

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
            await Task.Delay(350, cancellationToken).ConfigureAwait(false);
            _changed(_filePath);
        }
        catch (OperationCanceledException)
        {
        }
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

internal sealed class OpenDocumentWatchState(string filePath, string? fingerprint, OpenDocumentFileWatch registration) : IDisposable
{
    public string FilePath { get; } = filePath;
    public string? Fingerprint { get; set; } = fingerprint;
    public OpenDocumentFileWatch Registration { get; } = registration;
    public void Dispose() => Registration.Dispose();
}
