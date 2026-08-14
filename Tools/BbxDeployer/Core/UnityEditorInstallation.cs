namespace BbxDeployer.Core;

public sealed class UnityEditorInstallation
{
    public required string Version { get; init; }

    public required string ExecutablePath { get; init; }

    public string DisplayName => $"{Version}  —  {ExecutablePath}";
}
