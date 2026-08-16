#if UNITY_EDITOR
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class CardCollectionViewUiBuilder
    {
        public const string PrefabPath = "Assets/Resources/Ui/CardCollectionView.prefab";
        private const float ScrollHeight = 810f;

        public static void Build()
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("CardCollectionView", null);
            try
            {
                ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
                var view = root.AddComponent<CardCollectionView>();
                view.DefaultShow = false;

                var background = PreparationUiBuilderUtility.CreateUiObject("Background", root.transform);
                PreparationUiBuilderUtility.Stretch(background);
                var backgroundImage = PreparationUiBuilderUtility.AddImage(
                    background,
                    PreparationUiBuilderUtility.LoadSprite("PreparationPageBackground"));
                backgroundImage.raycastTarget = false;

                CreateCardPool(root.transform, view);
                CreateHeader(root.transform, view);
                CreatePreview(root.transform, view);
                PreparationUiBuilderUtility.SavePrefab(root, PrefabPath, false);
            }
            finally
            {
                PreparationUiBuilderUtility.DestroyTemporary(root);
            }
        }

        private static void CreateHeader(Transform parent, CardCollectionView view)
        {
            var titleFrame = PreparationUiBuilderUtility.CreateUiObject("TitleFrame", parent);
            PreparationUiBuilderUtility.SetRect(titleFrame, new Vector2(0.5f, 1f), new Vector2(520f, 104f), new Vector2(0f, -58f));
            var titleImage = PreparationUiBuilderUtility.AddImage(
                titleFrame,
                PreparationUiBuilderUtility.LoadMedievalParchmentControlSprite());
            titleImage.preserveAspect = false;
            var titleRoot = PreparationUiBuilderUtility.CreateUiObject("Title", titleFrame.transform);
            PreparationUiBuilderUtility.Stretch(titleRoot, 30f, 18f, 30f, 18f);
            var title = PreparationUiBuilderUtility.AddText(titleRoot, "卡牌图鉴", 45f);
            title.color = new Color(0.19f, 0.14f, 0.10f, 1f);

            var countRoot = PreparationUiBuilderUtility.CreateUiObject("CollectedCount", parent);
            PreparationUiBuilderUtility.SetRect(countRoot, new Vector2(1f, 1f), new Vector2(360f, 86f), new Vector2(-205f, -65f));
            var countBackground = PreparationUiBuilderUtility.AddImage(
                countRoot,
                PreparationUiBuilderUtility.LoadMedievalParchmentControlSprite());
            countBackground.preserveAspect = false;
            countBackground.raycastTarget = false;
            countBackground.color = new Color(0.78f, 0.74f, 0.66f, 0.92f);
            var countLabelRoot = PreparationUiBuilderUtility.CreateUiObject("Label", countRoot.transform);
            PreparationUiBuilderUtility.Stretch(countLabelRoot, 32f, 12f, 48f, 12f);
            var count = PreparationUiBuilderUtility.AddText(countLabelRoot, "已解锁 0/0", 34f);
            count.alignment = TextAlignmentOptions.MidlineRight;
            count.color = new Color(0.22f, 0.16f, 0.10f, 1f);
            view.CollectedCountText = count;

            var backRoot = PreparationUiBuilderUtility.CreateUiObject("BackButton", parent);
            PreparationUiBuilderUtility.SetRect(backRoot, new Vector2(0f, 1f), new Vector2(220f, 86f), new Vector2(145f, -60f));
            var backButton = PreparationUiBuilderUtility.AddMedievalParchmentButton(backRoot, out _);
            var backLabelRoot = PreparationUiBuilderUtility.CreateUiObject("Label", backRoot.transform);
            PreparationUiBuilderUtility.Stretch(backLabelRoot, 25f, 12f, 25f, 12f);
            var backLabel = PreparationUiBuilderUtility.AddText(backLabelRoot, "返回", 32f);
            backLabel.color = new Color(0.19f, 0.14f, 0.10f, 1f);
            view.BackButton = backButton;
        }

        private static void CreateCardPool(Transform parent, CardCollectionView view)
        {
            var panel = PreparationUiBuilderUtility.CreateUiObject("CardPoolPanel", parent);
            PreparationUiBuilderUtility.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(1780f, 900f), new Vector2(0f, -55f));
            var panelImage = PreparationUiBuilderUtility.AddImage(
                panel,
                PreparationUiBuilderUtility.LoadSprite("PreparationCardPoolPanel"),
                false,
                Image.Type.Sliced);
            panelImage.raycastTarget = false;

            var scrollObject = PreparationUiBuilderUtility.CreateUiObject("ScrollRect", panel.transform);
            PreparationUiBuilderUtility.SetRect(scrollObject, new Vector2(0.5f, 0.5f), new Vector2(1660f, ScrollHeight), Vector2.zero);
            var scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 45f;

            var viewport = PreparationUiBuilderUtility.CreateUiObject("Viewport", scrollObject.transform);
            PreparationUiBuilderUtility.Stretch(viewport);
            var viewportImage = PreparationUiBuilderUtility.AddImage(viewport, null, true);
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewport.AddComponent<RectMask2D>();

            var content = PreparationUiBuilderUtility.CreateUiObject("Content", viewport.transform);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(
                PreparationUiBuilderUtility.CardPoolCellWidth * RunCardRules.CardsPerRow,
                PreparationUiBuilderUtility.CardPoolCellHeight);
            contentRect.anchoredPosition = Vector2.zero;
            var list = content.AddComponent<UiList>();
            list.ArragementType = UiList.EArrangement.ConstantSlot;
            list.ConstantSlotDirection = UiList.EDirection.Horizontal;
            list.ConstantSlotSize = new Vector2(
                PreparationUiBuilderUtility.CardPoolCellWidth,
                PreparationUiBuilderUtility.CardPoolCellHeight);

            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = contentRect;
            view.CardScrollRect = scrollRect;
            view.CardList = list;
        }

        private static void CreatePreview(Transform parent, CardCollectionView view)
        {
            var overlay = PreparationUiBuilderUtility.CreateUiObject("PreviewOverlay", parent);
            PreparationUiBuilderUtility.Stretch(overlay);
            var dim = PreparationUiBuilderUtility.AddImage(overlay, null, true);
            dim.color = new Color(0.05f, 0.045f, 0.04f, 0.76f);
            var dismissButton = overlay.AddComponent<Button>();
            dismissButton.targetGraphic = dim;
            dismissButton.transition = Selectable.Transition.None;
            dismissButton.navigation = new Navigation { mode = Navigation.Mode.None };

            var cardRoot = PreparationUiBuilderUtility.CreateUiObject("PreviewCardRoot", overlay.transform);
            PreparationUiBuilderUtility.SetRect(cardRoot, new Vector2(0.5f, 0.5f), new Vector2(250f, 360f), Vector2.zero);
            var cardList = cardRoot.AddComponent<UiList>();
            cardList.ArragementType = UiList.EArrangement.Manual;

            view.PreviewOverlay = overlay;
            view.PreviewDismissButton = dismissButton;
            view.PreviewCardRoot = (RectTransform)cardRoot.transform;
            view.PreviewCardList = cardList;
            overlay.SetActive(false);
        }
    }
}
#endif
