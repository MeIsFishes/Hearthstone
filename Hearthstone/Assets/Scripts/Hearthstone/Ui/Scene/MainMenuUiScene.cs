using BbxCommon.Ui;

namespace Hearthstone
{
    public enum EMainMenuUiGroup
    {
        Main,
    }

    public sealed class MainMenuUiScene : UiSceneBase<EMainMenuUiGroup>
    {
        protected override void OnSceneInit()
        {
            UiGroupWrapper.CreateUiGroupRoot(EMainMenuUiGroup.Main);
        }
    }
}
