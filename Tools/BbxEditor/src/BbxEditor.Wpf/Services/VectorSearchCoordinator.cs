using BbxEditor.Infrastructure;
using System.IO;

namespace BbxEditor.Wpf.Services;

internal sealed class VectorSearchStatusChangedEventArgs(string status, bool ready) : EventArgs
{
    public string Status { get; } = status;
    public bool Ready { get; } = ready;
}

internal sealed class TransientVectorIndex(
    IReadOnlyDictionary<string, float[]> vectors,
    float[] center)
{
    public IReadOnlyDictionary<string, float[]> Vectors { get; } = vectors;
    public float[] Center { get; } = center;
}

internal sealed class VectorSearchCoordinator : IAsyncDisposable
{
    private readonly object _stateGate = new();
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    private readonly SemaphoreSlim _csvGate = new(1, 1);
    private readonly VectorIndexCacheStore _cacheStore;
    private readonly CsvVectorIndexCacheStore _csvCacheStore;
    private readonly string? _workerExecutablePath;
    private readonly CancellationTokenSource _lifetime = new();
    private EmbeddingWorkerClient? _worker;
    private VectorIndexCache _cache = new();
    private CsvVectorIndexCache _csvCache = new();
    private IReadOnlyList<string> _latestNames = [];
    private int _requestedVersion;
    private bool _syncRunning;
    private bool _enabled;
    private bool _ready;
    private string _status = "Vector search is disabled.";

    public VectorSearchCoordinator(string cacheFile, string? workerExecutablePath = null, string? csvCacheFile = null)
    {
        _cacheStore = new VectorIndexCacheStore(cacheFile);
        _csvCacheStore = new CsvVectorIndexCacheStore(csvCacheFile ?? Path.Combine(
            Path.GetDirectoryName(cacheFile) ?? ".", "csv-vector-index.tmp.json"));
        _workerExecutablePath = workerExecutablePath;
    }

    public event EventHandler<VectorSearchStatusChangedEventArgs>? StatusChanged;

    public bool IsReady
    {
        get { lock (_stateGate) return _ready; }
    }

    public string Status
    {
        get { lock (_stateGate) return _status; }
    }

    internal int? WorkerProcessId => _worker?.ProcessId;

    public async Task ApplyConfigurationAsync(bool enabled, string configuredModelDirectory, CancellationToken cancellationToken = default)
    {
        await _syncGate.WaitAsync(cancellationToken);
        try
        {
            await StopWorkerAsync();
            lock (_stateGate)
            {
                _enabled = false;
                _ready = false;
                _latestNames = [];
                _requestedVersion++;
            }

            if (!enabled)
            {
                SetStatus("Vector search is disabled.", false);
                return;
            }

            var modelDirectory = EmbeddingModelLayout.ResolveModelDirectory(configuredModelDirectory);
            if (modelDirectory is null)
            {
                SetStatus($"Model not found. Select a directory containing {EmbeddingModelLayout.ModelFolderName}.", false);
                return;
            }

            SetStatus("Loading the vector model…", false);
            try
            {
                _worker = await EmbeddingWorkerClient.StartAsync(modelDirectory, cancellationToken, _workerExecutablePath);
                var fingerprint = EmbeddingModelLayout.CreateFingerprint(modelDirectory);
                _cache = _cacheStore.Load();
                if (_cache.SchemaVersion != 1 || !_cache.ModelFingerprint.Equals(fingerprint, StringComparison.Ordinal))
                    _cache = new VectorIndexCache { ModelFingerprint = fingerprint };
                else
                    _cache.ModelFingerprint = fingerprint;
                _csvCache = _csvCacheStore.Load();
                if (_csvCache.SchemaVersion != 1 || !_csvCache.ModelFingerprint.Equals(fingerprint, StringComparison.Ordinal))
                    _csvCache = new CsvVectorIndexCache { ModelFingerprint = fingerprint };
                else
                    _csvCache.ModelFingerprint = fingerprint;
                lock (_stateGate) _enabled = true;
                SetStatus("Vector model loaded; waiting for the Explorer file index.", false);
            }
            catch (Exception exception)
            {
                await StopWorkerAsync();
                SetStatus("Vector model failed to load: " + exception.Message, false);
            }
        }
        finally
        {
            _syncGate.Release();
        }
    }

    public void SynchronizeFiles(IEnumerable<IndexedProjectFile> files)
    {
        var names = files.Select(file => VectorSearchNameNormalizer.NormalizeFileName(file.FileName))
            .ToArray();
        SynchronizeNames(names);
    }

    public void SynchronizeNames(IEnumerable<string> sourceNames)
    {
        var names = sourceNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        lock (_stateGate)
        {
            _latestNames = names;
            _requestedVersion++;
            _ready = false;
            if (!_enabled || _worker is null) return;
            if (_syncRunning) return;
            _syncRunning = true;
        }
        _ = RunSynchronizationLoopAsync();
    }

    public async Task<IReadOnlyList<string>> RankNamesAsync(string query, IReadOnlyCollection<string> candidateNames, CancellationToken cancellationToken = default)
    {
        EmbeddingWorkerClient? worker;
        Dictionary<string, float[]> vectors;
        float[][] corpusVectors;
        lock (_stateGate)
        {
            if (!_ready || _worker is null) return [];
            worker = _worker;
            corpusVectors = _cache.Vectors.Values.ToArray();
            vectors = _cache.Vectors
                .Where(item => candidateNames.Contains(item.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        }
        if (vectors.Count == 0) return [];

        var normalizedQuery = VectorSearchNameNormalizer.NormalizeQuery(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery)) return [];
        var queryVector = (await worker.EmbedAsync([normalizedQuery], cancellationToken))[0];
        var center = CalculateCenter(corpusVectors);
        var centeredQuery = CenterAndNormalize(queryVector, center);
        return vectors
            .Select(item => new { item.Key, Score = Dot(centeredQuery, CenterAndNormalize(item.Value, center)) })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Key)
            .ToArray();
    }

    public async Task<IReadOnlyList<string>> RankCsvColumnValuesAsync(
        string columnKey,
        string query,
        IReadOnlyCollection<string> candidateValues,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(columnKey)) return [];
        EmbeddingWorkerClient? worker;
        lock (_stateGate)
        {
            if (!_ready || _worker is null) return [];
            worker = _worker;
        }

        var values = candidateValues
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var normalizedQuery = VectorSearchNameNormalizer.NormalizeQuery(query);
        if (values.Length == 0 || string.IsNullOrWhiteSpace(normalizedQuery)) return [];

        await _csvGate.WaitAsync(cancellationToken);
        try
        {
            if (!_csvCache.Columns.TryGetValue(columnKey, out var columnCache))
            {
                columnCache = new VectorIndexCache { ModelFingerprint = _csvCache.ModelFingerprint };
                _csvCache.Columns[columnKey] = columnCache;
            }

            var active = values.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var stale in columnCache.Vectors.Keys.Where(key => !active.Contains(key)).ToArray())
                columnCache.Vectors.Remove(stale);

            var missing = values.Where(value => !columnCache.Vectors.ContainsKey(value)).ToArray();
            for (var start = 0; start < missing.Length; start += 16)
            {
                var batch = missing.Skip(start).Take(16).ToArray();
                var embedded = await worker.EmbedAsync(batch, cancellationToken);
                for (var index = 0; index < batch.Length; index++) columnCache.Vectors[batch[index]] = embedded[index];
                columnCache.Dimension = embedded.FirstOrDefault()?.Length ?? columnCache.Dimension;
                _csvCacheStore.Save(_csvCache);
            }
            _csvCacheStore.Save(_csvCache);

            var queryVector = (await worker.EmbedAsync([normalizedQuery], cancellationToken))[0];
            var center = CalculateCenter(columnCache.Vectors.Values);
            var centeredQuery = CenterAndNormalize(queryVector, center);
            return columnCache.Vectors
                .Select(item => new { item.Key, Score = Dot(centeredQuery, CenterAndNormalize(item.Value, center)) })
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.Key)
                .ToArray();
        }
        finally
        {
            _csvGate.Release();
        }
    }

    public async Task<TransientVectorIndex?> BuildTransientIndexAsync(
        IReadOnlyCollection<string> candidateTexts,
        CancellationToken cancellationToken = default)
    {
        EmbeddingWorkerClient? worker;
        lock (_stateGate)
        {
            if (!_enabled || _worker is null) return null;
            worker = _worker;
        }

        var texts = candidateTexts
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(text => text, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (texts.Length == 0) return new TransientVectorIndex(
            new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase), []);

        var rawVectors = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
        for (var start = 0; start < texts.Length; start += 16)
        {
            var batch = texts.Skip(start).Take(16).ToArray();
            var vectors = await worker.EmbedAsync(batch, cancellationToken);
            for (var index = 0; index < batch.Length; index++) rawVectors[batch[index]] = vectors[index];
        }

        var center = CalculateCenter(rawVectors.Values);
        var centeredVectors = rawVectors.ToDictionary(
            item => item.Key,
            item => CenterAndNormalize(item.Value, center),
            StringComparer.OrdinalIgnoreCase);
        return new TransientVectorIndex(centeredVectors, center);
    }

    public async Task<IReadOnlyList<string>> RankTransientIndexAsync(
        string query,
        TransientVectorIndex index,
        CancellationToken cancellationToken = default)
    {
        if (index.Vectors.Count == 0) return [];
        EmbeddingWorkerClient? worker;
        lock (_stateGate)
        {
            if (!_enabled || _worker is null) return [];
            worker = _worker;
        }

        var normalizedQuery = VectorSearchNameNormalizer.NormalizeQuery(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery)) return [];
        var queryVector = (await worker.EmbedAsync([normalizedQuery], cancellationToken))[0];
        var centeredQuery = CenterAndNormalize(queryVector, index.Center);
        return index.Vectors
            .Select(item => new { item.Key, Score = Dot(centeredQuery, item.Value) })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Key)
            .ToArray();
    }

    private async Task RunSynchronizationLoopAsync()
    {
        var handledVersion = -1;
        await _syncGate.WaitAsync(_lifetime.Token);
        try
        {
            while (!_lifetime.IsCancellationRequested)
            {
                IReadOnlyList<string> names;
                int version;
                EmbeddingWorkerClient? worker;
                lock (_stateGate)
                {
                    names = _latestNames;
                    version = _requestedVersion;
                    handledVersion = version;
                    worker = _worker;
                    if (!_enabled || worker is null) return;
                }

                var active = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var stale in _cache.Vectors.Keys.Where(key => !active.Contains(key)).ToArray()) _cache.Vectors.Remove(stale);
                var missing = names.Where(name => !_cache.Vectors.ContainsKey(name)).ToArray();
                SetStatus(missing.Length == 0 ? "Finalizing the vector index…" : $"Embedding file names: 0/{missing.Length}", false);
                for (var start = 0; start < missing.Length; start += 16)
                {
                    var batch = missing.Skip(start).Take(16).ToArray();
                    var embedded = await worker.EmbedAsync(batch, _lifetime.Token);
                    for (var index = 0; index < batch.Length; index++) _cache.Vectors[batch[index]] = embedded[index];
                    _cache.Dimension = embedded.FirstOrDefault()?.Length ?? _cache.Dimension;
                    _cacheStore.Save(_cache);
                    SetStatus($"Embedding file names: {Math.Min(start + batch.Length, missing.Length)}/{missing.Length}", false);
                }
                _cacheStore.Save(_cache);

                lock (_stateGate)
                {
                    if (version != _requestedVersion) continue;
                    _ready = true;
                }
                SetStatus($"Vector index ready ({names.Count} names).", true);
                return;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            lock (_stateGate) _ready = false;
            SetStatus("Vector indexing failed: " + exception.Message, false);
        }
        finally
        {
            var restart = false;
            lock (_stateGate)
            {
                _syncRunning = false;
                if (!_lifetime.IsCancellationRequested && _enabled && _worker is not null && _requestedVersion != handledVersion)
                {
                    _syncRunning = true;
                    restart = true;
                }
            }
            _syncGate.Release();
            if (restart) _ = RunSynchronizationLoopAsync();
        }
    }

    private void SetStatus(string status, bool ready)
    {
        lock (_stateGate)
        {
            _status = status;
            _ready = ready;
        }
        StatusChanged?.Invoke(this, new VectorSearchStatusChangedEventArgs(status, ready));
    }

    private async Task StopWorkerAsync()
    {
        var worker = Interlocked.Exchange(ref _worker, null);
        if (worker is not null) await worker.DisposeAsync().ConfigureAwait(false);
    }

    private static float[] CalculateCenter(IEnumerable<float[]> vectors)
    {
        var rows = vectors.ToArray();
        if (rows.Length == 0) return [];
        var center = new float[rows[0].Length];
        foreach (var row in rows)
            for (var index = 0; index < center.Length; index++) center[index] += row[index];
        for (var index = 0; index < center.Length; index++) center[index] /= rows.Length;
        return center;
    }

    private static float[] CenterAndNormalize(float[] vector, float[] center)
    {
        var result = new float[vector.Length];
        double norm = 0;
        for (var index = 0; index < result.Length; index++)
        {
            result[index] = vector[index] - center[index];
            norm += result[index] * result[index];
        }
        norm = Math.Sqrt(norm);
        if (norm > 1e-12)
            for (var index = 0; index < result.Length; index++) result[index] = (float)(result[index] / norm);
        return result;
    }

    private static double Dot(float[] left, float[] right)
    {
        double result = 0;
        for (var index = 0; index < left.Length; index++) result += left[index] * right[index];
        return result;
    }

    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        await _syncGate.WaitAsync().ConfigureAwait(false);
        _syncGate.Release();
        await StopWorkerAsync().ConfigureAwait(false);
        _lifetime.Dispose();
        _syncGate.Dispose();
        _csvGate.Dispose();
    }
}
