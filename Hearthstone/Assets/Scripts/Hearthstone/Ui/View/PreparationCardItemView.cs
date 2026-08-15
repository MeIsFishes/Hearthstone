using System;
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public sealed class PreparationCardItemView : UiViewBase
    {
        public GameObject EmptyState;
        public GameObject OwnedState;
        public GameObject MaterialSelectedState;
        public TMP_Text MaterialSelectedText;
        public Image ArtworkArea;
        public Image CardFrame;
        public Image CardNumberBadge;
        public TMP_Text CardNumberText;
        public TMP_Text NameText;
        public TMP_Text KeywordText;
        public TMP_Text AttackText;
        public TMP_Text HealthText;
        public UiDragable Dragable;
        public UiInteractor Interactor;
        public UiEventListener EmptyAttemptListener;

        public override Type GetControllerType()
        {
            return typeof(PreparationCardItemController);
        }
    }
}
