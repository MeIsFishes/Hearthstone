using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BbxCommon.Ui;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hearthstone.Tests
{
    public sealed class MainMenuTests
    {
        private const string ViewPrefabPath = "Assets/Resources/Ui/MainMenuView.prefab";
        private const string CoverPath =
            "Assets/Resources/Art/MainMenu/UI/MainMenuParchmentBackground.png";
        private const string WarriorFramesPath =
            "Assets/Resources/Art/MainMenu/UI/MainMenuGoblinWarriorFrames.png";
        private const string ArcherFramesPath =
            "Assets/Resources/Art/MainMenu/UI/MainMenuGoblinArcherFrames.png";
        private const string TitlePath = "Assets/Resources/Art/MainMenu/UI/MainMenuTitle.png";
        private const string StartHoverPath =
            "Assets/Resources/Art/MainMenu/UI/MainMenuStartHoverWetParchment.png";
        private const string SharedControlPath =
            "Assets/Resources/Art/Common/UI/MedievalParchmentControl.png";
        private const string UiSceneAssetPath = "Assets/Resources/Ui/MainMenu.asset";
        private const int FrameHorizontalPadding = 6;
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
        private const string UiScenePath = "Assets/Scenes/Ui/MainMenu.unity";

        [Test]
        public void EngineRoutesInitialLoadToMainMenuAndStartCreatesNewRun()
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Bootstrap/HearthstoneGameEngine.cs");
            Assert.NotNull(script);
            var source = script.text;
            var onAwakeStart = source.IndexOf("protected override void OnAwake()", StringComparison.Ordinal);
            var enterMainMenuStart = source.IndexOf("public void EnterMainMenuStageGroup()", StringComparison.Ordinal);
            var startNewRunStart = source.IndexOf("public void StartNewRun()", StringComparison.Ordinal);
            var enterBattleStart = source.IndexOf("public void EnterBattleStageGroup", StringComparison.Ordinal);
            var loadingCompletedStart = source.IndexOf(
                "protected override void OnStageLoadingCompleted",
                StringComparison.Ordinal);
            var submitStart = source.IndexOf(
                "private void TrySubmitRequestedStageGroup",
                StringComparison.Ordinal);

            Assert.That(onAwakeStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(enterMainMenuStart, Is.GreaterThan(onAwakeStart));
            Assert.That(startNewRunStart, Is.GreaterThan(enterMainMenuStart));
            Assert.That(enterBattleStart, Is.GreaterThan(startNewRunStart));
            Assert.That(loadingCompletedStart, Is.GreaterThan(enterBattleStart));
            Assert.That(submitStart, Is.GreaterThan(loadingCompletedStart));

            var onAwakeSource = source.Substring(onAwakeStart, enterMainMenuStart - onAwakeStart);
            var startNewRunSource = source.Substring(startNewRunStart, enterBattleStart - startNewRunStart);
            var loadingCompletedSource = source.Substring(
                loadingCompletedStart,
                submitStart - loadingCompletedStart);
            var submitSource = source.Substring(submitStart);
            StringAssert.DoesNotContain("CreateRunStateStage", onAwakeSource);
            StringAssert.Contains("RestartRun()", startNewRunSource);
            StringAssert.Contains("EnterMainMenuStageGroup()", loadingCompletedSource);
            StringAssert.Contains("case EHearthstoneStageGroup.MainMenu", submitSource);
            StringAssert.Contains("StageWrapper.SetActiveGameStage(m_MainMenuStage)", submitSource);
        }

        [Test]
        public void MainMenuPrefabContainsTitleCoverAndMenuButtons()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ViewPrefabPath);
            Assert.NotNull(prefab);
            var view = prefab.GetComponent<MainMenuView>();
            Assert.NotNull(view);
            Assert.IsTrue(view.DefaultShow);
            Assert.NotNull(view.GameTitle);
            Assert.NotNull(view.GameTitle.sprite);
            Assert.AreEqual("MainMenuTitle", view.GameTitle.sprite.name);
            Assert.IsFalse(view.GameTitle.raycastTarget);
            Assert.IsTrue(view.GameTitle.preserveAspect);
            Assert.AreEqual(
                new Vector2(0f, 285f),
                ((RectTransform)view.GameTitle.transform).anchoredPosition);
            AssertGoblinImage(
                view.LeftGoblinImage,
                view.LeftGoblinFrames,
                view.LeftGoblinFrameOffsets,
                "Warrior",
                new Vector2(0f, 0.5f),
                new Vector2(255f, -45f));
            AssertGoblinImage(
                view.RightGoblinImage,
                view.RightGoblinFrames,
                view.RightGoblinFrameOffsets,
                "Archer",
                new Vector2(1f, 0.5f),
                new Vector2(-255f, -45f));
            Assert.NotNull(view.StartGameButton);
            Assert.NotNull(view.StartGameHoverBackground);
            Assert.NotNull(view.StartGameLabel);
            Assert.AreEqual("开始游戏", view.StartGameLabel.text);
            Assert.AreEqual(Navigation.Mode.None, view.StartGameButton.navigation.mode);
            Assert.AreEqual(Selectable.Transition.ColorTint, view.StartGameButton.transition);
            Assert.AreSame(view.StartGameHoverBackground, view.StartGameButton.targetGraphic);
            Assert.AreEqual(
                "MainMenuStartHoverWetParchment",
                view.StartGameHoverBackground.sprite.name);
            Assert.AreEqual(0f, view.StartGameButton.colors.normalColor.a, 0.001f);
            Assert.AreEqual(0.42f, view.StartGameButton.colors.highlightedColor.a, 0.001f);
            Assert.AreEqual(0.58f, view.StartGameButton.colors.pressedColor.a, 0.001f);
            Assert.AreEqual(0f, view.StartGameButton.colors.disabledColor.a, 0.001f);
            Assert.AreEqual(new Color(0.22f, 0.17f, 0.12f, 1f), view.StartGameLabel.color);
            Assert.AreEqual(
                new Vector2(0f, 100f),
                ((RectTransform)view.StartGameButton.transform).anchoredPosition);
            Assert.AreEqual(44f, view.StartGameLabel.fontSize);
            Assert.NotNull(view.CollectionButton);
            Assert.NotNull(view.CollectionLabel);
            Assert.AreEqual("图鉴", view.CollectionLabel.text);
            Assert.AreEqual(
                new Vector2(0f, -50f),
                ((RectTransform)view.CollectionButton.transform).anchoredPosition);
            Assert.AreEqual(44f, view.CollectionLabel.fontSize);
            Assert.NotNull(view.ExitGameButton);
            Assert.NotNull(view.ExitGameLabel);
            Assert.AreEqual("退出游戏", view.ExitGameLabel.text);
            Assert.AreEqual(
                new Vector2(0f, -190f),
                ((RectTransform)view.ExitGameButton.transform).anchoredPosition);
            Assert.AreEqual(44f, view.ExitGameLabel.fontSize);
            Assert.AreEqual(Navigation.Mode.None, view.ExitGameButton.navigation.mode);
            Assert.AreEqual(Selectable.Transition.ColorTint, view.ExitGameButton.transition);
            Assert.AreEqual(
                "MainMenuStartHoverWetParchment",
                ((Image)view.ExitGameButton.targetGraphic).sprite.name);
            Assert.AreEqual(0f, view.ExitGameButton.colors.normalColor.a, 0.001f);
            Assert.That(
                view.ExitGameButton.colors.highlightedColor.r,
                Is.GreaterThan(view.ExitGameButton.colors.highlightedColor.g));
            Assert.That(
                view.ExitGameButton.colors.highlightedColor.r,
                Is.GreaterThan(view.ExitGameButton.colors.highlightedColor.b));
            Assert.AreEqual(0.48f, view.ExitGameButton.colors.highlightedColor.a, 0.001f);
            Assert.AreEqual(0.64f, view.ExitGameButton.colors.pressedColor.a, 0.001f);
            Assert.NotNull(view.VersionLabel);
            Assert.AreEqual("v0.1.0", view.VersionLabel.text);
            Assert.AreEqual(TextAlignmentOptions.MidlineLeft, view.VersionLabel.alignment);
            Assert.AreEqual(new Vector2(0f, 0f), view.VersionLabel.rectTransform.anchorMin);
            Assert.AreEqual(new Vector2(148f, 38f), view.VersionLabel.rectTransform.anchoredPosition);
            Assert.AreEqual(Color.black, view.VersionLabel.color);
            Assert.IsFalse(view.VersionLabel.raycastTarget);

            var cover = prefab.transform.Find("Cover");
            Assert.NotNull(cover);
            Assert.AreEqual(0, cover.GetSiblingIndex());
            var coverImage = cover.GetComponent<Image>();
            Assert.NotNull(coverImage);
            Assert.NotNull(coverImage.sprite);
            Assert.AreEqual("MainMenuParchmentBackground", coverImage.sprite.name);
            Assert.IsFalse(coverImage.raycastTarget);
            Assert.IsFalse(coverImage.preserveAspect);
            Assert.Less(view.LeftGoblinImage.transform.GetSiblingIndex(), view.GameTitle.transform.GetSiblingIndex());
            Assert.Less(view.RightGoblinImage.transform.GetSiblingIndex(), view.GameTitle.transform.GetSiblingIndex());
        }

        [Test]
        public void ProjectBundleVersionMatchesMainMenuVersionLabel()
        {
            Assert.AreEqual("0.1.0", PlayerSettings.bundleVersion);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefabPath);
            Assert.NotNull(prefab);
            Assert.AreEqual(
                $"v{PlayerSettings.bundleVersion}",
                prefab.GetComponent<MainMenuView>().VersionLabel.text);
        }

        [Test]
        public void MainMenuCoverUsesWideSpriteImportSettings()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CoverPath);
            var importer = AssetImporter.GetAtPath(CoverPath) as TextureImporter;
            Assert.NotNull(texture);
            Assert.NotNull(importer);
            Assert.AreEqual(1672, texture.width);
            Assert.AreEqual(941, texture.height);
            Assert.That((float)texture.width / texture.height, Is.EqualTo(16f / 9f).Within(0.002f));
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.IsFalse(importer.alphaIsTransparency);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode);
        }

        [Test]
        public void MainMenuGoblinSheetsImportAsTwelveOrderedFrames()
        {
            AssertAnimationSheet(WarriorFramesPath, "Warrior");
            AssertAnimationSheet(ArcherFramesPath, "Archer");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefabPath);
            Assert.NotNull(prefab);
            var view = prefab.GetComponent<MainMenuView>();
            AssertFrameAnchorsAreStable(WarriorFramesPath, view.LeftGoblinFrameOffsets);
            AssertFrameAnchorsAreStable(ArcherFramesPath, view.RightGoblinFrameOffsets);
        }

        [Test]
        public void MainMenuGoblinAnimationPlaysForwardAndBackwardWithoutPauses()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefabPath);
            Assert.NotNull(prefab);
            var prefabView = prefab.GetComponent<MainMenuView>();
            var viewRoot = new GameObject("MainMenuAnimationTestView");
            var controllerRoot = new GameObject("MainMenuAnimationTestController");
            try
            {
                var view = viewRoot.AddComponent<MainMenuView>();
                view.LeftGoblinImage = new GameObject("Left", typeof(RectTransform), typeof(Image))
                    .GetComponent<Image>();
                view.LeftGoblinImage.transform.SetParent(viewRoot.transform, false);
                view.RightGoblinImage = new GameObject("Right", typeof(RectTransform), typeof(Image))
                    .GetComponent<Image>();
                view.RightGoblinImage.transform.SetParent(viewRoot.transform, false);
                view.LeftGoblinFrames = prefabView.LeftGoblinFrames;
                view.RightGoblinFrames = prefabView.RightGoblinFrames;
                view.LeftGoblinFrameOffsets = prefabView.LeftGoblinFrameOffsets;
                view.RightGoblinFrameOffsets = prefabView.RightGoblinFrameOffsets;

                var controller = controllerRoot.AddComponent<MainMenuController>();
                controller.SetView(view);
                InvokeControllerLifecycle(controller, "OnUiShow");
                Assert.AreSame(view.LeftGoblinFrames[0], view.LeftGoblinImage.sprite);
                Assert.AreSame(view.RightGoblinFrames[0], view.RightGoblinImage.sprite);
                Assert.AreEqual(view.LeftGoblinFrameOffsets[0], view.LeftGoblinImage.rectTransform.anchoredPosition);
                Assert.AreEqual(view.RightGoblinFrameOffsets[0], view.RightGoblinImage.rectTransform.anchoredPosition);

                InvokeControllerUpdate(controller, 0.3f);
                Assert.AreSame(view.LeftGoblinFrames[1], view.LeftGoblinImage.sprite);
                for (var index = 2; index < 12; ++index)
                {
                    InvokeControllerUpdate(controller, 0.3f);
                    Assert.AreSame(view.LeftGoblinFrames[index], view.LeftGoblinImage.sprite);
                    Assert.AreSame(view.RightGoblinFrames[index], view.RightGoblinImage.sprite);
                    Assert.AreEqual(
                        view.LeftGoblinFrameOffsets[index],
                        view.LeftGoblinImage.rectTransform.anchoredPosition);
                    Assert.AreEqual(
                        view.RightGoblinFrameOffsets[index],
                        view.RightGoblinImage.rectTransform.anchoredPosition);
                }

                InvokeControllerUpdate(controller, 0.3f);
                Assert.AreSame(view.LeftGoblinFrames[10], view.LeftGoblinImage.sprite);
                for (var index = 9; index >= 0; --index)
                {
                    InvokeControllerUpdate(controller, 0.3f);
                    Assert.AreSame(view.LeftGoblinFrames[index], view.LeftGoblinImage.sprite);
                    Assert.AreSame(view.RightGoblinFrames[index], view.RightGoblinImage.sprite);
                }

                InvokeControllerUpdate(controller, 0.3f);
                Assert.AreSame(view.LeftGoblinFrames[1], view.LeftGoblinImage.sprite);

                InvokeControllerLifecycle(controller, "OnUiHide");
                InvokeControllerUpdate(controller, 10f);
                Assert.AreSame(view.LeftGoblinFrames[1], view.LeftGoblinImage.sprite);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(controllerRoot);
                UnityEngine.Object.DestroyImmediate(viewRoot);
            }
        }

        [Test]
        public void MainMenuTitleAndHoverArtUseTransparentSpriteImportSettings()
        {
            AssertTransparentSprite(TitlePath, "MainMenuTitle");
            AssertTransparentSprite(StartHoverPath, "MainMenuStartHoverWetParchment");
            AssertTransparentSprite(SharedControlPath, "MedievalParchmentControl");
        }

        [Test]
        public void GeneratedUiPrefabsDoNotReferenceLegacyGlossyRedControls()
        {
            var legacySprites = new[]
            {
                "PreparationContinueButtonIdle",
                "PreparationContinueButtonHighlighted",
                "PreparationContinueButtonPressed",
                "PreparationContinueButtonWaiting",
                "PreparationFusionButtonEnabled",
                "PreparationFusionButtonPressed",
                "PreparationFusionButtonDisabled",
                "PreparationTabIdleV2",
                "PreparationTabSelectedV2",
                "PreparationStageTitleFrame",
            };
            var prefabPaths = new[]
            {
                ViewPrefabPath,
                "Assets/Resources/Ui/PreparationView.prefab",
                "Assets/Resources/Ui/FusionRecommendationItem.prefab",
            };

            foreach (var prefabPath in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.NotNull(prefab, prefabPath);
                foreach (var image in prefab.GetComponentsInChildren<Image>(true))
                {
                    if (image.sprite == null)
                        continue;
                    CollectionAssert.DoesNotContain(
                        legacySprites,
                        image.sprite.name,
                        $"{prefabPath}/{image.transform.name}");
                }
            }
        }

        [Test]
        public void MainMenuUiSceneAssetContainsConnectedDefaultView()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UiSceneAsset>(UiSceneAssetPath);
            Assert.NotNull(asset);
            Assert.AreEqual(2, asset.UiObjectDatas.Count);
            var data = asset.UiObjectDatas.Single(item => item.PrefabPath == "Ui/MainMenuView");
            Assert.AreEqual("Ui/MainMenuView", data.PrefabPath);
            Assert.AreEqual((int)EMainMenuUiGroup.Main, data.UiGroup);
            Assert.IsTrue(data.DefaultShow);
            Assert.AreEqual(Vector3.zero, data.Position);
            Assert.AreEqual(Vector3.one, data.Scale);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), data.Pivot);

            var scene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);
            try
            {
                var exporter = scene.GetRootGameObjects()
                    .Select(root => root.GetComponent<UiSceneExporter>())
                    .FirstOrDefault(component => component != null);
                Assert.NotNull(exporter);
                Assert.AreEqual(typeof(EMainMenuUiGroup).FullName, exporter.FullUiGroupType);
                Assert.NotNull(exporter.UiGroups);
                Assert.AreEqual(1, exporter.UiGroups.Count);

                var view = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MainMenuView>(true))
                    .Single();
                Assert.AreEqual(
                    PrefabInstanceStatus.Connected,
                    PrefabUtility.GetPrefabInstanceStatus(view.gameObject));
                var collectionView = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<CardCollectionView>(true))
                    .Single();
                Assert.AreEqual(
                    PrefabInstanceStatus.Connected,
                    PrefabUtility.GetPrefabInstanceStatus(collectionView.gameObject));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void MainMenuControllerDisablesDuplicateStartRequests()
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/MainMenuController.cs");
            Assert.NotNull(script);
            StringAssert.Contains("if (m_StartRequested)", script.text);
            StringAssert.Contains("m_View.StartGameButton.interactable = false", script.text);
            StringAssert.Contains("HearthstoneGameEngine.Instance.StartNewRun()", script.text);
            StringAssert.Contains("m_View.ExitGameButton.onClick.AddListener(OnExitGameClicked)", script.text);
            StringAssert.Contains("Application.Quit()", script.text);
        }

        private static void AssertTransparentSprite(string path, string expectedName)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.NotNull(texture, path);
            Assert.NotNull(sprite, path);
            Assert.AreEqual(expectedName, sprite.name);
            Assert.NotNull(importer, path);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.IsTrue(importer.alphaIsTransparency);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode);
        }

        private static void AssertAnimationSheet(string path, string framePrefix)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var frames = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            Assert.NotNull(texture, path);
            Assert.NotNull(importer, path);
            Assert.AreEqual(1536, texture.width, path);
            Assert.AreEqual(1024, texture.height, path);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, path);
            Assert.AreEqual(SpriteImportMode.Multiple, importer.spriteImportMode, path);
            Assert.IsFalse(importer.mipmapEnabled, path);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode, path);
            Assert.AreEqual(12, frames.Length, path);
            for (var index = 0; index < frames.Length; ++index)
            {
                Assert.AreEqual($"{framePrefix}_{index:00}", frames[index].name, path);
                Assert.AreEqual(
                    GetExpectedAnimationFrameRect(framePrefix, index),
                    frames[index].rect,
                    $"{path} frame {index}");
            }
        }

        private static Rect GetExpectedAnimationFrameRect(string framePrefix, int frameIndex)
        {
            var bounds = framePrefix == "Warrior"
                ? WarriorFrameHorizontalBounds[frameIndex]
                : ArcherFrameHorizontalBounds[frameIndex];
            var x = bounds.x - FrameHorizontalPadding;
            var width = bounds.y - bounds.x + 1 + FrameHorizontalPadding * 2;
            var y = frameIndex < 6 ? 512f : 0f;
            return new Rect(x, y, width, 512f);
        }

        private static void AssertGoblinImage(
            Image image,
            Sprite[] frames,
            Vector2[] frameOffsets,
            string framePrefix,
            Vector2 anchor,
            Vector2 position)
        {
            Assert.NotNull(image);
            Assert.NotNull(frames);
            Assert.AreEqual(12, frames.Length);
            Assert.NotNull(frameOffsets);
            Assert.AreEqual(12, frameOffsets.Length);
            Assert.AreSame(frames[0], image.sprite);
            Assert.AreEqual($"{framePrefix}_00", image.sprite.name);
            Assert.NotNull(image.material);
            Assert.AreEqual("Hearthstone/UI/MainMenuSilhouetteKey", image.material.shader.name);
            Assert.IsTrue(image.preserveAspect);
            Assert.IsFalse(image.raycastTarget);
            Assert.AreEqual(anchor, image.rectTransform.anchorMin);
            Assert.AreEqual(anchor, image.rectTransform.anchorMax);
            Assert.AreEqual(position, image.rectTransform.anchoredPosition);
            Assert.AreEqual(new Vector2(560f, 760f), image.rectTransform.sizeDelta);
        }

        private static void AssertFrameAnchorsAreStable(string path, Vector2[] frameOffsets)
        {
            Assert.NotNull(frameOffsets, path);
            Assert.AreEqual(12, frameOffsets.Length, path);
            var texture = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path);
                var pixels = texture.GetPixels32();
                var frames = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>()
                    .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                    .ToArray();
                float? adjustedReference = null;
                float? adjustedCenterReference = null;
                for (var index = 0; index < 12; ++index)
                {
                    var rect = frames[index].rect;
                    var originX = Mathf.RoundToInt(rect.x);
                    var originY = Mathf.RoundToInt(rect.y);
                    var spriteWidth = Mathf.RoundToInt(rect.width);
                    var spriteHeight = Mathf.RoundToInt(rect.height);
                    var baseline = spriteHeight;
                    var foregroundMinX = spriteWidth;
                    var foregroundMaxX = -1;
                    for (var localY = 0; localY < spriteHeight; ++localY)
                    {
                        for (var localX = 0; localX < spriteWidth; ++localX)
                        {
                            var pixel = pixels[(originY + localY) * texture.width + originX + localX];
                            var luminance = (77 * pixel.r + 150 * pixel.g + 29 * pixel.b) >> 8;
                            if (luminance < 235)
                            {
                                baseline = Mathf.Min(baseline, localY);
                                foregroundMinX = Mathf.Min(foregroundMinX, localX);
                                foregroundMaxX = Mathf.Max(foregroundMaxX, localX);
                            }
                        }
                    }

                    Assert.Less(baseline, spriteHeight, $"{path} frame {index}");
                    Assert.That(
                        foregroundMinX,
                        Is.GreaterThanOrEqualTo(4),
                        $"{path} frame {index} left edge");
                    Assert.That(
                        foregroundMaxX,
                        Is.LessThanOrEqualTo(spriteWidth - 5),
                        $"{path} frame {index} right edge");
                    var displayScale = Mathf.Min(560f / spriteWidth, 760f / spriteHeight);
                    var adjustedBaseline = (baseline - spriteHeight * 0.5f) * displayScale +
                                           frameOffsets[index].y;
                    adjustedReference ??= adjustedBaseline;
                    Assert.That(
                        adjustedBaseline,
                        Is.EqualTo(adjustedReference.Value).Within(displayScale + 0.01f),
                        $"{path} frame {index}");

                    var groundMinX = spriteWidth;
                    var groundMaxX = -1;
                    var groundBandTop = Mathf.Min(spriteHeight, baseline + 80);
                    for (var localY = baseline; localY < groundBandTop; ++localY)
                    {
                        for (var localX = 0; localX < spriteWidth; ++localX)
                        {
                            var pixel = pixels[(originY + localY) * texture.width + originX + localX];
                            var luminance = (77 * pixel.r + 150 * pixel.g + 29 * pixel.b) >> 8;
                            if (luminance >= 235)
                                continue;
                            groundMinX = Mathf.Min(groundMinX, localX);
                            groundMaxX = Mathf.Max(groundMaxX, localX);
                        }
                    }

                    Assert.GreaterOrEqual(groundMaxX, groundMinX, $"{path} frame {index}");
                    var adjustedCenter =
                        ((groundMinX + groundMaxX) * 0.5f - spriteWidth * 0.5f) * displayScale +
                        frameOffsets[index].x;
                    adjustedCenterReference ??= adjustedCenter;
                    Assert.That(
                        adjustedCenter,
                        Is.EqualTo(adjustedCenterReference.Value).Within(displayScale + 0.01f),
                        $"{path} frame {index}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void InvokeControllerLifecycle(MainMenuController controller, string methodName)
        {
            typeof(MainMenuController)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, null);
        }

        private static void InvokeControllerUpdate(MainMenuController controller, float deltaTime)
        {
            typeof(MainMenuController)
                .GetMethod("OnUiUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, new object[] { deltaTime });
        }
    }
}
