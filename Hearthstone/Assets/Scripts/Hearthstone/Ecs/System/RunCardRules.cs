using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Hearthstone
{
    public enum ERewardBatchApplyResult
    {
        Applied,
        AlreadyApplied,
    }

    public enum EFusionOperationResult
    {
        Applied,
        NoChange,
        InvalidSlot,
        UnownedCard,
        ResultCardCannotBeMaterial,
        DuplicateMaterial,
        MaterialCountInvalid,
        SumMismatch,
        ResultAlreadyOwned,
        StatOverflow,
    }

    public readonly struct FusionEvaluationData
    {
        public int MaterialCount { get; }
        public int CardNumberSum { get; }
        public EFusionOperationResult BlockingResult { get; }
        public bool CanFuse => BlockingResult == EFusionOperationResult.Applied;

        public FusionEvaluationData(
            int materialCount,
            int cardNumberSum,
            EFusionOperationResult blockingResult)
        {
            MaterialCount = materialCount;
            CardNumberSum = cardNumberSum;
            BlockingResult = blockingResult;
        }
    }

    public readonly struct FusionMaterialSnapshot
    {
        public int FusionSlot { get; }
        public int CardNumber { get; }
        public int Attack { get; }
        public int MaxHealth { get; }
        public EBattleKeyword Keywords { get; }
        public int BattleSlot { get; }

        public FusionMaterialSnapshot(
            int fusionSlot,
            RunCardInstanceData card,
            int battleSlot)
        {
            FusionSlot = fusionSlot;
            CardNumber = card.CardNumber;
            Attack = card.Attack;
            MaxHealth = card.MaxHealth;
            Keywords = card.Keywords;
            BattleSlot = battleSlot;
        }
    }

    public sealed class FusionTransactionSnapshot
    {
        private readonly FusionMaterialSnapshot[] m_Materials;
        private readonly int[] m_BattleSlotsBefore;

        public int MaterialCount => m_Materials.Length;
        public RunCardInstanceData ResultCard { get; }

        internal FusionTransactionSnapshot(
            FusionMaterialSnapshot[] materials,
            int[] battleSlotsBefore,
            RunCardInstanceData resultCard)
        {
            m_Materials = (FusionMaterialSnapshot[])materials.Clone();
            m_BattleSlotsBefore = (int[])battleSlotsBefore.Clone();
            ResultCard = resultCard;
        }

        public FusionMaterialSnapshot GetMaterial(int index) => m_Materials[index];

        public int GetBattleSlotBefore(int index) => m_BattleSlotsBefore[index];
    }

    public static class RunCardRules
    {
        public const int BattleSlotCount = 3;
        public const int FirstCardNumber = 1;
        public const int LastOrdinaryCardNumber = 98;
        public const int FusionResultCardNumber = 99;
        public const int LastCardNumber = FusionResultCardNumber;
        public const int CardStorageLength = LastCardNumber + 1;
        public const int CardsPerRow = 7;
        public const int CardRowCount = 15;
        public const int CardAspectWidth = 2;
        public const int CardAspectHeight = 3;
        public const int RewardGrantCount = 5;
        public const int FusionSlotCount = 4;
        public const int FusionMinimumMaterialCount = 2;
        public const int FusionTargetCardNumberSum = 99;

        public static ERewardBatchApplyResult ApplyRewardBatch(
            RunStateSingletonRawComponent runState,
            PreparationRewardBatchStartupData batch)
        {
            if (runState == null)
                throw new ArgumentNullException(nameof(runState));
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));

            var payloadFingerprint = CreateRewardBatchPayloadFingerprint(batch);
            if (runState.AppliedRewardBatchPayloadFingerprints.TryGetValue(
                    batch.BatchId,
                    out var appliedPayloadFingerprint))
            {
                if (string.Equals(
                        appliedPayloadFingerprint,
                        payloadFingerprint,
                        StringComparison.Ordinal) == false)
                    throw new InvalidOperationException($"Applied reward batch '{batch.BatchId}' has a different payload.");
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
            runState.AppliedRewardBatchPayloadFingerprints.Add(batch.BatchId, payloadFingerprint);
            runState.Revision.SetValue(runState.Revision.Value + 1);
            return ERewardBatchApplyResult.Applied;
        }

        public static EFusionOperationResult TrySetFusionMaterial(
            RunStateSingletonRawComponent runState,
            PreparationSessionSingletonRawComponent session,
            int cardNumber,
            int targetSlot,
            int sourceFusionSlot = -1)
        {
            if (runState == null || session == null || sourceFusionSlot < -1 ||
                targetSlot < 0 || targetSlot >= FusionSlotCount)
                return EFusionOperationResult.InvalidSlot;
            if (cardNumber == FusionResultCardNumber)
                return EFusionOperationResult.ResultCardCannotBeMaterial;
            if (cardNumber < FirstCardNumber || cardNumber > LastOrdinaryCardNumber ||
                runState.HasCard(cardNumber) == false)
                return EFusionOperationResult.UnownedCard;

            var existingSlot = FindFusionSlot(session, cardNumber);
            if (sourceFusionSlot >= 0)
            {
                if (sourceFusionSlot >= FusionSlotCount ||
                    session.FusionSlotCardNumbers[sourceFusionSlot] != cardNumber)
                    return EFusionOperationResult.InvalidSlot;
                if (sourceFusionSlot == targetSlot)
                    return EFusionOperationResult.NoChange;
                if (existingSlot >= 0 && existingSlot != sourceFusionSlot)
                    return EFusionOperationResult.DuplicateMaterial;
            }
            else if (existingSlot >= 0)
            {
                return EFusionOperationResult.DuplicateMaterial;
            }

            if (sourceFusionSlot >= 0)
                session.FusionSlotCardNumbers[sourceFusionSlot] = 0;
            session.FusionSlotCardNumbers[targetSlot] = cardNumber;
            session.FusionRevision.SetValue(session.FusionRevision.Value + 1);
            return EFusionOperationResult.Applied;
        }

        public static EFusionOperationResult TryRemoveFusionMaterial(
            PreparationSessionSingletonRawComponent session,
            int sourceFusionSlot)
        {
            if (session == null || sourceFusionSlot < 0 || sourceFusionSlot >= FusionSlotCount)
                return EFusionOperationResult.InvalidSlot;
            if (session.FusionSlotCardNumbers[sourceFusionSlot] == 0)
                return EFusionOperationResult.NoChange;

            session.FusionSlotCardNumbers[sourceFusionSlot] = 0;
            session.FusionRevision.SetValue(session.FusionRevision.Value + 1);
            return EFusionOperationResult.Applied;
        }

        public static FusionEvaluationData EvaluateFusion(
            RunStateSingletonRawComponent runState,
            PreparationSessionSingletonRawComponent session)
        {
            if (runState == null || session == null)
                return new FusionEvaluationData(0, 0, EFusionOperationResult.InvalidSlot);

            var materialCount = 0;
            var cardNumberSum = 0;
            var visited = new HashSet<int>();
            for (var slot = 0; slot < FusionSlotCount; slot++)
            {
                var cardNumber = session.FusionSlotCardNumbers[slot];
                if (cardNumber == 0)
                    continue;
                materialCount++;
                cardNumberSum += cardNumber;
                if (cardNumber == FusionResultCardNumber)
                    return new FusionEvaluationData(materialCount, cardNumberSum, EFusionOperationResult.ResultCardCannotBeMaterial);
                if (cardNumber < FirstCardNumber || cardNumber > LastOrdinaryCardNumber ||
                    runState.HasCard(cardNumber) == false)
                    return new FusionEvaluationData(materialCount, cardNumberSum, EFusionOperationResult.UnownedCard);
                if (visited.Add(cardNumber) == false)
                    return new FusionEvaluationData(materialCount, cardNumberSum, EFusionOperationResult.DuplicateMaterial);
            }

            if (runState.HasCard(FusionResultCardNumber))
                return new FusionEvaluationData(materialCount, cardNumberSum, EFusionOperationResult.ResultAlreadyOwned);
            if (materialCount < FusionMinimumMaterialCount || materialCount > FusionSlotCount)
                return new FusionEvaluationData(materialCount, cardNumberSum, EFusionOperationResult.MaterialCountInvalid);
            if (cardNumberSum != FusionTargetCardNumberSum)
                return new FusionEvaluationData(materialCount, cardNumberSum, EFusionOperationResult.SumMismatch);
            return new FusionEvaluationData(materialCount, cardNumberSum, EFusionOperationResult.Applied);
        }

        public static EFusionOperationResult TryFuse(
            RunStateSingletonRawComponent runState,
            PreparationSessionSingletonRawComponent session,
            out RunCardInstanceData resultCard)
        {
            return TryFuse(runState, session, out resultCard, out _);
        }

        public static EFusionOperationResult TryFuse(
            RunStateSingletonRawComponent runState,
            PreparationSessionSingletonRawComponent session,
            out RunCardInstanceData resultCard,
            out FusionTransactionSnapshot transaction)
        {
            resultCard = default;
            transaction = null;
            var evaluation = EvaluateFusion(runState, session);
            if (evaluation.CanFuse == false)
                return evaluation.BlockingResult;

            var materialNumbers = new int[FusionSlotCount];
            var materialSnapshots = new FusionMaterialSnapshot[evaluation.MaterialCount];
            var battleSlotsBefore = (int[])runState.BattleSlotCardNumbers.Clone();
            var materialCount = 0;
            long attack = 0;
            long maxHealth = 0;
            var keywords = EBattleKeyword.None;
            for (var slot = 0; slot < FusionSlotCount; slot++)
            {
                var cardNumber = session.FusionSlotCardNumbers[slot];
                if (cardNumber == 0)
                    continue;
                var instance = runState.CardInstances[cardNumber];
                materialNumbers[materialCount++] = cardNumber;
                materialSnapshots[materialCount - 1] = new FusionMaterialSnapshot(
                    slot,
                    instance,
                    FindBattleSlot(runState, cardNumber));
                attack += instance.Attack;
                maxHealth += instance.MaxHealth;
                keywords = BattleKeywordRules.UnionKeywords(keywords, instance.Keywords);
            }
            if (attack > int.MaxValue || maxHealth > int.MaxValue)
                return EFusionOperationResult.StatOverflow;

            resultCard = new RunCardInstanceData(
                FusionResultCardNumber,
                (int)attack,
                (int)maxHealth,
                keywords);
            transaction = new FusionTransactionSnapshot(
                materialSnapshots,
                battleSlotsBefore,
                resultCard);

            for (var index = 0; index < materialCount; index++)
                runState.CardInstances[materialNumbers[index]] = default;
            for (var battleSlot = 0; battleSlot < BattleSlotCount; battleSlot++)
            {
                for (var index = 0; index < materialCount; index++)
                {
                    if (runState.BattleSlotCardNumbers[battleSlot] != materialNumbers[index])
                        continue;
                    runState.BattleSlotCardNumbers[battleSlot] = 0;
                    break;
                }
            }
            runState.CardInstances[FusionResultCardNumber] = resultCard;
            Array.Clear(session.FusionSlotCardNumbers, 0, session.FusionSlotCardNumbers.Length);
            runState.Revision.SetValue(runState.Revision.Value + 1);
            session.FusionRevision.SetValue(session.FusionRevision.Value + 1);
            var keywordLog = new StringBuilder();
            for (var index = 0; index < materialSnapshots.Length; index++)
            {
                if (index > 0)
                    keywordLog.Append(';');
                keywordLog.Append(materialSnapshots[index].CardNumber);
                keywordLog.Append(':');
                keywordLog.Append(materialSnapshots[index].Keywords);
            }
            BbxCommon.DebugApi.Log(
                $"[BattleKeyword] Fusion Materials=[{keywordLog}] " +
                $"ResultCard={resultCard.CardNumber} ResultKeywords={resultCard.Keywords} " +
                $"ResultAttack={resultCard.Attack} ResultMaxHealth={resultCard.MaxHealth}");
            return EFusionOperationResult.Applied;
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

        private static int FindFusionSlot(
            PreparationSessionSingletonRawComponent session,
            int cardNumber)
        {
            for (var slot = 0; slot < FusionSlotCount; slot++)
            {
                if (session.FusionSlotCardNumbers[slot] == cardNumber)
                    return slot;
            }
            return -1;
        }

        private static int FindBattleSlot(
            RunStateSingletonRawComponent runState,
            int cardNumber)
        {
            for (var slot = 0; slot < BattleSlotCount; slot++)
            {
                if (runState.BattleSlotCardNumbers[slot] == cardNumber)
                    return slot;
            }
            return -1;
        }

        private static string CreateRewardBatchPayloadFingerprint(
            PreparationRewardBatchStartupData batch)
        {
            var sorted = new RewardCardGrantStartupData[RewardGrantCount];
            for (var index = 0; index < sorted.Length; index++)
                sorted[index] = batch.Grants[index];
            Array.Sort(sorted, (left, right) => left.CardNumber.CompareTo(right.CardNumber));

            var builder = new StringBuilder(RewardGrantCount * 16);
            for (var index = 0; index < sorted.Length; index++)
            {
                if (index > 0)
                    builder.Append('|');
                var grant = sorted[index];
                builder.Append(grant.CardNumber.ToString(CultureInfo.InvariantCulture));
                builder.Append(':');
                builder.Append(grant.Attack.ToString(CultureInfo.InvariantCulture));
                builder.Append(':');
                builder.Append(grant.MaxHealth.ToString(CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }
    }
}
