using System;
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public sealed class NewPlayerGuideView : UiViewBase
    {
        public GameObject[] PageRoots;
        public Button PreviousButton;
        public Button NextButton;
        public TMP_Text PreviousButtonLabel;
        public TMP_Text NextButtonLabel;
        public TMP_Text PageIndicator;
        public UiList CardPreviewList;

        public override Type GetControllerType()
        {
            return typeof(NewPlayerGuideController);
        }
    }
}
