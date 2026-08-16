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
        private const string TauntShieldOutlinePath =
            "Assets/Resources/Art/BattleCards/UI/TauntShieldOutline.png";
        private const string DamageNumberBurstPath =
            "Assets/Resources/Art/BattleCards/UI/DamageNumberBurst.png";
        private const string ChargeHornIconPath =
            "Assets/Resources/Art/BattleCards/UI/ChargeHornIcon.png";
        private const string LongShotBowIconPath =
            "Assets/Resources/Art/BattleCards/UI/LongShotBowIcon.png";
        private const string KeywordTooltipBackgroundPath =
            "Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png";
        private const string AttackBadgePath =
            "Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png";
        private const string HealthBadgePath =
            "Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png";
        private const string PreparationDeployedStatePath =
            "Assets/Resources/Art/Preparation/UI/PreparationDeployedText.png";
        private const string ChineseFontAssetPath =
            "Assets/Resources/Fonts/NotoSansSC-SemiBold Dynamic SDF.asset";
        private const float FrameBottomInset = 24f;
        private static readonly Vector2 ArtworkRenderSize = new Vector2(210f, 297f);
        private static readonly Vector2 TauntShieldRenderSize = new Vector2(292f, 408f);
        private static readonly Vector2 TauntShieldRenderPosition = new Vector2(0f, -14f);

        public static void Build()
        {
            var fullCardFrame = AssetDatabase.LoadAssetAtPath<Sprite>(FullCardFramePath);
            if (fullCardFrame == null)
                throw new InvalidOperationException($"Full-card battle frame is missing at '{FullCardFramePath}'.");
            var tauntShieldOutline = LoadSprite(TauntShieldOutlinePath, "Taunt shield outline");
            var damageNumberBurst = LoadSprite(DamageNumberBurstPath, "Damage number burst");
            var chargeHornIcon = LoadSprite(ChargeHornIconPath, "Charge horn icon");
            var longShotBowIcon = LoadSprite(LongShotBowIconPath, "Long shot bow icon");
            var keywordTooltipBackground = LoadSprite(
                KeywordTooltipBackgroundPath,
                "Keyword tooltip wooden background");
            var attackBadge = LoadSprite(AttackBadgePath, "Attack badge");
            var healthBadge = LoadSprite(HealthBadgePath, "Health badge");
            var chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontAssetPath);
            if (chineseFont == null)
                throw new InvalidOperationException($"Chinese font is missing at '{ChineseFontAssetPath}'.");

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var view = root.GetComponent<BattleCardItemView>();
                if (view == null)
                    throw new InvalidOperationException("BattleCardItemView is missing from the card prefab root.");

                ConfigureFont(root, chineseFont);
                ConfigureTauntShield(root, view, tauntShieldOutline);
                ConfigureFrame(view.CardFrame, fullCardFrame, "CardFrameOverlay");
                ConfigureFrame(view.AttackerHighlight, fullCardFrame, "AttackerHighlight");
                ConfigureFrame(view.TargetHighlight, fullCardFrame, "TargetHighlight");
                ConfigureArtwork(view.ArtworkArea);
                ConfigureKeywordArea(view);
                ConfigureKeywordTooltip(root, view, keywordTooltipBackground);
                ConfigureCardBasePattern(view);
                ConfigureBadge(view.HealthText, healthBadge, Vector2.zero, new Vector2(30f, 30f), "HealthBadge");
                ConfigureBadge(view.AttackText, attackBadge, new Vector2(1f, 0f), new Vector2(-30f, 30f), "AttackBadge");
                ConfigureFeedbackLayers(
                    root,
                    view,
                    damageNumberBurst,
                    chargeHornIcon,
                    longShotBowIcon);
                ConfigureHoverInput(root, view);
                ConfigurePreparationFeatures(root, view);
                ConfigureLayerOrder(view);

                UiApi.EditorOperation.PreInitializeView(view);
                ConfigurePreparationInteractionDefaults(view);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureFont(GameObject root, TMP_FontAsset font)
        {
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = font;
                label.fontSharedMaterial = font.material;
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
            view.SkillDescriptionText.fontSize = 16f;
            view.SkillDescriptionText.enableAutoSizing = true;
            view.SkillDescriptionText.enableWordWrapping = false;
            view.SkillDescriptionText.fontSizeMin = 11f;
            view.SkillDescriptionText.fontSizeMax = 18f;

            keywordTransform.anchorMin = Vector2.zero;
            keywordTransform.anchorMax = new Vector2(1f, 0.5f);
            keywordTransform.offsetMin = new Vector2(8f, 2f);
            keywordTransform.offsetMax = new Vector2(-8f, 0f);
            keywordText.font = view.SkillDescriptionText.font;
            keywordText.fontSharedMaterial = view.SkillDescriptionText.fontSharedMaterial != null
                ? view.SkillDescriptionText.fontSharedMaterial
                : view.SkillDescriptionText.font.material;
            keywordText.text = string.Empty;
            keywordText.fontSize = 17f;
            keywordText.enableAutoSizing = true;
            keywordText.enableWordWrapping = true;
            keywordText.fontSizeMin = 10f;
            keywordText.fontSizeMax = 17f;
            keywordText.overflowMode = TextOverflowModes.Overflow;
            keywordText.alignment = TextAlignmentOptions.Top;
            keywordText.color = view.SkillDescriptionText.color;
            keywordText.raycastTarget = false;
            view.KeywordText = keywordText;
        }

        private static void ConfigureKeywordTooltip(
            GameObject root,
            BattleCardItemView view,
            Sprite woodenBackground)
        {
            var tooltipObject = FindOrCreate(root.transform, "KeywordTooltip");
            SetRect(
                (RectTransform)tooltipObject.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(368f, 156f),
                new Vector2(318f, 32f));
            var background = GetOrAdd<Image>(tooltipObject);
            background.sprite = woodenBackground;
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            background.color = new Color32(184, 137, 83, 255);
            background.raycastTarget = false;

            var border = tooltipObject.GetComponent<Outline>();
            if (border == null)
                border = tooltipObject.AddComponent<Outline>();
            border.effectColor = new Color32(91, 47, 24, 255);
            border.effectDistance = new Vector2(3f, -3f);
            border.useGraphicAlpha = true;

            var textObject = FindOrCreate(tooltipObject.transform, "KeywordTooltipText");
            Stretch((RectTransform)textObject.transform, 25f);
            var tooltipText = GetOrAdd<TextMeshProUGUI>(textObject);
            tooltipText.font = view.SkillDescriptionText.font;
            tooltipText.fontSharedMaterial = view.SkillDescriptionText.fontSharedMaterial != null
                ? view.SkillDescriptionText.fontSharedMaterial
                : view.SkillDescriptionText.font.material;
            tooltipText.text = string.Empty;
            tooltipText.fontSize = 16f;
            tooltipText.fontStyle = FontStyles.Bold;
            tooltipText.alignment = TextAlignmentOptions.TopLeft;
            tooltipText.color = new Color32(69, 34, 18, 255);
            tooltipText.enableAutoSizing = false;
            tooltipText.enableWordWrapping = true;
            tooltipText.overflowMode = TextOverflowModes.Overflow;
            tooltipText.lineSpacing = 3f;
            tooltipText.richText = false;
            tooltipText.raycastTarget = false;

            tooltipObject.SetActive(false);
            view.KeywordTooltip = tooltipObject;
            view.KeywordTooltipText = tooltipText;
        }

        private static void ConfigureCardBasePattern(BattleCardItemView view)
        {
            if (view.SkillDescriptionText == null)
                throw new InvalidOperationException("Battle card name text reference is missing.");

            var skillArea = view.SkillDescriptionText.transform.parent;
            var patternObject = FindOrCreate(skillArea, "CardBasePattern");
            Stretch((RectTransform)patternObject.transform, 5f);
            patternObject.transform.SetAsFirstSibling();

            var patternText = GetOrAdd<TextMeshProUGUI>(patternObject);
            patternText.font = view.SkillDescriptionText.font;
            patternText.fontSharedMaterial = view.SkillDescriptionText.fontSharedMaterial != null
                ? view.SkillDescriptionText.fontSharedMaterial
                : view.SkillDescriptionText.font.material;
            patternText.text = "◇  ·        ·  ◇\n  ∽          ∽";
            patternText.fontSize = 15f;
            patternText.fontStyle = FontStyles.Normal;
            patternText.alignment = TextAlignmentOptions.Center;
            patternText.color = new Color(1f, 0.9f, 0.68f, 0.12f);
            patternText.enableWordWrapping = false;
            patternText.overflowMode = TextOverflowModes.Overflow;
            patternText.richText = false;
            patternText.raycastTarget = false;
        }

        private static void ConfigureFrame(Image image, Sprite sprite, string objectName)
        {
            if (image == null)
                throw new InvalidOperationException($"{objectName} image reference is missing.");

            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(0f, FrameBottomInset);
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.color = Color.white;
        }

        private static void ConfigureHoverInput(GameObject root, BattleCardItemView view)
        {
            var obsoleteHoverInput = root.transform.Find("HoverInput");
            if (obsoleteHoverInput != null)
                UnityEngine.Object.DestroyImmediate(obsoleteHoverInput.gameObject);

            view.CardHoverInput = view.CardBackground;
            view.CardHoverListener = GetOrAdd<UiEventListener>(root);
            view.CardHoverListener.enabled = false;
        }

        private static void ConfigurePreparationInteractionDefaults(BattleCardItemView view)
        {
            view.CardBackground.raycastTarget = false;
            view.CardHoverListener.enabled = false;
            view.CardHoverInput.raycastTarget = false;
            view.PreparationDragable.enabled = false;
            if (view.PreparationDragable.EventListener != null)
                view.PreparationDragable.EventListener.enabled = false;
            view.PreparationInteractor.enabled = false;
            view.PreparationEmptyAttemptListener.enabled = false;
        }

        private static void ConfigureArtwork(Image artwork)
        {
            if (artwork == null)
                throw new InvalidOperationException("ArtworkArea image reference is missing.");

            var rectTransform = artwork.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = ArtworkRenderSize;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            artwork.type = Image.Type.Simple;
            artwork.preserveAspect = false;
            artwork.raycastTarget = false;
        }

        private static void ConfigureTauntShield(
            GameObject root,
            BattleCardItemView view,
            Sprite sprite)
        {
            var shieldObject = FindOrCreate(root.transform, "TauntShieldOutline");
            var rectTransform = (RectTransform)shieldObject.transform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = TauntShieldRenderPosition;
            rectTransform.sizeDelta = TauntShieldRenderSize;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            rectTransform.SetAsFirstSibling();

            var image = GetOrAdd<Image>(shieldObject);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.color = Color.white;
            shieldObject.SetActive(false);
            view.TauntShieldOutline = image;
        }

        private static void ConfigureLayerOrder(BattleCardItemView view)
        {
            view.TauntShieldOutline.transform.SetAsFirstSibling();
            view.CardFrame.transform.SetAsLastSibling();
            view.AttackerHighlight.transform.SetAsLastSibling();
            view.TargetHighlight.transform.SetAsLastSibling();
            if (view.DeadOverlay != null)
                view.DeadOverlay.transform.SetAsLastSibling();
            if (view.PreparationEmptyState != null)
                view.PreparationEmptyState.transform.SetAsLastSibling();
            if (view.PreparationBattleSlotEmptyState != null)
                view.PreparationBattleSlotEmptyState.transform.SetAsLastSibling();
            if (view.PreparationFusionSlotEmptyState != null)
                view.PreparationFusionSlotEmptyState.transform.SetAsLastSibling();
            if (view.PreparationDropHighlight != null)
                view.PreparationDropHighlight.transform.SetAsLastSibling();
            view.HealthText.transform.parent.SetAsLastSibling();
            view.AttackText.transform.parent.SetAsLastSibling();
            view.CardNumberBadge.transform.SetAsLastSibling();
            if (view.PreparationMaterialSelectedState != null)
                view.PreparationMaterialSelectedState.transform.SetAsLastSibling();
            if (view.PreparationDeployedState != null)
                view.PreparationDeployedState.transform.SetAsLastSibling();
            view.DamagePopupBackground.transform.SetAsLastSibling();
            view.ChargeFeedbackIcon.transform.SetAsLastSibling();
            view.LongShotFeedbackIcon.transform.SetAsLastSibling();
            view.KeywordTooltip.transform.SetAsLastSibling();
        }

        private static void ConfigureFeedbackLayers(
            GameObject root,
            BattleCardItemView view,
            Sprite damageNumberBurst,
            Sprite chargeHornIcon,
            Sprite longShotBowIcon)
        {
            view.AttackValueOutgoingText = ConfigureOutgoingStatText(
                view.AttackText,
                "AttackValueOutgoingText");
            view.HealthValueOutgoingText = ConfigureOutgoingStatText(
                view.HealthText,
                "HealthValueOutgoingText");

            var damagePopup = FindOrCreate(root.transform, "DamagePopup");
            SetRect(
                (RectTransform)damagePopup.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(92f, 70f),
                new Vector2(-82f, -112f));
            var damageBackground = GetOrAdd<Image>(damagePopup);
            ConfigureFeedbackImage(damageBackground, damageNumberBurst);

            var damageTextObject = FindOrCreate(damagePopup.transform, "DamageText");
            Stretch((RectTransform)damageTextObject.transform, 8f);
            var damageText = GetOrAdd<TextMeshProUGUI>(damageTextObject);
            ConfigureFeedbackText(damageText, view.HealthText, 28f);
            damageText.text = string.Empty;
            damageText.color = new Color32(210, 32, 32, 255);
            ConfigureTextOutline(damageText, new Color(0.12f, 0.01f, 0.01f, 0.98f), 2f);

            var chargeObject = FindOrCreate(root.transform, "ChargeFeedbackIcon");
            SetRect(
                (RectTransform)chargeObject.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(96f, 96f),
                new Vector2(-62f, 88f));
            var chargeImage = GetOrAdd<Image>(chargeObject);
            ConfigureFeedbackImage(chargeImage, chargeHornIcon);

            var longShotObject = FindOrCreate(root.transform, "LongShotFeedbackIcon");
            SetRect(
                (RectTransform)longShotObject.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(96f, 96f),
                new Vector2(62f, 88f));
            var longShotImage = GetOrAdd<Image>(longShotObject);
            ConfigureFeedbackImage(longShotImage, longShotBowIcon);

            damagePopup.SetActive(false);
            chargeObject.SetActive(false);
            longShotObject.SetActive(false);
            view.DamagePopupBackground = damageBackground;
            view.DamagePopupText = damageText;
            view.ChargeFeedbackIcon = chargeImage;
            view.LongShotFeedbackIcon = longShotImage;
        }

        private static TMP_Text ConfigureOutgoingStatText(TMP_Text source, string objectName)
        {
            if (source == null)
                throw new InvalidOperationException($"{objectName} source text is missing.");

            var textObject = FindOrCreate(source.transform.parent, objectName);
            Stretch((RectTransform)textObject.transform, 0f);
            var text = GetOrAdd<TextMeshProUGUI>(textObject);
            ConfigureFeedbackText(text, source, source.fontSize);
            text.text = string.Empty;
            ConfigureTextOutline(text, new Color(0.04f, 0.02f, 0.01f, 0.95f), 1.5f);
            textObject.SetActive(false);
            return text;
        }

        private static void ConfigureFeedbackImage(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static void ConfigureFeedbackText(TMP_Text text, TMP_Text source, float fontSize)
        {
            text.font = source.font;
            text.fontSharedMaterial = source.fontSharedMaterial != null
                ? source.fontSharedMaterial
                : source.font.material;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = false;
            text.enableWordWrapping = false;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        private static void ConfigureTextOutline(TMP_Text text, Color color, float distance)
        {
            var outline = text.GetComponent<Outline>();
            if (outline == null)
                outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        private static void ConfigurePreparationFeatures(GameObject root, BattleCardItemView view)
        {
            view.CardBackground.raycastTarget = true;

            var emptyState = FindOrCreate(root.transform, "PreparationEmptyState");
            Stretch((RectTransform)emptyState.transform, 0f);
            var emptyImage = GetOrAdd<Image>(emptyState);
            emptyImage.sprite = LoadSprite(
                "Assets/Resources/Art/Preparation/UI/PreparationPoolEmptySlot.png",
                "Preparation pool empty slot");
            emptyImage.type = Image.Type.Simple;
            emptyImage.preserveAspect = true;
            emptyImage.color = Color.white;
            emptyImage.raycastTarget = false;

            var emptyAttempt = FindOrCreate(emptyState.transform, "EmptyAttempt");
            Stretch((RectTransform)emptyAttempt.transform, 0f);
            var emptyAttemptImage = GetOrAdd<Image>(emptyAttempt);
            emptyAttemptImage.sprite = null;
            emptyAttemptImage.color = new Color(1f, 1f, 1f, 0.001f);
            emptyAttemptImage.raycastTarget = true;
            var emptyAttemptListener = GetOrAdd<UiEventListener>(emptyAttempt);

            var battleSlotEmpty = ConfigurePreparationEmptyState(
                root.transform,
                "PreparationBattleSlotEmptyState",
                "Assets/Resources/Art/Preparation/UI/PreparationPoolEmptySlot.png",
                "Preparation pool empty slot");
            var fusionSlotEmpty = ConfigurePreparationEmptyState(
                root.transform,
                "PreparationFusionSlotEmptyState",
                "Assets/Resources/Art/Preparation/UI/PreparationPoolEmptySlot.png",
                "Preparation pool empty slot");

            var dropHighlight = FindOrCreate(root.transform, "PreparationDropHighlight");
            Stretch((RectTransform)dropHighlight.transform, 0f);
            var dropHighlightImage = GetOrAdd<Image>(dropHighlight);
            dropHighlightImage.sprite = LoadSprite(
                "Assets/Resources/Art/Preparation/UI/PreparationDropHighlight.png",
                "Preparation drop highlight");
            dropHighlightImage.type = Image.Type.Simple;
            dropHighlightImage.preserveAspect = true;
            dropHighlightImage.color = new Color(1f, 1f, 1f, 0.72f);
            dropHighlightImage.raycastTarget = false;

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

            var deployedState = FindOrCreate(root.transform, "PreparationDeployedState");
            SetRect(
                (RectTransform)deployedState.transform,
                new Vector2(1f, 1f),
                new Vector2(120f, 44f),
                new Vector2(-67f, -26f));
            var obsoleteDeployedText = deployedState.GetComponent<TextMeshProUGUI>();
            if (obsoleteDeployedText != null)
                UnityEngine.Object.DestroyImmediate(obsoleteDeployedText);
            var deployedImage = GetOrAdd<Image>(deployedState);
            deployedImage.sprite = LoadSprite(PreparationDeployedStatePath, "Preparation deployed state");
            deployedImage.type = Image.Type.Simple;
            deployedImage.preserveAspect = true;
            deployedImage.color = Color.white;
            deployedImage.raycastTarget = false;

            var dragable = GetOrAdd<UiDragable>(root);
            dragable.TurnBackWhenDragEnd = true;
            dragable.AlwaysRelativeOffset = false;
            dragable.EventListener = view.CardHoverListener;
            var interactor = GetOrAdd<UiInteractor>(root);
            interactor.TransformOverride = root.transform;
            interactor.AutoInitUiDragable = true;
            interactor.UiDragableRef = dragable;

            emptyState.SetActive(false);
            battleSlotEmpty.SetActive(false);
            fusionSlotEmpty.SetActive(false);
            dropHighlight.SetActive(false);
            materialSelected.SetActive(false);
            deployedState.SetActive(false);
            view.PreparationEmptyState = emptyState;
            view.PreparationBattleSlotEmptyState = battleSlotEmpty;
            view.PreparationFusionSlotEmptyState = fusionSlotEmpty;
            view.PreparationMaterialSelectedState = materialSelected;
            view.PreparationDeployedState = deployedState;
            view.PreparationDropHighlight = dropHighlightImage;
            view.PreparationDragable = dragable;
            view.PreparationInteractor = interactor;
            view.PreparationEmptyAttemptListener = emptyAttemptListener;
        }

        private static GameObject ConfigurePreparationEmptyState(
            Transform parent,
            string objectName,
            string spritePath,
            string label)
        {
            var emptyState = FindOrCreate(parent, objectName);
            Stretch((RectTransform)emptyState.transform, 0f);
            var image = GetOrAdd<Image>(emptyState);
            image.sprite = LoadSprite(spritePath, label);
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return emptyState;
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
