using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class PreparationViewUiBuilder
    {
        private const string PrefabPath = "Assets/Resources/Ui/PreparationView.prefab";
        private const float FusionRevealCardWidth = 250f;
        private const float FusionRevealCardHeight = 360f;
        private const float CardPoolScrollHeight = 510f;

        public static void Build()
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("PreparationView", null);
            try
            {
                ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
                var view = root.AddComponent<PreparationView>();
                view.DefaultShow = true;

                var background = PreparationUiBuilderUtility.CreateUiObject("Background", root.transform);
                PreparationUiBuilderUtility.Stretch(background);
                PreparationUiBuilderUtility.AddImage(
                    background,
                    PreparationUiBuilderUtility.LoadSprite("PreparationPageBackground"));

                var parchmentAging = PreparationUiBuilderUtility.CreateUiObject("ParchmentAgingOverlay", root.transform);
                PreparationUiBuilderUtility.SetRect(
                    parchmentAging,
                    new Vector2(0.5f, 1f),
                    new Vector2(1700f, 380f),
                    new Vector2(0f, -270f));
                var parchmentAgingImage = PreparationUiBuilderUtility.AddImage(
                    parchmentAging,
                    PreparationUiBuilderUtility.LoadSprite("ParchmentAgingOverlay"));
                parchmentAgingImage.color = new Color(1f, 1f, 1f, 0.18f);
                parchmentAgingImage.raycastTarget = false;

                CreateTitle(root.transform);
                CreateContinue(root.transform, view);
                CreateTabs(root.transform, view);
                CreateBattleOperation(root.transform, view);
                CreateFusionOperation(root.transform, view);
                CreatePool(root.transform, view);
                CreateFusionRecommendationPopup(root.transform, view);
                CreateRewardReveal(root.transform, view);
                CreateFusionReveal(root.transform, view);

                PreparationUiBuilderUtility.SavePrefab(root, PrefabPath, false);
            }
            finally
            {
                PreparationUiBuilderUtility.DestroyTemporary(root);
            }
        }

        private static void CreateTitle(Transform parent)
        {
            var titleFrame = PreparationUiBuilderUtility.CreateUiObject("TitleFrame", parent);
            PreparationUiBuilderUtility.SetRect(titleFrame, new Vector2(0.5f, 1f), new Vector2(580f, 110f), new Vector2(0f, -55f));
            PreparationUiBuilderUtility.AddImage(
                titleFrame,
                PreparationUiBuilderUtility.LoadSprite("PreparationStageTitleFrame"));
            var title = PreparationUiBuilderUtility.CreateUiObject("Title", titleFrame.transform);
            PreparationUiBuilderUtility.SetRect(
                title,
                new Vector2(0.5f, 0.5f),
                new Vector2(580f, 110f),
                new Vector2(0f, 2f));
            PreparationUiBuilderUtility.AddText(title, "备战阶段", 46f);
        }

        private static void CreateContinue(Transform parent, PreparationView view)
        {
            var idle = PreparationUiBuilderUtility.LoadSprite("PreparationContinueButtonIdle");
            var highlighted = PreparationUiBuilderUtility.LoadSprite("PreparationContinueButtonHighlighted");
            var waiting = PreparationUiBuilderUtility.LoadSprite("PreparationContinueButtonWaiting");

            var root = PreparationUiBuilderUtility.CreateUiObject("ContinueButton", parent);
            PreparationUiBuilderUtility.SetRect(
                root,
                new Vector2(1f, 1f),
                new Vector2(350f, 144f),
                new Vector2(-220f, -120f));
            var image = PreparationUiBuilderUtility.AddImage(root, idle, true);
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.spriteState = new SpriteState
            {
                highlightedSprite = highlighted,
                pressedSprite = idle,
                selectedSprite = idle,
                disabledSprite = waiting,
            };

            var mainLabel = PreparationUiBuilderUtility.CreateUiObject("MainLabel", root.transform);
            PreparationUiBuilderUtility.SetRect(
                mainLabel,
                new Vector2(0.5f, 0.5f),
                new Vector2(250f, 64f),
                new Vector2(0f, 3f));
            var mainText = PreparationUiBuilderUtility.AddText(mainLabel, "继续", 40f);

            var blocker = PreparationUiBuilderUtility.CreateUiObject("ContinueWaitingInputBlocker", root.transform);
            PreparationUiBuilderUtility.Stretch(blocker);
            var blockerImage = PreparationUiBuilderUtility.AddImage(blocker, null, true);
            blockerImage.color = new Color(1f, 1f, 1f, 0.001f);
            var attemptListener = blocker.AddComponent<UiEventListener>();
            blocker.SetActive(false);

            view.ContinueButton = button;
            view.ContinueButtonImage = image;
            view.ContinueMainText = mainText;
            view.ContinueWaitingInputBlocker = blocker;
            view.ContinueWaitingAttemptListener = attemptListener;
        }

        private static void CreateTabs(Transform parent, PreparationView view)
        {
            var idleSprite = PreparationUiBuilderUtility.LoadSprite("PreparationTabIdleV2");
            var selectedSprite = PreparationUiBuilderUtility.LoadSprite("PreparationTabSelectedV2");
            view.BattleTabButton = CreateTab(
                parent,
                "BattleTab",
                "出战",
                new Vector2(215f, -58f),
                selectedSprite,
                out var battleImage);
            view.FusionTabButton = CreateTab(
                parent,
                "FusionTab",
                "融合",
                new Vector2(525f, -58f),
                idleSprite,
                out var fusionImage);
            view.BattleTabImage = battleImage;
            view.FusionTabImage = fusionImage;
        }

        private static Button CreateTab(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            Sprite sprite,
            out Image image)
        {
            var tab = PreparationUiBuilderUtility.CreateUiObject(name, parent);
            PreparationUiBuilderUtility.SetRect(tab, new Vector2(0f, 1f), new Vector2(330f, 100f), position);
            image = PreparationUiBuilderUtility.AddImage(
                tab,
                sprite,
                true);
            var button = tab.AddComponent<Button>();
            button.targetGraphic = image;
            var text = PreparationUiBuilderUtility.CreateUiObject("Label", tab.transform);
            PreparationUiBuilderUtility.SetRect(
                text,
                new Vector2(0.5f, 0.5f),
                new Vector2(250f, 70f),
                new Vector2(0f, 4f));
            PreparationUiBuilderUtility.AddText(text, label, 31f);
            return button;
        }

        private static void CreateBattleOperation(Transform parent, PreparationView view)
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("BattleOperation", parent);
            PreparationUiBuilderUtility.SetRect(root, new Vector2(0.5f, 1f), new Vector2(1200f, 330f), new Vector2(0f, -300f));

            var listObject = PreparationUiBuilderUtility.CreateUiObject("BattleSlotList", root.transform);
            PreparationUiBuilderUtility.SetRect(listObject, new Vector2(0.5f, 1f), new Vector2(1260f, 320f), new Vector2(0f, -130f));
            var list = listObject.AddComponent<UiList>();
            list.ArragementType = UiList.EArrangement.ConstantSlot;
            list.ConstantSlotDirection = UiList.EDirection.Horizontal;
            list.ConstantSlotSize = new Vector2(
                205f,
                205f * RunCardRules.CardAspectHeight / RunCardRules.CardAspectWidth);
            view.BattleOperationRoot = root;
            view.BattleSlotList = list;
        }

        private static void CreateFusionOperation(Transform parent, PreparationView view)
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("FusionOperation", parent);
            PreparationUiBuilderUtility.SetRect(root, new Vector2(0.5f, 1f), new Vector2(1480f, 330f), new Vector2(0f, -300f));
            var hitImage = PreparationUiBuilderUtility.AddImage(root, null, true);
            hitImage.color = new Color(1f, 1f, 1f, 0.001f);
            var areaInteractor = root.AddComponent<UiInteractor>();
            areaInteractor.TransformOverride = root.transform;
            areaInteractor.AutoInitUiDragable = false;

            var title = PreparationUiBuilderUtility.CreateUiObject("Title", root.transform);
            PreparationUiBuilderUtility.SetRect(title, new Vector2(0f, 1f), new Vector2(850f, 46f), new Vector2(425f, -40f));
            PreparationUiBuilderUtility.AddText(title, "融合素材", 32f);

            var listObject = PreparationUiBuilderUtility.CreateUiObject("FusionSlotList", root.transform);
            PreparationUiBuilderUtility.SetRect(listObject, new Vector2(0f, 1f), new Vector2(800f, 276f), new Vector2(420f, -150f));
            var list = listObject.AddComponent<UiList>();
            list.ArragementType = UiList.EArrangement.ConstantSlot;
            list.ConstantSlotDirection = UiList.EDirection.Horizontal;
            list.ConstantSlotSize = new Vector2(
                190f,
                190f * RunCardRules.CardAspectHeight / RunCardRules.CardAspectWidth);

            var sumPanel = PreparationUiBuilderUtility.CreateUiObject("FusionSumPanel", root.transform);
            PreparationUiBuilderUtility.SetRect(
                sumPanel,
                new Vector2(1f, 0.5f),
                new Vector2(610f, 250f),
                new Vector2(-315f, 0f));
            var pointPanelSprite = PreparationUiBuilderUtility.LoadSprite("PreparationFusionSumPanel");

            var currentPointPanel = PreparationUiBuilderUtility.CreateUiObject(
                "CurrentPointPanel",
                sumPanel.transform);
            PreparationUiBuilderUtility.SetRect(
                currentPointPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(280f, 72f),
                new Vector2(-150f, 82f));
            var currentPointPanelImage = PreparationUiBuilderUtility.AddImage(
                currentPointPanel,
                pointPanelSprite);
            currentPointPanelImage.preserveAspect = false;
            var currentPointLabelObject = PreparationUiBuilderUtility.CreateUiObject(
                "Label",
                currentPointPanel.transform);
            PreparationUiBuilderUtility.SetRect(
                currentPointLabelObject,
                new Vector2(0f, 0.5f),
                new Vector2(158f, 54f),
                new Vector2(91f, 0f));
            var currentPointLabel = PreparationUiBuilderUtility.AddText(
                currentPointLabelObject,
                "当前点数",
                24f,
                TextAlignmentOptions.MidlineLeft);
            currentPointLabel.color = Color.black;
            var currentPointValueObject = PreparationUiBuilderUtility.CreateUiObject(
                "Value",
                currentPointPanel.transform);
            PreparationUiBuilderUtility.SetRect(
                currentPointValueObject,
                new Vector2(1f, 0.5f),
                new Vector2(78f, 54f),
                new Vector2(-55f, 0f));
            var currentPointValue = PreparationUiBuilderUtility.AddText(
                currentPointValueObject,
                "0",
                28f,
                TextAlignmentOptions.MidlineRight);
            currentPointValue.color = Color.black;

            var remainingPointPanel = PreparationUiBuilderUtility.CreateUiObject(
                "RemainingPointPanel",
                sumPanel.transform);
            PreparationUiBuilderUtility.SetRect(
                remainingPointPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(280f, 72f),
                new Vector2(-150f, 0f));
            var remainingPointPanelImage = PreparationUiBuilderUtility.AddImage(
                remainingPointPanel,
                pointPanelSprite);
            remainingPointPanelImage.preserveAspect = false;
            var remainingPointLabelObject = PreparationUiBuilderUtility.CreateUiObject(
                "Label",
                remainingPointPanel.transform);
            PreparationUiBuilderUtility.SetRect(
                remainingPointLabelObject,
                new Vector2(0f, 0.5f),
                new Vector2(158f, 54f),
                new Vector2(91f, 0f));
            var remainingPointLabel = PreparationUiBuilderUtility.AddText(
                remainingPointLabelObject,
                "剩余点数",
                24f,
                TextAlignmentOptions.MidlineLeft);
            remainingPointLabel.color = Color.black;
            var remainingPointValueObject = PreparationUiBuilderUtility.CreateUiObject(
                "Value",
                remainingPointPanel.transform);
            PreparationUiBuilderUtility.SetRect(
                remainingPointValueObject,
                new Vector2(1f, 0.5f),
                new Vector2(78f, 54f),
                new Vector2(-55f, 0f));
            var remainingPointValue = PreparationUiBuilderUtility.AddText(
                remainingPointValueObject,
                "99",
                28f,
                TextAlignmentOptions.MidlineRight);
            remainingPointValue.color = Color.black;
            view.FusionUnderTargetColor = Color.black;
            view.FusionExactTargetColor = new Color(0.42f, 1f, 0.48f, 1f);
            view.FusionOverTargetColor = new Color(1f, 0.32f, 0.27f, 1f);

            var buttonObject = PreparationUiBuilderUtility.CreateUiObject("FusionButton", sumPanel.transform);
            PreparationUiBuilderUtility.SetRect(
                buttonObject,
                new Vector2(0.5f, 0.5f),
                new Vector2(300f, 82f),
                new Vector2(150f, 82f));
            var buttonImage = PreparationUiBuilderUtility.AddImage(
                buttonObject,
                PreparationUiBuilderUtility.LoadSprite("PreparationFusionButtonEnabled"),
                true);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = PreparationUiBuilderUtility.LoadSprite("PreparationFusionButtonEnabled"),
                pressedSprite = PreparationUiBuilderUtility.LoadSprite("PreparationFusionButtonPressed"),
                selectedSprite = PreparationUiBuilderUtility.LoadSprite("PreparationFusionButtonEnabled"),
                disabledSprite = PreparationUiBuilderUtility.LoadSprite("PreparationFusionButtonDisabled"),
            };
            var buttonLabel = PreparationUiBuilderUtility.CreateUiObject("Label", buttonObject.transform);
            PreparationUiBuilderUtility.Stretch(buttonLabel, 25f, 12f, 25f, 12f);
            PreparationUiBuilderUtility.AddText(buttonLabel, "融合", 30f);
            var attemptListener = buttonObject.AddComponent<UiEventListener>();

            var recommendationObject = PreparationUiBuilderUtility.CreateUiObject(
                "FusionRecommendationButton",
                sumPanel.transform);
            PreparationUiBuilderUtility.SetRect(
                recommendationObject,
                new Vector2(0.5f, 0.5f),
                new Vector2(216f, 68f),
                new Vector2(-150f, -82f));
            var recommendationImage = PreparationUiBuilderUtility.AddImage(
                recommendationObject,
                PreparationUiBuilderUtility.LoadSprite("PreparationFusionButtonEnabled"),
                true);
            var recommendationButton = recommendationObject.AddComponent<Button>();
            recommendationButton.targetGraphic = recommendationImage;
            recommendationButton.transition = Selectable.Transition.SpriteSwap;
            recommendationButton.spriteState = new SpriteState
            {
                highlightedSprite = PreparationUiBuilderUtility.LoadSprite("PreparationFusionButtonEnabled"),
                pressedSprite = PreparationUiBuilderUtility.LoadSprite("PreparationFusionButtonPressed"),
                selectedSprite = PreparationUiBuilderUtility.LoadSprite("PreparationFusionButtonEnabled"),
                disabledSprite = PreparationUiBuilderUtility.LoadSprite("PreparationFusionButtonDisabled"),
            };
            var recommendationLabel = PreparationUiBuilderUtility.CreateUiObject(
                "Label",
                recommendationObject.transform);
            PreparationUiBuilderUtility.Stretch(recommendationLabel, 16f, 10f, 16f, 10f);
            PreparationUiBuilderUtility.AddText(recommendationLabel, "智能推荐", 27f);
            var recommendationHoverListener = recommendationObject.AddComponent<UiEventListener>();

            var recommendationTooltip = PreparationUiBuilderUtility.CreateUiObject(
                "Tooltip",
                recommendationObject.transform);
            PreparationUiBuilderUtility.SetRect(
                recommendationTooltip,
                new Vector2(0.5f, 0.5f),
                new Vector2(460f, 94f),
                new Vector2(354f, 0f));
            var recommendationTooltipBackground = PreparationUiBuilderUtility.AddImage(
                recommendationTooltip,
                PreparationUiBuilderUtility.LoadExistingSprite(
                    "Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png"));
            recommendationTooltipBackground.preserveAspect = false;
            recommendationTooltipBackground.color = new Color32(184, 137, 83, 255);
            recommendationTooltipBackground.raycastTarget = false;
            var recommendationTooltipBorder = recommendationTooltip.AddComponent<Outline>();
            recommendationTooltipBorder.effectColor = new Color32(91, 47, 24, 255);
            recommendationTooltipBorder.effectDistance = new Vector2(3f, -3f);
            recommendationTooltipBorder.useGraphicAlpha = true;
            var recommendationTooltipTextObject = PreparationUiBuilderUtility.CreateUiObject(
                "Text",
                recommendationTooltip.transform);
            PreparationUiBuilderUtility.Stretch(recommendationTooltipTextObject, 24f, 16f, 24f, 16f);
            var recommendationTooltipText = PreparationUiBuilderUtility.AddText(
                recommendationTooltipTextObject,
                "智能寻找牌库中可以融合的组合",
                20f,
                TextAlignmentOptions.MidlineLeft);
            recommendationTooltipText.fontStyle = FontStyles.Bold;
            recommendationTooltipText.color = new Color32(69, 34, 18, 255);
            recommendationTooltipText.enableWordWrapping = false;
            recommendationTooltipText.overflowMode = TextOverflowModes.Overflow;
            recommendationTooltip.SetActive(false);

            view.FusionOperationRoot = root;
            view.FusionSlotList = list;
            view.FusionCurrentPointLabel = currentPointLabel;
            view.FusionCurrentPointValue = currentPointValue;
            view.FusionRemainingPointLabel = remainingPointLabel;
            view.FusionRemainingPointValue = remainingPointValue;
            view.FusionButton = button;
            view.FusionButtonImage = buttonImage;
            view.FusionButtonAttemptListener = attemptListener;
            view.FusionRecommendationButton = recommendationButton;
            view.FusionRecommendationHoverListener = recommendationHoverListener;
            view.FusionRecommendationTooltip = recommendationTooltip;
            view.FusionAreaInteractor = areaInteractor;
        }

        private static void CreateFusionRecommendationPopup(Transform parent, PreparationView view)
        {
            var overlay = PreparationUiBuilderUtility.CreateUiObject(
                "FusionRecommendationOverlay",
                parent);
            PreparationUiBuilderUtility.Stretch(overlay);
            var dimmer = PreparationUiBuilderUtility.AddImage(overlay, null, true);
            dimmer.color = new Color(0.02f, 0.04f, 0.08f, 0.78f);

            var panel = PreparationUiBuilderUtility.CreateUiObject("Panel", overlay.transform);
            PreparationUiBuilderUtility.SetRect(
                panel,
                new Vector2(0.5f, 0.5f),
                new Vector2(1240f, 700f),
                Vector2.zero);
            PreparationUiBuilderUtility.AddImage(
                panel,
                PreparationUiBuilderUtility.LoadExistingSprite(
                    "Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png"),
                true);

            var parchmentAging = PreparationUiBuilderUtility.CreateUiObject(
                "ParchmentAgingOverlay",
                panel.transform);
            PreparationUiBuilderUtility.Stretch(parchmentAging);
            var parchmentAgingImage = PreparationUiBuilderUtility.AddImage(
                parchmentAging,
                PreparationUiBuilderUtility.LoadSprite("ParchmentAgingOverlay"));
            parchmentAgingImage.color = new Color(1f, 1f, 1f, 0.14f);
            parchmentAgingImage.raycastTarget = false;

            var closeObject = PreparationUiBuilderUtility.CreateUiObject("CloseButton", panel.transform);
            PreparationUiBuilderUtility.SetRect(
                closeObject,
                new Vector2(1f, 1f),
                new Vector2(72f, 72f),
                new Vector2(-54f, -54f));
            var closeImage = PreparationUiBuilderUtility.AddImage(
                closeObject,
                PreparationUiBuilderUtility.LoadSprite("PreparationTabIdle"),
                true);
            closeImage.preserveAspect = true;
            var closeButton = closeObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            var closeLabel = PreparationUiBuilderUtility.CreateUiObject("Label", closeObject.transform);
            PreparationUiBuilderUtility.Stretch(closeLabel, 8f);
            PreparationUiBuilderUtility.AddText(closeLabel, "×", 42f);

            var scrollObject = PreparationUiBuilderUtility.CreateUiObject("ScrollRect", panel.transform);
            PreparationUiBuilderUtility.SetRect(
                scrollObject,
                new Vector2(0.5f, 0.5f),
                new Vector2(1060f, 560f),
                new Vector2(0f, -20f));
            var scrollBackground = PreparationUiBuilderUtility.AddImage(scrollObject, null, true);
            scrollBackground.color = new Color(1f, 1f, 1f, 0f);
            var scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 40f;

            var viewport = PreparationUiBuilderUtility.CreateUiObject("Viewport", scrollObject.transform);
            PreparationUiBuilderUtility.Stretch(viewport, 26f, 20f, 58f, 20f);
            viewport.AddComponent<RectMask2D>();

            var content = PreparationUiBuilderUtility.CreateUiObject("Content", viewport.transform);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 520f);
            contentRect.anchoredPosition = Vector2.zero;
            var recommendationList = content.AddComponent<UiList>();
            recommendationList.ArragementType = UiList.EArrangement.Manual;

            var emptyTextObject = PreparationUiBuilderUtility.CreateUiObject(
                "EmptyText",
                content.transform);
            PreparationUiBuilderUtility.Stretch(emptyTextObject, 20f);
            var emptyText = PreparationUiBuilderUtility.AddText(
                emptyTextObject,
                "无可用组合",
                27f,
                TextAlignmentOptions.Center);
            emptyText.color = new Color(0.23f, 0.12f, 0.055f, 1f);
            emptyText.raycastTarget = false;

            var scrollbarObject = PreparationUiBuilderUtility.CreateUiObject(
                "Scrollbar",
                scrollObject.transform);
            PreparationUiBuilderUtility.SetRect(
                scrollbarObject,
                new Vector2(1f, 0.5f),
                new Vector2(28f, 520f),
                new Vector2(-25f, 0f));
            var scrollbarTrack = PreparationUiBuilderUtility.AddImage(scrollbarObject, null, true);
            scrollbarTrack.color = new Color(0.24f, 0.11f, 0.04f, 0.45f);
            var scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            var slidingArea = PreparationUiBuilderUtility.CreateUiObject(
                "SlidingArea",
                scrollbarObject.transform);
            PreparationUiBuilderUtility.Stretch(slidingArea, 4f);
            var handle = PreparationUiBuilderUtility.CreateUiObject("Handle", slidingArea.transform);
            PreparationUiBuilderUtility.Stretch(handle);
            var handleImage = PreparationUiBuilderUtility.AddImage(handle, null, true);
            handleImage.color = new Color(0.83f, 0.63f, 0.25f, 1f);
            scrollbar.handleRect = (RectTransform)handle.transform;
            scrollbar.targetGraphic = handleImage;

            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = contentRect;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 12f;

            overlay.SetActive(false);
            view.FusionRecommendationOverlay = overlay;
            view.FusionRecommendationCloseButton = closeButton;
            view.FusionRecommendationScrollRect = scrollRect;
            view.FusionRecommendationList = recommendationList;
            view.FusionRecommendationEmptyText = emptyText;
        }

        private static void CreatePool(Transform parent, PreparationView view)
        {
            var panel = PreparationUiBuilderUtility.CreateUiObject("CardPoolPanel", parent);
            PreparationUiBuilderUtility.SetRect(panel, new Vector2(0.5f, 0f), new Vector2(1780f, 630f), new Vector2(0f, 320f));
            PreparationUiBuilderUtility.AddImage(
                panel,
                PreparationUiBuilderUtility.LoadSprite("PreparationCardPoolPanel"),
                false,
                Image.Type.Sliced);
            CreatePoolPattern(panel.transform);
            var poolInteractor = panel.AddComponent<UiInteractor>();
            poolInteractor.TransformOverride = panel.transform;
            poolInteractor.AutoInitUiDragable = false;
            CreateOwnedOnlyToggle(panel.transform, view);

            var scrollObject = PreparationUiBuilderUtility.CreateUiObject("ScrollRect", panel.transform);
            PreparationUiBuilderUtility.SetRect(
                scrollObject,
                new Vector2(0.5f, 0.5f),
                new Vector2(1650f, CardPoolScrollHeight),
                new Vector2(0f, -10f));
            var scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 45f;

            var viewport = PreparationUiBuilderUtility.CreateUiObject("Viewport", scrollObject.transform);
            PreparationUiBuilderUtility.SetRect(
                viewport,
                new Vector2(0.5f, 0.5f),
                new Vector2(PreparationUiBuilderUtility.CardPoolViewportWidth, CardPoolScrollHeight),
                new Vector2(-31f, 0f));
            var viewportImage = PreparationUiBuilderUtility.AddImage(viewport, null, true);
            viewportImage.color = Color.white;
            var viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            viewport.AddComponent<RectMask2D>();

            var cellWidth = PreparationUiBuilderUtility.CardPoolCellWidth;
            var cellHeight = PreparationUiBuilderUtility.CardPoolCellHeight;
            var content = PreparationUiBuilderUtility.CreateUiObject("Content", viewport.transform);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(
                cellWidth * RunCardRules.CardsPerRow,
                cellHeight * RunCardRules.CardRowCount);
            contentRect.anchoredPosition = Vector2.zero;
            var poolList = content.AddComponent<UiList>();
            poolList.ArragementType = UiList.EArrangement.ConstantSlot;
            poolList.ConstantSlotDirection = UiList.EDirection.Horizontal;
            poolList.ConstantSlotSize = new Vector2(cellWidth, cellHeight);

            var scrollbarObject = PreparationUiBuilderUtility.CreateUiObject("Scrollbar", scrollObject.transform);
            PreparationUiBuilderUtility.SetRect(
                scrollbarObject,
                new Vector2(0.5f, 0.5f),
                new Vector2(46f, CardPoolScrollHeight),
                new Vector2(780f, -10f));
            var scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var trackVisual = PreparationUiBuilderUtility.CreateUiObject("TrackVisual", scrollbarObject.transform);
            PreparationUiBuilderUtility.SetRect(
                trackVisual,
                new Vector2(0.5f, 0.5f),
                new Vector2(46f, CardPoolScrollHeight),
                Vector2.zero);
            var trackImage = PreparationUiBuilderUtility.AddImage(
                trackVisual,
                PreparationUiBuilderUtility.LoadSprite("PreparationScrollTrack"));
            trackImage.preserveAspect = false;
            trackImage.color = new Color(1f, 1f, 1f, 0.85f);

            var arrowSprite = PreparationUiBuilderUtility.LoadSprite("PreparationScrollArrow");
            CreateScrollArrow(scrollbarObject.transform, "UpArrow", arrowSprite, new Vector2(0f, 243f), 180f);
            CreateScrollArrow(scrollbarObject.transform, "DownArrow", arrowSprite, new Vector2(0f, -243f), 0f);

            var slidingArea = PreparationUiBuilderUtility.CreateUiObject("SlidingArea", scrollbarObject.transform);
            PreparationUiBuilderUtility.Stretch(slidingArea, 4f, 28f, 4f, 28f);
            var handle = PreparationUiBuilderUtility.CreateUiObject("Handle", slidingArea.transform);
            PreparationUiBuilderUtility.Stretch(handle);
            var handleImage = PreparationUiBuilderUtility.AddImage(
                handle,
                PreparationUiBuilderUtility.LoadSprite("PreparationScrollThumb"),
                true);
            scrollbar.handleRect = (RectTransform)handle.transform;
            scrollbar.targetGraphic = handleImage;
            scrollbar.size = 0.2f;

            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = contentRect;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarSpacing = 15f;
            view.CardPoolScrollRect = scrollRect;
            view.CardPoolList = poolList;
            view.CardPoolScrollbar = scrollbar;
            view.CardPoolInteractor = poolInteractor;
        }

        private static void CreateOwnedOnlyToggle(Transform parent, PreparationView view)
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("OwnedOnlyToggle", parent);
            PreparationUiBuilderUtility.SetRect(
                root,
                new Vector2(0.5f, 0.5f),
                new Vector2(240f, 42f),
                new Vector2(-770f, 285f));
            var hitArea = PreparationUiBuilderUtility.AddImage(
                root,
                PreparationUiBuilderUtility.LoadSprite("PreparationTabIdle"),
                true);
            hitArea.color = Color.white;
            hitArea.preserveAspect = true;

            var toggle = root.AddComponent<Toggle>();
            toggle.transition = Selectable.Transition.None;
            toggle.targetGraphic = hitArea;
            toggle.isOn = true;
            toggle.navigation = new Navigation { mode = Navigation.Mode.None };

            var box = PreparationUiBuilderUtility.CreateUiObject("Box", root.transform);
            PreparationUiBuilderUtility.SetRect(
                box,
                new Vector2(0f, 0.5f),
                new Vector2(38f, 38f),
                new Vector2(22f, 0f));
            var boxImage = PreparationUiBuilderUtility.AddImage(
                box,
                PreparationUiBuilderUtility.LoadSprite("PreparationTabSelected"));
            boxImage.preserveAspect = true;
            boxImage.raycastTarget = false;

            var checkmark = PreparationUiBuilderUtility.CreateUiObject("Checkmark", box.transform);
            PreparationUiBuilderUtility.Stretch(checkmark, 7f, 7f, 7f, 7f);
            var checkmarkText = PreparationUiBuilderUtility.AddText(checkmark, "✓", 23f);
            checkmarkText.fontStyle = FontStyles.Bold;
            checkmarkText.color = new Color(1f, 0.9f, 0.45f, 1f);
            checkmarkText.canvasRenderer.SetAlpha(0f);
            toggle.graphic = checkmarkText;

            var label = PreparationUiBuilderUtility.CreateUiObject("Label", root.transform);
            PreparationUiBuilderUtility.SetRect(
                label,
                new Vector2(0f, 0.5f),
                new Vector2(185f, 38f),
                new Vector2(132f, 0f));
            var labelText = PreparationUiBuilderUtility.AddText(
                label,
                "查看拥有",
                27f,
                TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.92f, 0.94f, 0.98f, 1f);

            view.OwnedOnlyToggle = toggle;
            view.OwnedOnlyLabel = labelText;
        }

        private static void CreateFusionReveal(Transform parent, PreparationView view)
        {
            var questionFaceSprite = PreparationUiBuilderUtility.LoadSprite("FusionRevealQuestionFace");
            var cardBackSprite = PreparationUiBuilderUtility.LoadSprite("FusionRevealCardBack");
            var overlay = PreparationUiBuilderUtility.CreateUiObject("FusionRevealOverlay", parent);
            PreparationUiBuilderUtility.Stretch(overlay);
            var canvasGroup = overlay.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
            var dimmer = PreparationUiBuilderUtility.AddImage(overlay, null, true);
            dimmer.color = new Color(0.2f, 0.2f, 0.2f, 0.78f);
            var dismissButton = overlay.AddComponent<Button>();
            dismissButton.targetGraphic = dimmer;
            dismissButton.transition = Selectable.Transition.None;
            dismissButton.interactable = false;
            var dismissNavigation = dismissButton.navigation;
            dismissNavigation.mode = Navigation.Mode.None;
            dismissButton.navigation = dismissNavigation;

            var materialListObject = PreparationUiBuilderUtility.CreateUiObject(
                "MaterialCardList",
                overlay.transform);
            PreparationUiBuilderUtility.Stretch(materialListObject);
            var materialList = materialListObject.AddComponent<UiList>();
            materialList.ArragementType = UiList.EArrangement.Manual;

            var cardRoot = PreparationUiBuilderUtility.CreateUiObject("CardRoot", overlay.transform);
            PreparationUiBuilderUtility.SetRect(
                cardRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(FusionRevealCardWidth, FusionRevealCardHeight),
                Vector2.zero);

            var sealedFace = CreateFusionRevealFace(cardRoot.transform, "SealedFace", questionFaceSprite);
            var back = CreateFusionRevealFace(cardRoot.transform, "CardBack", cardBackSprite);
            back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var resultListObject = PreparationUiBuilderUtility.CreateUiObject("ResultCardList", cardRoot.transform);
            PreparationUiBuilderUtility.Stretch(resultListObject);
            var resultList = resultListObject.AddComponent<UiList>();
            resultList.ArragementType = UiList.EArrangement.Manual;

            var flash = PreparationUiBuilderUtility.CreateUiObject("ScreenFlash", overlay.transform);
            PreparationUiBuilderUtility.Stretch(flash);
            var flashImage = PreparationUiBuilderUtility.AddImage(flash, null);
            flashImage.color = Color.white;
            flashImage.raycastTarget = false;
            var flashCanvasGroup = flash.AddComponent<CanvasGroup>();
            flashCanvasGroup.alpha = 0f;
            flashCanvasGroup.interactable = false;
            flashCanvasGroup.blocksRaycasts = false;

            sealedFace.SetActive(true);
            back.SetActive(false);
            flash.SetActive(false);
            view.FusionRevealOverlay = overlay;
            view.FusionRevealCanvasGroup = canvasGroup;
            view.FusionRevealDismissButton = dismissButton;
            view.FusionRevealMaterialCardList = materialList;
            view.FusionRevealCardRoot = (RectTransform)cardRoot.transform;
            view.FusionRevealCardList = resultList;
            view.FusionRevealSealedFace = sealedFace;
            view.FusionRevealCardBack = back;
            view.FusionRevealFlash = (RectTransform)flash.transform;
            view.FusionRevealFlashCanvasGroup = flashCanvasGroup;
        }

        private static void CreateRewardReveal(Transform parent, PreparationView view)
        {
            var overlay = PreparationUiBuilderUtility.CreateUiObject("RewardRevealOverlay", parent);
            PreparationUiBuilderUtility.Stretch(overlay);
            var canvasGroup = overlay.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
            var dimmer = PreparationUiBuilderUtility.AddImage(overlay, null, true);
            dimmer.color = new Color(0.2f, 0.2f, 0.2f, 0.78f);
            var confirmButton = overlay.AddComponent<Button>();
            confirmButton.targetGraphic = dimmer;
            confirmButton.transition = Selectable.Transition.None;
            confirmButton.interactable = false;
            var confirmNavigation = confirmButton.navigation;
            confirmNavigation.mode = Navigation.Mode.None;
            confirmButton.navigation = confirmNavigation;

            var cardListObject = PreparationUiBuilderUtility.CreateUiObject(
                "RewardCardList",
                overlay.transform);
            PreparationUiBuilderUtility.Stretch(cardListObject);
            var cardList = cardListObject.AddComponent<UiList>();
            cardList.ArragementType = UiList.EArrangement.Manual;

            var title = PreparationUiBuilderUtility.CreateUiObject(
                "RewardTitle",
                overlay.transform);
            PreparationUiBuilderUtility.SetRect(
                title,
                new Vector2(0.5f, 0.5f),
                new Vector2(620f, 225f),
                new Vector2(0f, 270f));
            var titleImage = PreparationUiBuilderUtility.AddImage(
                title,
                PreparationUiBuilderUtility.LoadSprite("PreparationRewardTitle"));
            titleImage.preserveAspect = true;
            titleImage.raycastTarget = false;

            overlay.SetActive(false);
            view.RewardRevealOverlay = overlay;
            view.RewardRevealCanvasGroup = canvasGroup;
            view.RewardRevealConfirmButton = confirmButton;
            view.RewardRevealCardList = cardList;
        }

        private static GameObject CreateFusionRevealFace(Transform parent, string name, Sprite artwork)
        {
            var face = PreparationUiBuilderUtility.CreateUiObject(name, parent);
            PreparationUiBuilderUtility.Stretch(face);
            var background = PreparationUiBuilderUtility.AddImage(face, artwork);
            background.preserveAspect = false;
            background.raycastTarget = false;

            var border = PreparationUiBuilderUtility.CreateUiObject("Border", face.transform);
            PreparationUiBuilderUtility.Stretch(border);
            var borderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png");
            if (borderSprite == null)
                throw new System.InvalidOperationException("Shared battle card frame is missing.");
            var borderImage = PreparationUiBuilderUtility.AddImage(
                border,
                borderSprite);
            borderImage.color = new Color(0.9f, 0.72f, 0.3f, 1f);
            borderImage.raycastTarget = false;
            return face;
        }

        private static void CreatePoolPattern(Transform parent)
        {
            var patternRoot = PreparationUiBuilderUtility.CreateUiObject("BluePanelPattern", parent);
            PreparationUiBuilderUtility.SetRect(
                patternRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(1600f, 500f),
                Vector2.zero);

            var patternColor = new Color(0.72f, 0.88f, 1f, 0.075f);
            var diamondPositions = new[]
            {
                new Vector2(-600f, 150f),
                new Vector2(0f, 150f),
                new Vector2(600f, 150f),
                new Vector2(-600f, -150f),
                new Vector2(0f, -150f),
                new Vector2(600f, -150f),
            };
            for (var index = 0; index < diamondPositions.Length; index++)
            {
                CreateDiamondOutline(
                    patternRoot.transform,
                    $"Diamond{index + 1:00}",
                    diamondPositions[index],
                    patternColor);
            }

            var dotColor = new Color(0.72f, 0.88f, 1f, 0.05f);
            var dotPositions = new[]
            {
                new Vector2(-735f, 150f), new Vector2(-465f, 150f),
                new Vector2(-135f, 150f), new Vector2(135f, 150f),
                new Vector2(465f, 150f), new Vector2(735f, 150f),
                new Vector2(-735f, -150f), new Vector2(-465f, -150f),
                new Vector2(-135f, -150f), new Vector2(135f, -150f),
                new Vector2(465f, -150f), new Vector2(735f, -150f),
            };
            for (var index = 0; index < dotPositions.Length; index++)
            {
                var dot = PreparationUiBuilderUtility.CreateUiObject($"Dot{index + 1:00}", patternRoot.transform);
                PreparationUiBuilderUtility.SetRect(dot, new Vector2(0.5f, 0.5f), new Vector2(5f, 5f), dotPositions[index]);
                dot.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                var dotImage = PreparationUiBuilderUtility.AddImage(dot, null);
                dotImage.color = dotColor;
                dotImage.raycastTarget = false;
            }
        }

        private static void CreateDiamondOutline(
            Transform parent,
            string name,
            Vector2 position,
            Color color)
        {
            const float halfWidth = 38f;
            const float halfHeight = 23f;
            var diamond = PreparationUiBuilderUtility.CreateUiObject(name, parent);
            PreparationUiBuilderUtility.SetRect(
                diamond,
                new Vector2(0.5f, 0.5f),
                new Vector2(halfWidth * 2f, halfHeight * 2f),
                position);

            var sideLength = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
            var angle = Mathf.Atan2(halfHeight, halfWidth) * Mathf.Rad2Deg;
            CreatePatternLine(diamond.transform, "TopLeft", sideLength, new Vector2(-halfWidth * 0.5f, halfHeight * 0.5f), angle, color);
            CreatePatternLine(diamond.transform, "TopRight", sideLength, new Vector2(halfWidth * 0.5f, halfHeight * 0.5f), -angle, color);
            CreatePatternLine(diamond.transform, "BottomLeft", sideLength, new Vector2(-halfWidth * 0.5f, -halfHeight * 0.5f), -angle, color);
            CreatePatternLine(diamond.transform, "BottomRight", sideLength, new Vector2(halfWidth * 0.5f, -halfHeight * 0.5f), angle, color);
        }

        private static void CreatePatternLine(
            Transform parent,
            string name,
            float length,
            Vector2 position,
            float rotationZ,
            Color color)
        {
            var line = PreparationUiBuilderUtility.CreateUiObject(name, parent);
            PreparationUiBuilderUtility.SetRect(line, new Vector2(0.5f, 0.5f), new Vector2(length, 2f), position);
            line.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            var image = PreparationUiBuilderUtility.AddImage(line, null);
            image.color = color;
            image.raycastTarget = false;
        }

        private static void CreateScrollArrow(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 position,
            float rotationZ)
        {
            var arrow = PreparationUiBuilderUtility.CreateUiObject(name, parent);
            PreparationUiBuilderUtility.SetRect(arrow, new Vector2(0.5f, 0.5f), new Vector2(44f, 44f), position);
            arrow.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            PreparationUiBuilderUtility.AddImage(arrow, sprite);
        }
    }
}
