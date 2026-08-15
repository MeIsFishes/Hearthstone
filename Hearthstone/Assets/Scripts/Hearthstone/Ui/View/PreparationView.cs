using System;
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public sealed class PreparationView : UiViewBase
    {
        public Button ContinueButton;
        public Image ContinueButtonImage;
        public TMP_Text ContinueMainText;
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
        public TMP_Text FusionCurrentPointLabel;
        public TMP_Text FusionCurrentPointValue;
        public TMP_Text FusionRemainingPointLabel;
        public TMP_Text FusionRemainingPointValue;
        public Color FusionUnderTargetColor;
        public Color FusionExactTargetColor;
        public Color FusionOverTargetColor;
        public Button FusionButton;
        public Image FusionButtonImage;
        public UiEventListener FusionButtonAttemptListener;
        public Button FusionRecommendationButton;
        public UiEventListener FusionRecommendationHoverListener;
        public GameObject FusionRecommendationTooltip;
        public UiInteractor FusionAreaInteractor;
        public GameObject FusionRecommendationOverlay;
        public Button FusionRecommendationCloseButton;
        public ScrollRect FusionRecommendationScrollRect;
        public UiList FusionRecommendationList;
        public TMP_Text FusionRecommendationEmptyText;
        public GameObject RewardRevealOverlay;
        public CanvasGroup RewardRevealCanvasGroup;
        public Button RewardRevealConfirmButton;
        public UiList RewardRevealCardList;
        public GameObject FusionRevealOverlay;
        public CanvasGroup FusionRevealCanvasGroup;
        public Button FusionRevealDismissButton;
        public UiList FusionRevealMaterialCardList;
        public RectTransform FusionRevealCardRoot;
        public UiList FusionRevealCardList;
        public GameObject FusionRevealSealedFace;
        public GameObject FusionRevealCardBack;
        public RectTransform FusionRevealFlash;
        public CanvasGroup FusionRevealFlashCanvasGroup;
        public ScrollRect CardPoolScrollRect;
        public UiList CardPoolList;
        public Scrollbar CardPoolScrollbar;
        public UiInteractor CardPoolInteractor;
        public Toggle OwnedOnlyToggle;
        public TMP_Text OwnedOnlyLabel;

        public override Type GetControllerType()
        {
            return typeof(PreparationController);
        }
    }
}
