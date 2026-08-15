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
        public void ApplyRewardBatch_IsAtomicAndIdempotent()
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
            Assert.Throws<InvalidOperationException>(() => RunCardRules.ApplyRewardBatch(runState, overlapping));
            Assert.AreEqual(revision, runState.Revision.Value);
            Assert.AreEqual(5, runState.GetOwnedCardCount());
            Assert.IsFalse(runState.AppliedRewardBatchPayloadFingerprints.ContainsKey("batch-b"));
            Assert.IsFalse(runState.HasCard(8));
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
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, runState.BattleSlotCardNumbers);
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
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, runState.BattleSlotCardNumbers);
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
        public void FusionEvaluationAndCommit_UsesAllFourCardTypesForLegendaryRecipeAndAllStats()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            RunCardRules.ApplyRewardBatch(runState, CreateFusionBatch("fusion-batch"));
            runState.BattleSlotCardNumbers[0] = 14;
            runState.BattleSlotCardNumbers[1] = 54;

            SelectFusionMaterials(runState, session, 14, 20);
            var pair = RunCardRules.EvaluateFusion(runState, session);
            Assert.IsTrue(pair.CanFuse);
            Assert.AreEqual(2, pair.RecipeMaterialCount);
            Assert.AreEqual(105, pair.ResultCardNumber);

            Assert.AreEqual(EFusionOperationResult.Applied,
                RunCardRules.TrySetFusionMaterial(runState, session, 30, 2));
            Assert.AreEqual(EFusionOperationResult.Applied,
                RunCardRules.TrySetFusionMaterial(runState, session, 54, 3));
            var evaluation = RunCardRules.EvaluateFusion(runState, session);
            Assert.AreEqual(118, evaluation.CardNumberSum);
            Assert.AreEqual(4, evaluation.RecipeMaterialCount);
            Assert.AreEqual(187, evaluation.ResultCardNumber);
            Assert.IsTrue(evaluation.CanFuse);
            Assert.AreEqual(EFusionOperationResult.Applied,
                RunCardRules.TryFuse(runState, session, out var result));
            Assert.AreEqual(187, result.CardNumber);
            Assert.AreEqual(EBattleCardTier.Legendary, result.Tier);
            Assert.AreEqual(11, result.Attack);
            Assert.AreEqual(12, result.MaxHealth);
            Assert.IsFalse(runState.HasCard(14));
            Assert.IsFalse(runState.HasCard(20));
            Assert.IsFalse(runState.HasCard(30));
            Assert.IsFalse(runState.HasCard(54));
            Assert.IsTrue(runState.HasCard(35));
            Assert.IsTrue(runState.HasCard(187));
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, runState.BattleSlotCardNumbers);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, session.FusionSlotCardNumbers);
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
            Assert.AreEqual(4, evaluation.RecipeMaterialCount);
            Assert.AreEqual(0, evaluation.ResultCardNumber);
            Assert.AreEqual(EFusionOperationResult.RecipeNotFound, evaluation.BlockingResult);
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
            RunCardRules.ApplyRewardBatch(runState, CreateBatch("batch-a", 2, 3, 5, 6, 7));

            Assert.IsTrue(RunCardRules.TryPlaceCard(runState, 2, 0));
            Assert.IsTrue(RunCardRules.TryPlaceCard(runState, 3, 0));
            CollectionAssert.AreEqual(new[] { 3, 0, 0 }, runState.BattleSlotCardNumbers);
            Assert.IsTrue(RunCardRules.TryPlaceCard(runState, 3, 2));
            CollectionAssert.AreEqual(new[] { 0, 0, 3 }, runState.BattleSlotCardNumbers);
            Assert.IsFalse(RunCardRules.TryPlaceCard(runState, 4, 1));
            CollectionAssert.AreEqual(new[] { 0, 0, 3 }, runState.BattleSlotCardNumbers);
        }

        [Test]
        public void RewardBatch_RequiresExactlyFiveUniqueGrants()
        {
            Assert.Throws<ArgumentException>(() => new PreparationRewardBatchStartupData(
                "short", new[] { Grant(2) }));
            Assert.Throws<ArgumentException>(() => new PreparationRewardBatchStartupData(
                "duplicate", new[] { Grant(2), Grant(2), Grant(3), Grant(5), Grant(6) }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RewardCardGrantStartupData(RunCardRules.LockedCardNumber, 1, 1));
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
                "备战阶段卡槽位池哥布林战士弓手投弹野猪食人魔融合造物出战素材合计继续";
            Assert.IsTrue(font.HasCharacters(requiredCharacters));

            var prefabPaths = new[]
            {
                "Assets/Resources/Ui/PreparationView.prefab",
                "Assets/Resources/Ui/BattleCardItem.prefab",
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
            Assert.NotNull(page.FusionExpressionText);
            Assert.NotNull(page.FusionResultText);
            Assert.NotNull(page.FusionButton);
            Assert.NotNull(page.FusionButtonAttemptListener);
            Assert.NotNull(page.FusionAreaInteractor);
            Assert.NotNull(page.FusionRevealOverlay);
            Assert.NotNull(page.FusionRevealCanvasGroup);
            Assert.NotNull(page.FusionRevealCardRoot);
            Assert.NotNull(page.FusionRevealCardList);
            Assert.NotNull(page.FusionRevealSealedFace);
            Assert.NotNull(page.FusionRevealCardBack);
            Assert.NotNull(page.FusionRevealFlash);
            Assert.NotNull(page.FusionRevealFlashCanvasGroup);
            Assert.IsTrue(page.FusionRevealCanvasGroup.blocksRaycasts);
            Assert.AreEqual(UiList.EArrangement.Manual, page.FusionRevealCardList.ArragementType);
            Assert.AreEqual(180f, page.FusionRevealCardBack.transform.localEulerAngles.y, 0.01f);
            Assert.NotNull(page.FusionRevealFlash.parent.GetComponent<RectMask2D>());
            Assert.AreEqual(
                pagePrefab.transform.childCount - 1,
                page.FusionRevealOverlay.transform.GetSiblingIndex());
            Assert.NotNull(page.CardPoolInteractor);
            Assert.NotNull(page.OwnedOnlyToggle);
            Assert.NotNull(page.OwnedOnlyLabel);
            Assert.IsFalse(page.OwnedOnlyToggle.isOn);
            Assert.AreEqual("查看拥有", page.OwnedOnlyLabel.text);
            Assert.AreSame(
                page.OwnedOnlyToggle.transform,
                pagePrefab.transform.Find("CardPoolPanel/OwnedOnlyToggle"));
            Assert.IsNull(pagePrefab.transform.Find("RewardPanel"));
            Assert.IsNull(pagePrefab.transform.Find("CardPoolPanel/PoolTitle"));
            Assert.IsNull(pagePrefab.transform.Find("ContinueButton/AuxiliaryLabel"));
            Assert.AreEqual("继续", page.ContinueMainText.text);
            Assert.AreEqual("PreparationTabSelectedV2", page.BattleTabImage.sprite.name);
            Assert.AreEqual("PreparationTabIdleV2", page.FusionTabImage.sprite.name);

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
            Assert.NotNull(Resources.Load<UiSceneAsset>("Ui/Preparation"));
            Assert.AreEqual(31, RunCardRules.CardRowCount);
            Assert.AreEqual(213, RunCardRules.LastCardNumber);

            var spriteKeys = new[]
            {
                "PreparationTabIdle", "PreparationTabIdleV2", "PreparationTabSelectedV2",
                "PreparationTabSelected",
                "PreparationFusionSlotFrame",
                "PreparationFusionSumPanel", "PreparationFusionButtonDisabled",
                "PreparationFusionButtonEnabled", "PreparationFusionButtonPressed",
                "PreparationMaterialSelected", "FusionCard_099",
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
                "m_ShowOwnedOnly && m_RunState.HasCard(cardNumber) == false",
                controllerScript.text);
            StringAssert.Contains("item.BindPreparation(this, cardNumber, displayNumber)", controllerScript.text);
            StringAssert.Contains("nextLegendaryDisplayNumber++", controllerScript.text);
            StringAssert.Contains("SetIsOnWithoutNotify(false)", controllerScript.text);
            StringAssert.Contains("verticalNormalizedPosition = 1f", controllerScript.text);
            StringAssert.Contains("HasOwnedCardSetChanged()", controllerScript.text);
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
        public void FusionRevealUsesScalePulseReducedSpeedAndRegisteredAudio()
        {
            ResourceApi.Initialize();
            var controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs");
            Assert.NotNull(controllerScript);
            StringAssert.Contains("FusionRevealPlaybackSpeed = 0.8f", controllerScript.text);
            StringAssert.Contains("FusionRevealPeakScale = 1.28f", controllerScript.text);
            StringAssert.Contains(
                "rotationProgress / FusionRevealResultRotationProgress",
                controllerScript.text);
            StringAssert.Contains("FusionRevealMotionAudioKey = \"card-shuffle\"", controllerScript.text);
            StringAssert.Contains("FusionRevealMomentAudioKey = \"highUp\"", controllerScript.text);
            StringAssert.Contains("m_FusionRevealMomentAudioPlayed == false", controllerScript.text);
            StringAssert.Contains("StopFusionRevealAudio();", controllerScript.text);

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
