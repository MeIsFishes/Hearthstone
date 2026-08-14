using System;
using BbxCommon.Ui;
using TMPro;

namespace Hearthstone
{
    public sealed class BattleView : UiViewBase
    {
        public UiList EnemyCardList;
        public UiList PlayerCardList;
        public TMP_Text TurnText;
        public TMP_Text ResultText;

        public override Type GetControllerType()
        {
            return typeof(BattleController);
        }
    }
}
