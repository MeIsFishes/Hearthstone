using System;
using BbxCommon.Ui;

namespace Hearthstone
{
    /// <summary>
    /// 全屏静态 Loading 视图。画面内容完全保存在对应 Prefab 中。
    /// </summary>
    public sealed class LoadingView : UiViewBase
    {
        public override Type GetControllerType()
        {
            return typeof(LoadingController);
        }
    }
}
