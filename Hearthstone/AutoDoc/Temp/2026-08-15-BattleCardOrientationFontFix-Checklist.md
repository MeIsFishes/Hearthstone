# 战斗卡牌朝向与字体修复检查清单

## 用户要求

- [x] **通过**：敌方卡牌不再旋转，敌我双方卡面均保持正常正向。证据：`BattleCardItemController.RefreshAll()` 与 `Unbind()` 均写入 `Quaternion.identity`，源码中不存在 `180f` 或 `Quaternion.Euler`。
- [x] **通过**：修复战斗界面文字因字体缺少中文字符而显示方框的问题。证据：两个战斗 Prefab 的 9 个 TMP 文本全部引用 `NotoSansSC-Dynamic SDF`，当前全部中文字符覆盖检查为 true。

## UI 与资源流程

- [x] **通过**：已定位 `BattleCardItemController`、`BattleView.prefab`、`BattleCardItem.prefab` 与全部 TMP 文本引用；原字体为不含中文的 `LiberationSans SDF`，`DeadText` 原为 null。
- [x] **通过**：保持 `BattleCardItemController + UiList` 生命周期，不在 Entity 或运行时另建 UI 表现路径；未修改 ECS 数据与列表创建入口。
- [x] **通过**：卡面朝向仍由 Controller 在刷新和解绑时确定性写为单位旋转，回池/换绑不会残留旧旋转。
- [x] **通过**：通过官方 Unity MCP 的 Unity Editor API 导入 `NotoSansSC-VF.ttf`、创建 Dynamic/Multi Atlas TMP 字体资产并写入两个 Prefab；`.asset`、Prefab 与 `.meta` 均未由文件工具手写。
- [x] **不适用**：页面 Group、DefaultShow、场景级位置/缩放/Pivot 与导出条目均未变化，因此未修改或重新导出 `Battle.unity` / `Battle.asset`。

## 验证

- [x] **通过**：Unity MCP v10.0.0 配置、当前实例、活动场景与前置 Console 均验证成功；前置场景为未脏 `Main.unity`，error 0。
- [x] **通过**：两个 Prefab 根节点均为 `(0,0,0)`；Controller 刷新与解绑路径均写单位旋转。
- [x] **通过**：`NotoSansSC-Dynamic SDF` 为 Dynamic population、Multi Atlas；预置当前中文字符且 `HasCharacters()` 为 true，9/9 个战斗 TMP 文本绑定成功。
- [x] **通过**：脚本标准诊断 0 error、0 warning；EditMode `Hearthstone.Tests` 12/12 通过，任务 ID `d366cb0c01f24bcd98f04e70b98aa10e`。最终 Console 仅保留 Unity Test Runner 保存结果文件的无堆栈 Exception 类型记录，与测试结果一致，不是编译或运行失败。
- [x] **通过**：最终活动场景仍为未脏 `Assets/Scenes/Main.unity`；本任务未进入 Play Mode。

## 文档同步

- [x] **通过**：完整读取 `design-doc-format`、`art-doc-writer`、`program-doc-format`，以及战斗设计、战斗程序、UI 程序、美术风格/UI/模块格式，并核对六篇现有相关文档。
- [x] **通过**：更新 `AutoDoc/Design/Specific/combat-system/combat-system.md`，记录敌我正向与中文正常显示的当前玩家体验。
- [x] **通过**：更新美术风格总览与 UI 美术总览，记录正向阅读、蓝红阵营区分及 Noto Sans SC；battle-card 模块文档未包含朝向或字体事实，无需修改。
- [x] **通过**：更新 `AutoDoc/Program/UI/battle/battle.md`，记录 Controller 单位旋转和 Dynamic/Multi Atlas 字体资源；战斗系统玩法程序链路未变化，无需修改。

## 框架边界与收尾

- [x] **通过**：未绕过 BbxCommon UI、TMP 资源、Unity Editor 资产流程或现有导出配置源；资源字典通过项目构建器重建。
- [x] **通过**：仅删除条件旋转并新增资源覆盖测试，没有引入一次性函数、字段、平行状态或直接底层 Manager 访问。
- [x] **通过**：已逐项复核；字体导入、9 个 Prefab 引用、中文覆盖、源码朝向、测试、文档和最终 Editor 状态均有证据。
- [x] **通过**：仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0；清理后创建对应实施报告。
