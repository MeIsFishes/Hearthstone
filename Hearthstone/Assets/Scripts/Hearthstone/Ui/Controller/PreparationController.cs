using System;
using System.Runtime.CompilerServices;
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

        protected override void InitListeners()
        {
            m_RevisionListener = ModelWrapper.CreateVariableDirtyListener<int>(
                EControllerLifeCycle.Open,
                ignored => RefreshAll());
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
            m_View.FusionButtonAttemptListener.AddCallback(EUiEvent.PointerClick, OnFusionButtonAttempt);
            m_View.ContinueButton.onClick.AddListener(OnContinueClicked);
            m_View.ContinueWaitingAttemptListener.AddCallback(EUiEvent.PointerClick, OnDuplicateContinueClicked);
            m_View.CardPoolScrollRect.onValueChanged.AddListener(OnCardPoolScrollChanged);
            m_View.FusionAreaInteractor.Wrapper.OnInteract += OnFusionAreaInteract;
            m_View.CardPoolInteractor.Wrapper.OnInteract += OnCardPoolInteract;
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
            if (m_View.RewardText != null)
                m_View.RewardText.text = $"本轮获得 {RunCardRules.RewardGrantCount} 张卡";
            PopulateItems();
            SelectTab(EOperationTab.Battle);
            ApplyContinueState(m_ContinueState.State.Value);
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
            LogFusion("SetMaterial", result, cardNumber, targetSlot);
            if (result != EFusionOperationResult.Applied)
                RefreshAll();
        }

        internal void ReportUnownedCardAttempt(int cardNumber)
        {
            if (m_Tab == EOperationTab.Fusion)
                LogFusion("SetMaterial", EFusionOperationResult.UnownedCard, cardNumber, -1);
        }

        internal void RemoveFusionMaterial(int cardNumber, int sourceSlot)
        {
            var result = RunCardRules.TryRemoveFusionMaterial(m_Session, sourceSlot);
            LogFusion("RemoveMaterial", result, cardNumber, sourceSlot);
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

        private void PopulateItems()
        {
            m_View.CardPoolList.ItemWrapper.ClearItems();
            for (var cardNumber = RunCardRules.FirstCardNumber; cardNumber <= RunCardRules.LastCardNumber; cardNumber++)
            {
                var item = m_View.CardPoolList.ItemWrapper.AddItem<PreparationCardItemController>();
                if (item == null)
                    throw new InvalidOperationException("PreparationCardItemController preload mapping is missing.");
                item.Bind(this, cardNumber);
            }

            m_View.BattleSlotList.ItemWrapper.ClearItems();
            for (var slot = 0; slot < RunCardRules.BattleSlotCount; slot++)
            {
                var item = m_View.BattleSlotList.ItemWrapper.AddItem<PreparationSlotItemController>();
                if (item == null)
                    throw new InvalidOperationException("PreparationSlotItemController preload mapping is missing.");
                item.Bind(this, slot);
            }
            m_View.FusionSlotList.ItemWrapper.ClearItems();
            for (var slot = 0; slot < RunCardRules.FusionSlotCount; slot++)
            {
                var item = m_View.FusionSlotList.ItemWrapper.AddItem<PreparationFusionSlotItemController>();
                if (item == null)
                    throw new InvalidOperationException("PreparationFusionSlotItemController preload mapping is missing.");
                item.Bind(this, slot);
            }
        }

        private void RefreshAll()
        {
            if (m_RunState == null)
                return;
            for (var index = 0; index < m_View.CardPoolList.ItemWrapper.Count; index++)
                m_View.CardPoolList.ItemWrapper.GetItem<PreparationCardItemController>(index).Refresh(
                    m_RunState,
                    m_Session,
                    m_Tab == EOperationTab.Fusion);
            for (var index = 0; index < m_View.BattleSlotList.ItemWrapper.Count; index++)
                m_View.BattleSlotList.ItemWrapper.GetItem<PreparationSlotItemController>(index).Refresh(m_RunState);
            for (var index = 0; index < m_View.FusionSlotList.ItemWrapper.Count; index++)
                m_View.FusionSlotList.ItemWrapper.GetItem<PreparationFusionSlotItemController>(index).Refresh(m_RunState, m_Session);

            var evaluation = RunCardRules.EvaluateFusion(m_RunState, m_Session);
            m_View.FusionExpressionText.text = BuildFusionExpression();
            m_View.FusionResultText.text = $"合计 {evaluation.CardNumberSum} / {RunCardRules.FusionTargetCardNumberSum}";
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
                battle ? "PreparationTabSelected" : "PreparationTabIdle");
            m_View.FusionTabImage.sprite = ResourceApi.LoadSprite(
                battle ? "PreparationTabIdle" : "PreparationTabSelected");
            RefreshAll();
            LogFusion("SelectTab", EFusionOperationResult.Applied, 0, -1);
            LogContinuePageState("TabChanged");
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

        private void OnDuplicateContinueClicked(PointerEventData ignored)
        {
            var engine = HearthstoneGameEngine.Instance;
            DebugApi.Log(
                $"[PreparationContinue] Action=DuplicateIgnored Result={EPreparationContinueResult.DuplicateIgnored} " +
                $"AttemptId={(engine == null ? 0 : engine.CurrentContinueAttemptId)} ButtonState={m_ContinueState?.State.Value}");
        }

        private void ApplyContinueState(EPreparationContinueState state)
        {
            if (m_View.ContinueButton == null || m_View.ContinueWaitingInputBlocker == null)
                return;
            var waiting = state == EPreparationContinueState.Waiting;
            m_View.ContinueButton.interactable = !waiting;
            m_View.ContinueWaitingInputBlocker.SetActive(waiting);
        }

        private void OnCardPoolScrollChanged(Vector2 ignored)
        {
            LogContinuePageState("ScrollChanged");
        }

        private void LogContinuePageState(string action)
        {
            if (m_RunState == null || m_Session == null || m_View.ContinueButton == null)
                return;
            var progression = EcsApi.GetSingletonRawComponent<RunProgressionSingletonRawComponent>();
            DebugApi.Log(
                $"[PreparationContinue] Action={action} Tab={m_Tab} " +
                $"Scroll={m_View.CardPoolScrollRect.verticalNormalizedPosition:F3} " +
                $"ButtonActive={m_View.ContinueButton.gameObject.activeInHierarchy} " +
                $"ButtonInteractable={m_View.ContinueButton.interactable} " +
                $"BattleNumber={(progression == null ? 0 : progression.CurrentBattleNumber)} " +
                $"BattleStageCreationCount={(progression == null ? 0 : progression.BattleStageCreationCount)}");
        }

        private void OnFuseClicked()
        {
            var result = RunCardRules.TryFuse(
                m_RunState,
                m_Session,
                out var card,
                out var transaction);
            if (result == EFusionOperationResult.Applied)
                LogFusionCommit(transaction);
            else
            {
                LogFusion("Fuse", result, card.CardNumber, -1);
                RefreshAll();
            }
        }

        private void OnFusionButtonAttempt(PointerEventData ignored)
        {
            var evaluation = RunCardRules.EvaluateFusion(m_RunState, m_Session);
            if (evaluation.CanFuse == false)
                LogFusion("FuseAttempt", evaluation.BlockingResult, 0, -1);
        }

        private void OnFusionAreaInteract(Interactor requester, Interactor responder)
        {
            if (!ReferenceEquals(responder, m_View.FusionAreaInteractor) ||
                !(requester is UiInteractor uiInteractor) ||
                !(uiInteractor.Wrapper.ExtraInfo is PreparationInteractorData source))
                return;
            LogFusion("SetMaterial", EFusionOperationResult.InvalidSlot, source.CardNumber, -1);
        }

        private void OnCardPoolInteract(Interactor requester, Interactor responder)
        {
            if (!ReferenceEquals(responder, m_View.CardPoolInteractor) ||
                !(requester is UiInteractor uiInteractor) ||
                !(uiInteractor.Wrapper.ExtraInfo is PreparationInteractorData source) ||
                source.Source != EPreparationCardSource.FusionSlot)
                return;
            RemoveFusionMaterial(source.CardNumber, source.SourceSlot);
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
            if (evaluation.CardNumberSum < RunCardRules.FusionTargetCardNumberSum)
            {
                color = m_View.FusionUnderTargetColor;
                style = FontStyles.Bold;
            }
            else if (evaluation.CardNumberSum == RunCardRules.FusionTargetCardNumberSum)
            {
                color = m_View.FusionExactTargetColor;
                style = FontStyles.Bold | FontStyles.Underline;
            }
            else
            {
                color = m_View.FusionOverTargetColor;
                style = FontStyles.Bold | FontStyles.Italic;
            }
            m_View.FusionExpressionText.color = color;
            m_View.FusionExpressionText.fontStyle = style;
            m_View.FusionResultText.color = color;
            m_View.FusionResultText.fontStyle = style;
        }

        private void LogFusionCommit(FusionTransactionSnapshot transaction)
        {
            var materials = new StringBuilder();
            var postMaterialOwnership = new StringBuilder();
            for (var index = 0; index < transaction.MaterialCount; index++)
            {
                if (index > 0)
                {
                    materials.Append(';');
                    postMaterialOwnership.Append(',');
                }
                var material = transaction.GetMaterial(index);
                materials.Append($"FusionSlot={material.FusionSlot},Card={material.CardNumber},Attack={material.Attack},MaxHealth={material.MaxHealth},AffectedBattleSlot={material.BattleSlot}");
                postMaterialOwnership.Append($"{material.CardNumber}:{m_RunState.HasCard(material.CardNumber)}");
            }
            var battleBefore = new int[RunCardRules.BattleSlotCount];
            for (var slot = 0; slot < battleBefore.Length; slot++)
                battleBefore[slot] = transaction.GetBattleSlotBefore(slot);

            DebugApi.Log(
                $"[PreparationFusion] Action=Fuse Result=Applied Stage=PreparationStage " +
                $"SessionId={RuntimeHelpers.GetHashCode(m_Session)} BatchId={m_Session.BatchId} " +
                $"Materials=[{materials}] BattleSlotsBefore=[{string.Join(",", battleBefore)}] " +
                $"ResultCard={transaction.ResultCard.CardNumber} ResultAttack={transaction.ResultCard.Attack} " +
                $"ResultMaxHealth={transaction.ResultCard.MaxHealth} PostFusionSlots=[{string.Join(",", m_Session.FusionSlotCardNumbers)}] " +
                $"PostBattleSlots=[{string.Join(",", m_RunState.BattleSlotCardNumbers)}] " +
                $"PostMaterialOwned=[{postMaterialOwnership}] ResultOwned={m_RunState.HasCard(transaction.ResultCard.CardNumber)} " +
                $"PostOwned={m_RunState.GetOwnedCardCount()} RunRevision={m_RunState.Revision.Value} " +
                $"FusionRevision={m_Session.FusionRevision.Value}");
        }

        private void LogFusion(string action, EFusionOperationResult result, int cardNumber, int slot)
        {
            var evaluation = RunCardRules.EvaluateFusion(m_RunState, m_Session);
            DebugApi.Log(
                $"[PreparationFusion] Action={action} Result={result} Stage=PreparationStage " +
                $"SessionId={RuntimeHelpers.GetHashCode(m_Session)} BatchId={m_Session.BatchId} Tab={m_Tab} " +
                $"RewardWasNewlyApplied={m_Session.WasNewlyApplied} " +
                $"AppliedBatchCount={m_RunState.AppliedRewardBatchPayloadFingerprints.Count} " +
                $"CardNumber={cardNumber} Slot={slot} FusionSlots=[{string.Join(",", m_Session.FusionSlotCardNumbers)}] " +
                $"Count={evaluation.MaterialCount} Sum={evaluation.CardNumberSum} CanFuse={evaluation.CanFuse} " +
                $"RunRevision={m_RunState.Revision.Value} FusionRevision={m_Session.FusionRevision.Value} " +
                $"Owned={m_RunState.GetOwnedCardCount()} BattleSlots=[{string.Join(",", m_RunState.BattleSlotCardNumbers)}]");
        }
    }
}
