using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class FusionRecommendationItemUiBuilder
    {
        private const string PrefabPath =
            "Assets/Resources/Ui/FusionRecommendationItem.prefab";

        public static void Build()
        {
            var root = PreparationUiBuilderUtility.CreateUiObject(
                "FusionRecommendationItem",
                null);
            try
            {
                ((RectTransform)root.transform).sizeDelta = new Vector2(970f, 224f);
                var view = root.AddComponent<FusionRecommendationItemView>();

                var background = PreparationUiBuilderUtility.AddImage(root, null, false);
                background.color = new Color(0.22f, 0.105f, 0.035f, 0.18f);

                var cardListObject = PreparationUiBuilderUtility.CreateUiObject(
                    "CardList",
                    root.transform);
                PreparationUiBuilderUtility.SetRect(
                    cardListObject,
                    new Vector2(0f, 0.5f),
                    new Vector2(760f, 214f),
                    new Vector2(385f, 0f));
                var cardList = cardListObject.AddComponent<UiList>();
                cardList.ArragementType = UiList.EArrangement.ConstantSlot;
                cardList.ConstantSlotDirection = UiList.EDirection.Horizontal;
                cardList.ConstantSlotSize = new Vector2(180f, 214f);

                var selectObject = PreparationUiBuilderUtility.CreateUiObject(
                    "SelectButton",
                    root.transform);
                PreparationUiBuilderUtility.SetRect(
                    selectObject,
                    new Vector2(1f, 0.5f),
                    new Vector2(156f, 78f),
                    new Vector2(-92f, 0f));
                var selectButton = PreparationUiBuilderUtility.AddMedievalParchmentButton(
                    selectObject,
                    out _);
                var labelObject = PreparationUiBuilderUtility.CreateUiObject(
                    "Label",
                    selectObject.transform);
                PreparationUiBuilderUtility.Stretch(labelObject, 12f, 8f, 12f, 8f);
                var label = PreparationUiBuilderUtility.AddText(labelObject, "选择", 30f);
                label.fontStyle = FontStyles.Bold;
                label.color = new Color(0.16f, 0.075f, 0.025f, 1f);

                view.CardList = cardList;
                view.SelectButton = selectButton;
                PreparationUiBuilderUtility.SavePrefab(root, PrefabPath, true);
            }
            finally
            {
                PreparationUiBuilderUtility.DestroyTemporary(root);
            }
        }
    }
}
