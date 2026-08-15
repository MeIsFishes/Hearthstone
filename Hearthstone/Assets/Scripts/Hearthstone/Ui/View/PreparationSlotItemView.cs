using System;
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public sealed class PreparationSlotItemView : UiViewBase
    {
        public GameObject EmptyState;
        public GameObject OccupiedState;
        public Image ArtworkArea;
        public Image CardFrame;
        public Image DropHighlight;
        public TMP_Text NameText;
        public TMP_Text KeywordText;
        public TMP_Text AttackText;
        public TMP_Text HealthText;
        public UiDragable Dragable;
        public UiInteractor Interactor;

        public override Type GetControllerType()
        {
            return typeof(PreparationSlotItemController);
        }
    }
}
