using System;
using BbxCommon.Ui;
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
        public GameObject ResultPopupRoot;
        public CanvasGroup ResultPopupCanvasGroup;
        public Image ResultPopupImage;
        public Button ReturnToMainMenuButton;

        public override Type GetControllerType()
        {
            return typeof(BattleController);
        }
    }
}
