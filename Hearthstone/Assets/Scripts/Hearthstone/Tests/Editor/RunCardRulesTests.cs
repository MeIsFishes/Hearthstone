using System;
using System.Reflection;
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
            Assert.IsFalse(runState.AppliedRewardBatchIds.Contains("batch-b"));
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
                "备战阶段本轮获得张卡槽位池哥布林战士弓手投弹野猪食人魔";
            Assert.IsTrue(font.HasCharacters(requiredCharacters));

            var prefabPaths = new[]
            {
                "Assets/Resources/Ui/PreparationView.prefab",
                "Assets/Resources/Ui/PreparationCardItem.prefab",
                "Assets/Resources/Ui/PreparationSlotItem.prefab",
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

        private static RewardCardGrantStartupData Grant(int number)
        {
            return new RewardCardGrantStartupData(number, number % 6, 3 + number % 3);
        }
    }
}
