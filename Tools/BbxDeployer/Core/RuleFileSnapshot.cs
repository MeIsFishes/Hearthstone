namespace BbxDeployer.Core;

public sealed class RuleFileSnapshot
{
    public required string Path { get; init; }

    public long Length { get; init; }

    public DateTime LastWriteTimeUtc { get; init; }
}
