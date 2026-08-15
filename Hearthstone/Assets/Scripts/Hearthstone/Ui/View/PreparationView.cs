using System;
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public sealed class PreparationView : UiViewBase
    {
        public TMP_Text RewardText;
        public Button ContinueButton;
        public Image ContinueButtonImage;
        public TMP_Text ContinueMainText;
        public TMP_Text ContinueAuxiliaryText;
        public GameObject ContinueWaitingInputBlocker;
        public UiEventListener ContinueWaitingAttemptListener;
        public Button BattleTabButton;
        public Button FusionTabButton;
        public Image BattleTabImage;
        public Image FusionTabImage;
        public GameObject BattleOperationRoot;
        public GameObject FusionOperationRoot;
        public UiList BattleSlotList;
        public UiList FusionSlotList;
        public TMP_Text FusionExpressionText;
        public TMP_Text FusionResultText;
        public Color FusionUnderTargetColor;
        public Color FusionExactTargetColor;
        public Color FusionOverTargetColor;
        public Button FusionButton;
        public Image FusionButtonImage;
        public UiEventListener FusionButtonAttemptListener;
        public UiInteractor FusionAreaInteractor;
        public ScrollRect CardPoolScrollRect;
        public UiList CardPoolList;
        public Scrollbar CardPoolScrollbar;
        public UiInteractor CardPoolInteractor;

        public override Type GetControllerType()
        {
            return typeof(PreparationController);
        }
    }
}
