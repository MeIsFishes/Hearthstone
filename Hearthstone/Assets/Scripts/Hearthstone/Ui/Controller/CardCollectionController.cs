using System;
using System.Collections.Generic;
using BbxCommon;
using BbxCommon.Ui;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hearthstone
{
    public sealed class CardCollectionController : UiControllerBase<CardCollectionView>
    {
        private const float GridCardScale = 0.8f;
        private const float PreviewCardScale = 2f;
        private const float PreviewOpenDuration = 0.28f;
        private const float PocketFinalScale = 0.3f;
        private const float PocketDuration = 0.36f;

        private readonly List<int> m_CardNumbers = new List<int>();
        private BattleCardItemController m_PreviewCard;
        private bool m_Opening;
        private float m_OpenElapsed;
        private Vector2 m_OpenStartPosition;
        private float m_OpenStartScale;
        private bool m_Pocketing;
        private float m_PocketElapsed;
        private Vector2 m_PocketStartPosition;
        private float m_PocketStartScale;

        protected override void OnUiInit()
        {
            m_View.BackButton.onClick.AddListener(OnBackClicked);
            m_View.PreviewDismissButton.onClick.AddListener(OnPreviewDismissed);
            m_View.CardScrollRect.scrollSensitivity *= 1.5f;
            ResetPreview();
        }

        protected override void OnUiOpen()
        {
            PopulateCards();
            ResetPreview();
        }

        protected override void OnUiShow()
        {
            PopulateCards();
            ResetPreview();
        }

        protected override void OnUiHide()
        {
            ResetPreview();
        }

        protected override void OnUiUpdate(float deltaTime)
        {
            if (m_Opening)
            {
                UpdatePreviewOpen(deltaTime);
                return;
            }
            if (m_Pocketing == false)
                return;
            m_PocketElapsed += Mathf.Max(0f, deltaTime);
            var progress = Mathf.Clamp01(m_PocketElapsed / PocketDuration);
            var eased = 1f - Mathf.Pow(1f - progress, 2f);
            var overlayRect = (RectTransform)m_View.PreviewOverlay.transform;
            var cardHeight = m_View.PreviewCardRoot.rect.height > 0f
                ? m_View.PreviewCardRoot.rect.height
                : 360f;
            var pocketTarget = new Vector2(
                0f,
                overlayRect.rect.yMin - cardHeight * PocketFinalScale * 0.5f);
            m_View.PreviewCardRoot.anchoredPosition = Vector2.LerpUnclamped(
                m_PocketStartPosition,
                pocketTarget,
                eased);
            var scale = Mathf.LerpUnclamped(m_PocketStartScale, PocketFinalScale, eased);
            m_View.PreviewCardRoot.localScale = Vector3.one * scale;
            if (progress >= 1f)
                ResetPreview();
        }

        private void UpdatePreviewOpen(float deltaTime)
        {
            m_OpenElapsed += Mathf.Max(0f, deltaTime);
            var progress = Mathf.Clamp01(m_OpenElapsed / PreviewOpenDuration);
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            m_View.PreviewCardRoot.anchoredPosition = Vector2.LerpUnclamped(
                m_OpenStartPosition,
                Vector2.zero,
                eased);
            var scale = Mathf.LerpUnclamped(m_OpenStartScale, PreviewCardScale, eased);
            m_View.PreviewCardRoot.localScale = Vector3.one * scale;
            if (progress < 1f)
                return;
            m_Opening = false;
            m_View.PreviewCardRoot.anchoredPosition = Vector2.zero;
            m_View.PreviewCardRoot.localScale = Vector3.one * PreviewCardScale;
            m_View.PreviewDismissButton.interactable = true;
        }

        private void PopulateCards()
        {
            m_CardNumbers.Clear();
            m_CardNumbers.AddRange(CardCollectionCatalog.GetCollectibleCardNumbers());
            var unlocked = CardCollectionSave.Repository.GetUnlockedSnapshot();
            m_View.CardList.ItemWrapper.ClearItems();
            var collectedCount = 0;
            for (var index = 0; index < m_CardNumbers.Count; index++)
            {
                var cardNumber = m_CardNumbers[index];
                var isUnlocked = unlocked.Contains(cardNumber);
                if (isUnlocked)
                    collectedCount++;
                var item = m_View.CardList.ItemWrapper.AddItem<BattleCardItemController>();
                if (item == null)
                    throw new InvalidOperationException("BattleCardItemController preload mapping is missing for collection.");
                item.BindCollection(cardNumber, isUnlocked, OpenPreview, ForwardScroll);
                item.transform.localScale = Vector3.one * GridCardScale;
            }

            m_View.CollectedCountText.text = $"已解锁 {collectedCount}/{m_CardNumbers.Count}";
            var rowCount = Mathf.Max(1, Mathf.CeilToInt(m_CardNumbers.Count / (float)RunCardRules.CardsPerRow));
            var content = (RectTransform)m_View.CardList.transform;
            content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(m_View.CardScrollRect.viewport.rect.height, m_View.CardList.ConstantSlotSize.y * rowCount));
            m_View.CardList.RefreshLayout();
            m_View.CardScrollRect.StopMovement();
            m_View.CardScrollRect.verticalNormalizedPosition = 1f;
        }

        private void OpenPreview(int cardNumber, RectTransform sourceRect)
        {
            if (m_Opening || m_Pocketing || sourceRect == null ||
                CardCollectionSave.Repository.IsUnlocked(cardNumber) == false)
                return;
            m_View.PreviewCardList.ItemWrapper.ClearItems();
            m_PreviewCard = m_View.PreviewCardList.ItemWrapper.AddItem<BattleCardItemController>();
            if (m_PreviewCard == null)
                throw new InvalidOperationException("BattleCardItemController preload mapping is missing for collection preview.");
            m_PreviewCard.BindCollection(cardNumber, true, null, null);
            m_View.PreviewOverlay.SetActive(true);
            m_View.PreviewCardRoot.position = sourceRect.position;
            m_OpenStartPosition = m_View.PreviewCardRoot.anchoredPosition;
            m_OpenStartScale = GridCardScale;
            m_View.PreviewCardRoot.localScale = Vector3.one * m_OpenStartScale;
            m_OpenElapsed = 0f;
            m_Opening = true;
            m_View.PreviewDismissButton.interactable = false;
            AudioApi.Play("click_001", 0.7f);
        }

        private void OnPreviewDismissed()
        {
            if (m_PreviewCard == null || m_Opening || m_Pocketing)
                return;
            m_Pocketing = true;
            m_PocketElapsed = 0f;
            m_PocketStartPosition = m_View.PreviewCardRoot.anchoredPosition;
            m_PocketStartScale = m_View.PreviewCardRoot.localScale.x;
            m_View.PreviewDismissButton.interactable = false;
            AudioApi.Play("handleSmallLeather", 0.68f);
        }

        private void ForwardScroll(PointerEventData eventData)
        {
            if (eventData != null)
                m_View.CardScrollRect.OnScroll(eventData);
        }

        private void ResetPreview()
        {
            m_Pocketing = false;
            m_Opening = false;
            m_OpenElapsed = 0f;
            m_OpenStartPosition = Vector2.zero;
            m_OpenStartScale = GridCardScale;
            m_PocketElapsed = 0f;
            m_PreviewCard = null;
            if (m_View.PreviewCardList != null)
                m_View.PreviewCardList.ItemWrapper.ClearItems();
            if (m_View.PreviewCardRoot != null)
            {
                m_View.PreviewCardRoot.anchoredPosition = Vector2.zero;
                m_View.PreviewCardRoot.localScale = Vector3.one * PreviewCardScale;
            }
            if (m_View.PreviewOverlay != null)
                m_View.PreviewOverlay.SetActive(false);
        }

        private void OnBackClicked()
        {
            ResetPreview();
            var mainMenu = UiApi.GetUiController<MainMenuController>();
            mainMenu?.ControllerWrapper.Show();
            ControllerWrapper.Hide();
        }
    }
}
