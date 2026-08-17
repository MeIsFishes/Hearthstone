using System;
using BbxCommon;
using BbxCommon.Ui;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    public sealed class BattleCardItemController : UiControllerBase<BattleCardItemView>, IScrollHandler
    {
        private enum EPreparationBindingMode
        {
            None,
            CardPool,
            BattleSlot,
            FusionSlot,
            FusionRecommendation,
            Collection,
        }

        private const string UnifiedCardFrameArtworkKey =
            "CardFrameRoundedSubtleOpenCornersPreview";
        private const string EmptySlotFallbackArtworkKey =
            "PreparationPoolEmptySlotAgedWood01";
        private const float PreparationPoolScale = 0.8f;
        private const float FusionRecommendationScale = 0.52f;
        private const float StatTransitionDuration = 0.38f;
        private const float StatTransitionIncomingDistance = 18f;
        private const float StatTransitionOutgoingDistance = 24f;
        private const float DamagePopupDuration = 0.75f;
        private const float DamagePopupDistance = 54f;
        private const float KeywordFeedbackDuration = 0.78f;
        private const float KeywordFeedbackDistance = 84f;
        private const float KeywordTooltipHorizontalOffset = 318f;
        private const float KeywordTooltipVerticalOffset = 32f;
        private const float KeywordTooltipMinHeight = 112f;
        private const float KeywordTooltipMaxHeight = 240f;
        private const float KeywordTooltipVerticalPadding = 38f;
        private const float InactiveFeedbackElapsed = -1f;

        public static readonly Color BronzeFrameColor = new Color32(184, 115, 51, 255);
        public static readonly Color SilverFrameColor = new Color32(192, 204, 216, 255);
        public static readonly Color GoldFrameColor = new Color32(231, 169, 59, 255);
        public static readonly Color LegendaryFrameColor = new Color32(178, 92, 255, 255);
        public static readonly Color HoverFrameColor = new Color32(255, 210, 48, 255);
        public static readonly Color LockedFrameColor = new Color32(82, 82, 88, 255);
        public static readonly Color DefaultStatTextColor = Color.white;
        public static readonly Color LowerStatTextColor = new Color32(255, 92, 92, 255);
        public static readonly Color HigherStatTextColor = new Color32(88, 176, 255, 255);

        private static readonly Color AttackerHighlightColor = new Color32(255, 184, 26, 199);
        private static readonly Color TargetHighlightColor = new Color32(255, 41, 20, 209);
        private static readonly Color HitFlashColor = new Color32(255, 52, 36, 255);
        private static readonly string[] EmptySlotArtworkKeys =
        {
            "PreparationPoolEmptySlotAgedWood01",
            "PreparationPoolEmptySlotAgedWood02",
            "PreparationPoolEmptySlotAgedWood03",
            "PreparationPoolEmptySlotAgedWood04",
            "PreparationPoolEmptySlotAgedWood05",
        };
        private static Sprite[] s_EmptySlotSprites;

        private Entity m_BoundEntity;
        private BattleCardRawComponent m_Card;
        private BattleSessionSingletonRawComponent m_Session;
        private PreparationController m_PreparationPage;
        private EPreparationBindingMode m_PreparationBindingMode;
        private int m_PreparationCardNumber;
        private int m_PreparationDisplayNumber;
        private int m_PreparationCopyIndex;
        private int m_PreparationSlot = -1;
        private bool m_PreparationCardOwned;
        private bool m_PreparationCardLocked;
        private RectTransform m_DetachedPreparationSlotBackdrop;
        private bool m_RestorePreparationSlotBackdropAfterDrag;
        private Action<int, RectTransform> m_CollectionClick;
        private Action<PointerEventData> m_CollectionScroll;
        private EBattleKeyword m_DisplayKeywords;
        private Color m_DefaultFrameColor = BronzeFrameColor;
        private bool m_IsHovered;
        private ListenableItemListener m_HealthListener;
        private ListenableItemListener m_AttackListener;
        private ListenableItemListener m_AliveListener;
        private ListenableItemListener m_AttackerListener;
        private ListenableItemListener m_TargetListener;
        private ListenableItemListener m_AttackPresentationListener;
        private RawImage m_AttackEffect;
        private BattleCardTypeCsvData m_AttackPresentationConfig;
        private int m_ActivePresentationSequence;
        private bool m_HasAnimationOrigin;
        private Vector2 m_AnimationOrigin;
        private Color m_ArtworkBaseColor = Color.white;
        private int m_LastAttack;
        private int m_LastHealth;
        private bool m_HasLastAttack;
        private bool m_HasLastHealth;
        private int m_LastKeywordFeedbackSequence;
        private float m_AttackTransitionElapsed = InactiveFeedbackElapsed;
        private float m_HealthTransitionElapsed = InactiveFeedbackElapsed;
        private float m_DamagePopupElapsed = InactiveFeedbackElapsed;
        private float m_ChargeFeedbackElapsed = InactiveFeedbackElapsed;
        private float m_LongShotFeedbackElapsed = InactiveFeedbackElapsed;
        private Vector2 m_AttackTextOrigin;
        private Vector2 m_HealthTextOrigin;
        private Vector2 m_DamagePopupOrigin;
        private Vector2 m_ChargeFeedbackOrigin;
        private Vector2 m_LongShotFeedbackOrigin;
        private Transform m_KeywordTooltipHomeParent;
        private Vector2 m_KeywordTooltipHomePosition;

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
            m_AttackPresentationListener = ModelWrapper.CreateVariableDirtyListener<int>(
                EControllerLifeCycle.Init,
                StartAttackPresentation);
        }

        protected override void OnUiInit()
        {
            if (m_View.PreparationDragable != null)
            {
                m_View.PreparationDragable.Wrapper.OnBeginDrag += OnPreparationDragStarted;
                m_View.PreparationDragable.Wrapper.OnBackFromTop += OnPreparationDragReturned;
            }
            if (m_View.PreparationInteractor != null)
            {
                m_View.PreparationInteractor.Wrapper.OnInteractorTouch += OnPreparationInteractorTouch;
                m_View.PreparationInteractor.Wrapper.OnInteractorTouchEnd += OnPreparationInteractorTouchEnd;
                m_View.PreparationInteractor.Wrapper.OnInteract += OnPreparationInteract;
            }
            if (m_View.CardHoverListener != null)
            {
                m_View.CardHoverListener.AddCallback(EUiEvent.PointerEnter, OnCardPointerEnter);
                m_View.CardHoverListener.AddCallback(EUiEvent.PointerExit, OnCardPointerExit);
            }
            if (m_View.CardClickListener != null)
                m_View.CardClickListener.AddCallback(EUiEvent.PointerClick, OnCardPointerClicked);
            InitializePreparationEmptySlotInteraction();
            CreateAttackEffectOverlay();
            CacheFeedbackLayout();
            CacheKeywordTooltipLayout();
            ResetFeedbackAnimations(true);
            ApplyPreparationState(false, false);
        }

        private void InitializePreparationEmptySlotInteraction()
        {
            if (m_View.PreparationEmptyAttemptListener == null)
                return;
            m_View.PreparationEmptyAttemptListener.enabled = false;
            var emptyInput = m_View.PreparationEmptyAttemptListener.GetComponent<Graphic>();
            if (emptyInput != null)
                emptyInput.raycastTarget = false;
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
                m_AttackPresentationListener.RebindTarget(m_Session.AttackPresentationSequence);
            }
            RefreshAll();
        }

        protected override void OnUiUpdate(float deltaTime)
        {
            if (m_RestorePreparationSlotBackdropAfterDrag)
                RestorePreparationSlotBackdrop();
            UpdateFeedbackAnimations(deltaTime * BattleRules.AttackPresentationPlaybackSpeed);
            if (m_Session == null ||
                m_Session.AttackPresentationActive == false ||
                m_ActivePresentationSequence != m_Session.AttackPresentationSequence.Value)
            {
                ResetAttackPresentationVisuals();
                return;
            }

            var elapsed = m_Session.AttackPresentationElapsed;
            if (m_Session.CurrentAttacker.Value == m_BoundEntity)
                UpdateAttackLunge(elapsed);
            if (m_Session.CurrentTarget.Value == m_BoundEntity)
                UpdateHitPresentation(elapsed);
        }

        internal void BindPreparation(
            PreparationController page,
            int cardNumber,
            int displayNumber,
            int copyIndex = 0)
        {
            ResetBinding();
            m_PreparationPage = page;
            m_PreparationBindingMode = EPreparationBindingMode.CardPool;
            m_PreparationCardNumber = cardNumber;
            m_PreparationDisplayNumber = displayNumber;
            m_PreparationCopyIndex = copyIndex;
            transform.localScale = Vector3.one * PreparationPoolScale;
            ApplyEmptySlotVariant(
                m_View.PreparationEmptyState,
                ResolveEmptySlotVariantIndex((int)EPreparationBindingMode.CardPool, displayNumber, copyIndex));
            UpdatePreparationInteractorData(cardNumber);
        }

        internal void BindPreparationBattleSlot(
            PreparationController page,
            int slot,
            Vector2 slotSize)
        {
            BindPreparationSlot(page, slot, EPreparationBindingMode.BattleSlot, slotSize);
        }

        internal void BindPreparationFusionSlot(
            PreparationController page,
            int slot,
            Vector2 slotSize)
        {
            BindPreparationSlot(page, slot, EPreparationBindingMode.FusionSlot, slotSize);
        }

        internal void BindFusionReveal(
            RunStateSingletonRawComponent runState,
            int cardNumber,
            bool showNewCollectionNotice)
        {
            ResetBinding();
            if (runState == null || runState.HasCard(cardNumber) == false)
                return;

            m_PreparationCardNumber = cardNumber;
            if (m_View.CardNumberText != null)
                m_View.CardNumberText.text = cardNumber.ToString();
            if (m_View.CardNumberBadge != null)
                m_View.CardNumberBadge.gameObject.SetActive(true);
            ShowPreparationCard(runState, cardNumber);
            SetNewCollectionNoticeVisible(showNewCollectionNotice);
            ApplyInteractionPermissions(false, false, false, false);
        }

        internal void BindFusionMaterialReveal(FusionMaterialSnapshot material)
        {
            ResetBinding();
            var cardConfig = DataApi.GetData<BattleCardCsvData>(material.CardNumber);
            var typeConfig = cardConfig == null
                ? null
                : DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
            if (cardConfig == null || typeConfig == null)
            {
                DebugApi.LogError($"Fusion material card configuration {material.CardNumber} is missing.");
                return;
            }

            m_PreparationCardNumber = material.CardNumber;
            if (m_View.CardNumberText != null)
                m_View.CardNumberText.text = material.CardNumber.ToString("00");
            if (m_View.CardNumberBadge != null)
                m_View.CardNumberBadge.gameObject.SetActive(true);
            ShowCardPresentation(GetTierFrameColor(typeConfig.Tier));
            ApplyCardContent(
                cardConfig,
                typeConfig,
                material.Keywords,
                material.Attack,
                material.MaxHealth);
            RefreshAlive(true);
            RefreshHighlights(Entity.Null);
            ApplyInteractionPermissions(false, false, false, false);
        }

        internal void BindPreparationRewardReveal(RunCardInstanceData reward)
        {
            BindPreparationRewardReveal(reward, false);
        }

        internal void BindPreparationRewardReveal(
            RunCardInstanceData reward,
            bool showNewCollectionNotice)
        {
            ResetBinding();
            if (reward.IsValid == false)
                return;

            var cardConfig = DataApi.GetData<BattleCardCsvData>(reward.PresentationCardNumber);
            var typeConfig = cardConfig == null
                ? null
                : DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
            if (cardConfig == null || typeConfig == null)
            {
                DebugApi.LogError(
                    $"Preparation reward card configuration {reward.PresentationCardNumber} is missing.");
                return;
            }

            m_PreparationCardNumber = reward.CardNumber;
            if (m_View.CardNumberText != null)
                m_View.CardNumberText.text = reward.CardNumber.ToString("00");
            if (m_View.CardNumberBadge != null)
                m_View.CardNumberBadge.gameObject.SetActive(true);
            ShowCardPresentation(GetTierFrameColor(reward.Tier));
            ApplyCardContent(
                cardConfig,
                typeConfig,
                reward.Keywords,
                reward.Attack,
                reward.MaxHealth);
            RefreshAlive(true);
            RefreshHighlights(Entity.Null);
            SetNewCollectionNoticeVisible(showNewCollectionNotice);
            ApplyInteractionPermissions(false, false, false, false);
        }

        internal void BindEnemyPreview(RunCardInstanceData instance, Vector2 slotSize)
        {
            ResetBinding();
            if (instance.IsValid == false)
                return;

            var cardConfig = DataApi.GetData<BattleCardCsvData>(instance.PresentationCardNumber);
            var typeConfig = cardConfig == null
                ? null
                : DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
            if (cardConfig == null || typeConfig == null)
            {
                DebugApi.LogError(
                    $"Enemy preview card configuration {instance.PresentationCardNumber} is missing.");
                return;
            }

            m_PreparationCardNumber = instance.CardNumber;
            ApplyContainedSlotScale(slotSize);
            if (m_View.CardNumberText != null)
                m_View.CardNumberText.text = instance.CardNumber.ToString("00");
            if (m_View.CardNumberBadge != null)
                m_View.CardNumberBadge.gameObject.SetActive(true);
            ShowCardPresentation(GetTierFrameColor(instance.Tier));
            ApplyCardContent(
                cardConfig,
                typeConfig,
                instance.Keywords,
                instance.Attack,
                instance.MaxHealth);
            RefreshAlive(true);
            RefreshHighlights(Entity.Null);
            ApplyInteractionPermissions(true, false, false, false);
        }

        internal void SetFusionRevealInteraction(bool enabled)
        {
            ApplyInteractionPermissions(enabled, false, false, false);
        }

        internal void BindCollection(
            int cardNumber,
            bool unlocked,
            Action<int, RectTransform> onClick,
            Action<PointerEventData> onScroll)
        {
            ResetBinding();
            m_PreparationBindingMode = EPreparationBindingMode.Collection;
            m_PreparationCardNumber = cardNumber;
            m_PreparationCardLocked = unlocked == false;
            m_CollectionClick = onClick;
            m_CollectionScroll = onScroll;
            if (unlocked == false)
            {
                ShowCollectionEmptySlot(cardNumber);
                ShowCollectionRecipeTooltip(cardNumber);
            }
            else
            {
                ShowCollectionCard(cardNumber);
            }

            if (m_View.PreparationInteractor != null)
                m_View.PreparationInteractor.enabled = false;
            ApplyInteractionPermissions(true, unlocked, false, true);
            if (m_View.CardBackground != null)
                m_View.CardBackground.color = Color.clear;
        }

        private void ShowCollectionRecipeTooltip(int cardNumber)
        {
            m_DisplayKeywords = EBattleKeyword.None;
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
            if (m_View.KeywordTooltipText != null)
                m_View.KeywordTooltipText.text = FormatCollectionRecipe(cardNumber);
            HideKeywordTooltip();
        }

        private static string FormatCollectionRecipe(int cardNumber)
        {
            var cardConfig = DataApi.GetData<BattleCardCsvData>(cardNumber);
            if (cardConfig == null)
                return string.Empty;
            if (cardConfig.IsFusionResult == false)
                return "合成配方：无（基础卡牌）";

            var recipe = "合成配方：";
            for (var index = 0; index < cardConfig.FusionRecipeTypeIds.Count; index++)
            {
                if (index > 0)
                    recipe += " + ";
                var typeId = cardConfig.FusionRecipeTypeIds[index];
                var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(typeId);
                recipe += typeConfig == null ? $"类型 {typeId}" : typeConfig.DisplayName;
            }
            return recipe;
        }

        private void ShowCollectionCard(int cardNumber)
        {
            var cardConfig = DataApi.GetData<BattleCardCsvData>(cardNumber);
            var typeConfig = cardConfig == null
                ? null
                : DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
            if (cardConfig == null || typeConfig == null)
            {
                DebugApi.LogError($"Collection battle card configuration {cardNumber} is missing.");
                HideCardPresentation(false);
                return;
            }

            if (m_View.CardNumberText != null)
                m_View.CardNumberText.text = cardNumber.ToString("00");
            if (m_View.CardNumberBadge != null)
                m_View.CardNumberBadge.gameObject.SetActive(true);
            var keywords = typeConfig.InitialKeyword;
            var attack = typeConfig.MinAttack;
            var health = typeConfig.MinHealth;
            var tier = typeConfig.Tier;
            if (cardConfig.IsFusionResult)
            {
                var simulated = BattleCardSimulationFactory.CreateDeterministic(cardNumber);
                keywords = simulated.Keywords;
                attack = simulated.Attack;
                health = simulated.MaxHealth;
                tier = simulated.Tier;
            }

            ShowCardPresentation(GetTierFrameColor(tier));
            ApplyCardContent(
                cardConfig,
                typeConfig,
                keywords,
                attack,
                health);
            RefreshAlive(true);
            RefreshHighlights(Entity.Null);
        }

        internal void BindFusionRecommendation(
            PreparationController page,
            RunStateSingletonRawComponent runState,
            int cardNumber,
            bool selectedAsMaterial)
        {
            ResetBinding();
            if (page == null || runState == null || runState.HasCard(cardNumber) == false)
                return;

            m_PreparationPage = page;
            m_PreparationBindingMode = EPreparationBindingMode.FusionRecommendation;
            m_PreparationCardNumber = cardNumber;
            m_PreparationDisplayNumber = cardNumber;
            m_PreparationCardOwned = true;
            transform.localScale = Vector3.one * FusionRecommendationScale;
            if (m_View.CardNumberText != null)
                m_View.CardNumberText.text = cardNumber.ToString("00");
            if (m_View.CardNumberBadge != null)
                m_View.CardNumberBadge.gameObject.SetActive(true);
            ShowPreparationCard(runState, cardNumber);
            ApplyFusionRecommendationState(selectedAsMaterial);
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (m_PreparationBindingMode == EPreparationBindingMode.CardPool)
                m_PreparationPage?.ForwardCardPoolScroll(eventData);
            else if (m_PreparationBindingMode == EPreparationBindingMode.FusionRecommendation)
                m_PreparationPage?.ForwardFusionRecommendationScroll(eventData);
            else if (m_PreparationBindingMode == EPreparationBindingMode.Collection)
                m_CollectionScroll?.Invoke(eventData);
        }

        internal void RefreshPreparation(
            RunStateSingletonRawComponent runState,
            PreparationSessionSingletonRawComponent session,
            bool fusionTabSelected)
        {
            if (m_PreparationPage == null || runState == null)
                return;

            if (m_PreparationBindingMode == EPreparationBindingMode.BattleSlot)
            {
                RefreshPreparationSlot(runState, runState.BattleSlotCardNumbers[m_PreparationSlot]);
                return;
            }
            if (m_PreparationBindingMode == EPreparationBindingMode.FusionSlot)
            {
                var cardNumber = session == null ? 0 : session.FusionSlotCardNumbers[m_PreparationSlot];
                RefreshPreparationSlot(runState, cardNumber);
                return;
            }
            if (m_PreparationBindingMode != EPreparationBindingMode.CardPool)
                return;

            m_PreparationCardLocked = m_PreparationCardNumber == RunCardRules.LockedCardNumber;
            if (m_PreparationCardLocked)
            {
                m_PreparationCardOwned = false;
                UpdatePreparationInteractorData(0);
                if (m_View.CardNumberText != null)
                    m_View.CardNumberText.text = m_PreparationDisplayNumber.ToString("00");
                if (m_View.CardNumberBadge != null)
                    m_View.CardNumberBadge.gameObject.SetActive(true);
                ShowLockedPreparationCard();
                ApplyPreparationState(false, false);
                return;
            }

            m_PreparationCardOwned =
                m_PreparationCopyIndex < runState.GetCardCopyCount(m_PreparationCardNumber);
            var selectedAsMaterial = false;
            var deployed = false;
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
            for (var slot = 0; slot < runState.BattleSlotCardNumbers.Length; slot++)
            {
                if (runState.BattleSlotCardNumbers[slot] != m_PreparationCardNumber)
                    continue;
                deployed = true;
                break;
            }

            if (m_View.CardNumberText != null)
                m_View.CardNumberText.text = m_PreparationDisplayNumber.ToString("00");
            if (m_View.CardNumberBadge != null)
                m_View.CardNumberBadge.gameObject.SetActive(true);
            if (m_PreparationCardOwned == false)
            {
                HideCardPresentation(false);
                ApplyPreparationState(false, false);
                return;
            }

            if (fusionTabSelected && selectedAsMaterial)
            {
                HideCardPresentation(false);
                ApplyPreparationState(false, false);
                return;
            }

            ShowPreparationCard(runState, m_PreparationCardNumber, m_PreparationCopyIndex);
            ApplyPreparationState(true, deployed);
        }

        private void BindPreparationSlot(
            PreparationController page,
            int slot,
            EPreparationBindingMode bindingMode,
            Vector2 slotSize)
        {
            ResetBinding();
            m_PreparationPage = page;
            m_PreparationBindingMode = bindingMode;
            m_PreparationSlot = slot;
            ApplyExactSlotScale(slotSize);
            var emptyState = bindingMode == EPreparationBindingMode.BattleSlot
                ? m_View.PreparationBattleSlotEmptyState
                : m_View.PreparationFusionSlotEmptyState;
            ApplyEmptySlotVariant(
                emptyState,
                ResolveEmptySlotVariantIndex((int)bindingMode, slot, 0));
            UpdatePreparationInteractorData(0);
        }

        private void ShowCollectionEmptySlot(int cardNumber)
        {
            HideCardPresentation(true);
            ApplyEmptySlotVariant(
                m_View.PreparationEmptyState,
                ResolveEmptySlotVariantIndex((int)EPreparationBindingMode.Collection, cardNumber, 0));
            if (m_View.PreparationEmptyState != null)
                m_View.PreparationEmptyState.SetActive(true);
            if (m_View.CollectionLockedOverlay != null)
                m_View.CollectionLockedOverlay.gameObject.SetActive(true);
        }

        private static void ApplyEmptySlotVariant(GameObject emptyState, int variantIndex)
        {
            if (emptyState == null)
                return;

            var image = emptyState.GetComponent<Image>();
            if (image == null)
                return;

            EnsureEmptySlotSpritesLoaded();
            var sprite = s_EmptySlotSprites[variantIndex];
            if (sprite == null)
                sprite = ResourceApi.LoadSprite(EmptySlotFallbackArtworkKey);
            if (sprite != null)
                image.sprite = sprite;
            image.color = Color.white;
        }

        private static void EnsureEmptySlotSpritesLoaded()
        {
            if (s_EmptySlotSprites != null)
                return;

            s_EmptySlotSprites = new Sprite[EmptySlotArtworkKeys.Length];
            for (var index = 0; index < EmptySlotArtworkKeys.Length; index++)
                s_EmptySlotSprites[index] = ResourceApi.LoadSprite(EmptySlotArtworkKeys[index]);
        }

        private static int ResolveEmptySlotVariantIndex(int context, int primary, int secondary)
        {
            unchecked
            {
                var hash = 2166136261u;
                hash = (hash ^ (uint)context) * 16777619u;
                hash = (hash ^ (uint)primary) * 16777619u;
                hash = (hash ^ (uint)secondary) * 16777619u;
                hash ^= hash >> 16;
                hash *= 2246822519u;
                hash ^= hash >> 13;
                return (int)(hash % (uint)EmptySlotArtworkKeys.Length);
            }
        }

        private void ApplyExactSlotScale(Vector2 slotSize)
        {
            // UiApi creates a zero-sized controller wrapper and parents the prefab view below it.
            // Read the actual card view dimensions, then scale the wrapper so every visual and
            // interaction child (empty slot or occupied card) shares the exact same outline.
            var cardRect = m_View == null ? null : m_View.transform as RectTransform;
            if (cardRect == null ||
                cardRect.rect.width <= 0f ||
                cardRect.rect.height <= 0f ||
                slotSize.x <= 0f ||
                slotSize.y <= 0f)
            {
                transform.localScale = Vector3.one;
                return;
            }

            var scaleX = Mathf.Min(1f, slotSize.x / cardRect.rect.width);
            var scaleY = Mathf.Min(1f, slotSize.y / cardRect.rect.height);
            transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        private void ApplyContainedSlotScale(Vector2 slotSize)
        {
            // UiList slots size the controller wrapper, but the shared card view remains 250 × 360.
            // Scale from the card root so preview cards match their requested final visual size.
            var cardRect = m_View == null ? null : m_View.transform as RectTransform;
            if (cardRect == null ||
                cardRect.rect.width <= 0f ||
                cardRect.rect.height <= 0f ||
                slotSize.x <= 0f ||
                slotSize.y <= 0f)
            {
                transform.localScale = Vector3.one;
                return;
            }

            var scale = Mathf.Min(
                slotSize.x / cardRect.rect.width,
                slotSize.y / cardRect.rect.height);
            transform.localScale = Vector3.one * Mathf.Min(1f, scale);
        }

        private void RefreshPreparationSlot(RunStateSingletonRawComponent runState, int cardNumber)
        {
            var cardReplaced = m_PreparationCardNumber != 0 &&
                               cardNumber != 0 &&
                               m_PreparationCardNumber != cardNumber;
            if (cardReplaced)
                ResetFeedbackAnimations(true);
            m_PreparationCardNumber = cardNumber;
            m_PreparationCardOwned = cardNumber != 0 && runState.HasCard(cardNumber);
            UpdatePreparationInteractorData(m_PreparationCardOwned ? cardNumber : 0);
            if (m_PreparationCardOwned == false)
            {
                HideCardPresentation(true);
                ApplyPreparationState(false, false);
                return;
            }

            if (m_View.CardNumberText != null)
                m_View.CardNumberText.text = cardNumber.ToString("00");
            if (m_View.CardNumberBadge != null)
                m_View.CardNumberBadge.gameObject.SetActive(true);
            ShowPreparationCard(runState, cardNumber);
            ApplyPreparationState(true, false);
        }

        private void ShowPreparationCard(
            RunStateSingletonRawComponent runState,
            int cardNumber,
            int copyIndex = 0)
        {
            var instance = runState.GetCardInstance(cardNumber, copyIndex);
            var cardConfig = DataApi.GetData<BattleCardCsvData>(instance.PresentationCardNumber);
            if (cardConfig == null)
            {
                DebugApi.LogError(
                    $"Battle card presentation configuration {instance.PresentationCardNumber} is missing.");
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

            ShowCardPresentation(GetTierFrameColor(instance.Tier));
            ApplyCardContent(
                cardConfig,
                typeConfig,
                instance.Keywords,
                instance.Attack,
                instance.MaxHealth);
            RefreshAlive(true);
            RefreshHighlights(Entity.Null);
        }

        private void ShowLockedPreparationCard()
        {
            var cardConfig = DataApi.GetData<BattleCardCsvData>(RunCardRules.LockedCardNumber);
            var typeConfig = cardConfig == null
                ? null
                : DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
            if (cardConfig == null || typeConfig == null)
            {
                DebugApi.LogError("Locked battle card configuration 99 is missing.");
                HideCardPresentation(false);
                return;
            }

            ShowCardPresentation(LockedFrameColor);
            ApplyCardContent(cardConfig, typeConfig, EBattleKeyword.None, 0, 0);
            if (m_View.AttackText != null)
                m_View.AttackText.transform.parent.gameObject.SetActive(false);
            if (m_View.HealthText != null)
                m_View.HealthText.transform.parent.gameObject.SetActive(false);
            RefreshAlive(true);
            RefreshHighlights(Entity.Null);
        }

        private void UpdatePreparationInteractorData(int cardNumber)
        {
            if (m_View.PreparationInteractor == null)
                return;

            var source = EPreparationCardSource.CardPool;
            if (m_PreparationBindingMode == EPreparationBindingMode.BattleSlot)
                source = EPreparationCardSource.BattleSlot;
            else if (m_PreparationBindingMode == EPreparationBindingMode.FusionSlot)
                source = EPreparationCardSource.FusionSlot;

            m_View.PreparationInteractor.Wrapper.ExtraInfo = new PreparationInteractorData
            {
                CardNumber = cardNumber,
                Source = source,
                SourceSlot = m_PreparationSlot,
                TargetSlot = m_PreparationSlot,
            };
        }

        protected override void OnUiClose()
        {
            ResetBinding();
        }

        private void ResetBinding()
        {
            RestorePreparationSlotBackdrop();
            m_HealthListener.RebindTarget(null);
            m_AttackListener.RebindTarget(null);
            m_AliveListener.RebindTarget(null);
            m_AttackerListener.RebindTarget(null);
            m_TargetListener.RebindTarget(null);
            m_AttackPresentationListener.RebindTarget(null);
            ResetAttackPresentationVisuals();
            m_BoundEntity = Entity.Null;
            m_Card = null;
            m_Session = null;
            m_PreparationPage = null;
            m_PreparationBindingMode = EPreparationBindingMode.None;
            m_PreparationCardNumber = 0;
            m_PreparationDisplayNumber = 0;
            m_PreparationCopyIndex = 0;
            m_PreparationSlot = -1;
            m_PreparationCardOwned = false;
            m_PreparationCardLocked = false;
            m_CollectionClick = null;
            m_CollectionScroll = null;
            m_DisplayKeywords = EBattleKeyword.None;
            m_DefaultFrameColor = BronzeFrameColor;
            m_IsHovered = false;

            if (m_View != null)
            {
                transform.localScale = Vector3.one;
                m_View.transform.localRotation = Quaternion.identity;
                HideCardPresentation(true);
                ApplyPreparationState(false, false);
                SetNewCollectionNoticeVisible(false);
                ApplyFrameColors();
                if (m_View.PreparationInteractor != null)
                    m_View.PreparationInteractor.Wrapper.ExtraInfo = null;
            }
        }

        private void SetNewCollectionNoticeVisible(bool visible)
        {
            if (m_View.NewCollectionNotice != null)
                m_View.NewCollectionNotice.gameObject.SetActive(visible);
        }

        private void RefreshAll()
        {
            if (m_Card == null)
                return;

            ApplyPreparationState(false, false);
            RefreshCardNumber();
            var cardConfig = DataApi.GetData<BattleCardCsvData>(m_Card.PresentationCardNumber);
            var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(m_Card.PresentationCardTypeId);
            ShowCardPresentation(GetTierFrameColor(m_Card.Tier));
            if (cardConfig == null)
                DebugApi.LogError(
                    $"Battle card presentation configuration {m_Card.PresentationCardNumber} is missing.");
            else if (typeConfig == null)
                DebugApi.LogError($"Battle card presentation type {m_Card.PresentationCardTypeId} is missing.");
            else
                ApplyCardContent(cardConfig, typeConfig, m_Card.Keywords, m_Card.Attack, m_Card.CurrentHealth.Value);
            RefreshHealth(m_Card.CurrentHealth.Value);
            RefreshAlive(m_Card.IsAlive.Value);
            RefreshHighlights(Entity.Null);
        }

        private void RefreshHealth(int health)
        {
            if (m_HasLastHealth && health < m_LastHealth && m_Card != null)
                StartDamagePopup(m_LastHealth - health);
            var color = m_Card == null
                ? DefaultStatTextColor
                : GetStatTextColor(health, m_Card.EntryHealth);
            RefreshStatValue(
                m_View.HealthText,
                m_View.HealthValueOutgoingText,
                health,
                color,
                ref m_LastHealth,
                ref m_HasLastHealth,
                ref m_HealthTransitionElapsed,
                m_HealthTextOrigin);
        }

        private void RefreshAttack(int attack)
        {
            var color = m_Card == null
                ? DefaultStatTextColor
                : GetStatTextColor(attack, m_Card.EntryAttack);
            RefreshStatValue(
                m_View.AttackText,
                m_View.AttackValueOutgoingText,
                attack,
                color,
                ref m_LastAttack,
                ref m_HasLastAttack,
                ref m_AttackTransitionElapsed,
                m_AttackTextOrigin);
        }

        private static void RefreshStatValue(
            TMP_Text currentText,
            TMP_Text outgoingText,
            int value,
            Color color,
            ref int lastValue,
            ref bool hasLastValue,
            ref float transitionElapsed,
            Vector2 origin)
        {
            if (currentText == null)
            {
                lastValue = value;
                hasLastValue = true;
                return;
            }

            if (hasLastValue == false)
            {
                SetStatValueImmediate(currentText, outgoingText, value, color, ref transitionElapsed, origin);
            }
            else if (value > lastValue && outgoingText != null)
            {
                outgoingText.text = lastValue.ToString();
                outgoingText.color = WithAlpha(currentText.color, 1f);
                outgoingText.rectTransform.anchoredPosition = origin;
                outgoingText.gameObject.SetActive(true);
                currentText.text = value.ToString();
                currentText.color = WithAlpha(color, 0f);
                currentText.rectTransform.anchoredPosition =
                    origin + Vector2.down * StatTransitionIncomingDistance;
                transitionElapsed = 0f;
            }
            else if (value < lastValue)
            {
                SetStatValueImmediate(currentText, outgoingText, value, color, ref transitionElapsed, origin);
            }
            else
            {
                currentText.text = value.ToString();
                currentText.color = WithAlpha(color, currentText.color.a);
            }

            lastValue = value;
            hasLastValue = true;
        }

        private static void SetStatValueImmediate(
            TMP_Text currentText,
            TMP_Text outgoingText,
            int value,
            Color color,
            ref float transitionElapsed,
            Vector2 origin)
        {
            currentText.text = value.ToString();
            currentText.color = WithAlpha(color, 1f);
            currentText.rectTransform.anchoredPosition = origin;
            if (outgoingText != null)
                outgoingText.gameObject.SetActive(false);
            transitionElapsed = InactiveFeedbackElapsed;
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
            m_DisplayKeywords = BattleKeywordRules.Normalize(keywords);
            if (m_View.KeywordText != null)
            {
                m_View.KeywordText.text = keywordText;
                m_View.KeywordText.gameObject.SetActive(string.IsNullOrEmpty(m_View.KeywordText.text) == false);
            }
            if (m_View.KeywordTooltipText != null)
                m_View.KeywordTooltipText.text = BattleKeywordRules.FormatDescriptionText(m_DisplayKeywords);
            if (m_IsHovered)
                ShowKeywordTooltip();
            else
                HideKeywordTooltip();
            if (m_View.TauntShieldOutline != null)
            {
                m_View.TauntShieldOutline.gameObject.SetActive(
                    BattleKeywordRules.Has(keywords, EBattleKeyword.Taunt));
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
                m_View.ArtworkArea.color = Color.white;
                m_View.ArtworkArea.preserveAspect = false;
                m_View.ArtworkArea.gameObject.SetActive(m_View.ArtworkArea.sprite != null);
                if (m_View.ArtworkArea.sprite == null)
                    DebugApi.LogError($"Battle card artwork '{cardConfig.ArtworkKey}' is missing.");
            }
            RefreshAttack(attack);
            RefreshHealth(health);
        }

        private void ShowCardPresentation(Color defaultFrameColor)
        {
            HidePreparationEmptyStates();
            SetCardBaseAreasVisible(true);
            if (m_View.CardBackground != null)
                m_View.CardBackground.color = Color.clear;
            if (m_View.CardFrame != null)
            {
                m_View.CardFrame.gameObject.SetActive(true);
                var frameSprite = ResourceApi.LoadSprite(UnifiedCardFrameArtworkKey);
                m_View.CardFrame.sprite = frameSprite;
                if (m_View.AttackerHighlight != null)
                    m_View.AttackerHighlight.sprite = frameSprite;
                if (m_View.TargetHighlight != null)
                    m_View.TargetHighlight.sprite = frameSprite;
                if (frameSprite == null)
                    DebugApi.LogError($"Battle card frame artwork '{UnifiedCardFrameArtworkKey}' is missing.");
            }
            m_DefaultFrameColor = defaultFrameColor;
            ApplyFrameColors();
            if (m_View.AttackText != null)
                m_View.AttackText.transform.parent.gameObject.SetActive(true);
            if (m_View.HealthText != null)
                m_View.HealthText.transform.parent.gameObject.SetActive(true);
            m_View.transform.localRotation = Quaternion.identity;
        }

        private void SetCardBaseAreasVisible(bool visible)
        {
            if (m_View.ArtworkArea != null && m_View.ArtworkArea.transform.parent != null)
                m_View.ArtworkArea.transform.parent.gameObject.SetActive(visible);
            if (m_View.SkillDescriptionText != null && m_View.SkillDescriptionText.transform.parent != null)
                m_View.SkillDescriptionText.transform.parent.gameObject.SetActive(visible);
        }

        private void HidePreparationEmptyStates()
        {
            if (m_View.CollectionLockedOverlay != null)
                m_View.CollectionLockedOverlay.gameObject.SetActive(false);
            if (m_View.PreparationEmptyState != null)
                m_View.PreparationEmptyState.SetActive(false);
            if (m_View.PreparationBattleSlotEmptyState != null)
                m_View.PreparationBattleSlotEmptyState.SetActive(false);
            if (m_View.PreparationFusionSlotEmptyState != null)
                m_View.PreparationFusionSlotEmptyState.SetActive(false);
        }

        private void HideCardPresentation(bool hideCardNumber)
        {
            ResetFeedbackAnimations(true);
            m_DisplayKeywords = EBattleKeyword.None;
            HideKeywordTooltip();
            SetCardBaseAreasVisible(false);
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
            if (m_View.KeywordTooltipText != null)
                m_View.KeywordTooltipText.text = string.Empty;
            if (m_View.ArtworkArea != null)
            {
                m_View.ArtworkArea.sprite = null;
                m_View.ArtworkArea.color = Color.white;
                m_View.ArtworkArea.gameObject.SetActive(false);
            }
            if (m_AttackEffect != null)
                m_AttackEffect.gameObject.SetActive(false);
            if (m_View.CardFrame != null)
                m_View.CardFrame.gameObject.SetActive(false);
            if (m_View.TauntShieldOutline != null)
                m_View.TauntShieldOutline.gameObject.SetActive(false);
            if (m_View.AttackText != null)
            {
                m_View.AttackText.text = string.Empty;
                m_View.AttackText.color = DefaultStatTextColor;
                m_View.AttackText.transform.parent.gameObject.SetActive(false);
            }
            if (m_View.HealthText != null)
            {
                m_View.HealthText.text = string.Empty;
                m_View.HealthText.color = DefaultStatTextColor;
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

        private void ApplyPreparationState(bool occupied, bool deployed)
        {
            var preparationMode = m_PreparationBindingMode != EPreparationBindingMode.None;
            var poolMode = m_PreparationBindingMode == EPreparationBindingMode.CardPool;
            var poolEmpty = poolMode && occupied == false && m_PreparationCardLocked == false;
            HidePreparationEmptyStates();
            if (m_View.PreparationEmptyState != null)
                m_View.PreparationEmptyState.SetActive(poolEmpty);
            SetPreparationSlotBackdropVisible(
                m_View.PreparationBattleSlotEmptyState,
                m_PreparationBindingMode == EPreparationBindingMode.BattleSlot);
            SetPreparationSlotBackdropVisible(
                m_View.PreparationFusionSlotEmptyState,
                m_PreparationBindingMode == EPreparationBindingMode.FusionSlot);
            if (m_View.PreparationMaterialSelectedState != null)
                m_View.PreparationMaterialSelectedState.SetActive(false);
            if (m_View.PreparationDeployedState != null)
                m_View.PreparationDeployedState.SetActive(poolMode && occupied && deployed);
            if (m_View.PreparationDropHighlight != null)
                m_View.PreparationDropHighlight.gameObject.SetActive(false);
            if (m_View.PreparationInteractor != null)
                m_View.PreparationInteractor.enabled = preparationMode && m_PreparationCardLocked == false;
            var dragEnabled = preparationMode && occupied && m_PreparationCardLocked == false;
            var hoverEnabled = preparationMode
                ? occupied || m_PreparationCardLocked
                : m_Card != null;
            var dropTargetEnabled =
                m_PreparationBindingMode == EPreparationBindingMode.BattleSlot ||
                m_PreparationBindingMode == EPreparationBindingMode.FusionSlot;
            ApplyInteractionPermissions(
                hoverEnabled,
                false,
                dragEnabled,
                poolMode,
                dropTargetEnabled);
            if (m_View.CardBackground != null)
                m_View.CardBackground.color = Color.clear;
        }

        private void SetPreparationSlotBackdropVisible(GameObject backdrop, bool visible)
        {
            if (backdrop == null)
                return;

            backdrop.SetActive(visible);
            if (visible && backdrop.transform.parent == m_View.transform)
                backdrop.transform.SetAsFirstSibling();
        }

        private void ApplyFusionRecommendationState(bool selectedAsMaterial)
        {
            HidePreparationEmptyStates();
            if (m_View.PreparationMaterialSelectedState != null)
                m_View.PreparationMaterialSelectedState.SetActive(selectedAsMaterial);
            if (m_View.PreparationDeployedState != null)
                m_View.PreparationDeployedState.SetActive(false);
            if (m_View.PreparationDropHighlight != null)
                m_View.PreparationDropHighlight.gameObject.SetActive(false);
            if (m_View.PreparationInteractor != null)
            {
                m_View.PreparationInteractor.enabled = false;
                m_View.PreparationInteractor.Wrapper.ExtraInfo = null;
            }
            ApplyInteractionPermissions(true, false, false, true);
            if (m_View.CardBackground != null)
                m_View.CardBackground.color = Color.clear;
        }

        private void ApplyInteractionPermissions(
            bool hoverEnabled,
            bool clickEnabled,
            bool dragEnabled,
            bool scrollEnabled,
            bool dropTargetEnabled = false)
        {
            if (m_View.CardHoverListener != null)
                m_View.CardHoverListener.enabled = hoverEnabled;
            if (m_View.CardClickListener != null)
                m_View.CardClickListener.enabled = clickEnabled;
            if (m_View.CardDragListener != null)
                m_View.CardDragListener.enabled = dragEnabled;
            if (m_View.PreparationDragable != null)
                m_View.PreparationDragable.enabled = true;
            if (m_View.CardHoverInput != null)
            {
                m_View.CardHoverInput.raycastTarget =
                    hoverEnabled || clickEnabled || dragEnabled || scrollEnabled || dropTargetEnabled;
            }
            if (hoverEnabled || m_IsHovered == false)
                return;

            m_IsHovered = false;
            HideKeywordTooltip();
            ApplyFrameColors();
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
            ApplyFrameColors();
        }

        private void CreateAttackEffectOverlay()
        {
            var existing = m_View.transform.Find("AttackFrameEffect");
            GameObject effectObject;
            if (existing == null)
            {
                effectObject = new GameObject("AttackFrameEffect", typeof(RectTransform), typeof(RawImage));
                effectObject.transform.SetParent(m_View.transform, false);
            }
            else
            {
                effectObject = existing.gameObject;
            }

            var rect = (RectTransform)effectObject.transform;
            rect.anchorMin = new Vector2(0.04f, 0.08f);
            rect.anchorMax = new Vector2(0.96f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();

            m_AttackEffect = effectObject.GetComponent<RawImage>();
            m_AttackEffect.raycastTarget = false;
            m_AttackEffect.color = Color.white;
            m_AttackEffect.gameObject.SetActive(false);
        }

        private void StartAttackPresentation(int sequence)
        {
            ResetAttackPresentationVisuals();
            if (sequence <= 0 || m_Session == null || m_Card == null ||
                m_Session.AttackPresentationActive == false)
                return;

            var attackerEntity = m_Session.CurrentAttacker.Value;
            var attacker = attackerEntity == Entity.Null
                ? null
                : attackerEntity.GetRawComponent<BattleCardRawComponent>();
            if (attacker == null)
                return;

            m_ActivePresentationSequence = sequence;
            m_AttackPresentationConfig =
                DataApi.GetData<BattleCardTypeCsvData>(attacker.PresentationCardTypeId);
            var rect = (RectTransform)transform;
            m_AnimationOrigin = rect.anchoredPosition;
            m_HasAnimationOrigin = true;
            if (m_View.ArtworkArea != null)
                m_ArtworkBaseColor = m_View.ArtworkArea.color;

            if (m_BoundEntity == attackerEntity && m_LastKeywordFeedbackSequence != sequence)
            {
                m_LastKeywordFeedbackSequence = sequence;
                if (BattleKeywordRules.Has(attacker.Keywords, EBattleKeyword.Charge))
                {
                    StartKeywordFeedback(
                        m_View.ChargeFeedbackIcon,
                        ref m_ChargeFeedbackElapsed,
                        m_ChargeFeedbackOrigin);
                }
                if (BattleKeywordRules.Has(attacker.Keywords, EBattleKeyword.LongShot))
                {
                    StartKeywordFeedback(
                        m_View.LongShotFeedbackIcon,
                        ref m_LongShotFeedbackElapsed,
                        m_LongShotFeedbackOrigin);
                }
            }

            if (m_BoundEntity != m_Session.CurrentTarget.Value ||
                string.IsNullOrWhiteSpace(m_AttackPresentationConfig?.AttackFrameAnimationKey))
                return;

            var sheet = ResourceApi.LoadSprite(m_AttackPresentationConfig.AttackFrameAnimationKey);
            m_AttackEffect.texture = sheet == null ? null : sheet.texture;
            m_AttackEffect.gameObject.SetActive(m_AttackEffect.texture != null);
        }

        private void UpdateAttackLunge(float elapsed)
        {
            if (m_HasAnimationOrigin == false)
                return;
            var progress = Mathf.Clamp01(elapsed / BattleRules.AttackLungeDuration);
            var direction = m_Card.Side == EBattleSide.Player ? 1f : -1f;
            var offset = Mathf.Sin(progress * Mathf.PI) * BattleRules.AttackLungeDistance * direction;
            ((RectTransform)transform).anchoredPosition = m_AnimationOrigin + new Vector2(0f, offset);
        }

        private void UpdateHitPresentation(float elapsed)
        {
            if (m_AttackPresentationConfig == null)
                return;

            UpdateAttackEffectFrame(elapsed);
            UpdateHitFlash(elapsed);
        }

        private void UpdateAttackEffectFrame(float elapsed)
        {
            if (m_AttackEffect == null || m_AttackEffect.texture == null)
                return;
            var effectDuration = BattleRules.AttackEffectFrameCount * BattleRules.AttackEffectFrameInterval;
            if (elapsed >= effectDuration)
            {
                m_AttackEffect.gameObject.SetActive(false);
                return;
            }

            var frame = Mathf.Min(
                BattleRules.AttackEffectFrameCount - 1,
                Mathf.FloorToInt(elapsed / BattleRules.AttackEffectFrameInterval));
            var column = frame % BattleRules.AttackEffectColumns;
            var topDownRow = frame / BattleRules.AttackEffectColumns;
            var row = BattleRules.AttackEffectRows - 1 - topDownRow;
            m_AttackEffect.uvRect = new Rect(
                column / (float)BattleRules.AttackEffectColumns,
                row / (float)BattleRules.AttackEffectRows,
                1f / BattleRules.AttackEffectColumns,
                1f / BattleRules.AttackEffectRows);
            m_AttackEffect.gameObject.SetActive(true);
        }

        private void UpdateHitFlash(float elapsed)
        {
            if (m_View.ArtworkArea == null)
                return;
            var hitDelays = m_AttackPresentationConfig.HitDelays;
            var strength = 0f;
            for (var i = 0; i < hitDelays.Length; i++)
            {
                var flashElapsed = elapsed - hitDelays[i];
                if (flashElapsed < 0f || flashElapsed >= BattleRules.HitFlashDuration)
                    continue;
                var flashStrength = Mathf.Sin(flashElapsed / BattleRules.HitFlashDuration * Mathf.PI);
                strength = Mathf.Max(strength, flashStrength);
            }
            m_View.ArtworkArea.color = Color.Lerp(m_ArtworkBaseColor, HitFlashColor, strength);
        }

        private void ResetAttackPresentationVisuals()
        {
            if (m_HasAnimationOrigin)
                ((RectTransform)transform).anchoredPosition = m_AnimationOrigin;
            if (m_View != null && m_View.ArtworkArea != null)
                m_View.ArtworkArea.color = m_ArtworkBaseColor;
            if (m_AttackEffect != null)
            {
                m_AttackEffect.texture = null;
                m_AttackEffect.gameObject.SetActive(false);
            }
            m_AttackPresentationConfig = null;
            m_ActivePresentationSequence = 0;
            m_HasAnimationOrigin = false;
        }

        private void CacheFeedbackLayout()
        {
            if (m_View.AttackText != null)
                m_AttackTextOrigin = m_View.AttackText.rectTransform.anchoredPosition;
            if (m_View.HealthText != null)
                m_HealthTextOrigin = m_View.HealthText.rectTransform.anchoredPosition;
            if (m_View.DamagePopupBackground != null)
                m_DamagePopupOrigin = m_View.DamagePopupBackground.rectTransform.anchoredPosition;
            if (m_View.ChargeFeedbackIcon != null)
                m_ChargeFeedbackOrigin = m_View.ChargeFeedbackIcon.rectTransform.anchoredPosition;
            if (m_View.LongShotFeedbackIcon != null)
                m_LongShotFeedbackOrigin = m_View.LongShotFeedbackIcon.rectTransform.anchoredPosition;
        }

        private void CacheKeywordTooltipLayout()
        {
            if (m_View.KeywordTooltip == null)
                return;
            var tooltipRect = (RectTransform)m_View.KeywordTooltip.transform;
            m_KeywordTooltipHomeParent = tooltipRect.parent;
            m_KeywordTooltipHomePosition = tooltipRect.anchoredPosition;
        }

        private void UpdateFeedbackAnimations(float deltaTime)
        {
            UpdateStatTransition(
                m_View.AttackText,
                m_View.AttackValueOutgoingText,
                ref m_AttackTransitionElapsed,
                m_AttackTextOrigin,
                deltaTime);
            UpdateStatTransition(
                m_View.HealthText,
                m_View.HealthValueOutgoingText,
                ref m_HealthTransitionElapsed,
                m_HealthTextOrigin,
                deltaTime);
            UpdateDamagePopup(deltaTime);
            UpdateKeywordFeedback(
                m_View.ChargeFeedbackIcon,
                ref m_ChargeFeedbackElapsed,
                m_ChargeFeedbackOrigin,
                deltaTime);
            UpdateKeywordFeedback(
                m_View.LongShotFeedbackIcon,
                ref m_LongShotFeedbackElapsed,
                m_LongShotFeedbackOrigin,
                deltaTime);
        }

        private static void UpdateStatTransition(
            TMP_Text currentText,
            TMP_Text outgoingText,
            ref float elapsed,
            Vector2 origin,
            float deltaTime)
        {
            if (elapsed < 0f || currentText == null || outgoingText == null)
                return;

            elapsed += deltaTime;
            var progress = Mathf.Clamp01(elapsed / StatTransitionDuration);
            var eased = Mathf.SmoothStep(0f, 1f, progress);
            currentText.rectTransform.anchoredPosition =
                origin + Vector2.down * Mathf.Lerp(StatTransitionIncomingDistance, 0f, eased);
            currentText.color = WithAlpha(currentText.color, eased);
            outgoingText.rectTransform.anchoredPosition =
                origin + Vector2.up * (StatTransitionOutgoingDistance * eased);
            outgoingText.color = WithAlpha(outgoingText.color, 1f - eased);
            if (progress < 1f)
                return;

            currentText.rectTransform.anchoredPosition = origin;
            currentText.color = WithAlpha(currentText.color, 1f);
            outgoingText.gameObject.SetActive(false);
            elapsed = InactiveFeedbackElapsed;
        }

        private void StartDamagePopup(int damage)
        {
            if (damage <= 0 || m_View.DamagePopupBackground == null || m_View.DamagePopupText == null)
                return;

            m_View.DamagePopupText.text = $"-{damage}";
            m_View.DamagePopupBackground.rectTransform.anchoredPosition = m_DamagePopupOrigin;
            SetGraphicAlpha(m_View.DamagePopupBackground, 1f);
            SetGraphicAlpha(m_View.DamagePopupText, 1f);
            m_View.DamagePopupBackground.gameObject.SetActive(true);
            m_View.DamagePopupBackground.transform.SetAsLastSibling();
            m_DamagePopupElapsed = 0f;
        }

        private void UpdateDamagePopup(float deltaTime)
        {
            if (m_DamagePopupElapsed < 0f ||
                m_View.DamagePopupBackground == null ||
                m_View.DamagePopupText == null)
                return;

            m_DamagePopupElapsed += deltaTime;
            var progress = Mathf.Clamp01(m_DamagePopupElapsed / DamagePopupDuration);
            var eased = Mathf.SmoothStep(0f, 1f, progress);
            var alpha = 1f - Mathf.InverseLerp(0.24f, 1f, progress);
            m_View.DamagePopupBackground.rectTransform.anchoredPosition =
                m_DamagePopupOrigin + Vector2.up * (DamagePopupDistance * eased);
            SetGraphicAlpha(m_View.DamagePopupBackground, alpha);
            SetGraphicAlpha(m_View.DamagePopupText, alpha);
            if (progress < 1f)
                return;

            m_View.DamagePopupBackground.gameObject.SetActive(false);
            m_DamagePopupElapsed = InactiveFeedbackElapsed;
        }

        private static void StartKeywordFeedback(Image icon, ref float elapsed, Vector2 origin)
        {
            if (icon == null)
                return;
            icon.rectTransform.anchoredPosition = origin;
            SetGraphicAlpha(icon, 1f);
            icon.gameObject.SetActive(true);
            icon.transform.SetAsLastSibling();
            elapsed = 0f;
        }

        private static void UpdateKeywordFeedback(
            Image icon,
            ref float elapsed,
            Vector2 origin,
            float deltaTime)
        {
            if (elapsed < 0f || icon == null)
                return;

            elapsed += deltaTime;
            var progress = Mathf.Clamp01(elapsed / KeywordFeedbackDuration);
            var eased = Mathf.SmoothStep(0f, 1f, progress);
            icon.rectTransform.anchoredPosition =
                origin + Vector2.up * (KeywordFeedbackDistance * eased);
            SetGraphicAlpha(icon, 1f - Mathf.InverseLerp(0.18f, 1f, progress));
            if (progress < 1f)
                return;

            icon.gameObject.SetActive(false);
            elapsed = InactiveFeedbackElapsed;
        }

        private void ResetFeedbackAnimations(bool clearTrackedValues)
        {
            ResetStatTransition(
                m_View?.AttackText,
                m_View?.AttackValueOutgoingText,
                ref m_AttackTransitionElapsed,
                m_AttackTextOrigin);
            ResetStatTransition(
                m_View?.HealthText,
                m_View?.HealthValueOutgoingText,
                ref m_HealthTransitionElapsed,
                m_HealthTextOrigin);
            ResetFloatingFeedback(
                m_View?.DamagePopupBackground,
                ref m_DamagePopupElapsed,
                m_DamagePopupOrigin);
            ResetFloatingFeedback(
                m_View?.ChargeFeedbackIcon,
                ref m_ChargeFeedbackElapsed,
                m_ChargeFeedbackOrigin);
            ResetFloatingFeedback(
                m_View?.LongShotFeedbackIcon,
                ref m_LongShotFeedbackElapsed,
                m_LongShotFeedbackOrigin);
            if (m_View?.DamagePopupText != null)
            {
                m_View.DamagePopupText.text = string.Empty;
                SetGraphicAlpha(m_View.DamagePopupText, 1f);
            }
            if (clearTrackedValues == false)
                return;
            m_HasLastAttack = false;
            m_HasLastHealth = false;
            m_LastKeywordFeedbackSequence = 0;
        }

        private static void ResetStatTransition(
            TMP_Text currentText,
            TMP_Text outgoingText,
            ref float elapsed,
            Vector2 origin)
        {
            if (currentText != null)
            {
                currentText.rectTransform.anchoredPosition = origin;
                currentText.color = WithAlpha(currentText.color, 1f);
            }
            if (outgoingText != null)
            {
                outgoingText.text = string.Empty;
                outgoingText.rectTransform.anchoredPosition = origin;
                outgoingText.gameObject.SetActive(false);
            }
            elapsed = InactiveFeedbackElapsed;
        }

        private static void ResetFloatingFeedback(Image image, ref float elapsed, Vector2 origin)
        {
            if (image != null)
            {
                image.rectTransform.anchoredPosition = origin;
                SetGraphicAlpha(image, 1f);
                image.gameObject.SetActive(false);
            }
            elapsed = InactiveFeedbackElapsed;
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic != null)
                graphic.color = WithAlpha(graphic.color, alpha);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void ApplyFrameColors()
        {
            var frameColor = m_IsHovered ? HoverFrameColor : m_DefaultFrameColor;
            if (m_View.CardFrame != null)
                m_View.CardFrame.color = frameColor;
            if (m_View.AttackerHighlight != null)
                m_View.AttackerHighlight.color = m_IsHovered ? HoverFrameColor : AttackerHighlightColor;
            if (m_View.TargetHighlight != null)
                m_View.TargetHighlight.color = m_IsHovered ? HoverFrameColor : TargetHighlightColor;
        }

        public static Color GetTierFrameColor(EBattleCardTier tier)
        {
            switch (tier)
            {
                case EBattleCardTier.Silver:
                    return SilverFrameColor;
                case EBattleCardTier.Gold:
                    return GoldFrameColor;
                case EBattleCardTier.Legendary:
                    return LegendaryFrameColor;
                default:
                    return BronzeFrameColor;
            }
        }

        public static Color GetStatTextColor(int currentValue, int entryValue)
        {
            if (currentValue < entryValue)
                return LowerStatTextColor;
            if (currentValue > entryValue)
                return HigherStatTextColor;
            return DefaultStatTextColor;
        }

        private void OnCardPointerEnter(PointerEventData ignored)
        {
            m_IsHovered = true;
            ApplyFrameColors();
            ShowKeywordTooltip();
        }

        private void OnCardPointerExit(PointerEventData ignored)
        {
            m_IsHovered = false;
            HideKeywordTooltip();
            ApplyFrameColors();
        }

        private void OnCardPointerClicked(PointerEventData ignored)
        {
            if (m_PreparationBindingMode == EPreparationBindingMode.Collection && m_PreparationCardLocked == false)
                m_CollectionClick?.Invoke(m_PreparationCardNumber, (RectTransform)transform);
        }

        private void ShowKeywordTooltip()
        {
            if (m_View.KeywordTooltip == null ||
                m_View.KeywordTooltipText == null ||
                string.IsNullOrWhiteSpace(m_View.KeywordTooltipText.text))
            {
                HideKeywordTooltip();
                return;
            }

            var tooltipRect = (RectTransform)m_View.KeywordTooltip.transform;
            m_View.KeywordTooltip.SetActive(true);
            m_View.KeywordTooltipText.ForceMeshUpdate();
            var tooltipHeight = Mathf.Clamp(
                m_View.KeywordTooltipText.preferredHeight + KeywordTooltipVerticalPadding,
                KeywordTooltipMinHeight,
                KeywordTooltipMaxHeight);
            tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tooltipHeight);

            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            var cardScreenPosition = RectTransformUtility.WorldToScreenPoint(
                camera,
                ((RectTransform)transform).position);
            var placeOnLeft = cardScreenPosition.x > Screen.width * 0.58f;
            var offset = new Vector2(
                placeOnLeft ? -KeywordTooltipHorizontalOffset : KeywordTooltipHorizontalOffset,
                KeywordTooltipVerticalOffset);
            if (canvas != null && canvas.transform is RectTransform canvasRect &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    cardScreenPosition,
                    camera,
                    out var cardCanvasPosition))
            {
                tooltipRect.SetParent(canvasRect, false);
                tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
                tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
                tooltipRect.localScale = Vector3.one;
                tooltipRect.anchoredPosition = cardCanvasPosition + offset;
            }
            else
            {
                tooltipRect.anchoredPosition = offset;
            }
            tooltipRect.SetAsLastSibling();
        }

        private void HideKeywordTooltip()
        {
            if (m_View?.KeywordTooltip == null)
                return;
            var tooltipRect = (RectTransform)m_View.KeywordTooltip.transform;
            m_View.KeywordTooltip.SetActive(false);
            if (m_KeywordTooltipHomeParent == null || tooltipRect.parent == m_KeywordTooltipHomeParent)
                return;
            tooltipRect.SetParent(m_KeywordTooltipHomeParent, false);
            tooltipRect.localScale = Vector3.one;
            tooltipRect.anchoredPosition = m_KeywordTooltipHomePosition;
        }

        private void OnPreparationDragStarted(PointerEventData ignored)
        {
            if (m_PreparationCardNumber == 0 || m_DetachedPreparationSlotBackdrop != null)
                return;

            var backdrop = GetPreparationSlotBackdrop();
            if (backdrop == null || backdrop.parent != m_View.transform)
                return;

            backdrop.SetParent(transform, true);
            backdrop.SetAsFirstSibling();
            m_DetachedPreparationSlotBackdrop = backdrop;
        }

        private RectTransform GetPreparationSlotBackdrop()
        {
            if (m_PreparationBindingMode == EPreparationBindingMode.BattleSlot)
                return m_View.PreparationBattleSlotEmptyState?.transform as RectTransform;
            if (m_PreparationBindingMode == EPreparationBindingMode.FusionSlot)
                return m_View.PreparationFusionSlotEmptyState?.transform as RectTransform;
            return null;
        }

        private void RestorePreparationSlotBackdrop()
        {
            m_RestorePreparationSlotBackdropAfterDrag = false;
            if (m_DetachedPreparationSlotBackdrop == null || m_View == null)
                return;

            m_DetachedPreparationSlotBackdrop.SetParent(m_View.transform, false);
            m_DetachedPreparationSlotBackdrop.anchorMin = Vector2.zero;
            m_DetachedPreparationSlotBackdrop.anchorMax = Vector2.one;
            m_DetachedPreparationSlotBackdrop.pivot = new Vector2(0.5f, 0.5f);
            m_DetachedPreparationSlotBackdrop.anchoredPosition = Vector2.zero;
            m_DetachedPreparationSlotBackdrop.sizeDelta = Vector2.zero;
            m_DetachedPreparationSlotBackdrop.localRotation = Quaternion.identity;
            m_DetachedPreparationSlotBackdrop.localScale = Vector3.one;
            m_DetachedPreparationSlotBackdrop.SetAsFirstSibling();
            m_DetachedPreparationSlotBackdrop = null;
        }

        private void OnPreparationDragReturned(PointerEventData eventData)
        {
            var returnBattleCardToPool =
                m_PreparationBindingMode == EPreparationBindingMode.BattleSlot &&
                m_PreparationCardNumber != 0 &&
                m_PreparationPage != null &&
                m_PreparationPage.IsPointerInsideCardPool(eventData);
            var returnFusionMaterialToPool =
                m_PreparationBindingMode == EPreparationBindingMode.FusionSlot &&
                m_PreparationCardNumber != 0 &&
                m_PreparationPage != null &&
                m_PreparationPage.IsPointerInsideFusionArea(eventData) == false;
            m_View.transform.localRotation = Quaternion.identity;
            if (returnBattleCardToPool)
                m_PreparationPage.RemoveBattleCard(m_PreparationSlot, m_PreparationCardNumber);
            else if (returnFusionMaterialToPool)
                m_PreparationPage.RemoveFusionMaterial(m_PreparationSlot);
            m_PreparationPage?.OnDragReturned();
            m_RestorePreparationSlotBackdropAfterDrag = m_DetachedPreparationSlotBackdrop != null;
        }

        private void OnPreparationInteractorTouch(Interactor requester)
        {
            if (m_View.PreparationDropHighlight == null || TryGetPreparationSource(requester, out var source) == false)
                return;
            if (CanDropOnCurrentPreparationTarget(source))
                m_View.PreparationDropHighlight.gameObject.SetActive(true);
        }

        private void OnPreparationInteractorTouchEnd(Interactor ignored)
        {
            if (m_View.PreparationDropHighlight != null)
                m_View.PreparationDropHighlight.gameObject.SetActive(false);
        }

        private void OnPreparationInteract(Interactor requester, Interactor responder)
        {
            if (m_PreparationPage == null ||
                !ReferenceEquals(responder, m_View.PreparationInteractor) ||
                TryGetPreparationSource(requester, out var source) == false)
                return;

            if (m_View.PreparationDropHighlight != null)
                m_View.PreparationDropHighlight.gameObject.SetActive(false);
            switch (m_PreparationBindingMode)
            {
                case EPreparationBindingMode.CardPool:
                    break;
                case EPreparationBindingMode.BattleSlot:
                    if (source.Source == EPreparationCardSource.FusionSlot)
                        break;
                    m_PreparationPage.DropCardOnSlot(source.CardNumber, m_PreparationSlot);
                    break;
                case EPreparationBindingMode.FusionSlot:
                    if (source.Source == EPreparationCardSource.BattleSlot)
                        break;
                    var sourceSlot = source.Source == EPreparationCardSource.FusionSlot
                        ? source.SourceSlot
                        : -1;
                    m_PreparationPage.DropCardOnFusionSlot(source.CardNumber, m_PreparationSlot, sourceSlot);
                    break;
            }
        }

        private bool CanDropOnCurrentPreparationTarget(PreparationInteractorData source)
        {
            if (source == null || source.CardNumber == 0)
                return false;
            if (m_PreparationBindingMode == EPreparationBindingMode.BattleSlot)
                return true;
            if (m_PreparationBindingMode == EPreparationBindingMode.FusionSlot)
                return source.Source != EPreparationCardSource.BattleSlot;
            return false;
        }

        private static bool TryGetPreparationSource(
            Interactor requester,
            out PreparationInteractorData source)
        {
            source = null;
            if (!(requester is UiInteractor uiInteractor))
                return false;
            source = uiInteractor.Wrapper.ExtraInfo as PreparationInteractorData;
            return source != null && source.CardNumber != 0;
        }
    }
}
