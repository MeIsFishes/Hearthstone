using System;
using System.Collections.Generic;
using BbxCommon;
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hearthstone
{
    public sealed class PreparationController : UiControllerBase<PreparationView>
    {
        private const float FusionRevealFadeInDuration = 0.24f;
        private const float FusionRevealGatherDelay = 0.16f;
        private const float FusionRevealGatherDuration = 0.95f;
        private const float FusionRevealRotationDelay = 0.18f;
        private const float FusionRevealRotationDuration = 2.35f;
        private const float FusionRevealFlashDuration = 0.48f;
        private const float FusionRevealPlaybackSpeed = 0.8f;
        private const float FusionRevealMaterialStartScale = 0.78f;
        private const float FusionRevealMaterialEndScale = 0.05f;
        private const float FusionRevealInitialScale = 0.12f;
        private const float FusionRevealPeakScale = 1.28f;
        private const float FusionRevealRestScale = 0.82f;
        private const float FusionRevealGrowEndProgress = 0.22f;
        private const float FusionRevealShrinkEndProgress = 0.62f;
        private const float FusionRevealRotationTurns = 2f;
        private const float FusionRevealMinimumResultScale = 2f;
        private const float FusionRevealMinimumScreenHeightCoverage = 2f / 3f;
        private const float FusionRevealMaterialSpacing = 340f;
        private const float FusionRevealMaterialVerticalOffset = 42f;
        private const float FusionRevealMaterialRotationStep = 7f;
        private const float FusionRevealPocketDuration = 0.36f;
        private const float RewardRevealFadeDuration = 0.18f;
        private const float RewardRevealDealDuration = 0.3f;
        private const float RewardRevealDealStagger = 0.14f;
        private const float RewardRevealPocketDuration = 0.34f;
        private const float RewardRevealPocketStagger = 0.11f;
        private const float RewardRevealCardSpacing = 280f;
        private const float RewardRevealDisplayScale = 0.82f;
        private const float CardPocketFinalScale = 0.3f;
        private const float BattleSlotVisualFillRatio = 150f / 185f;
        private const float FusionSlotVisualFillRatio = 150f / 190f;
        private const float EnemyPreviewDrawerDuration = 0.34f;
        private const float EnemyPreviewCardVisualFillRatio = 150f / 185f;
        private const float FusionTargetGlowSpeed = 4.5f;
        private const float FusionTargetGlowScale = 0.05f;
        private const float FusionRecommendationRowStride = 236f;
        private const string FusionRevealMotionAudioKey = "card-shuffle";
        private const string FusionRevealMomentAudioKey = "highUp";
        private const string FusionRevealAudioGroup = "UiFusionReveal";
        private const string RewardRevealDealAudioKey = "card-place-1";
        private const string CardPocketAudioKey = "handleSmallLeather";
        private const string BattleSlotDropAudioKey = "drop_001";
        private const string EnemyPreviewDrawerAudioKey = "handleSmallLeather";
        private const string PreparationCardAnimationAudioGroup = "UiPreparationCardAnimation";

        private enum EOperationTab
        {
            Battle,
            Fusion,
        }

        private enum ERewardRevealPhase
        {
            Inactive,
            Dealing,
            AwaitingConfirm,
            Pocketing,
        }

        private RunStateSingletonRawComponent m_RunState;
        private PreparationSessionSingletonRawComponent m_Session;
        private PreparationContinueSingletonRawComponent m_ContinueState;
        private ListenableItemListener m_RevisionListener;
        private ListenableItemListener m_FusionRevisionListener;
        private ListenableItemListener m_ContinueStateListener;
        private EOperationTab m_Tab;
        private BattleCardItemController m_FusionRevealCard;
        private AudioHandle m_FusionRevealMotionAudio;
        private AudioHandle m_FusionRevealMomentAudio;
        private float m_FusionRevealElapsed;
        private bool m_FusionRevealActive;
        private bool m_FusionRevealAwaitingDismiss;
        private bool m_FusionRevealPocketActive;
        private bool m_FusionRevealMomentAudioPlayed;
        private float m_FusionRevealPocketElapsed;
        private Vector2 m_FusionRevealPocketStartPosition;
        private float m_FusionRevealPocketStartScale;
        private ERewardRevealPhase m_RewardRevealPhase;
        private float m_RewardRevealElapsed;
        private int m_RewardRevealNextAudioIndex;
        private string m_RewardRevealBatchId;
        private string m_LastConfirmedRewardBatchId;
        private float m_FusionTargetGlowElapsed;
        private bool m_FusionTargetGlowActive;
        private bool m_ShowOwnedOnly;
        private readonly int[] m_OwnedCardCountSnapshot = new int[RunCardRules.LastCardNumber + 1];
        private readonly List<FusionRecommendationData> m_FusionRecommendations =
            new List<FusionRecommendationData>();
        private int m_FirstVisibleFusionRecommendation = -1;
        private float m_EnemyPreviewDrawerProgress;
        private bool m_EnemyPreviewDrawerTargetOpen;
        private bool m_EnemyPreviewArrowPointsLeft;
        private bool m_EnemyPreviewAvailable;

        protected override void InitListeners()
        {
            m_RevisionListener = ModelWrapper.CreateVariableDirtyListener<int>(
                EControllerLifeCycle.Open,
                ignored => OnRunStateRevision());
            m_FusionRevisionListener = ModelWrapper.CreateVariableDirtyListener<int>(
                EControllerLifeCycle.Open,
                ignored => OnFusionRevision());
            m_ContinueStateListener = ModelWrapper.CreateVariableDirtyListener<EPreparationContinueState>(
                EControllerLifeCycle.Open,
                ApplyContinueState);
        }

        protected override void OnUiInit()
        {
            m_View.BattleTabButton.onClick.AddListener(() => SelectTab(EOperationTab.Battle));
            m_View.FusionTabButton.onClick.AddListener(() => SelectTab(EOperationTab.Fusion));
            m_View.FusionButton.onClick.AddListener(OnFuseClicked);
            m_View.FusionRevealDismissButton.onClick.AddListener(OnFusionRevealDismissClicked);
            m_View.RewardRevealConfirmButton.onClick.AddListener(OnRewardRevealConfirmed);
            m_View.FusionRecommendationButton.onClick.AddListener(OnFusionRecommendationClicked);
            m_View.FusionRecommendationHoverListener.AddCallback(
                EUiEvent.PointerEnter,
                OnFusionRecommendationPointerEnter);
            m_View.FusionRecommendationHoverListener.AddCallback(
                EUiEvent.PointerExit,
                OnFusionRecommendationPointerExit);
            m_View.FusionRecommendationCloseButton.onClick.AddListener(CloseFusionRecommendationPopup);
            m_View.FusionRecommendationScrollRect.onValueChanged.AddListener(
                OnFusionRecommendationScrollChanged);
            m_View.ContinueButton.onClick.AddListener(OnContinueClicked);
            m_View.OwnedOnlyToggle.onValueChanged.AddListener(OnOwnedOnlyChanged);
            m_View.EnemyPreviewToggleButton.onClick.AddListener(OnEnemyPreviewToggleClicked);
            m_View.CardPoolScrollRect.scrollSensitivity *= 1.5f;
            ResetFusionReveal();
            ResetRewardReveal();
            CloseFusionRecommendationPopup();
            HideFusionRecommendationTooltip();
            ResetEnemyPreviewDrawer();
        }

        protected override void OnUiUpdate(float deltaTime)
        {
            if (m_FusionTargetGlowActive)
                UpdateFusionTargetGlow(deltaTime);
            if (m_FusionRevealActive)
                UpdateFusionReveal(deltaTime);
            else if (m_FusionRevealPocketActive)
                UpdateFusionRevealPocket(deltaTime);
            if (m_RewardRevealPhase == ERewardRevealPhase.Dealing)
                UpdateRewardRevealDeal(deltaTime);
            else if (m_RewardRevealPhase == ERewardRevealPhase.Pocketing)
                UpdateRewardRevealPocket(deltaTime);
            UpdateEnemyPreviewDrawer(deltaTime);
        }

        protected override void OnUiOpen()
        {
            m_RunState = EcsApi.GetSingletonRawComponent<RunStateSingletonRawComponent>();
            m_Session = EcsApi.GetSingletonRawComponent<PreparationSessionSingletonRawComponent>();
            m_ContinueState = EcsApi.GetSingletonRawComponent<PreparationContinueSingletonRawComponent>();
            if (m_RunState == null || m_Session == null || m_ContinueState == null)
            {
                DebugApi.LogError("Preparation UI opened before runtime state was initialized.");
                return;
            }

            m_RevisionListener.RebindTarget(m_RunState.Revision);
            m_FusionRevisionListener.RebindTarget(m_Session.FusionRevision);
            m_ContinueStateListener.RebindTarget(m_ContinueState.State);
            m_ShowOwnedOnly = true;
            CardCollectionSave.RegisterOwnedCards(m_RunState);
            m_View.OwnedOnlyToggle.SetIsOnWithoutNotify(true);
            PopulateItems();
            ResetEnemyPreviewDrawer();
            SelectTab(EOperationTab.Battle);
            ApplyContinueState(m_ContinueState.State.Value);
            ResetFusionReveal();
            ResetRewardReveal();
            CloseFusionRecommendationPopup();
            HideFusionRecommendationTooltip();
            ResetEnemyPreviewDrawer();
            RefreshAll();
            TryStartRewardReveal();
        }

        protected override void OnUiClose()
        {
            m_RevisionListener.RebindTarget(null);
            m_FusionRevisionListener.RebindTarget(null);
            m_ContinueStateListener.RebindTarget(null);
            m_RunState = null;
            m_Session = null;
            m_ContinueState = null;
            m_FusionRevealCard = null;
            m_ShowOwnedOnly = true;
            SetFusionTargetGlow(false);
            ResetFusionReveal();
            ResetRewardReveal();
            CloseFusionRecommendationPopup();
            HideFusionRecommendationTooltip();
            ResetEnemyPreviewDrawer();
        }

        protected override void OnUiShow()
        {
            if (m_RunState != null)
            {
                RefreshAll();
                TryStartRewardReveal();
            }
        }

        protected override void OnUiHide()
        {
            SetFusionTargetGlow(false);
            ResetFusionReveal();
            ResetRewardReveal();
            CloseFusionRecommendationPopup();
            HideFusionRecommendationTooltip();
        }

        internal void DropCardOnSlot(int cardNumber, int targetSlot)
        {
            if (RunCardRules.TryPlaceCard(m_RunState, cardNumber, targetSlot))
                AudioApi.Play(BattleSlotDropAudioKey, 0.78f);
            else
                RefreshAll();
        }

        internal void DropCardOnFusionSlot(int cardNumber, int targetSlot, int sourceFusionSlot)
        {
            var result = RunCardRules.TrySetFusionMaterial(
                m_RunState,
                m_Session,
                cardNumber,
                targetSlot,
                sourceFusionSlot);
            if (result != EFusionOperationResult.Applied)
                RefreshAll();
        }

        internal void RemoveFusionMaterial(int sourceSlot)
        {
            var result = RunCardRules.TryRemoveFusionMaterial(m_Session, sourceSlot);
            if (result != EFusionOperationResult.Applied)
                RefreshAll();
        }

        internal void RemoveBattleCard(int sourceSlot, int cardNumber)
        {
            if (RunCardRules.TryRemoveCardFromBattleSlot(m_RunState, sourceSlot, cardNumber) == false)
                RefreshAll();
        }

        internal void OnDragReturned()
        {
            if (m_View.CardPoolList != null)
                m_View.CardPoolList.RefreshLayout();
            if (m_View.BattleSlotList != null)
                m_View.BattleSlotList.RefreshLayout();
            if (m_View.FusionSlotList != null)
                m_View.FusionSlotList.RefreshLayout();
            RefreshAll();
        }

        internal bool IsPointerInsideFusionArea(PointerEventData eventData)
        {
            if (eventData == null || m_View.FusionSlotList == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)m_View.FusionSlotList.transform,
                eventData.position,
                eventData.pressEventCamera);
        }

        internal bool IsPointerInsideBattleSlot(PointerEventData eventData, int slot)
        {
            if (eventData == null || m_View.BattleSlotList == null ||
                slot < 0 || slot >= m_View.BattleSlotList.ItemWrapper.Count)
                return false;

            var item = m_View.BattleSlotList.ItemWrapper.GetItem<BattleCardItemController>(slot);
            return item != null && RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)item.transform,
                eventData.position,
                eventData.pressEventCamera);
        }

        internal void ForwardCardPoolScroll(PointerEventData eventData)
        {
            if (eventData != null)
                m_View.CardPoolScrollRect.OnScroll(eventData);
        }

        internal void ForwardFusionRecommendationScroll(PointerEventData eventData)
        {
            if (eventData != null)
                m_View.FusionRecommendationScrollRect.OnScroll(eventData);
        }

        internal void ApplyFusionRecommendation(FusionRecommendationData recommendation)
        {
            var result = RunCardRules.TryApplyFusionRecommendation(
                m_RunState,
                m_Session,
                recommendation);
            if (result != EFusionOperationResult.Applied &&
                result != EFusionOperationResult.NoChange)
                RefreshAll();
            CloseFusionRecommendationPopup();
        }

        private void PopulateItems()
        {
            PopulateCardPoolItems();

            m_View.BattleSlotList.ItemWrapper.ClearItems();
            for (var slot = 0; slot < m_RunState.UnlockedBattleSlotCount; slot++)
            {
                var item = m_View.BattleSlotList.ItemWrapper.AddItem<BattleCardItemController>();
                if (item == null)
                    throw new InvalidOperationException("BattleCardItemController preload mapping is missing.");
                item.BindPreparationBattleSlot(
                    this,
                    slot,
                    m_View.BattleSlotList.ConstantSlotSize * BattleSlotVisualFillRatio);
            }
            m_View.FusionSlotList.ItemWrapper.ClearItems();
            for (var slot = 0; slot < RunCardRules.FusionSlotCount; slot++)
            {
                var item = m_View.FusionSlotList.ItemWrapper.AddItem<BattleCardItemController>();
                if (item == null)
                    throw new InvalidOperationException("BattleCardItemController preload mapping is missing.");
                item.BindPreparationFusionSlot(
                    this,
                    slot,
                    m_View.FusionSlotList.ConstantSlotSize * FusionSlotVisualFillRatio);
            }

            m_View.FusionRevealCardList.ItemWrapper.ClearItems();
            m_FusionRevealCard = m_View.FusionRevealCardList.ItemWrapper.AddItem<BattleCardItemController>();
            if (m_FusionRevealCard == null)
                throw new InvalidOperationException("BattleCardItemController preload mapping is missing for fusion reveal.");
            m_View.FusionRevealMaterialCardList.ItemWrapper.ClearItems();
            m_View.RewardRevealCardList.ItemWrapper.ClearItems();
            PopulateEnemyPreviewItems();
        }

        private void PopulateEnemyPreviewItems()
        {
            m_View.EnemyPreviewCardList.ItemWrapper.ClearItems();
            var preview = m_Session?.EnemyPreview;
            m_EnemyPreviewAvailable = preview != null && preview.CardCount > 0;
            m_View.EnemyPreviewToggleButton.interactable = m_EnemyPreviewAvailable;
            if (!m_EnemyPreviewAvailable)
                return;

            for (var slot = 0; slot < preview.CardCount; slot++)
            {
                var item = m_View.EnemyPreviewCardList.ItemWrapper.AddItem<BattleCardItemController>();
                if (item == null)
                    throw new InvalidOperationException(
                        "BattleCardItemController preload mapping is missing for enemy preview.");
                item.BindEnemyPreview(
                    preview.GetCard(slot),
                    m_View.EnemyPreviewCardList.ConstantSlotSize * EnemyPreviewCardVisualFillRatio);
            }
        }

        private void PopulateCardPoolItems()
        {
            var itemCount = 0;
            var nextLegendaryDisplayNumber = RunCardRules.FirstLegendaryCardNumber;
            m_View.CardPoolList.ItemWrapper.ClearItems();
            for (var cardNumber = RunCardRules.FirstCardNumber;
                 cardNumber <= RunCardRules.LastCardNumber;
                 cardNumber++)
            {
                var displayNumber = cardNumber;
                var cardConfig = DataApi.GetData<BattleCardCsvData>(cardNumber);
                var typeConfig = cardConfig == null
                    ? null
                    : DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
                if (typeConfig != null && typeConfig.Tier == EBattleCardTier.Legendary)
                    displayNumber = nextLegendaryDisplayNumber++;
                var copyCount = m_RunState.GetCardCopyCount(cardNumber);
                if (m_ShowOwnedOnly && copyCount == 0)
                    continue;
                var visibleCopyCount = Mathf.Max(1, copyCount);
                for (var copyIndex = 0; copyIndex < visibleCopyCount; copyIndex++)
                {
                    var item = m_View.CardPoolList.ItemWrapper.AddItem<BattleCardItemController>();
                    if (item == null)
                        throw new InvalidOperationException("BattleCardItemController preload mapping is missing.");
                    item.BindPreparation(this, cardNumber, displayNumber, copyIndex);
                    itemCount++;
                }
            }

            var rowCount = Mathf.Max(1, Mathf.CeilToInt(itemCount / (float)RunCardRules.CardsPerRow));
            var poolContent = (RectTransform)m_View.CardPoolList.transform;
            poolContent.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(
                    m_View.CardPoolScrollRect.viewport.rect.height,
                    m_View.CardPoolList.ConstantSlotSize.y * rowCount));
            m_View.CardPoolList.RefreshLayout();
            m_View.CardPoolScrollRect.StopMovement();
            m_View.CardPoolScrollRect.verticalNormalizedPosition = 1f;
            CaptureOwnedCardCounts();
        }

        private void OnOwnedOnlyChanged(bool showOwnedOnly)
        {
            if (m_RunState == null || m_ShowOwnedOnly == showOwnedOnly)
                return;

            m_ShowOwnedOnly = showOwnedOnly;
            PopulateCardPoolItems();
            RefreshAll();
        }

        private void OnRunStateRevision()
        {
            if (m_RunState == null)
                return;

            CloseFusionRecommendationPopup();
            if (HasOwnedCardCountChanged())
                PopulateCardPoolItems();
            RefreshAll();
        }

        private void OnFusionRevision()
        {
            CloseFusionRecommendationPopup();
            RefreshAll();
        }

        private void CaptureOwnedCardCounts()
        {
            for (var cardNumber = RunCardRules.FirstCardNumber;
                 cardNumber <= RunCardRules.LastCardNumber;
                 cardNumber++)
            {
                m_OwnedCardCountSnapshot[cardNumber] = m_RunState.GetCardCopyCount(cardNumber);
            }
        }

        private bool HasOwnedCardCountChanged()
        {
            for (var cardNumber = RunCardRules.FirstCardNumber;
                 cardNumber <= RunCardRules.LastCardNumber;
                 cardNumber++)
            {
                if (m_OwnedCardCountSnapshot[cardNumber] != m_RunState.GetCardCopyCount(cardNumber))
                    return true;
            }
            return false;
        }

        private void RefreshAll()
        {
            if (m_RunState == null)
                return;
            for (var index = 0; index < m_View.CardPoolList.ItemWrapper.Count; index++)
                m_View.CardPoolList.ItemWrapper.GetItem<BattleCardItemController>(index).RefreshPreparation(
                    m_RunState,
                    m_Session,
                    m_Tab == EOperationTab.Fusion);
            for (var index = 0; index < m_View.BattleSlotList.ItemWrapper.Count; index++)
                m_View.BattleSlotList.ItemWrapper.GetItem<BattleCardItemController>(index).RefreshPreparation(
                    m_RunState,
                    m_Session,
                    false);
            for (var index = 0; index < m_View.FusionSlotList.ItemWrapper.Count; index++)
                m_View.FusionSlotList.ItemWrapper.GetItem<BattleCardItemController>(index).RefreshPreparation(
                    m_RunState,
                    m_Session,
                    true);

            var evaluation = RunCardRules.EvaluateFusion(m_RunState, m_Session);
            m_View.FusionCurrentPointValue.text = evaluation.CardNumberSum.ToString();
            m_View.FusionRemainingPointValue.text =
                (RunCardRules.FusionTargetCardNumberSum - evaluation.CardNumberSum).ToString();
            ApplyFusionEvaluationVisual(evaluation);
            SetFusionTargetGlow(
                m_Tab == EOperationTab.Fusion &&
                evaluation.CardNumberSum == RunCardRules.FusionTargetCardNumberSum);
            m_View.FusionButton.interactable = evaluation.CanFuse;
            m_View.FusionRecommendationButton.interactable = true;
        }

        private void SelectTab(EOperationTab tab)
        {
            m_Tab = tab;
            var battle = tab == EOperationTab.Battle;
            m_View.BattleOperationRoot.SetActive(battle);
            m_View.FusionOperationRoot.SetActive(!battle);
            m_View.EnemyPreviewDrawerRoot.gameObject.SetActive(battle);
            if (!battle)
                ResetEnemyPreviewDrawer();
            m_View.BattleTabImage.sprite = ResourceApi.LoadSprite("MedievalParchmentControl");
            m_View.FusionTabImage.sprite = ResourceApi.LoadSprite("MedievalParchmentControl");
            m_View.BattleTabImage.color = battle
                ? new Color(0.96f, 0.92f, 0.82f, 1f)
                : new Color(0.62f, 0.58f, 0.51f, 1f);
            m_View.FusionTabImage.color = battle
                ? new Color(0.62f, 0.58f, 0.51f, 1f)
                : new Color(0.96f, 0.92f, 0.82f, 1f);
            if (battle)
            {
                SetFusionTargetGlow(false);
                CloseFusionRecommendationPopup();
                HideFusionRecommendationTooltip();
            }
            RefreshAll();
        }

        private void OnEnemyPreviewToggleClicked()
        {
            if (!m_EnemyPreviewAvailable || m_Tab != EOperationTab.Battle)
                return;

            m_EnemyPreviewDrawerTargetOpen = !m_EnemyPreviewDrawerTargetOpen;
            AudioApi.Play(EnemyPreviewDrawerAudioKey, 0.72f);
        }

        private void UpdateEnemyPreviewDrawer(float deltaTime)
        {
            if (m_View.EnemyPreviewDrawerRoot == null)
                return;

            var target = m_EnemyPreviewDrawerTargetOpen ? 1f : 0f;
            if (Mathf.Approximately(m_EnemyPreviewDrawerProgress, target))
                return;

            m_EnemyPreviewDrawerProgress = Mathf.MoveTowards(
                m_EnemyPreviewDrawerProgress,
                target,
                deltaTime / EnemyPreviewDrawerDuration);
            if (m_EnemyPreviewDrawerTargetOpen && m_EnemyPreviewDrawerProgress >= 1f)
                m_EnemyPreviewArrowPointsLeft = true;
            else if (!m_EnemyPreviewDrawerTargetOpen && m_EnemyPreviewDrawerProgress <= 0f)
                m_EnemyPreviewArrowPointsLeft = false;
            ApplyEnemyPreviewDrawerState();
        }

        private void ResetEnemyPreviewDrawer()
        {
            m_EnemyPreviewDrawerTargetOpen = false;
            m_EnemyPreviewDrawerProgress = 0f;
            m_EnemyPreviewArrowPointsLeft = false;
            ApplyEnemyPreviewDrawerState();
        }

        private void ApplyEnemyPreviewDrawerState()
        {
            if (m_View.EnemyPreviewDrawerRoot == null ||
                m_View.EnemyPreviewPanelCanvasGroup == null ||
                m_View.EnemyPreviewToggleArrow == null)
                return;

            var easedProgress = Mathf.SmoothStep(0f, 1f, m_EnemyPreviewDrawerProgress);
            m_View.EnemyPreviewDrawerRoot.anchoredPosition = Vector2.LerpUnclamped(
                m_View.EnemyPreviewClosedPosition,
                m_View.EnemyPreviewOpenPosition,
                easedProgress);
            m_View.EnemyPreviewToggleArrow.localRotation = Quaternion.Euler(
                0f,
                0f,
                m_EnemyPreviewArrowPointsLeft ? 180f : 0f);

            var panelProgress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(0.06f, 0.42f, m_EnemyPreviewDrawerProgress));
            m_View.EnemyPreviewPanelCanvasGroup.alpha = panelProgress;
            var open = m_EnemyPreviewDrawerProgress >= 0.98f;
            m_View.EnemyPreviewPanelCanvasGroup.interactable = open;
            m_View.EnemyPreviewPanelCanvasGroup.blocksRaycasts = open;
        }

        private void OnFusionRecommendationClicked()
        {
            HideFusionRecommendationTooltip();
            if (m_RunState == null || m_Session == null)
                return;

            RunCardRules.FindFusionRecommendations(
                m_RunState,
                m_Session,
                m_FusionRecommendations);
            var hasRecommendations = m_FusionRecommendations.Count > 0;
            m_View.FusionRecommendationOverlay.SetActive(true);
            m_View.FusionRecommendationEmptyText.gameObject.SetActive(!hasRecommendations);
            var list = m_View.FusionRecommendationList;
            list.ItemWrapper.ClearItems();
            var viewportHeight = m_View.FusionRecommendationScrollRect.viewport.rect.height;
            var contentRect = (RectTransform)list.transform;
            contentRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(
                    viewportHeight,
                    m_FusionRecommendations.Count * FusionRecommendationRowStride));
            if (hasRecommendations)
            {
                var visibleRowCount = Mathf.Min(
                    m_FusionRecommendations.Count,
                    Mathf.CeilToInt(viewportHeight / FusionRecommendationRowStride) + 1);
                list.ItemWrapper.ModifyCount<FusionRecommendationItemController>(visibleRowCount);
            }

            m_FirstVisibleFusionRecommendation = -1;
            m_View.FusionRecommendationScrollRect.StopMovement();
            m_View.FusionRecommendationScrollRect.verticalNormalizedPosition = 1f;
            RefreshVisibleFusionRecommendations();
        }

        private void OnFusionRecommendationPointerEnter(PointerEventData ignored)
        {
            if (m_View.FusionRecommendationOverlay.activeSelf == false)
                m_View.FusionRecommendationTooltip.SetActive(true);
        }

        private void OnFusionRecommendationPointerExit(PointerEventData ignored)
        {
            HideFusionRecommendationTooltip();
        }

        private void HideFusionRecommendationTooltip()
        {
            if (m_View?.FusionRecommendationTooltip != null)
                m_View.FusionRecommendationTooltip.SetActive(false);
        }

        private void OnFusionRecommendationScrollChanged(Vector2 ignored)
        {
            if (m_View.FusionRecommendationOverlay.activeSelf)
                RefreshVisibleFusionRecommendations();
        }

        private void RefreshVisibleFusionRecommendations()
        {
            var rowCount = m_View.FusionRecommendationList.ItemWrapper.Count;
            if (rowCount == 0 || m_FusionRecommendations.Count == 0)
                return;

            var contentRect = (RectTransform)m_View.FusionRecommendationList.transform;
            var firstIndex = Mathf.Clamp(
                Mathf.FloorToInt(Mathf.Max(0f, contentRect.anchoredPosition.y) /
                    FusionRecommendationRowStride),
                0,
                Mathf.Max(0, m_FusionRecommendations.Count - rowCount));
            if (m_FirstVisibleFusionRecommendation == firstIndex)
                return;

            m_FirstVisibleFusionRecommendation = firstIndex;
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var recommendationIndex = firstIndex + rowIndex;
                var item = m_View.FusionRecommendationList.ItemWrapper
                    .GetItem<FusionRecommendationItemController>(rowIndex);
                var itemRect = (RectTransform)item.transform;
                itemRect.anchorMin = new Vector2(0.5f, 1f);
                itemRect.anchorMax = new Vector2(0.5f, 1f);
                itemRect.pivot = new Vector2(0.5f, 0.5f);
                itemRect.anchoredPosition = new Vector2(
                    0f,
                    -(recommendationIndex + 0.5f) * FusionRecommendationRowStride);
                item.Bind(
                    this,
                    m_RunState,
                    m_Session,
                    m_FusionRecommendations[recommendationIndex]);
            }
        }

        private void CloseFusionRecommendationPopup()
        {
            m_FusionRecommendations.Clear();
            m_FirstVisibleFusionRecommendation = -1;
            if (m_View.FusionRecommendationOverlay != null)
                m_View.FusionRecommendationOverlay.SetActive(false);
            if (m_View.FusionRecommendationList != null)
                m_View.FusionRecommendationList.ItemWrapper.ClearItems();
            if (m_View.FusionRecommendationEmptyText != null)
                m_View.FusionRecommendationEmptyText.gameObject.SetActive(false);
        }

        private void OnContinueClicked()
        {
            var engine = HearthstoneGameEngine.Instance;
            if (engine == null)
            {
                DebugApi.LogError("[PreparationContinue] Result=InvalidRuntimeState Reason=EngineMissing");
                return;
            }
            engine.TryEnterNextBattleStageGroup();
        }

        private void ApplyContinueState(EPreparationContinueState state)
        {
            if (m_View.ContinueButton == null || m_View.ContinueWaitingInputBlocker == null)
                return;
            var waiting = state == EPreparationContinueState.Waiting;
            m_View.ContinueButton.interactable = !waiting;
            m_View.ContinueWaitingInputBlocker.SetActive(waiting);
        }

        private void OnFuseClicked()
        {
            if (m_FusionRevealActive ||
                m_FusionRevealAwaitingDismiss ||
                m_FusionRevealPocketActive)
                return;

            var result = RunCardRules.TryFuse(
                m_RunState,
                m_Session,
                out var resultCard,
                out var transaction);
            if (result != EFusionOperationResult.Applied)
            {
                RefreshAll();
                return;
            }

            CardCollectionSave.Repository.Register(resultCard.CardNumber);

            StartFusionReveal(resultCard.CardNumber, transaction);
        }

        private void StartFusionReveal(int cardNumber, FusionTransactionSnapshot transaction)
        {
            if (m_FusionRevealCard == null || transaction == null)
            {
                DebugApi.LogError("Fusion reveal UI or transaction snapshot was not initialized.");
                return;
            }

            m_FusionRevealCard.BindFusionReveal(m_RunState, cardNumber);
            m_FusionRevealCard.SetFusionRevealInteraction(false);
            PopulateFusionRevealMaterials(transaction);
            StopFusionRevealAudio();
            StopPreparationCardAnimationAudio();
            m_FusionRevealElapsed = 0f;
            m_FusionRevealActive = true;
            m_FusionRevealAwaitingDismiss = false;
            m_FusionRevealPocketActive = false;
            m_FusionRevealMomentAudioPlayed = false;
            m_View.FusionRevealOverlay.SetActive(true);
            m_View.FusionRevealCanvasGroup.alpha = 0f;
            m_View.FusionRevealCanvasGroup.interactable = false;
            m_View.FusionRevealDismissButton.interactable = false;
            m_View.FusionRevealMaterialCardList.gameObject.SetActive(true);
            m_View.FusionRevealCardRoot.gameObject.SetActive(false);
            m_View.FusionRevealSealedFace.SetActive(true);
            m_View.FusionRevealCardBack.SetActive(false);
            m_View.FusionRevealCardList.gameObject.SetActive(false);
            m_View.FusionRevealFlash.gameObject.SetActive(false);
            m_View.FusionRevealFlashCanvasGroup.alpha = 0f;
            m_View.FusionRevealCardRoot.localRotation = Quaternion.identity;
            m_View.FusionRevealCardRoot.localScale = Vector3.one * FusionRevealInitialScale;
            m_View.FusionRevealCardRoot.anchoredPosition = Vector2.zero;
            m_FusionRevealMotionAudio = PlayFusionRevealAudio(
                FusionRevealMotionAudioKey,
                0.55f,
                96,
                "FusionRevealMotion");
        }

        private void UpdateFusionReveal(float deltaTime)
        {
            m_FusionRevealElapsed += Mathf.Max(0f, deltaTime) * FusionRevealPlaybackSpeed;
            var gatherStart = FusionRevealGatherDelay;
            var gatherEnd = gatherStart + FusionRevealGatherDuration;
            var rotationStart = gatherEnd + FusionRevealRotationDelay;
            var rotationEnd = rotationStart + FusionRevealRotationDuration;
            var flashEnd = rotationEnd + FusionRevealFlashDuration;
            var revealMoment = rotationEnd + FusionRevealFlashDuration * 0.5f;

            var fadeIn = Mathf.Clamp01(m_FusionRevealElapsed / FusionRevealFadeInDuration);
            m_View.FusionRevealCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, fadeIn);
            UpdateFusionRevealMaterials(gatherStart, gatherEnd);

            var rotationStarted = m_FusionRevealElapsed >= rotationStart;
            m_View.FusionRevealCardRoot.gameObject.SetActive(rotationStarted);
            var rotationProgress = Mathf.Clamp01(
                (m_FusionRevealElapsed - rotationStart) / FusionRevealRotationDuration);
            var easedRotation = Mathf.SmoothStep(0f, 1f, rotationProgress);
            var rotationY = easedRotation * 360f * FusionRevealRotationTurns;
            m_View.FusionRevealCardRoot.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            var revealScale = EvaluateFusionRevealScale(rotationProgress);
            m_View.FusionRevealCardRoot.localScale = Vector3.one * revealScale;
            m_View.FusionRevealCardRoot.anchoredPosition = new Vector2(0f, Mathf.Sin(m_FusionRevealElapsed * 4.5f) * 5f);

            var rotationFrame = Mathf.Repeat(rotationY, 360f);
            var showBack = rotationStarted && rotationFrame >= 90f && rotationFrame < 270f;
            var showResult = m_FusionRevealElapsed >= revealMoment;
            m_View.FusionRevealSealedFace.SetActive(showBack == false && showResult == false);
            m_View.FusionRevealCardBack.SetActive(showBack);
            m_View.FusionRevealCardList.gameObject.SetActive(showResult);
            if (showResult && m_FusionRevealMomentAudioPlayed == false)
            {
                m_FusionRevealMomentAudioPlayed = true;
                m_FusionRevealMomentAudio = PlayFusionRevealAudio(
                    FusionRevealMomentAudioKey,
                    0.72f,
                    80,
                    "FusionRevealMoment");
            }

            var flashProgress = Mathf.Clamp01((m_FusionRevealElapsed - rotationEnd) / FusionRevealFlashDuration);
            var flashActive = m_FusionRevealElapsed >= rotationEnd && m_FusionRevealElapsed < flashEnd;
            m_View.FusionRevealFlash.gameObject.SetActive(flashActive);
            if (flashActive)
            {
                m_View.FusionRevealFlash.anchoredPosition = new Vector2(Mathf.Lerp(-260f, 260f, flashProgress), 0f);
                m_View.FusionRevealFlashCanvasGroup.alpha = Mathf.Sin(flashProgress * Mathf.PI);
            }

            if (m_FusionRevealElapsed >= flashEnd)
                CompleteFusionReveal();
        }

        private void PopulateFusionRevealMaterials(FusionTransactionSnapshot transaction)
        {
            var list = m_View.FusionRevealMaterialCardList;
            list.ItemWrapper.ClearItems();
            for (var index = 0; index < transaction.MaterialCount; index++)
            {
                var item = list.ItemWrapper.AddItem<BattleCardItemController>();
                if (item == null)
                    throw new InvalidOperationException("BattleCardItemController preload mapping is missing for fusion material reveal.");
                item.BindFusionMaterialReveal(transaction.GetMaterial(index));
                ApplyFusionRevealMaterialTransform(item, index, transaction.MaterialCount, 0f);
            }
        }

        private void UpdateFusionRevealMaterials(float gatherStart, float gatherEnd)
        {
            var list = m_View.FusionRevealMaterialCardList;
            var materialCount = list.ItemWrapper.Count;
            var progress = Mathf.InverseLerp(gatherStart, gatherEnd, m_FusionRevealElapsed);
            var eased = Mathf.SmoothStep(0f, 1f, progress);
            for (var index = 0; index < materialCount; index++)
            {
                var item = list.ItemWrapper.GetItem<BattleCardItemController>(index);
                ApplyFusionRevealMaterialTransform(item, index, materialCount, eased);
            }
            if (m_FusionRevealElapsed >= gatherEnd)
                list.gameObject.SetActive(false);
        }

        private static void ApplyFusionRevealMaterialTransform(
            BattleCardItemController item,
            int index,
            int materialCount,
            float progress)
        {
            var itemRect = (RectTransform)item.transform;
            var centeredIndex = index - (materialCount - 1f) * 0.5f;
            var startPosition = new Vector2(
                centeredIndex * FusionRevealMaterialSpacing,
                (index % 2 == 0 ? 1f : -1f) * FusionRevealMaterialVerticalOffset);
            itemRect.anchoredPosition = Vector2.Lerp(startPosition, Vector2.zero, progress);
            itemRect.localScale = Vector3.one * Mathf.Lerp(
                FusionRevealMaterialStartScale,
                FusionRevealMaterialEndScale,
                progress);
            itemRect.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Lerp(centeredIndex * FusionRevealMaterialRotationStep, 0f, progress));
        }

        private float EvaluateFusionRevealScale(float rotationProgress)
        {
            if (rotationProgress < FusionRevealGrowEndProgress)
            {
                var grow = Mathf.InverseLerp(0f, FusionRevealGrowEndProgress, rotationProgress);
                return Mathf.Lerp(FusionRevealInitialScale, FusionRevealPeakScale, Mathf.SmoothStep(0f, 1f, grow));
            }
            if (rotationProgress < FusionRevealShrinkEndProgress)
            {
                var shrink = Mathf.InverseLerp(
                    FusionRevealGrowEndProgress,
                    FusionRevealShrinkEndProgress,
                    rotationProgress);
                return Mathf.Lerp(FusionRevealPeakScale, FusionRevealRestScale, Mathf.SmoothStep(0f, 1f, shrink));
            }

            var finalGrow = Mathf.InverseLerp(FusionRevealShrinkEndProgress, 1f, rotationProgress);
            return Mathf.Lerp(FusionRevealRestScale, GetFusionRevealResultScale(), Mathf.SmoothStep(0f, 1f, finalGrow));
        }

        private float GetFusionRevealResultScale()
        {
            var overlayRect = m_View.FusionRevealOverlay.transform as RectTransform;
            var cardHeight = m_View.FusionRevealCardRoot.rect.height;
            if (overlayRect == null || overlayRect.rect.height <= 0f || cardHeight <= 0f)
                return FusionRevealMinimumResultScale;
            return Mathf.Max(
                FusionRevealMinimumResultScale,
                overlayRect.rect.height * FusionRevealMinimumScreenHeightCoverage / cardHeight);
        }

        private void CompleteFusionReveal()
        {
            StopFusionRevealAudio();
            m_FusionRevealActive = false;
            m_FusionRevealAwaitingDismiss = true;
            m_View.FusionRevealFlash.gameObject.SetActive(false);
            m_View.FusionRevealFlashCanvasGroup.alpha = 0f;
            m_View.FusionRevealCanvasGroup.interactable = true;
            m_View.FusionRevealDismissButton.interactable = true;
            m_FusionRevealCard.SetFusionRevealInteraction(true);
        }

        private void OnFusionRevealDismissClicked()
        {
            if (m_FusionRevealAwaitingDismiss)
                StartFusionRevealPocket();
        }

        private void StartFusionRevealPocket()
        {
            m_FusionRevealAwaitingDismiss = false;
            m_FusionRevealPocketActive = true;
            m_FusionRevealPocketElapsed = 0f;
            m_FusionRevealPocketStartPosition = m_View.FusionRevealCardRoot.anchoredPosition;
            m_FusionRevealPocketStartScale = m_View.FusionRevealCardRoot.localScale.x;
            m_View.FusionRevealCanvasGroup.interactable = false;
            m_View.FusionRevealDismissButton.interactable = false;
            m_FusionRevealCard.SetFusionRevealInteraction(false);
            StopPreparationCardAnimationAudio();
            PlayPreparationCardAnimationAudio(
                CardPocketAudioKey,
                0.68f,
                82,
                "FusionPocket",
                1,
                0f);
        }

        private void UpdateFusionRevealPocket(float deltaTime)
        {
            m_FusionRevealPocketElapsed += Mathf.Max(0f, deltaTime);
            var progress = Mathf.Clamp01(m_FusionRevealPocketElapsed / FusionRevealPocketDuration);
            var pocketTarget = GetPocketTarget(
                (RectTransform)m_View.FusionRevealOverlay.transform,
                m_View.FusionRevealCardRoot);
            ApplyPocketTransform(
                m_View.FusionRevealCardRoot,
                m_FusionRevealPocketStartPosition,
                m_FusionRevealPocketStartScale,
                pocketTarget,
                progress);
            if (progress >= 1f)
                ResetFusionReveal();
        }

        private void ResetFusionReveal()
        {
            StopFusionRevealAudio();
            StopPreparationCardAnimationAudio();
            m_FusionRevealActive = false;
            m_FusionRevealAwaitingDismiss = false;
            m_FusionRevealPocketActive = false;
            m_FusionRevealElapsed = 0f;
            m_FusionRevealPocketElapsed = 0f;
            m_FusionRevealPocketStartPosition = Vector2.zero;
            m_FusionRevealPocketStartScale = 1f;
            m_FusionRevealMomentAudioPlayed = false;
            if (m_View.FusionRevealOverlay == null)
                return;

            m_View.FusionRevealOverlay.SetActive(false);
            m_View.FusionRevealCanvasGroup.alpha = 0f;
            m_View.FusionRevealCanvasGroup.interactable = false;
            m_View.FusionRevealDismissButton.interactable = false;
            m_View.FusionRevealMaterialCardList.ItemWrapper.ClearItems();
            m_View.FusionRevealMaterialCardList.gameObject.SetActive(false);
            m_FusionRevealCard?.SetFusionRevealInteraction(false);
            m_View.FusionRevealFlash.gameObject.SetActive(false);
            m_View.FusionRevealFlashCanvasGroup.alpha = 0f;
            m_View.FusionRevealCardRoot.localRotation = Quaternion.identity;
            m_View.FusionRevealCardRoot.localScale = Vector3.one;
            m_View.FusionRevealCardRoot.anchoredPosition = Vector2.zero;
        }

        private void TryStartRewardReveal()
        {
            if (m_Session == null ||
                m_RewardRevealPhase != ERewardRevealPhase.Inactive ||
                m_Session.WasNewlyApplied == false ||
                m_Session.RewardCards == null ||
                m_Session.RewardCards.Length == 0 ||
                string.Equals(m_LastConfirmedRewardBatchId, m_Session.BatchId, StringComparison.Ordinal))
                return;

            var list = m_View.RewardRevealCardList;
            list.ItemWrapper.ClearItems();
            for (var index = 0; index < m_Session.RewardCards.Length; index++)
            {
                var item = list.ItemWrapper.AddItem<BattleCardItemController>();
                if (item == null)
                    throw new InvalidOperationException(
                        "BattleCardItemController preload mapping is missing for reward reveal.");
                item.BindPreparationRewardReveal(m_Session.RewardCards[index]);
                item.gameObject.SetActive(false);
            }

            StopPreparationCardAnimationAudio();
            m_RewardRevealBatchId = m_Session.BatchId;
            m_RewardRevealElapsed = 0f;
            m_RewardRevealNextAudioIndex = 0;
            m_RewardRevealPhase = ERewardRevealPhase.Dealing;
            m_View.RewardRevealOverlay.SetActive(true);
            m_View.RewardRevealCanvasGroup.alpha = 0f;
            m_View.RewardRevealCanvasGroup.interactable = false;
            m_View.RewardRevealConfirmButton.interactable = false;
        }

        private void UpdateRewardRevealDeal(float deltaTime)
        {
            m_RewardRevealElapsed += Mathf.Max(0f, deltaTime);
            m_View.RewardRevealCanvasGroup.alpha = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(m_RewardRevealElapsed / RewardRevealFadeDuration));
            var list = m_View.RewardRevealCardList;
            var cardCount = list.ItemWrapper.Count;
            while (m_RewardRevealNextAudioIndex < cardCount &&
                   m_RewardRevealElapsed >= m_RewardRevealNextAudioIndex * RewardRevealDealStagger)
            {
                var item = list.ItemWrapper.GetItem<BattleCardItemController>(m_RewardRevealNextAudioIndex);
                item.gameObject.SetActive(true);
                PlayPreparationCardAnimationAudio(
                    RewardRevealDealAudioKey,
                    0.5f,
                    104,
                    "RewardDeal",
                    3,
                    0.72f);
                m_RewardRevealNextAudioIndex++;
            }

            var overlayRect = (RectTransform)m_View.RewardRevealOverlay.transform;
            for (var index = 0; index < cardCount; index++)
            {
                var item = list.ItemWrapper.GetItem<BattleCardItemController>(index);
                var itemRect = (RectTransform)item.transform;
                var localElapsed = m_RewardRevealElapsed - index * RewardRevealDealStagger;
                var progress = Mathf.Clamp01(localElapsed / RewardRevealDealDuration);
                var eased = Mathf.SmoothStep(0f, 1f, progress);
                itemRect.anchoredPosition = Vector2.Lerp(
                    GetPocketTarget(overlayRect, itemRect),
                    GetRewardRevealPosition(index, cardCount),
                    eased);
                itemRect.localScale = Vector3.one * Mathf.Lerp(
                    CardPocketFinalScale,
                    RewardRevealDisplayScale,
                    eased);
                itemRect.localRotation = Quaternion.identity;
            }

            var dealEnd = (cardCount - 1) * RewardRevealDealStagger + RewardRevealDealDuration;
            if (m_RewardRevealElapsed < dealEnd)
                return;

            for (var index = 0; index < cardCount; index++)
            {
                var item = list.ItemWrapper.GetItem<BattleCardItemController>(index);
                var itemRect = (RectTransform)item.transform;
                itemRect.anchoredPosition = GetRewardRevealPosition(index, cardCount);
                itemRect.localScale = Vector3.one * RewardRevealDisplayScale;
                item.SetFusionRevealInteraction(true);
            }
            m_RewardRevealPhase = ERewardRevealPhase.AwaitingConfirm;
            m_View.RewardRevealCanvasGroup.alpha = 1f;
            m_View.RewardRevealCanvasGroup.interactable = true;
            m_View.RewardRevealConfirmButton.interactable = true;
        }

        private void OnRewardRevealConfirmed()
        {
            if (m_RewardRevealPhase != ERewardRevealPhase.AwaitingConfirm)
                return;

            StopPreparationCardAnimationAudio();
            m_RewardRevealElapsed = 0f;
            m_RewardRevealNextAudioIndex = 0;
            m_RewardRevealPhase = ERewardRevealPhase.Pocketing;
            m_View.RewardRevealCanvasGroup.interactable = false;
            m_View.RewardRevealConfirmButton.interactable = false;
            var list = m_View.RewardRevealCardList;
            for (var index = 0; index < list.ItemWrapper.Count; index++)
                list.ItemWrapper.GetItem<BattleCardItemController>(index).SetFusionRevealInteraction(false);
        }

        private void UpdateRewardRevealPocket(float deltaTime)
        {
            m_RewardRevealElapsed += Mathf.Max(0f, deltaTime);
            var list = m_View.RewardRevealCardList;
            var cardCount = list.ItemWrapper.Count;
            while (m_RewardRevealNextAudioIndex < cardCount &&
                   m_RewardRevealElapsed >= m_RewardRevealNextAudioIndex * RewardRevealPocketStagger)
            {
                PlayPreparationCardAnimationAudio(
                    CardPocketAudioKey,
                    0.58f,
                    92,
                    "RewardPocket",
                    3,
                    0.7f);
                m_RewardRevealNextAudioIndex++;
            }

            var overlayRect = (RectTransform)m_View.RewardRevealOverlay.transform;
            for (var index = 0; index < cardCount; index++)
            {
                var item = list.ItemWrapper.GetItem<BattleCardItemController>(index);
                var itemRect = (RectTransform)item.transform;
                var localElapsed = m_RewardRevealElapsed - index * RewardRevealPocketStagger;
                var progress = Mathf.Clamp01(localElapsed / RewardRevealPocketDuration);
                ApplyPocketTransform(
                    itemRect,
                    GetRewardRevealPosition(index, cardCount),
                    RewardRevealDisplayScale,
                    GetPocketTarget(overlayRect, itemRect),
                    progress);
                item.gameObject.SetActive(progress < 1f);
            }

            var pocketEnd = (cardCount - 1) * RewardRevealPocketStagger + RewardRevealPocketDuration;
            if (m_RewardRevealElapsed < pocketEnd)
                return;

            m_LastConfirmedRewardBatchId = m_RewardRevealBatchId;
            ResetRewardReveal();
        }

        private void ResetRewardReveal()
        {
            StopPreparationCardAnimationAudio();
            m_RewardRevealPhase = ERewardRevealPhase.Inactive;
            m_RewardRevealElapsed = 0f;
            m_RewardRevealNextAudioIndex = 0;
            m_RewardRevealBatchId = null;
            if (m_View.RewardRevealOverlay == null)
                return;

            m_View.RewardRevealOverlay.SetActive(false);
            m_View.RewardRevealCanvasGroup.alpha = 0f;
            m_View.RewardRevealCanvasGroup.interactable = false;
            m_View.RewardRevealConfirmButton.interactable = false;
            m_View.RewardRevealCardList.ItemWrapper.ClearItems();
        }

        private static Vector2 GetRewardRevealPosition(int index, int cardCount)
        {
            var centeredIndex = index - (cardCount - 1f) * 0.5f;
            return new Vector2(centeredIndex * RewardRevealCardSpacing, 0f);
        }

        private static Vector2 GetPocketTarget(RectTransform overlayRect, RectTransform cardRect)
        {
            var cardHeight = cardRect.rect.height > 0f
                ? cardRect.rect.height
                : 360f;
            return new Vector2(
                0f,
                overlayRect.rect.yMin - cardHeight * CardPocketFinalScale * 0.5f);
        }

        private static void ApplyPocketTransform(
            RectTransform cardRect,
            Vector2 startPosition,
            float startScale,
            Vector2 targetPosition,
            float progress)
        {
            var eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress), 2f);
            cardRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, eased);
            cardRect.localScale = Vector3.one * Mathf.Lerp(startScale, CardPocketFinalScale, eased);
            cardRect.localRotation = Quaternion.identity;
        }

        private static void PlayPreparationCardAnimationAudio(
            string key,
            float volume,
            int priority,
            string concurrencyKey,
            int maxConcurrent,
            float concurrencyVolumeFalloff)
        {
            var options = AudioPlayOptions.Default;
            options.Volume = volume;
            options.Priority = priority;
            options.GroupKey = PreparationCardAnimationAudioGroup;
            options.ConcurrencyKey = concurrencyKey;
            options.MaxConcurrent = maxConcurrent;
            options.ConcurrencyVolumeFalloff = concurrencyVolumeFalloff;
            AudioApi.Play(key, options);
        }

        private static void StopPreparationCardAnimationAudio()
        {
            AudioApi.StopGroup(PreparationCardAnimationAudioGroup);
        }

        private static AudioHandle PlayFusionRevealAudio(
            string key,
            float volume,
            int priority,
            string concurrencyKey)
        {
            var options = AudioPlayOptions.Default;
            options.Volume = volume;
            options.Priority = priority;
            options.GroupKey = FusionRevealAudioGroup;
            options.ConcurrencyKey = concurrencyKey;
            options.MaxConcurrent = 1;
            return AudioApi.Play(key, options);
        }

        private void StopFusionRevealAudio()
        {
            AudioApi.Stop(m_FusionRevealMotionAudio);
            AudioApi.Stop(m_FusionRevealMomentAudio);
            m_FusionRevealMotionAudio = default;
            m_FusionRevealMomentAudio = default;
        }

        private void ApplyFusionEvaluationVisual(FusionEvaluationData evaluation)
        {
            Color color;
            FontStyles style;
            if (evaluation.CardNumberSum == RunCardRules.FusionTargetCardNumberSum)
            {
                color = m_View.FusionExactTargetColor;
                style = FontStyles.Bold | FontStyles.Underline;
            }
            else if (evaluation.CardNumberSum > RunCardRules.FusionTargetCardNumberSum)
            {
                color = m_View.FusionOverTargetColor;
                style = FontStyles.Bold | FontStyles.Italic;
            }
            else
            {
                color = m_View.FusionUnderTargetColor;
                style = FontStyles.Bold;
            }
            m_View.FusionCurrentPointLabel.color = m_View.FusionUnderTargetColor;
            m_View.FusionCurrentPointLabel.fontStyle = FontStyles.Normal;
            m_View.FusionCurrentPointValue.color = color;
            m_View.FusionCurrentPointValue.fontStyle = style;
            m_View.FusionRemainingPointLabel.color = m_View.FusionUnderTargetColor;
            m_View.FusionRemainingPointLabel.fontStyle = FontStyles.Normal;
            m_View.FusionRemainingPointValue.color = m_View.FusionUnderTargetColor;
            m_View.FusionRemainingPointValue.fontStyle = FontStyles.Bold;
        }

        private void SetFusionTargetGlow(bool active)
        {
            if (m_FusionTargetGlowActive == active)
                return;

            m_FusionTargetGlowActive = active;
            m_FusionTargetGlowElapsed = 0f;
            if (active == false)
                m_View.FusionCurrentPointValue.rectTransform.localScale = Vector3.one;
        }

        private void UpdateFusionTargetGlow(float deltaTime)
        {
            m_FusionTargetGlowElapsed += Mathf.Max(0f, deltaTime);
            var pulse = 0.5f + 0.5f * Mathf.Sin(m_FusionTargetGlowElapsed * FusionTargetGlowSpeed);
            m_View.FusionCurrentPointValue.color = Color.Lerp(
                m_View.FusionExactTargetColor,
                Color.white,
                pulse * 0.8f);
            m_View.FusionCurrentPointValue.rectTransform.localScale =
                Vector3.one * (1f + pulse * FusionTargetGlowScale);
        }

    }
}
