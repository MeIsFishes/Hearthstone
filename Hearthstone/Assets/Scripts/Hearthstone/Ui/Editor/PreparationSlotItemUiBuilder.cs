using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class PreparationSlotItemUiBuilder
    {
        private const string PrefabPath = "Assets/Resources/Ui/PreparationSlotItem.prefab";

        public static void Build()
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("PreparationSlotItem", null);
            try
            {
                ((RectTransform)root.transform).sizeDelta = new Vector2(
                    220f,
                    220f * RunCardRules.CardAspectHeight / RunCardRules.CardAspectWidth);
                var hitArea = PreparationUiBuilderUtility.AddImage(root, null, true);
                hitArea.color = new Color(1f, 1f, 1f, 0.001f);
                var view = root.AddComponent<PreparationSlotItemView>();

                var empty = PreparationUiBuilderUtility.CreateUiObject("EmptyState", root.transform);
                PreparationUiBuilderUtility.Stretch(empty, 5f, 5f, 5f, 5f);
                PreparationUiBuilderUtility.AddImage(
                    empty,
                    PreparationUiBuilderUtility.LoadSprite("PreparationBattleSlotFrame"));

                var occupied = PreparationUiBuilderUtility.CreateUiObject("OccupiedState", root.transform);
                PreparationUiBuilderUtility.Stretch(occupied, 5f, 5f, 5f, 5f);
                var artwork = PreparationUiBuilderUtility.CreateUiObject("Artwork", occupied.transform);
                PreparationUiBuilderUtility.Stretch(artwork, 18f, 78f, 18f, 25f);
                var artworkImage = PreparationUiBuilderUtility.AddImage(artwork, null);
                artworkImage.preserveAspect = true;
                var frame = PreparationUiBuilderUtility.CreateUiObject("CardFrame", occupied.transform);
                PreparationUiBuilderUtility.Stretch(frame);
                var frameImage = PreparationUiBuilderUtility.AddImage(
                    frame,
                    PreparationUiBuilderUtility.LoadExistingSprite(
                        "Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png"));
                var nameObject = PreparationUiBuilderUtility.CreateUiObject("Name", occupied.transform);
                PreparationUiBuilderUtility.SetRect(nameObject, new Vector2(0.5f, 0f), new Vector2(165f, 38f), new Vector2(0f, 62f));
                var nameText = PreparationUiBuilderUtility.AddText(nameObject, string.Empty, 23f);
                var keywordObject = PreparationUiBuilderUtility.CreateUiObject("Keywords", occupied.transform);
                PreparationUiBuilderUtility.SetRect(keywordObject, new Vector2(0.5f, 0f), new Vector2(170f, 34f), new Vector2(0f, 27f));
                var keywordText = PreparationUiBuilderUtility.AddText(keywordObject, string.Empty, 16f);
                keywordText.enableAutoSizing = true;
                keywordText.enableWordWrapping = false;
                keywordText.fontSizeMin = 9f;
                keywordText.fontSizeMax = 16f;
                var attack = CreateStat(occupied.transform, "Attack", new Vector2(1f, 0f), new Vector2(-34f, 34f),
                    "Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png");
                var health = CreateStat(occupied.transform, "Health", new Vector2(0f, 0f), new Vector2(34f, 34f),
                    "Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png");

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
            PreparationUiBuilderUtility.SetRect(badge, anchor, new Vector2(60f, 60f), position);
            PreparationUiBuilderUtility.AddImage(badge, PreparationUiBuilderUtility.LoadExistingSprite(spritePath));
            var textObject = PreparationUiBuilderUtility.CreateUiObject(name + "Text", badge.transform);
            PreparationUiBuilderUtility.Stretch(textObject);
            var text = PreparationUiBuilderUtility.AddText(textObject, "0", 28f);
            text.color = Color.white;
            return text;
        }
    }
}
