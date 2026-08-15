using System;
using BbxCommon.Ui;
using UnityEngine.UI;

namespace Hearthstone
{
    public sealed class FusionRecommendationItemView : UiViewBase
    {
        public UiList CardList;
        public Button SelectButton;

        public override Type GetControllerType()
        {
            return typeof(FusionRecommendationItemController);
        }
    }
}
