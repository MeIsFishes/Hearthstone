using BbxCommon.Ui;

namespace Hearthstone
{
    public enum EBattleUiGroup
    {
        Main,
    }

    public sealed class BattleUiScene : UiSceneBase<EBattleUiGroup>
    {
        protected override void OnSceneInit()
        {
            UiGroupWrapper.CreateUiGroupRoot(EBattleUiGroup.Main);
        }
    }
}
