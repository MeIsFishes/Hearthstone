using BbxCommon.Ui;
using TMPro;
using UnityEngine;

namespace Hearthstone
{
    public static class PreparationFusionSlotItemUiBuilder
    {
        private const string PrefabPath = "Assets/Resources/Ui/PreparationFusionSlotItem.prefab";

        public static void Build()
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("PreparationFusionSlotItem", null);
            try
            {
                ((RectTransform)root.transform).sizeDelta = new Vector2(
                    190f,
                    190f * RunCardRules.CardAspectHeight / RunCardRules.CardAspectWidth);
                var hitArea = PreparationUiBuilderUtility.AddImage(root, null, true);
                hitArea.color = new Color(1f, 1f, 1f, 0.001f);
                var view = root.AddComponent<PreparationFusionSlotItemView>();

                var empty = PreparationUiBuilderUtility.CreateUiObject("EmptyState", root.transform);
                PreparationUiBuilderUtility.Stretch(empty, 4f, 4f, 4f, 4f);
                PreparationUiBuilderUtility.AddImage(
                    empty,
                    PreparationUiBuilderUtility.LoadSprite("PreparationFusionSlotFrame"));

                var occupied = PreparationUiBuilderUtility.CreateUiObject("OccupiedState", root.transform);
                PreparationUiBuilderUtility.Stretch(occupied, 4f, 4f, 4f, 4f);
                var artwork = PreparationUiBuilderUtility.CreateUiObject("Artwork", occupied.transform);
                PreparationUiBuilderUtility.Stretch(artwork, 16f, 68f, 16f, 22f);
                var artworkImage = PreparationUiBuilderUtility.AddImage(artwork, null);
                artworkImage.preserveAspect = true;
                var frame = PreparationUiBuilderUtility.CreateUiObject("CardFrame", occupied.transform);
                PreparationUiBuilderUtility.Stretch(frame);
                var frameImage = PreparationUiBuilderUtility.AddImage(
                    frame,
                    PreparationUiBuilderUtility.LoadExistingSprite(
                        "Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png"));
                var nameObject = PreparationUiBuilderUtility.CreateUiObject("Name", occupied.transform);
                PreparationUiBuilderUtility.SetRect(nameObject, new Vector2(0.5f, 0f), new Vector2(142f, 34f), new Vector2(0f, 54f));
                var nameText = PreparationUiBuilderUtility.AddText(nameObject, string.Empty, 20f);
                var keywordObject = PreparationUiBuilderUtility.CreateUiObject("Keywords", occupied.transform);
                PreparationUiBuilderUtility.SetRect(keywordObject, new Vector2(0.5f, 0f), new Vector2(150f, 30f), new Vector2(0f, 24f));
                var keywordText = PreparationUiBuilderUtility.AddText(keywordObject, string.Empty, 15f);
                keywordText.enableAutoSizing = true;
                keywordText.enableWordWrapping = false;
                keywordText.fontSizeMin = 8f;
                keywordText.fontSizeMax = 15f;
                var attack = CreateStat(occupied.transform, "Attack", new Vector2(1f, 0f), new Vector2(-30f, 30f),
                    "Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png");
                var health = CreateStat(occupied.transform, "Health", new Vector2(0f, 0f), new Vector2(30f, 30f),
                    "Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png");

                var badge = PreparationUiBuilderUtility.CreateUiObject("CardNumberBadge", occupied.transform);
                PreparationUiBuilderUtility.SetRect(badge, new Vector2(0f, 1f), new Vector2(62f, 42f), new Vector2(31f, -22f));
                PreparationUiBuilderUtility.AddImage(
                    badge,
                    PreparationUiBuilderUtility.LoadExistingSprite(
                        "Assets/Resources/Art/BattleCards/UI/CardNumberBadgeHex.png"));
                var numberObject = PreparationUiBuilderUtility.CreateUiObject("Number", badge.transform);
                PreparationUiBuilderUtility.Stretch(numberObject);
                var number = PreparationUiBuilderUtility.AddText(numberObject, "00", 21f);
                number.color = Color.white;

                var highlight = PreparationUiBuilderUtility.CreateUiObject("DropHighlight", root.transform);
                PreparationUiBuilderUtility.Stretch(highlight);
                var highlightImage = PreparationUiBuilderUtility.AddImage(
                    highlight,
                    PreparationUiBuilderUtility.LoadSprite("PreparationDropHighlight"));
                highlightImage.color = new Color(1f, 1f, 1f, 0.72f);
                highlight.SetActive(false);

                var dragable = root.AddComponent<UiDragable>();
                dragable.TurnBackWhenDragEnd = true;
                dragable.AlwaysRelativeOffset = false;
                var interactor = root.AddComponent<UiInteractor>();
                interactor.TransformOverride = root.transform;
                interactor.UiDragableRef = dragable;

                view.EmptyState = empty;
                view.OccupiedState = occupied;
                view.ArtworkArea = artworkImage;
                view.CardFrame = frameImage;
                view.DropHighlight = highlightImage;
                view.CardNumberText = number;
                view.NameText = nameText;
                view.KeywordText = keywordText;
                view.AttackText = attack;
                view.HealthText = health;
                view.Dragable = dragable;
                view.Interactor = interactor;
                PreparationUiBuilderUtility.SavePrefab(root, PrefabPath, false);
            }
            finally
            {
                PreparationUiBuilderUtility.DestroyTemporary(root);
            }
        }

        private static TextMeshProUGUI CreateStat(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            string spritePath)
        {
            var badge = PreparationUiBuilderUtility.CreateUiObject(name + "Badge", parent);
            PreparationUiBuilderUtility.SetRect(badge, anchor, new Vector2(52f, 52f), position);
            PreparationUiBuilderUtility.AddImage(badge, PreparationUiBuilderUtility.LoadExistingSprite(spritePath));
            var textObject = PreparationUiBuilderUtility.CreateUiObject(name + "Text", badge.transform);
            PreparationUiBuilderUtility.Stretch(textObject);
            var text = PreparationUiBuilderUtility.AddText(textObject, "0", 25f);
            text.color = Color.white;
            return text;
        }
    }
}
