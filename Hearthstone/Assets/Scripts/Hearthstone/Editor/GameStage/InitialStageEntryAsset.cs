using BbxCommon.Editor;
using UnityEngine;

namespace Hearthstone.Editor
{
    /// <summary>
    /// 无额外输入的初始 Stage Group 编辑器入口。
    /// GameEngine 启动时已经进入该 Group，因此这里只等待入口可用。
    /// </summary>
    [CreateAssetMenu(
        fileName = "InitialStageEntry",
        menuName = "Hearthstone/GameStage Entry/Initial")]
    public sealed class InitialStageEntryAsset : GameStageEntryAsset
    {
        public override bool ValidateEntry(out string error)
        {
            error = string.Empty;
            return true;
        }

        public override System.Func<bool> CreateStageGroupBuildCallback()
        {
            return IsInitialStageGroupReady;
        }

        private static bool IsInitialStageGroupReady()
        {
            return Object.FindObjectOfType<HearthstoneGameEngine>() != null;
        }
    }
}
