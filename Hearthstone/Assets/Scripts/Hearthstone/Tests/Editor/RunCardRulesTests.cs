using System;
using System.Linq;
using System.Reflection;
using BbxCommon;
using BbxCommon.Ui;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthstone.Tests
{
    public sealed class RunCardRulesTests
    {
        [SetUp]
        public void SetUp()
        {
            DataApi.ReleaseAllData<BattleCardCsvData>(false);
            DataApi.ReleaseAllData<BattleCardTypeCsvData>(false);
            ResourceApi.Initialize();
            CsvApi.ReadFromString<BattleCardTypeCsvData>(
                nameof(BattleCardTypeCsvData),
                ResourceApi.LoadTextAsset(nameof(BattleCardTypeCsvData)).text);
            CsvApi.ReadFromString<BattleCardCsvData>(
                nameof(BattleCardCsvData),
                ResourceApi.LoadTextAsset(nameof(BattleCardCsvData)).text);
        }

        [TearDown]
        public void TearDown()
        {
            DataApi.ReleaseAllData<BattleCardCsvData>(false);
            DataApi.ReleaseAllData<BattleCardTypeCsvData>(false);
        }

        [Test]
        public void ApplyRewardBatch_PreservesDuplicateCopiesAndRemainsIdempotent()
        {
            var runState = new RunStateSingletonRawComponent();
            var batch = CreateBatch("batch-a", 2, 3, 5, 6, 7);

            Assert.AreEqual(ERewardBatchApplyResult.Applied, RunCardRules.ApplyRewardBatch(runState, batch));
            var revision = runState.Revision.Value;
            Assert.AreEqual(5, runState.GetOwnedCardCount());

            Assert.AreEqual(ERewardBatchApplyResult.AlreadyApplied, RunCardRules.ApplyRewardBatch(runState, batch));
            Assert.AreEqual(revision, runState.Revision.Value);
            Assert.AreEqual(5, runState.GetOwnedCardCount());

            var overlapping = CreateBatch("batch-b", 2, 8, 9, 10, 11);
            Assert.AreEqual(ERewardBatchApplyResult.Applied, RunCardRules.ApplyRewardBatch(runState, overlapping));
            Assert.AreEqual(revision + 1, runState.Revision.Value);
            Assert.AreEqual(10, runState.GetOwnedCardCount());
            Assert.AreEqual(2, runState.GetCardCopyCount(2));
            Assert.IsTrue(runState.AppliedRewardBatchPayloadFingerprints.ContainsKey("batch-b"));
            Assert.IsTrue(runState.HasCard(8));
            Assert.AreEqual(
                ERewardBatchApplyResult.AlreadyApplied,
                RunCardRules.ApplyRewardBatch(runState, overlapping));
            Assert.AreEqual(10, runState.GetOwnedCardCount());
        }

        [Test]
        public void ApplyRewardBatch_RejectsMismatchedPayloadForAppliedId()
        {
            var runState = new RunStateSingletonRawComponent();
            RunCardRules.ApplyRewardBatch(runState, CreateBatch("batch-a", 2, 3, 5, 6, 7));
            var altered = new PreparationRewardBatchStartupData(
                "batch-a",
                new[]
                {
                    new RewardCardGrantStartupData(2, 99, 3),
                    Grant(3), Grant(5), Grant(6), Grant(7),
                });

            Assert.Throws<InvalidOperationException>(() => RunCardRules.ApplyRewardBatch(runState, altered));
        }

        [Test]
        public void ApplyRewardBatch_RemainsIdempotentAfterGrantedCardsWereFused()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            var batch = CreateFusionBatch("fusion-batch");

            Assert.AreEqual(ERewardBatchApplyResult.Applied, RunCardRules.ApplyRewardBatch(runState, batch));
            SelectFusionMaterials(runState, session, 14, 20, 30, 35);
            Assert.AreEqual(
                EFusionOperationResult.Applied,
                RunCardRules.TryFuse(runState, session, out var result));
            Assert.AreEqual(11, result.Attack);
            Assert.AreEqual(15, result.MaxHealth);
            var revision = runState.Revision.Value;

            Assert.AreEqual(ERewardBatchApplyResult.AlreadyApplied, RunCardRules.ApplyRewardBatch(runState, batch));
            Assert.AreEqual(revision, runState.Revision.Value);
            Assert.IsFalse(runState.HasCard(14));
            Assert.IsFalse(runState.HasCard(20));
            Assert.IsFalse(runState.HasCard(30));
            Assert.IsFalse(runState.HasCard(35));
            Assert.IsTrue(runState.HasCard(54));
            Assert.AreEqual(result, runState.CardInstances[result.CardNumber]);
        }

        [Test]
        public void ApplyRewardBatch_RejectsDifferentPayloadForRecordedIdAfterFusion()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            var batch = CreateFusionBatch("fusion-batch");
            RunCardRules.ApplyRewardBatch(runState, batch);
            SelectFusionMaterials(runState, session, 14, 20, 30, 35);
            RunCardRules.TryFuse(runState, session, out var result);
            var revision = runState.Revision.Value;
            var fingerprint = runState.AppliedRewardBatchPayloadFingerprints["fusion-batch"];
            var altered = new PreparationRewardBatchStartupData(
                "fusion-batch",
                new[]
                {
                    new RewardCardGrantStartupData(14, 3, 3),
                    new RewardCardGrantStartupData(20, 3, 4),
                    new RewardCardGrantStartupData(30, 2, 3),
                    new RewardCardGrantStartupData(35, 4, 5),
                    new RewardCardGrantStartupData(54, 4, 2),
                });

            Assert.Throws<InvalidOperationException>(() => RunCardRules.ApplyRewardBatch(runState, altered));
            Assert.AreEqual(fingerprint, runState.AppliedRewardBatchPayloadFingerprints["fusion-batch"]);
            Assert.AreEqual(revision, runState.Revision.Value);
            Assert.AreEqual(result, runState.CardInstances[result.CardNumber]);
            Assert.IsTrue(runState.HasCard(54));
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, 0 }, runState.BattleSlotCardNumbers);
        }

        [Test]
        public void RunAndPreparationCollectionClearRewardLedgerAndFusionSelection()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            var batch = CreateFusionBatch("fusion-batch");
            RunCardRules.ApplyRewardBatch(runState, batch);
            session.Initialize(batch, true);
            SelectFusionMaterials(runState, session, 14, 20);

            session.CollectToPool();
            Assert.IsNull(session.BatchId);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, session.FusionSlotCardNumbers);

            runState.CollectToPool();
            Assert.IsEmpty(runState.AppliedRewardBatchPayloadFingerprints);
            Assert.AreEqual(0, runState.GetOwnedCardCount());
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, 0 }, runState.BattleSlotCardNumbers);
        }

        [Test]
        public void FusionSelection_ReplacesMovesRemovesAndRejectsInvalidMaterials()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            RunCardRules.ApplyRewardBatch(runState, CreateFusionBatch("fusion-batch"));

            Assert.AreEqual(EFusionOperationResult.Applied, RunCardRules.TrySetFusionMaterial(runState, session, 14, 0));
            Assert.AreEqual(EFusionOperationResult.DuplicateMaterial, RunCardRules.TrySetFusionMaterial(runState, session, 14, 1));
            Assert.AreEqual(EFusionOperationResult.Applied, RunCardRules.TrySetFusionMaterial(runState, session, 20, 1));
            Assert.AreEqual(EFusionOperationResult.Applied, RunCardRules.TrySetFusionMaterial(runState, session, 30, 1));
            CollectionAssert.AreEqual(new[] { 14, 30, 0, 0 }, session.FusionSlotCardNumbers);
            Assert.AreEqual(EFusionOperationResult.Applied, RunCardRules.TrySetFusionMaterial(runState, session, 30, 2, 1));
            CollectionAssert.AreEqual(new[] { 14, 0, 30, 0 }, session.FusionSlotCardNumbers);
            Assert.AreEqual(EFusionOperationResult.Applied, RunCardRules.TryRemoveFusionMaterial(session, 0));
            CollectionAssert.AreEqual(new[] { 0, 0, 30, 0 }, session.FusionSlotCardNumbers);
            Assert.AreEqual(EFusionOperationResult.UnownedCard, RunCardRules.TrySetFusionMaterial(runState, session, 2, 0));
            Assert.AreEqual(EFusionOperationResult.ResultCardCannotBeMaterial,
                RunCardRules.TrySetFusionMaterial(runState, session, RunCardRules.LockedCardNumber, 0));
            Assert.AreEqual(EFusionOperationResult.ResultCardCannotBeMaterial,
                RunCardRules.TrySetFusionMaterial(runState, session, RunCardRules.FirstFusionCardNumber, 0));
            Assert.AreEqual(EFusionOperationResult.InvalidSlot, RunCardRules.TrySetFusionMaterial(runState, session, 14, 4));
        }

        [Test]
        public void FusionSelection_InvalidBranchesDoNotMutateOrDirty()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            RunCardRules.ApplyRewardBatch(runState, CreateFusionBatch("fusion-batch"));
            SelectFusionMaterials(runState, session, 14, 20);

            AssertRejectedFusionSelection(
                runState,
                session,
                EFusionOperationResult.DuplicateMaterial,
                () => RunCardRules.TrySetFusionMaterial(runState, session, 14, 2));
            AssertRejectedFusionSelection(
                runState,
                session,
                EFusionOperationResult.ResultCardCannotBeMaterial,
                () => RunCardRules.TrySetFusionMaterial(
                    runState,
                    session,
                    RunCardRules.LockedCardNumber,
                    2));
            AssertRejectedFusionSelection(
                runState,
                session,
                EFusionOperationResult.UnownedCard,
                () => RunCardRules.TrySetFusionMaterial(runState, session, 2, 2));
            AssertRejectedFusionSelection(
                runState,
                session,
                EFusionOperationResult.InvalidSlot,
                () => RunCardRules.TrySetFusionMaterial(runState, session, 30, 4));
            AssertRejectedFusionSelection(
                runState,
                session,
                EFusionOperationResult.NoChange,
                () => RunCardRules.TrySetFusionMaterial(runState, session, 14, 0, 0));
            AssertRejectedFusionSelection(
                runState,
                session,
                EFusionOperationResult.NoChange,
                () => RunCardRules.TryRemoveFusionMaterial(session, 2));
            AssertRejectedFusionSelection(
                runState,
                session,
                EFusionOperationResult.InvalidSlot,
                () => RunCardRules.TrySetFusionMaterial(runState, session, 14, 2, -2));
            AssertRejectedFusionSelection(
                runState,
                session,
                EFusionOperationResult.InvalidSlot,
                () => RunCardRules.TrySetFusionMaterial(runState, session, 14, 2, 4));
            AssertRejectedFusionSelection(
                runState,
                session,
                EFusionOperationResult.InvalidSlot,
                () => RunCardRules.TrySetFusionMaterial(runState, session, 14, 2, 1));
        }

        [Test]
        public void FusionEvaluation_MalformedSessionsAreRejectedWithoutMutationOrDirty()
        {
            AssertMalformedFusionSessionIsRejected(
                new[] { 14, 14, 0, 0 },
                EFusionOperationResult.DuplicateMaterial);
            AssertMalformedFusionSessionIsRejected(
                new[] { 14, RunCardRules.LockedCardNumber, 0, 0 },
                EFusionOperationResult.ResultCardCannotBeMaterial);
            AssertMalformedFusionSessionIsRejected(
                new[] { 14, 2, 0, 0 },
                EFusionOperationResult.UnownedCard);
        }

        [Test]
        public void FusionEvaluationAndCommit_UsesAllFourMaterialsForLegendaryRecipeAndAllStats()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            RunCardRules.ApplyRewardBatch(runState, CreateFusionBatch("fusion-batch"));
            runState.BattleSlotCardNumbers[0] = 14;
            runState.BattleSlotCardNumbers[1] = 35;

            SelectFusionMaterials(runState, session, 14, 20, 30, 35);
            var evaluation = RunCardRules.EvaluateFusion(runState, session);
            Assert.AreEqual(RunCardRules.FusionTargetCardNumberSum, evaluation.CardNumberSum);
            Assert.AreEqual(4, evaluation.RecipeMaterialCount);
            Assert.AreEqual(184, evaluation.ResultCardNumber);
            Assert.AreEqual(131, evaluation.PresentationCardNumber);
            Assert.IsTrue(evaluation.CanFuse);
            Assert.AreEqual(EFusionOperationResult.Applied,
                RunCardRules.TryFuse(runState, session, out var result));
            Assert.AreEqual(184, result.CardNumber);
            Assert.AreEqual(131, result.PresentationCardNumber);
            Assert.AreEqual(EBattleCardTier.Legendary, result.Tier);
            Assert.AreEqual(11, result.Attack);
            Assert.AreEqual(15, result.MaxHealth);
            Assert.IsFalse(runState.HasCard(14));
            Assert.IsFalse(runState.HasCard(20));
            Assert.IsFalse(runState.HasCard(30));
            Assert.IsFalse(runState.HasCard(35));
            Assert.IsTrue(runState.HasCard(54));
            Assert.IsTrue(runState.HasCard(184));
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, 0 }, runState.BattleSlotCardNumbers);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, session.FusionSlotCardNumbers);

            var battleCard = new BattleCardRawComponent();
            battleCard.InitializePlayer(0, result);
            Assert.AreEqual(184, battleCard.CardNumber);
            Assert.AreEqual(184, battleCard.CardTypeId);
            Assert.AreEqual(131, battleCard.PresentationCardNumber);
            Assert.AreEqual(131, battleCard.PresentationCardTypeId);
            Assert.AreEqual(EBattleCardTier.Legendary, battleCard.Tier);
        }

        [Test]
        public void FourCardFusionPresentationUsesActualHighestThreeCardNumbers()
        {
            AssertFourCardPresentation(
                new[] { 2, 4, 8, 85 },
                177,
                127);
            AssertFourCardPresentation(
                new[] { 4, 7, 8, 80 },
                177,
                143);
        }

        [Test]
        public void FusionEvaluation_RequiresCardNumberSumExactlyNinetyNine()
        {
            var underState = new RunStateSingletonRawComponent();
            var underSession = new PreparationSessionSingletonRawComponent();
            underState.CardInstances[14] = new RunCardInstanceData(14, 1, 1);
            underState.CardInstances[20] = new RunCardInstanceData(20, 1, 1);
            SelectFusionMaterials(underState, underSession, 14, 20);

            var under = RunCardRules.EvaluateFusion(underState, underSession);
            Assert.AreEqual(34, under.CardNumberSum);
            Assert.AreEqual(EFusionOperationResult.CardNumberSumNotExact, under.BlockingResult);
            Assert.IsFalse(under.CanFuse);
            Assert.AreEqual(
                EFusionOperationResult.CardNumberSumNotExact,
                RunCardRules.TryFuse(underState, underSession, out _));

            var exactState = new RunStateSingletonRawComponent();
            var exactSession = new PreparationSessionSingletonRawComponent();
            exactState.CardInstances[44] = new RunCardInstanceData(44, 1, 1);
            exactState.CardInstances[55] = new RunCardInstanceData(55, 1, 1);
            SelectFusionMaterials(exactState, exactSession, 44, 55);

            var exact = RunCardRules.EvaluateFusion(exactState, exactSession);
            Assert.AreEqual(RunCardRules.FusionTargetCardNumberSum, exact.CardNumberSum);
            Assert.AreEqual(EFusionOperationResult.Applied, exact.BlockingResult);
            Assert.IsTrue(exact.CanFuse);

            var overState = new RunStateSingletonRawComponent();
            var overSession = new PreparationSessionSingletonRawComponent();
            overState.CardInstances[44] = new RunCardInstanceData(44, 1, 1);
            overState.CardInstances[56] = new RunCardInstanceData(56, 1, 1);
            SelectFusionMaterials(overState, overSession, 44, 56);

            var over = RunCardRules.EvaluateFusion(overState, overSession);
            Assert.AreEqual(100, over.CardNumberSum);
            Assert.AreEqual(EFusionOperationResult.CardNumberSumNotExact, over.BlockingResult);
            Assert.IsFalse(over.CanFuse);
            Assert.AreEqual(
                EFusionOperationResult.CardNumberSumNotExact,
                RunCardRules.TryFuse(overState, overSession, out _));
        }

        [Test]
        public void FusionRecommendations_ReturnEveryLegalExactCombinationContainingSelectedMaterials()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            foreach (var cardNumber in new[] { 14, 20, 30, 35, 44, 55 })
                runState.CardInstances[cardNumber] = new RunCardInstanceData(cardNumber, 1, 1);
            SelectFusionMaterials(runState, session, 14);
            var recommendations = new System.Collections.Generic.List<FusionRecommendationData>();

            Assert.AreEqual(
                2,
                RunCardRules.FindFusionRecommendations(runState, session, recommendations));
            CollectionAssert.AreEqual(
                new[] { 14, 30, 55 },
                Enumerable.Range(0, recommendations[0].MaterialCount)
                    .Select(recommendations[0].GetCardNumber)
                    .ToArray());
            CollectionAssert.AreEqual(
                new[] { 14, 20, 30, 35 },
                Enumerable.Range(0, recommendations[1].MaterialCount)
                    .Select(recommendations[1].GetCardNumber)
                    .ToArray());
            foreach (var recommendation in recommendations)
            {
                var cardNumbers = Enumerable.Range(0, recommendation.MaterialCount)
                    .Select(recommendation.GetCardNumber)
                    .ToArray();
                Assert.Contains(14, cardNumbers);
                Assert.AreEqual(RunCardRules.FusionTargetCardNumberSum, cardNumbers.Sum());
                Assert.AreEqual(cardNumbers.Length, cardNumbers.Distinct().Count());
                Assert.IsFalse(runState.HasCard(recommendation.ResultCardNumber));
            }
        }

        [Test]
        public void FusionRecommendations_WithEmptySelectionReturnEveryApplicableCombination()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            runState.CardInstances[44] = new RunCardInstanceData(44, 1, 1);
            runState.CardInstances[55] = new RunCardInstanceData(55, 1, 1);
            var recommendations = new System.Collections.Generic.List<FusionRecommendationData>
            {
                default,
            };

            Assert.AreEqual(1, RunCardRules.FindFusionRecommendations(runState, session, recommendations));
            CollectionAssert.AreEqual(
                new[] { 44, 55 },
                Enumerable.Range(0, recommendations[0].MaterialCount)
                    .Select(recommendations[0].GetCardNumber)
                    .ToArray());
            var resultCardNumber = recommendations[0].ResultCardNumber;
            runState.CardInstances[resultCardNumber] = new RunCardInstanceData(resultCardNumber, 1, 1);

            Assert.AreEqual(0, RunCardRules.FindFusionRecommendations(runState, session, recommendations));
            Assert.IsEmpty(recommendations);
        }

        [Test]
        public void ApplyingFusionRecommendationAtomicallyReplacesMaterialSlots()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            foreach (var cardNumber in new[] { 14, 30, 44, 55 })
                runState.CardInstances[cardNumber] = new RunCardInstanceData(cardNumber, 1, 1);
            SelectFusionMaterials(runState, session, 44);
            var recommendations = new System.Collections.Generic.List<FusionRecommendationData>();
            RunCardRules.FindFusionRecommendations(runState, session, recommendations);
            var recommendation = recommendations.Single(item =>
                item.MaterialCount == 2 &&
                item.GetCardNumber(0) == 44 &&
                item.GetCardNumber(1) == 55);
            var revision = session.FusionRevision.Value;

            Assert.AreEqual(
                EFusionOperationResult.Applied,
                RunCardRules.TryApplyFusionRecommendation(runState, session, recommendation));
            CollectionAssert.AreEqual(new[] { 44, 55, 0, 0 }, session.FusionSlotCardNumbers);
            Assert.AreEqual(revision + 1, session.FusionRevision.Value);
            Assert.IsTrue(RunCardRules.EvaluateFusion(runState, session).CanFuse);

            Assert.AreEqual(
                EFusionOperationResult.NoChange,
                RunCardRules.TryApplyFusionRecommendation(runState, session, recommendation));
            Assert.AreEqual(revision + 1, session.FusionRevision.Value);
        }

        [Test]
        public void FusionCommit_AcceptsTwoThreeAndFourMaterialsFromConfiguredRecipes()
        {
            var combinations = new[]
            {
                new[] { 44, 55 },
                new[] { 14, 30, 55 },
                new[] { 14, 20, 30, 35 },
            };

            foreach (var combination in combinations)
            {
                var runState = new RunStateSingletonRawComponent();
                var session = new PreparationSessionSingletonRawComponent();
                var expectedAttack = 0;
                var expectedHealth = 0;
                for (var index = 0; index < combination.Length; index++)
                {
                    var attack = index + 2;
                    var health = index + 3;
                    runState.CardInstances[combination[index]] =
                        new RunCardInstanceData(combination[index], attack, health);
                    expectedAttack += attack;
                    expectedHealth += health;
                }

                SelectFusionMaterials(runState, session, combination);
                Assert.IsTrue(RunCardRules.EvaluateFusion(runState, session).CanFuse);
                Assert.AreEqual(EFusionOperationResult.Applied,
                    RunCardRules.TryFuse(runState, session, out var result));
                Assert.AreEqual(expectedAttack, result.Attack);
                Assert.AreEqual(expectedHealth, result.MaxHealth);
                Assert.AreEqual(
                    RunCardRules.GetTierForFusionMaterialCount(combination.Length),
                    result.Tier);
                Assert.That(result.CardNumber, Is.InRange(
                    RunCardRules.FirstFusionCardNumber,
                    RunCardRules.LastFusionCardNumber));
                foreach (var cardNumber in combination)
                    Assert.IsFalse(runState.HasCard(cardNumber));
            }
        }

        [Test]
        public void FusionRecipes_AreOrderIndependentAndDoNotReserveTripleOgreResult()
        {
            var warriorOgre = BattleCardCsvData.GetFusionResult(1, 5, 0, 0, 2);
            var ogreWarrior = BattleCardCsvData.GetFusionResult(5, 1, 0, 0, 2);
            Assert.NotNull(warriorOgre);
            Assert.AreSame(warriorOgre, ogreWarrior);
            Assert.AreEqual(104, warriorOgre.CardNumber);
            Assert.IsNull(BattleCardCsvData.GetFusionResult(5, 5, 5, 0, 3));
            var boarOgreLegendary = BattleCardCsvData.GetFusionResult(4, 5, 4, 5, 4);
            Assert.NotNull(boarOgreLegendary);
            Assert.AreEqual(213, boarOgreLegendary.CardNumber);

            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            foreach (var cardNumber in new[] { 4, 44, 45, 49 })
                runState.CardInstances[cardNumber] = new RunCardInstanceData(cardNumber, 1, 1);
            SelectFusionMaterials(runState, session, 4, 44, 45, 49);

            var evaluation = RunCardRules.EvaluateFusion(runState, session);
            Assert.AreEqual(142, evaluation.CardNumberSum);
            Assert.AreEqual(4, evaluation.RecipeMaterialCount);
            Assert.AreEqual(0, evaluation.ResultCardNumber);
            Assert.AreEqual(EFusionOperationResult.CardNumberSumNotExact, evaluation.BlockingResult);
        }

        [Test]
        public void FusionCommit_InvalidAndOverflowBranchesDoNotMutateOrDirty()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            runState.CardInstances[44] = new RunCardInstanceData(44, int.MaxValue, 3);
            runState.CardInstances[55] = new RunCardInstanceData(55, 1, 4);
            SelectFusionMaterials(runState, session, 44, 55);
            var runRevision = runState.Revision.Value;
            var fusionRevision = session.FusionRevision.Value;

            Assert.AreEqual(EFusionOperationResult.StatOverflow,
                RunCardRules.TryFuse(runState, session, out _));
            Assert.AreEqual(runRevision, runState.Revision.Value);
            Assert.AreEqual(fusionRevision, session.FusionRevision.Value);
            Assert.IsTrue(runState.HasCard(44));
            Assert.IsTrue(runState.HasCard(55));
            CollectionAssert.AreEqual(new[] { 44, 55, 0, 0 }, session.FusionSlotCardNumbers);

            var resultCardNumber = RunCardRules.EvaluateFusion(runState, session).ResultCardNumber;
            runState.CardInstances[resultCardNumber] =
                new RunCardInstanceData(resultCardNumber, 11, 15);
            Assert.AreEqual(EFusionOperationResult.ResultAlreadyOwned,
                RunCardRules.TryFuse(runState, session, out _));
            Assert.AreEqual(runRevision, runState.Revision.Value);
            Assert.AreEqual(fusionRevision, session.FusionRevision.Value);
        }

        [Test]
        public void TryPlaceCard_ReplacesAndMovesWithoutDuplicates()
        {
            var runState = new RunStateSingletonRawComponent();
            runState.SetUnlockedBattleSlotCount(3);
            RunCardRules.ApplyRewardBatch(runState, CreateBatch("batch-a", 2, 3, 5, 6, 7));

            Assert.IsTrue(RunCardRules.TryPlaceCard(runState, 2, 0));
            Assert.IsTrue(RunCardRules.TryPlaceCard(runState, 3, 0));
            CollectionAssert.AreEqual(new[] { 3, 0, 0, 0, 0, 0 }, runState.BattleSlotCardNumbers);
            Assert.IsTrue(RunCardRules.TryPlaceCard(runState, 3, 2));
            CollectionAssert.AreEqual(new[] { 0, 0, 3, 0, 0, 0 }, runState.BattleSlotCardNumbers);
            Assert.IsFalse(RunCardRules.TryPlaceCard(runState, 4, 1));
            CollectionAssert.AreEqual(new[] { 0, 0, 3, 0, 0, 0 }, runState.BattleSlotCardNumbers);
        }

        [Test]
        public void TryRemoveCardFromBattleSlot_ReturnsOnlyTheExpectedCardToPool()
        {
            var runState = new RunStateSingletonRawComponent();
            RunCardRules.ApplyRewardBatch(runState, CreateBatch("remove-battle-card", 2, 3, 5));
            Assert.IsTrue(RunCardRules.TryPlaceCard(runState, 2, 0));
            var revision = runState.Revision.Value;

            Assert.IsFalse(RunCardRules.TryRemoveCardFromBattleSlot(runState, 0, 3));
            Assert.AreEqual(revision, runState.Revision.Value);
            Assert.AreEqual(2, runState.BattleSlotCardNumbers[0]);

            Assert.IsTrue(RunCardRules.TryRemoveCardFromBattleSlot(runState, 0, 2));
            Assert.AreEqual(revision + 1, runState.Revision.Value);
            Assert.AreEqual(0, runState.BattleSlotCardNumbers[0]);
            Assert.IsTrue(runState.HasCard(2));
            Assert.IsFalse(RunCardRules.TryRemoveCardFromBattleSlot(runState, 0, 2));
        }

        [Test]
        public void RewardBatch_AllowsConfiguredGrantCountAndDuplicateNumbers()
        {
            var shortBatch = new PreparationRewardBatchStartupData("short", new[] { Grant(2) });
            Assert.AreEqual(1, shortBatch.Grants.Count);
            var duplicateBatch = new PreparationRewardBatchStartupData(
                "duplicate", new[] { Grant(2), Grant(2), Grant(3), Grant(5), Grant(6) });
            var runState = new RunStateSingletonRawComponent();
            Assert.AreEqual(ERewardBatchApplyResult.Applied, RunCardRules.ApplyRewardBatch(runState, duplicateBatch));
            Assert.AreEqual(5, runState.GetOwnedCardCount());
            Assert.AreEqual(2, runState.GetCardCopyCount(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RewardCardGrantStartupData(RunCardRules.LockedCardNumber, 1, 1));
        }

        [Test]
        public void BattleProgressionConfigurationDefinesRoundDrawAndSlotUnlocks()
        {
            DataApi.ReleaseAllData<BattleProgressionCsvData>(false);
            try
            {
                CsvApi.ReadFromString<BattleProgressionCsvData>(
                    nameof(BattleProgressionCsvData),
                    "BattleNumber,UnlockSlotCount,DrawCardCount\n" +
                    "1,2,3");
                var progression = DataApi.GetData<BattleProgressionCsvData>(1);
                Assert.NotNull(progression);
                Assert.AreEqual(2, progression.UnlockSlotCount);
                Assert.AreEqual(3, progression.DrawCardCount);
                Assert.AreEqual(2, BattleProgressionCsvData.GetUnlockedSlotTotal(1));
            }
            finally
            {
                DataApi.ReleaseAllData<BattleProgressionCsvData>(false);
            }
        }

        [Test]
        public void FusionConsumesOneDuplicateCopyAndPromotesTheNextCopy()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            var batch = new PreparationRewardBatchStartupData(
                "duplicate-fusion",
                new[]
                {
                    new RewardCardGrantStartupData(44, 1, 2),
                    new RewardCardGrantStartupData(44, 9, 10),
                    new RewardCardGrantStartupData(55, 3, 4),
                    Grant(2),
                    Grant(3),
                });
            RunCardRules.ApplyRewardBatch(runState, batch);
            SelectFusionMaterials(runState, session, 44, 55);

            Assert.AreEqual(EFusionOperationResult.Applied, RunCardRules.TryFuse(runState, session, out _));
            Assert.AreEqual(1, runState.GetCardCopyCount(44));
            Assert.AreEqual(9, runState.GetCardInstance(44).Attack);
            Assert.AreEqual(10, runState.GetCardInstance(44).MaxHealth);
            Assert.AreEqual(0, runState.GetCardCopyCount(55));
            Assert.AreEqual(4, runState.GetOwnedCardCount());
        }

        [Test]
        public void StageGroupCoordinator_MergesOnAwakeAndInitialEntryDuplicateWhileLoading()
        {
            var coordinator = new HearthstoneStageGroupTransitionCoordinator();
            Assert.IsTrue(coordinator.Request(EHearthstoneStageGroup.Battle, "initial"));
            Assert.IsTrue(coordinator.TryBeginTransition(out var group, out var key));
            Assert.AreEqual(EHearthstoneStageGroup.Battle, group);
            Assert.AreEqual("initial", key);
            Assert.AreEqual(EStageGroupTransitionPhase.Loading, coordinator.Phase);

            Assert.IsFalse(coordinator.Request(EHearthstoneStageGroup.Battle, "initial"));
            Assert.IsFalse(coordinator.TryBeginTransition(out _, out _));
            coordinator.CompleteTransition(group, key);
            Assert.AreEqual(EStageGroupTransitionPhase.Active, coordinator.Phase);
        }

        [Test]
        public void StageGroupCoordinator_SerializesLatestConflictingRequest()
        {
            var coordinator = new HearthstoneStageGroupTransitionCoordinator();
            coordinator.Request(EHearthstoneStageGroup.Battle, "default-battle");
            Assert.IsTrue(coordinator.TryBeginTransition(out var firstGroup, out var firstKey));

            Assert.IsTrue(coordinator.Request(EHearthstoneStageGroup.Preparation, "reward-a"));
            Assert.IsFalse(coordinator.Request(EHearthstoneStageGroup.Preparation, "reward-a"));
            Assert.IsFalse(coordinator.TryBeginTransition(out _, out _));

            coordinator.CompleteTransition(firstGroup, firstKey);
            Assert.AreEqual(EStageGroupTransitionPhase.Requested, coordinator.Phase);
            Assert.IsTrue(coordinator.TryBeginTransition(out var secondGroup, out var secondKey));
            Assert.AreEqual(EHearthstoneStageGroup.Preparation, secondGroup);
            Assert.AreEqual("reward-a", secondKey);
            coordinator.CompleteTransition(secondGroup, secondKey);
            Assert.AreEqual(EHearthstoneStageGroup.Preparation, coordinator.ActiveGroup);
            Assert.AreEqual(EStageGroupTransitionPhase.Active, coordinator.Phase);
        }

        [Test]
        public void UiInteractor_ClearsTouchingOnMissAndDragEnd()
        {
            var requesterObject = new GameObject("Requester");
            var responderObject = new GameObject("Responder");
            try
            {
                var requester = requesterObject.AddComponent<UiInteractor>();
                var responder = responderObject.AddComponent<UiInteractor>();
                var touchingField = typeof(UiInteractor).GetField("m_Touching", BindingFlags.Instance | BindingFlags.NonPublic);
                var setTouching = typeof(UiInteractor).GetMethod("SetTouching", BindingFlags.Instance | BindingFlags.NonPublic);
                var onDrag = typeof(UiInteractor).GetMethod("OnDrag", BindingFlags.Instance | BindingFlags.NonPublic);
                var onDragEnd = typeof(UiInteractor).GetMethod("OnDragEnd", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(touchingField);
                Assert.NotNull(setTouching);
                Assert.NotNull(onDrag);
                Assert.NotNull(onDragEnd);

                var touchCount = 0;
                var touchEndCount = 0;
                responder.OnInteractorTouch += ignored => touchCount++;
                responder.OnInteractorTouchEnd += ignored => touchEndCount++;
                var eventData = new PointerEventData(null);

                setTouching.Invoke(requester, new object[] { responder });
                Assert.AreEqual(1, touchCount);
                onDrag.Invoke(requester, new object[] { eventData });
                Assert.AreEqual(1, touchEndCount);
                Assert.IsNull(touchingField.GetValue(requester));

                setTouching.Invoke(requester, new object[] { responder });
                Assert.AreEqual(2, touchCount);
                onDragEnd.Invoke(requester, new object[] { eventData });
                Assert.AreEqual(2, touchEndCount);
                Assert.IsNull(touchingField.GetValue(requester));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(responderObject);
                UnityEngine.Object.DestroyImmediate(requesterObject);
            }
        }

        [Test]
        public void PreparationPrefabsUseChineseFontAsset()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Resources/Fonts/NotoSansSC-SemiBold Dynamic SDF.asset");
            Assert.NotNull(font);
            const string requiredCharacters =
                "备战阶段卡槽位池哥布林战士弓手投弹野猪食人魔融合造物出战素材合计继续智能推荐无可用组合选择";
            Assert.IsTrue(font.HasCharacters(requiredCharacters));

            var prefabPaths = new[]
            {
                "Assets/Resources/Ui/PreparationView.prefab",
                "Assets/Resources/Ui/BattleCardItem.prefab",
                "Assets/Resources/Ui/FusionRecommendationItem.prefab",
            };
            foreach (var path in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.NotNull(prefab, path);
                var labels = prefab.GetComponentsInChildren<TMP_Text>(true);
                Assert.IsNotEmpty(labels, path);
                foreach (var label in labels)
                    Assert.AreSame(font, label.font, $"{path}/{label.name}");
            }
        }

        [Test]
        public void PreparationSharedCardAndResourcesAreFullyExported()
        {
            ResourceApi.Initialize();
            var pagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationView.prefab");
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/BattleCardItem.prefab");
            Assert.NotNull(pagePrefab);
            Assert.NotNull(cardPrefab);

            var page = pagePrefab.GetComponent<PreparationView>();
            Assert.NotNull(page.BattleTabButton);
            Assert.NotNull(page.FusionTabButton);
            Assert.NotNull(page.BattleOperationRoot);
            Assert.NotNull(page.FusionOperationRoot);
            Assert.NotNull(page.BattleSlotList);
            Assert.NotNull(page.FusionSlotList);
            Assert.NotNull(page.FusionCurrentPointLabel);
            Assert.NotNull(page.FusionCurrentPointValue);
            Assert.NotNull(page.FusionRemainingPointLabel);
            Assert.NotNull(page.FusionRemainingPointValue);
            Assert.NotNull(page.FusionButton);
            Assert.NotNull(page.FusionButtonAttemptListener);
            Assert.NotNull(page.FusionRecommendationButton);
            Assert.NotNull(page.FusionRecommendationHoverListener);
            Assert.NotNull(page.FusionRecommendationTooltip);
            Assert.NotNull(page.FusionAreaInteractor);
            Assert.NotNull(page.FusionRecommendationOverlay);
            Assert.NotNull(page.FusionRecommendationCloseButton);
            Assert.NotNull(page.FusionRecommendationScrollRect);
            Assert.NotNull(page.FusionRecommendationList);
            Assert.NotNull(page.FusionRecommendationEmptyText);
            Assert.IsFalse(page.FusionRecommendationOverlay.activeSelf);
            var serializedPage = new SerializedObject(page);
            var serializedUiItems = serializedPage.FindProperty("BbxUiItems");
            Assert.NotNull(serializedUiItems);
            Assert.IsTrue(
                Enumerable.Range(0, serializedUiItems.arraySize).Any(index =>
                    serializedUiItems.GetArrayElementAtIndex(index).objectReferenceValue ==
                    page.FusionRecommendationList),
                "Inactive recommendation UiList must participate in the page UI lifecycle.");
            Assert.AreSame(
                page.FusionRecommendationList.transform,
                page.FusionRecommendationScrollRect.content);
            Assert.AreEqual(
                "智能推荐",
                page.FusionRecommendationButton.transform.Find("Label").GetComponent<TMP_Text>().text);
            Assert.AreSame(
                page.FusionRecommendationButton.transform,
                page.FusionRecommendationTooltip.transform.parent);
            Assert.AreSame(
                page.FusionRecommendationButton.gameObject,
                page.FusionRecommendationHoverListener.gameObject);
            Assert.IsFalse(page.FusionRecommendationTooltip.activeSelf);
            var recommendationTooltipText = page.FusionRecommendationTooltip.transform
                .Find("Text")?.GetComponent<TMP_Text>();
            var recommendationTooltipBackground = page.FusionRecommendationTooltip.GetComponent<Image>();
            Assert.NotNull(recommendationTooltipText);
            Assert.NotNull(recommendationTooltipBackground);
            Assert.AreEqual("智能寻找牌库中可以融合的组合", recommendationTooltipText.text);
            Assert.AreEqual(TextAlignmentOptions.MidlineLeft, recommendationTooltipText.alignment);
            Assert.IsFalse(recommendationTooltipText.raycastTarget);
            Assert.IsFalse(recommendationTooltipBackground.raycastTarget);
            Assert.AreEqual(new Vector2(460f, 94f),
                ((RectTransform)page.FusionRecommendationTooltip.transform).sizeDelta);
            Assert.AreEqual(new Vector2(354f, 0f),
                ((RectTransform)page.FusionRecommendationTooltip.transform).anchoredPosition);
            var battlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/BattleView.prefab");
            Assert.NotNull(battlePrefab);
            var recommendationPanel = page.FusionRecommendationOverlay.transform.Find("Panel");
            var battleBackground = battlePrefab.transform.Find("BoardBackground")?.GetComponent<Image>();
            var recommendationBackground = recommendationPanel?.GetComponent<Image>();
            var recommendationAging = recommendationPanel?.Find("ParchmentAgingOverlay")?.GetComponent<Image>();
            var battleAging = battlePrefab.transform.Find("ParchmentAgingOverlay")?.GetComponent<Image>();
            var recommendationScroll = recommendationPanel?.Find("ScrollRect")?.GetComponent<Image>();
            var recommendationViewport = recommendationPanel?.Find("ScrollRect/Viewport");
            Assert.NotNull(recommendationPanel);
            Assert.NotNull(battleBackground);
            Assert.NotNull(recommendationBackground);
            Assert.NotNull(recommendationAging);
            Assert.NotNull(battleAging);
            Assert.NotNull(recommendationScroll);
            Assert.NotNull(recommendationViewport);
            Assert.IsNull(recommendationPanel.Find("Title"));
            Assert.IsNull(recommendationPanel.Find("Hint"));
            Assert.AreSame(battleBackground.sprite, recommendationBackground.sprite);
            Assert.AreSame(battleAging.sprite, recommendationAging.sprite);
            Assert.AreEqual(0.14f, recommendationAging.color.a, 0.001f);
            Assert.IsFalse(recommendationAging.raycastTarget);
            Assert.AreEqual(0f, recommendationScroll.color.a, 0.001f);
            Assert.IsNull(recommendationViewport.GetComponent<Image>());
            Assert.AreEqual("无可用组合", page.FusionRecommendationEmptyText.text);
            Assert.AreEqual(TextAlignmentOptions.Center, page.FusionRecommendationEmptyText.alignment);
            Assert.AreEqual(
                UiList.EArrangement.Manual,
                page.FusionRecommendationList.ArragementType);
            var recommendationItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/FusionRecommendationItem.prefab");
            Assert.NotNull(recommendationItemPrefab);
            var recommendationItem = recommendationItemPrefab.GetComponent<FusionRecommendationItemView>();
            Assert.NotNull(recommendationItem);
            Assert.NotNull(recommendationItem.CardList);
            Assert.NotNull(recommendationItem.SelectButton);
            Assert.IsNull(recommendationItemPrefab.transform.Find("ResultCard"));
            Assert.IsNull(recommendationItemPrefab.transform.Find("Result"));
            Assert.AreEqual(
                "选择",
                recommendationItem.SelectButton.transform.Find("Label").GetComponent<TMP_Text>().text);
            var currentPointPanel = page.FusionOperationRoot.transform.Find(
                "FusionSumPanel/CurrentPointPanel") as RectTransform;
            var remainingPointPanel = page.FusionOperationRoot.transform.Find(
                "FusionSumPanel/RemainingPointPanel") as RectTransform;
            var recommendationButtonRect = page.FusionRecommendationButton.transform as RectTransform;
            var fusionButtonRect = page.FusionButton.transform as RectTransform;
            Assert.NotNull(currentPointPanel);
            Assert.NotNull(remainingPointPanel);
            Assert.NotNull(recommendationButtonRect);
            Assert.NotNull(fusionButtonRect);
            Assert.AreEqual(new Vector2(280f, 72f), currentPointPanel.sizeDelta);
            Assert.AreEqual(new Vector2(280f, 72f), remainingPointPanel.sizeDelta);
            Assert.AreEqual(new Vector2(216f, 68f), recommendationButtonRect.sizeDelta);
            Assert.AreEqual(new Vector2(300f, 82f), fusionButtonRect.sizeDelta);
            Assert.AreEqual(currentPointPanel.anchoredPosition.x, remainingPointPanel.anchoredPosition.x);
            Assert.AreEqual(currentPointPanel.anchoredPosition.x, recommendationButtonRect.anchoredPosition.x);
            Assert.Greater(currentPointPanel.anchoredPosition.y, remainingPointPanel.anchoredPosition.y);
            Assert.Greater(remainingPointPanel.anchoredPosition.y, recommendationButtonRect.anchoredPosition.y);
            Assert.Greater(fusionButtonRect.anchoredPosition.x, currentPointPanel.anchoredPosition.x);
            Assert.Greater(fusionButtonRect.sizeDelta.x, currentPointPanel.sizeDelta.x);
            Assert.NotNull(currentPointPanel.GetComponent<Image>().sprite);
            Assert.AreEqual(
                "PreparationFusionSumPanel",
                currentPointPanel.GetComponent<Image>().sprite.name);
            Assert.AreSame(
                currentPointPanel.GetComponent<Image>().sprite,
                remainingPointPanel.GetComponent<Image>().sprite);
            Assert.IsFalse(currentPointPanel.GetComponent<Image>().preserveAspect);
            Assert.IsFalse(remainingPointPanel.GetComponent<Image>().preserveAspect);
            Assert.AreEqual("当前点数", page.FusionCurrentPointLabel.text);
            Assert.AreEqual("0", page.FusionCurrentPointValue.text);
            Assert.AreEqual("剩余点数", page.FusionRemainingPointLabel.text);
            Assert.AreEqual("99", page.FusionRemainingPointValue.text);
            Assert.AreEqual(TextAlignmentOptions.MidlineLeft, page.FusionCurrentPointLabel.alignment);
            Assert.AreEqual(TextAlignmentOptions.MidlineRight, page.FusionCurrentPointValue.alignment);
            Assert.AreEqual(TextAlignmentOptions.MidlineLeft, page.FusionRemainingPointLabel.alignment);
            Assert.AreEqual(TextAlignmentOptions.MidlineRight, page.FusionRemainingPointValue.alignment);
            Assert.AreEqual(Color.black, page.FusionCurrentPointLabel.color);
            Assert.AreEqual(Color.black, page.FusionCurrentPointValue.color);
            Assert.AreEqual(Color.black, page.FusionRemainingPointLabel.color);
            Assert.AreEqual(Color.black, page.FusionRemainingPointValue.color);
            var currentLabelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                currentPointPanel,
                page.FusionCurrentPointLabel.rectTransform);
            var currentValueBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                currentPointPanel,
                page.FusionCurrentPointValue.rectTransform);
            var remainingLabelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                remainingPointPanel,
                page.FusionRemainingPointLabel.rectTransform);
            var remainingValueBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                remainingPointPanel,
                page.FusionRemainingPointValue.rectTransform);
            Assert.AreEqual(-90f, currentLabelBounds.min.x, 0.001f);
            Assert.AreEqual(10f, currentLabelBounds.max.x, 0.001f);
            Assert.AreEqual(10f, currentValueBounds.min.x, 0.001f);
            Assert.AreEqual(80f, currentValueBounds.max.x, 0.001f);
            Assert.AreEqual(currentLabelBounds, remainingLabelBounds);
            Assert.AreEqual(currentValueBounds, remainingValueBounds);
            Assert.Greater(currentLabelBounds.min.x - currentPointPanel.rect.xMin, 40f);
            Assert.Greater(currentPointPanel.rect.xMax - currentValueBounds.max.x, 40f);
            var labelPreferredWidth = page.FusionCurrentPointLabel.GetPreferredValues("当前点数").x;
            var widestValuePreferredWidth = page.FusionCurrentPointValue.GetPreferredValues("-293").x;
            var renderedLabelRight = currentLabelBounds.min.x + labelPreferredWidth;
            var renderedValueLeft = currentValueBounds.max.x - widestValuePreferredWidth;
            Assert.Greater(renderedValueLeft - renderedLabelRight, 10f);
            Assert.Less(renderedValueLeft - renderedLabelRight, 20f);
            Assert.NotNull(page.RewardRevealOverlay);
            Assert.NotNull(page.RewardRevealCanvasGroup);
            Assert.NotNull(page.RewardRevealConfirmButton);
            Assert.NotNull(page.RewardRevealCardList);
            Assert.IsFalse(page.RewardRevealOverlay.activeSelf);
            Assert.IsTrue(page.RewardRevealCanvasGroup.blocksRaycasts);
            Assert.IsFalse(page.RewardRevealCanvasGroup.interactable);
            Assert.IsFalse(page.RewardRevealConfirmButton.interactable);
            Assert.AreEqual(Navigation.Mode.None, page.RewardRevealConfirmButton.navigation.mode);
            Assert.AreSame(
                page.RewardRevealOverlay.GetComponent<Image>(),
                page.RewardRevealConfirmButton.targetGraphic);
            Assert.AreSame(
                page.RewardRevealOverlay.transform,
                page.RewardRevealCardList.transform.parent);
            Assert.AreEqual(UiList.EArrangement.Manual, page.RewardRevealCardList.ArragementType);
            var rewardTitle = page.RewardRevealOverlay.transform.Find("RewardTitle");
            Assert.NotNull(rewardTitle);
            var rewardTitleImage = rewardTitle.GetComponent<Image>();
            Assert.NotNull(rewardTitleImage);
            Assert.NotNull(rewardTitleImage.sprite);
            Assert.AreEqual("PreparationRewardTitle", rewardTitleImage.sprite.name);
            Assert.IsTrue(rewardTitleImage.preserveAspect);
            Assert.IsFalse(rewardTitleImage.raycastTarget);
            Assert.AreEqual(new Vector2(620f, 225f), ((RectTransform)rewardTitle).sizeDelta);
            Assert.AreEqual(new Vector2(0f, 270f), ((RectTransform)rewardTitle).anchoredPosition);
            var rewardTitleTexture = rewardTitleImage.sprite.texture;
            Assert.GreaterOrEqual(rewardTitleTexture.width, 2000);
            Assert.GreaterOrEqual(rewardTitleTexture.height, 700);
            var rewardTitleImporter = AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(rewardTitleTexture)) as TextureImporter;
            Assert.NotNull(rewardTitleImporter);
            Assert.IsTrue(rewardTitleImporter.DoesSourceTextureHaveAlpha());
            Assert.IsTrue(rewardTitleImporter.alphaIsTransparency);
            Assert.IsFalse(rewardTitleImporter.mipmapEnabled);
            Assert.AreEqual(TextureWrapMode.Clamp, rewardTitleImporter.wrapMode);
            Assert.IsTrue(
                Enumerable.Range(0, serializedUiItems.arraySize).Any(index =>
                    serializedUiItems.GetArrayElementAtIndex(index).objectReferenceValue ==
                    page.RewardRevealCardList),
                "Inactive reward reveal UiList must participate in the page UI lifecycle.");
            Assert.NotNull(page.FusionRevealOverlay);
            Assert.NotNull(page.FusionRevealCanvasGroup);
            Assert.NotNull(page.FusionRevealDismissButton);
            Assert.NotNull(page.FusionRevealMaterialCardList);
            Assert.NotNull(page.FusionRevealCardRoot);
            Assert.NotNull(page.FusionRevealCardList);
            Assert.NotNull(page.FusionRevealSealedFace);
            Assert.NotNull(page.FusionRevealCardBack);
            Assert.NotNull(page.FusionRevealFlash);
            Assert.NotNull(page.FusionRevealFlashCanvasGroup);
            Assert.IsTrue(page.FusionRevealCanvasGroup.blocksRaycasts);
            Assert.IsFalse(page.FusionRevealCanvasGroup.interactable);
            Assert.IsFalse(page.FusionRevealDismissButton.interactable);
            Assert.AreEqual(Navigation.Mode.None, page.FusionRevealDismissButton.navigation.mode);
            Assert.AreSame(
                page.FusionRevealOverlay.GetComponent<Image>(),
                page.FusionRevealDismissButton.targetGraphic);
            Assert.AreSame(
                page.FusionRevealOverlay.transform,
                page.FusionRevealMaterialCardList.transform.parent);
            Assert.AreEqual(UiList.EArrangement.Manual, page.FusionRevealMaterialCardList.ArragementType);
            Assert.AreEqual(UiList.EArrangement.Manual, page.FusionRevealCardList.ArragementType);
            Assert.AreEqual(180f, page.FusionRevealCardBack.transform.localEulerAngles.y, 0.01f);
            Assert.AreSame(page.FusionRevealOverlay.transform, page.FusionRevealFlash.parent);
            Assert.AreEqual(Vector2.zero, page.FusionRevealFlash.offsetMin);
            Assert.AreEqual(Vector2.zero, page.FusionRevealFlash.offsetMax);
            Assert.IsFalse(page.FusionRevealFlash.GetComponent<Image>().raycastTarget);
            foreach (var revealText in page.FusionRevealOverlay.GetComponentsInChildren<TMP_Text>(true))
                StringAssert.DoesNotContain("按任意键继续", revealText.text);
            Assert.AreEqual(
                pagePrefab.transform.childCount - 1,
                page.FusionRevealOverlay.transform.GetSiblingIndex());
            Assert.NotNull(page.CardPoolInteractor);
            Assert.NotNull(page.OwnedOnlyToggle);
            Assert.NotNull(page.OwnedOnlyLabel);
            Assert.IsTrue(page.OwnedOnlyToggle.isOn);
            Assert.AreEqual("查看拥有", page.OwnedOnlyLabel.text);
            Assert.AreSame(
                page.OwnedOnlyToggle.transform,
                pagePrefab.transform.Find("CardPoolPanel/OwnedOnlyToggle"));
            Assert.IsNull(pagePrefab.transform.Find("RewardPanel"));
            Assert.IsNull(pagePrefab.transform.Find("CardPoolPanel/PoolTitle"));
            Assert.IsNull(pagePrefab.transform.Find("ContinueButton/AuxiliaryLabel"));
            Assert.AreEqual("继续", page.ContinueMainText.text);
            Assert.AreEqual(Navigation.Mode.None, page.ContinueButton.navigation.mode);
            Assert.AreEqual(Selectable.Transition.ColorTint, page.ContinueButton.transition);
            Assert.AreEqual("MedievalParchmentControl", page.ContinueButtonImage.sprite.name);
            Assert.AreEqual("MedievalParchmentControl", page.BattleTabImage.sprite.name);
            Assert.AreEqual("MedievalParchmentControl", page.FusionTabImage.sprite.name);
            Assert.AreEqual(Selectable.Transition.ColorTint, page.FusionButton.transition);
            Assert.AreEqual(
                "MedievalParchmentControl",
                ((Image)page.FusionButton.targetGraphic).sprite.name);
            Assert.AreEqual(Selectable.Transition.ColorTint, page.FusionRecommendationButton.transition);
            Assert.AreEqual(
                "MedievalParchmentControl",
                ((Image)page.FusionRecommendationButton.targetGraphic).sprite.name);

            var titleFrame = pagePrefab.transform.Find("TitleFrame");
            var titleText = titleFrame.Find("Title").GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(new Vector2(580f, 110f), ((RectTransform)titleFrame).sizeDelta);
            Assert.AreEqual(46f, titleText.fontSize);
            Assert.AreEqual(TextAlignmentOptions.Center, titleText.alignment);
            Assert.AreEqual(new Vector2(0f, 2f), ((RectTransform)titleText.transform).anchoredPosition);

            var battleTabText = page.BattleTabButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            var fusionTabText = page.FusionTabButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(31f, battleTabText.fontSize);
            Assert.AreEqual(31f, fusionTabText.fontSize);
            Assert.AreEqual(TextAlignmentOptions.Center, battleTabText.alignment);
            Assert.AreEqual(TextAlignmentOptions.Center, fusionTabText.alignment);
            Assert.AreEqual(new Vector2(0f, 4f), ((RectTransform)battleTabText.transform).anchoredPosition);
            Assert.AreEqual(new Vector2(0f, 4f), ((RectTransform)fusionTabText.transform).anchoredPosition);

            Assert.AreEqual(TextAlignmentOptions.Center, page.ContinueMainText.alignment);
            Assert.AreEqual(
                new Vector2(0f, 3f),
                ((RectTransform)page.ContinueMainText.transform).anchoredPosition);

            var battleTabRect = (RectTransform)page.BattleTabButton.transform;
            var fusionTabRect = (RectTransform)page.FusionTabButton.transform;
            Assert.AreEqual(new Vector2(0f, 1f), battleTabRect.anchorMin);
            Assert.AreEqual(new Vector2(0f, 1f), fusionTabRect.anchorMin);
            Assert.That(battleTabRect.anchoredPosition.x, Is.LessThan(fusionTabRect.anchoredPosition.x));
            Assert.AreEqual(-58f, battleTabRect.anchoredPosition.y);
            Assert.AreEqual(-58f, fusionTabRect.anchoredPosition.y);
            Assert.AreEqual(-130f, ((RectTransform)page.BattleSlotList.transform).anchoredPosition.y);

            var fusionMaterialTitleRect = page.FusionOperationRoot.transform.Find("Title") as RectTransform;
            var fusionSlotListRect = (RectTransform)page.FusionSlotList.transform;
            Assert.NotNull(fusionMaterialTitleRect);
            Assert.AreEqual(new Vector2(425f, -40f), fusionMaterialTitleRect.anchoredPosition);
            Assert.AreEqual(new Vector2(420f, -150f), fusionSlotListRect.anchoredPosition);

            var poolPanelRect = (RectTransform)page.CardPoolInteractor.transform;
            Assert.AreEqual(new Vector2(1780f, 630f), poolPanelRect.sizeDelta);
            Assert.AreEqual(new Vector2(1650f, 510f), ((RectTransform)page.CardPoolScrollRect.transform).sizeDelta);
            Assert.AreEqual(new Vector2(0f, -10f), ((RectTransform)page.CardPoolScrollRect.transform).anchoredPosition);
            var ownedOnlyRect = (RectTransform)page.OwnedOnlyToggle.transform;
            Assert.AreEqual(new Vector2(240f, 42f), ownedOnlyRect.sizeDelta);
            Assert.AreEqual(new Vector2(-770f, 285f), ownedOnlyRect.anchoredPosition);
            Assert.AreEqual(
                "PreparationTabSelected",
                page.OwnedOnlyToggle.transform.Find("Box").GetComponent<Image>().sprite.name);
            Assert.AreEqual(
                "PreparationTabIdle",
                page.OwnedOnlyToggle.targetGraphic.GetComponent<Image>().sprite.name);
            var filterFrameTexture = page.OwnedOnlyToggle.targetGraphic.GetComponent<Image>().sprite.texture;
            Assert.AreEqual(1536, filterFrameTexture.width);
            Assert.AreEqual(270, filterFrameTexture.height);
            var filterFrameImporter = AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(filterFrameTexture)) as TextureImporter;
            Assert.NotNull(filterFrameImporter);
            Assert.IsTrue(filterFrameImporter.DoesSourceTextureHaveAlpha());

            var card = cardPrefab.GetComponent<BattleCardItemView>();
            Assert.NotNull(card.PreparationMaterialSelectedState);
            Assert.NotNull(card.PreparationEmptyAttemptListener);
            Assert.NotNull(card.PreparationBattleSlotEmptyState);
            Assert.NotNull(card.PreparationFusionSlotEmptyState);
            Assert.NotNull(card.PreparationDropHighlight);

            var mappings = UiApi.CapturePreloadedUiPrefabPathsForValidation();
            Assert.AreEqual("Ui/BattleCardItem",
                mappings[typeof(BattleCardItemController).FullName]);
            Assert.AreEqual("Ui/FusionRecommendationItem",
                mappings[typeof(FusionRecommendationItemController).FullName]);
            Assert.NotNull(Resources.Load<UiSceneAsset>("Ui/Preparation"));
            Assert.AreEqual(31, RunCardRules.CardRowCount);
            Assert.AreEqual(213, RunCardRules.LastCardNumber);

            var spriteKeys = new[]
            {
                "PreparationTabIdle", "PreparationTabSelected",
                "PreparationFusionSlotFrame",
                "PreparationFusionSumPanel",
                "PreparationMaterialSelected", "PreparationRewardTitle", "FusionCard_099",
                "MedievalParchmentControl",
            };
            foreach (var key in spriteKeys)
                Assert.NotNull(ResourceApi.LoadSprite(key), key);
        }

        [Test]
        public void PreparationOwnedFilterRebuildsSharedPoolInNumberOrder()
        {
            var controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs");
            Assert.NotNull(controllerScript);
            StringAssert.Contains(
                "m_View.OwnedOnlyToggle.onValueChanged.AddListener(OnOwnedOnlyChanged)",
                controllerScript.text);
            StringAssert.Contains(
                "for (var cardNumber = RunCardRules.FirstCardNumber;",
                controllerScript.text);
            StringAssert.Contains(
                "m_ShowOwnedOnly && copyCount == 0",
                controllerScript.text);
            StringAssert.Contains("var copyCount = m_RunState.GetCardCopyCount(cardNumber)", controllerScript.text);
            StringAssert.Contains(
                "for (var copyIndex = 0; copyIndex < visibleCopyCount; copyIndex++)",
                controllerScript.text);
            StringAssert.Contains(
                "item.BindPreparation(this, cardNumber, displayNumber, copyIndex)",
                controllerScript.text);
            StringAssert.Contains("nextLegendaryDisplayNumber++", controllerScript.text);
            StringAssert.Contains("m_ShowOwnedOnly = true", controllerScript.text);
            StringAssert.Contains("SetIsOnWithoutNotify(true)", controllerScript.text);
            StringAssert.Contains("verticalNormalizedPosition = 1f", controllerScript.text);
            StringAssert.Contains("HasOwnedCardCountChanged()", controllerScript.text);
            var resizeIndex = controllerScript.text.IndexOf("poolContent.SetSizeWithCurrentAnchors", StringComparison.Ordinal);
            Assert.GreaterOrEqual(resizeIndex, 0);
            var relayoutIndex = controllerScript.text.IndexOf(
                "m_View.CardPoolList.RefreshLayout();",
                resizeIndex,
                StringComparison.Ordinal);
            Assert.Greater(
                relayoutIndex,
                resizeIndex,
                "Filtered card items must be laid out again after the scroll content height changes.");
        }

        [Test]
        public void FusionMaterialDragReturnIsResolvedAfterTopLayerRestoreAndOutsideFusionArea()
        {
            var itemControllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            var pageControllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs");
            var dragableScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/BbxCommon/Ui/Misc/UiDragable.cs");
            Assert.NotNull(itemControllerScript);
            Assert.NotNull(pageControllerScript);
            Assert.NotNull(dragableScript);

            StringAssert.Contains(
                "m_PreparationPage.IsPointerInsideFusionArea(eventData) == false",
                itemControllerScript.text);
            StringAssert.Contains(
                "Wrapper.OnBackFromTop += OnPreparationDragReturned",
                itemControllerScript.text);
            StringAssert.Contains(
                "m_PreparationPage.RemoveFusionMaterial(m_PreparationSlot)",
                itemControllerScript.text);
            StringAssert.Contains(
                "RectTransformUtility.RectangleContainsScreenPoint",
                pageControllerScript.text);
            StringAssert.DoesNotContain(
                "m_View.CardPoolInteractor.Wrapper.OnInteract += OnCardPoolInteract",
                pageControllerScript.text);
            StringAssert.DoesNotContain("restoredSlotPosition", itemControllerScript.text);
            StringAssert.DoesNotContain("PreparationDragReturnPriority", itemControllerScript.text);
            StringAssert.Contains("UiDragable : BbxUiItem", dragableScript.text);
            StringAssert.Contains(
                "RectTransformUtility.ScreenPointToWorldPointInRectangle",
                dragableScript.text);
            StringAssert.DoesNotContain("eventData.position.AsVector3XY()", dragableScript.text);

            var topLayerRestoreIndex = dragableScript.text.IndexOf(
                "UiApi.SetTopUiBack(EventListener.gameObject)",
                StringComparison.Ordinal);
            var localPositionRestoreIndex = dragableScript.text.IndexOf(
                "SetLocalPositionOnce(m_OriginalPos",
                topLayerRestoreIndex,
                StringComparison.Ordinal);
            Assert.That(topLayerRestoreIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(localPositionRestoreIndex, Is.GreaterThan(topLayerRestoreIndex));

            var battleSlotCase = itemControllerScript.text.IndexOf(
                "case EPreparationBindingMode.BattleSlot:",
                StringComparison.Ordinal);
            var fusionSlotGuard = itemControllerScript.text.IndexOf(
                "source.Source == EPreparationCardSource.FusionSlot",
                battleSlotCase,
                StringComparison.Ordinal);
            var battleSlotDrop = itemControllerScript.text.IndexOf(
                "m_PreparationPage.DropCardOnSlot",
                battleSlotCase,
                StringComparison.Ordinal);
            Assert.That(fusionSlotGuard, Is.GreaterThan(battleSlotCase));
            Assert.That(battleSlotDrop, Is.GreaterThan(fusionSlotGuard));
        }

        [Test]
        public void BattleSlotDragReturnRemovesTheCardOnlyWhenReleasedOutsideItsSourceSlot()
        {
            var itemControllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            var pageControllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs");
            Assert.NotNull(itemControllerScript);
            Assert.NotNull(pageControllerScript);

            StringAssert.Contains(
                "m_PreparationPage.IsPointerInsideBattleSlot(eventData, m_PreparationSlot) == false",
                itemControllerScript.text);
            StringAssert.Contains(
                "m_PreparationPage.RemoveBattleCard(m_PreparationSlot, m_PreparationCardNumber)",
                itemControllerScript.text);
            StringAssert.Contains(
                "RunCardRules.TryRemoveCardFromBattleSlot(m_RunState, sourceSlot, cardNumber)",
                pageControllerScript.text);
        }

        [Test]
        public void SuccessfulBattleSlotDropUsesTheDedicatedPlacementAudio()
        {
            var pageControllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs");
            Assert.NotNull(pageControllerScript);

            StringAssert.Contains(
                "if (RunCardRules.TryPlaceCard(m_RunState, cardNumber, targetSlot))",
                pageControllerScript.text);
            StringAssert.Contains(
                "AudioApi.Play(BattleSlotDropAudioKey, 0.78f)",
                pageControllerScript.text);
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Resources/BbxCommon/Audio/Library/Interface Sounds/drop_001.ogg"));
        }

        [Test]
        public void FusionRevealUsesPaintedFacesWithoutCardLocalGrayBacking()
        {
            var pagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationView.prefab");
            Assert.NotNull(pagePrefab);
            var page = pagePrefab.GetComponent<PreparationView>();
            Assert.NotNull(page);
            Assert.NotNull(page.FusionRevealOverlay);
            Assert.AreEqual(0.78f, page.FusionRevealOverlay.GetComponent<Image>().color.a, 0.001f);
            Assert.IsNull(page.FusionRevealCardRoot.Find("FloatingShadow"));
            Assert.AreEqual(
                "FusionRevealQuestionFace",
                page.FusionRevealSealedFace.GetComponent<Image>().sprite.name);
            Assert.IsNull(page.FusionRevealSealedFace.transform.Find("Seal"));
            Assert.IsNull(page.FusionRevealSealedFace.transform.Find("Question"));
            Assert.AreEqual(
                "FusionRevealCardBack",
                page.FusionRevealCardBack.GetComponent<Image>().sprite.name);
            Assert.IsNull(page.FusionRevealCardBack.transform.Find("CenterDiamond"));
        }

        [Test]
        public void PreparationRewardsDealThenPocketSequentiallyWithManagedAudio()
        {
            ResourceApi.Initialize();
            var controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs");
            Assert.NotNull(controllerScript);
            StringAssert.Contains("m_Session.WasNewlyApplied == false", controllerScript.text);
            StringAssert.Contains("m_Session.RewardCards", controllerScript.text);
            StringAssert.Contains("ERewardRevealPhase.Dealing", controllerScript.text);
            StringAssert.Contains("ERewardRevealPhase.AwaitingConfirm", controllerScript.text);
            StringAssert.Contains("ERewardRevealPhase.Pocketing", controllerScript.text);
            StringAssert.Contains("RewardRevealDealStagger = 0.14f", controllerScript.text);
            StringAssert.Contains("RewardRevealPocketStagger = 0.11f", controllerScript.text);
            StringAssert.Contains("CardPocketFinalScale = 0.3f", controllerScript.text);
            StringAssert.Contains("GetRewardRevealPosition(index, cardCount)", controllerScript.text);
            StringAssert.Contains("GetPocketTarget(overlayRect, itemRect)", controllerScript.text);
            StringAssert.Contains("m_LastConfirmedRewardBatchId = m_RewardRevealBatchId", controllerScript.text);
            StringAssert.Contains("RewardRevealDealAudioKey = \"card-place-1\"", controllerScript.text);
            StringAssert.Contains("CardPocketAudioKey = \"handleSmallLeather\"", controllerScript.text);
            StringAssert.Contains("AudioApi.StopGroup(PreparationCardAnimationAudioGroup)", controllerScript.text);

            var cardControllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            Assert.NotNull(cardControllerScript);
            StringAssert.Contains(
                "BindPreparationRewardReveal(RunCardInstanceData reward)",
                cardControllerScript.text);

            var dealClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Resources/BbxCommon/Audio/Library/Casino Audio/card-place-1.ogg");
            var pocketClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Resources/BbxCommon/Audio/Library/RPG Audio/handleSmallLeather.ogg");
            Assert.NotNull(dealClip);
            Assert.NotNull(pocketClip);
            Assert.NotNull(ResourceApi.GetFile("card-place-1"));
            Assert.NotNull(ResourceApi.GetFile("handleSmallLeather"));
            Assert.That(dealClip.length, Is.InRange(0.68f, 0.7f));
            Assert.That(pocketClip.length, Is.InRange(0.33f, 0.35f));
        }

        [Test]
        public void FusionRevealGathersMaterialsTurnsTwiceWaitsForOutsideClickAndKeepsCardTooltip()
        {
            ResourceApi.Initialize();
            var controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs");
            Assert.NotNull(controllerScript);
            StringAssert.Contains("FusionRevealPlaybackSpeed = 0.8f", controllerScript.text);
            StringAssert.Contains("FusionRevealPeakScale = 1.28f", controllerScript.text);
            StringAssert.Contains("FusionRevealRotationTurns = 2f", controllerScript.text);
            StringAssert.Contains("FusionRevealMinimumScreenHeightCoverage = 2f / 3f", controllerScript.text);
            StringAssert.Contains("PopulateFusionRevealMaterials(transaction)", controllerScript.text);
            StringAssert.Contains("Vector2.Lerp(startPosition, Vector2.zero, progress)", controllerScript.text);
            StringAssert.Contains("EvaluateFusionRevealScale(rotationProgress)", controllerScript.text);
            StringAssert.Contains("CompleteFusionReveal()", controllerScript.text);
            StringAssert.Contains("m_FusionRevealAwaitingDismiss = true", controllerScript.text);
            StringAssert.Contains("OnFusionRevealDismissClicked", controllerScript.text);
            StringAssert.Contains("StartFusionRevealPocket()", controllerScript.text);
            StringAssert.Contains("UpdateFusionRevealPocket(deltaTime)", controllerScript.text);
            StringAssert.Contains("FusionRevealPocketDuration = 0.36f", controllerScript.text);
            StringAssert.Contains("CardPocketFinalScale = 0.3f", controllerScript.text);
            StringAssert.Contains("GetPocketTarget(", controllerScript.text);
            StringAssert.Contains("CardPocketAudioKey = \"handleSmallLeather\"", controllerScript.text);
            StringAssert.Contains("m_FusionRevealCard.SetFusionRevealInteraction(true)", controllerScript.text);
            StringAssert.DoesNotContain("FusionRevealHoldDuration", controllerScript.text);
            StringAssert.DoesNotContain("FusionRevealFadeOutDuration", controllerScript.text);
            StringAssert.Contains("FusionRevealMotionAudioKey = \"card-shuffle\"", controllerScript.text);
            StringAssert.Contains("FusionRevealMomentAudioKey = \"highUp\"", controllerScript.text);
            StringAssert.Contains("m_FusionRevealMomentAudioPlayed == false", controllerScript.text);
            StringAssert.Contains("StopFusionRevealAudio();", controllerScript.text);

            var cardControllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            Assert.NotNull(cardControllerScript);
            StringAssert.Contains("BindFusionMaterialReveal(FusionMaterialSnapshot material)", cardControllerScript.text);
            StringAssert.Contains("SetFusionRevealInteraction(bool enabled)", cardControllerScript.text);

            var motionClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Resources/BbxCommon/Audio/Library/Casino Audio/card-shuffle.ogg");
            var revealClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Resources/BbxCommon/Audio/Library/Digital Audio/highUp.ogg");
            Assert.NotNull(motionClip);
            Assert.NotNull(revealClip);
            Assert.NotNull(ResourceApi.GetFile("card-shuffle"));
            Assert.NotNull(ResourceApi.GetFile("highUp"));
            Assert.That(motionClip.length, Is.GreaterThan(3f));
            Assert.That(revealClip.length, Is.InRange(0.5f, 0.6f));
        }

        [Test]
        public void FusionPanelUsesCurrentRemainingExactTargetGlowAndAuthoritativeButtonState()
        {
            var controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs");
            Assert.NotNull(controllerScript);
            StringAssert.Contains(
                "m_View.FusionCurrentPointValue.text = evaluation.CardNumberSum.ToString()",
                controllerScript.text);
            StringAssert.Contains(
                "m_View.FusionRemainingPointValue.text =",
                controllerScript.text);
            StringAssert.Contains(
                "(RunCardRules.FusionTargetCardNumberSum - evaluation.CardNumberSum).ToString()",
                controllerScript.text);
            StringAssert.Contains(
                "evaluation.CardNumberSum == RunCardRules.FusionTargetCardNumberSum",
                controllerScript.text);
            StringAssert.Contains("UpdateFusionTargetGlow(deltaTime)", controllerScript.text);
            StringAssert.Contains(
                "color = m_View.FusionOverTargetColor",
                controllerScript.text);
            StringAssert.Contains(
                "m_View.FusionRemainingPointValue.color = m_View.FusionUnderTargetColor",
                controllerScript.text);
            StringAssert.Contains(
                "m_View.FusionButton.interactable = evaluation.CanFuse",
                controllerScript.text);
        }

        [Test]
        public void FusionSmartRecommendationUsesVirtualizedRowsAndAllowsEmptySelection()
        {
            var controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs");
            Assert.NotNull(controllerScript);
            StringAssert.Contains(
                "m_View.FusionRecommendationButton.interactable = true",
                controllerScript.text);
            StringAssert.Contains("FindFusionRecommendations", controllerScript.text);
            StringAssert.Contains("ModifyCount<FusionRecommendationItemController>", controllerScript.text);
            StringAssert.Contains("RefreshVisibleFusionRecommendations", controllerScript.text);
            StringAssert.Contains("TryApplyFusionRecommendation", controllerScript.text);
            StringAssert.Contains("FusionRecommendationOverlay.SetActive(true)", controllerScript.text);
            StringAssert.Contains("verticalNormalizedPosition = 1f", controllerScript.text);
            StringAssert.Contains("OnFusionRecommendationPointerEnter", controllerScript.text);
            StringAssert.Contains(
                "m_View.FusionRecommendationTooltip.SetActive(true)",
                controllerScript.text);
            StringAssert.Contains("HideFusionRecommendationTooltip()", controllerScript.text);

            var cardScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            Assert.NotNull(cardScript);
            StringAssert.Contains("EPreparationBindingMode.FusionRecommendation", cardScript.text);
            StringAssert.Contains("m_View.PreparationDragable.enabled = false", cardScript.text);
        }

        [Test]
        public void PreparationCardPoolPanelUsesSparseLightPatternBehindScrollContent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationView.prefab");
            Assert.NotNull(prefab);

            var panel = prefab.transform.Find("CardPoolPanel");
            var pattern = panel != null ? panel.Find("BluePanelPattern") : null;
            var scrollRect = panel != null ? panel.Find("ScrollRect") : null;
            Assert.NotNull(panel);
            Assert.NotNull(pattern);
            Assert.NotNull(scrollRect);
            Assert.That(pattern.GetSiblingIndex(), Is.LessThan(scrollRect.GetSiblingIndex()));

            var patternRect = (RectTransform)pattern;
            Assert.AreEqual(new Vector2(1600f, 500f), patternRect.sizeDelta);
            Assert.AreEqual(6, pattern.Cast<Transform>().Count(child => child.name.StartsWith("Diamond")));
            Assert.AreEqual(12, pattern.Cast<Transform>().Count(child => child.name.StartsWith("Dot")));

            var images = pattern.GetComponentsInChildren<Image>(true);
            Assert.AreEqual(36, images.Length);
            foreach (var image in images)
            {
                Assert.IsFalse(image.raycastTarget, image.name);
                Assert.That(image.color.r, Is.EqualTo(0.72f).Within(0.001f), image.name);
                Assert.That(image.color.g, Is.EqualTo(0.88f).Within(0.001f), image.name);
                Assert.That(image.color.b, Is.EqualTo(1f).Within(0.001f), image.name);
                Assert.That(image.color.a, Is.InRange(0.05f, 0.075f), image.name);
            }

            var raycastablePatternImages = images.Where(image => image.raycastTarget).ToArray();
            Assert.IsEmpty(raycastablePatternImages);
        }

        [Test]
        public void PreparationPoolDynamicItem_UsesViewportMaskAndStencilChain()
        {
            var canvasObject = new GameObject("PreparationClipTestCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject viewObject = null;
            GameObject controllerObject = null;
            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var viewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Resources/Ui/PreparationView.prefab");
                var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Resources/Ui/BattleCardItem.prefab");
                Assert.NotNull(viewPrefab);
                Assert.NotNull(itemPrefab);

                viewObject = UnityEngine.Object.Instantiate(viewPrefab, canvasObject.transform, false);
                var viewRect = (RectTransform)viewObject.transform;
                viewRect.anchorMin = viewRect.anchorMax = new Vector2(0.5f, 0.5f);
                viewRect.anchoredPosition = Vector2.zero;
                viewRect.sizeDelta = new Vector2(1920f, 1080f);

                var scrollRect = viewObject.GetComponent<PreparationView>().CardPoolScrollRect;
                Assert.NotNull(scrollRect);
                var viewportMask = scrollRect.viewport.GetComponent<Mask>();
                var viewportRectMask = scrollRect.viewport.GetComponent<RectMask2D>();
                Assert.NotNull(viewportMask);
                Assert.IsFalse(viewportMask.showMaskGraphic);
                Assert.NotNull(viewportRectMask);

                controllerObject = new GameObject(
                    "BattleCardItemController",
                    typeof(RectTransform));
                controllerObject.transform.SetParent(scrollRect.content, false);
                var itemObject = UnityEngine.Object.Instantiate(
                    itemPrefab,
                    controllerObject.transform,
                    false);

                Canvas.ForceUpdateCanvases();
                var controllerRect = (RectTransform)controllerObject.transform;
                controllerRect.position = scrollRect.viewport.TransformPoint(new Vector3(0f, 1000f, 0f));
                Canvas.ForceUpdateCanvases();
                var canvasTransform = (RectTransform)canvasObject.transform;
                var viewportRect = GetRectRelativeToCanvas(scrollRect.viewport, canvasTransform);

                var activeGraphics = itemObject.GetComponentsInChildren<MaskableGraphic>(false);
                Assert.IsNotEmpty(activeGraphics);
                foreach (var graphic in activeGraphics)
                {
                    Assert.IsTrue(graphic.maskable, graphic.name);
                    CollectionAssert.Contains(
                        graphic.GetComponentsInParent<Mask>(true),
                        viewportMask,
                        $"{graphic.name} is outside the Viewport stencil-mask chain.");
                    CollectionAssert.Contains(
                        graphic.GetComponentsInParent<RectMask2D>(true),
                        viewportRectMask,
                        $"{graphic.name} is outside the Viewport rect-mask chain.");
                    var graphicRect = GetRectRelativeToCanvas(
                        (RectTransform)graphic.transform,
                        canvasTransform);
                    Assert.IsFalse(
                        viewportRect.Overlaps(graphicRect, true),
                        $"{graphic.name} was not positioned outside the Viewport for the mask-chain geometry check.");
                    var renderingMaterial = graphic.materialForRendering;
                    Assert.IsTrue(renderingMaterial.HasProperty("_Stencil"), graphic.name);
                    Assert.AreEqual(1, renderingMaterial.GetInt("_Stencil"), graphic.name);
                    Assert.AreEqual(3, renderingMaterial.GetInt("_StencilComp"), graphic.name);
                }
            }
            finally
            {
                if (controllerObject != null)
                    UnityEngine.Object.DestroyImmediate(controllerObject);
                if (viewObject != null)
                    UnityEngine.Object.DestroyImmediate(viewObject);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static Rect GetRectRelativeToCanvas(RectTransform rect, RectTransform canvas)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var minimum = (Vector2)canvas.InverseTransformPoint(corners[0]);
            var maximum = (Vector2)canvas.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }

        private static void AssertFourCardPresentation(
            int[] materialCardNumbers,
            int expectedResultCardNumber,
            int expectedPresentationCardNumber)
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            foreach (var cardNumber in materialCardNumbers)
                runState.CardInstances[cardNumber] = new RunCardInstanceData(cardNumber, 1, 2);
            SelectFusionMaterials(runState, session, materialCardNumbers);

            var evaluation = RunCardRules.EvaluateFusion(runState, session);
            Assert.IsTrue(evaluation.CanFuse);
            Assert.AreEqual(expectedResultCardNumber, evaluation.ResultCardNumber);
            Assert.AreEqual(expectedPresentationCardNumber, evaluation.PresentationCardNumber);
            Assert.AreEqual(
                EFusionOperationResult.Applied,
                RunCardRules.TryFuse(runState, session, out var result));
            Assert.AreEqual(expectedResultCardNumber, result.CardNumber);
            Assert.AreEqual(expectedPresentationCardNumber, result.PresentationCardNumber);
            Assert.AreEqual(EBattleCardTier.Legendary, result.Tier);
            Assert.AreEqual(materialCardNumbers.Length, result.Attack);
            Assert.AreEqual(materialCardNumbers.Length * 2, result.MaxHealth);
        }

        private static PreparationRewardBatchStartupData CreateBatch(string id, params int[] numbers)
        {
            var grants = new RewardCardGrantStartupData[numbers.Length];
            for (var index = 0; index < numbers.Length; index++)
                grants[index] = Grant(numbers[index]);
            return new PreparationRewardBatchStartupData(id, grants);
        }

        private static PreparationRewardBatchStartupData CreateFusionBatch(string id)
        {
            return new PreparationRewardBatchStartupData(
                id,
                new[]
                {
                    new RewardCardGrantStartupData(14, 2, 3),
                    new RewardCardGrantStartupData(20, 3, 4),
                    new RewardCardGrantStartupData(30, 2, 3),
                    new RewardCardGrantStartupData(35, 4, 5),
                    new RewardCardGrantStartupData(54, 4, 2),
                });
        }

        private static void SelectFusionMaterials(
            RunStateSingletonRawComponent runState,
            PreparationSessionSingletonRawComponent session,
            params int[] cardNumbers)
        {
            for (var index = 0; index < cardNumbers.Length; index++)
            {
                Assert.AreEqual(
                    EFusionOperationResult.Applied,
                    RunCardRules.TrySetFusionMaterial(runState, session, cardNumbers[index], index));
            }
        }

        private static void AssertRejectedFusionSelection(
            RunStateSingletonRawComponent runState,
            PreparationSessionSingletonRawComponent session,
            EFusionOperationResult expectedResult,
            Func<EFusionOperationResult> operation)
        {
            var fusionSlots = (int[])session.FusionSlotCardNumbers.Clone();
            var cardInstances = (RunCardInstanceData[])runState.CardInstances.Clone();
            var ownedCardCount = runState.GetOwnedCardCount();
            var runRevision = runState.Revision.Value;
            var fusionRevision = session.FusionRevision.Value;

            Assert.AreEqual(expectedResult, operation());
            CollectionAssert.AreEqual(fusionSlots, session.FusionSlotCardNumbers);
            Assert.AreEqual(runRevision, runState.Revision.Value);
            Assert.AreEqual(fusionRevision, session.FusionRevision.Value);
            CollectionAssert.AreEqual(cardInstances, runState.CardInstances);
            Assert.AreEqual(ownedCardCount, runState.GetOwnedCardCount());
        }

        private static void AssertMalformedFusionSessionIsRejected(
            int[] malformedFusionSlots,
            EFusionOperationResult expectedResult)
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            RunCardRules.ApplyRewardBatch(runState, CreateFusionBatch("fusion-batch"));
            Array.Copy(
                malformedFusionSlots,
                session.FusionSlotCardNumbers,
                RunCardRules.FusionSlotCount);
            var fusionSlots = (int[])session.FusionSlotCardNumbers.Clone();
            var cardInstances = (RunCardInstanceData[])runState.CardInstances.Clone();
            var ownedCardCount = runState.GetOwnedCardCount();
            var runRevision = runState.Revision.Value;
            var fusionRevision = session.FusionRevision.Value;

            var evaluation = RunCardRules.EvaluateFusion(runState, session);
            Assert.AreEqual(expectedResult, evaluation.BlockingResult);
            Assert.AreEqual(expectedResult, RunCardRules.TryFuse(runState, session, out _));
            CollectionAssert.AreEqual(fusionSlots, session.FusionSlotCardNumbers);
            Assert.AreEqual(runRevision, runState.Revision.Value);
            Assert.AreEqual(fusionRevision, session.FusionRevision.Value);
            CollectionAssert.AreEqual(cardInstances, runState.CardInstances);
            Assert.AreEqual(ownedCardCount, runState.GetOwnedCardCount());
        }

        private static RewardCardGrantStartupData Grant(int number)
        {
            return new RewardCardGrantStartupData(number, number % 6, 3 + number % 3);
        }
    }
}
