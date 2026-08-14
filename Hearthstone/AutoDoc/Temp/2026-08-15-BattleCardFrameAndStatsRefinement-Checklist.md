# 战斗卡框与属性徽章精修检查清单

## 用户要求

- [x] 通过：敌我双方主体窄框从 `250 × 360` 收缩为 `210 × 328`，左右各内缩 `20`、上下各内缩 `16`；底部属性徽章会露出主体框。
- [x] 通过：生命绿色血滴位于左下，锚点 `(0,0)`、中心 `(30,30)`；攻击力位于右下，锚点 `(1,0)`、中心 `(-30,30)`。
- [x] 通过：攻击徽章改用无剑 `AttackBadgeFrame.png`，保留红宝石盾形外框；生命与攻击数字均为 `30` 号白色粗体并带深色 `Outline`。

## 资源与 UI 实现

- [x] 通过：核对原配置为满卡面框、左攻击剑徽章、右生命血滴、`23` 号文字；攻击数字颜色为深红，在红色剑盾图上对比不足。
- [x] 通过：按 `imagegen` 以 `AttackSwordBadge.png` 为编辑目标生成无剑红金盾框；第一版出现烘焙棋盘格后未采用，第二次只执行背景提取，得到真实透明版本。
- [x] 通过：最终新资产保存为 `Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png`，保留旧资产不覆盖；尺寸 `1254 × 1254`，Alpha 范围 `0~255`，抽样透明像素 `48735/98596`。
- [x] 通过：`BattleCardItemUiBuilder` 统一维护三层框内缩、徽章位置与 Sprite、TMP 字号/粗体/白色、`Outline`，并校验三张相关图片的 Sprite/Alpha/Mipmap/WrapMode 导入设置。
- [x] 通过：当前会话完整工具目录确认 `mcp__unityMCP__execute_code` 可用；前置活动场景与 Console 检查通过，并由 `execute_code` 成功调用 `Hearthstone.BattleCardItemUiBuilder.Build()`。
- [x] 通过：`BattleCardItemView/Controller` 序列化引用与阵营窄框切换保持不变；未修改 ECS 数据、战斗结算、列表槽位或 UiScene 导出配置。
- [x] 通过：未新增不必要的业务抽象；新增 `ConfigureBadge` 与 `LoadSprite` 由同一 Builder 内生命/攻击/框资源复用。

## 框架边界与 Skill

- [x] 通过：遵循 `project-state-preflight` 的项目修改、文档同步、审计、清理和报告流程。
- [x] 通过：遵循 `bbxcommon-ui`；静态结构与导入约束落在 Prefab/一一对应 UiBuilder，Controller 继续只负责动态数据和阵营表现。
- [x] 通过：遵循 `imagegen`；本地编辑目标已先用 `view_image` 查看，使用内置图像编辑，最终资产已保存到工作区，提示词与生成偏差记录在报告。
- [x] 通过：框架边界审计未发现运行时拼装静态 UI、绕过 View/Controller、直接改 UiSceneAsset、平行资源加载或手写 Unity 资产 YAML。
- [x] 不适用：未新增或修改 `BbxUiItem` 自定义组件，无需更新 `AutoDoc/UIItem/`。

## 验证

- [x] 通过：最终资源检查确认无剑盾框、真实透明外缘、无文字/数字/水印，Unity 导入类型为 Single Sprite、`alphaIsTransparency=true`、Mipmap 关闭。
- [x] 通过：MCP Prefab 探针返回根 `250×360`、框 `210×328`、生命左/攻击右、攻击 Sprite `AttackBadgeFrame`、两项文字 `30/Bold/White`、两个 Outline 均存在。
- [x] 通过：新增两项 UI EditMode 测试 `2/2` 通过，覆盖主体框尺寸、状态框一致性、属性左右位置、新攻击 Sprite 与数字可读性。
- [x] 未通过（非本任务引入）：完整 `Hearthstone.Tests` 共执行 16 项，15 通过、1 失败；失败为并发更新的 `BattleCardCsvData.csv` 已将默认卡原画键改为 `Boar_001`，旧测试仍断言 `Boar`。CSV 修改时间晚于本次测试文件，且同时存在 `Boar_001.png`，本任务未回退或改写该无关配置。
- [x] 通过：最终活动场景为 `Assets/Scenes/Main.unity`、`isDirty=false`，Editor idle；最终错误 Console 为 0。
- [x] 不适用：按项目默认授权未进入 Play Mode；未执行游戏内攻击时序与目标分辨率的目视回归。

## 文档同步门槛

- [x] 通过：完整读取 `design-doc-format`、战斗系统子格式、`art-doc-writer`、UI/模块美术格式、`program-doc-format` 与 UI 界面格式。
- [x] 通过：更新 `AutoDoc/Design/Specific/combat-system/combat-system.md`，记录主体窄框、框外左生命/右攻击与粗体描边数值。
- [x] 通过：更新 `AutoDoc/Art/UI/ui-art-overview.md` 与 `AutoDoc/Art/Modules/battle-card/battle-card.md`，记录无剑攻击框新资产、规格、位置、透明与历史资源状态。
- [x] 通过：更新 `AutoDoc/Program/UI/battle/battle.md`，记录 `210×328` 三层框、徽章锚点、TMP/Outline 和 Builder 导入约束。
- [x] 不适用：项目级美术风格未变化；只更新战斗 UI 风格组和战斗卡牌模块文档。

## 结束审计

- [x] 通过：已重新打开并按当前资源、Prefab、Builder、测试、文档、MCP 场景与 Console 证据逐项核对。
- [x] 通过：将在本清单复核完成后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，实际退出结果写入报告。
- [x] 通过：清理后创建 `AutoDoc/Temp/2026-08-15-BattleCardFrameAndStatsRefinement-Report.md`。
