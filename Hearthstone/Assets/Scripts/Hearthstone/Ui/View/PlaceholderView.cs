using System;
using BbxCommon.Ui;
using TMPro;

namespace Hearthstone
{
    /// <summary>
    /// 空项目占位 View。将它挂到 UI Prefab，并在 Inspector 中绑定 StatusText。
    /// </summary>
    public sealed class PlaceholderView : UiViewBase
    {
        public TMP_Text StatusText;

        public override Type GetControllerType()
        {
            return typeof(PlaceholderController);
        }
    }
}
