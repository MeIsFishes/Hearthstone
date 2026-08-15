using System;
using BbxCommon;

namespace Hearthstone
{
    public sealed class BattleResultPreparationStageListener : StageListenerBase
    {
        private BattleSessionSingletonRawComponent m_Session;

        protected override void InitListener()
        {
            m_Session = EcsApi.GetSingletonRawComponent<BattleSessionSingletonRawComponent>();
            if (m_Session == null)
                throw new InvalidOperationException("Battle result listener requires a battle session.");
            AddVariableDirtyListener(m_Session.Result, OnBattleResultChanged);
        }

        private void OnBattleResultChanged(EBattleResult result)
        {
            if (result == EBattleResult.InProgress || m_Session.PreparationTransitionRequested)
                return;
            if (m_Session.PendingPreparationRewardBatch == null)
                throw new InvalidOperationException("Battle session has no pending preparation reward batch.");

            m_Session.PreparationTransitionRequested = true;
            HearthstoneGameEngine.Instance.EnterPreparationStageGroup(
                m_Session.PendingPreparationRewardBatch.CreateSnapshot());
        }
    }
}
