namespace BbxDeployer.Core;

public sealed class IgnoreRule
{
    public IgnoreRuleKind Kind { get; init; }

    public string Pattern { get; init; } = string.Empty;

    public string BaseDirectory { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public bool IsNegation { get; init; }

    public bool DirectoryOnly { get; init; }

    public int Order { get; init; }
}
