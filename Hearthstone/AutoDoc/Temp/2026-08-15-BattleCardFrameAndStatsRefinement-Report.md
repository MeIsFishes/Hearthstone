# 战斗卡框与属性徽章精修任务报告

## 1. 任务结果

本次三项视觉调整均已完成：

- 敌我双方窄框只包围卡片主体，从卡面根 `250 × 360` 内缩为 `210 × 328`；生命与攻击徽章分别向左右露出 `20`、向下露出 `16`。
- 生命绿色血滴移到左下，攻击力移到右下。
- 攻击徽章移除剑主体，改用无剑红金盾形外框；生命与攻击数字统一为 `30` 号白色粗体并添加深色 `Outline`。

## 2. 主要产物

- `Assets/Resources/Art/BattleCards/UI/AttackBadgeFrame.png`：`1254 × 1254` 无剑红金攻击力外框，真实 Alpha 透明背景。
- `Assets/Resources/Ui/BattleCardItem.prefab`：更新主体窄框、状态框、徽章位置、攻击 Sprite 与数字表现。
- `Assets/Scripts/Hearthstone/Ui/Editor/BattleCardItemUiBuilder.cs`：维护上述布局、TMP/Outline 以及 Sprite 导入设置。
- `Assets/Scripts/Hearthstone/Ui/Editor/Hearthstone.Ui.Editor.asmdef`：补充 `Unity.TextMeshPro` Editor 引用。
- `Assets/Scripts/Hearthstone/Tests/Editor/BattleRulesTests.cs`：新增/更新两项 UI 配置测试。

旧 `AttackSwordBadge.png` 作为历史资产保留，但当前卡牌 Prefab 已不引用。

## 3. 实现与框架边界

- `CardFrameOverlay`、`AttackerHighlight`、`TargetHighlight` 使用相同 `sizeDelta=(-40,-32)`，保证基础与状态框都只包围主体。
- `HealthBadge`：左下锚点，中心 `(30,30)`，尺寸 `60 × 60`。
- `AttackBadge`：右下锚点，中心 `(-30,30)`，尺寸 `60 × 60`，Sprite 为 `AttackBadgeFrame`。
- `HealthText` / `AttackText`：`30` 号、`Bold`、白色、深色 `Outline`。
- 静态配置全部保存在 Prefab/一一对应 UiBuilder；Controller、ECS 数据、战斗结算、UiList 和 UiScene 导出信息均未改变。
- Builder 通过当前会话 `unityMCP execute_code` 执行，未手写 Prefab/Scene/Asset YAML。

## 4. Imagegen 记录

使用内置 `imagegen` 编辑模式，输入为 `Assets/Resources/Art/BattleCards/UI/AttackSwordBadge.png`。

首轮提示词要点：

> 移除整把剑及所有剑形银/金元素；保留红宝石盾框、暖金装饰、侧叶、铆钉、对称比例与透明外缘；重建深红中央数值区；无文字、数字、图标或水印。

首轮结果的棋盘格被烘焙进图片，因此未采用。第二轮只执行背景提取：

> 只移除烘焙的浅灰棋盘格并替换为真实透明 Alpha；徽章造型、画布、位置、比例、色彩、材质与边缘保持不变。

最终图片 Alpha 范围为 `0~255`，四角透明，抽样透明像素 `48735/98596`；Unity 导入为 Single Sprite，启用 Alpha Transparency，关闭 Mipmap，Wrap Mode 为 Clamp。

## 5. 验证结果

- MCP Prefab 探针：根 `250×360`、框 `210×328`、生命左/攻击右、攻击 Sprite `AttackBadgeFrame`、两项文字 `30/Bold/White`、两个 Outline 均存在。
- Prefab GUID 扫描：引用新 `AttackBadgeFrame.png`，不再引用旧 `AttackSwordBadge.png`。
- 本次新增 UI EditMode 测试：2 通过、0 失败。
- 完整 `Hearthstone.Tests`：16 项中 15 通过、1 失败。唯一失败与本任务无关：并发更新的 `BattleCardCsvData.csv` 已把默认卡原画键改为 `Boar_001`，旧测试仍断言 `Boar`；本任务未回退或改写该配置。
- 最终活动场景：`Assets/Scenes/Main.unity`，`isDirty=false`，Editor idle。
- 最终 Console：error 0。
- 未进入 Play Mode，未执行游戏内攻击时序和目标分辨率目视回归。

## 6. 文档处理

- 更新玩家视角战斗文档：`AutoDoc/Design/Specific/combat-system/combat-system.md`。
- 更新 UI 美术文档：`AutoDoc/Art/UI/ui-art-overview.md`。
- 更新战斗卡牌美术模块：`AutoDoc/Art/Modules/battle-card/battle-card.md`。
- 更新战斗 UI 程序文档：`AutoDoc/Program/UI/battle/battle.md`。
- 未修改项目级美术风格或 `AutoDoc/UIItem/`，因为没有新增风格体系或自定义 `BbxUiItem`。

## 7. 偏差、风险与清理

- Imagegen 第一版背景不是真实透明，已通过第二次针对性背景提取修正，第一版未进入项目。
- 完整测试集存在 1 个并发卡牌配置导致的无关旧断言失败；本任务相关测试全部通过。
- 当前目录未检测到 Git 仓库，无法提供 Git diff；改动范围通过明确路径、Prefab 探针、资源 GUID 和文档核对确认。
- `AutoDoc/CleanupTempDocs.bat` 仅执行一次，退出码 `0`；本报告在清理后创建。
