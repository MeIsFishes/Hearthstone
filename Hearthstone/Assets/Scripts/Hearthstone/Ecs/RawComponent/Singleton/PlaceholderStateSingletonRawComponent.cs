using BbxCommon;

namespace Hearthstone
{
    /// <summary>
    /// 空项目占位状态。首个真实模块建立后，替换为有业务含义的 Component。
    /// </summary>
    public sealed class PlaceholderStateSingletonRawComponent : EcsSingletonRawComponent
    {
        public readonly ListenableVariable<bool> Initialized = new(false);

        protected override void OnSingletonCollect()
        {
            Initialized.MakeInvalid();
            Initialized.SetValue(false);
        }
    }
}
