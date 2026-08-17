using System;
using BbxCommon;
using BbxCommon.Ui;
using UnityEngine;

namespace Hearthstone
{
    public sealed class NewPlayerGuideController : UiControllerBase<NewPlayerGuideView>
    {
        public const int FusionPageIndex = 2;
        private const int TutorialCardNumber = 4;

        private Action m_OnDismissed;
        private int m_PageIndex;

        protected override void OnUiInit()
        {
            m_View.PreviousButton.onClick.AddListener(ShowPreviousPage);
            m_View.NextButton.onClick.AddListener(ShowNextPageOrDismiss);
        }

        protected override void OnUiOpen()
        {
            m_OnDismissed = null;
            m_PageIndex = 0;
            PopulateCardPreview();
            RefreshPage();
        }

        protected override void OnUiClose()
        {
            m_OnDismissed = null;
            m_View.CardPreviewList.ItemWrapper.ClearItems();
        }

        public void SetDismissedCallback(Action onDismissed)
        {
            m_OnDismissed = onDismissed;
        }

        public void ShowPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= m_View.PageRoots.Length)
                throw new ArgumentOutOfRangeException(nameof(pageIndex));

            m_PageIndex = pageIndex;
            RefreshPage();
        }

        private void PopulateCardPreview()
        {
            var list = m_View.CardPreviewList;
            list.ItemWrapper.ClearItems();
            var card = list.ItemWrapper.AddItem<BattleCardItemController>();
            if (card == null)
            {
                DebugApi.LogError("BattleCardItemController preload mapping is missing for the new-player guide.");
                return;
            }

            card.BindCollection(TutorialCardNumber, true, null, null);
            var cardRect = (RectTransform)card.transform;
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localScale = Vector3.one * 1.18f;
        }

        private void ShowPreviousPage()
        {
            if (m_PageIndex <= 0)
                return;
            --m_PageIndex;
            RefreshPage();
        }

        private void ShowNextPageOrDismiss()
        {
            if (m_PageIndex >= m_View.PageRoots.Length - 1)
            {
                Dismiss();
                return;
            }

            ++m_PageIndex;
            RefreshPage();
        }

        private void RefreshPage()
        {
            for (var index = 0; index < m_View.PageRoots.Length; index++)
                m_View.PageRoots[index].SetActive(index == m_PageIndex);

            m_View.PreviousButton.interactable = m_PageIndex > 0;
            m_View.PreviousButtonLabel.text = "上一页";
            m_View.NextButtonLabel.text = m_PageIndex == m_View.PageRoots.Length - 1
                ? "我知道了"
                : "下一页";
            m_View.PageIndicator.text = $"{m_PageIndex + 1} / {m_View.PageRoots.Length}";
        }

        private void Dismiss()
        {
            var callback = m_OnDismissed;
            m_OnDismissed = null;
            callback?.Invoke();
            ControllerWrapper.Close();
        }
    }
}
