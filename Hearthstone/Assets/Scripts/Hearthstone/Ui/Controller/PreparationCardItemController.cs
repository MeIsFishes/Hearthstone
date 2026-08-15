using BbxCommon.Ui;

namespace Hearthstone
{
    internal sealed class PreparationInteractorData
    {
        public int CardNumber;
        public EPreparationCardSource Source;
        public int SourceSlot;
        public int TargetSlot;
    }

    internal enum EPreparationCardSource
    {
        CardPool,
        BattleSlot,
        FusionSlot,
    }

    // Retained only so legacy serialized assets keep a resolvable controller type.
    // The active preparation card pool creates BattleCardItemController instances.
    public sealed class PreparationCardItemController : UiControllerBase<PreparationCardItemView>
    {
    }
}
