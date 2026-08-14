using BbxCommon.Ui;

namespace Hearthstone
{
    public enum PlaceholderUiGroup
    {
        Main,
    }

    /// <summary>
    /// 空项目占位 UiScene。对应的 Canvas、Prefab 和 UiSceneAsset 由 Unity Editor 创建。
    /// </summary>
    public sealed class PlaceholderUiScene : UiSceneBase<PlaceholderUiGroup>
    {
        protected override void OnSceneInit()
        {
            UiGroupWrapper.CreateUiGroupRoot(PlaceholderUiGroup.Main);
        }
    }
}
