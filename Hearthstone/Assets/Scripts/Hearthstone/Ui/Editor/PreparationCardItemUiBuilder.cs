using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class PreparationCardItemUiBuilder
    {
        private const string PrefabPath = "Assets/Resources/Ui/PreparationCardItem.prefab";

        public static void Build()
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("PreparationCardItem", null);
            try
            {
                var rootRect = (RectTransform)root.transform;
                rootRect.sizeDelta = new Vector2(
                    PreparationUiBuilderUtility.CardPoolCellWidth,
                    PreparationUiBuilderUtility.CardPoolCellHeight);
                var hitArea = PreparationUiBuilderUtility.AddImage(root, null, true);
                hitArea.color = new Color(1f, 1f, 1f, 0.001f);
                var view = root.AddComponent<PreparationCardItemView>();

                var empty = PreparationUiBuilderUtility.CreateUiObject("EmptyState", root.transform);
                PreparationUiBuilderUtility.Stretch(empty, 4f, 4f, 4f, 4f);
                PreparationUiBuilderUtility.AddImage(
                    empty,
                    PreparationUiBuilderUtility.LoadSprite("PreparationPoolEmptySlot"));

                var owned = PreparationUiBuilderUtility.CreateUiObject("OwnedState", root.transform);
                PreparationUiBuilderUtility.Stretch(owned, 4f, 4f, 4f, 4f);
                var artwork = PreparationUiBuilderUtility.CreateUiObject("Artwork", owned.transform);
                PreparationUiBuilderUtility.Stretch(artwork, 13f, 58f, 13f, 18f);
                var artworkImage = PreparationUiBuilderUtility.AddImage(artwork, null);
                artworkImage.preserveAspect = true;

                var frame = PreparationUiBuilderUtility.CreateUiObject("CardFrame", owned.transform);
                PreparationUiBuilderUtility.Stretch(frame);
                var frameImage = PreparationUiBuilderUtility.AddImage(
                    frame,
                    PreparationUiBuilderUtility.LoadExistingSprite(
                        "Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png"));

                var nameObject = PreparationUiBuilderUtility.CreateUiObject("Name", owned.transform);
                PreparationUiBuilderUtility.SetRect(nameObject, new Vector2(0.5f, 0f), new Vector2(120f, 30f), new Vector2(0f, 45f));
                var nameText = PreparationUiBuilderUtility.AddText(nameObject, string.Empty, 18f);

                var attack = CreateStat(owned.transform, "Attack", new Vector2(1f, 0f), new Vector2(-25f, 25f),
                    "Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png");
                var health = CreateStat(owned.transform, "Health", new Vector2(0f, 0f), new Vector2(25f, 25f),
                    "Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png");

                var badge = PreparationUiBuilderUtility.CreateUiObject("CardNumberBadge", root.transform);
                PreparationUiBuilderUtility.SetRect(badge, new Vector2(0f, 1f), new Vector2(58f, 38f), new Vector2(28f, -19f));
                var badgeImage = PreparationUiBuilderUtility.AddImage(
                    badge,
                    PreparationUiBuilderUtility.LoadExistingSprite(
                        "Assets/Resources/Art/BattleCards/UI/CardNumberBadgeHex.png"));
                var numberText = PreparationUiBuilderUtility.CreateUiObject("Number", badge.transform);
                PreparationUiBuilderUtility.Stretch(numberText);
                var number = PreparationUiBuilderUtility.AddText(numberText, "01", 20f);
                number.color = Color.white;

                var dragable = root.AddComponent<UiDragable>();
                dragable.TurnBackWhenDragEnd = true;
                dragable.AlwaysRelativeOffset = false;
                var interactor = root.AddComponent<UiInteractor>();
                interactor.TransformOverride = root.transform;
                interactor.UiDragableRef = dragable;

                view.EmptyState = empty;
                view.OwnedState = owned;
                view.ArtworkArea = artworkImage;
                view.CardFrame = frameImage;
                view.CardNumberBadge = badgeImage;
                view.CardNumberText = number;
                view.NameText = nameText;
                view.AttackText = attack;
                view.HealthText = health;
                view.Dragable = dragable;
                view.Interactor = interactor;

                PreparationUiBuilderUtility.SavePrefab(root, PrefabPath, true);
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
            PreparationUiBuilderUtility.SetRect(badge, anchor, new Vector2(46f, 46f), position);
            PreparationUiBuilderUtility.AddImage(
                badge,
                PreparationUiBuilderUtility.LoadExistingSprite(spritePath));
            var textObject = PreparationUiBuilderUtility.CreateUiObject(name + "Text", badge.transform);
            PreparationUiBuilderUtility.Stretch(textObject);
            var text = PreparationUiBuilderUtility.AddText(textObject, "0", 22f);
            text.color = Color.white;
            return text;
        }
    }
}
