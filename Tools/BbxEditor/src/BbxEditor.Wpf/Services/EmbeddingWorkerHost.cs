using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Tokenizers.DotNet;
using BbxEditor.Infrastructure;

namespace BbxEditor.Wpf.Services;

internal static class EmbeddingWorkerHost
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<int> RunAsync(string[] args)
    {
        var pipeName = ReadArgument(args, "--pipe");
        var modelDirectory = ReadArgument(args, "--model-directory");
        if (!int.TryParse(ReadArgument(args, "--parent-pid"), out var parentPid) ||
            string.IsNullOrWhiteSpace(pipeName) || string.IsNullOrWhiteSpace(modelDirectory))
            return 2;

        using var lifetime = new CancellationTokenSource();
        _ = MonitorParentAsync(parentPid, lifetime);
        try
        {
            await using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(TimeSpan.FromSeconds(20), lifetime.Token);
            using var reader = new StreamReader(pipe);
            await using var writer = new StreamWriter(pipe) { AutoFlush = true };

            MpnetEmbedder? embedder = null;
            try
            {
                embedder = new MpnetEmbedder(modelDirectory);
                await WriteResponseAsync(writer, new EmbeddingResponse { Id = "startup", Success = true });
            }
            catch (Exception exception)
            {
                await WriteResponseAsync(writer, new EmbeddingResponse { Id = "startup", Error = exception.Message });
                return 3;
            }

            using (embedder)
            {
                while (!lifetime.IsCancellationRequested && pipe.IsConnected)
                {
                    var line = await reader.ReadLineAsync(lifetime.Token);
                    if (line is null) break;
                    EmbeddingRequest? request = null;
                    try
                    {
                        request = JsonSerializer.Deserialize<EmbeddingRequest>(line, JsonOptions);
                        if (request is null) throw new InvalidDataException("The embedding request was empty.");
                        if (request.Command.Equals("shutdown", StringComparison.OrdinalIgnoreCase))
                        {
                            await WriteResponseAsync(writer, new EmbeddingResponse { Id = request.Id, Success = true });
                            break;
                        }

                        if (!request.Command.Equals("embed", StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException($"Unknown worker command: {request.Command}");
                        var vectors = embedder.Encode(request.Texts);
                        await WriteResponseAsync(writer, new EmbeddingResponse { Id = request.Id, Success = true, Vectors = vectors });
                    }
                    catch (Exception exception)
                    {
                        await WriteResponseAsync(writer, new EmbeddingResponse
                        {
                            Id = request?.Id ?? string.Empty,
                            Error = exception.Message,
                        });
                    }
                }
            }
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (IOException)
        {
            return 0;
        }
        finally
        {
            lifetime.Cancel();
        }
    }

    private static async Task MonitorParentAsync(int parentPid, CancellationTokenSource lifetime)
    {
        try
        {
            using var parent = Process.GetProcessById(parentPid);
            await parent.WaitForExitAsync(lifetime.Token);
            lifetime.Cancel();
        }
        catch (ArgumentException)
        {
            lifetime.Cancel();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static string ReadArgument(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index + 1 < args.Count; index++)
            if (args[index].Equals(name, StringComparison.OrdinalIgnoreCase)) return args[index + 1];
        return string.Empty;
    }

    private static Task WriteResponseAsync(StreamWriter writer, EmbeddingResponse response) =>
        writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions));

    private sealed class MpnetEmbedder : IDisposable
    {
        private const int MaximumTokens = 128;
        private const int PaddingTokenId = 1;
        private readonly Tokenizer _tokenizer;
        private readonly InferenceSession _session;

        public MpnetEmbedder(string modelDirectory)
        {
            _tokenizer = new Tokenizer(Path.Combine(modelDirectory, EmbeddingModelLayout.TokenizerFileName));
            _session = new InferenceSession(Path.Combine(modelDirectory, EmbeddingModelLayout.ModelFileName));
        }

        public List<float[]> Encode(IReadOnlyList<string> texts)
        {
            var result = new List<float[]>(texts.Count);
            for (var start = 0; start < texts.Count; start += 16)
                result.AddRange(EncodeBatch(texts.Skip(start).Take(16).ToArray()));
            return result;
        }

        private IReadOnlyList<float[]> EncodeBatch(IReadOnlyList<string> texts)
        {
            if (texts.Count == 0) return [];
            var tokenRows = texts.Select(text => Truncate(_tokenizer.Encode(text).Select(id => (long)id).ToArray())).ToArray();
            var sequenceLength = tokenRows.Max(row => row.Length);
            var inputIds = new DenseTensor<long>(new[] { texts.Count, sequenceLength });
            var attentionMask = new DenseTensor<long>(new[] { texts.Count, sequenceLength });
            for (var row = 0; row < tokenRows.Length; row++)
            {
                for (var column = 0; column < sequenceLength; column++)
                {
                    inputIds[row, column] = column < tokenRows[row].Length ? tokenRows[row][column] : PaddingTokenId;
                    attentionMask[row, column] = column < tokenRows[row].Length ? 1 : 0;
                }
            }

            using var outputs = _session.Run(
            [
                NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
            ]);
            var hidden = outputs.First(value => value.Name == "last_hidden_state").AsTensor<float>();
            var dimension = hidden.Dimensions[2];
            var vectors = new List<float[]>(texts.Count);
            for (var row = 0; row < texts.Count; row++)
            {
                var vector = new float[dimension];
                var tokens = tokenRows[row].Length;
                for (var column = 0; column < tokens; column++)
                    for (var index = 0; index < dimension; index++) vector[index] += hidden[row, column, index];
                for (var index = 0; index < dimension; index++) vector[index] /= Math.Max(tokens, 1);
                Normalize(vector);
                vectors.Add(vector);
            }
            return vectors;
        }

        private static long[] Truncate(long[] tokens)
        {
            if (tokens.Length <= MaximumTokens) return tokens;
            var result = tokens[..MaximumTokens];
            result[^1] = tokens[^1];
            return result;
        }

        private static void Normalize(float[] vector)
        {
            var norm = Math.Sqrt(vector.Sum(value => value * value));
            if (norm <= 1e-12) return;
            for (var index = 0; index < vector.Length; index++) vector[index] = (float)(vector[index] / norm);
        }

        public void Dispose()
        {
            _session.Dispose();
            _tokenizer.Dispose();
        }
    }
}
