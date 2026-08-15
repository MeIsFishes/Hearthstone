using BbxCommon.Ui;

namespace Hearthstone
{
    public enum EPreparationUiGroup
    {
        Main,
    }

    public sealed class PreparationUiScene : UiSceneBase<EPreparationUiGroup>
    {
        protected override void OnSceneInit()
        {
            UiGroupWrapper.CreateUiGroupRoot(EPreparationUiGroup.Main);
        }
    }
}
