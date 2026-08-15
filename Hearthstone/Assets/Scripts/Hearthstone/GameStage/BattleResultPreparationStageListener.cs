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
            AddVariableDirtyListener(m_Session.OutcomePresentationCompleted, OnOutcomePresentationCompleted);
        }

        private void OnOutcomePresentationCompleted(bool completed)
        {
            if (completed == false ||
                m_Session.Result.Value != EBattleResult.PlayerVictory ||
                m_Session.IsFinalBattle ||
                m_Session.PreparationTransitionRequested)
                return;

            m_Session.PreparationTransitionRequested = true;
            HearthstoneGameEngine.Instance.BeginPreparationForBattle(m_Session.BattleNumber + 1);
        }
    }
}
