# 卡牌方形边框与立绘等比缩放任务检查清单

## 用户要求

- [x] **通过**：红金 `CardFrame-v3.png` 与蓝金 `CardFrameBlue-v2.png` 已替换为直顶、直边、直底的矩形全卡面框；Unity 导入后保持原资源键。
- [x] **通过**：战斗卡 `CardFrameOverlay`、`AttackerHighlight`、`TargetHighlight` 的 RectTransform 均为四边拉伸且 `sizeDelta = (0, 0)`；备战三个卡片 Prefab 原本即覆盖完整卡面容器。
- [x] **通过**：战斗、卡池、出战槽、融合槽的 `ArtworkArea.preserveAspect` 均为 `true`；定向 EditMode 测试覆盖这些 Prefab。
- [x] **通过**：未修改卡牌数据、交互或文字业务逻辑；框层仍位于生命、攻击与编号标志下方。

## 实现与资源

- [x] **通过**：定位并核验 `BattleCardItemUiBuilder`、四类卡牌 Prefab/Builder、Controller 的 `ResourceApi.LoadSprite` 引用以及红蓝卡框 PNG。
- [x] **通过**：Prefab 由原工程活动 Unity Editor 中的 `BattleCardItemUiBuilder.Build()` 通过 MCP 执行并保存；未手写 Scene、Prefab 或 `.asset` YAML，未创建、编辑或删除 `.meta`。
- [x] **通过**：使用内置 imagegen 编辑红框并制作同轮廓蓝框；原始输出误烘焙棋盘格后仅做确定性 Alpha 格式修复。最终两张项目 PNG 均为 `1024 × 1536 / 32bpp ARGB`，中心与画布角落 Alpha 为 0，框体 Alpha 为 255。最终提示词意图为：把现有红金拱顶框改为直边矩形全卡框，保留材质与透明中心；以新红框几何和旧蓝框配色制作相同轮廓蓝金版本。
- [x] **通过**：战斗 Builder 的全卡面布局配置已落入 `Assets/Resources/Ui/BattleCardItem.prefab`；备战卡池、出战槽与融合槽继续复用同名资源并保持全容器拉伸。
- [x] **通过**：资源键仍为唯一文件名 `CardFrame-v3` 与 `CardFrameBlue-v2`；业务 Controller 继续通过 `ResourceApi.LoadSprite` 读取，没有绕过 `ResourceApi`。
- [x] **不适用**：本次未新增或修改自定义 BbxCommon UI 组件，只修改业务 Prefab Builder 的静态布局和已有位图资源，因此无需更新 `AutoDoc/UIItem/`。

## 代码与框架质量

- [x] **通过**：只重命名卡框路径常量、把框尺寸改为 `Vector2.zero` 并补充直接回归测试；没有引入一次性生产抽象。
- [x] **通过**：检查了战斗与备战直接复用点；MCP 重建使用当前工作区 Builder，保留用户在同文件中的既有关键词区域改动和其他并行修改，没有回退无关文件。
- [x] **通过**：框架边界审计确认资源继续走 `ResourceApi`，Prefab 继续走项目 `UiBuilder` 与 `PrefabUtility` 配置源，Unity 操作走正式 MCP；没有平行运行时布局、手写导出产物或内部资源管理器访问。

## 验证

- [x] **通过**：MCP 现场核验返回 `frameSizeDelta=(0.00, 0.00)`、`frameSprite=CardFrame-v3`，层级测试确认三个框层低于生命、攻击与编号标志。
- [x] **通过**：MCP 现场核验返回 `artworkPreserveAspect=True`；`BattleCardPrefabUsesFullCardFramesForEveryCombatState` 与 `PreparationCardPrefabsKeepArtworkAspectAndCoverTheWholeCardWithTheFrame` 均通过。
- [x] **通过**：`validate_script` 对 Builder 为 0 warning/0 error、对测试为 0 error（仅有通用 GetComponent 空检查提示）；定向 EditMode 结果 `2 passed / 0 failed / 0 skipped`；Unity Console 最终为 0 error。
- [x] **通过**：按项目默认未进入游戏；验证边界为 PNG 像素/Alpha 检查、Unity 导入状态、Prefab MCP 现场检查与 EditMode 测试。
- [x] **通过**：工作区原有大量用户与并行任务改动均保留；本任务仅增量修改两张卡框 PNG、战斗卡 Prefab 的当前 Builder 结果、相关 Builder/测试和四篇直接相关正式文档。

## 文档同步

- [x] **通过**：玩家视角设计文档已完整读取 `design-doc-format` 及战斗系统格式，检查现有 `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`，并在该现状文档补充完整卡框与立绘等比显示规则；未为单一视觉调整扩张新的战斗玩法文档。
- [x] **通过**：美术文档已完整读取 `art-doc-writer`、模块与 UI 总览格式，更新 `AutoDoc/Art/Modules/battle-card/battle-card.md` 和 `AutoDoc/Art/UI/ui-art-overview.md` 的直边矩形、全卡面覆盖与真实 Alpha 现状。
- [x] **通过**：程序文档已完整读取 `program-doc-format` 与 UI 界面格式，更新 `AutoDoc/Program/UI/battle/battle.md` 的 `sizeDelta=(0,0)`、`250 × 360` 全卡面框和立绘等比行为；备战程序逻辑未变化，现有备战程序文档已检查，无需写入新的内部流程。

## 收尾

- [x] **通过**：已重新打开并逐项依据 PNG、源码、MCP、测试和正式文档证据复核本清单。
- [x] **通过**：仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`；当时 Temp Markdown 数量为 99，低于删除阈值。
- [x] **通过**：清理后已创建 `AutoDoc/Temp/CardFrameSquare-Report.md`，记录任务结果、证据、验证、偏差、风险、文档处理与清理结果。
