using BbxCommon;
using BbxCommon.Ui;
using UnityEngine;

namespace Hearthstone
{
    /// <summary>
    /// 空项目占位 Stage。它让 ECS 基础流程可以直接运行，并在 UI 资产存在时接入占位页面。
    /// </summary>
    public static class PlaceholderStages
    {
        private const string PLACEHOLDER_UI_ASSET_PATH = "Ui/Placeholder";

        public static GameStage CreateBaseStage(HearthstoneGameEngine engine)
        {
            var stage = engine.StageWrapper.CreateStage("BaseStage");
            stage.AddLoadItem<InitializePlaceholderState>();
            stage.AddUpdateSystem<PlaceholderStateSystem>();

            TryAddPlaceholderUi(engine, stage);
            return stage;
        }

        private static void TryAddPlaceholderUi(HearthstoneGameEngine engine, GameStage stage)
        {
            if (engine.UiCanvasProto == null)
                return;

            var uiSceneAsset = Resources.Load<UiSceneAsset>(PLACEHOLDER_UI_ASSET_PATH);
            if (uiSceneAsset == null)
                return;

            var uiScene = engine.GetOrCreateUiScene<PlaceholderUiScene>();
            stage.SetUiScene(uiScene, uiSceneAsset);
        }

        public sealed class InitializePlaceholderState : IStageLoad
        {
            public void Load(GameStage stage)
            {
                EcsApi.AddSingletonRawComponent<PlaceholderStateSingletonRawComponent>();
            }

            public void Unload(GameStage stage)
            {
                EcsApi.RemoveSingletonRawComponent<PlaceholderStateSingletonRawComponent>();
            }
        }
    }
}
