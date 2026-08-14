using System;
using BbxCommon;
using BbxCommon.Ui;
using Unity.Entities;
using UnityEngine;

namespace Hearthstone
{
    public static class BattleStages
    {
        private const string BattleUiAssetPath = "Ui/Battle";

        public static GameStage CreateBattleStage(HearthstoneGameEngine engine)
        {
            var stage = engine.StageWrapper.CreateStage("BattleStage");
            stage.AddLoadItem<InitializeBattleRuntime>();
            stage.AddUpdateSystem<BattleSystem>();
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
                var session = EcsApi.AddSingletonRawComponent<BattleSessionSingletonRawComponent>();
                if (session == null)
                    throw new InvalidOperationException("Unable to create BattleSessionSingletonRawComponent.");

                session.Initialize(CreateRandomSeed());
                try
                {
                    CreateCards(EBattleSide.Player, session.PlayerCards, ref session.TargetRandom);
                    CreateCards(EBattleSide.Enemy, session.EnemyCards, ref session.TargetRandom);
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

            private static void CreateCards(
                EBattleSide side,
                Entity[] destination,
                ref Unity.Mathematics.Random random)
            {
                for (var slot = 0; slot < BattleRules.CardsPerSide; slot++)
                {
                    var cardNumber = BattleRules.GetCardNumber(side, slot);
                    var cardConfig = DataApi.GetData<BattleCardCsvData>(cardNumber);
                    if (cardConfig == null)
                    {
                        throw new InvalidOperationException(
                            $"Battle card configuration {cardNumber} is missing for {side} slot {slot}.");
                    }

                    var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
                    if (typeConfig == null)
                    {
                        throw new InvalidOperationException(
                            $"Battle card type {cardConfig.CardTypeId} is missing for card {cardNumber}.");
                    }

                    var entity = EcsApi.CreateEntity(BattleRules.CardEntityGroup);
                    var card = entity.AddRawComponent<BattleCardRawComponent>();
                    card.Initialize(side, slot, cardConfig, typeConfig, ref random);
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
