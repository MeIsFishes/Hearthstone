using BbxDeployer.Core;

namespace BbxDeployer.Services;

public interface IUnityEditorLocator
{
    IReadOnlyList<UnityEditorInstallation> DiscoverInstalledEditors();
}
