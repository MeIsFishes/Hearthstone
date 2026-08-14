using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class BattleCardItemUiBuilder
    {
        private const string PrefabPath = "Assets/Resources/Ui/BattleCardItem.prefab";
        private const string NarrowFramePath =
            "Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png";
        private const string AttackBadgePath =
            "Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png";
        private const string HealthBadgePath =
            "Assets/Resources/Art/BattleCards/UI/HealthDropBadge.png";

        public static void Build()
        {
            var narrowFrame = AssetDatabase.LoadAssetAtPath<Sprite>(NarrowFramePath);
            if (narrowFrame == null)
                throw new InvalidOperationException($"Narrow battle card frame is missing at '{NarrowFramePath}'.");
            var attackBadge = LoadSprite(AttackBadgePath, "Attack badge");
            var healthBadge = LoadSprite(HealthBadgePath, "Health badge");

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var view = root.GetComponent<BattleCardItemView>();
                if (view == null)
                    throw new InvalidOperationException("BattleCardItemView is missing from the card prefab root.");

                ConfigureFrame(view.CardFrame, narrowFrame, "CardFrameOverlay");
                ConfigureFrame(view.AttackerHighlight, narrowFrame, "AttackerHighlight");
                ConfigureFrame(view.TargetHighlight, narrowFrame, "TargetHighlight");
                ConfigureBadge(view.HealthText, healthBadge, Vector2.zero, new Vector2(30f, 30f), "HealthBadge");
                ConfigureBadge(view.AttackText, attackBadge, new Vector2(1f, 0f), new Vector2(-30f, 30f), "AttackBadge");

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureFrame(Image image, Sprite sprite, string objectName)
        {
            if (image == null)
                throw new InvalidOperationException($"{objectName} image reference is missing.");

            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(-40f, -32f);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
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
