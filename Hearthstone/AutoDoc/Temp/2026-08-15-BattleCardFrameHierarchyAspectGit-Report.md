# 战斗卡牌边框层级与立绘比例任务报告

## 任务结果

已将双方战斗卡牌的基础框、攻击者高亮框与目标高亮框统一调整为 `230 × 348`。边框完整包围 `222.5 × 297` 的立绘视窗和 `180 × 75.6` 的下侧说明栏，同时生命、攻击徽章仍从左右及底部自然露出。三个框层均位于生命、攻击和卡牌编号之下。

立绘 `ArtworkArea` 已在 Prefab 构建阶段和 Controller 绑定阶段双重启用 `preserveAspect`，保留原始宽高比，不做非等比拉伸。生命仍位于左下，攻击仍位于右下，攻击使用无剑 `AttackBadgeFrame`，属性数字继续使用 30 号白色粗体与深色描边。

## 检查项状态与证据

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 边框覆盖立绘与说明栏 | 通过 | Unity Prefab 探针：根 `250 × 360`、边框 `230 × 348`、立绘视窗 `222.5 × 297`、说明栏 `180 × 75.6` |
| 边框层低于属性与编号 | 通过 | 三个框层同级索引 `2/3/4`，生命、攻击、编号同级索引 `6/7/8` |
| 立绘保持原始比例 | 通过 | Prefab 探针 `preserveAspect=True`；Builder 与 Controller 均有明确配置 |
| 生命/攻击位置与可读性 | 通过 | 左下 `(30,30)`、右下 `(-30,30)`；`AttackBadgeFrame`；30 号白色粗体和深色 Outline |
| UI 框架边界 | 通过 | 使用既有 `BattleCardItemUiBuilder`，通过官方 Unity MCP 的 `execute_code` 执行；未手写 Prefab YAML |
| 玩家视角设计文档 | 通过 | 已更新 `AutoDoc/Design/Specific/combat-system/combat-system.md` |
| 美术文档 | 通过 | 已更新 UI 美术总览与战斗卡牌模块文档 |
| 程序文档 | 通过 | 已更新 `AutoDoc/Program/UI/battle/battle.md` |
| 临时文档清理 | 通过 | `AutoDoc/CleanupTempDocs.bat` 执行一次，退出码 0；文档数量 79，未触发阈值删除 |
| Git 工作区隔离 | 通过 | 只计划暂存本任务的 Prefab、Builder、测试、四篇现状文档及本次清单/报告；并行任务文件保持未暂存 |
| Git 提交与远端推送 | 通过 | 实现提交 `4ba7367` 已推送到 `origin/main`；本报告的最终状态更新随审计提交继续推送 |

## 验证结果

- Unity 脚本刷新与编译成功；Builder 执行成功。
- 两项卡牌 UI 定向 EditMode 测试全部通过。
- 完整 `Hearthstone.Tests` EditMode 测试执行 16 项，15 项通过、1 项失败。失败项为 `RuntimeResourcesContainBattleCardCsvAndStageDataRegistry`：当前 CSV 使用 `Boar_001`，旧断言期待 `Boar`；该差异来自现有卡图配置，与本次卡框修改无关，未擅自纳入提交。
- 最终活动场景为 `Assets/Scenes/Main.unity`，未脏；清理 Console 后错误数为 0。
- `git diff --check` 未发现本任务文件的空白错误。

## 偏差与未解决风险

- 按项目约定未进入 Play Mode；本次以 Prefab 几何探针、层级探针、素材比例检查和 EditMode 测试验收。
- 完整测试中的 `Boar` / `Boar_001` 既存断言不一致仍待对应卡图任务统一配置或测试预期。
- 工作区存在其他并行任务改动；本任务 Git 操作明确排除这些文件。
