using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    /// <summary>
    /// LoadingView Prefab 的唯一 Editor 配置源。
    /// </summary>
    public static class LoadingViewUiBuilder
    {
        private const string PrefabPath = "Assets/Resources/Ui/LoadingView.prefab";
        private const string BackgroundPath =
            "Assets/Resources/Art/Loading/UI/HearthstoneLoadingBackground.png";

        public static void Build()
        {
            ConfigureBackgroundImporter();

            var root = PreparationUiBuilderUtility.CreateUiObject("LoadingView", null);
            try
            {
                PreparationUiBuilderUtility.Stretch(root);
                var view = root.AddComponent<LoadingView>();
                view.DefaultShow = false;

                var background = PreparationUiBuilderUtility.CreateUiObject("Background", root.transform);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath)
                    ?? throw new InvalidOperationException(
                        $"Loading background Sprite is missing at '{BackgroundPath}'.");
                var image = PreparationUiBuilderUtility.AddImage(background, sprite, true);
                image.preserveAspect = false;

                var aspectRatioFitter = background.AddComponent<AspectRatioFitter>();
                aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                aspectRatioFitter.aspectRatio = sprite.rect.width / sprite.rect.height;

                PreparationUiBuilderUtility.SavePrefab(root, PrefabPath, true);
            }
            finally
            {
                PreparationUiBuilderUtility.DestroyTemporary(root);
            }
        }

        private static void ConfigureBackgroundImporter()
        {
            var importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter
                ?? throw new InvalidOperationException(
                    $"Loading background texture is missing at '{BackgroundPath}'.");
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                !importer.mipmapEnabled &&
                importer.wrapMode == TextureWrapMode.Clamp)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }
}
