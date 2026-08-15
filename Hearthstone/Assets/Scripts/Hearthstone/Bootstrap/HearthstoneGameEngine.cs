using System;
using System.Collections.Generic;
using BbxCommon;

namespace Hearthstone
{
    public enum EHearthstoneStageGroup
    {
        None,
        Battle,
        Preparation,
    }

    public enum EStageGroupTransitionPhase
    {
        None,
        Requested,
        Loading,
        Active,
    }

    /// <summary>
    /// Serializes stage-group requests across the framework's asynchronous stage loading batches.
    /// </summary>
    public sealed class HearthstoneStageGroupTransitionCoordinator
    {
        public EHearthstoneStageGroup RequestedGroup { get; private set; }
        public string RequestedKey { get; private set; }
        public EHearthstoneStageGroup ActiveGroup { get; private set; }
        public string ActiveKey { get; private set; }
        public EStageGroupTransitionPhase Phase { get; private set; }
        public bool IsLoading => Phase == EStageGroupTransitionPhase.Loading;

        private EHearthstoneStageGroup m_LoadingGroup;
        private string m_LoadingKey;

        public bool Request(EHearthstoneStageGroup group, string key)
        {
            if (group == EHearthstoneStageGroup.None)
                throw new ArgumentOutOfRangeException(nameof(group));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Stage-group request key cannot be empty.", nameof(key));
            if (RequestedGroup == group && string.Equals(RequestedKey, key, StringComparison.Ordinal))
                return false;

            RequestedGroup = group;
            RequestedKey = key;
            if (!IsLoading)
                Phase = MatchesActive(group, key) ? EStageGroupTransitionPhase.Active : EStageGroupTransitionPhase.Requested;
            return true;
        }

        public bool TryBeginTransition(out EHearthstoneStageGroup group, out string key)
        {
            group = RequestedGroup;
            key = RequestedKey;
            if (IsLoading || group == EHearthstoneStageGroup.None || MatchesActive(group, key))
            {
                if (!IsLoading && group != EHearthstoneStageGroup.None)
                    Phase = EStageGroupTransitionPhase.Active;
                return false;
            }

            m_LoadingGroup = group;
            m_LoadingKey = key;
            Phase = EStageGroupTransitionPhase.Loading;
            return true;
        }

        public void CompleteTransition(EHearthstoneStageGroup group, string key)
        {
            if (!IsLoading || m_LoadingGroup != group ||
                !string.Equals(m_LoadingKey, key, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Completed stage group does not match the loading request.");
            }

            ActiveGroup = group;
            ActiveKey = key;
            m_LoadingGroup = EHearthstoneStageGroup.None;
            m_LoadingKey = null;
            Phase = MatchesActive(RequestedGroup, RequestedKey)
                ? EStageGroupTransitionPhase.Active
                : EStageGroupTransitionPhase.Requested;
        }

        public void FailTransition(EHearthstoneStageGroup group, string key)
        {
            if (!IsLoading || m_LoadingGroup != group ||
                !string.Equals(m_LoadingKey, key, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Failed stage group does not match the loading request.");
            }

            m_LoadingGroup = EHearthstoneStageGroup.None;
            m_LoadingKey = null;
            RequestedGroup = ActiveGroup;
            RequestedKey = ActiveKey;
            Phase = ActiveGroup == EHearthstoneStageGroup.None
                ? EStageGroupTransitionPhase.None
                : EStageGroupTransitionPhase.Active;
        }

        private bool MatchesActive(EHearthstoneStageGroup group, string key)
        {
            return ActiveGroup == group && string.Equals(ActiveKey, key, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// 项目运行入口。当前默认进入核心自动战斗模式。
    /// </summary>
    public sealed class HearthstoneGameEngine : GameEngineBase<HearthstoneGameEngine>
    {
        private GameStage m_RunStateStage;
        private GameStage m_BattleStage;
        private GameStage m_PreparationStage;
        private GameStage m_LoadingStage;
        private BattleStageStartupData m_RequestedBattleStartupData;
        private PreparationRewardBatchStartupData m_RequestedPreparationBatch;
        private readonly HearthstoneStageGroupTransitionCoordinator m_StageGroupCoordinator = new();
        private EHearthstoneStageGroup m_LoadingGroup;
        private string m_LoadingRequestKey;
        private long m_LoadingFrameworkAttemptId;
        private BattleStageStartupData m_LoadingBattleStartupData;
        private PreparationContinueTransactionSnapshot m_ContinueSnapshot;
        private long m_NextContinueAttemptId;

        public long CurrentContinueAttemptId => m_ContinueSnapshot?.AttemptId ?? 0;

        protected override void OnAwake()
        {
            RegisterSystemOrder(
                typeof(InputSystem),
                typeof(BattleSystem),
                typeof(TaskSystem));

            m_RunStateStage = RunStateStages.CreateRunStateStage(this);
            EnterBattleStageGroup(BattleStageStartupData.CreateDefault());
        }

        /// <summary>
        /// 进入完整的核心战斗 StageGroup。
        /// </summary>
        public void EnterBattleStageGroup(BattleStageStartupData startupData)
        {
            if (startupData == null)
                throw new ArgumentNullException(nameof(startupData));
            var snapshot = startupData.CreateSnapshot();
            m_RequestedBattleStartupData = snapshot;
            m_StageGroupCoordinator.Request(
                EHearthstoneStageGroup.Battle,
                CreateBattleRequestKey(snapshot));
            TrySubmitRequestedStageGroup();
        }

        public EPreparationContinueResult TryEnterNextBattleStageGroup()
        {
            var continueState = EcsApi.GetSingletonRawComponent<PreparationContinueSingletonRawComponent>();
            if (m_StageGroupCoordinator.ActiveGroup != EHearthstoneStageGroup.Preparation)
                return LogContinueRejected(EPreparationContinueResult.InvalidStage, "ActiveGroupIsNotPreparation", continueState);
            if (continueState == null)
                return LogContinueRejected(EPreparationContinueResult.InvalidRuntimeState, "ContinueStateMissing", null);
            if (continueState.State.Value != EPreparationContinueState.Idle || m_StageGroupCoordinator.IsLoading)
                return LogContinueRejected(EPreparationContinueResult.DuplicateIgnored, "AttemptAlreadyWaiting", continueState);

            var runState = EcsApi.GetSingletonRawComponent<RunStateSingletonRawComponent>();
            var session = EcsApi.GetSingletonRawComponent<PreparationSessionSingletonRawComponent>();
            var progression = EcsApi.GetSingletonRawComponent<RunProgressionSingletonRawComponent>();
            if (runState == null || session == null || progression == null || progression.CurrentBattleNumber <= 0)
                return LogContinueRejected(EPreparationContinueResult.InvalidRuntimeState, "RequiredRuntimeStateMissing", continueState);

            var targetBattleNumber = checked(progression.CurrentBattleNumber + 1);
            PreparationRewardBatchStartupData rewardBatch;
            try
            {
                var progressionConfig = DataApi.GetData<BattleProgressionCsvData>(targetBattleNumber)
                    ?? throw new InvalidOperationException($"Battle progression {targetBattleNumber} is missing.");
                rewardBatch = progressionConfig.CreateRewardBatchSnapshot();
                for (var index = 0; index < rewardBatch.Grants.Count; index++)
                {
                    if (runState.HasCard(rewardBatch.Grants[index].CardNumber))
                    {
                        throw new InvalidOperationException(
                            $"Battle progression {targetBattleNumber} reward card {rewardBatch.Grants[index].CardNumber} is already owned.");
                    }
                }
            }
            catch (Exception exception)
            {
                DebugApi.LogError(
                    $"[PreparationContinue] Result={EPreparationContinueResult.InvalidProgressionConfig} " +
                    $"TargetBattleNumber={targetBattleNumber} Reason={exception.Message}");
                continueState.State.SetValue(EPreparationContinueState.Idle);
                return EPreparationContinueResult.InvalidProgressionConfig;
            }

            var attemptId = checked(++m_NextContinueAttemptId);
            var playerLineup = BattlePlayerLineupStartupData.Capture(runState, runState.BattleSlotCardNumbers);
            m_ContinueSnapshot = new PreparationContinueTransactionSnapshot(
                attemptId,
                progression.CurrentBattleNumber,
                targetBattleNumber,
                playerLineup,
                session.FusionSlotCardNumbers,
                runState.GetOwnedCardCount(),
                runState.Revision.Value,
                session.FusionRevision.Value,
                runState.AppliedRewardBatchPayloadFingerprints.Count,
                progression.BattleStageCreationCount,
                rewardBatch);
            continueState.State.SetValue(EPreparationContinueState.Waiting);

            DebugApi.Log(
                $"[PreparationContinue] Action=Request Result={EPreparationContinueResult.Accepted} AttemptId={attemptId} " +
                $"FromBattleNumber={progression.CurrentBattleNumber} TargetBattleNumber={targetBattleNumber} " +
                $"BatchId={rewardBatch.BatchId} BattleSlots=[{string.Join(",", runState.BattleSlotCardNumbers)}] " +
                $"FusionSlots=[{string.Join(",", session.FusionSlotCardNumbers)}] Owned={runState.GetOwnedCardCount()} " +
                $"RunRevision={runState.Revision.Value} FusionRevision={session.FusionRevision.Value} " +
                $"AppliedBatchCount={runState.AppliedRewardBatchPayloadFingerprints.Count} " +
                $"BattleStageCreationCount={progression.BattleStageCreationCount}");

            EnterBattleStageGroup(new BattleStageStartupData(
                targetBattleNumber,
                rewardBatch,
                scenario: null,
                continuePlayerLineup: m_ContinueSnapshot.PlayerLineup));
            return EPreparationContinueResult.Accepted;
        }

        public void EnterPreparationStageGroup(PreparationRewardBatchStartupData rewardBatch)
        {
            if (rewardBatch == null)
                throw new ArgumentNullException(nameof(rewardBatch));
            var snapshot = rewardBatch.CreateSnapshot();
            m_RequestedPreparationBatch = snapshot;
            m_StageGroupCoordinator.Request(
                EHearthstoneStageGroup.Preparation,
                CreateBatchRequestKey(snapshot));
            TrySubmitRequestedStageGroup();
        }

        protected override void OnStageTransitionCompleted(
            GameStageTransitionResult result,
            IReadOnlyList<GameStage> activeStages)
        {
            if (!m_StageGroupCoordinator.IsLoading)
                return;
            if (result.AttemptId != m_LoadingFrameworkAttemptId)
                return;

            if (result.IsCommitted)
            {
                if (!ContainsStage(activeStages, m_LoadingStage))
                    throw new InvalidOperationException("The requested Hearthstone stage group did not become active.");
                m_StageGroupCoordinator.CompleteTransition(m_LoadingGroup, m_LoadingRequestKey);
                CommitProgressionAfterTransition(result);
            }
            else
            {
                m_StageGroupCoordinator.FailTransition(m_LoadingGroup, m_LoadingRequestKey);
                var continueState = EcsApi.GetSingletonRawComponent<PreparationContinueSingletonRawComponent>();
                continueState?.State.SetValue(EPreparationContinueState.Idle);
                DebugApi.LogError(
                    $"[PreparationContinue] Action=RolledBack Result={EPreparationContinueResult.TargetLoadFailed} " +
                    $"AttemptId={CurrentContinueAttemptId} FrameworkAttemptId={result.AttemptId} " +
                    $"FailurePhase={result.FailurePhase} Failure={result.Failure?.Message} " +
                    $"RollbackErrors={result.RollbackErrors.Count}");
            }
            m_LoadingStage = null;
            m_LoadingBattleStartupData = null;
            m_LoadingGroup = EHearthstoneStageGroup.None;
            m_LoadingRequestKey = null;
            m_LoadingFrameworkAttemptId = 0;
            TrySubmitRequestedStageGroup();
        }

        private void TrySubmitRequestedStageGroup()
        {
            if (!m_StageGroupCoordinator.TryBeginTransition(out var group, out var key))
                return;

            m_LoadingGroup = group;
            m_LoadingRequestKey = key;
            switch (group)
            {
                case EHearthstoneStageGroup.Battle:
                    if (m_RequestedBattleStartupData == null)
                        throw new InvalidOperationException("Requested Battle startup data is missing.");
                    m_BattleStage = BattleStages.CreateBattleStage(this, m_RequestedBattleStartupData.CreateSnapshot());
                    m_LoadingBattleStartupData = m_RequestedBattleStartupData.CreateSnapshot();
                    m_LoadingStage = m_BattleStage;
                    m_LoadingFrameworkAttemptId = StageWrapper.SetActiveGameStage(m_RunStateStage, m_BattleStage);
                    break;
                case EHearthstoneStageGroup.Preparation:
                    if (m_RequestedPreparationBatch == null)
                        throw new InvalidOperationException("Requested Preparation batch is missing.");
                    m_PreparationStage = PreparationStages.CreatePreparationStage(this, m_RequestedPreparationBatch.CreateSnapshot());
                    m_LoadingStage = m_PreparationStage;
                    m_LoadingFrameworkAttemptId = StageWrapper.SetActiveGameStage(m_RunStateStage, m_PreparationStage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(group));
            }
        }

        private static bool ContainsStage(IReadOnlyList<GameStage> stages, GameStage target)
        {
            for (var index = 0; index < stages.Count; index++)
            {
                if (ReferenceEquals(stages[index], target))
                    return true;
            }
            return false;
        }

        private static string CreateBatchRequestKey(PreparationRewardBatchStartupData batch)
        {
            var key = batch.BatchId;
            for (var index = 0; index < batch.Grants.Count; index++)
            {
                var grant = batch.Grants[index];
                key += $"|{grant.CardNumber}:{grant.Attack}:{grant.MaxHealth}";
            }
            return key;
        }

        private static string CreateBattleRequestKey(BattleStageStartupData startupData)
        {
            var key = $"Battle={startupData.BattleNumber}|{CreateBatchRequestKey(startupData.PreparationRewardBatch)}";
            var scenario = startupData.Scenario;
            if (scenario == null)
            {
                key += "|Scenario=Default";
                var lineup = startupData.ContinuePlayerLineup;
                if (lineup == null)
                    return key + "|Lineup=RunState";
                for (var slot = 0; slot < lineup.SlotCount; slot++)
                {
                    var card = lineup.GetSlot(slot);
                    key += card.IsValid
                        ? $"|P{slot}={card.CardNumber}:{card.Attack}:{card.MaxHealth}:{(int)card.Keywords}"
                        : $"|P{slot}=Empty";
                }
                return key;
            }
            key += $"|Seed={scenario.RandomSeed}";
            for (var slot = 0; slot < scenario.SlotCount; slot++)
                key += "|P" + slot + "=" + FormatScenarioSlot(scenario.GetPlayerSlot(slot));
            for (var slot = 0; slot < scenario.SlotCount; slot++)
                key += "|E" + slot + "=" + FormatScenarioSlot(scenario.GetEnemySlot(slot));
            return key;
        }

        private static string FormatScenarioSlot(BattleCardSlotStartupData slot)
        {
            return slot.IsOccupied
                ? $"{slot.CardNumber}:{slot.StatSource}:{slot.Attack}:{slot.MaxHealth}:{slot.CurrentHealth}"
                : "Empty";
        }

        private EPreparationContinueResult LogContinueRejected(
            EPreparationContinueResult result,
            string reason,
            PreparationContinueSingletonRawComponent continueState)
        {
            DebugApi.Log(
                $"[PreparationContinue] Action=RequestRejected Result={result} AttemptId={CurrentContinueAttemptId} " +
                $"Reason={reason} ActiveGroup={m_StageGroupCoordinator.ActiveGroup} " +
                $"Phase={m_StageGroupCoordinator.Phase} ButtonState={continueState?.State.Value}");
            return result;
        }

        private void CommitProgressionAfterTransition(GameStageTransitionResult result)
        {
            var progression = EcsApi.GetSingletonRawComponent<RunProgressionSingletonRawComponent>();
            if (progression == null)
                throw new InvalidOperationException("Committed Hearthstone stage group has no progression state.");

            var battleNumber = m_LoadingGroup == EHearthstoneStageGroup.Battle
                ? m_LoadingBattleStartupData?.BattleNumber ?? 0
                : progression.CurrentBattleNumber == 0 ? 1 : progression.CurrentBattleNumber;
            if (progression.CurrentBattleNumber != battleNumber)
                progression.CommitBattle(battleNumber);

            var resultValue = result.Status == EGameStageTransitionStatus.CommittedWithCleanupErrors
                ? EPreparationContinueResult.CommittedWithCleanupErrors
                : EPreparationContinueResult.Committed;
            DebugApi.Log(
                $"[PreparationContinue] Action=TransitionSettled Result={resultValue} AttemptId={CurrentContinueAttemptId} " +
                $"FrameworkAttemptId={result.AttemptId} Group={m_LoadingGroup} BattleNumber={progression.CurrentBattleNumber} " +
                $"BattleStageCreationCount={progression.BattleStageCreationCount} ProgressionRevision={progression.Revision} " +
                $"CleanupErrors={result.CleanupErrors.Count}");
        }
    }
}
