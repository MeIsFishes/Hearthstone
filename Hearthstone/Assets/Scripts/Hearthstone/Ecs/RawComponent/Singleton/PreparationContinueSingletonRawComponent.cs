using BbxCommon;

namespace Hearthstone
{
    public enum EPreparationContinueState
    {
        Idle,
        Waiting,
    }

    public sealed class PreparationContinueSingletonRawComponent : EcsSingletonRawComponent
    {
        public readonly ListenableVariable<EPreparationContinueState> State =
            new ListenableVariable<EPreparationContinueState>(EPreparationContinueState.Idle);

        protected override void OnSingletonCollect()
        {
            State.MakeInvalid();
            State.SetValue(EPreparationContinueState.Idle);
        }
    }
}
