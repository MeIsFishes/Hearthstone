using System;
using BbxCommon.Ui;

namespace Hearthstone
{
    public sealed class FusionRecommendationItemController :
        UiControllerBase<FusionRecommendationItemView>
    {
        private readonly BattleCardItemController[] m_Cards =
            new BattleCardItemController[RunCardRules.FusionSlotCount];
        private PreparationController m_PreparationPage;
        private FusionRecommendationData m_Recommendation;

        protected override void OnUiInit()
        {
            m_View.SelectButton.onClick.AddListener(OnSelectClicked);
        }

        protected override void OnUiOpen()
        {
            m_View.CardList.ItemWrapper.ClearItems();
            for (var index = 0; index < m_Cards.Length; index++)
            {
                m_Cards[index] = m_View.CardList.ItemWrapper.AddItem<BattleCardItemController>();
                if (m_Cards[index] == null)
                    throw new InvalidOperationException(
                        "BattleCardItemController preload mapping is missing for fusion recommendations.");
            }
        }

        protected override void OnUiClose()
        {
            m_PreparationPage = null;
            m_Recommendation = default;
            Array.Clear(m_Cards, 0, m_Cards.Length);
        }

        internal void Bind(
            PreparationController page,
            RunStateSingletonRawComponent runState,
            PreparationSessionSingletonRawComponent session,
            FusionRecommendationData recommendation)
        {
            m_PreparationPage = page;
            m_Recommendation = recommendation;
            for (var index = 0; index < m_Cards.Length; index++)
            {
                var card = m_Cards[index];
                if (index >= recommendation.MaterialCount)
                {
                    card.Hide();
                    continue;
                }

                var cardNumber = recommendation.GetCardNumber(index);
                card.Show();
                card.BindFusionRecommendation(
                    page,
                    runState,
                    cardNumber,
                    IsSelectedMaterial(session, cardNumber));
            }
            m_View.SelectButton.interactable = page != null;
        }

        private void OnSelectClicked()
        {
            m_PreparationPage?.ApplyFusionRecommendation(m_Recommendation);
        }

        private static bool IsSelectedMaterial(
            PreparationSessionSingletonRawComponent session,
            int cardNumber)
        {
            if (session == null)
                return false;
            for (var slot = 0; slot < session.FusionSlotCardNumbers.Length; slot++)
            {
                if (session.FusionSlotCardNumbers[slot] == cardNumber)
                    return true;
            }
            return false;
        }
    }
}
