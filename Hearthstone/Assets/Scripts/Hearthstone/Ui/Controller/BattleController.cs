using BbxCommon;
using BbxCommon.Ui;

namespace Hearthstone
{
    public sealed class BattleController : UiControllerBase<BattleView>
    {
        private BattleSessionSingletonRawComponent m_Session;
        private ListenableItemListener m_CurrentSideListener;
        private ListenableItemListener m_ResultListener;

        protected override void InitListeners()
        {
            m_CurrentSideListener = ModelWrapper.CreateVariableDirtyListener<EBattleSide>(
                EControllerLifeCycle.Init,
                RefreshTurn);
            m_ResultListener = ModelWrapper.CreateVariableDirtyListener<EBattleResult>(
                EControllerLifeCycle.Init,
                RefreshResult);
        }

        protected override void OnUiOpen()
        {
            m_Session = EcsApi.GetSingletonRawComponent<BattleSessionSingletonRawComponent>();
            if (m_Session == null)
            {
                DebugApi.LogError("Battle UI opened before BattleSession was initialized.");
                return;
            }

            m_CurrentSideListener.RebindTarget(m_Session.CurrentSide);
            m_ResultListener.RebindTarget(m_Session.Result);
            PopulateCards(m_View.EnemyCardList, m_Session.EnemyCards);
            PopulateCards(m_View.PlayerCardList, m_Session.PlayerCards);
            RefreshTurn(m_Session.CurrentSide.Value);
            RefreshResult(m_Session.Result.Value);
        }

        protected override void OnUiClose()
        {
            m_CurrentSideListener.RebindTarget(null);
            m_ResultListener.RebindTarget(null);
            m_Session = null;
        }

        private static void PopulateCards(UiList list, Unity.Entities.Entity[] cards)
        {
            if (list == null)
                return;

            list.ItemWrapper.ClearItems();
            for (var slot = 0; slot < cards.Length; slot++)
            {
                var item = list.ItemWrapper.AddItem<BattleCardItemController>();
                if (item == null)
                {
                    DebugApi.LogError("BattleCardItemController preload mapping is missing.");
                    continue;
                }
                item.Bind(cards[slot]);
            }
        }

        private void RefreshTurn(EBattleSide side)
        {
            if (m_View.TurnText == null)
                return;

            if (m_Session != null && m_Session.Result.Value != EBattleResult.InProgress)
                m_View.TurnText.text = string.Empty;
            else
                m_View.TurnText.text = "战斗进行中";
        }

        private void RefreshResult(EBattleResult result)
        {
            if (m_View.ResultText != null)
            {
                switch (result)
                {
                    case EBattleResult.PlayerVictory:
                        m_View.ResultText.text = "胜利";
                        break;
                    case EBattleResult.EnemyVictory:
                        m_View.ResultText.text = "失败";
                        break;
                    default:
                        m_View.ResultText.text = string.Empty;
                        break;
                }
            }

            if (result != EBattleResult.InProgress && m_View.TurnText != null)
                m_View.TurnText.text = string.Empty;
        }
    }
}
