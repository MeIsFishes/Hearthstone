# 战斗场景角落装饰任务检查清单

- [通过] 用户要求：右下角已放置羽毛笔与实体印章组合；左上角匕首斜放并只露出越界裁切后的主体，按用户补充要求通过 `localScale.x = -1` 让刀尖朝向左下。
- [通过] 美术风格：两张素材以当前战场底板和旧化结算资产为参考，采用正俯视、轻油画笔触、象牙暖褐、胡桃木、旧钢和低饱和古金材质；已逐张查看最终图片。
- [通过] 画面与交互：`UpperLeftDagger` 与 `LowerRightQuillStamp` 分别使用左上/右下角锚点，均不接收射线并位于敌我卡牌列表下层；卡牌仍保持视觉和交互优先。
- [通过] 素材生成约束：使用内置 imagegen 生成并定向修正两张素材；因生成器把透明预览棋盘格烘入 RGB，改为 imagegen 绿幕版本后确定性提取 Alpha。最终工作区 PNG 为 `Format32bppArgb`，四角 Alpha 均为 `0`、中心为 `255`，无文字、商标或水印。
- [通过] 项目接入：新增素材位于 `Assets/Resources/Art/BattleCards/UI/`；只修改一一对应的 `BattleViewUiBuilder`，并通过 Unity MCP 正式调用 `Build()` 重建 Prefab。`.meta` 由 Unity 自动生成，未手工编辑。
- [通过] 验证：Unity Console 无编译错误；EditMode 测试 `Hearthstone.Tests.BattleRulesTests.BattleViewUsesCornerDecorationsBehindCards` 为 `1/1 Passed`，固定检查 Sprite、锚点、位置、尺寸、匕首镜像、射线与层级；PNG Alpha 和导入设置静态检查通过。按项目默认未进入游戏。
- [通过] 无关改动审计：任务文件状态核对只涉及两张新素材及其 Unity 元数据、资源字典、`BattleView` Prefab/Builder、对应测试和直接相关正式文档；未回退或整理工作区原有大量并行改动。
- [通过] 框架边界审计：静态装饰继续由 `BattleView` Prefab 与一一对应 UiBuilder 维护；未在 Controller 运行时拼装、未手写 Prefab/Scene/UiSceneAsset YAML、未修改 UiGroup 或导出 Asset。
- [通过] 代码质量：`CreateCornerDecorations()` 与 Builder 既有 `CreateVictoryBanner()`/`CreateResultPopup()` 结构一致，集中维护两张同类静态装饰；没有新增 View 字段、Controller 逻辑或一次性运行时抽象。
- [通过] 玩家视角设计文档：已完整读取 `design-doc-format` 与战斗系统专项格式，更新 `AutoDoc/Design/Specific/combat-system/combat-system.md` 第 8 章，记录当前可见角落装饰、方向、裁切与非交互现状。
- [通过] 美术文档：已完整读取 `art-doc-writer`、美术风格总文档、UI 美术与模块美术格式；更新 `art-style-overview.md`、`ui-art-overview.md` 和 `battle-card.md`，记录当前风格、资产路径、透明规格和接入用途。两张资产是战斗专属 UI 装饰，不建立跨模块物件规格分类。
- [通过] 程序文档：已完整读取 `program-doc-format` 与 UI 界面格式，更新 `AutoDoc/Program/UI/battle/battle.md`，记录 Prefab 节点、资源、锚点、尺寸、镜像、射线、层级和 Builder 配置源。
- [通过] 结束审计：已重新读取本清单并依据最终资源、Prefab、Builder、测试结果、Unity 状态和文档逐项复核；可修正缺口均已修正。
- [通过] 临时文档清理与报告：结束审计后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`；随后创建同任务名 `*-Report.md` 记录完整结果。
