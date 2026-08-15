using BbxCommon;
using BbxCommon.Ui;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hearthstone
{
    public sealed class PreparationSlotItemController : UiControllerBase<PreparationSlotItemView>
    {
        private const string CardFrameKey = "CardFrame-v3";
        private PreparationController m_Page;
        private int m_Slot;
        private int m_CardNumber;

        protected override void OnUiInit()
        {
            m_View.Interactor.Wrapper.OnInteractorTouch += OnInteractorTouch;
            m_View.Interactor.Wrapper.OnInteractorTouchEnd += OnInteractorTouchEnd;
            m_View.Interactor.Wrapper.OnInteract += OnInteract;
            m_View.Dragable.Wrapper.OnBackFromTop += OnBackFromTop;
        }

        public void Bind(PreparationController page, int slot)
        {
            m_Page = page;
            m_Slot = slot;
            m_View.Interactor.Wrapper.ExtraInfo = new PreparationInteractorData
            {
                CardNumber = 0,
                SourceSlot = slot,
                TargetSlot = slot,
            };
        }

        public void Refresh(RunStateSingletonRawComponent runState)
        {
            m_CardNumber = runState.BattleSlotCardNumbers[m_Slot];
            var occupied = m_CardNumber != 0 && runState.HasCard(m_CardNumber);
            m_View.EmptyState.SetActive(occupied == false);
            m_View.OccupiedState.SetActive(occupied);
            m_View.DropHighlight.gameObject.SetActive(false);
            var data = (PreparationInteractorData)m_View.Interactor.Wrapper.ExtraInfo;
            data.CardNumber = occupied ? m_CardNumber : 0;
            SetDraggingEnabled(occupied);
            if (occupied == false)
                return;

            var instance = runState.CardInstances[m_CardNumber];
            var card = DataApi.GetData<BattleCardCsvData>(m_CardNumber);
            var type = card == null ? null : DataApi.GetData<BattleCardTypeCsvData>(card.CardTypeId);
            m_View.CardFrame.sprite = ResourceApi.LoadSprite(CardFrameKey);
            m_View.ArtworkArea.sprite = card == null ? null : ResourceApi.LoadSprite(card.ArtworkKey);
            m_View.NameText.text = type == null ? string.Empty : type.DisplayName;
            m_View.AttackText.text = instance.Attack.ToString();
            m_View.HealthText.text = instance.MaxHealth.ToString();
        }

        protected override void OnUiClose()
        {
            m_Page = null;
            m_Slot = -1;
            m_CardNumber = 0;
        }

        private void SetDraggingEnabled(bool enabled)
        {
            m_View.Dragable.enabled = enabled;
            if (m_View.Dragable.EventListener != null)
                m_View.Dragable.EventListener.enabled = enabled;
        }

        private void OnInteractorTouch(Interactor requester)
        {
            if (TryGetSource(requester, out var source) && source.CardNumber != 0)
                m_View.DropHighlight.gameObject.SetActive(true);
        }

        private void OnInteractorTouchEnd(Interactor ignored)
        {
            m_View.DropHighlight.gameObject.SetActive(false);
        }

        private void OnInteract(Interactor requester, Interactor responder)
        {
            if (ReferenceEquals(responder, m_View.Interactor) == false)
                return;
            m_View.DropHighlight.gameObject.SetActive(false);
            if (TryGetSource(requester, out var source))
                m_Page?.DropCardOnSlot(source.CardNumber, m_Slot);
        }

        private static bool TryGetSource(Interactor requester, out PreparationInteractorData source)
        {
            source = null;
            if (!(requester is UiInteractor uiInteractor))
                return false;
            source = uiInteractor.Wrapper.ExtraInfo as PreparationInteractorData;
            return source != null && source.CardNumber != 0;
        }

        private void OnBackFromTop(PointerEventData ignored)
        {
            m_Page?.OnDragReturned();
        }
    }
}
