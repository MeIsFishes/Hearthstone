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
                view.BattleSlotList = CreateBattleSlots(root.transform);
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
            PreparationUiBuilderUtility.SetRect(panel, new Vector2(0f, 1f), new Vector2(310f, 100f), new Vector2(205f, -170f));
            PreparationUiBuilderUtility.AddImage(panel, PreparationUiBuilderUtility.LoadSprite("PreparationRewardPanel"));
            var text = PreparationUiBuilderUtility.CreateUiObject("RewardText", panel.transform);
            PreparationUiBuilderUtility.Stretch(text, 15f, 10f, 15f, 10f);
            return PreparationUiBuilderUtility.AddText(text, "本轮获得 5 张卡", 30f);
        }

        private static UiList CreateBattleSlots(Transform parent)
        {
            var header = PreparationUiBuilderUtility.CreateUiObject("BattleSlotHeader", parent);
            PreparationUiBuilderUtility.SetRect(header, new Vector2(0.5f, 1f), new Vector2(800f, 55f), new Vector2(0f, -125f));
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

            var listObject = PreparationUiBuilderUtility.CreateUiObject("BattleSlotList", parent);
            PreparationUiBuilderUtility.SetRect(listObject, new Vector2(0.5f, 1f), new Vector2(720f, 330f), new Vector2(0f, -320f));
            var list = listObject.AddComponent<UiList>();
            list.ArragementType = UiList.EArrangement.ConstantSlot;
            list.ConstantSlotDirection = UiList.EDirection.Horizontal;
            list.ConstantSlotSize = new Vector2(240f, 330f);
            return list;
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

            var label = PreparationUiBuilderUtility.CreateUiObject("PoolTitle", panel.transform);
            PreparationUiBuilderUtility.SetRect(label, new Vector2(0.5f, 1f), new Vector2(400f, 55f), new Vector2(0f, -35f));
            PreparationUiBuilderUtility.AddText(label, "卡池 1-98", 36f);

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
