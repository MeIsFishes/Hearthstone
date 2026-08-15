# 卡牌方形边框与立绘等比缩放任务报告

## 任务结果

已完成。战斗与备战复用的红金、蓝金卡框资源已替换为直边矩形全卡面框；战斗卡框及攻击/目标高亮覆盖完整 `250 × 360` 卡面。战斗、卡池、出战槽、融合槽的立绘均保持原始宽高比，不做横向或纵向拉伸。

## 实际修改

- 美术资源：
  - `Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png`
  - `Assets/Resources/Art/BattleCards/UI/CardFrameBlue-v2.png`
- Unity 配置源与产物：
  - `Assets/Scripts/Hearthstone/Ui/Editor/BattleCardItemUiBuilder.cs`
  - `Assets/Resources/Ui/BattleCardItem.prefab`
- 回归测试：
  - `Assets/Scripts/Hearthstone/Tests/Editor/BattleRulesTests.cs`
- 正式文档：
  - `AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`
  - `AutoDoc/Art/Modules/battle-card/battle-card.md`
  - `AutoDoc/Art/UI/ui-art-overview.md`
  - `AutoDoc/Program/UI/battle/battle.md`

## 检查项结果与证据

| 检查范围 | 状态 | 证据 |
| --- | --- | --- |
| 方形/矩形卡框资源 | 通过 | 两张最终 PNG 均为 `1024 × 1536 / 32bpp ARGB`，轮廓为直顶、直边、直底矩形；中心与画布角落 Alpha 为 0，框体采样 Alpha 为 255 |
| 包裹完整卡面 | 通过 | MCP 现场读取 `BattleCardItem.prefab` 得到 `frameSizeDelta=(0.00, 0.00)`；基础框与两个状态高亮的定向测试通过 |
| 立绘等比缩放 | 通过 | MCP 现场读取 `artworkPreserveAspect=True`；战斗与三个备战卡片 Prefab 的测试均断言 `preserveAspect=true` |
| 信息层级 | 通过 | 测试确认框层同级索引低于生命、攻击和编号标志 |
| 资源框架 | 通过 | 原资源键不变，Controller 继续使用 `ResourceApi.LoadSprite`；没有直接访问 `ResourceManager` |
| Unity 资产流程 | 通过 | 通过当前原工程 `Hearthstone@e97c0c17` 的 Unity MCP 调用 `BattleCardItemUiBuilder.Build()`；未手写 YAML，未修改 `.meta` |
| 代码质量 | 通过 | 生产代码只调整卡框命名和全卡面 `Vector2.zero` 布局；未引入新的一次性生产抽象 |
| 框架边界 | 通过 | 保持 UiBuilder/PrefabUtility 配置源、ResourceApi 资源读取和原 UI 生命周期；无平行运行时布局或内部管理器访问 |
| 玩家视角设计文档 | 通过 | 已更新备战卡池当前可见的完整卡框与立绘等比规则 |
| 美术文档 | 通过 | 已更新 UI 总览与战斗卡模块的直边矩形、全卡面覆盖和真实 Alpha 现状 |
| 程序文档 | 通过 | 已更新战斗 UI 的 `sizeDelta=(0,0)`、`250 × 360` 全卡面框和等比立绘说明 |
| BbxCommon UI 组件文档 | 不适用 | 本次未新增或修改自定义 UI 组件 |

## 验证结果

- Unity MCP 完整目录发现 `mcp__unityMCP__*` 延迟工具，实例资源返回唯一活动实例 `Hearthstone@e97c0c17`。
- MCP 只读闭环：活动 Scene 为 `Main`，路径 `Assets/Scenes/Main.unity`，Scene 未脏；最终 Console error 数量为 0。
- `BattleCardItemUiBuilder.Build()`：MCP 执行成功，Prefab 已保存。
- `validate_script`：Builder 为 0 warning / 0 error；测试脚本为 0 error，只有验证器通用的 GetComponent 空检查提示，实际测试已逐项 `Assert.NotNull`。
- 定向 EditMode：`2 passed / 0 failed / 0 skipped`。
  - `BattleCardPrefabUsesFullCardFramesForEveryCombatState`
  - `PreparationCardPrefabsKeepArtworkAspectAndCoverTheWholeCardWithTheFrame`
- MCP 现场结果：`redAlpha=True, blueAlpha=True, frameSizeDelta=(0.00, 0.00), artworkPreserveAspect=True, frameSprite=CardFrame-v3`。
- 按项目默认未进入游戏；未执行 Game View 人工验收。

## Imagegen 产物与提示词

使用方式：内置 imagegen 编辑模式；没有切换到需要 API Key 的 CLI fallback。

最终项目保存路径：

- `Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png`
- `Assets/Resources/Art/BattleCards/UI/CardFrameBlue-v2.png`

提示词 1（红框）：

```text
Use case: precise-object-edit
Asset type: Unity game UI transparent card-frame sprite
Primary request: 把现有红金拱顶卡框只改成直顶、直边、直底的竖向矩形全卡面框，四边贴近画布，保持轻薄和完全对称。
Constraints: 保留红色珐琅、暖金金属、宝石与装饰质感；中心和框外真实透明；无卡面原画、文字、编号、徽章或水印。
```

提示词 2（蓝框）：

```text
Use case: precise-object-edit
Asset type: Unity game UI transparent card-frame sprite
Primary request: 保持新红框的矩形几何、装饰位置、比例与透明区域不变，只把红色珐琅和红宝石替换为现有蓝框的皇家蓝珐琅与蓝宝石材质。
Constraints: 暖金金属、直边轮廓、边框厚度和全部装饰位置不变；中心和框外真实透明；无额外元素或水印。
```

内置工具两次把透明预览棋盘误烘焙为 RGB。未采用这些 RGB 文件；对最终红蓝生成稿仅执行确定性的棋盘背景 Alpha 修复，之后通过像素格式与 Unity 导入 Alpha 双重验证再覆盖原同名 PNG。

## 执行偏差与处理

- 初次尝试启动新的原工程 batchmode Builder 时，Unity 报告项目已被两个现有 `MCPForUnity.Editor.McpCiBoot.StartStdioForCi` Editor 进程占用。
- 曾创建 `AutoDoc/Temp/CardFrameSquare-UnityProject` 隔离副本，但按用户指示立即停止并完整删除；最终没有从隔离工程复制任何产物。
- 用户明确要求使用 MCP 后，从 `ALL_TOOLS` 发现 Unity MCP 延迟工具，并直接在原工程活动实例完成刷新、Builder、测试和验收。
- 刷新域重载后出现一次 `No Unity Editor instances found`；按故障矩阵重新读取实例并设置精确 `Name@hash` 后恢复，随后写操作和两项只读验收均成功。
- 当前工作区原先已有大量用户与并行任务改动；本任务没有回退它们。Prefab 重建使用当前 Builder，因此保留并序列化了 Builder 中既有的关键词区域配置。

## 未解决风险

- 未进入 Game View 进行人工观感验收；现有证据覆盖图片透明度、Unity 导入、Prefab 几何、层级和等比参数。若需要评估小尺寸下边框装饰是否过密，可另行进行一次游戏画面视觉验收。

## 文档与清理结果

- 检查清单：`AutoDoc/Temp/CardFrameSquare-Checklist.md`，所有实现与验证项已复核。
- `AutoDoc/CleanupTempDocs.bat`：仅运行一次，退出码 `0`。
- 清理时 `AutoDoc/Temp` 中 Markdown 数量为 99，低于脚本阈值 500，因此脚本未删除现有文档。
- 隔离 Unity 临时工程已删除，确认路径不存在。
