# 备战卡池“查看拥有”筛选任务报告

## 任务结果

已在备战卡池左上角加入默认未勾选的“查看拥有”Toggle。未勾选时卡池继续按 `01~148` 展示全部固定编号；勾选时复用同一个 `UiList`，仅按 `RunStateSingletonRawComponent.HasCard()` 保留当前拥有卡，并保持编号升序。筛选切换会按实际条目数重算 Content 高度、停止滚动惯性并复位到顶部。

项目内没有语义、比例和状态均适合的通用勾选框 Sprite，因此使用 imagegen 生成透明蓝金方形底板。为遵守不新增或修改 `.meta` 的约束，复用了已确认无代码与 Prefab 引用的旧 `PreparationTabSelected.png` 文件及其既有 GUID；当前页签仍使用 `PreparationTabSelectedV2.png`。生成图为 `1254 × 1254` RGBA，角点 Alpha 为 `0`，中心 Alpha 为 `253`。

## 检查项与证据

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 左上角默认未勾选 Toggle | 通过 | Unity 读取 Prefab：`label=查看拥有`、`isOn=False`、位置 `(-720, 285)`、尺寸 `240 × 42` |
| 未勾选完整卡池、勾选仅拥有 | 通过 | `PopulateCardPoolItems()` 保持 `FirstCardNumber..LastCardNumber` 循环，仅在 `m_ShowOwnedOnly` 为真时跳过 `HasCard()==false` |
| 编号升序与 99 号边界 | 通过 | 同一递增循环直接绑定条目；99 号没有拥有实例时会被拥有筛选自然排除 |
| 复用同一 UiList 与对象池 | 通过 | 仅调用既有 `CardPoolList.ItemWrapper.ClearItems()` / `AddItem<BattleCardItemController>()`，未新增第二套列表或条目类型 |
| 动态高度与滚动复位 | 通过 | 按实际 `itemCount` 计算行数，Content 高度不低于 viewport；调用 `StopMovement()` 并设置 `verticalNormalizedPosition=1` |
| 拥有状态变化同步 | 通过 | Run Revision 下使用固定 `bool[]` 快照检测拥有成员变化；仅筛选开启且成员变化时重建，普通出战槽变化只刷新条目 |
| View / Controller 职责 | 通过 | View 仅新增 Toggle/TMP 引用；监听、状态与重建全部位于 Controller；权威数据仍来自 Run/Preparation Component |
| Builder / Prefab 同步 | 通过 | Unity MCP 成功执行 `Hearthstone.PreparationViewUiBuilder.Build()`；Prefab 现场读取引用和布局均正确 |
| BbxCommon 框架边界 | 通过 | 保留现有 `UiList`、预加载条目、对象池、`ScrollRect`、View/Controller 生命周期与 `ResourceApi` 路径 |
| 修改范围与 `.meta` | 通过 | `git diff --name-only -- '*.meta'` 无输出；未回退或覆盖工作区其他既有修改 |
| 分配与刷新审计 | 通过 | 拥有集合比较复用定长数组，无逐 Revision 临时集合；只在筛选结果成员变化时重建 |
| 定向测试 | 通过 | 两条筛选/Prefab EditMode 测试 `2/2` 通过 |
| 关联测试 | 通过 | `Hearthstone.Tests.RunCardRulesTests` 全类 `23/23` 通过 |
| C# 编译 | 通过 | `Hearthstone.csproj`、`Hearthstone.Ui.Editor.csproj`、`Hearthstone.Tests.csproj` 串行构建均为 `0` 错误 |
| Unity Console | 通过 | 强制刷新并编译后读取 Error Console 为 `0` 条 |
| 玩家视角设计文档 | 通过 | 已同步默认状态、完整/拥有筛选、升序与列表回顶体验 |
| 美术文档 | 通过 | 已同步蓝金透明底板、浅金勾、尺寸、资源复用来源和真实路径 |
| 程序文档 | 通过 | 已同步 Toggle 监听、动态列表重建、快照刷新、布局和资源加载方式 |

## 验证结果

- Unity Prefab 读取：Toggle 引用存在，标签为“查看拥有”，默认关闭；Toggle 尺寸 `240 × 42`，滚动区 `1650 × 510`，底板 Sprite 为 `PreparationTabSelected`、Sprite Rect 为 `1254 × 1254`。
- 编译：三个相关 C# 项目全部成功，仅保留工程原有程序集版本警告。
- 测试：定向 `2/2`、完整 `RunCardRulesTests` `23/23` 通过。
- 差异检查：任务相关 C# 与文档没有 `git diff --check` 问题；Unity 生成 Prefab 中既有 YAML 空字段尾随空格不属于本次手写代码问题。

## 偏差与未解决风险

- 生成底板沿用旧资源名 `PreparationTabSelected.png`，名称与当前用途不完全一致；这是为了在用户已有脏工作区内遵守“不创建、编辑或删除 `.meta`”约束。程序和美术文档已明确该兼容关系，当前页签不引用此资源。
- 按项目指示未进入游戏 Play Mode；视觉结果通过生成图透明通道、Prefab 现场结构与 EditMode 测试验证。实际运行中的最终观感仍可由用户在当前打开的 Unity 中直接查看。

## 清理结果

按要求只运行一次 `AutoDoc/CleanupTempDocs.bat`。运行前后 `AutoDoc/Temp/` 的 Markdown 数量均为 `151`，脚本退出码为 `0`，未删除文件。
