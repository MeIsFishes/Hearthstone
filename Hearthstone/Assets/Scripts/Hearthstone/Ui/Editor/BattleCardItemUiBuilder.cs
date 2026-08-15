using System;
using BbxCommon.Ui;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class BattleCardItemUiBuilder
    {
        private const string PrefabPath = "Assets/Resources/Ui/BattleCardItem.prefab";
        private const string FullCardFramePath =
            "Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png";
        private const string AttackBadgePath =
            "Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png";
        private const string HealthBadgePath =
            "Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png";

        public static void Build()
        {
            var fullCardFrame = AssetDatabase.LoadAssetAtPath<Sprite>(FullCardFramePath);
            if (fullCardFrame == null)
                throw new InvalidOperationException($"Full-card battle frame is missing at '{FullCardFramePath}'.");
            var attackBadge = LoadSprite(AttackBadgePath, "Attack badge");
            var healthBadge = LoadSprite(HealthBadgePath, "Health badge");

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var view = root.GetComponent<BattleCardItemView>();
                if (view == null)
                    throw new InvalidOperationException("BattleCardItemView is missing from the card prefab root.");

                ConfigureFrame(view.CardFrame, fullCardFrame, "CardFrameOverlay");
                ConfigureFrame(view.AttackerHighlight, fullCardFrame, "AttackerHighlight");
                ConfigureFrame(view.TargetHighlight, fullCardFrame, "TargetHighlight");
                ConfigureArtwork(view.ArtworkArea);
                ConfigureKeywordArea(view);
                ConfigureBadge(view.HealthText, healthBadge, Vector2.zero, new Vector2(30f, 30f), "HealthBadge");
                ConfigureBadge(view.AttackText, attackBadge, new Vector2(1f, 0f), new Vector2(-30f, 30f), "AttackBadge");
                ConfigurePreparationFeatures(root, view);
                ConfigureLayerOrder(view);

                UiApi.EditorOperation.PreInitializeView(view);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureKeywordArea(BattleCardItemView view)
        {
            if (view.SkillDescriptionText == null)
                throw new InvalidOperationException("Battle card name text reference is missing.");

            var area = view.SkillDescriptionText.transform.parent;
            var keywordTransform = area.Find("KeywordText") as RectTransform;
            TextMeshProUGUI keywordText;
            if (keywordTransform == null)
            {
                var keywordObject = new GameObject("KeywordText", typeof(RectTransform), typeof(TextMeshProUGUI));
                keywordTransform = (RectTransform)keywordObject.transform;
                keywordTransform.SetParent(area, false);
                keywordText = keywordObject.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                keywordText = keywordTransform.GetComponent<TextMeshProUGUI>();
            }

            var nameRect = view.SkillDescriptionText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(10f, 0f);
            nameRect.offsetMax = new Vector2(-10f, -3f);
            view.SkillDescriptionText.alignment = TextAlignmentOptions.Center;

            keywordTransform.anchorMin = Vector2.zero;
            keywordTransform.anchorMax = new Vector2(1f, 0.5f);
            keywordTransform.offsetMin = new Vector2(8f, 2f);
            keywordTransform.offsetMax = new Vector2(-8f, 0f);
            keywordText.font = view.SkillDescriptionText.font;
            keywordText.fontSharedMaterial = view.SkillDescriptionText.fontSharedMaterial != null
                ? view.SkillDescriptionText.fontSharedMaterial
                : view.SkillDescriptionText.font.material;
            keywordText.text = string.Empty;
            keywordText.fontSize = 13f;
            keywordText.enableAutoSizing = true;
            keywordText.enableWordWrapping = false;
            keywordText.fontSizeMin = 8f;
            keywordText.fontSizeMax = 13f;
            keywordText.alignment = TextAlignmentOptions.Center;
            keywordText.color = view.SkillDescriptionText.color;
            keywordText.raycastTarget = false;
            view.KeywordText = keywordText;
        }

        private static void ConfigureFrame(Image image, Sprite sprite, string objectName)
        {
            if (image == null)
                throw new InvalidOperationException($"{objectName} image reference is missing.");

            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private static void ConfigureArtwork(Image artwork)
        {
            if (artwork == null)
                throw new InvalidOperationException("ArtworkArea image reference is missing.");

            artwork.type = Image.Type.Simple;
            artwork.preserveAspect = true;
            artwork.raycastTarget = false;
        }

        private static void ConfigureLayerOrder(BattleCardItemView view)
        {
            view.CardFrame.transform.SetAsLastSibling();
            view.AttackerHighlight.transform.SetAsLastSibling();
            view.TargetHighlight.transform.SetAsLastSibling();
            if (view.DeadOverlay != null)
                view.DeadOverlay.transform.SetAsLastSibling();
            if (view.PreparationEmptyState != null)
                view.PreparationEmptyState.transform.SetAsLastSibling();
            view.HealthText.transform.parent.SetAsLastSibling();
            view.AttackText.transform.parent.SetAsLastSibling();
            view.CardNumberBadge.transform.SetAsLastSibling();
            if (view.PreparationMaterialSelectedState != null)
                view.PreparationMaterialSelectedState.transform.SetAsLastSibling();
        }

        private static void ConfigurePreparationFeatures(GameObject root, BattleCardItemView view)
        {
            view.CardBackground.raycastTarget = true;

            var emptyState = FindOrCreate(root.transform, "PreparationEmptyState");
            Stretch((RectTransform)emptyState.transform, 5f);
            var emptyImage = GetOrAdd<Image>(emptyState);
            emptyImage.sprite = LoadSprite(
                "Assets/Resources/Art/Preparation/UI/PreparationPoolEmptySlot.png",
                "Preparation pool empty slot");
            emptyImage.type = Image.Type.Simple;
            emptyImage.preserveAspect = true;
            emptyImage.raycastTarget = false;

            var emptyAttempt = FindOrCreate(emptyState.transform, "EmptyAttempt");
            Stretch((RectTransform)emptyAttempt.transform, 0f);
            var emptyAttemptImage = GetOrAdd<Image>(emptyAttempt);
            emptyAttemptImage.sprite = null;
            emptyAttemptImage.color = new Color(1f, 1f, 1f, 0.001f);
            emptyAttemptImage.raycastTarget = true;
            var emptyAttemptListener = GetOrAdd<UiEventListener>(emptyAttempt);

            var materialSelected = FindOrCreate(root.transform, "PreparationMaterialSelected");
            SetRect(
                (RectTransform)materialSelected.transform,
                new Vector2(1f, 1f),
                new Vector2(98f, 98f),
                new Vector2(-42.5f, -42.5f));
            var materialImage = GetOrAdd<Image>(materialSelected);
            materialImage.sprite = LoadSprite(
                "Assets/Resources/Art/Preparation/UI/PreparationMaterialSelected.png",
                "Preparation material selected marker");
            materialImage.type = Image.Type.Simple;
            materialImage.preserveAspect = true;
            materialImage.raycastTarget = false;

            var materialLabel = FindOrCreate(materialSelected.transform, "Label");
            Stretch((RectTransform)materialLabel.transform, 4f);
            var materialText = GetOrAdd<TextMeshProUGUI>(materialLabel);
            materialText.font = view.SkillDescriptionText.font;
            materialText.fontSharedMaterial = view.SkillDescriptionText.fontSharedMaterial;
            materialText.text = "素材\n已选";
            materialText.fontSize = 21f;
            materialText.fontStyle = FontStyles.Bold;
            materialText.alignment = TextAlignmentOptions.Center;
            materialText.color = Color.white;
            materialText.lineSpacing = -18f;
            materialText.raycastTarget = false;

            var dragable = GetOrAdd<UiDragable>(root);
            dragable.TurnBackWhenDragEnd = true;
            dragable.AlwaysRelativeOffset = false;
            var interactor = GetOrAdd<UiInteractor>(root);
            interactor.TransformOverride = root.transform;
            interactor.AutoInitUiDragable = true;
            interactor.UiDragableRef = dragable;

            emptyState.SetActive(false);
            materialSelected.SetActive(false);
            view.PreparationEmptyState = emptyState;
            view.PreparationMaterialSelectedState = materialSelected;
            view.PreparationDragable = dragable;
            view.PreparationInteractor = interactor;
            view.PreparationEmptyAttemptListener = emptyAttemptListener;
        }

        private static GameObject FindOrCreate(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
                return child.gameObject;
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            return component == null ? gameObject.AddComponent<T>() : component;
        }

        private static void Stretch(RectTransform rectTransform, float inset)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(inset, inset);
            rectTransform.offsetMax = new Vector2(-inset, -inset);
            rectTransform.localScale = Vector3.one;
        }

        private static void SetRect(
            RectTransform rectTransform,
            Vector2 anchor,
            Vector2 size,
            Vector2 anchoredPosition)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localScale = Vector3.one;
        }

        private static void ConfigureBadge(
            TMP_Text text,
            Sprite sprite,
            Vector2 anchor,
            Vector2 anchoredPosition,
            string objectName)
        {
            if (text == null)
                throw new InvalidOperationException($"{objectName} text reference is missing.");

            var image = text.transform.parent.GetComponent<Image>();
            if (image == null)
                throw new InvalidOperationException($"{objectName} image is missing from the text parent.");

            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(60f, 60f);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;

            text.fontSize = 30f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = false;
            text.raycastTarget = false;

            var outline = text.GetComponent<Outline>();
            if (outline == null)
                outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.04f, 0.02f, 0.01f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
        }

        private static Sprite LoadSprite(string path, string label)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"{label} texture importer is missing at '{path}'.");

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.alphaIsTransparency == false ||
                importer.mipmapEnabled ||
                importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException($"{label} is missing at '{path}'.");
            return sprite;
        }
    }
}
