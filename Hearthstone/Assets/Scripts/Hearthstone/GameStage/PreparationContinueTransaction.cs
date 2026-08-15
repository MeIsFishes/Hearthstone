using System;

namespace Hearthstone
{
    public enum EPreparationContinueResult
    {
        Accepted,
        DuplicateIgnored,
        InvalidStage,
        InvalidRuntimeState,
        InvalidProgressionConfig,
        Committed,
    }

    public sealed class PreparationContinueTransactionSnapshot
    {
        public long AttemptId { get; }
        public int FromBattleNumber { get; }
        public int TargetBattleNumber { get; }
        public int[] BattleSlotCardNumbers { get; }
        public BattlePlayerLineupStartupData PlayerLineup { get; }
        public int[] FusionSlotCardNumbers { get; }
        public int OwnedCardCount { get; }
        public int RunRevision { get; }
        public int FusionRevision { get; }
        public int AppliedRewardBatchCount { get; }
        public int BattleStageCreationCount { get; }
        public PreparationRewardBatchStartupData TargetRewardBatch { get; }

        public PreparationContinueTransactionSnapshot(
            long attemptId,
            int fromBattleNumber,
            int targetBattleNumber,
            BattlePlayerLineupStartupData playerLineup,
            int[] fusionSlotCardNumbers,
            int ownedCardCount,
            int runRevision,
            int fusionRevision,
            int appliedRewardBatchCount,
            int battleStageCreationCount,
            PreparationRewardBatchStartupData targetRewardBatch)
        {
            if (attemptId <= 0)
                throw new ArgumentOutOfRangeException(nameof(attemptId));
            if (fromBattleNumber <= 0 || targetBattleNumber != fromBattleNumber)
                throw new ArgumentOutOfRangeException(nameof(targetBattleNumber));
            if (playerLineup == null ||
                playerLineup.SlotCount < RunCardRules.InitialBattleSlotCount ||
                playerLineup.SlotCount > RunCardRules.MaximumBattleSlotCount)
                throw new ArgumentException("Continue snapshot has an invalid battle slot count.", nameof(playerLineup));
            if (fusionSlotCardNumbers == null || fusionSlotCardNumbers.Length != RunCardRules.FusionSlotCount)
                throw new ArgumentException("Continue snapshot requires exactly four fusion slots.", nameof(fusionSlotCardNumbers));

            AttemptId = attemptId;
            FromBattleNumber = fromBattleNumber;
            TargetBattleNumber = targetBattleNumber;
            PlayerLineup = playerLineup.CreateSnapshot();
            BattleSlotCardNumbers = new int[playerLineup.SlotCount];
            for (var slot = 0; slot < BattleSlotCardNumbers.Length; slot++)
                BattleSlotCardNumbers[slot] = PlayerLineup.GetSlot(slot).CardNumber;
            FusionSlotCardNumbers = (int[])fusionSlotCardNumbers.Clone();
            OwnedCardCount = ownedCardCount;
            RunRevision = runRevision;
            FusionRevision = fusionRevision;
            AppliedRewardBatchCount = appliedRewardBatchCount;
            BattleStageCreationCount = battleStageCreationCount;
            TargetRewardBatch = targetRewardBatch?.CreateSnapshot()
                ?? throw new ArgumentNullException(nameof(targetRewardBatch));
        }
    }
}
