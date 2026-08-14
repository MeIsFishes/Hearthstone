using BbxDeployer.Core;
using BbxDeployer.Services;

namespace BbxDeployer.Tests;

internal sealed class FakeUnityProjectCreator(bool fail = false) : IUnityProjectCreator
{
    public int CallCount { get; private set; }

    public Task CreateAsync(
        UnityEditorInstallation editor,
        string unityProjectRoot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        if (fail)
        {
            throw new InvalidOperationException("simulated Unity failure");
        }

        Directory.CreateDirectory(Path.Combine(unityProjectRoot, "Assets"));
        Directory.CreateDirectory(Path.Combine(unityProjectRoot, "Packages"));
        Directory.CreateDirectory(Path.Combine(unityProjectRoot, "ProjectSettings"));
        File.WriteAllText(
            Path.Combine(unityProjectRoot, "Packages", "manifest.json"),
            """{"dependencies":{"created.by.unity":"0.0.1"}}""");
        File.WriteAllText(
            Path.Combine(unityProjectRoot, "Packages", "packages-lock.json"),
            """{"dependencies":{"created-by-unity":{}}}""");
        File.WriteAllText(
            Path.Combine(unityProjectRoot, "ProjectSettings", "ProjectVersion.txt"),
            $"m_EditorVersion: {editor.Version}{Environment.NewLine}");
        return Task.CompletedTask;
    }
}
