using System;
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public sealed class BattleView : UiViewBase
    {
        public UiList EnemyCardList;
        public UiList PlayerCardList;
        public RectTransform VictoryBannerRoot;
        public CanvasGroup VictoryBannerCanvasGroup;
        public TMP_Text VictoryBannerText;
        public GameObject ResultPopupRoot;
        public CanvasGroup ResultPopupCanvasGroup;
        public Image ResultPopupImage;
        public TMP_Text ResultPopupTitle;
        public TMP_Text ResultPopupBody;
        public Button RestartButton;
        public TMP_Text RestartButtonText;

        public override Type GetControllerType()
        {
            return typeof(BattleController);
        }
    }
}
