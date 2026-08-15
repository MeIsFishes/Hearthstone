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

        public sealed class InitializeRunStateRuntime : ITransactionalStageLoad
        {
            public void Validate(GameStage stage, GameStageTransitionContext context)
            {
                if (EcsApi.GetSingletonRawComponent<RunStateSingletonRawComponent>() != null)
                    throw new InvalidOperationException("RunStateSingletonRawComponent already exists.");
                if (EcsApi.GetSingletonRawComponent<RunProgressionSingletonRawComponent>() != null)
                    throw new InvalidOperationException("RunProgressionSingletonRawComponent already exists.");
            }

            public void Prepare(GameStage stage, GameStageTransitionContext context) { }

            public void Load(GameStage stage)
            {
                if (EcsApi.GetSingletonRawComponent<RunStateSingletonRawComponent>() != null)
                    throw new InvalidOperationException("RunStateSingletonRawComponent already exists.");
                if (EcsApi.GetSingletonRawComponent<RunProgressionSingletonRawComponent>() != null)
                    throw new InvalidOperationException("RunProgressionSingletonRawComponent already exists.");

                EcsApi.AddSingletonRawComponent<RunStateSingletonRawComponent>();
                try
                {
                    if (EcsApi.AddSingletonRawComponent<RunProgressionSingletonRawComponent>() == null)
                        throw new InvalidOperationException("Unable to create RunProgressionSingletonRawComponent.");
                }
                catch
                {
                    EcsApi.RemoveSingletonRawComponent<RunStateSingletonRawComponent>();
                    throw;
                }
            }

            public void Unload(GameStage stage)
            {
                EcsApi.RemoveSingletonRawComponent<RunProgressionSingletonRawComponent>();
                EcsApi.RemoveSingletonRawComponent<RunStateSingletonRawComponent>();
            }

            public void Rollback(GameStage stage, GameStageTransitionContext context) => Unload(stage);
        }
    }
}
