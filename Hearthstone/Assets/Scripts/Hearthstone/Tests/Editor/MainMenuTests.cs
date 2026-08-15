using System;
using System.Linq;
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
        private const string CoverPath = "Assets/Resources/Art/MainMenu/UI/MainMenuCover.png";
        private const string UiSceneAssetPath = "Assets/Resources/Ui/MainMenu.asset";
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
        public void MainMenuPrefabContainsTitleCoverAndStartButton()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ViewPrefabPath);
            Assert.NotNull(prefab);
            var view = prefab.GetComponent<MainMenuView>();
            Assert.NotNull(view);
            Assert.IsTrue(view.DefaultShow);
            Assert.NotNull(view.GameTitle);
            Assert.AreEqual("99升变", view.GameTitle.text);
            Assert.NotNull(view.StartGameButton);
            Assert.NotNull(view.StartGameLabel);
            Assert.AreEqual("开始游戏", view.StartGameLabel.text);
            Assert.AreEqual(Navigation.Mode.None, view.StartGameButton.navigation.mode);
            Assert.AreEqual(Selectable.Transition.SpriteSwap, view.StartGameButton.transition);
            Assert.NotNull(view.StartGameButton.spriteState.highlightedSprite);
            Assert.NotNull(view.StartGameButton.spriteState.pressedSprite);
            Assert.NotNull(view.StartGameButton.spriteState.disabledSprite);

            var cover = prefab.transform.Find("Cover");
            Assert.NotNull(cover);
            Assert.AreEqual(0, cover.GetSiblingIndex());
            var coverImage = cover.GetComponent<Image>();
            Assert.NotNull(coverImage);
            Assert.NotNull(coverImage.sprite);
            Assert.AreEqual("MainMenuCover", coverImage.sprite.name);
            Assert.IsFalse(coverImage.raycastTarget);
            Assert.IsFalse(coverImage.preserveAspect);
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
        public void MainMenuUiSceneAssetContainsConnectedDefaultView()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UiSceneAsset>(UiSceneAssetPath);
            Assert.NotNull(asset);
            Assert.AreEqual(1, asset.UiObjectDatas.Count);
            var data = asset.UiObjectDatas[0];
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
        }
    }
}
