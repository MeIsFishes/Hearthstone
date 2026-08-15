# 备战卡池复用战斗卡片 UI 检查清单

## 需求与实现

- [通过] 备战卡池复用战斗页面的卡片视觉 UI，不再单独维护一套卡片主体表现。
  - 证据：`PreparationController` 的卡池只创建和取得 `BattleCardItemController`；共享预加载路径为 `Ui/BattleCardItem`。
- [通过] 保留备战卡池专属的未拥有状态、素材选中状态、拖拽与交互能力。
  - 证据：`BattleCardItemView` 和 `BattleCardItem.prefab` 已包含 Preparation Empty/Material/Dragable/Interactor/EmptyAttempt 引用，Controller 按绑定模式开关。
- [通过] 将备战卡池、出战槽、融合槽及其列表槽位尺寸统一到战斗卡片的宽高比例，并校正容器与间距，避免非等比拉伸。
  - 证据：共享卡 `250 × 360`；卡池槽 `200 × 288`；出战槽 `220 × 316.8`；融合槽 `190 × 273.6`，均为 `25:36`。
- [通过] 备战卡池继续通过 `UiList` 和预加载映射创建条目，不在运行时拼装静态 UI。
  - 证据：`UiList.AddItem<BattleCardItemController>()` 与既有 `BattleCardItemController → Ui/BattleCardItem` 映射。
- [通过] View 只保存序列化引用，数据绑定、交互和刷新逻辑留在 Controller。
  - 证据：新增 View 字段均为组件引用；绑定、数据刷新、状态清理和事件处理位于 `BattleCardItemController`。
- [通过] 调整与 Prefab 一一对应的 UiBuilder，并通过允许的 Unity Editor 通道重建/核验 Prefab；若当前环境缺少执行通道，明确记录未完成项，不手写 Prefab YAML。
  - 证据：通过已连接 Unity Editor 调用四个公开 `Build()`，结构校验返回成功；Prefab 均由 Editor API 保存。
- [通过] 检查相关预加载映射、页面引用和直接依赖，确保重入与对象池复用时状态正确清理。
  - 证据：共享 Controller 的 `ResetBinding()` 解除五个监听、清除两种绑定数据、恢复缩放并关闭另一上下文状态；资源加载与预加载断言通过。
- [通过] 检查新增函数和字段是否确有复用价值，删除不必要的一次性抽象。
  - 证据：卡面内容、显示/隐藏和绑定清理集中复用；旧 Preparation Builder 仅保留兼容入口并转调共享 Builder，不再维护视觉层级。

## 验证

- [通过] 执行适用的编译或 Editor 测试，验证备战卡池与战斗卡片共享视觉实现且原有交互未回归。
  - 证据：Unity 编译后 Console 0 Error；直接受影响 7 项测试通过；`RunCardRulesTests` 19/19、`BattleKeywordRulesTests` 9/9 通过。整类 `BattleRulesTests` 的既有 `Boar`/`Boar_001` 数据断言失败与本次修改无关。
- [通过] 检查改动范围，确保未修改 `.meta` 文件且未误改无关文件。
  - 证据：`git diff --name-only -- '*.meta'` 无输出；未处理并保留同时存在的 Loading/UI Builder 任务文件。
- [通过] 框架边界审计：未绕过 BbxCommon UI 生命周期、UiList 预加载池、Prefab/Builder 配置源或资源 API，未残留平行卡片视觉实现。
  - 证据：活跃卡池路径唯一使用共享 `BattleCardItemController`；静态层级来自一一对应 Builder，动态条目来自 `UiList`。旧序列化 Prefab/类型仅为既有资产兼容保留，不在活跃路径中，Builder 已不再维护其独立视觉。

## 文档同步

- [通过] 玩家视角设计文档：已读取 `design-doc-format`，更新 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md` 的共享卡面和 `25:36` 现状。
- [通过] 美术文档：已读取 `art-doc-writer` 与模块格式，重整并更新 `AutoDoc/Art/Modules/preparation-card-pool/preparation-card-pool.md`；通用 UI 文档现有分组无需改动。
- [通过] 程序文档：已读取 `program-doc-format` 与 UI 界面格式，更新备战和战斗 UI 程序文档的共享 Prefab、预加载、绑定清理和比例现状。

## 收尾

- [通过] 逐项复核并补充状态与证据。
- [通过] 只运行一次 `AutoDoc/CleanupTempDocs.bat` 并记录结果。
  - 证据：脚本实际执行一次，`CleanupExitCode=0`；此前一次 PowerShell 外层语法解析失败，脚本未启动，不计执行次数。
- [通过] 清理后创建 `AutoDoc/Temp/PreparationCardUiReuse-Report.md`，记录结果、验证、偏差、风险、文档与清理情况。
