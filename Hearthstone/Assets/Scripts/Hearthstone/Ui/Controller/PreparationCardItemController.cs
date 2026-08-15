using BbxCommon;
using BbxCommon.Ui;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hearthstone
{
    internal sealed class PreparationInteractorData
    {
        public int CardNumber;
        public EPreparationCardSource Source;
        public int SourceSlot;
        public int TargetSlot;
    }

    internal enum EPreparationCardSource
    {
        CardPool,
        BattleSlot,
        FusionSlot,
    }

    public sealed class PreparationCardItemController : UiControllerBase<PreparationCardItemView>
    {
        private const string CardFrameKey = "CardFrame-v3";
        private PreparationController m_Page;
        private int m_CardNumber;

        protected override void OnUiInit()
        {
            m_View.Dragable.Wrapper.OnBackFromTop += OnBackFromTop;
            m_View.Interactor.Wrapper.OnInteract += OnInteract;
            if (m_View.EmptyAttemptListener != null)
                m_View.EmptyAttemptListener.AddCallback(EUiEvent.PointerClick, OnEmptyAttempt);
        }

        public void Bind(PreparationController page, int cardNumber)
        {
            m_Page = page;
            m_CardNumber = cardNumber;
            m_View.Interactor.Wrapper.ExtraInfo = new PreparationInteractorData
            {
                CardNumber = cardNumber,
                Source = EPreparationCardSource.CardPool,
                SourceSlot = -1,
                TargetSlot = -1,
            };
        }

        public void Refresh(
            RunStateSingletonRawComponent runState,
            PreparationSessionSingletonRawComponent session,
            bool fusionTabSelected)
        {
            var owned = runState.HasCard(m_CardNumber);
            m_View.EmptyState.SetActive(owned == false);
            m_View.OwnedState.SetActive(owned);
            m_View.CardNumberText.text = m_CardNumber.ToString("00");
            var selected = false;
            if (session != null)
            {
                for (var slot = 0; slot < session.FusionSlotCardNumbers.Length; slot++)
                {
                    if (session.FusionSlotCardNumbers[slot] == m_CardNumber)
                    {
                        selected = true;
                        break;
                    }
                }
            }
            if (m_View.MaterialSelectedState != null)
                m_View.MaterialSelectedState.SetActive(fusionTabSelected && selected);
            SetDraggingEnabled(owned);
            if (owned == false)
            {
                if (m_View.KeywordText != null)
                    m_View.KeywordText.text = string.Empty;
                return;
            }

            var instance = runState.CardInstances[m_CardNumber];
            var card = DataApi.GetData<BattleCardCsvData>(m_CardNumber);
            var type = card == null ? null : DataApi.GetData<BattleCardTypeCsvData>(card.CardTypeId);
            m_View.CardFrame.sprite = ResourceApi.LoadSprite(CardFrameKey);
            m_View.ArtworkArea.sprite = card == null ? null : ResourceApi.LoadSprite(card.ArtworkKey);
            m_View.NameText.text = type == null ? string.Empty : type.DisplayName;
            if (m_View.KeywordText != null)
                m_View.KeywordText.text = BattleKeywordRules.FormatDisplayText(instance.Keywords);
            m_View.AttackText.text = instance.Attack.ToString();
            m_View.HealthText.text = instance.MaxHealth.ToString();
        }

        protected override void OnUiClose()
        {
            m_Page = null;
            m_CardNumber = 0;
        }

        private void SetDraggingEnabled(bool enabled)
        {
            m_View.Dragable.enabled = enabled;
            if (m_View.Dragable.EventListener != null)
                m_View.Dragable.EventListener.enabled = enabled;
        }

        private void OnBackFromTop(PointerEventData ignored)
        {
            m_Page?.OnDragReturned();
        }

        private void OnEmptyAttempt(PointerEventData ignored)
        {
            m_Page?.ReportUnownedCardAttempt(m_CardNumber);
        }

        private void OnInteract(Interactor requester, Interactor responder)
        {
            if (!ReferenceEquals(responder, m_View.Interactor) ||
                !(requester is UiInteractor uiInteractor) ||
                !(uiInteractor.Wrapper.ExtraInfo is PreparationInteractorData source) ||
                source.Source != EPreparationCardSource.FusionSlot)
                return;
            m_Page?.RemoveFusionMaterial(source.CardNumber, source.SourceSlot);
        }
    }
}
