using System;
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public sealed class BattleCardItemView : UiViewBase
    {
        public Image CardBackground;
        public Image TauntShieldOutline;
        public Image ArtworkArea;
        public Image CardFrame;
        public Image CardNumberBadge;
        public Image AttackerHighlight;
        public Image TargetHighlight;
        public Image DeadOverlay;
        public TMP_Text CardNumberText;
        public TMP_Text SkillDescriptionText;
        public TMP_Text KeywordText;
        public TMP_Text AttackText;
        public TMP_Text HealthText;
        public TMP_Text AttackValueOutgoingText;
        public TMP_Text HealthValueOutgoingText;
        public Image DamagePopupBackground;
        public TMP_Text DamagePopupText;
        public Image ChargeFeedbackIcon;
        public Image LongShotFeedbackIcon;
        public GameObject KeywordTooltip;
        public TMP_Text KeywordTooltipText;
        public GameObject PreparationEmptyState;
        public GameObject PreparationBattleSlotEmptyState;
        public GameObject PreparationFusionSlotEmptyState;
        public GameObject PreparationMaterialSelectedState;
        public GameObject PreparationDeployedState;
        public Image PreparationDropHighlight;
        public Image CardHoverInput;
        public UiDragable PreparationDragable;
        public UiInteractor PreparationInteractor;
        public UiEventListener PreparationEmptyAttemptListener;
        public UiEventListener CardHoverListener;

        public override Type GetControllerType()
        {
            return typeof(BattleCardItemController);
        }
    }
}
