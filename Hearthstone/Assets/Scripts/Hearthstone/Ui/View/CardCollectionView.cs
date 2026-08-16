using System;
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public sealed class CardCollectionView : UiViewBase
    {
        public Button BackButton;
        public TMP_Text CollectedCountText;
        public ScrollRect CardScrollRect;
        public UiList CardList;
        public GameObject PreviewOverlay;
        public Button PreviewDismissButton;
        public RectTransform PreviewCardRoot;
        public UiList PreviewCardList;

        public override Type GetControllerType()
        {
            return typeof(CardCollectionController);
        }
    }
}
