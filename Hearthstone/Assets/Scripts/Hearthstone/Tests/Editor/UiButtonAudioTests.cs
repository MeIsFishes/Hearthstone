using System;
using BbxCommon;
using BbxCommon.Ui;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hearthstone.Tests
{
    public sealed class UiButtonAudioTests
    {
        [Test]
        public void UiViewBaseRegistersOneClickSoundForItsOwnButtons()
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/BbxCommon/Ui/Mvc/UiViewBase.cs");
            Assert.NotNull(script);

            StringAssert.Contains("InitButtonClickAudio();", script.text);
            StringAssert.Contains("GetComponentsInChildren<Button>(true)", script.text);
            StringAssert.Contains(
                "button.GetComponentInParent<UiViewBase>(true) == this",
                script.text);
            StringAssert.Contains(
                "button.onClick.AddListener(PlayButtonClickAudio)",
                script.text);
            StringAssert.Contains("if (m_ButtonClickAudioInitialized)", script.text);
            StringAssert.Contains(
                "AudioApi.Play(ButtonClickAudioKey, ButtonClickAudioVolume)",
                script.text);
        }

        [Test]
        public void DefaultButtonClickAudioAssetExists()
        {
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Resources/BbxCommon/Audio/Library/Interface Sounds/click_001.ogg"));
        }

        [Test]
        public void BgmAssetsExist()
        {
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Resources/BGM/Lobby.mp3"));
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Resources/BGM/Battle.mp3"));
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Resources/BGM/Win.mp3"));
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Resources/BGM/Failed.mp3"));
        }

        [Test]
        public void BgmAssetsAreInPlayerResourceIndex()
        {
            var resourceIndex = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/Resources/ResourcesDictionary.json");

            Assert.NotNull(resourceIndex);
            StringAssert.IsMatch("\\\"(?:\\d+, )?Key\\\":\\\"Lobby\\\",\\\"(?:\\d+, )?Value\\\":\\\"BGM/Lobby\\\"", resourceIndex.text);
            StringAssert.IsMatch("\\\"(?:\\d+, )?Key\\\":\\\"Battle\\\",\\\"(?:\\d+, )?Value\\\":\\\"BGM/Battle\\\"", resourceIndex.text);
            StringAssert.IsMatch("\\\"(?:\\d+, )?Key\\\":\\\"Win\\\",\\\"(?:\\d+, )?Value\\\":\\\"BGM/Win\\\"", resourceIndex.text);
            StringAssert.IsMatch("\\\"(?:\\d+, )?Key\\\":\\\"Failed\\\",\\\"(?:\\d+, )?Value\\\":\\\"BGM/Failed\\\"", resourceIndex.text);
        }

        [Test]
        public void SetBgmTransitionDurationIsOptional()
        {
            var method = typeof(AudioApi).GetMethod(
                nameof(AudioApi.SetBgm),
                new[] { typeof(string), typeof(float), typeof(bool) });

            Assert.NotNull(method);
            var parameters = method.GetParameters();
            Assert.IsTrue(parameters[1].IsOptional);
            Assert.AreEqual(0f, parameters[1].DefaultValue);
            Assert.IsTrue(parameters[2].IsOptional);
            Assert.AreEqual(true, parameters[2].DefaultValue);
        }

        [Test]
        public void StageGroupsAndBattleResultsSetExpectedBgm()
        {
            var engineScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Bootstrap/HearthstoneGameEngine.cs");
            var resultListenerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/GameStage/BattleResultPreparationStageListener.cs");
            var battleStagesScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/GameStage/BattleStages.cs");

            Assert.NotNull(engineScript);
            Assert.NotNull(resultListenerScript);
            Assert.NotNull(battleStagesScript);
            StringAssert.Contains("private const string LobbyBgmKey = \"Lobby\"", engineScript.text);
            StringAssert.Contains("private const string Battle1BgmKey = \"Battle\"", engineScript.text);
            StringAssert.Contains("AudioApi.SetBgm(LobbyBgmKey)", engineScript.text);
            StringAssert.Contains("AudioApi.SetBgm(Battle1BgmKey)", engineScript.text);
            StringAssert.Contains("private const string VictoryBgmKey = \"Win\"", resultListenerScript.text);
            StringAssert.Contains("private const string DefeatBgmKey = \"Failed\"", resultListenerScript.text);
            StringAssert.Contains("private const float ResultBgmTransitionDuration = 0.5f", resultListenerScript.text);
            StringAssert.Contains("loop: false", resultListenerScript.text);
            StringAssert.Contains("AddStageListener<BattleBgmStageListener>()", battleStagesScript.text);
        }
    }
}
