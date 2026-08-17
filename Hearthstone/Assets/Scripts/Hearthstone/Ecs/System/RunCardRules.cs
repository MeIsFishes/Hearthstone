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
        RecipeNotFound,
        ResultAlreadyOwned,
        StatOverflow,
        CardNumberSumNotExact,
    }

    public readonly struct FusionEvaluationData
    {
        public int MaterialCount { get; }
        public int CardNumberSum { get; }
        public int RecipeMaterialCount { get; }
        public int ResultCardNumber { get; }
        public int PresentationCardNumber { get; }
        public EFusionOperationResult BlockingResult { get; }
        public bool CanFuse => BlockingResult == EFusionOperationResult.Applied;

        public FusionEvaluationData(
            int materialCount,
            int cardNumberSum,
            int recipeMaterialCount,
            int resultCardNumber,
            int presentationCardNumber,
            EFusionOperationResult blockingResult)
        {
            MaterialCount = materialCount;
            CardNumberSum = cardNumberSum;
            RecipeMaterialCount = recipeMaterialCount;
            ResultCardNumber = resultCardNumber;
            PresentationCardNumber = presentationCardNumber;
            BlockingResult = blockingResult;
        }
    }

    public readonly struct FusionRecommendationData
    {
        private readonly int m_FirstCardNumber;
        private readonly int m_SecondCardNumber;
        private readonly int m_ThirdCardNumber;
        private readonly int m_FourthCardNumber;

        public int MaterialCount { get; }
        public int ResultCardNumber { get; }

        internal FusionRecommendationData(
            int firstCardNumber,
            int secondCardNumber,
            int thirdCardNumber,
            int fourthCardNumber,
            int materialCount,
            int resultCardNumber)
        {
            m_FirstCardNumber = firstCardNumber;
            m_SecondCardNumber = secondCardNumber;
            m_ThirdCardNumber = thirdCardNumber;
            m_FourthCardNumber = fourthCardNumber;
            MaterialCount = materialCount;
            ResultCardNumber = resultCardNumber;
        }

        public int GetCardNumber(int index)
        {
            if (index < 0 || index >= MaterialCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            switch (index)
            {
                case 0:
                    return m_FirstCardNumber;
                case 1:
                    return m_SecondCardNumber;
                case 2:
                    return m_ThirdCardNumber;
                default:
                    return m_FourthCardNumber;
            }
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
        public const int InitialBattleSlotCount = 2;
        public const int InitialDrawCardCount = 3;
        public const int MaximumBattleSlotCount = 6;
        public const int BattleSlotCount = InitialBattleSlotCount;
        public const int FirstCardNumber = 1;
        public const int LastOrdinaryCardNumber = 98;
        public const int LockedCardNumber = 99;
        public const int FirstFusionCardNumber = 100;
        public const int FirstLegendaryCardNumber = 149;
        public const int LastFusionCardNumber = 213;
        public const int LastCardNumber = LastFusionCardNumber;
        public const int CardStorageLength = LastCardNumber + 1;
        public const int CardsPerRow = 7;
        public const int CardRowCount = (LastCardNumber + CardsPerRow - 1) / CardsPerRow;
        public const int CardAspectWidth = 25;
        public const int CardAspectHeight = 36;
        public const int RewardGrantCount = 5;
        public const int FusionSlotCount = 4;
        public const int FusionTargetCardNumberSum = 99;
        public const int FusionMinimumSelectionCount = 2;
        public const int FusionMaximumSelectionCount = FusionSlotCount;
        public const int FusionMinimumRecipeMaterialCount = 2;
        public const int FusionMaximumRecipeMaterialCount = FusionSlotCount;
        public const int BaseCardTypeCount = 5;
        public const int OgreCardTypeId = 5;
        public const int MaximumOgreRecipeCount = 2;

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

            var pending = new RunCardInstanceData[batch.Grants.Count];
            for (var index = 0; index < batch.Grants.Count; index++)
            {
                var grant = batch.Grants[index];
                pending[index] = new RunCardInstanceData(grant.CardNumber, grant.Attack, grant.MaxHealth);
            }

            for (var index = 0; index < pending.Length; index++)
                runState.AddCardInstance(pending[index]);
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
            if (cardNumber >= LockedCardNumber && cardNumber <= LastCardNumber)
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
                return CreateFusionEvaluation(0, 0, 0, 0, EFusionOperationResult.InvalidSlot);

            return EvaluateFusion(runState, session.FusionSlotCardNumbers);
        }

        public static int FindFusionRecommendations(
            RunStateSingletonRawComponent runState,
            PreparationSessionSingletonRawComponent session,
            List<FusionRecommendationData> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));
            results.Clear();
            if (runState == null || session == null)
                return 0;

            var selectedCardNumbers = new int[FusionSlotCount];
            var selectedCount = 0;
            var selectedSum = 0;
            for (var slot = 0; slot < FusionSlotCount; slot++)
            {
                var cardNumber = session.FusionSlotCardNumbers[slot];
                if (cardNumber == 0)
                    continue;
                if (cardNumber < FirstCardNumber || cardNumber > LastOrdinaryCardNumber ||
                    runState.HasCard(cardNumber) == false)
                    return 0;
                for (var index = 0; index < selectedCount; index++)
                {
                    if (selectedCardNumbers[index] == cardNumber)
                        return 0;
                }

                var cardConfig = BbxCommon.DataApi.GetData<BattleCardCsvData>(cardNumber);
                if (cardConfig == null || cardConfig.IsFusionResult)
                    return 0;
                selectedCardNumbers[selectedCount++] = cardNumber;
                selectedSum += cardNumber;
            }

            if (selectedCount > FusionMaximumSelectionCount ||
                selectedSum > FusionTargetCardNumberSum)
                return 0;

            var candidateCardNumbers = new int[LastOrdinaryCardNumber];
            var candidateCount = 0;
            for (var cardNumber = FirstCardNumber; cardNumber <= LastOrdinaryCardNumber; cardNumber++)
            {
                if (runState.HasCard(cardNumber) == false ||
                    ContainsCardNumber(selectedCardNumbers, selectedCount, cardNumber))
                    continue;
                var cardConfig = BbxCommon.DataApi.GetData<BattleCardCsvData>(cardNumber);
                if (cardConfig == null || cardConfig.IsFusionResult)
                    continue;
                candidateCardNumbers[candidateCount++] = cardNumber;
            }

            var workingCardNumbers = new int[FusionSlotCount];
            Array.Copy(selectedCardNumbers, workingCardNumbers, selectedCount);
            var minimumMaterialCount = Math.Max(FusionMinimumSelectionCount, selectedCount);
            for (var materialCount = minimumMaterialCount;
                 materialCount <= FusionMaximumSelectionCount;
                 materialCount++)
            {
                FindFusionRecommendations(
                    runState,
                    results,
                    candidateCardNumbers,
                    candidateCount,
                    workingCardNumbers,
                    selectedCount,
                    0,
                    materialCount - selectedCount,
                    FusionTargetCardNumberSum - selectedSum);
            }
            return results.Count;
        }

        public static EFusionOperationResult TryApplyFusionRecommendation(
            RunStateSingletonRawComponent runState,
            PreparationSessionSingletonRawComponent session,
            FusionRecommendationData recommendation)
        {
            if (runState == null || session == null)
                return EFusionOperationResult.InvalidSlot;
            if (recommendation.MaterialCount < FusionMinimumSelectionCount ||
                recommendation.MaterialCount > FusionMaximumSelectionCount)
                return EFusionOperationResult.MaterialCountInvalid;

            var materialCardNumbers = new int[FusionSlotCount];
            for (var index = 0; index < recommendation.MaterialCount; index++)
                materialCardNumbers[index] = recommendation.GetCardNumber(index);

            var evaluation = EvaluateFusion(runState, materialCardNumbers);
            if (evaluation.CanFuse == false ||
                evaluation.ResultCardNumber != recommendation.ResultCardNumber)
                return evaluation.CanFuse
                    ? EFusionOperationResult.RecipeNotFound
                    : evaluation.BlockingResult;

            var changed = false;
            for (var slot = 0; slot < FusionSlotCount; slot++)
            {
                if (session.FusionSlotCardNumbers[slot] == materialCardNumbers[slot])
                    continue;
                changed = true;
                break;
            }
            if (changed == false)
                return EFusionOperationResult.NoChange;

            Array.Copy(materialCardNumbers, session.FusionSlotCardNumbers, FusionSlotCount);
            session.FusionRevision.SetValue(session.FusionRevision.Value + 1);
            return EFusionOperationResult.Applied;
        }

        private static FusionEvaluationData EvaluateFusion(
            RunStateSingletonRawComponent runState,
            int[] materialCardNumbers)
        {
            if (runState == null || materialCardNumbers == null ||
                materialCardNumbers.Length < FusionSlotCount)
                return CreateFusionEvaluation(0, 0, 0, 0, EFusionOperationResult.InvalidSlot);

            var materialCount = 0;
            var cardNumberSum = 0;
            var firstTypeId = 0;
            var secondTypeId = 0;
            var thirdTypeId = 0;
            var fourthTypeId = 0;
            var firstCardNumber = 0;
            var secondCardNumber = 0;
            var thirdCardNumber = 0;
            var fourthCardNumber = 0;
            for (var slot = 0; slot < FusionSlotCount; slot++)
            {
                var cardNumber = materialCardNumbers[slot];
                if (cardNumber == 0)
                    continue;
                materialCount++;
                cardNumberSum += cardNumber;
                if (cardNumber >= LockedCardNumber && cardNumber <= LastCardNumber)
                {
                    return CreateFusionEvaluation(
                        materialCount, cardNumberSum, 0, 0, EFusionOperationResult.ResultCardCannotBeMaterial);
                }
                if (cardNumber < FirstCardNumber || cardNumber > LastOrdinaryCardNumber ||
                    runState.HasCard(cardNumber) == false)
                    return CreateFusionEvaluation(materialCount, cardNumberSum, 0, 0, EFusionOperationResult.UnownedCard);
                for (var previousSlot = 0; previousSlot < slot; previousSlot++)
                {
                    if (materialCardNumbers[previousSlot] == cardNumber)
                    {
                        return CreateFusionEvaluation(
                            materialCount, cardNumberSum, 0, 0, EFusionOperationResult.DuplicateMaterial);
                    }
                }

                var cardConfig = BbxCommon.DataApi.GetData<BattleCardCsvData>(cardNumber);
                if (cardConfig == null || cardConfig.IsFusionResult)
                    return CreateFusionEvaluation(materialCount, cardNumberSum, 0, 0, EFusionOperationResult.UnownedCard);
                switch (materialCount)
                {
                    case 1:
                        firstCardNumber = cardNumber;
                        firstTypeId = cardConfig.CardTypeId;
                        break;
                    case 2:
                        secondCardNumber = cardNumber;
                        secondTypeId = cardConfig.CardTypeId;
                        break;
                    case 3:
                        thirdCardNumber = cardNumber;
                        thirdTypeId = cardConfig.CardTypeId;
                        break;
                    case 4:
                        fourthCardNumber = cardNumber;
                        fourthTypeId = cardConfig.CardTypeId;
                        break;
                }
            }

            if (materialCount < FusionMinimumSelectionCount || materialCount > FusionMaximumSelectionCount)
                return CreateFusionEvaluation(materialCount, cardNumberSum, 0, 0, EFusionOperationResult.MaterialCountInvalid);
            if (cardNumberSum != FusionTargetCardNumberSum)
            {
                return CreateFusionEvaluation(
                    materialCount,
                    cardNumberSum,
                    materialCount,
                    0,
                    EFusionOperationResult.CardNumberSumNotExact);
            }

            var firstPresentationTypeId = firstTypeId;
            var secondPresentationTypeId = secondTypeId;
            var thirdPresentationTypeId = thirdTypeId;
            var fourthPresentationTypeId = fourthTypeId;
            SortFusionTypeIds(
                ref firstTypeId,
                ref secondTypeId,
                ref thirdTypeId,
                ref fourthTypeId,
                materialCount);
            var recipeMaterialCount = materialCount;
            var resultConfig = BattleCardCsvData.GetFusionResult(
                firstTypeId,
                secondTypeId,
                thirdTypeId,
                fourthTypeId,
                recipeMaterialCount);
            if (resultConfig == null)
            {
                return CreateFusionEvaluation(
                    materialCount, cardNumberSum, recipeMaterialCount, 0, EFusionOperationResult.RecipeNotFound);
            }
            var presentationCardNumber = ResolveFusionPresentationCardNumber(
                firstCardNumber,
                firstPresentationTypeId,
                secondCardNumber,
                secondPresentationTypeId,
                thirdCardNumber,
                thirdPresentationTypeId,
                fourthCardNumber,
                fourthPresentationTypeId,
                materialCount,
                resultConfig.CardNumber);
            if (presentationCardNumber == 0)
            {
                return CreateFusionEvaluation(
                    materialCount, cardNumberSum, recipeMaterialCount, 0, EFusionOperationResult.RecipeNotFound);
            }
            if (runState.HasCard(resultConfig.CardNumber))
            {
                return CreateFusionEvaluation(
                    materialCount,
                    cardNumberSum,
                    recipeMaterialCount,
                    resultConfig.CardNumber,
                    presentationCardNumber,
                    EFusionOperationResult.ResultAlreadyOwned);
            }
            return CreateFusionEvaluation(
                materialCount,
                cardNumberSum,
                recipeMaterialCount,
                resultConfig.CardNumber,
                presentationCardNumber,
                EFusionOperationResult.Applied);
        }

        private static void FindFusionRecommendations(
            RunStateSingletonRawComponent runState,
            List<FusionRecommendationData> results,
            int[] candidateCardNumbers,
            int candidateCount,
            int[] workingCardNumbers,
            int workingIndex,
            int candidateStartIndex,
            int remainingCandidateCount,
            int remainingCardNumberSum)
        {
            if (remainingCandidateCount == 0)
            {
                if (remainingCardNumberSum != 0)
                    return;
                var evaluation = EvaluateFusion(runState, workingCardNumbers);
                if (evaluation.CanFuse == false)
                    return;

                var first = workingCardNumbers[0];
                var second = workingCardNumbers[1];
                var third = workingCardNumbers[2];
                var fourth = workingCardNumbers[3];
                SortFusionTypeIds(
                    ref first,
                    ref second,
                    ref third,
                    ref fourth,
                    evaluation.MaterialCount);
                results.Add(new FusionRecommendationData(
                    first,
                    second,
                    third,
                    fourth,
                    evaluation.MaterialCount,
                    evaluation.ResultCardNumber));
                return;
            }
            if (remainingCardNumberSum <= 0 ||
                candidateCount - candidateStartIndex < remainingCandidateCount)
                return;

            var lastCandidateIndex = candidateCount - remainingCandidateCount;
            for (var candidateIndex = candidateStartIndex;
                 candidateIndex <= lastCandidateIndex;
                 candidateIndex++)
            {
                var cardNumber = candidateCardNumbers[candidateIndex];
                if (cardNumber > remainingCardNumberSum)
                    break;
                workingCardNumbers[workingIndex] = cardNumber;
                FindFusionRecommendations(
                    runState,
                    results,
                    candidateCardNumbers,
                    candidateCount,
                    workingCardNumbers,
                    workingIndex + 1,
                    candidateIndex + 1,
                    remainingCandidateCount - 1,
                    remainingCardNumberSum - cardNumber);
                workingCardNumbers[workingIndex] = 0;
            }
        }

        private static bool ContainsCardNumber(int[] cardNumbers, int count, int cardNumber)
        {
            for (var index = 0; index < count; index++)
            {
                if (cardNumbers[index] == cardNumber)
                    return true;
            }
            return false;
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
            var materialInstances = new RunCardInstanceData[evaluation.MaterialCount];
            var materialSnapshots = new FusionMaterialSnapshot[evaluation.MaterialCount];
            var battleSlotsBefore = (int[])runState.BattleSlotCardNumbers.Clone();
            var materialCount = 0;
            for (var slot = 0; slot < FusionSlotCount; slot++)
            {
                var cardNumber = session.FusionSlotCardNumbers[slot];
                if (cardNumber == 0)
                    continue;
                var instance = runState.GetCardInstance(cardNumber);
                materialNumbers[materialCount] = cardNumber;
                materialInstances[materialCount] = instance;
                materialSnapshots[materialCount] = new FusionMaterialSnapshot(
                    slot,
                    instance,
                    FindBattleSlot(runState, cardNumber));
                materialCount++;
            }
            var resultConfig = BbxCommon.DataApi.GetData<BattleCardCsvData>(evaluation.ResultCardNumber);
            if (TryCreateFusionResultInstance(resultConfig, materialInstances, out resultCard) == false)
                return EFusionOperationResult.StatOverflow;
            transaction = new FusionTransactionSnapshot(
                materialSnapshots,
                battleSlotsBefore,
                resultCard);

            for (var index = 0; index < materialCount; index++)
                runState.RemoveCardInstance(materialNumbers[index], out _);
            for (var battleSlot = 0; battleSlot < runState.BattleSlotCardNumbers.Length; battleSlot++)
            {
                for (var index = 0; index < materialCount; index++)
                {
                    if (runState.BattleSlotCardNumbers[battleSlot] != materialNumbers[index])
                        continue;
                    runState.BattleSlotCardNumbers[battleSlot] = 0;
                    break;
                }
            }
            runState.AddCardInstance(resultCard);
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

        public static bool TryCreateFusionResultInstance(
            BattleCardCsvData resultConfig,
            IReadOnlyList<RunCardInstanceData> materials,
            out RunCardInstanceData resultCard)
        {
            resultCard = default;
            if (resultConfig == null || resultConfig.IsFusionResult == false)
                throw new ArgumentException("Fusion result configuration is missing or invalid.", nameof(resultConfig));
            if (materials == null || materials.Count != resultConfig.FusionRecipeTypeIds.Count)
                throw new ArgumentException("Fusion materials do not match the result recipe length.", nameof(materials));

            long attack = 0;
            long maxHealth = 0;
            var keywords = EBattleKeyword.None;
            var firstCardNumber = 0;
            var secondCardNumber = 0;
            var thirdCardNumber = 0;
            var fourthCardNumber = 0;
            var firstTypeId = 0;
            var secondTypeId = 0;
            var thirdTypeId = 0;
            var fourthTypeId = 0;
            for (var index = 0; index < materials.Count; index++)
            {
                var material = materials[index];
                if (material.IsValid == false || material.CardNumber > LastOrdinaryCardNumber)
                    throw new ArgumentException("Fusion materials must be valid ordinary cards.", nameof(materials));
                for (var previous = 0; previous < index; previous++)
                {
                    if (materials[previous].CardNumber == material.CardNumber)
                        throw new ArgumentException("Fusion materials cannot repeat a card number.", nameof(materials));
                }

                var cardConfig = BbxCommon.DataApi.GetData<BattleCardCsvData>(material.CardNumber);
                if (cardConfig == null || cardConfig.IsFusionResult)
                    throw new ArgumentException($"Fusion material card {material.CardNumber} is not configured as an ordinary card.", nameof(materials));
                switch (index)
                {
                    case 0:
                        firstCardNumber = material.CardNumber;
                        firstTypeId = cardConfig.CardTypeId;
                        break;
                    case 1:
                        secondCardNumber = material.CardNumber;
                        secondTypeId = cardConfig.CardTypeId;
                        break;
                    case 2:
                        thirdCardNumber = material.CardNumber;
                        thirdTypeId = cardConfig.CardTypeId;
                        break;
                    case 3:
                        fourthCardNumber = material.CardNumber;
                        fourthTypeId = cardConfig.CardTypeId;
                        break;
                }
                attack += material.Attack;
                maxHealth += material.MaxHealth;
                keywords = BattleKeywordRules.MergeFusionKeywords(keywords, material.Keywords);
            }

            var sortedFirstTypeId = firstTypeId;
            var sortedSecondTypeId = secondTypeId;
            var sortedThirdTypeId = thirdTypeId;
            var sortedFourthTypeId = fourthTypeId;
            SortFusionTypeIds(
                ref sortedFirstTypeId,
                ref sortedSecondTypeId,
                ref sortedThirdTypeId,
                ref sortedFourthTypeId,
                materials.Count);
            var sortedTypes = new[]
            {
                sortedFirstTypeId,
                sortedSecondTypeId,
                sortedThirdTypeId,
                sortedFourthTypeId,
            };
            for (var index = 0; index < resultConfig.FusionRecipeTypeIds.Count; index++)
            {
                if (resultConfig.FusionRecipeTypeIds[index] != sortedTypes[index])
                    throw new ArgumentException("Fusion material types do not match the result recipe.", nameof(materials));
            }

            if (attack > int.MaxValue || maxHealth > int.MaxValue)
                return false;
            var presentationCardNumber = ResolveFusionPresentationCardNumber(
                firstCardNumber,
                firstTypeId,
                secondCardNumber,
                secondTypeId,
                thirdCardNumber,
                thirdTypeId,
                fourthCardNumber,
                fourthTypeId,
                materials.Count,
                resultConfig.CardNumber);
            if (presentationCardNumber == 0)
                throw new InvalidOperationException($"Fusion result {resultConfig.CardNumber} has no presentation card.");

            resultCard = new RunCardInstanceData(
                resultConfig.CardNumber,
                (int)attack,
                (int)maxHealth,
                keywords,
                GetTierForFusionMaterialCount(materials.Count),
                presentationCardNumber);
            return true;
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

        public static EBattleCardTier GetTierForFusionMaterialCount(int materialCount)
        {
            switch (materialCount)
            {
                case 2:
                    return EBattleCardTier.Silver;
                case 3:
                    return EBattleCardTier.Gold;
                case 4:
                    return EBattleCardTier.Legendary;
                default:
                    throw new ArgumentOutOfRangeException(nameof(materialCount));
            }
        }

        public static bool TryPlaceCard(
            RunStateSingletonRawComponent runState,
            int cardNumber,
            int targetSlot)
        {
            if (runState == null || runState.HasCard(cardNumber) == false)
                return false;
            var availableSlotCount = runState.UnlockedBattleSlotCount == 0
                ? InitialBattleSlotCount
                : runState.UnlockedBattleSlotCount;
            if (targetSlot < 0 || targetSlot >= availableSlotCount)
                return false;

            var sourceSlot = -1;
            for (var slot = 0; slot < runState.BattleSlotCardNumbers.Length; slot++)
            {
                if (runState.BattleSlotCardNumbers[slot] == cardNumber)
                {
                    sourceSlot = slot;
                    break;
                }
            }
            if (sourceSlot == targetSlot)
                return false;

            var targetCardNumber = runState.BattleSlotCardNumbers[targetSlot];
            if (sourceSlot >= 0)
                runState.BattleSlotCardNumbers[sourceSlot] = targetCardNumber;
            runState.BattleSlotCardNumbers[targetSlot] = cardNumber;
            runState.Revision.SetValue(runState.Revision.Value + 1);
            return true;
        }

        public static bool TryRemoveCardFromBattleSlot(
            RunStateSingletonRawComponent runState,
            int sourceSlot,
            int expectedCardNumber)
        {
            if (runState == null || expectedCardNumber <= 0 ||
                sourceSlot < 0 || sourceSlot >= runState.BattleSlotCardNumbers.Length ||
                runState.BattleSlotCardNumbers[sourceSlot] != expectedCardNumber)
                return false;

            runState.BattleSlotCardNumbers[sourceSlot] = 0;
            runState.Revision.SetValue(runState.Revision.Value + 1);
            return true;
        }

        private static FusionEvaluationData CreateFusionEvaluation(
            int materialCount,
            int cardNumberSum,
            int recipeMaterialCount,
            int resultCardNumber,
            EFusionOperationResult result)
        {
            return CreateFusionEvaluation(
                materialCount,
                cardNumberSum,
                recipeMaterialCount,
                resultCardNumber,
                0,
                result);
        }

        private static FusionEvaluationData CreateFusionEvaluation(
            int materialCount,
            int cardNumberSum,
            int recipeMaterialCount,
            int resultCardNumber,
            int presentationCardNumber,
            EFusionOperationResult result)
        {
            return new FusionEvaluationData(
                materialCount,
                cardNumberSum,
                recipeMaterialCount,
                resultCardNumber,
                presentationCardNumber,
                result);
        }

        private static int ResolveFusionPresentationCardNumber(
            int firstCardNumber,
            int firstTypeId,
            int secondCardNumber,
            int secondTypeId,
            int thirdCardNumber,
            int thirdTypeId,
            int fourthCardNumber,
            int fourthTypeId,
            int materialCount,
            int resultCardNumber)
        {
            if (materialCount != FusionMaximumSelectionCount)
                return resultCardNumber;

            SortMaterialAscending(
                ref firstCardNumber,
                ref firstTypeId,
                ref secondCardNumber,
                ref secondTypeId);
            SortMaterialAscending(
                ref secondCardNumber,
                ref secondTypeId,
                ref thirdCardNumber,
                ref thirdTypeId);
            SortMaterialAscending(
                ref firstCardNumber,
                ref firstTypeId,
                ref secondCardNumber,
                ref secondTypeId);
            SortMaterialAscending(
                ref thirdCardNumber,
                ref thirdTypeId,
                ref fourthCardNumber,
                ref fourthTypeId);
            SortMaterialAscending(
                ref secondCardNumber,
                ref secondTypeId,
                ref thirdCardNumber,
                ref thirdTypeId);
            SortMaterialAscending(
                ref firstCardNumber,
                ref firstTypeId,
                ref secondCardNumber,
                ref secondTypeId);

            var presentationConfig = BattleCardCsvData.GetFusionResult(
                secondTypeId,
                thirdTypeId,
                fourthTypeId,
                0,
                3);
            return presentationConfig == null ? 0 : presentationConfig.CardNumber;
        }

        private static void SortMaterialAscending(
            ref int leftCardNumber,
            ref int leftTypeId,
            ref int rightCardNumber,
            ref int rightTypeId)
        {
            if (leftCardNumber <= rightCardNumber)
                return;
            var temporaryCardNumber = leftCardNumber;
            leftCardNumber = rightCardNumber;
            rightCardNumber = temporaryCardNumber;
            var temporaryTypeId = leftTypeId;
            leftTypeId = rightTypeId;
            rightTypeId = temporaryTypeId;
        }

        private static void SortFusionTypeIds(
            ref int firstTypeId,
            ref int secondTypeId,
            ref int thirdTypeId,
            ref int fourthTypeId,
            int materialCount)
        {
            SortAscending(ref firstTypeId, ref secondTypeId);
            if (materialCount >= 3)
            {
                SortAscending(ref secondTypeId, ref thirdTypeId);
                SortAscending(ref firstTypeId, ref secondTypeId);
            }
            if (materialCount == 4)
            {
                SortAscending(ref thirdTypeId, ref fourthTypeId);
                SortAscending(ref secondTypeId, ref thirdTypeId);
                SortAscending(ref firstTypeId, ref secondTypeId);
            }
        }

        private static void SortAscending(ref int left, ref int right)
        {
            if (left <= right)
                return;
            var temporary = left;
            left = right;
            right = temporary;
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
            for (var slot = 0; slot < runState.BattleSlotCardNumbers.Length; slot++)
            {
                if (runState.BattleSlotCardNumbers[slot] == cardNumber)
                    return slot;
            }
            return -1;
        }

        private static string CreateRewardBatchPayloadFingerprint(
            PreparationRewardBatchStartupData batch)
        {
            var sorted = new RewardCardGrantStartupData[batch.Grants.Count];
            for (var index = 0; index < sorted.Length; index++)
                sorted[index] = batch.Grants[index];
            Array.Sort(sorted, (left, right) => left.CardNumber.CompareTo(right.CardNumber));

            var builder = new StringBuilder(sorted.Length * 16);
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
