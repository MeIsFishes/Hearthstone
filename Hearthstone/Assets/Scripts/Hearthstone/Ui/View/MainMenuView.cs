using System;
using BbxCommon.Ui;
using TMPro;
using UnityEngine.UI;

namespace Hearthstone
{
    public sealed class MainMenuView : UiViewBase
    {
        public TMP_Text GameTitle;
        public Button StartGameButton;
        public TMP_Text StartGameLabel;

        public override Type GetControllerType()
        {
            return typeof(MainMenuController);
        }
    }
}
