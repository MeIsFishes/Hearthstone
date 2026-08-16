#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class MainMenuViewUiBuilder
    {
        public const string PrefabPath = "Assets/Resources/Ui/MainMenuView.prefab";
        public const string CoverPath = "Assets/Resources/Art/MainMenu/UI/MainMenuCover.png";
        public const string TitlePath = "Assets/Resources/Art/MainMenu/UI/MainMenuTitle.png";
        public const string StartHoverPath =
            "Assets/Resources/Art/MainMenu/UI/MainMenuStartHoverWetParchment.png";

        public static void Build()
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("MainMenuView", null);
            try
            {
                ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
                var view = root.AddComponent<MainMenuView>();
                view.DefaultShow = true;

                var cover = PreparationUiBuilderUtility.CreateUiObject("Cover", root.transform);
                PreparationUiBuilderUtility.Stretch(cover);
                var coverImage = PreparationUiBuilderUtility.AddImage(cover, LoadCoverSprite());
                coverImage.preserveAspect = false;
                coverImage.raycastTarget = false;

                CreateTitle(root.transform, view);
                CreateStartButton(root.transform, view);
                CreateCollectionButton(root.transform, view);
                CreateClearDataButton(root.transform, view);
                CreateVersionLabel(root.transform, view);
                PreparationUiBuilderUtility.SavePrefab(root, PrefabPath, false);
            }
            finally
            {
                PreparationUiBuilderUtility.DestroyTemporary(root);
            }
        }

        private static void CreateVersionLabel(Transform parent, MainMenuView view)
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("VersionLabel", parent);
            PreparationUiBuilderUtility.SetRect(
                root,
                new Vector2(1f, 0f),
                new Vector2(240f, 52f),
                new Vector2(-148f, 38f));
            var label = PreparationUiBuilderUtility.AddText(
                root,
                $"v{PlayerSettings.bundleVersion}",
                24f);
            label.alignment = TextAlignmentOptions.MidlineRight;
            label.color = Color.black;
            label.raycastTarget = false;
            view.VersionLabel = label;
        }

        private static void CreateCollectionButton(Transform parent, MainMenuView view)
        {
            var root = CreateTextButton(parent, "CollectionButton", "图鉴", new Vector2(0f, -390f), 44f,
                out var button, out var label);
            view.CollectionButton = button;
            view.CollectionLabel = label;
        }

        private static void CreateClearDataButton(Transform parent, MainMenuView view)
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("ClearDataButton", parent);
            PreparationUiBuilderUtility.SetRect(
                root,
                new Vector2(1f, 1f),
                new Vector2(180f, 58f),
                new Vector2(-112f, -54f));
            var hitArea = PreparationUiBuilderUtility.AddImage(root, null, true);
            hitArea.color = new Color(1f, 1f, 1f, 0.001f);
            var button = root.AddComponent<Button>();
            button.targetGraphic = hitArea;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1f, 0.82f, 0.82f, 1f),
                pressedColor = new Color(0.78f, 0.62f, 0.62f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(1f, 1f, 1f, 0.35f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f,
            };
            var labelRoot = PreparationUiBuilderUtility.CreateUiObject("Label", root.transform);
            PreparationUiBuilderUtility.Stretch(labelRoot);
            var label = PreparationUiBuilderUtility.AddText(labelRoot, "清除数据", 27f);
            label.color = new Color(0.62f, 0.08f, 0.06f, 1f);
            view.ClearDataButton = button;
            view.ClearDataLabel = label;
        }

        private static GameObject CreateTextButton(
            Transform parent,
            string name,
            string text,
            Vector2 position,
            float fontSize,
            out Button button,
            out TextMeshProUGUI label)
        {
            var root = PreparationUiBuilderUtility.CreateUiObject(name, parent);
            PreparationUiBuilderUtility.SetRect(
                root,
                new Vector2(0.5f, 0.5f),
                new Vector2(360f, 120f),
                position);
            var image = PreparationUiBuilderUtility.AddImage(root, LoadUiSprite(StartHoverPath), true);
            image.preserveAspect = false;
            button = root.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = new ColorBlock
            {
                normalColor = new Color(1f, 1f, 1f, 0f),
                highlightedColor = new Color(0.68f, 0.65f, 0.60f, 0.42f),
                pressedColor = new Color(0.56f, 0.53f, 0.49f, 0.58f),
                selectedColor = new Color(0.68f, 0.65f, 0.60f, 0.42f),
                disabledColor = new Color(1f, 1f, 1f, 0f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f,
            };
            var labelRoot = PreparationUiBuilderUtility.CreateUiObject("Label", root.transform);
            PreparationUiBuilderUtility.Stretch(labelRoot, 36f, 20f, 36f, 20f);
            label = PreparationUiBuilderUtility.AddText(labelRoot, text, fontSize);
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.22f, 0.17f, 0.12f, 1f);
            return root;
        }

        private static void CreateTitle(Transform parent, MainMenuView view)
        {
            var titleRoot = PreparationUiBuilderUtility.CreateUiObject("GameTitle", parent);
            PreparationUiBuilderUtility.SetRect(
                titleRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(820f, 300f),
                new Vector2(0f, 285f));
            var title = PreparationUiBuilderUtility.AddImage(
                titleRoot,
                LoadUiSprite(TitlePath));
            title.preserveAspect = true;
            title.raycastTarget = false;
            view.GameTitle = title;
        }

        private static void CreateStartButton(Transform parent, MainMenuView view)
        {
            var buttonRoot = PreparationUiBuilderUtility.CreateUiObject("StartGameButton", parent);
            PreparationUiBuilderUtility.SetRect(
                buttonRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(420f, 150f),
                new Vector2(0f, -230f));
            var image = PreparationUiBuilderUtility.AddImage(
                buttonRoot,
                LoadUiSprite(StartHoverPath),
                true);
            image.preserveAspect = false;
            var button = buttonRoot.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = new ColorBlock
            {
                normalColor = new Color(1f, 1f, 1f, 0f),
                highlightedColor = new Color(0.68f, 0.65f, 0.60f, 0.42f),
                pressedColor = new Color(0.56f, 0.53f, 0.49f, 0.58f),
                selectedColor = new Color(0.68f, 0.65f, 0.60f, 0.42f),
                disabledColor = new Color(1f, 1f, 1f, 0f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f,
            };

            var labelRoot = PreparationUiBuilderUtility.CreateUiObject("Label", buttonRoot.transform);
            PreparationUiBuilderUtility.Stretch(labelRoot, 42f, 24f, 42f, 24f);
            var label = PreparationUiBuilderUtility.AddText(labelRoot, "开始游戏", 50f);
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.22f, 0.17f, 0.12f, 1f);
            var labelShadow = label.gameObject.AddComponent<Shadow>();
            labelShadow.effectColor = new Color(0.88f, 0.82f, 0.72f, 0.55f);
            labelShadow.effectDistance = new Vector2(1f, -1f);
            view.StartGameButton = button;
            view.StartGameHoverBackground = image;
            view.StartGameLabel = label;
        }

        private static Sprite LoadCoverSprite()
        {
            return LoadUiSprite(CoverPath, false);
        }

        private static Sprite LoadUiSprite(string path, bool alphaIsTransparency = true)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Main menu UI texture is missing at '{path}'.");
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.alphaIsTransparency != alphaIsTransparency ||
                importer.mipmapEnabled ||
                importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = alphaIsTransparency;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path)
                ?? throw new InvalidOperationException($"Main menu UI Sprite is missing at '{path}'.");
        }
    }
}
#endif
