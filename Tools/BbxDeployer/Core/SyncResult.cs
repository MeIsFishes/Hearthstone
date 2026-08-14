namespace BbxDeployer.Core;

public sealed class SyncResult
{
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.Now;

    public DateTimeOffset FinishedAt { get; set; }

    public bool Cancelled { get; set; }

    public string? LogPath { get; set; }

    public List<TargetSyncResult> Targets { get; } = [];
}

public sealed class TargetSyncResult
{
    public string TargetName { get; init; } = string.Empty;

    public int CopiedFiles { get; set; }

    public long CopiedBytes { get; set; }

    public bool Succeeded { get; set; }

    public bool Cancelled { get; set; }

    public string? Error { get; set; }
}
