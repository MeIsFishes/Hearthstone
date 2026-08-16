using BbxCommon;
using BbxCommon.Ui;
using UnityEngine;

namespace Hearthstone
{
    public sealed class BattleController : UiControllerBase<BattleView>
    {
        private BattleSessionSingletonRawComponent m_Session;
        private ListenableItemListener m_ResultListener;
        private float m_VictoryBannerElapsed;
        private bool m_VictoryBannerActive;
        private float m_PopupElapsed;
        private bool m_PopupAnimating;

        protected override void OnUiInit()
        {
            if (m_View.ReturnToMainMenuButton != null)
                m_View.ReturnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuClicked);
        }

        protected override void InitListeners()
        {
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

            m_ResultListener.RebindTarget(m_Session.Result);
            PopulateCards(m_View.EnemyCardList, m_Session.EnemyCards);
            PopulateCards(m_View.PlayerCardList, m_Session.PlayerCards);
            RefreshResult(m_Session.Result.Value);
        }

        protected override void OnUiUpdate(float deltaTime)
        {
            if (m_VictoryBannerActive)
                UpdateVictoryBanner(deltaTime);
            if (m_PopupAnimating)
                UpdatePopup(deltaTime);
        }

        protected override void OnUiClose()
        {
            m_ResultListener.RebindTarget(null);
            ResetResultPresentation();
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
            list.RefreshLayout();
        }

        private void RefreshResult(EBattleResult result)
        {
            switch (result)
            {
                case EBattleResult.PlayerVictory:
                    StartVictoryBanner();
                    break;
                case EBattleResult.EnemyVictory:
                    ShowResultPopup(false);
                    break;
            }
        }

        private void StartVictoryBanner()
        {
            m_VictoryBannerElapsed = 0f;
            m_VictoryBannerActive = true;
            if (m_View.ResultPopupRoot != null)
                m_View.ResultPopupRoot.SetActive(false);
            if (m_View.VictoryBannerRoot == null)
                return;
            m_View.VictoryBannerRoot.gameObject.SetActive(true);
            m_View.VictoryBannerRoot.anchoredPosition = new Vector2(-1450f, 0f);
            if (m_View.VictoryBannerCanvasGroup != null)
                m_View.VictoryBannerCanvasGroup.alpha = 1f;
        }

        private void UpdateVictoryBanner(float deltaTime)
        {
            m_VictoryBannerElapsed += Mathf.Max(0f, deltaTime);
            var enterEnd = BattleRules.VictoryBannerEnterDuration;
            var holdEnd = enterEnd + BattleRules.VictoryBannerHoldDuration;
            var exitEnd = holdEnd + BattleRules.VictoryBannerExitDuration;
            if (m_View.VictoryBannerRoot != null)
            {
                if (m_VictoryBannerElapsed < enterEnd)
                {
                    var progress = Mathf.SmoothStep(
                        0f,
                        1f,
                        m_VictoryBannerElapsed / BattleRules.VictoryBannerEnterDuration);
                    m_View.VictoryBannerRoot.anchoredPosition =
                        new Vector2(Mathf.Lerp(-1450f, 0f, progress), 0f);
                }
                else if (m_VictoryBannerElapsed < holdEnd)
                {
                    m_View.VictoryBannerRoot.anchoredPosition = Vector2.zero;
                }
                else
                {
                    var progress = Mathf.SmoothStep(
                        0f,
                        1f,
                        (m_VictoryBannerElapsed - holdEnd) / BattleRules.VictoryBannerExitDuration);
                    m_View.VictoryBannerRoot.anchoredPosition =
                        new Vector2(Mathf.Lerp(0f, 1450f, progress), 0f);
                }
            }

            if (m_VictoryBannerElapsed < exitEnd)
                return;
            m_VictoryBannerActive = false;
            if (m_View.VictoryBannerRoot != null)
                m_View.VictoryBannerRoot.gameObject.SetActive(false);
            if (m_Session != null && m_Session.IsFinalBattle)
                ShowResultPopup(true);
        }

        private void ShowResultPopup(bool wholeRunVictory)
        {
            if (m_View.ResultPopupRoot == null)
                return;
            var resourcePath = wholeRunVictory
                ? "RunVictoryPanelAged"
                : "BattleDefeatPanelAged";
            if (m_View.ResultPopupImage != null)
                m_View.ResultPopupImage.sprite = ResourceApi.LoadSprite(resourcePath);
            m_View.ResultPopupRoot.SetActive(true);
            m_PopupElapsed = 0f;
            m_PopupAnimating = true;
            if (m_View.ResultPopupCanvasGroup != null)
                m_View.ResultPopupCanvasGroup.alpha = 0f;
            m_View.ResultPopupRoot.transform.localScale = Vector3.one * 0.9f;
        }

        private void UpdatePopup(float deltaTime)
        {
            m_PopupElapsed += Mathf.Max(0f, deltaTime);
            var progress = Mathf.Clamp01(m_PopupElapsed / 0.18f);
            var eased = Mathf.SmoothStep(0f, 1f, progress);
            if (m_View.ResultPopupCanvasGroup != null)
                m_View.ResultPopupCanvasGroup.alpha = eased;
            if (m_View.ResultPopupRoot != null)
                m_View.ResultPopupRoot.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, eased);
            if (progress >= 1f)
                m_PopupAnimating = false;
        }

        private void OnReturnToMainMenuClicked()
        {
            if (m_View.ReturnToMainMenuButton != null)
                m_View.ReturnToMainMenuButton.interactable = false;
            HearthstoneGameEngine.Instance?.EnterMainMenuStageGroup();
        }

        private void ResetResultPresentation()
        {
            m_VictoryBannerActive = false;
            m_VictoryBannerElapsed = 0f;
            m_PopupAnimating = false;
            m_PopupElapsed = 0f;
            if (m_View.VictoryBannerRoot != null)
                m_View.VictoryBannerRoot.gameObject.SetActive(false);
            if (m_View.ResultPopupRoot != null)
                m_View.ResultPopupRoot.SetActive(false);
            if (m_View.ReturnToMainMenuButton != null)
                m_View.ReturnToMainMenuButton.interactable = true;
        }
    }
}
