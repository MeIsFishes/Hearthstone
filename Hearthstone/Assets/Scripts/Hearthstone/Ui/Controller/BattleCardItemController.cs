using BbxCommon;
using BbxCommon.Ui;
using Unity.Entities;
using UnityEngine;

namespace Hearthstone
{
    public sealed class BattleCardItemController : UiControllerBase<BattleCardItemView>
    {
        private const string EnemyCardFrameArtworkKey = "CardFrame-v3";
        private const string PlayerCardFrameArtworkKey = "CardFrameBlue-v2";

        private Entity m_BoundEntity;
        private BattleCardRawComponent m_Card;
        private BattleSessionSingletonRawComponent m_Session;
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

        public void Bind(Entity entity)
        {
            Unbind();
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

        protected override void OnUiClose()
        {
            Unbind();
        }

        private void Unbind()
        {
            m_HealthListener.RebindTarget(null);
            m_AttackListener.RebindTarget(null);
            m_AliveListener.RebindTarget(null);
            m_AttackerListener.RebindTarget(null);
            m_TargetListener.RebindTarget(null);
            m_BoundEntity = Entity.Null;
            m_Card = null;
            m_Session = null;

            if (m_View != null)
            {
                m_View.transform.localRotation = Quaternion.identity;
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
                if (m_View.CardNumberBadge != null)
                    m_View.CardNumberBadge.gameObject.SetActive(false);
                if (m_View.DeadOverlay != null)
                    m_View.DeadOverlay.gameObject.SetActive(false);
                if (m_View.AttackerHighlight != null)
                    m_View.AttackerHighlight.gameObject.SetActive(false);
                if (m_View.TargetHighlight != null)
                    m_View.TargetHighlight.gameObject.SetActive(false);
            }
        }

        private void RefreshAll()
        {
            if (m_Card == null)
                return;

            if (m_View.CardBackground != null)
                m_View.CardBackground.color = Color.clear;
            if (m_View.CardFrame != null)
            {
                m_View.CardFrame.gameObject.SetActive(true);
                var frameKey = m_Card.Side == EBattleSide.Player
                    ? PlayerCardFrameArtworkKey
                    : EnemyCardFrameArtworkKey;
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
            RefreshAttack(m_Card.Attack);

            RefreshCardNumber();

            RefreshPresentation();
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

        private void RefreshPresentation()
        {
            var cardConfig = DataApi.GetData<BattleCardCsvData>(m_Card.CardNumber);
            if (cardConfig == null)
            {
                DebugApi.LogError($"Battle card configuration {m_Card.CardNumber} is missing.");
                return;
            }

            var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(m_Card.CardTypeId);
            if (typeConfig == null)
            {
                DebugApi.LogError($"Battle card type {m_Card.CardTypeId} is missing.");
                return;
            }

            if (m_View.SkillDescriptionText != null)
            {
                m_View.SkillDescriptionText.text = typeConfig.DisplayName;
                m_View.SkillDescriptionText.gameObject.SetActive(true);
            }

            var keywordText = BattleKeywordRules.FormatDisplayText(m_Card.Keywords);
            if (m_View.KeywordText != null)
            {
                m_View.KeywordText.text = keywordText;
                m_View.KeywordText.gameObject.SetActive(string.IsNullOrEmpty(m_View.KeywordText.text) == false);
            }

            DebugApi.Log(
                $"[BattleKeyword] Presentation Side={m_Card.Side} Slot={m_Card.SlotIndex} " +
                $"Card={m_Card.CardNumber} Keywords={m_Card.Keywords} Text='{keywordText}'");

            if (m_View.ArtworkArea != null)
            {
                m_View.ArtworkArea.sprite = ResourceApi.LoadSprite(cardConfig.ArtworkKey);
                m_View.ArtworkArea.preserveAspect = true;
                m_View.ArtworkArea.gameObject.SetActive(m_View.ArtworkArea.sprite != null);
                if (m_View.ArtworkArea.sprite == null)
                    DebugApi.LogError($"Battle card artwork '{cardConfig.ArtworkKey}' is missing.");
            }
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
    }
}
