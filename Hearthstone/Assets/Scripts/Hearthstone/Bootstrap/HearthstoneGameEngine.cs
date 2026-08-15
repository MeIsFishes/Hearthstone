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
                CreateBatchRequestKey(snapshot.PreparationRewardBatch));
            TrySubmitRequestedStageGroup();
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

        protected override void OnStageLoadingCompleted(IReadOnlyList<GameStage> activeStages)
        {
            if (!m_StageGroupCoordinator.IsLoading)
                return;
            if (!ContainsStage(activeStages, m_LoadingStage))
                throw new InvalidOperationException("The requested Hearthstone stage group did not become active.");

            m_StageGroupCoordinator.CompleteTransition(
                m_LoadingGroup,
                m_LoadingRequestKey);
            m_LoadingStage = null;
            m_LoadingGroup = EHearthstoneStageGroup.None;
            m_LoadingRequestKey = null;
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
                    m_LoadingStage = m_BattleStage;
                    StageWrapper.SetActiveGameStage(m_RunStateStage, m_BattleStage);
                    break;
                case EHearthstoneStageGroup.Preparation:
                    if (m_RequestedPreparationBatch == null)
                        throw new InvalidOperationException("Requested Preparation batch is missing.");
                    m_PreparationStage = PreparationStages.CreatePreparationStage(this, m_RequestedPreparationBatch.CreateSnapshot());
                    m_LoadingStage = m_PreparationStage;
                    StageWrapper.SetActiveGameStage(m_RunStateStage, m_PreparationStage);
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
    }
}
