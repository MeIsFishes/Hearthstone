using System;
using System.Collections.Generic;

namespace Hearthstone
{
    public enum ERewardBatchApplyResult
    {
        Applied,
        AlreadyApplied,
    }

    public static class RunCardRules
    {
        public const int BattleSlotCount = 3;
        public const int FirstCardNumber = 1;
        public const int LastCardNumber = 98;
        public const int CardStorageLength = LastCardNumber + 1;
        public const int CardsPerRow = 7;
        public const int CardRowCount = 14;
        public const int CardAspectWidth = 2;
        public const int CardAspectHeight = 3;
        public const int RewardGrantCount = 5;

        public static ERewardBatchApplyResult ApplyRewardBatch(
            RunStateSingletonRawComponent runState,
            PreparationRewardBatchStartupData batch)
        {
            if (runState == null)
                throw new ArgumentNullException(nameof(runState));
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));

            if (runState.AppliedRewardBatchIds.Contains(batch.BatchId))
            {
                for (var index = 0; index < batch.Grants.Count; index++)
                {
                    var grant = batch.Grants[index];
                    var expected = new RunCardInstanceData(grant.CardNumber, grant.Attack, grant.MaxHealth);
                    if (runState.CardInstances[grant.CardNumber].Equals(expected) == false)
                        throw new InvalidOperationException($"Applied reward batch '{batch.BatchId}' does not match run state.");
                }
                return ERewardBatchApplyResult.AlreadyApplied;
            }

            var pending = new RunCardInstanceData[RewardGrantCount];
            for (var index = 0; index < batch.Grants.Count; index++)
            {
                var grant = batch.Grants[index];
                if (runState.HasCard(grant.CardNumber))
                {
                    throw new InvalidOperationException(
                        $"Reward batch '{batch.BatchId}' contains already-owned card {grant.CardNumber}; the whole batch was rejected.");
                }
                pending[index] = new RunCardInstanceData(grant.CardNumber, grant.Attack, grant.MaxHealth);
            }

            for (var index = 0; index < pending.Length; index++)
                runState.CardInstances[pending[index].CardNumber] = pending[index];
            runState.AppliedRewardBatchIds.Add(batch.BatchId);
            runState.Revision.SetValue(runState.Revision.Value + 1);
            return ERewardBatchApplyResult.Applied;
        }

        public static void InitializeFirstBattleLineup(
            RunStateSingletonRawComponent runState,
            IReadOnlyList<RunCardInstanceData> cards)
        {
            if (runState == null)
                throw new ArgumentNullException(nameof(runState));
            if (cards == null || cards.Count != BattleSlotCount)
                throw new ArgumentException($"Initial lineup must contain exactly {BattleSlotCount} cards.", nameof(cards));
            if (runState.GetOwnedCardCount() != 0)
                return;

            var visited = new HashSet<int>();
            for (var slot = 0; slot < cards.Count; slot++)
            {
                if (cards[slot].IsValid == false || visited.Add(cards[slot].CardNumber) == false)
                    throw new ArgumentException("Initial lineup cards must be valid and unique.", nameof(cards));
            }

            for (var slot = 0; slot < cards.Count; slot++)
            {
                var card = cards[slot];
                runState.CardInstances[card.CardNumber] = card;
                runState.BattleSlotCardNumbers[slot] = card.CardNumber;
            }
            runState.Revision.SetValue(runState.Revision.Value + 1);
        }

        public static bool TryPlaceCard(
            RunStateSingletonRawComponent runState,
            int cardNumber,
            int targetSlot)
        {
            if (runState == null || runState.HasCard(cardNumber) == false)
                return false;
            if (targetSlot < 0 || targetSlot >= BattleSlotCount)
                return false;

            var sourceSlot = -1;
            for (var slot = 0; slot < BattleSlotCount; slot++)
            {
                if (runState.BattleSlotCardNumbers[slot] == cardNumber)
                {
                    sourceSlot = slot;
                    break;
                }
            }
            if (sourceSlot == targetSlot)
                return false;

            if (sourceSlot >= 0)
                runState.BattleSlotCardNumbers[sourceSlot] = 0;
            runState.BattleSlotCardNumbers[targetSlot] = cardNumber;
            runState.Revision.SetValue(runState.Revision.Value + 1);
            return true;
        }
    }
}
