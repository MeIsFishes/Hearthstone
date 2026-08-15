using BbxCommon;
using BbxCommon.Ui;
using UnityEngine.EventSystems;

namespace Hearthstone
{
    public sealed class PreparationFusionSlotItemController : UiControllerBase<PreparationFusionSlotItemView>
    {
        private const string CardFrameKey = "CardFrame-v3";
        private PreparationController m_Page;
        private int m_Slot;
        private int m_CardNumber;

        protected override void OnUiInit()
        {
            m_View.Interactor.Wrapper.OnInteractorTouch += OnTouch;
            m_View.Interactor.Wrapper.OnInteractorTouchEnd += ignored => m_View.DropHighlight.gameObject.SetActive(false);
            m_View.Interactor.Wrapper.OnInteract += OnInteract;
            m_View.Dragable.Wrapper.OnBackFromTop += OnBackFromTop;
        }

        public void Bind(PreparationController page, int slot)
        {
            m_Page = page;
            m_Slot = slot;
            m_View.Interactor.Wrapper.ExtraInfo = new PreparationInteractorData
            {
                Source = EPreparationCardSource.FusionSlot,
                SourceSlot = slot,
                TargetSlot = slot,
            };
        }

        public void Refresh(RunStateSingletonRawComponent runState, PreparationSessionSingletonRawComponent session)
        {
            m_CardNumber = session.FusionSlotCardNumbers[m_Slot];
            var occupied = m_CardNumber != 0 && runState.HasCard(m_CardNumber);
            m_View.EmptyState.SetActive(!occupied);
            m_View.OccupiedState.SetActive(occupied);
            m_View.DropHighlight.gameObject.SetActive(false);
            var data = (PreparationInteractorData)m_View.Interactor.Wrapper.ExtraInfo;
            data.CardNumber = occupied ? m_CardNumber : 0;
            SetDraggingEnabled(occupied);
            if (!occupied)
            {
                m_View.CardNumberText.text = string.Empty;
                m_View.NameText.text = string.Empty;
                if (m_View.KeywordText != null)
                    m_View.KeywordText.text = string.Empty;
                m_View.AttackText.text = string.Empty;
                m_View.HealthText.text = string.Empty;
                return;
            }
            var instance = runState.CardInstances[m_CardNumber];
            var card = DataApi.GetData<BattleCardCsvData>(m_CardNumber);
            var type = card == null ? null : DataApi.GetData<BattleCardTypeCsvData>(card.CardTypeId);
            m_View.CardFrame.sprite = ResourceApi.LoadSprite(CardFrameKey);
            m_View.ArtworkArea.sprite = card == null ? null : ResourceApi.LoadSprite(card.ArtworkKey);
            m_View.CardNumberText.text = m_CardNumber.ToString("00");
            m_View.NameText.text = type == null ? string.Empty : type.DisplayName;
            if (m_View.KeywordText != null)
                m_View.KeywordText.text = BattleKeywordRules.FormatDisplayText(instance.Keywords);
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

        private void OnTouch(Interactor requester)
        {
            if (TryGetSource(requester, out var source) && source.CardNumber != 0)
                m_View.DropHighlight.gameObject.SetActive(true);
        }

        private void OnInteract(Interactor requester, Interactor responder)
        {
            if (!ReferenceEquals(responder, m_View.Interactor) || !TryGetSource(requester, out var source))
                return;
            m_View.DropHighlight.gameObject.SetActive(false);
            var sourceSlot = source.Source == EPreparationCardSource.FusionSlot ? source.SourceSlot : -1;
            m_Page?.DropCardOnFusionSlot(source.CardNumber, m_Slot, sourceSlot);
        }

        private static bool TryGetSource(Interactor requester, out PreparationInteractorData source)
        {
            source = null;
            if (!(requester is UiInteractor uiInteractor))
                return false;
            source = uiInteractor.Wrapper.ExtraInfo as PreparationInteractorData;
            return source != null && source.CardNumber != 0 && source.Source != EPreparationCardSource.BattleSlot;
        }

        private void OnBackFromTop(PointerEventData ignored) => m_Page?.OnDragReturned();
    }
}
