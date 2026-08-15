using System;
using BbxCommon;
using BbxCommon.Ui;

namespace Hearthstone
{
    public sealed class PreparationController : UiControllerBase<PreparationView>
    {
        private RunStateSingletonRawComponent m_RunState;
        private PreparationSessionSingletonRawComponent m_Session;
        private ListenableItemListener m_RevisionListener;

        protected override void InitListeners()
        {
            m_RevisionListener = ModelWrapper.CreateVariableDirtyListener<int>(
                EControllerLifeCycle.Open,
                ignored => RefreshAll());
        }

        protected override void OnUiOpen()
        {
            m_RunState = EcsApi.GetSingletonRawComponent<RunStateSingletonRawComponent>();
            m_Session = EcsApi.GetSingletonRawComponent<PreparationSessionSingletonRawComponent>();
            if (m_RunState == null || m_Session == null)
            {
                DebugApi.LogError("Preparation UI opened before runtime state was initialized.");
                return;
            }

            m_RevisionListener.RebindTarget(m_RunState.Revision);
            if (m_View.RewardText != null)
                m_View.RewardText.text = $"本轮获得 {RunCardRules.RewardGrantCount} 张卡";
            PopulateItems();
            RefreshAll();
        }

        protected override void OnUiClose()
        {
            m_RevisionListener.RebindTarget(null);
            m_RunState = null;
            m_Session = null;
        }

        internal void DropCardOnSlot(int cardNumber, int targetSlot)
        {
            if (RunCardRules.TryPlaceCard(m_RunState, cardNumber, targetSlot) == false)
                RefreshAll();
        }

        internal void OnDragReturned()
        {
            if (m_View.CardPoolList != null)
                m_View.CardPoolList.RefreshLayout();
            if (m_View.BattleSlotList != null)
                m_View.BattleSlotList.RefreshLayout();
            RefreshAll();
        }

        private void PopulateItems()
        {
            m_View.CardPoolList.ItemWrapper.ClearItems();
            for (var cardNumber = RunCardRules.FirstCardNumber; cardNumber <= RunCardRules.LastCardNumber; cardNumber++)
            {
                var item = m_View.CardPoolList.ItemWrapper.AddItem<PreparationCardItemController>();
                if (item == null)
                    throw new InvalidOperationException("PreparationCardItemController preload mapping is missing.");
                item.Bind(this, cardNumber);
            }

            m_View.BattleSlotList.ItemWrapper.ClearItems();
            for (var slot = 0; slot < RunCardRules.BattleSlotCount; slot++)
            {
                var item = m_View.BattleSlotList.ItemWrapper.AddItem<PreparationSlotItemController>();
                if (item == null)
                    throw new InvalidOperationException("PreparationSlotItemController preload mapping is missing.");
                item.Bind(this, slot);
            }
        }

        private void RefreshAll()
        {
            if (m_RunState == null)
                return;
            for (var index = 0; index < m_View.CardPoolList.ItemWrapper.Count; index++)
                m_View.CardPoolList.ItemWrapper.GetItem<PreparationCardItemController>(index).Refresh(m_RunState);
            for (var index = 0; index < m_View.BattleSlotList.ItemWrapper.Count; index++)
                m_View.BattleSlotList.ItemWrapper.GetItem<PreparationSlotItemController>(index).Refresh(m_RunState);
        }
    }
}
