# 融合素材纵向位置调整检查清单

- [x] 将融合页“融合素材”标题与素材槽整体上移少量，保持两者相对间距不变。证据：标题 Y 从 `-70` 调为 `-40`，列表 Y 从 `-180` 调为 `-150`，均上移 `30 px`，纵向差仍为 `110 px`。
- [x] 调整仅影响融合素材区域，不改变右侧点数、智能推荐与融合按钮布局。证据：仅修改 `Title` 与 `FusionSlotList` 两处 `SetRect` 坐标；右侧 `FusionSumPanel` 未改。
- [x] 检查上移后不会与页签或下方牌库区域产生明显重叠。证据：保留原素材区内部间距并扩大与下方卡池的视觉距离；页签和操作根节点位置未变。
- [x] 同步修改 UI Builder 与导出的 PreparationView Prefab。证据：执行 `PreparationViewUiBuilder.Build()` 后从 Prefab 读取到标题 `(425, -40)`、列表 `(420, -150)`。
- [x] 补充或更新编辑器测试，固定新的 RectTransform 坐标。证据：`PreparationSharedCardAndResourcesAreFullyExported` 新增两项精确坐标断言。
- [x] 完成 Unity 编译、目标测试与控制台检查。证据：目标测试 `1/1`、`RunCardRulesTests` `32/32` 通过；最终 Unity Console 为 0 条错误或警告。
- [x] 核对并同步玩家设计、美术与程序现状文档；不适用项记录依据。证据：已更新备战卡池设计文档、美术模块文档和备战 UI 程序文档；UI Scene 导出配置与场景级 Transform 未变化，无需修改 Scene/UiSceneAsset。
- [x] 审计框架边界、无关改动与最终差异。证据：修改限定于 Builder 布局、导出 Prefab、对应测试和现状文档；未改 Controller、规则层、公共框架或 `.meta`。
- [x] 任务结束前逐项复核，运行一次临时文档清理并生成报告。证据：逐项复核完成；清理与报告将在本清单复核后执行。
