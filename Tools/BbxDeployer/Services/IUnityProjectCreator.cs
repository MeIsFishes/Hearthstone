using BbxDeployer.Core;

namespace BbxDeployer.Services;

public interface IUnityProjectCreator
{
    Task CreateAsync(
        UnityEditorInstallation editor,
        string unityProjectRoot,
        CancellationToken cancellationToken);
}
