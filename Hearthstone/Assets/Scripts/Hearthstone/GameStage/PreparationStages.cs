using System;
using System.Runtime.CompilerServices;
using System.Text;
using BbxCommon;
using BbxCommon.Ui;
using UnityEngine;

namespace Hearthstone
{
    public static class PreparationStages
    {
        private const string PreparationStartupDataKey = "PreparationStage.Round";
        private const string PreparationUiAssetPath = "Ui/Preparation";

        public static GameStage CreatePreparationStage(
            HearthstoneGameEngine engine,
            PreparationRoundStartupData round)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));
            if (round == null)
                throw new ArgumentNullException(nameof(round));

            var stage = engine.StageWrapper.CreateStage("PreparationStage");
            stage.SetStageData(PreparationStartupDataKey, round.CreateSnapshot());
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
                var round = stage.GetStageData(PreparationStartupDataKey) as PreparationRoundStartupData;
                if (round == null)
                    throw new InvalidOperationException("PreparationStage round startup data is missing or invalid.");
                var batch = round.RewardBatch;

                ValidateGrantReferences(batch);
                var runState = EcsApi.GetSingletonRawComponent<RunStateSingletonRawComponent>();
                if (runState == null)
                    throw new InvalidOperationException("PreparationStage requires an active RunStateStage.");
                if (EcsApi.GetSingletonRawComponent<PreparationSessionSingletonRawComponent>() != null)
                    throw new InvalidOperationException("Preparation session already exists.");
                if (EcsApi.GetSingletonRawComponent<PreparationContinueSingletonRawComponent>() != null)
                    throw new InvalidOperationException("Preparation continue state already exists.");

                runState.SetUnlockedBattleSlotCount(round.UnlockedBattleSlotCount);
                var result = RunCardRules.ApplyRewardBatch(runState, batch);
                var session = EcsApi.AddSingletonRawComponent<PreparationSessionSingletonRawComponent>();
                if (session == null)
                    throw new InvalidOperationException("Unable to create PreparationSessionSingletonRawComponent.");
                session.Initialize(round, result == ERewardBatchApplyResult.Applied);
                if (EcsApi.AddSingletonRawComponent<PreparationContinueSingletonRawComponent>() == null)
                    throw new InvalidOperationException("Unable to create PreparationContinueSingletonRawComponent.");
                DebugApi.Log(
                    $"[PreparationContinue] StageInitialize Stage=PreparationStage " +
                    $"StageId={RuntimeHelpers.GetHashCode(stage)} SessionId={RuntimeHelpers.GetHashCode(session)} " +
                    $"BattleNumber={round.BattleNumber} UnlockedSlots={round.UnlockedBattleSlotCount} " +
                    $"BatchId={session.BatchId} RewardApplyResult={result} " +
                    $"AppliedBatchCount={runState.AppliedRewardBatchPayloadFingerprints.Count} " +
                    $"RewardCards=[{FormatRewardCards(session)}] Owned={runState.GetOwnedCardCount()} " +
                    $"RunRevision={runState.Revision.Value} FusionRevision={session.FusionRevision.Value}");
            }

            public void Unload(GameStage stage)
            {
                EcsApi.RemoveSingletonRawComponent<PreparationContinueSingletonRawComponent>();
                var session = EcsApi.GetSingletonRawComponent<PreparationSessionSingletonRawComponent>();
                var runState = EcsApi.GetSingletonRawComponent<RunStateSingletonRawComponent>();
                if (session != null)
                {
                    var batchId = session.BatchId;
                    var sessionId = RuntimeHelpers.GetHashCode(session);
                    var selectedMaterials = (int[])session.FusionSlotCardNumbers.Clone();
                    var runRevisionBefore = runState == null ? -1 : runState.Revision.Value;
                    var materialRunBefore = FormatMaterialRunState(runState, selectedMaterials);
                    DebugApi.Log(
                        $"[PreparationFusion] StageUnloadBegin Stage=PreparationStage SessionId={sessionId} BatchId={batchId} " +
                        $"FusionSlots=[{string.Join(",", session.FusionSlotCardNumbers)}] " +
                        $"MaterialRunState=[{materialRunBefore}] RunRevision={runRevisionBefore} " +
                        $"Owned={(runState == null ? -1 : runState.GetOwnedCardCount())} " +
                        $"BattleSlots=[{(runState == null ? string.Empty : string.Join(",", runState.BattleSlotCardNumbers))}]");
                    EcsApi.RemoveSingletonRawComponent<PreparationSessionSingletonRawComponent>();
                    var remainingSession = EcsApi.GetSingletonRawComponent<PreparationSessionSingletonRawComponent>();
                    DebugApi.Log(
                        $"[PreparationFusion] StageUnloadComplete Stage=PreparationStage SessionId={sessionId} BatchId={batchId} " +
                        $"SessionExists={remainingSession != null} FusionSlots=[] " +
                        $"MaterialRunBefore=[{materialRunBefore}] " +
                        $"MaterialRunAfter=[{FormatMaterialRunState(runState, selectedMaterials)}] " +
                        $"RunRevisionBefore={runRevisionBefore} RunRevisionAfter={(runState == null ? -1 : runState.Revision.Value)} " +
                        $"Owned={(runState == null ? -1 : runState.GetOwnedCardCount())} " +
                        $"BattleSlots=[{(runState == null ? string.Empty : string.Join(",", runState.BattleSlotCardNumbers))}]");
                    return;
                }
                EcsApi.RemoveSingletonRawComponent<PreparationSessionSingletonRawComponent>();
            }

            private static string FormatRewardCards(PreparationSessionSingletonRawComponent session)
            {
                var builder = new StringBuilder();
                for (var index = 0; index < session.RewardCards.Length; index++)
                {
                    if (index > 0)
                        builder.Append(';');
                    var card = session.RewardCards[index];
                    builder.Append($"{card.CardNumber}:{card.Attack}/{card.MaxHealth}");
                }
                return builder.ToString();
            }

            private static string FormatMaterialRunState(
                RunStateSingletonRawComponent runState,
                int[] materialCardNumbers)
            {
                if (runState == null)
                    return "RunStateMissing";
                var builder = new StringBuilder();
                for (var index = 0; index < materialCardNumbers.Length; index++)
                {
                    var cardNumber = materialCardNumbers[index];
                    if (cardNumber == 0)
                        continue;
                    if (builder.Length > 0)
                        builder.Append(';');
                    var owned = runState.HasCard(cardNumber);
                    var card = owned ? runState.CardInstances[cardNumber] : default;
                    builder.Append($"{cardNumber}:Owned={owned},Attack={card.Attack},MaxHealth={card.MaxHealth}");
                }
                return builder.Length == 0 ? "None" : builder.ToString();
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
                }
            }

        }
    }
}
