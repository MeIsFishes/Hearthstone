namespace BbxDeployer.Core;

public sealed class SyncProgress
{
    public string TargetName { get; init; } = string.Empty;

    public string SyncItemName { get; init; } = string.Empty;

    public string CurrentFile { get; init; } = string.Empty;

    public int CompletedFiles { get; init; }

    public int TotalFiles { get; init; }

    public long CompletedBytes { get; init; }

    public long TotalBytes { get; init; }

    public bool IsIndeterminate { get; init; }

    public double? PercentageOverride { get; init; }

    public double Percentage => PercentageOverride ?? (TotalFiles == 0
        ? 100
        : Math.Min(100, CompletedFiles * 100d / TotalFiles));
}
