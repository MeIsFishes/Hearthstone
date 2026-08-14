using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;

namespace BbxEditor.Wpf.Services;

internal sealed class EmbeddingWorkerClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _requestGate = new(1, 1);
    private readonly Process _process;
    private readonly NamedPipeServerStream _pipe;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private bool _disposed;

    public int ProcessId => _process.Id;

    private EmbeddingWorkerClient(Process process, NamedPipeServerStream pipe, StreamReader reader, StreamWriter writer)
    {
        _process = process;
        _pipe = pipe;
        _reader = reader;
        _writer = writer;
    }

    public static async Task<EmbeddingWorkerClient> StartAsync(
        string modelDirectory,
        CancellationToken cancellationToken,
        string? workerExecutablePath = null)
    {
        var pipeName = "BbxEditor.Embedding." + Guid.NewGuid().ToString("N");
        var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        Process? process = null;
        try
        {
            var executable = workerExecutablePath ?? Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
                throw new InvalidOperationException("BbxEditor could not locate its executable for the embedding worker.");
            var startInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory,
            };
            startInfo.ArgumentList.Add("--embedding-worker");
            startInfo.ArgumentList.Add("--parent-pid");
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
            startInfo.ArgumentList.Add("--pipe");
            startInfo.ArgumentList.Add(pipeName);
            startInfo.ArgumentList.Add("--model-directory");
            startInfo.ArgumentList.Add(modelDirectory);
            process = Process.Start(startInfo) ?? throw new InvalidOperationException("The embedding worker could not be started.");

            await pipe.WaitForConnectionAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            var reader = new StreamReader(pipe);
            var writer = new StreamWriter(pipe) { AutoFlush = true };
            var startupLine = await reader.ReadLineAsync(cancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(120), cancellationToken);
            var startup = startupLine is null ? null : JsonSerializer.Deserialize<EmbeddingResponse>(startupLine, JsonOptions);
            if (startup is not { Success: true })
                throw new InvalidOperationException(startup?.Error ?? "The embedding worker stopped during startup.");
            return new EmbeddingWorkerClient(process, pipe, reader, writer);
        }
        catch
        {
            pipe.Dispose();
            if (process is { HasExited: false }) process.Kill(true);
            process?.Dispose();
            throw;
        }
    }

    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (texts.Count == 0) return [];
        await _requestGate.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = new EmbeddingRequest
            {
                Id = Guid.NewGuid().ToString("N"),
                Command = "embed",
                Texts = [.. texts],
            };
            // Once a request is on the pipe, its response must be consumed even if the caller no longer
            // needs it. Otherwise the next request reads this response and permanently desynchronizes the
            // single-request protocol during rapid search input.
            await _writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions));
            var line = await _reader.ReadLineAsync();
            var response = line is null ? null : JsonSerializer.Deserialize<EmbeddingResponse>(line, JsonOptions);
            if (response is null) throw new IOException("The embedding worker disconnected.");
            if (!response.Id.Equals(request.Id, StringComparison.Ordinal)) throw new IOException("The embedding worker returned an unexpected response.");
            if (!response.Success) throw new InvalidOperationException(response.Error);
            if (response.Vectors.Count != texts.Count) throw new InvalidDataException("The embedding worker returned an incomplete batch.");
            cancellationToken.ThrowIfCancellationRequested();
            return response.Vectors;
        }
        finally
        {
            _requestGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (_pipe.IsConnected && !_process.HasExited)
            {
                var request = new EmbeddingRequest { Id = Guid.NewGuid().ToString("N"), Command = "shutdown" };
                await _writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions));
                await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));
            }
        }
        catch (Exception)
        {
            // Shutdown is best-effort; the process tree is forcibly reclaimed below.
        }
        finally
        {
            if (!_process.HasExited) _process.Kill(true);
            _writer.Dispose();
            _reader.Dispose();
            _pipe.Dispose();
            _process.Dispose();
            _requestGate.Dispose();
        }
    }
}
