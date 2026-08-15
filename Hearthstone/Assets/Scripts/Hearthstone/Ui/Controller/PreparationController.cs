using System;
using System.Text;
using BbxCommon;
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hearthstone
{
    public sealed class PreparationController : UiControllerBase<PreparationView>
    {
        private const float FusionRevealFadeInDuration = 0.18f;
        private const float FusionRevealRotationDelay = 0.24f;
        private const float FusionRevealRotationDuration = 1.5f;
        private const float FusionRevealFlashDuration = 0.55f;
        private const float FusionRevealHoldDuration = 0.8f;
        private const float FusionRevealFadeOutDuration = 0.3f;
        private const float FusionRevealPlaybackSpeed = 0.8f;
        private const float FusionRevealInitialScale = 0.72f;
        private const float FusionRevealPeakScale = 1.28f;
        private const float FusionRevealRestScale = 1f;
        private const float FusionRevealResultRotationProgress = 0.75f;
        private const string FusionRevealMotionAudioKey = "card-shuffle";
        private const string FusionRevealMomentAudioKey = "highUp";
        private const string FusionRevealAudioGroup = "UiFusionReveal";

        private enum EOperationTab
        {
            Battle,
            Fusion,
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
        private bool m_FusionRevealMomentAudioPlayed;
        private bool m_ShowOwnedOnly;
        private readonly bool[] m_OwnedCardSnapshot = new bool[RunCardRules.LastCardNumber + 1];

        protected override void InitListeners()
        {
            m_RevisionListener = ModelWrapper.CreateVariableDirtyListener<int>(
                EControllerLifeCycle.Open,
                ignored => OnRunStateRevision());
            m_FusionRevisionListener = ModelWrapper.CreateVariableDirtyListener<int>(
                EControllerLifeCycle.Open,
                ignored => RefreshAll());
            m_ContinueStateListener = ModelWrapper.CreateVariableDirtyListener<EPreparationContinueState>(
                EControllerLifeCycle.Open,
                ApplyContinueState);
        }

        protected override void OnUiInit()
        {
            m_View.BattleTabButton.onClick.AddListener(() => SelectTab(EOperationTab.Battle));
            m_View.FusionTabButton.onClick.AddListener(() => SelectTab(EOperationTab.Fusion));
            m_View.FusionButton.onClick.AddListener(OnFuseClicked);
            m_View.ContinueButton.onClick.AddListener(OnContinueClicked);
            m_View.OwnedOnlyToggle.onValueChanged.AddListener(OnOwnedOnlyChanged);
            m_View.CardPoolScrollRect.scrollSensitivity *= 1.5f;
            m_View.CardPoolInteractor.Wrapper.OnInteract += OnCardPoolInteract;
            ResetFusionReveal();
        }

        protected override void OnUiUpdate(float deltaTime)
        {
            if (m_FusionRevealActive)
                UpdateFusionReveal(deltaTime);
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
            m_ShowOwnedOnly = false;
            m_View.OwnedOnlyToggle.SetIsOnWithoutNotify(false);
            PopulateItems();
            SelectTab(EOperationTab.Battle);
            ApplyContinueState(m_ContinueState.State.Value);
            ResetFusionReveal();
            RefreshAll();
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
            m_ShowOwnedOnly = false;
            ResetFusionReveal();
        }

        protected override void OnUiHide()
        {
            ResetFusionReveal();
        }

        internal void DropCardOnSlot(int cardNumber, int targetSlot)
        {
            if (RunCardRules.TryPlaceCard(m_RunState, cardNumber, targetSlot) == false)
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

        internal void ForwardCardPoolScroll(PointerEventData eventData)
        {
            if (eventData != null)
                m_View.CardPoolScrollRect.OnScroll(eventData);
        }

        private void PopulateItems()
        {
            PopulateCardPoolItems();

            m_View.BattleSlotList.ItemWrapper.ClearItems();
            for (var slot = 0; slot < RunCardRules.BattleSlotCount; slot++)
            {
                var item = m_View.BattleSlotList.ItemWrapper.AddItem<BattleCardItemController>();
                if (item == null)
                    throw new InvalidOperationException("BattleCardItemController preload mapping is missing.");
                item.BindPreparationBattleSlot(this, slot);
            }
            m_View.FusionSlotList.ItemWrapper.ClearItems();
            for (var slot = 0; slot < RunCardRules.FusionSlotCount; slot++)
            {
                var item = m_View.FusionSlotList.ItemWrapper.AddItem<BattleCardItemController>();
                if (item == null)
                    throw new InvalidOperationException("BattleCardItemController preload mapping is missing.");
                item.BindPreparationFusionSlot(this, slot);
            }

            m_View.FusionRevealCardList.ItemWrapper.ClearItems();
            m_FusionRevealCard = m_View.FusionRevealCardList.ItemWrapper.AddItem<BattleCardItemController>();
            if (m_FusionRevealCard == null)
                throw new InvalidOperationException("BattleCardItemController preload mapping is missing for fusion reveal.");
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
                if (m_ShowOwnedOnly && m_RunState.HasCard(cardNumber) == false)
                    continue;

                var item = m_View.CardPoolList.ItemWrapper.AddItem<BattleCardItemController>();
                if (item == null)
                    throw new InvalidOperationException("BattleCardItemController preload mapping is missing.");
                item.BindPreparation(this, cardNumber, displayNumber);
                itemCount++;
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
            CaptureOwnedCardSet();
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

            if (m_ShowOwnedOnly && HasOwnedCardSetChanged())
                PopulateCardPoolItems();
            RefreshAll();
        }

        private void CaptureOwnedCardSet()
        {
            for (var cardNumber = RunCardRules.FirstCardNumber;
                 cardNumber <= RunCardRules.LastCardNumber;
                 cardNumber++)
            {
                m_OwnedCardSnapshot[cardNumber] = m_RunState.HasCard(cardNumber);
            }
        }

        private bool HasOwnedCardSetChanged()
        {
            for (var cardNumber = RunCardRules.FirstCardNumber;
                 cardNumber <= RunCardRules.LastCardNumber;
                 cardNumber++)
            {
                if (m_OwnedCardSnapshot[cardNumber] != m_RunState.HasCard(cardNumber))
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
            m_View.FusionExpressionText.text = BuildFusionExpression();
            m_View.FusionResultText.text = BuildFusionResultText(evaluation);
            ApplyFusionEvaluationVisual(evaluation);
            m_View.FusionButton.interactable = evaluation.CanFuse;
        }

        private void SelectTab(EOperationTab tab)
        {
            m_Tab = tab;
            var battle = tab == EOperationTab.Battle;
            m_View.BattleOperationRoot.SetActive(battle);
            m_View.FusionOperationRoot.SetActive(!battle);
            m_View.BattleTabImage.sprite = ResourceApi.LoadSprite(
                battle ? "PreparationTabSelectedV2" : "PreparationTabIdleV2");
            m_View.FusionTabImage.sprite = ResourceApi.LoadSprite(
                battle ? "PreparationTabIdleV2" : "PreparationTabSelectedV2");
            RefreshAll();
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
            if (m_FusionRevealActive)
                return;

            var result = RunCardRules.TryFuse(
                m_RunState,
                m_Session,
                out var resultCard,
                out _);
            if (result != EFusionOperationResult.Applied)
            {
                RefreshAll();
                return;
            }

            StartFusionReveal(resultCard.CardNumber);
        }

        private void StartFusionReveal(int cardNumber)
        {
            if (m_FusionRevealCard == null)
            {
                DebugApi.LogError("Fusion reveal card UI was not initialized.");
                return;
            }

            m_FusionRevealCard.BindFusionReveal(m_RunState, cardNumber);
            StopFusionRevealAudio();
            m_FusionRevealElapsed = 0f;
            m_FusionRevealActive = true;
            m_FusionRevealMomentAudioPlayed = false;
            m_View.FusionRevealOverlay.SetActive(true);
            m_View.FusionRevealCanvasGroup.alpha = 0f;
            m_View.FusionRevealSealedFace.SetActive(true);
            m_View.FusionRevealCardBack.SetActive(false);
            m_View.FusionRevealCardList.gameObject.SetActive(false);
            m_View.FusionRevealFlash.gameObject.SetActive(false);
            m_View.FusionRevealFlashCanvasGroup.alpha = 0f;
            m_View.FusionRevealCardRoot.localRotation = Quaternion.identity;
            m_View.FusionRevealCardRoot.localScale = Vector3.one * FusionRevealInitialScale;
            m_View.FusionRevealCardRoot.anchoredPosition = new Vector2(0f, -35f);
            m_FusionRevealMotionAudio = PlayFusionRevealAudio(
                FusionRevealMotionAudioKey,
                0.55f,
                96,
                "FusionRevealMotion");
        }

        private void UpdateFusionReveal(float deltaTime)
        {
            m_FusionRevealElapsed += Mathf.Max(0f, deltaTime) * FusionRevealPlaybackSpeed;
            var rotationEnd = FusionRevealRotationDelay + FusionRevealRotationDuration;
            var flashEnd = rotationEnd + FusionRevealFlashDuration;
            var holdEnd = flashEnd + FusionRevealHoldDuration;
            var animationEnd = holdEnd + FusionRevealFadeOutDuration;

            var fadeIn = Mathf.Clamp01(m_FusionRevealElapsed / FusionRevealFadeInDuration);
            var fadeOut = Mathf.Clamp01((m_FusionRevealElapsed - holdEnd) / FusionRevealFadeOutDuration);
            m_View.FusionRevealCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, fadeIn) * (1f - fadeOut);

            var intro = Mathf.Clamp01(m_FusionRevealElapsed / FusionRevealRotationDelay);
            var rotationProgress = Mathf.Clamp01(
                (m_FusionRevealElapsed - FusionRevealRotationDelay) / FusionRevealRotationDuration);
            var easedRotation = Mathf.SmoothStep(0f, 1f, rotationProgress);
            var rotationY = easedRotation * 360f;
            m_View.FusionRevealCardRoot.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            float revealScale;
            if (intro < 1f)
            {
                revealScale = Mathf.Lerp(
                    FusionRevealInitialScale,
                    FusionRevealPeakScale,
                    Mathf.SmoothStep(0f, 1f, intro));
            }
            else
            {
                var shrinkProgress = Mathf.Clamp01(rotationProgress / FusionRevealResultRotationProgress);
                revealScale = Mathf.Lerp(
                    FusionRevealPeakScale,
                    FusionRevealRestScale,
                    Mathf.SmoothStep(0f, 1f, shrinkProgress));
            }
            m_View.FusionRevealCardRoot.localScale = Vector3.one * revealScale;
            m_View.FusionRevealCardRoot.anchoredPosition = new Vector2(
                0f,
                Mathf.Lerp(-35f, 0f, Mathf.SmoothStep(0f, 1f, intro)) + Mathf.Sin(m_FusionRevealElapsed * 4.5f) * 5f);

            var showBack = rotationY >= 90f && rotationY < 270f;
            var showResult = rotationY >= 270f;
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

            if (m_FusionRevealElapsed >= animationEnd)
                ResetFusionReveal();
        }

        private void ResetFusionReveal()
        {
            StopFusionRevealAudio();
            m_FusionRevealActive = false;
            m_FusionRevealElapsed = 0f;
            m_FusionRevealMomentAudioPlayed = false;
            if (m_View.FusionRevealOverlay == null)
                return;

            m_View.FusionRevealOverlay.SetActive(false);
            m_View.FusionRevealCanvasGroup.alpha = 0f;
            m_View.FusionRevealFlash.gameObject.SetActive(false);
            m_View.FusionRevealFlashCanvasGroup.alpha = 0f;
            m_View.FusionRevealCardRoot.localRotation = Quaternion.identity;
            m_View.FusionRevealCardRoot.localScale = Vector3.one;
            m_View.FusionRevealCardRoot.anchoredPosition = Vector2.zero;
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

        private void OnCardPoolInteract(Interactor requester, Interactor responder)
        {
            if (!ReferenceEquals(responder, m_View.CardPoolInteractor) ||
                !(requester is UiInteractor uiInteractor) ||
                !(uiInteractor.Wrapper.ExtraInfo is PreparationInteractorData source) ||
                source.Source != EPreparationCardSource.FusionSlot)
                return;
            RemoveFusionMaterial(source.SourceSlot);
        }

        private string BuildFusionExpression()
        {
            var builder = new StringBuilder();
            for (var slot = 0; slot < RunCardRules.FusionSlotCount; slot++)
            {
                var cardNumber = m_Session.FusionSlotCardNumbers[slot];
                if (cardNumber == 0)
                    continue;
                if (builder.Length > 0)
                    builder.Append(" + ");
                builder.Append(cardNumber);
            }
            if (builder.Length == 0)
                builder.Append('0');
            return builder.ToString();
        }

        private void ApplyFusionEvaluationVisual(FusionEvaluationData evaluation)
        {
            Color color;
            FontStyles style;
            if (evaluation.CanFuse)
            {
                color = m_View.FusionExactTargetColor;
                style = FontStyles.Bold | FontStyles.Underline;
            }
            else if (evaluation.BlockingResult == EFusionOperationResult.RecipeNotFound ||
                     evaluation.BlockingResult == EFusionOperationResult.ResultAlreadyOwned)
            {
                color = m_View.FusionOverTargetColor;
                style = FontStyles.Bold | FontStyles.Italic;
            }
            else
            {
                color = m_View.FusionUnderTargetColor;
                style = FontStyles.Bold;
            }
            m_View.FusionExpressionText.color = color;
            m_View.FusionExpressionText.fontStyle = style;
            m_View.FusionResultText.color = color;
            m_View.FusionResultText.fontStyle = style;
        }

        private static string BuildFusionResultText(FusionEvaluationData evaluation)
        {
            if (evaluation.ResultCardNumber > 0)
            {
                var card = DataApi.GetData<BattleCardCsvData>(evaluation.ResultCardNumber);
                var type = card == null ? null : DataApi.GetData<BattleCardTypeCsvData>(card.CardTypeId);
                var name = type == null ? $"#{evaluation.ResultCardNumber}" : type.DisplayName;
                if (evaluation.BlockingResult == EFusionOperationResult.ResultAlreadyOwned)
                    return $"{name} 已拥有";
                var prefix = evaluation.MaterialCount == RunCardRules.FusionSlotCount ? "传奇 → " : string.Empty;
                return $"{prefix}#{evaluation.ResultCardNumber} {name}";
            }

            switch (evaluation.BlockingResult)
            {
                case EFusionOperationResult.MaterialCountInvalid:
                    return "请选择2～4张基础卡";
                case EFusionOperationResult.RecipeNotFound:
                    return "没有对应的融合公式";
                case EFusionOperationResult.ResultCardCannotBeMaterial:
                    return "融合卡不能作为材料";
                default:
                    return "等待融合材料";
            }
        }

    }
}
