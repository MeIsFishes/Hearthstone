namespace BbxEditor.Wpf.Services;

internal sealed class EmbeddingRequest
{
    public string Id { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public List<string> Texts { get; set; } = [];
}

internal sealed class EmbeddingResponse
{
    public string Id { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public List<float[]> Vectors { get; set; } = [];
}
