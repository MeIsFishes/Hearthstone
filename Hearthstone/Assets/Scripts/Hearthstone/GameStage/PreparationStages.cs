using System;
using BbxCommon;
using BbxCommon.Ui;
using UnityEngine;

namespace Hearthstone
{
    public static class PreparationStages
    {
        private const string PreparationStartupDataKey = "PreparationStage.RewardBatch";
        private const string PreparationUiAssetPath = "Ui/Preparation";

        public static GameStage CreatePreparationStage(
            HearthstoneGameEngine engine,
            PreparationRewardBatchStartupData rewardBatch)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));
            if (rewardBatch == null)
                throw new ArgumentNullException(nameof(rewardBatch));

            var stage = engine.StageWrapper.CreateStage("PreparationStage");
            stage.SetStageData(PreparationStartupDataKey, rewardBatch.CreateSnapshot());
            stage.AddLoadItem<InitializePreparationRuntime>();
            TryAddPreparationUi(engine, stage);
            return stage;
        }

        private static void TryAddPreparationUi(HearthstoneGameEngine engine, GameStage stage)
        {
            if (engine.UiCanvasProto == null)
            {
                DebugApi.LogError("PreparationStage cannot load UI because UiCanvasProto is missing.");
                return;
            }

            var asset = Resources.Load<UiSceneAsset>(PreparationUiAssetPath);
            if (asset == null)
            {
                DebugApi.LogError($"Preparation UiSceneAsset is missing at Resources/{PreparationUiAssetPath}.");
                return;
            }

            stage.SetUiScene(engine.GetOrCreateUiScene<PreparationUiScene>(), asset);
        }

        public sealed class InitializePreparationRuntime : IStageLoad
        {
            public void Load(GameStage stage)
            {
                var batch = stage.GetStageData(PreparationStartupDataKey) as PreparationRewardBatchStartupData;
                if (batch == null)
                    throw new InvalidOperationException("PreparationStage reward batch is missing or invalid.");

                ValidateGrantReferences(batch);
                var runState = EcsApi.GetSingletonRawComponent<RunStateSingletonRawComponent>();
                if (runState == null)
                    throw new InvalidOperationException("PreparationStage requires an active RunStateStage.");
                if (EcsApi.GetSingletonRawComponent<PreparationSessionSingletonRawComponent>() != null)
                    throw new InvalidOperationException("Preparation session already exists.");

                var result = RunCardRules.ApplyRewardBatch(runState, batch);
                var session = EcsApi.AddSingletonRawComponent<PreparationSessionSingletonRawComponent>();
                session.Initialize(batch, result == ERewardBatchApplyResult.Applied);
            }

            public void Unload(GameStage stage)
            {
                EcsApi.RemoveSingletonRawComponent<PreparationSessionSingletonRawComponent>();
            }

            private static void ValidateGrantReferences(PreparationRewardBatchStartupData batch)
            {
                for (var index = 0; index < batch.Grants.Count; index++)
                {
                    var grant = batch.Grants[index];
                    var card = DataApi.GetData<BattleCardCsvData>(grant.CardNumber);
                    if (card == null)
                        throw new InvalidOperationException($"Reward card configuration {grant.CardNumber} is missing.");
                    var type = DataApi.GetData<BattleCardTypeCsvData>(card.CardTypeId);
                    if (type == null)
                        throw new InvalidOperationException($"Reward card type {card.CardTypeId} is missing.");
                    if (grant.Attack < type.MinAttack || grant.Attack > type.MaxAttack ||
                        grant.MaxHealth < type.MinHealth || grant.MaxHealth > type.MaxHealth)
                    {
                        throw new InvalidOperationException(
                            $"Reward card {grant.CardNumber} permanent stats are outside type {card.CardTypeId} ranges.");
                    }
                }
            }
        }
    }
}
