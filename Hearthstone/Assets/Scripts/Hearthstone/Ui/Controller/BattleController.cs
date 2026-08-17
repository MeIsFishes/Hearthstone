using BbxCommon;
using BbxCommon.Ui;
using UnityEngine;

namespace Hearthstone
{
    public sealed class BattleController : UiControllerBase<BattleView>
    {
        private BattleSessionSingletonRawComponent m_Session;
        private ListenableItemListener m_ResultListener;
        private float m_ResultBannerElapsed;
        private bool m_ResultBannerActive;
        private float m_TimeScaleBeforeResult = 1f;
        private bool m_ResultPauseOwned;
        private bool m_ResultContinueReady;
        private bool m_ResultContinueConsumed;

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
            var presentationDeltaTime = m_ResultPauseOwned
                ? Time.unscaledDeltaTime
                : deltaTime;
            if (m_ResultBannerActive)
                UpdateResultBanner(presentationDeltaTime);
            if (m_ResultContinueReady &&
                m_ResultContinueConsumed == false &&
                Input.GetMouseButtonDown(0))
                ContinueAfterResult();
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
                    PauseForResult();
                    StartResultBanner(
                        m_Session != null && m_Session.IsFinalBattle
                            ? m_View.FinalVictoryResultBanner
                            : m_View.VictoryResultBanner);
                    break;
                case EBattleResult.EnemyVictory:
                    PauseForResult();
                    StartResultBanner(m_View.DefeatResultBanner);
                    break;
            }
        }

        private void StartResultBanner(Sprite resultBanner)
        {
            m_ResultBannerElapsed = 0f;
            m_ResultBannerActive = true;
            if (m_View.ResultBackdropImage != null)
                m_View.ResultBackdropImage.gameObject.SetActive(true);
            if (m_View.ResultBannerImage != null)
                m_View.ResultBannerImage.sprite = resultBanner;
            if (m_View.ResultBannerRoot == null)
            {
                m_ResultContinueReady = true;
                return;
            }
            if (resultBanner != null)
            {
                const float maxBannerWidth = 1200f;
                const float maxBannerHeight = 720f;
                var aspectRatio = resultBanner.rect.width / resultBanner.rect.height;
                var width = Mathf.Min(maxBannerWidth, maxBannerHeight * aspectRatio);
                m_View.ResultBannerRoot.sizeDelta = new Vector2(width, width / aspectRatio);
            }
            m_View.ResultBannerRoot.gameObject.SetActive(true);
            m_View.ResultBannerRoot.anchoredPosition = new Vector2(-1450f, 0f);
            if (m_View.ResultBannerCanvasGroup != null)
                m_View.ResultBannerCanvasGroup.alpha = 1f;
        }

        private void UpdateResultBanner(float deltaTime)
        {
            m_ResultBannerElapsed += Mathf.Max(0f, deltaTime);
            var enterEnd = BattleRules.VictoryBannerEnterDuration;
            if (m_ResultBannerElapsed >= enterEnd)
            {
                if (m_View.ResultBannerRoot != null)
                    m_View.ResultBannerRoot.anchoredPosition = Vector2.zero;
                m_ResultBannerActive = false;
                m_ResultContinueReady = true;
                return;
            }
            if (m_View.ResultBannerRoot == null)
                return;
            var progress = Mathf.SmoothStep(
                0f,
                1f,
                m_ResultBannerElapsed / BattleRules.VictoryBannerEnterDuration);
            m_View.ResultBannerRoot.anchoredPosition =
                new Vector2(Mathf.Lerp(-1450f, 0f, progress), 0f);
        }

        private void PauseForResult()
        {
            if (m_ResultPauseOwned)
                return;

            m_TimeScaleBeforeResult = Time.timeScale;
            Time.timeScale = 0f;
            m_ResultPauseOwned = true;
            m_ResultContinueReady = false;
            m_ResultContinueConsumed = false;
        }

        private void ContinueAfterResult()
        {
            if (m_ResultContinueReady == false ||
                m_ResultContinueConsumed ||
                m_Session == null)
                return;

            m_ResultContinueConsumed = true;
            AudioApi.Play("click1", 0.7f);
            var continueToPreparation =
                m_Session.Result.Value == EBattleResult.PlayerVictory &&
                m_Session.IsFinalBattle == false;
            ReleaseResultPause();
            if (continueToPreparation)
            {
                m_Session.OutcomePresentationCountdown = 0f;
                m_Session.OutcomePresentationCompleted.SetValue(true);
                return;
            }

            HearthstoneGameEngine.Instance?.EnterMainMenuStageGroup();
        }

        private void ReleaseResultPause()
        {
            if (m_ResultPauseOwned == false)
                return;

            Time.timeScale = m_TimeScaleBeforeResult;
            m_ResultPauseOwned = false;
        }

        private void ResetResultPresentation()
        {
            ReleaseResultPause();
            m_ResultBannerActive = false;
            m_ResultBannerElapsed = 0f;
            m_ResultContinueReady = false;
            m_ResultContinueConsumed = false;
            if (m_View.ResultBackdropImage != null)
                m_View.ResultBackdropImage.gameObject.SetActive(false);
            if (m_View.ResultBannerRoot != null)
                m_View.ResultBannerRoot.gameObject.SetActive(false);
        }
    }
}
