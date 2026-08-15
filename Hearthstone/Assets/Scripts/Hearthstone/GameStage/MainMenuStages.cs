using System;
using BbxCommon;
using BbxCommon.Ui;
using UnityEngine;

namespace Hearthstone
{
    public static class MainMenuStages
    {
        private const string MainMenuUiAssetPath = "Ui/MainMenu";

        public static GameStage CreateMainMenuStage(HearthstoneGameEngine engine)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            var stage = engine.StageWrapper.CreateStage("MainMenuStage");
            if (engine.UiCanvasProto == null)
            {
                DebugApi.LogError("MainMenuStage cannot load UI because UiCanvasProto is missing.");
                return stage;
            }

            var asset = Resources.Load<UiSceneAsset>(MainMenuUiAssetPath);
            if (asset == null)
            {
                DebugApi.LogError($"Main menu UiSceneAsset is missing at Resources/{MainMenuUiAssetPath}.");
                return stage;
            }

            stage.SetUiScene(engine.GetOrCreateUiScene<MainMenuUiScene>(), asset);
            return stage;
        }
    }
}
