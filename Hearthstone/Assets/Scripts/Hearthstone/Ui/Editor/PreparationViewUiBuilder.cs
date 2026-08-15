using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class PreparationViewUiBuilder
    {
        private const string PrefabPath = "Assets/Resources/Ui/PreparationView.prefab";

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

                CreateTitle(root.transform);
                view.RewardText = CreateReward(root.transform);
                CreateContinue(root.transform, view);
                CreateTabs(root.transform, view);
                CreateBattleOperation(root.transform, view);
                CreateFusionOperation(root.transform, view);
                CreatePool(root.transform, view);

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
            PreparationUiBuilderUtility.SetRect(titleFrame, new Vector2(0.5f, 1f), new Vector2(580f, 100f), new Vector2(0f, -55f));
            PreparationUiBuilderUtility.AddImage(
                titleFrame,
                PreparationUiBuilderUtility.LoadSprite("PreparationStageTitleFrame"));
            var title = PreparationUiBuilderUtility.CreateUiObject("Title", titleFrame.transform);
            PreparationUiBuilderUtility.Stretch(title);
            PreparationUiBuilderUtility.AddText(title, "备战阶段", 50f);
        }

        private static TextMeshProUGUI CreateReward(Transform parent)
        {
            var panel = PreparationUiBuilderUtility.CreateUiObject("RewardPanel", parent);
            PreparationUiBuilderUtility.SetRect(panel, new Vector2(0f, 1f), new Vector2(310f, 90f), new Vector2(200f, -135f));
            PreparationUiBuilderUtility.AddImage(panel, PreparationUiBuilderUtility.LoadSprite("PreparationRewardPanel"));
            var text = PreparationUiBuilderUtility.CreateUiObject("RewardText", panel.transform);
            PreparationUiBuilderUtility.Stretch(text, 15f, 10f, 15f, 10f);
            return PreparationUiBuilderUtility.AddText(text, "本轮获得 5 张卡", 30f);
        }

        private static void CreateContinue(Transform parent, PreparationView view)
        {
            var idle = PreparationUiBuilderUtility.LoadSprite("PreparationContinueButtonIdle");
            var highlighted = PreparationUiBuilderUtility.LoadSprite("PreparationContinueButtonHighlighted");
            var pressed = PreparationUiBuilderUtility.LoadSprite("PreparationContinueButtonPressed");
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
            button.spriteState = new SpriteState
            {
                highlightedSprite = highlighted,
                pressedSprite = pressed,
                selectedSprite = highlighted,
                disabledSprite = waiting,
            };

            var mainLabel = PreparationUiBuilderUtility.CreateUiObject("MainLabel", root.transform);
            PreparationUiBuilderUtility.SetRect(
                mainLabel,
                new Vector2(0.5f, 0.5f),
                new Vector2(240f, 50f),
                new Vector2(0f, 19f));
            var mainText = PreparationUiBuilderUtility.AddText(mainLabel, "继续", 36f);

            var auxiliaryLabel = PreparationUiBuilderUtility.CreateUiObject("AuxiliaryLabel", root.transform);
            PreparationUiBuilderUtility.SetRect(
                auxiliaryLabel,
                new Vector2(0.5f, 0.5f),
                new Vector2(240f, 34f),
                new Vector2(0f, -27f));
            var auxiliaryText = PreparationUiBuilderUtility.AddText(auxiliaryLabel, "下一关", 22f);
            auxiliaryText.color = new Color(1f, 0.82f, 0.45f, 0.95f);

            var blocker = PreparationUiBuilderUtility.CreateUiObject("ContinueWaitingInputBlocker", root.transform);
            PreparationUiBuilderUtility.Stretch(blocker);
            var blockerImage = PreparationUiBuilderUtility.AddImage(blocker, null, true);
            blockerImage.color = new Color(1f, 1f, 1f, 0.001f);
            var attemptListener = blocker.AddComponent<UiEventListener>();
            blocker.SetActive(false);

            view.ContinueButton = button;
            view.ContinueButtonImage = image;
            view.ContinueMainText = mainText;
            view.ContinueAuxiliaryText = auxiliaryText;
            view.ContinueWaitingInputBlocker = blocker;
            view.ContinueWaitingAttemptListener = attemptListener;
        }

        private static void CreateTabs(Transform parent, PreparationView view)
        {
            var idleSprite = PreparationUiBuilderUtility.LoadSprite("PreparationTabIdle");
            var selectedSprite = PreparationUiBuilderUtility.LoadSprite("PreparationTabSelected");
            view.BattleTabButton = CreateTab(
                parent,
                "BattleTab",
                "出战",
                new Vector2(-215f, -135f),
                selectedSprite,
                out var battleImage);
            view.FusionTabButton = CreateTab(
                parent,
                "FusionTab",
                "融合",
                new Vector2(215f, -135f),
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
            PreparationUiBuilderUtility.SetRect(tab, new Vector2(0.5f, 1f), new Vector2(400f, 76f), position);
            image = PreparationUiBuilderUtility.AddImage(
                tab,
                sprite,
                true);
            var button = tab.AddComponent<Button>();
            button.targetGraphic = image;
            var text = PreparationUiBuilderUtility.CreateUiObject("Label", tab.transform);
            PreparationUiBuilderUtility.Stretch(text, 20f, 8f, 20f, 8f);
            PreparationUiBuilderUtility.AddText(text, label, 32f);
            return button;
        }

        private static void CreateBattleOperation(Transform parent, PreparationView view)
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("BattleOperation", parent);
            PreparationUiBuilderUtility.SetRect(root, new Vector2(0.5f, 1f), new Vector2(1200f, 330f), new Vector2(0f, -300f));
            var header = PreparationUiBuilderUtility.CreateUiObject("BattleSlotHeader", root.transform);
            PreparationUiBuilderUtility.SetRect(header, new Vector2(0.5f, 1f), new Vector2(800f, 50f), new Vector2(0f, -70f));
            var lineSprite = PreparationUiBuilderUtility.LoadSprite("PreparationSectionLine");
            var left = PreparationUiBuilderUtility.CreateUiObject("LeftLine", header.transform);
            PreparationUiBuilderUtility.SetRect(left, new Vector2(0f, 0.5f), new Vector2(280f, 55f), new Vector2(140f, 0f));
            PreparationUiBuilderUtility.AddImage(left, lineSprite).preserveAspect = false;
            var right = PreparationUiBuilderUtility.CreateUiObject("RightLine", header.transform);
            PreparationUiBuilderUtility.SetRect(right, new Vector2(1f, 0.5f), new Vector2(280f, 55f), new Vector2(-140f, 0f));
            PreparationUiBuilderUtility.AddImage(right, lineSprite).preserveAspect = false;
            var text = PreparationUiBuilderUtility.CreateUiObject("Label", header.transform);
            PreparationUiBuilderUtility.SetRect(text, new Vector2(0.5f, 0.5f), new Vector2(220f, 55f), Vector2.zero);
            PreparationUiBuilderUtility.AddText(text, "战斗槽位", 34f);

            var listObject = PreparationUiBuilderUtility.CreateUiObject("BattleSlotList", root.transform);
            PreparationUiBuilderUtility.SetRect(listObject, new Vector2(0.5f, 1f), new Vector2(720f, 285f), new Vector2(0f, -180f));
            var list = listObject.AddComponent<UiList>();
            list.ArragementType = UiList.EArrangement.ConstantSlot;
            list.ConstantSlotDirection = UiList.EDirection.Horizontal;
            list.ConstantSlotSize = new Vector2(240f, 330f);
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
            PreparationUiBuilderUtility.SetRect(title, new Vector2(0f, 1f), new Vector2(850f, 46f), new Vector2(425f, -70f));
            PreparationUiBuilderUtility.AddText(title, "融合素材", 32f);

            var listObject = PreparationUiBuilderUtility.CreateUiObject("FusionSlotList", root.transform);
            PreparationUiBuilderUtility.SetRect(listObject, new Vector2(0f, 1f), new Vector2(800f, 285f), new Vector2(420f, -180f));
            var list = listObject.AddComponent<UiList>();
            list.ArragementType = UiList.EArrangement.ConstantSlot;
            list.ConstantSlotDirection = UiList.EDirection.Horizontal;
            list.ConstantSlotSize = new Vector2(200f, 285f);

            var sumPanel = PreparationUiBuilderUtility.CreateUiObject("FusionSumPanel", root.transform);
            PreparationUiBuilderUtility.SetRect(sumPanel, new Vector2(1f, 0.5f), new Vector2(480f, 230f), new Vector2(-250f, 0f));
            PreparationUiBuilderUtility.AddImage(
                sumPanel,
                PreparationUiBuilderUtility.LoadSprite("PreparationFusionSumPanel"));
            var expression = PreparationUiBuilderUtility.CreateUiObject("Expression", sumPanel.transform);
            PreparationUiBuilderUtility.SetRect(expression, new Vector2(0.5f, 1f), new Vector2(420f, 50f), new Vector2(0f, -50f));
            var expressionText = PreparationUiBuilderUtility.AddText(expression, "0", 28f);
            var result = PreparationUiBuilderUtility.CreateUiObject("Result", sumPanel.transform);
            PreparationUiBuilderUtility.SetRect(result, new Vector2(0.5f, 1f), new Vector2(420f, 50f), new Vector2(0f, -98f));
            var resultText = PreparationUiBuilderUtility.AddText(result, "合计 0 / 99", 27f);
            view.FusionUnderTargetColor = new Color(1f, 0.76f, 0.28f, 1f);
            view.FusionExactTargetColor = new Color(0.42f, 1f, 0.48f, 1f);
            view.FusionOverTargetColor = new Color(1f, 0.32f, 0.27f, 1f);

            var buttonObject = PreparationUiBuilderUtility.CreateUiObject("FusionButton", sumPanel.transform);
            PreparationUiBuilderUtility.SetRect(buttonObject, new Vector2(0.5f, 0f), new Vector2(320f, 80f), new Vector2(0f, 48f));
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
            PreparationUiBuilderUtility.AddText(buttonLabel, "融合", 34f);
            var attemptListener = buttonObject.AddComponent<UiEventListener>();

            view.FusionOperationRoot = root;
            view.FusionSlotList = list;
            view.FusionExpressionText = expressionText;
            view.FusionResultText = resultText;
            view.FusionButton = button;
            view.FusionButtonImage = buttonImage;
            view.FusionButtonAttemptListener = attemptListener;
            view.FusionAreaInteractor = areaInteractor;
        }

        private static void CreatePool(Transform parent, PreparationView view)
        {
            var panel = PreparationUiBuilderUtility.CreateUiObject("CardPoolPanel", parent);
            PreparationUiBuilderUtility.SetRect(panel, new Vector2(0.5f, 0f), new Vector2(1640f, 600f), new Vector2(0f, 305f));
            PreparationUiBuilderUtility.AddImage(
                panel,
                PreparationUiBuilderUtility.LoadSprite("PreparationCardPoolPanel"),
                false,
                Image.Type.Sliced);
            var poolInteractor = panel.AddComponent<UiInteractor>();
            poolInteractor.TransformOverride = panel.transform;
            poolInteractor.AutoInitUiDragable = false;

            var label = PreparationUiBuilderUtility.CreateUiObject("PoolTitle", panel.transform);
            PreparationUiBuilderUtility.SetRect(label, new Vector2(0.5f, 1f), new Vector2(400f, 55f), new Vector2(0f, -35f));
            PreparationUiBuilderUtility.AddText(label, "卡池 1-99", 36f);

            var scrollObject = PreparationUiBuilderUtility.CreateUiObject("ScrollRect", panel.transform);
            PreparationUiBuilderUtility.SetRect(scrollObject, new Vector2(0.5f, 0.5f), new Vector2(1510f, 500f), new Vector2(0f, -25f));
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
                new Vector2(PreparationUiBuilderUtility.CardPoolViewportWidth, 500f),
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
                new Vector2(46f, 500f),
                new Vector2(708f, 0f));
            var scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var trackVisual = PreparationUiBuilderUtility.CreateUiObject("TrackVisual", scrollbarObject.transform);
            PreparationUiBuilderUtility.SetRect(trackVisual, new Vector2(0.5f, 0.5f), new Vector2(46f, 500f), Vector2.zero);
            var trackImage = PreparationUiBuilderUtility.AddImage(
                trackVisual,
                PreparationUiBuilderUtility.LoadSprite("PreparationScrollTrack"));
            trackImage.preserveAspect = false;
            trackImage.color = new Color(1f, 1f, 1f, 0.85f);

            var arrowSprite = PreparationUiBuilderUtility.LoadSprite("PreparationScrollArrow");
            CreateScrollArrow(scrollbarObject.transform, "UpArrow", arrowSprite, new Vector2(0f, 238f), 180f);
            CreateScrollArrow(scrollbarObject.transform, "DownArrow", arrowSprite, new Vector2(0f, -238f), 0f);

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
