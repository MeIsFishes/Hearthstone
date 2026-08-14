# 核心战斗系统方案检查清单

## 任务范围

- **通过**：本轮仅创建 `AutoDoc/Temp/` 下的需求确认稿与审计文档，未修改游戏代码、配置、资源或正式文档。
- **通过**：需求确认稿覆盖双方各 3 张牌、初始统一 5 攻/3 血、我方先攻、敌我交替、各方从左到右轮转、随机索敌和双向同时伤害。
- **通过**：需求确认稿明确死亡、移除、空场、胜负、攻击游标及随机种子规则；这些属于第一版待用户确认的边界。

## 项目现状调研

- **通过**：读取了 `Assets/Scripts/Hearthstone/` 下全部业务源码，以及现有 `Assets/Resources/`、`Assets/Scenes/` 资产清单；项目没有 `AutoDoc/Program/`、`AutoDoc/Art/` 或 `AutoDoc/Design/` 目录。
- **通过**：未搜索、枚举或读取 `AutoDoc/DesignPlan/`。
- **通过**：确认当前只有占位 `BaseStage`、占位单例 Component/System、占位 UiScene/View/Controller 和初始化构建器，无现成战斗模块。
- **通过**：业务资源范围内仅有 Bootstrap、Canvas、Placeholder UI、Main 场景和对应导出 Asset；没有卡牌、战斗场景或正式 2D 美术资产可直接复用。

## 方案设计

- **通过**：已给出需求对齐、范围排除和可验收示例，并明确本轮是等待用户确认的第一阶段稿。
- **未通过**：尚未设计 ECS 数据权威来源；按 `game-module-design` 流程，必须等用户确认功能点后进入数据设计。
- **未通过**：尚未形成数据生产者、消费者、实体归属和配置复制表；等待用户确认。
- **通过**：需求层已明确攻击循环、随机索敌、同时结算、死亡处理、游标推进与胜负判定算法。
- **未通过**：尚未确定最终 System、LoadItem 或 StageListener 类；等待用户确认后在完整方案中设计。
- **未通过**：尚未形成完整 UI 类、Prefab、UiScene 与监听方案；等待用户确认。
- **未通过**：尚未逐项确定 System、UI、LoadItem、DataGroup 和场景的 GameStage 归属；等待用户确认。
- **不适用**：需求确认阶段尚未规划 Utils；完整方案会根据复用点重新判断。
- **通过**：需求规则已收敛为一条路线，没有并列算法；4 个关键边界显式请求用户确认。
- **通过**：需求确认稿只包含 `plan-output-format` 的首章，编号连续且没有空章节；完整方案尚未开始。
- **未通过**：实现顺序与 Todo 尚未形成；等待用户确认后补齐。
- **通过**：需求稿包含 5 条规则验收示例；完整的编辑器外测试和 Unity 集成验证将在最终方案补齐。

## 框架边界审计

- **通过**：需求确认稿没有提出绕过 EcsApi、DataApi、UiApi、ResourceApi、GameStage 或 UI 导出流程的实现。
- **通过**：没有设计业务层 Manager 访问、平行对象池或手写 Scene/Prefab/Asset YAML。
- **不适用**：当前尚未发现框架能力缺口；完整架构设计时需再次审计加载时序和初始化边界。

## 结束审计

- **通过**：需求确认稿保存于 `AutoDoc/Temp/Plan/2026-08-14-CoreBattleSystem-RequirementDraft.md`。
- **通过**：已于本阶段结束前重新读取并逐项标记本清单，保留等待用户确认后才能完成的未通过项。
- **通过**：结束审计后只运行了一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0。
- **通过**：清理后创建 `AutoDoc/Temp/2026-08-14-CoreBattleSystem-Report.md`。
