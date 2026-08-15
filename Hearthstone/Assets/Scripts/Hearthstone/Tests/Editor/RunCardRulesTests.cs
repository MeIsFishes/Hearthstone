using System;
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
            Assert.AreEqual(result, runState.CardInstances[RunCardRules.FusionResultCardNumber]);
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
            Assert.AreEqual(result, runState.CardInstances[RunCardRules.FusionResultCardNumber]);
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
                RunCardRules.TrySetFusionMaterial(runState, session, RunCardRules.FusionResultCardNumber, 0));
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
                    RunCardRules.FusionResultCardNumber,
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
                new[] { 14, RunCardRules.FusionResultCardNumber, 0, 0 },
                EFusionOperationResult.ResultCardCannotBeMaterial);
            AssertMalformedFusionSessionIsRejected(
                new[] { 14, 2, 0, 0 },
                EFusionOperationResult.UnownedCard);
        }

        [Test]
        public void FusionEvaluationAndCommit_AreAtomicAndUsePermanentStats()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            RunCardRules.ApplyRewardBatch(runState, CreateFusionBatch("fusion-batch"));
            runState.BattleSlotCardNumbers[0] = 14;
            runState.BattleSlotCardNumbers[1] = 54;

            SelectFusionMaterials(runState, session, 14, 20);
            var under = RunCardRules.EvaluateFusion(runState, session);
            Assert.AreEqual(34, under.CardNumberSum);
            Assert.IsFalse(under.CanFuse);

            RunCardRules.TrySetFusionMaterial(runState, session, 30, 2);
            RunCardRules.TrySetFusionMaterial(runState, session, 54, 3);
            var over = RunCardRules.EvaluateFusion(runState, session);
            Assert.AreEqual(118, over.CardNumberSum);
            Assert.IsFalse(over.CanFuse);
            var revisionBeforeInvalidFuse = runState.Revision.Value;
            Assert.AreEqual(EFusionOperationResult.SumMismatch,
                RunCardRules.TryFuse(runState, session, out _));
            Assert.AreEqual(revisionBeforeInvalidFuse, runState.Revision.Value);
            Assert.IsTrue(runState.HasCard(14));

            Assert.AreEqual(EFusionOperationResult.Applied,
                RunCardRules.TrySetFusionMaterial(runState, session, 35, 3));
            Assert.IsTrue(RunCardRules.EvaluateFusion(runState, session).CanFuse);
            Assert.AreEqual(EFusionOperationResult.Applied,
                RunCardRules.TryFuse(runState, session, out var result));
            Assert.AreEqual(RunCardRules.FusionResultCardNumber, result.CardNumber);
            Assert.AreEqual(11, result.Attack);
            Assert.AreEqual(15, result.MaxHealth);
            Assert.IsFalse(runState.HasCard(14));
            Assert.IsFalse(runState.HasCard(20));
            Assert.IsFalse(runState.HasCard(30));
            Assert.IsFalse(runState.HasCard(35));
            Assert.IsTrue(runState.HasCard(54));
            Assert.IsTrue(runState.HasCard(RunCardRules.FusionResultCardNumber));
            CollectionAssert.AreEqual(new[] { 0, 54, 0 }, runState.BattleSlotCardNumbers);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, session.FusionSlotCardNumbers);
        }

        [Test]
        public void FusionCommit_AcceptsTwoThreeAndFourMaterialsThatSumTo99()
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
                Assert.AreEqual(RunCardRules.FusionResultCardNumber, result.CardNumber);
                foreach (var cardNumber in combination)
                    Assert.IsFalse(runState.HasCard(cardNumber));
            }
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

            runState.CardInstances[RunCardRules.FusionResultCardNumber] =
                new RunCardInstanceData(RunCardRules.FusionResultCardNumber, 11, 15);
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
                new RewardCardGrantStartupData(RunCardRules.FusionResultCardNumber, 1, 1));
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
                "Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset");
            Assert.NotNull(font);
            const string requiredCharacters =
                "备战阶段本轮获得张卡槽位池哥布林战士弓手投弹野猪食人魔融合造物出战素材合计";
            Assert.IsTrue(font.HasCharacters(requiredCharacters));

            var prefabPaths = new[]
            {
                "Assets/Resources/Ui/PreparationView.prefab",
                "Assets/Resources/Ui/PreparationCardItem.prefab",
                "Assets/Resources/Ui/PreparationSlotItem.prefab",
                "Assets/Resources/Ui/PreparationFusionSlotItem.prefab",
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
        public void PreparationFusionPrefabsAndResourcesAreFullyExported()
        {
            ResourceApi.Initialize();
            var pagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationView.prefab");
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationCardItem.prefab");
            var fusionSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationFusionSlotItem.prefab");
            Assert.NotNull(pagePrefab);
            Assert.NotNull(cardPrefab);
            Assert.NotNull(fusionSlotPrefab);

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
            Assert.NotNull(page.CardPoolInteractor);

            var card = cardPrefab.GetComponent<PreparationCardItemView>();
            Assert.NotNull(card.MaterialSelectedState);
            Assert.NotNull(card.EmptyAttemptListener);
            Assert.NotNull(fusionSlotPrefab.GetComponent<PreparationFusionSlotItemView>());

            var mappings = UiApi.CapturePreloadedUiPrefabPathsForValidation();
            Assert.AreEqual("Ui/PreparationFusionSlotItem",
                mappings[typeof(PreparationFusionSlotItemController).FullName]);
            Assert.NotNull(Resources.Load<UiSceneAsset>("Ui/Preparation"));
            Assert.AreEqual(15, RunCardRules.CardRowCount);
            Assert.AreEqual(99, RunCardRules.LastCardNumber);

            var spriteKeys = new[]
            {
                "PreparationTabIdle", "PreparationTabSelected", "PreparationFusionSlotFrame",
                "PreparationFusionSumPanel", "PreparationFusionButtonDisabled",
                "PreparationFusionButtonEnabled", "PreparationFusionButtonPressed",
                "PreparationMaterialSelected", "FusionCard_099",
            };
            foreach (var key in spriteKeys)
                Assert.NotNull(ResourceApi.LoadSprite(key), key);
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
                    "Assets/Resources/Ui/PreparationCardItem.prefab");
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
                    "PreparationCardItemController",
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
