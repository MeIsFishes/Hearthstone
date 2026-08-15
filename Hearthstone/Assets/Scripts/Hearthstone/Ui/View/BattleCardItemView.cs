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
        public GameObject PreparationEmptyState;
        public GameObject PreparationMaterialSelectedState;
        public UiDragable PreparationDragable;
        public UiInteractor PreparationInteractor;
        public UiEventListener PreparationEmptyAttemptListener;

        public override Type GetControllerType()
        {
            return typeof(BattleCardItemController);
        }
    }
}
