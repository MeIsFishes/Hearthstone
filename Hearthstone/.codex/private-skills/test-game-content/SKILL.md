---
name: test-game-content
description: 进入指定游戏内容时，通过框架StageGroup配置参数并直接启动。
---

# 通过 StageGroup 打开游戏

## 框架层能力

通过 StageGroup 进入游戏是 BbxCommon 框架能力，不是具体项目自行实现的测试工具：

- 运行时由框架的 `StageWrapper.SetActiveGameStage(...)` 负责完整 Stage 集合的卸载与加载。
- `GameStageEntryAsset.CreateStageGroupBuildCallback()` 让具体 Editor 入口携带 `Func<bool>` 构建回调；Launcher 每个 Editor update 调用一次，返回 `false` 时继续等待，返回 `true` 时完成并自动注销。特殊启动条件只写在入口构建回调中，不修改正式 GameEngine 流程。
- Editor 侧由框架提供 `GameStageEntryAsset`、`GameStageEntryLauncher`、`GameStageWindow.CreateEntryAsset(...)`、`Assets/Resources/Editor/` 入口目录协议，以及校验、保存、`SessionState` 和跨域 Play Mode 派发。
- 具体项目只提供具名 StageGroup 组合方法、`XxxStageEntryAsset`、业务输入字段、强类型 StartupData 映射和入口资产。
- 框架能力缺失时修改 `Assets/Scripts/BbxCommon/` 和对应底层 skill；禁止在项目目录复制 Launcher、Play Mode runner、入口发现、资产创建或 SessionState 协议。

## 选择入口

1. 查找 GameEngine 上对应的具名 StageGroup 入口、`XxxStageEntryAsset` 和 `Assets/Resources/Editor/` 下的入口资产。
2. 现有入口能通过修改序列化字段表达本次启动参数时，直接复用该入口资产；不要仅因参数值不同创建新入口。
3. 现有入口无法进入目标 StageGroup 或无法表达必需参数时，允许创建新的 Editor 入口文件与入口资产。此时读取 [创建新的 StageGroup 入口](./missing-stage-entry.md)。

## 由 Agent 直接启动

不要依赖鼠标点击 Game Stage 窗口。通过 Unity Editor C# 执行能力或临时 Editor 脚本完成以下操作：

1. 用 `AssetDatabase.LoadAssetAtPath<GameStageEntryAsset>(path)` 加载入口资产。
2. 需要改参数时，用 `SerializedObject` 修改入口的序列化字段并应用修改；只设置该入口已经声明的 StartupData 输入。
3. 调用公开 API `GameStageEntryLauncher.Start(entry)`。参数已经正确时也可直接调用 `GameStageEntryLauncher.Start(assetPath)`。
4. Launcher 会检查 Edit Mode、资产路径与 `ValidateEntry`，保存入口资产，把路径写入 `SessionState` 并进入 Play Mode；域重载后仍由该入口调用正式 StageGroup。

可执行 C# 的编辑器工具直接运行上述调用即可。只能通过文件触发 Unity 编译时，在 `Assets/Editor/` 创建一次性脚本，并用唯一的 `SessionState` 键防止域重载后重复启动：

```csharp
using BbxCommon.Editor;
using System;
using UnityEditor;

[InitializeOnLoad]
internal static class TempStageGroupLaunch
{
    private const string GuardKey = "Agent.TempStageGroupLaunch.20260810";

    static TempStageGroupLaunch()
    {
        if (SessionState.GetBool(GuardKey, false))
            return;

        SessionState.SetBool(GuardKey, true);
        EditorApplication.delayCall += Launch;
    }

    private static void Launch()
    {
        const string path = "Assets/Resources/Editor/DirectGame.asset";
        var entry = AssetDatabase.LoadAssetAtPath<GameStageEntryAsset>(path);
        if (entry == null)
            throw new InvalidOperationException($"Missing StageGroup entry: {path}");

        var serializedEntry = new SerializedObject(entry);
        serializedEntry.FindProperty("PlayerShipId").intValue = 1;
        serializedEntry.FindProperty("LevelId").intValue = 1;
        serializedEntry.ApplyModifiedPropertiesWithoutUndo();
        GameStageEntryLauncher.Start(entry);
    }
}
```

为每次临时调用更换 Guard Key。确认脚本已经触发启动后保留它直到本轮 Play Mode 使用结束，再删除临时 `.cs` 与 `.meta`；不要让临时脚本进入正式项目内容。

## 边界

- 把通用启动能力保留在 BbxCommon 框架层；项目入口只能适配业务 StageGroup 与参数。
- Editor 入口只负责编辑输入、构造强类型 StartupData 并调用正式具名 StageGroup；不要在入口或临时脚本中自行拼装 Stage。
- Editor 入口需要等待初始 StageGroup 或其它特殊条件时，在自身构建回调中检查条件并返回 `false`；条件满足后调用正式具名 Group 入口并返回 `true`。不要把这类等待状态加入项目 GameEngine 主流程。
- 不在 `SetActiveGameStage` 后补造 StartupData，不直接写运行时 Component，不手动打开内部 Unity Scene 替代 StageGroup。
- 窗口按钮和脚本调用统一使用 `GameStageEntryLauncher`，不得再实现第二套 Play Mode 调度。
