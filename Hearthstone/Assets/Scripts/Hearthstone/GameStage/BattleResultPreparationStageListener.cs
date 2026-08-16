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

    public sealed class BattleBgmStageListener : StageListenerBase
    {
        private const string VictoryBgmKey = "Win";
        private const string DefeatBgmKey = "Failed";
        private const float ResultBgmTransitionDuration = 0.5f;

        private BattleSessionSingletonRawComponent m_Session;

        protected override void InitListener()
        {
            m_Session = EcsApi.GetSingletonRawComponent<BattleSessionSingletonRawComponent>();
            if (m_Session == null)
                throw new InvalidOperationException("Battle BGM listener requires a battle session.");
            AddVariableDirtyListener(m_Session.Result, OnBattleResultChanged);
        }

        private static void OnBattleResultChanged(EBattleResult result)
        {
            string bgmKey;
            switch (result)
            {
                case EBattleResult.PlayerVictory:
                    bgmKey = VictoryBgmKey;
                    break;
                case EBattleResult.EnemyVictory:
                    bgmKey = DefeatBgmKey;
                    break;
                default:
                    return;
            }

            AudioApi.SetBgm(
                bgmKey,
                ResultBgmTransitionDuration,
                loop: false);
        }
    }
}
