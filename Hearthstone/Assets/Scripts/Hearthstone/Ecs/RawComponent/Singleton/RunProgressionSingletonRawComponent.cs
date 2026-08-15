using BbxCommon;

namespace Hearthstone
{
    public sealed class RunProgressionSingletonRawComponent : EcsSingletonRawComponent
    {
        public int CurrentBattleNumber { get; private set; }
        public int BattleStageCreationCount { get; private set; }
        public int Revision { get; private set; }

        public void CommitBattle(int battleNumber)
        {
            if (battleNumber <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(battleNumber));
            if (CurrentBattleNumber != 0 && battleNumber != CurrentBattleNumber + 1)
            {
                throw new System.InvalidOperationException(
                    $"Battle progression must advance exactly once from {CurrentBattleNumber} to {battleNumber}.");
            }
            if (CurrentBattleNumber == battleNumber)
                throw new System.InvalidOperationException($"Battle {battleNumber} has already been committed.");

            CurrentBattleNumber = battleNumber;
            BattleStageCreationCount = checked(BattleStageCreationCount + 1);
            Revision = checked(Revision + 1);
        }

        protected override void OnSingletonCollect()
        {
            CurrentBattleNumber = 0;
            BattleStageCreationCount = 0;
            Revision = 0;
        }
    }
}
