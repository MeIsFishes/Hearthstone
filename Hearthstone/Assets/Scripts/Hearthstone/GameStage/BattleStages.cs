using System;
using BbxCommon;
using BbxCommon.Ui;
using Unity.Entities;
using UnityEngine;

namespace Hearthstone
{
    public static class BattleStages
    {
        private const string BattleStartupDataKey = "BattleStage.StartupData";
        private const string BattleUiAssetPath = "Ui/Battle";

        public static GameStage CreateBattleStage(
            HearthstoneGameEngine engine,
            BattleStageStartupData startupData)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));
            if (startupData == null)
                throw new ArgumentNullException(nameof(startupData));

            var stage = engine.StageWrapper.CreateStage("BattleStage");
            stage.SetStageData(BattleStartupDataKey, startupData.CreateSnapshot());
            stage.AddLoadItem<InitializeBattleRuntime>();
            stage.AddUpdateSystem<BattleSystem>();
            stage.AddStageListener<BattleResultPreparationStageListener>();
            TryAddBattleUi(engine, stage);
            return stage;
        }

        private static void TryAddBattleUi(HearthstoneGameEngine engine, GameStage stage)
        {
            if (engine.UiCanvasProto == null)
            {
                DebugApi.LogError("BattleStage cannot load UI because UiCanvasProto is missing.");
                return;
            }

            var uiSceneAsset = Resources.Load<UiSceneAsset>(BattleUiAssetPath);
            if (uiSceneAsset == null)
            {
                DebugApi.LogError($"Battle UiSceneAsset is missing at Resources/{BattleUiAssetPath}.");
                return;
            }

            var uiScene = engine.GetOrCreateUiScene<BattleUiScene>();
            stage.SetUiScene(uiScene, uiSceneAsset);
        }

        public sealed class InitializeBattleRuntime : IStageLoad
        {
            public void Load(GameStage stage)
            {
                var startupData = stage.GetStageData(BattleStartupDataKey) as BattleStageStartupData;
                if (startupData == null)
                    throw new InvalidOperationException("BattleStage startup data is missing or invalid.");
                var runState = EcsApi.GetSingletonRawComponent<RunStateSingletonRawComponent>();
                if (runState == null)
                    throw new InvalidOperationException("BattleStage requires an active RunStateStage.");

                var session = EcsApi.AddSingletonRawComponent<BattleSessionSingletonRawComponent>();
                if (session == null)
                    throw new InvalidOperationException("Unable to create BattleSessionSingletonRawComponent.");

                session.Initialize(CreateRandomSeed(), startupData.PreparationRewardBatch);
                try
                {
                    EnsureInitialPlayerLineup(runState, ref session.TargetRandom);
                    CreatePlayerCards(runState, session.PlayerCards);
                    CreateEnemyCards(session.EnemyCards, ref session.TargetRandom);
                }
                catch
                {
                    DestroyCards(session.PlayerCards);
                    DestroyCards(session.EnemyCards);
                    EcsApi.RemoveSingletonRawComponent<BattleSessionSingletonRawComponent>();
                    throw;
                }
            }

            public void Unload(GameStage stage)
            {
                var session = EcsApi.GetSingletonRawComponent<BattleSessionSingletonRawComponent>();
                if (session == null)
                    return;

                DestroyCards(session.PlayerCards);
                DestroyCards(session.EnemyCards);
                EcsApi.RemoveSingletonRawComponent<BattleSessionSingletonRawComponent>();
            }

            private static void EnsureInitialPlayerLineup(
                RunStateSingletonRawComponent runState,
                ref Unity.Mathematics.Random random)
            {
                if (runState.GetOwnedCardCount() != 0)
                    return;

                var cards = new RunCardInstanceData[BattleRules.CardsPerSide];
                for (var slot = 0; slot < BattleRules.CardsPerSide; slot++)
                {
                    var cardNumber = BattleRules.GetCardNumber(EBattleSide.Player, slot);
                    var cardConfig = DataApi.GetData<BattleCardCsvData>(cardNumber);
                    if (cardConfig == null)
                        throw new InvalidOperationException($"Initial player card configuration {cardNumber} is missing.");

                    var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
                    if (typeConfig == null)
                        throw new InvalidOperationException($"Initial player card type {cardConfig.CardTypeId} is missing.");

                    cards[slot] = new RunCardInstanceData(
                        cardNumber,
                        typeConfig.RollAttack(ref random),
                        typeConfig.RollHealth(ref random));
                }
                RunCardRules.InitializeFirstBattleLineup(runState, cards);
            }

            private static void CreatePlayerCards(
                RunStateSingletonRawComponent runState,
                Entity[] destination)
            {
                for (var slot = 0; slot < BattleRules.CardsPerSide; slot++)
                {
                    var cardNumber = runState.BattleSlotCardNumbers[slot];
                    if (cardNumber == 0 || runState.HasCard(cardNumber) == false)
                        throw new InvalidOperationException($"Run state battle slot {slot} has no valid card instance.");

                    var entity = EcsApi.CreateEntity(BattleRules.CardEntityGroup);
                    var card = entity.AddRawComponent<BattleCardRawComponent>();
                    card.InitializePlayer(slot, runState.CardInstances[cardNumber]);
                    destination[slot] = entity;
                }
            }

            private static void CreateEnemyCards(
                Entity[] destination,
                ref Unity.Mathematics.Random random)
            {
                for (var slot = 0; slot < BattleRules.CardsPerSide; slot++)
                {
                    var cardNumber = BattleRules.GetCardNumber(EBattleSide.Enemy, slot);
                    var cardConfig = DataApi.GetData<BattleCardCsvData>(cardNumber);
                    if (cardConfig == null)
                        throw new InvalidOperationException($"Enemy card configuration {cardNumber} is missing.");
                    var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
                    if (typeConfig == null)
                        throw new InvalidOperationException($"Enemy card type {cardConfig.CardTypeId} is missing.");

                    var entity = EcsApi.CreateEntity(BattleRules.CardEntityGroup);
                    var card = entity.AddRawComponent<BattleCardRawComponent>();
                    card.Initialize(EBattleSide.Enemy, slot, cardConfig, typeConfig, ref random);
                    destination[slot] = entity;
                }
            }

            private static void DestroyCards(Entity[] cards)
            {
                for (var slot = 0; slot < cards.Length; slot++)
                {
                    if (cards[slot] != Entity.Null)
                        EcsApi.DestroyEntity(cards[slot]);
                    cards[slot] = Entity.Null;
                }
            }

            private static uint CreateRandomSeed()
            {
                return BattleRules.NormalizeSeed(unchecked((uint)DateTime.UtcNow.Ticks));
            }
        }
    }
}
