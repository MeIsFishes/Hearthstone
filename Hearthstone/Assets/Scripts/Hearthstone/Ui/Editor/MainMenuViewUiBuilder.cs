#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class MainMenuViewUiBuilder
    {
        public const string PrefabPath = "Assets/Resources/Ui/MainMenuView.prefab";
        public const string CoverPath =
            "Assets/Resources/Art/MainMenu/UI/MainMenuParchmentBackground.png";
        public const string WarriorFramesPath =
            "Assets/Resources/Art/MainMenu/UI/MainMenuGoblinWarriorFrames.png";
        public const string ArcherFramesPath =
            "Assets/Resources/Art/MainMenu/UI/MainMenuGoblinArcherFrames.png";
        public const string SilhouetteMaterialPath =
            "Assets/Resources/Art/MainMenu/UI/MainMenuSilhouetteKey.mat";
        private const string SilhouetteShaderName = "Hearthstone/UI/MainMenuSilhouetteKey";
        private const int AnimationColumns = 6;
        private const int AnimationRows = 2;
        private const int AnimationFrameCount = AnimationColumns * AnimationRows;
        private const int ForegroundLuminanceThreshold = 235;
        private const int GroundAnchorBandHeight = 80;
        private const int AnimationFrameHorizontalPadding = 6;
        private static readonly Vector2 GoblinImageSize = new Vector2(560f, 760f);
        private static readonly Vector2Int[] WarriorFrameHorizontalBounds =
        {
            new Vector2Int(23, 255),
            new Vector2Int(272, 496),
            new Vector2Int(518, 743),
            new Vector2Int(769, 991),
            new Vector2Int(1019, 1241),
            new Vector2Int(1265, 1483),
            new Vector2Int(28, 248),
            new Vector2Int(274, 499),
            new Vector2Int(523, 743),
            new Vector2Int(776, 998),
            new Vector2Int(1022, 1243),
            new Vector2Int(1263, 1493),
        };
        private static readonly Vector2Int[] ArcherFrameHorizontalBounds =
        {
            new Vector2Int(25, 249),
            new Vector2Int(291, 508),
            new Vector2Int(542, 753),
            new Vector2Int(782, 1003),
            new Vector2Int(1035, 1252),
            new Vector2Int(1275, 1494),
            new Vector2Int(26, 242),
            new Vector2Int(290, 505),
            new Vector2Int(543, 752),
            new Vector2Int(782, 1001),
            new Vector2Int(1036, 1247),
            new Vector2Int(1283, 1504),
        };
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

                CreateGoblinAnimations(root.transform, view);
                CreateTitle(root.transform, view);
                CreateStartButton(root.transform, view);
                CreateCollectionButton(root.transform, view);
                CreateExitGameButton(root.transform, view);
                CreateClearDataButton(root.transform, view);
                CreateVersionLabel(root.transform, view);
                PreparationUiBuilderUtility.SavePrefab(root, PrefabPath, false);
            }
            finally
            {
                PreparationUiBuilderUtility.DestroyTemporary(root);
            }
        }

        private static void CreateGoblinAnimations(Transform parent, MainMenuView view)
        {
            var material = LoadOrCreateSilhouetteMaterial();
            var warriorFrames = LoadAnimationFrames(
                WarriorFramesPath,
                "Warrior",
                out var warriorFrameOffsets);
            var archerFrames = LoadAnimationFrames(
                ArcherFramesPath,
                "Archer",
                out var archerFrameOffsets);

            view.LeftGoblinImage = CreateGoblinImage(
                parent,
                "LeftGoblinWarrior",
                new Vector2(0f, 0.5f),
                new Vector2(255f, -45f),
                warriorFrames[0],
                material);
            view.LeftGoblinFrames = warriorFrames;
            view.LeftGoblinFrameOffsets = warriorFrameOffsets;
            view.RightGoblinImage = CreateGoblinImage(
                parent,
                "RightGoblinArcher",
                new Vector2(1f, 0.5f),
                new Vector2(-255f, -45f),
                archerFrames[0],
                material);
            view.RightGoblinFrames = archerFrames;
            view.RightGoblinFrameOffsets = archerFrameOffsets;
        }

        private static Image CreateGoblinImage(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Sprite firstFrame,
            Material material)
        {
            var root = PreparationUiBuilderUtility.CreateUiObject(name, parent);
            PreparationUiBuilderUtility.SetRect(
                root,
                anchor,
                GoblinImageSize,
                position);
            var image = PreparationUiBuilderUtility.AddImage(root, firstFrame);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.material = material;
            return image;
        }

        private static Material LoadOrCreateSilhouetteMaterial()
        {
            var shader = Shader.Find(SilhouetteShaderName)
                ?? throw new InvalidOperationException(
                    $"Main menu silhouette shader '{SilhouetteShaderName}' is unavailable.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(SilhouetteMaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "MainMenuSilhouetteKey",
                };
                material.SetFloat("_KeyLow", 0.92f);
                material.SetFloat("_KeyHigh", 0.97f);
                AssetDatabase.CreateAsset(material, SilhouetteMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        private static Sprite[] LoadAnimationFrames(
            string path,
            string framePrefix,
            out Vector2[] frameOffsets)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Main menu animation sheet is missing at '{path}'.");
            importer.GetSourceTextureWidthAndHeight(out var textureWidth, out var textureHeight);
            if (textureWidth % AnimationColumns != 0 || textureHeight % AnimationRows != 0)
                throw new InvalidOperationException(
                    $"Main menu animation sheet '{path}' must divide into a {AnimationColumns}x{AnimationRows} grid.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.isReadable = true;
#pragma warning disable CS0618
            var spriteMetadata = CreateAnimationSpriteMetadata(
                textureWidth,
                textureHeight,
                framePrefix);
            importer.spritesheet = spriteMetadata;
#pragma warning restore CS0618
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path)
                ?? throw new InvalidOperationException(
                    $"Main menu animation texture is missing at '{path}'.");
            frameOffsets = CalculateFrameOffsets(texture, spriteMetadata);
            importer.isReadable = false;
            importer.SaveAndReimport();

            var frames = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            if (frames.Length != AnimationFrameCount)
                throw new InvalidOperationException(
                    $"Main menu animation sheet '{path}' imported {frames.Length} frames instead of {AnimationFrameCount}.");
            return frames;
        }

        private static Vector2[] CalculateFrameOffsets(
            Texture2D texture,
            SpriteMetaData[] spriteMetadata)
        {
            var pixels = texture.GetPixels32();
            var baselines = new int[AnimationFrameCount];
            var groundCenters = new float[AnimationFrameCount];
            var spriteCenters = new float[AnimationFrameCount];
            var displayScales = new float[AnimationFrameCount];
            for (var index = 0; index < baselines.Length; ++index)
            {
                var rect = spriteMetadata[index].rect;
                var originX = Mathf.RoundToInt(rect.x);
                var originY = Mathf.RoundToInt(rect.y);
                var spriteWidth = Mathf.RoundToInt(rect.width);
                var spriteHeight = Mathf.RoundToInt(rect.height);
                var baseline = spriteHeight;
                for (var localY = 0; localY < spriteHeight; ++localY)
                {
                    for (var localX = 0; localX < spriteWidth; ++localX)
                    {
                        var pixel = pixels[(originY + localY) * texture.width + originX + localX];
                        var luminance = (77 * pixel.r + 150 * pixel.g + 29 * pixel.b) >> 8;
                        if (luminance < ForegroundLuminanceThreshold)
                            baseline = Mathf.Min(baseline, localY);
                    }
                }

                if (baseline == spriteHeight)
                    throw new InvalidOperationException(
                        $"Main menu animation frame {index} contains no visible silhouette pixels.");
                baselines[index] = baseline;

                var groundMinX = spriteWidth;
                var groundMaxX = -1;
                var groundBandTop = Mathf.Min(
                    spriteHeight,
                    baseline + GroundAnchorBandHeight);
                for (var localY = baseline; localY < groundBandTop; ++localY)
                {
                    for (var localX = 0; localX < spriteWidth; ++localX)
                    {
                        var pixel = pixels[(originY + localY) * texture.width + originX + localX];
                        var luminance = (77 * pixel.r + 150 * pixel.g + 29 * pixel.b) >> 8;
                        if (luminance >= ForegroundLuminanceThreshold)
                            continue;
                        groundMinX = Mathf.Min(groundMinX, localX);
                        groundMaxX = Mathf.Max(groundMaxX, localX);
                    }
                }
                if (groundMaxX < groundMinX)
                    throw new InvalidOperationException(
                        $"Main menu animation frame {index} contains no ground anchor pixels.");
                groundCenters[index] = (groundMinX + groundMaxX) * 0.5f;
                spriteCenters[index] = spriteWidth * 0.5f;
                displayScales[index] = Mathf.Min(
                    GoblinImageSize.x / spriteWidth,
                    GoblinImageSize.y / spriteHeight);
            }

            var referenceGroundCenter =
                (groundCenters[0] - spriteCenters[0]) * displayScales[0];
            var referenceBaseline =
                (baselines[0] - spriteMetadata[0].rect.height * 0.5f) * displayScales[0];
            var offsets = new Vector2[AnimationFrameCount];
            for (var index = 0; index < offsets.Length; ++index)
            {
                offsets[index] = new Vector2(
                    referenceGroundCenter -
                    (groundCenters[index] - spriteCenters[index]) * displayScales[index],
                    referenceBaseline -
                    (baselines[index] - spriteMetadata[index].rect.height * 0.5f) *
                    displayScales[index]);
            }
            return offsets;
        }

        private static SpriteMetaData[] CreateAnimationSpriteMetadata(
            int textureWidth,
            int textureHeight,
            string framePrefix)
        {
            var cellHeight = textureHeight / AnimationRows;
            var metadata = new SpriteMetaData[AnimationFrameCount];
            for (var index = 0; index < metadata.Length; ++index)
            {
                var rowFromTop = index / AnimationColumns;
                var horizontalBounds = GetAnimationFrameHorizontalBounds(
                    framePrefix,
                    index);
                var rectX = horizontalBounds.x - AnimationFrameHorizontalPadding;
                var rectWidth = horizontalBounds.y - horizontalBounds.x + 1 +
                                AnimationFrameHorizontalPadding * 2;
                metadata[index] = new SpriteMetaData
                {
                    name = $"{framePrefix}_{index:00}",
                    rect = new Rect(
                        rectX,
                        textureHeight - (rowFromTop + 1) * cellHeight,
                        rectWidth,
                        cellHeight),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                };
            }
            return metadata;
        }

        private static Vector2Int GetAnimationFrameHorizontalBounds(
            string framePrefix,
            int frameIndex)
        {
            var bounds = framePrefix == "Warrior"
                ? WarriorFrameHorizontalBounds
                : framePrefix == "Archer"
                    ? ArcherFrameHorizontalBounds
                    : throw new InvalidOperationException(
                        $"Unsupported main menu animation prefix '{framePrefix}'.");
            return bounds[frameIndex];
        }

        private static void CreateVersionLabel(Transform parent, MainMenuView view)
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("VersionLabel", parent);
            PreparationUiBuilderUtility.SetRect(
                root,
                new Vector2(0f, 0f),
                new Vector2(240f, 52f),
                new Vector2(148f, 38f));
            var label = PreparationUiBuilderUtility.AddText(
                root,
                $"v{PlayerSettings.bundleVersion}",
                24f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.black;
            label.raycastTarget = false;
            view.VersionLabel = label;
        }

        private static void CreateCollectionButton(Transform parent, MainMenuView view)
        {
            CreateTextButton(
                parent,
                "CollectionButton",
                "图鉴",
                new Vector2(0f, -50f),
                44f,
                new Color(0.68f, 0.65f, 0.60f, 0.42f),
                new Color(0.56f, 0.53f, 0.49f, 0.58f),
                out var button,
                out var label);
            view.CollectionButton = button;
            view.CollectionLabel = label;
        }

        private static void CreateExitGameButton(Transform parent, MainMenuView view)
        {
            CreateTextButton(
                parent,
                "ExitGameButton",
                "退出游戏",
                new Vector2(0f, -190f),
                44f,
                new Color(0.76f, 0.22f, 0.18f, 0.48f),
                new Color(0.62f, 0.10f, 0.08f, 0.64f),
                out var button,
                out var label);
            view.ExitGameButton = button;
            view.ExitGameLabel = label;
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
            Color highlightedColor,
            Color pressedColor,
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
                highlightedColor = highlightedColor,
                pressedColor = pressedColor,
                selectedColor = highlightedColor,
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
                new Vector2(0f, 100f));
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
            var label = PreparationUiBuilderUtility.AddText(labelRoot, "开始游戏", 44f);
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
