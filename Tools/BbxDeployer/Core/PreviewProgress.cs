namespace BbxDeployer.Core;

public sealed class PreviewProgress
{
    public string Message { get; init; } = string.Empty;

    public bool IsIndeterminate { get; init; }

    public int CompletedFiles { get; init; }

    public int TotalFiles { get; init; }

    public double? ExplicitPercentage { get; init; }

    public double Percentage => ExplicitPercentage is { } explicitPercentage
        ? Math.Clamp(explicitPercentage, 0, 100)
        : IsIndeterminate
            ? 0
            : TotalFiles == 0
            ? 100
            : Math.Min(100, CompletedFiles * 100d / TotalFiles);
}
