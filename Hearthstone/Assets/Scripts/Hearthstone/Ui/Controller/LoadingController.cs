using BbxCommon.Ui;
using UnityEngine;

namespace Hearthstone
{
    /// <summary>
    /// 仅负责让静态 Loading View 覆盖其所属的引擎 Loading Canvas。
    /// 显隐生命周期由 GameEngineBase 统一管理。
    /// </summary>
    public sealed class LoadingController : UiControllerBase<LoadingView>
    {
        protected override void OnUiShow()
        {
            var controllerRect = (RectTransform)transform;
            controllerRect.anchorMin = Vector2.zero;
            controllerRect.anchorMax = Vector2.one;
            controllerRect.offsetMin = Vector2.zero;
            controllerRect.offsetMax = Vector2.zero;
            controllerRect.SetAsLastSibling();
        }
    }
}
