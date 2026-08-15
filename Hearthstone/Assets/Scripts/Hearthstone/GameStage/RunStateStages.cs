using System;
using BbxCommon;

namespace Hearthstone
{
    public static class RunStateStages
    {
        public static GameStage CreateRunStateStage(HearthstoneGameEngine engine)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));
            var stage = engine.StageWrapper.CreateStage("RunStateStage");
            stage.AddLoadItem<InitializeRunStateRuntime>();
            return stage;
        }

        public sealed class InitializeRunStateRuntime : IStageLoad
        {
            public void Load(GameStage stage)
            {
                if (EcsApi.GetSingletonRawComponent<RunStateSingletonRawComponent>() != null)
                    throw new InvalidOperationException("RunStateSingletonRawComponent already exists.");
                EcsApi.AddSingletonRawComponent<RunStateSingletonRawComponent>();
            }

            public void Unload(GameStage stage)
            {
                EcsApi.RemoveSingletonRawComponent<RunStateSingletonRawComponent>();
            }
        }
    }
}
