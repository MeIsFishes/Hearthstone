using BbxCommon;
using BbxCommon.Ui;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hearthstone
{
    public sealed class BattleCardItemController : UiControllerBase<BattleCardItemView>
    {
        private const string EnemyCardFrameArtworkKey = "CardFrame-v3";
        private const string PlayerCardFrameArtworkKey = "CardFrameBlue-v2";
        private const float PreparationPoolScale = 0.8f;

        private Entity m_BoundEntity;
        private BattleCardRawComponent m_Card;
        private BattleSessionSingletonRawComponent m_Session;
        private PreparationController m_PreparationPage;
        private int m_PreparationCardNumber;
        private bool m_PreparationCardOwned;
        private ListenableItemListener m_HealthListener;
        private ListenableItemListener m_AttackListener;
        private ListenableItemListener m_AliveListener;
        private ListenableItemListener m_AttackerListener;
        private ListenableItemListener m_TargetListener;

        protected override void InitListeners()
        {
            m_HealthListener = ModelWrapper.CreateVariableDirtyListener<int>(
                EControllerLifeCycle.Init,
                RefreshHealth);
            m_AttackListener = ModelWrapper.CreateVariableDirtyListener<int>(
                EControllerLifeCycle.Init,
                RefreshAttack);
            m_AliveListener = ModelWrapper.CreateVariableDirtyListener<bool>(
                EControllerLifeCycle.Init,
                RefreshAlive);
            m_AttackerListener = ModelWrapper.CreateVariableDirtyListener<Entity>(
                EControllerLifeCycle.Init,
                RefreshHighlights);
            m_TargetListener = ModelWrapper.CreateVariableDirtyListener<Entity>(
                EControllerLifeCycle.Init,
                RefreshHighlights);
        }

        protected override void OnUiInit()
        {
            if (m_View.PreparationDragable != null)
                m_View.PreparationDragable.Wrapper.OnBackFromTop += OnPreparationDragReturned;
            if (m_View.PreparationInteractor != null)
                m_View.PreparationInteractor.Wrapper.OnInteract += OnPreparationInteract;
            if (m_View.PreparationEmptyAttemptListener != null)
                m_View.PreparationEmptyAttemptListener.AddCallback(EUiEvent.PointerClick, OnPreparationEmptyAttempt);
            ApplyPreparationState(false, false, false);
        }

        public void Bind(Entity entity)
        {
            ResetBinding();
            if (entity == Entity.Null)
                return;

            var card = entity.GetRawComponent<BattleCardRawComponent>();
            if (card == null)
            {
                DebugApi.LogError("Cannot bind battle card UI to an Entity without BattleCardRawComponent.");
                return;
            }

            m_BoundEntity = entity;
            m_Card = card;
            m_Session = EcsApi.GetSingletonRawComponent<BattleSessionSingletonRawComponent>();
            m_Card.SyncAttackValue();
            m_AttackListener.RebindTarget(m_Card.AttackValue);
            m_HealthListener.RebindTarget(m_Card.CurrentHealth);
            m_AliveListener.RebindTarget(m_Card.IsAlive);
            if (m_Session != null)
            {
                m_AttackerListener.RebindTarget(m_Session.CurrentAttacker);
                m_TargetListener.RebindTarget(m_Session.CurrentTarget);
            }
            RefreshAll();
        }

        internal void BindPreparation(PreparationController page, int cardNumber)
        {
            ResetBinding();
            m_PreparationPage = page;
            m_PreparationCardNumber = cardNumber;
            transform.localScale = Vector3.one * PreparationPoolScale;
            if (m_View.PreparationInteractor != null)
            {
                m_View.PreparationInteractor.Wrapper.ExtraInfo = new PreparationInteractorData
                {
                    CardNumber = cardNumber,
                    Source = EPreparationCardSource.CardPool,
                    SourceSlot = -1,
                    TargetSlot = -1,
                };
            }
        }

        internal void RefreshPreparation(
            RunStateSingletonRawComponent runState,
            PreparationSessionSingletonRawComponent session,
            bool fusionTabSelected)
        {
            if (m_PreparationPage == null || runState == null)
                return;

            m_PreparationCardOwned = runState.HasCard(m_PreparationCardNumber);
            var selectedAsMaterial = false;
            if (session != null)
            {
                for (var slot = 0; slot < session.FusionSlotCardNumbers.Length; slot++)
                {
                    if (session.FusionSlotCardNumbers[slot] != m_PreparationCardNumber)
                        continue;
                    selectedAsMaterial = true;
                    break;
                }
            }

            ApplyPreparationState(true, m_PreparationCardOwned, fusionTabSelected && selectedAsMaterial);
            if (m_View.CardNumberText != null)
                m_View.CardNumberText.text = m_PreparationCardNumber.ToString("00");
            if (m_View.CardNumberBadge != null)
                m_View.CardNumberBadge.gameObject.SetActive(true);
            if (m_PreparationCardOwned == false)
            {
                HideCardPresentation(false);
                return;
            }

            var instance = runState.CardInstances[m_PreparationCardNumber];
            var cardConfig = DataApi.GetData<BattleCardCsvData>(m_PreparationCardNumber);
            if (cardConfig == null)
            {
                DebugApi.LogError($"Battle card configuration {m_PreparationCardNumber} is missing.");
                HideCardPresentation(false);
                return;
            }
            var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
            if (typeConfig == null)
            {
                DebugApi.LogError($"Battle card type {cardConfig.CardTypeId} is missing.");
                HideCardPresentation(false);
                return;
            }

            ShowCardPresentation(PlayerCardFrameArtworkKey);
            ApplyCardContent(
                cardConfig,
                typeConfig,
                instance.Keywords,
                instance.Attack,
                instance.MaxHealth);
            RefreshAlive(true);
            RefreshHighlights(Entity.Null);
        }

        protected override void OnUiClose()
        {
            ResetBinding();
        }

        private void ResetBinding()
        {
            m_HealthListener.RebindTarget(null);
            m_AttackListener.RebindTarget(null);
            m_AliveListener.RebindTarget(null);
            m_AttackerListener.RebindTarget(null);
            m_TargetListener.RebindTarget(null);
            m_BoundEntity = Entity.Null;
            m_Card = null;
            m_Session = null;
            m_PreparationPage = null;
            m_PreparationCardNumber = 0;
            m_PreparationCardOwned = false;

            if (m_View != null)
            {
                transform.localScale = Vector3.one;
                m_View.transform.localRotation = Quaternion.identity;
                HideCardPresentation(true);
                ApplyPreparationState(false, false, false);
                if (m_View.PreparationInteractor != null)
                    m_View.PreparationInteractor.Wrapper.ExtraInfo = null;
            }
        }

        private void RefreshAll()
        {
            if (m_Card == null)
                return;

            ApplyPreparationState(false, false, false);
            var frameKey = m_Card.Side == EBattleSide.Player
                ? PlayerCardFrameArtworkKey
                : EnemyCardFrameArtworkKey;
            ShowCardPresentation(frameKey);

            RefreshCardNumber();
            var cardConfig = DataApi.GetData<BattleCardCsvData>(m_Card.CardNumber);
            var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(m_Card.CardTypeId);
            if (cardConfig == null)
                DebugApi.LogError($"Battle card configuration {m_Card.CardNumber} is missing.");
            else if (typeConfig == null)
                DebugApi.LogError($"Battle card type {m_Card.CardTypeId} is missing.");
            else
                ApplyCardContent(cardConfig, typeConfig, m_Card.Keywords, m_Card.Attack, m_Card.CurrentHealth.Value);
            RefreshHealth(m_Card.CurrentHealth.Value);
            RefreshAlive(m_Card.IsAlive.Value);
            RefreshHighlights(Entity.Null);
        }

        private void RefreshHealth(int health)
        {
            if (m_View.HealthText != null)
                m_View.HealthText.text = health.ToString();
        }

        private void RefreshAttack(int attack)
        {
            if (m_View.AttackText != null)
                m_View.AttackText.text = attack.ToString();
        }

        private void ApplyCardContent(
            BattleCardCsvData cardConfig,
            BattleCardTypeCsvData typeConfig,
            EBattleKeyword keywords,
            int attack,
            int health)
        {
            if (m_View.SkillDescriptionText != null)
            {
                m_View.SkillDescriptionText.text = typeConfig.DisplayName;
                m_View.SkillDescriptionText.gameObject.SetActive(true);
            }

            var keywordText = BattleKeywordRules.FormatDisplayText(keywords);
            if (m_View.KeywordText != null)
            {
                m_View.KeywordText.text = keywordText;
                m_View.KeywordText.gameObject.SetActive(string.IsNullOrEmpty(m_View.KeywordText.text) == false);
            }
            if (m_Card != null)
            {
                DebugApi.Log(
                    $"[BattleKeyword] Presentation Side={m_Card.Side} Slot={m_Card.SlotIndex} " +
                    $"Card={m_Card.CardNumber} Keywords={m_Card.Keywords} Text='{keywordText}'");
            }

            if (m_View.ArtworkArea != null)
            {
                m_View.ArtworkArea.sprite = ResourceApi.LoadSprite(cardConfig.ArtworkKey);
                m_View.ArtworkArea.preserveAspect = true;
                m_View.ArtworkArea.gameObject.SetActive(m_View.ArtworkArea.sprite != null);
                if (m_View.ArtworkArea.sprite == null)
                    DebugApi.LogError($"Battle card artwork '{cardConfig.ArtworkKey}' is missing.");
            }
            RefreshAttack(attack);
            RefreshHealth(health);
        }

        private void ShowCardPresentation(string frameKey)
        {
            if (m_View.CardBackground != null)
                m_View.CardBackground.color = Color.clear;
            if (m_View.CardFrame != null)
            {
                m_View.CardFrame.gameObject.SetActive(true);
                var frameSprite = ResourceApi.LoadSprite(frameKey);
                m_View.CardFrame.sprite = frameSprite;
                m_View.CardFrame.color = Color.white;
                if (m_View.AttackerHighlight != null)
                    m_View.AttackerHighlight.sprite = frameSprite;
                if (m_View.TargetHighlight != null)
                    m_View.TargetHighlight.sprite = frameSprite;
                if (frameSprite == null)
                    DebugApi.LogError($"Battle card frame artwork '{frameKey}' is missing.");
            }
            if (m_View.AttackText != null)
                m_View.AttackText.transform.parent.gameObject.SetActive(true);
            if (m_View.HealthText != null)
                m_View.HealthText.transform.parent.gameObject.SetActive(true);
            m_View.transform.localRotation = Quaternion.identity;
        }

        private void HideCardPresentation(bool hideCardNumber)
        {
            if (m_View.SkillDescriptionText != null)
            {
                m_View.SkillDescriptionText.text = string.Empty;
                m_View.SkillDescriptionText.gameObject.SetActive(false);
            }
            if (m_View.KeywordText != null)
            {
                m_View.KeywordText.text = string.Empty;
                m_View.KeywordText.gameObject.SetActive(false);
            }
            if (m_View.ArtworkArea != null)
            {
                m_View.ArtworkArea.sprite = null;
                m_View.ArtworkArea.gameObject.SetActive(false);
            }
            if (m_View.CardFrame != null)
                m_View.CardFrame.gameObject.SetActive(false);
            if (m_View.AttackText != null)
            {
                m_View.AttackText.text = string.Empty;
                m_View.AttackText.transform.parent.gameObject.SetActive(false);
            }
            if (m_View.HealthText != null)
            {
                m_View.HealthText.text = string.Empty;
                m_View.HealthText.transform.parent.gameObject.SetActive(false);
            }
            if (hideCardNumber && m_View.CardNumberText != null)
                m_View.CardNumberText.text = string.Empty;
            if (hideCardNumber && m_View.CardNumberBadge != null)
                m_View.CardNumberBadge.gameObject.SetActive(false);
            if (m_View.DeadOverlay != null)
                m_View.DeadOverlay.gameObject.SetActive(false);
            if (m_View.AttackerHighlight != null)
                m_View.AttackerHighlight.gameObject.SetActive(false);
            if (m_View.TargetHighlight != null)
                m_View.TargetHighlight.gameObject.SetActive(false);
        }

        private void ApplyPreparationState(bool preparationMode, bool owned, bool materialSelected)
        {
            if (m_View.PreparationEmptyState != null)
                m_View.PreparationEmptyState.SetActive(preparationMode && owned == false);
            if (m_View.PreparationMaterialSelectedState != null)
                m_View.PreparationMaterialSelectedState.SetActive(preparationMode && materialSelected);
            if (m_View.PreparationInteractor != null)
                m_View.PreparationInteractor.enabled = preparationMode;
            if (m_View.PreparationDragable != null)
            {
                var dragEnabled = preparationMode && owned;
                m_View.PreparationDragable.enabled = dragEnabled;
                if (m_View.PreparationDragable.EventListener != null)
                    m_View.PreparationDragable.EventListener.enabled = dragEnabled;
            }
            if (m_View.PreparationEmptyAttemptListener != null)
                m_View.PreparationEmptyAttemptListener.enabled = preparationMode && owned == false;
            if (m_View.CardBackground != null)
                m_View.CardBackground.raycastTarget = preparationMode;
        }

        private void RefreshCardNumber()
        {
            if (m_View.CardNumberText != null)
                m_View.CardNumberText.text = m_Card.CardNumber.ToString();
            if (m_View.CardNumberBadge != null)
                m_View.CardNumberBadge.gameObject.SetActive(true);
        }

        private void RefreshAlive(bool alive)
        {
            if (m_View.DeadOverlay != null)
                m_View.DeadOverlay.gameObject.SetActive(alive == false);
        }

        private void RefreshHighlights(Entity ignored)
        {
            var isAttacker = m_Session != null && m_Session.CurrentAttacker.Value == m_BoundEntity;
            var isTarget = m_Session != null && m_Session.CurrentTarget.Value == m_BoundEntity;
            if (m_View.AttackerHighlight != null)
                m_View.AttackerHighlight.gameObject.SetActive(isAttacker);
            if (m_View.TargetHighlight != null)
                m_View.TargetHighlight.gameObject.SetActive(isTarget);
        }

        private void OnPreparationDragReturned(PointerEventData ignored)
        {
            m_PreparationPage?.OnDragReturned();
        }

        private void OnPreparationEmptyAttempt(PointerEventData ignored)
        {
            if (m_PreparationCardOwned == false)
                m_PreparationPage?.ReportUnownedCardAttempt(m_PreparationCardNumber);
        }

        private void OnPreparationInteract(Interactor requester, Interactor responder)
        {
            if (m_PreparationPage == null ||
                !ReferenceEquals(responder, m_View.PreparationInteractor) ||
                !(requester is UiInteractor uiInteractor) ||
                !(uiInteractor.Wrapper.ExtraInfo is PreparationInteractorData source) ||
                source.Source != EPreparationCardSource.FusionSlot)
                return;
            m_PreparationPage.RemoveFusionMaterial(source.CardNumber, source.SourceSlot);
        }
    }
}
