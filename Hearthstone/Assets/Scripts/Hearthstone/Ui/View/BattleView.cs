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
        public Image ResultBackdropImage;
        public RectTransform ResultBannerRoot;
        public CanvasGroup ResultBannerCanvasGroup;
        public Image ResultBannerImage;
        public Sprite VictoryResultBanner;
        public Sprite DefeatResultBanner;
        public Sprite FinalVictoryResultBanner;

        public override Type GetControllerType()
        {
            return typeof(BattleController);
        }
    }
}
