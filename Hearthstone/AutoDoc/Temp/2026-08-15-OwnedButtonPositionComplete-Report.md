# “查看拥有”按钮位置调整完成报告

## 任务结果

“查看拥有”按钮已从 `(-720, 285)` 移至 `(-770, 285)`，尺寸保持 `240 × 42`。Unity 重载 Prefab 后计算得到按钮左边缘为 `-890`，与 `1780 px` 宽卡池面板的左边缘 `-890` 完全贴齐；标签仍为“查看拥有”，Toggle 与 Label 序列化引用有效。

## 实现与框架边界

- 只修改 `PreparationViewUiBuilder` 中 `OwnedOnlyToggle` 的 X 坐标，并同步更新直接依赖的 EditMode 测试断言。
- 通过 Unity MCP 实际调用 `Hearthstone.PreparationViewUiBuilder.Build()`，返回 `PreparationView rebuilt`，由 Builder 经 Unity Editor API 重建 `PreparationView.prefab`。
- 未在 View/Controller 中增加运行时布局补丁，未手写 Prefab 或 `UiSceneAsset` YAML，未新增函数或字段。
- 该变化仅涉及 View Prefab 内部子控件坐标，不影响 UiScene Group、默认显隐、整体 Position/Scale/Pivot 或导出路径，因此无需重新导出 UiScene。
- 未创建、编辑或删除相关 `.meta`；工作区其他既有改动保持不动。

## 验证结果

| 检查 | 结果 |
| --- | --- |
| Unity 编译 | 通过；最终 Console 错误为 0 |
| Builder 执行 | 通过；返回 `PreparationView rebuilt` |
| Prefab 实际坐标 | `(-770, 285)` |
| Prefab 实际尺寸 | `240 × 42` |
| 左边缘关系 | 按钮 `-890`，卡池面板 `-890` |
| 标签与引用 | “查看拥有”；Toggle/Label 引用有效 |
| EditMode 测试 | `Hearthstone.Tests.RunCardRulesTests.PreparationSharedCardAndResourcesAreFullyExported` 通过 |
| Editor 状态 | 活动场景仍为 `Assets/Scenes/Main.unity`，未进入 Play Mode |

## 文档同步

- 玩家视角文档已记录“查看拥有”横框与卡池面板左边缘贴齐。
- 模块美术文档已同步该左上角对齐关系。
- 备战界面程序文档已记录局部坐标、尺寸与 `-890` 左边缘关系。

## 执行偏差与风险

无执行偏差。未进行游戏内 Play Mode 视觉验收；本次以 Builder 重建、Unity Prefab 实际读取、相关 EditMode 测试和 Console 检查完成结构验收。

## 清理

已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0；本报告在清理后创建。
