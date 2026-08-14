# 卡面边框与描述区微调检查清单

- [ ] 读取当前卡框、卡牌 Prefab、相关美术文档与适用 skill，确认修改边界。
- [ ] 以现有红金卡框为编辑目标生成更轻薄的透明 V3 边框，保留整体风格、透明中心、2:3 比例和无文字要求。
- [ ] 扩大卡牌描述区，并相应调整原画裁切视窗，确保原画仍覆盖完整上半区且不拉伸。
- [ ] 通过 Unity MCP 导入 V3 Sprite、更新 `BattleCardItem.prefab` 引用与布局，不手写 Prefab/`.asset`/`.meta`。
- [ ] 为我方生成同造型轻薄蓝框变体，并同步敌我运行时资源键。
- [ ] 仅执行 Editor 静态验收：Prefab 层级、RectTransform、Sprite 引用、Alpha、活动场景与错误 Console；不进入 Play Mode。
- [ ] 框架边界审计：静态表现保留在 View Prefab，不在 Controller 中新增运行时 UI 构建，不改变 UiScene 导出字段。
- [ ] 玩家视角设计文档：读取基础与战斗专项格式，按实际视觉变化决定并同步。
- [ ] 美术文档：读取基础与 UI/模块格式，按 V3 边框和新布局同步资产与规格。
- [ ] 程序文档：读取基础与 UI 界面格式，按实际 Prefab 布局同步。
- [ ] 结束前逐项复核证据；仅运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建同名报告。
