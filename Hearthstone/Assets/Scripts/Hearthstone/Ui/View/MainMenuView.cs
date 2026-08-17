using System;
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public sealed class MainMenuView : UiViewBase
    {
        public Image GameTitle;
        public Image LeftGoblinImage;
        public Sprite[] LeftGoblinFrames;
        public Vector2[] LeftGoblinFrameOffsets;
        public Image RightGoblinImage;
        public Sprite[] RightGoblinFrames;
        public Vector2[] RightGoblinFrameOffsets;
        public Button StartGameButton;
        public Image StartGameHoverBackground;
        public TMP_Text StartGameLabel;
        public Button CollectionButton;
        public TMP_Text CollectionLabel;
        public Button ExitGameButton;
        public TMP_Text ExitGameLabel;
        public Button ClearDataButton;
        public TMP_Text ClearDataLabel;
        public TMP_Text VersionLabel;

        public override Type GetControllerType()
        {
            return typeof(MainMenuController);
        }
    }
}
