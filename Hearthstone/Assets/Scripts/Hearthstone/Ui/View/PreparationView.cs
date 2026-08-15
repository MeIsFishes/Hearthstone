using System;
using BbxCommon.Ui;
using TMPro;
using UnityEngine.UI;

namespace Hearthstone
{
    public sealed class PreparationView : UiViewBase
    {
        public TMP_Text RewardText;
        public UiList BattleSlotList;
        public ScrollRect CardPoolScrollRect;
        public UiList CardPoolList;
        public Scrollbar CardPoolScrollbar;

        public override Type GetControllerType()
        {
            return typeof(PreparationController);
        }
    }
}
