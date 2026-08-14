using BbxDeployer.Core;
using BbxDeployer.Services;

namespace BbxDeployer.Tests;

internal sealed class FakeUnityEditorLocator(
    IReadOnlyList<UnityEditorInstallation> editors) : IUnityEditorLocator
{
    public int DiscoveryCount { get; private set; }

    public IReadOnlyList<UnityEditorInstallation> DiscoverInstalledEditors()
    {
        DiscoveryCount++;
        return editors;
    }
}
