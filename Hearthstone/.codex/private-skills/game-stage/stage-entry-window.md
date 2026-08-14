# Stage 入口窗口规范

当某个 GameStage Group 需要从 Editor 以指定必需输入直接启动时，为其创建 `GameStageEntryAsset` 具体子类。该资产是 Editor Group 入口，不是另一套运行时 Stage 组合逻辑。

## 层级归属

StageGroup 的通用 Editor 进入能力属于 BbxCommon 框架层：框架维护 `GameStageEntryAsset`、`GameStageEntryLauncher`、`GameStageWindow.CreateEntryAsset(...)`、固定入口目录、资产校验保存、`SessionState` 和跨域 Play Mode 派发。

项目层只编写 `XxxStageEntryAsset` 业务适配类与入口资产，把项目字段转换为强类型 StartupData，再调用项目 GameEngine 上的正式具名 Group 入口。不得在项目层创建第二套入口基类、Launcher、窗口、资产发现或 SessionState 协议；发现这些框架能力不足时，直接补充 BbxCommon 框架并同步本底层规范。

## 位置与职责

- 入口脚本放在项目 Editor 程序集内；配置资产由 `GameStageWindow` 创建在 `Assets/Resources/Editor/`。
- 配置类型必须继承 `GameStageEntryAsset`，从而同时满足 `BbxScriptableObject` 存储规范和窗口发现协议。
- 只声明让 Stage 能正常运行的入口输入。不要把 CSV、ScriptableObject 正式配置整体复制到入口资产。
- `ValidateEntry` 在进 Play Mode 前校验结构性输入。
- `CreateStageGroupBuildCallback()` 返回该入口自带的 `Func<bool>` 构建回调。Launcher 在 Play Mode 的 Editor update 中反复调用；条件未满足时返回 `false`，完成 Group 进入后返回 `true`。
- 构建回调可以等待 GameEngine、初始 StageGroup 或其它仅对 Editor 直达入口成立的特殊条件；不得为了这类条件修改正式 GameEngine 主流程。
- 委托在域重载后的 Play Mode 中由入口代码创建，不作为字段序列化到入口资产。Launcher 统一隔离异常；回调抛出异常时记录日志、注销回调并退出 Play Mode。
- 框架仍兼容只覆写旧 `EnterPlayMode()` 的已有入口：默认构建回调会调用它一次并立即完成。新入口统一覆写 `CreateStageGroupBuildCallback()`，不要继续扩散旧写法。
- Stage 和 System 不得保存或逐帧读取 Editor 入口资产。
- 一个入口资产描述一次完整 Group 激活；需要多个 Stage 时，由运行时 Group 入口一次性把完整集合传给 `SetActiveGameStage`。

## 如何编写入口文件

入口文件命名为 `XxxStageEntryAsset.cs`，放在项目的 Editor 程序集内，例如 `Assets/Scripts/<Project>/Editor/GameStage/`。窗口会自动发现所有非抽象的 `GameStageEntryAsset` 子类，因此不需要修改窗口代码或维护类型注册表。

编写顺序固定如下：

1. 继承 `BbxCommon.Editor.GameStageEntryAsset`；不要直接继承 `ScriptableObject` 或 `BbxScriptableObject`。
2. 只添加让目标 StageGroup 能正常启动的可序列化输入字段。基类已经提供 `Display Name`、`LoadingType` 和 `GroupName`，不要重复声明。
3. 在 `ValidateEntry(out string error)` 中检查不依赖 DataGroup 的结构问题，例如必填 ID、范围、空集合和互斥字段。
4. 实现 `CreateStageGroupBuildCallback()` 并返回 `Func<bool>`。回调中等待项目 GameEngine 或入口特有前置条件；未满足时返回 `false`，满足后构造新的 `XxxStageStartupData`、调用正式具名 Group 入口并返回 `true`。
5. 不要在项目入口自行订阅 `EditorApplication.update`。Launcher 统一负责逐帧调用、完成注销、退出 Play Mode 清理和异常停止。

有必需输入的典型入口文件：

```csharp
using BbxCommon.Editor;
using UnityEngine;

namespace ProjectName.Editor
{
    [CreateAssetMenu(
        fileName = "BattleStageEntry",
        menuName = "ProjectName/GameStage Entry/Battle")]
    public sealed class BattleStageEntryAsset : GameStageEntryAsset
    {
        [Min(1)]
        public int PlayerShipId = 1;

        public override bool ValidateEntry(out string error)
        {
            if (PlayerShipId <= 0)
            {
                error = "Battle entry requires a positive PlayerShipId.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public override System.Func<bool> CreateStageGroupBuildCallback()
        {
            return TryEnterStageGroup;
        }

        private bool TryEnterStageGroup()
        {
            var engine = FindObjectOfType<ProjectGameEngine>();
            if (engine == null)
                return false;

            var startupData = new BattleStageStartupData(PlayerShipId);
            engine.EnterBattleStageGroup(startupData);
            return true;
        }
    }
}
```

模板中的 `ProjectGameEngine`、`BattleStageStartupData` 和 `EnterBattleStageGroup` 必须替换为项目实际类型。`EnterBattleStageGroup` 是运行时 Group 边界：它负责创建完整 Stage 集合并调用一次 `SetActiveGameStage`；入口文件不得自行拼装 Stage，也不得在进入后修改 Component。

无额外输入的入口仍需实现 `ValidateEntry`，并为新入口覆写 `CreateStageGroupBuildCallback()`。若项目 GameEngine 启动时已经自动进入目标 Group，构建回调只记录入口启动并返回 `true`，不做额外切换；不要为统一形式创建空 StartupData。

入口脚本编译完成后，可以在 Game Stage 窗口的 Entries 页点击 `Create Entry` 并选择对应类型，也可以由 Editor 脚本调用 `GameStageWindow.CreateEntryAsset(typeof(XxxStageEntryAsset))`。两种方式都把 `.asset` 创建到 `Assets/Resources/Editor/`；随后填写 `Display Name` 和业务字段，通过窗口 `Play` 或公开的 `GameStageEntryLauncher.Start(...)` 进入 Play Mode。不要把入口资产放到其它路径，也不要手动填写由基类维护的加载设置。

## 脚本启动入口

`GameStageEntryLauncher` 是窗口与自动化脚本共享的唯一 Play Mode 启动 API：

```csharp
var entry = AssetDatabase.LoadAssetAtPath<GameStageEntryAsset>(
    "Assets/Resources/Editor/DirectGame.asset");
var serializedEntry = new SerializedObject(entry);
serializedEntry.FindProperty("PlayerShipId").intValue = 1;
serializedEntry.FindProperty("LevelId").intValue = 1;
serializedEntry.ApplyModifiedPropertiesWithoutUndo();
GameStageEntryLauncher.Start(entry);
```

- `Start(GameStageEntryAsset entry)` 供调用方先配置入口字段再启动。
- `Start(string assetPath)` 供参数已经保存在资产中时直接按路径启动。
- Launcher 只允许从 Edit Mode 启动，复用入口路径校验与 `ValidateEntry`，保存入口资产，通过 `SessionState` 跨域重载创建并执行入口的 StageGroup 构建回调。
- Agent 可以用编辑器代码执行工具直接调用，也可以创建带 `InitializeOnLoad`、`EditorApplication.delayCall` 和唯一 `SessionState` 防重键的临时 Editor 脚本。临时脚本不得自行拼装 Stage 或实现另一套 Play Mode runner。

## Inspector

不要为窗口重复实现业务字段绘制。启用 Odin 时，入口窗口和 `GameStageEntryAssetInspector` 必须调用同一个字段绘制器；窗口为当前入口持有独立的 `PropertyTree`，切换入口时重建，避免复用上一入口的 Odin 缓存。未启用 Odin 时，窗口使用 `Editor.CreateEditor` 和 `OnInspectorGUI` 复用 Unity Inspector。入口专用 Inspector 只过滤由框架固定维护的 `LoadingType` 与 `GroupName`。

入口的 `Display Name` 用于左侧列表和右侧标题，修改后窗口会立即刷新并按新名称排序；留空时回退到资产名。`Create Entry` 始终在 `Assets/Resources/Editor/` 创建资产。

Entries 页左右区域必须各自保留纵向滚动；中间分界线应支持横向拖拽，并限制两侧最小可用宽度。右侧标题栏的 `Delete` 放在 `Play` 旁，只能删除 `Assets/Resources/Editor/` 下的入口资产。删除必须先通过 `EditorUtility.DisplayDialog` 显示入口名称、路径和不可撤销提示，用户确认后才调用 `AssetDatabase.DeleteAsset`；取消不得改变资产或选择，删除成功后刷新列表和选中状态。

## 项目示例

- `InitialPage.asset` 使用无额外输入的 `InitialPageStageEntryAsset`，沿用 GameEngine 默认流程进入初始页面；不要为此创建空 StartupData。
- `DirectGame.asset` 使用 `BattleStageEntryAsset` 编辑 `PlayerShipId` 与 `LevelId`，运行时构造 `BattleStageStartupData` 并调用 `PakGameEngine.StartBattle(startupData)`。`PakGameEngine` 在首次战斗请求时创建 BattleStage，保证编辑入口可以在 Stage 创建前注入数据。
