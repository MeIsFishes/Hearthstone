using BbxCommon;
using Unity.Entities;

namespace Hearthstone
{
    /// <summary>
    /// 空项目占位 System。它只证明 Stage、Component 与 System 已正确接通。
    /// </summary>
    [DisableAutoCreation]
    public partial class PlaceholderStateSystem : EcsMixSystemBase
    {
        protected override void OnSystemUpdate()
        {
            var state = GetSingletonRawComponent<PlaceholderStateSingletonRawComponent>();
            if (state == null || state.Initialized.Value)
                return;

            state.Initialized.SetValue(true);
        }
    }
}
