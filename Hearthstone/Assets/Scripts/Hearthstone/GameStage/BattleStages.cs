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
            stage.AddStageListener<BattleBgmStageListener>();
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

                var scenario = startupData.Scenario;
                var enemyPreview = startupData.EnemyPreview;
                var randomSeed = scenario?.RandomSeed ?? enemyPreview?.RandomSeed ?? CreateRandomSeed();
                var lineupRandom = new Unity.Mathematics.Random(BattleRules.NormalizeSeed(randomSeed));
                var enemyLineup = scenario == null && enemyPreview == null
                    ? EnemyLineupCsvData.GetRandomRequired(startupData.BattleNumber, ref lineupRandom)
                    : null;
                var playerSlotCount = scenario?.SlotCount ??
                    startupData.ContinuePlayerLineup?.SlotCount ??
                    (runState.UnlockedBattleSlotCount == 0
                        ? RunCardRules.InitialBattleSlotCount
                        : runState.UnlockedBattleSlotCount);
                var enemySlotCount = scenario?.SlotCount ??
                    enemyPreview?.CardCount ??
                    enemyLineup.CardNumbers.Length;
                session.Initialize(
                    randomSeed,
                    startupData.PreparationRewardBatch,
                    startupData.BattleNumber,
                    BattleProgressionCsvData.HasBattle(startupData.BattleNumber + 1) == false,
                    playerSlotCount,
                    enemySlotCount);
                if (scenario == null)
                {
                    session.TargetRandom = enemyPreview == null
                        ? lineupRandom
                        : new Unity.Mathematics.Random(enemyPreview.TargetRandomState);
                }
                try
                {
                    EnsureInitialPlayerLineup(runState, ref session.TargetRandom);
                    CreatePlayerCards(runState, session.PlayerCards, scenario, startupData.ContinuePlayerLineup);
                    CreateEnemyCards(
                        session.EnemyCards,
                        ref session.TargetRandom,
                        scenario,
                        enemyLineup,
                        enemyPreview);
                    DebugApi.Log(
                        $"[PreparationContinue] BattleRuntimePrepared BattleNumber={startupData.BattleNumber} " +
                        $"Scenario={(scenario == null ? "Default" : "Explicit")} " +
                        $"Seed={session.RandomSeed} PlayerSlots={CountOccupied(session.PlayerCards)} " +
                        $"EnemySlots={CountOccupied(session.EnemyCards)}");
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

                runState.SetUnlockedBattleSlotCount(RunCardRules.InitialBattleSlotCount);
                var cards = new RunCardInstanceData[RunCardRules.InitialBattleSlotCount];
                for (var slot = 0; slot < cards.Length; slot++)
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
                Entity[] destination,
                BattleScenarioStartupData scenario,
                BattlePlayerLineupStartupData continueLineup)
            {
                for (var slot = 0; slot < destination.Length; slot++)
                {
                    var slotData = scenario?.GetPlayerSlot(slot) ?? default;
                    if (scenario != null && slotData.IsOccupied == false)
                    {
                        destination[slot] = Entity.Null;
                        DebugApi.Log($"[PreparationContinue] BattlePlayerSlot Slot={slot} State=Empty");
                        continue;
                    }

                    var capturedCard = continueLineup?.GetSlot(slot) ?? default;
                    var cardNumber = scenario != null
                        ? slotData.CardNumber
                        : continueLineup != null
                            ? capturedCard.CardNumber
                            : runState.BattleSlotCardNumbers[slot];
                    if (cardNumber == 0)
                    {
                        destination[slot] = Entity.Null;
                        DebugApi.Log($"[PreparationContinue] BattlePlayerSlot Slot={slot} State=Empty Source=RunState");
                        continue;
                    }
                    if (runState.HasCard(cardNumber) == false)
                        throw new InvalidOperationException($"Run state battle slot {slot} has no valid card instance.");
                    if (scenario != null && runState.BattleSlotCardNumbers[slot] != cardNumber)
                    {
                        throw new InvalidOperationException(
                            $"Battle scenario player card {cardNumber} does not match run battle slot {slot}.");
                    }

                    var entity = EcsApi.CreateEntity(BattleRules.CardEntityGroup);
                    var card = entity.AddRawComponent<BattleCardRawComponent>();
                    var instance = continueLineup == null ? runState.CardInstances[cardNumber] : capturedCard;
                    if (scenario == null || slotData.StatSource == EBattleCardStatSource.RunState)
                    {
                        card.InitializePlayer(slot, instance);
                    }
                    else
                    {
                        card.InitializePlayerExplicit(
                            slot,
                            instance,
                            slotData.Attack,
                            slotData.MaxHealth,
                            slotData.CurrentHealth);
                    }
                    destination[slot] = entity;
                    DebugApi.Log(
                        $"[PreparationContinue] BattlePlayerSlot Slot={slot} CardNumber={card.CardNumber} " +
                        $"Source={(scenario == null ? EBattleCardStatSource.RunState : slotData.StatSource)} " +
                        $"Attack={card.Attack} MaxHealth={card.MaxHealth} CurrentHealth={card.CurrentHealth.Value}");
                }
            }

            private static void CreateEnemyCards(
                Entity[] destination,
                ref Unity.Mathematics.Random random,
                BattleScenarioStartupData scenario,
                EnemyLineupCsvData lineup,
                EnemyBattlePreviewStartupData preview)
            {
                if (scenario == null && lineup == null && preview == null)
                    throw new ArgumentException("A default battle requires an enemy lineup or preview snapshot.");
                for (var slot = 0; slot < destination.Length; slot++)
                {
                    var slotData = scenario?.GetEnemySlot(slot) ?? default;
                    if (scenario != null && slotData.IsOccupied == false)
                    {
                        destination[slot] = Entity.Null;
                        DebugApi.Log($"[PreparationContinue] BattleEnemySlot Slot={slot} State=Empty");
                        continue;
                    }

                    var previewCard = preview?.GetCard(slot) ?? default;
                    var cardNumber = scenario == null
                        ? preview == null
                            ? lineup.CardNumbers[slot]
                            : previewCard.CardNumber
                        : slotData.CardNumber;
                    var cardConfig = DataApi.GetData<BattleCardCsvData>(cardNumber);
                    if (cardConfig == null)
                        throw new InvalidOperationException($"Enemy card configuration {cardNumber} is missing.");

                    var entity = EcsApi.CreateEntity(BattleRules.CardEntityGroup);
                    var card = entity.AddRawComponent<BattleCardRawComponent>();
                    if (scenario == null)
                    {
                        card.InitializeFromInstance(
                            EBattleSide.Enemy,
                            slot,
                            preview == null
                                ? EnemyCardFactory.Create(cardNumber, ref random)
                                : previewCard);
                    }
                    else
                    {
                        var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
                        if (typeConfig == null)
                            throw new InvalidOperationException($"Enemy card type {cardConfig.CardTypeId} is missing.");
                        card.InitializeExplicit(
                            EBattleSide.Enemy,
                            slot,
                            cardConfig,
                            typeConfig,
                            slotData.Attack,
                            slotData.MaxHealth,
                            slotData.CurrentHealth);
                    }
                    destination[slot] = entity;
                    DebugApi.Log(
                        $"[PreparationContinue] BattleEnemySlot Slot={slot} CardNumber={card.CardNumber} " +
                        $"Source={(scenario == null ? "ConfigRandom" : "Explicit")} " +
                        $"Attack={card.Attack} MaxHealth={card.MaxHealth} CurrentHealth={card.CurrentHealth.Value}");
                }
            }

            private static int CountOccupied(Entity[] cards)
            {
                var count = 0;
                for (var slot = 0; slot < cards.Length; slot++)
                {
                    if (cards[slot] != Entity.Null)
                        count++;
                }
                return count;
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
