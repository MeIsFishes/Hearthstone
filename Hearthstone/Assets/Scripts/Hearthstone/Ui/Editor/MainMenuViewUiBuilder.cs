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
                PreparationUiBuilderUtility.SavePrefab(root, PrefabPath, false);
            }
            finally
            {
                PreparationUiBuilderUtility.DestroyTemporary(root);
            }
        }

        private static void CreateTitle(Transform parent, MainMenuView view)
        {
            var titleRoot = PreparationUiBuilderUtility.CreateUiObject("GameTitle", parent);
            PreparationUiBuilderUtility.SetRect(
                titleRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(900f, 190f),
                new Vector2(0f, 190f));
            var title = PreparationUiBuilderUtility.AddText(titleRoot, "99升变", 132f);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.22f, 0.09f, 0.035f, 1f);
            title.outlineColor = new Color32(190, 132, 55, 210);
            title.outlineWidth = 0.16f;
            title.characterSpacing = 5f;
            view.GameTitle = title;
        }

        private static void CreateStartButton(Transform parent, MainMenuView view)
        {
            var idle = PreparationUiBuilderUtility.LoadSprite("PreparationContinueButtonIdle");
            var highlighted = PreparationUiBuilderUtility.LoadSprite("PreparationContinueButtonHighlighted");
            var pressed = PreparationUiBuilderUtility.LoadSprite("PreparationContinueButtonPressed");
            var disabled = PreparationUiBuilderUtility.LoadSprite("PreparationContinueButtonWaiting");

            var buttonRoot = PreparationUiBuilderUtility.CreateUiObject("StartGameButton", parent);
            PreparationUiBuilderUtility.SetRect(
                buttonRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(420f, 160f),
                new Vector2(0f, -230f));
            var image = PreparationUiBuilderUtility.AddImage(buttonRoot, idle, true);
            var button = buttonRoot.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.spriteState = new SpriteState
            {
                highlightedSprite = highlighted,
                pressedSprite = pressed,
                selectedSprite = highlighted,
                disabledSprite = disabled,
            };

            var labelRoot = PreparationUiBuilderUtility.CreateUiObject("Label", buttonRoot.transform);
            PreparationUiBuilderUtility.Stretch(labelRoot, 42f, 24f, 42f, 24f);
            var label = PreparationUiBuilderUtility.AddText(labelRoot, "开始游戏", 50f);
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(1f, 0.91f, 0.65f, 1f);
            view.StartGameButton = button;
            view.StartGameLabel = label;
        }

        private static Sprite LoadCoverSprite()
        {
            var importer = AssetImporter.GetAtPath(CoverPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Main menu cover is missing at '{CoverPath}'.");
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.alphaIsTransparency ||
                importer.mipmapEnabled ||
                importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = false;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(CoverPath)
                ?? throw new InvalidOperationException($"Main menu cover Sprite is missing at '{CoverPath}'.");
        }
    }
}
#endif
