# 卡牌底板浅色稀疏花纹检查清单

## 用户要求与资源处理

- [通过] 已确认卡片底板是共享 Prefab 的 `SkillArea` 纯色 Image；新增纹样仅位于其子级，不改边框、立绘、说明文字或属性徽章。
- [不适用] 已完整读取 imagegen 技能；核验后发现目标是代码原生纯色 UI 且项目禁止新增 `.meta`，按技能边界改用单个静态 TMP 装饰层，没有调用位图生成工具。
- [不适用] 本次不新增位图或 Sprite 资源；纹样随 `BattleCardItem.prefab` 保存，使用现有字体和透明顶点色。
- [通过] 花纹写入唯一共享 `BattleCardItem.prefab`，战斗、备战卡池、出战槽和融合槽自动复用，没有平行卡面实现。
- [通过] 结构与四项 Editor 测试确认统一边框、悬停开关、外框下沿和攻血徽章层级保持不变。

## UI 与框架边界

- [通过] 已同步一一对应的 `BattleCardItemUiBuilder`，通过 Unity Editor 执行 `Build()` 重建 Prefab；未手写 Prefab YAML。
- [通过] `CardBasePattern` 是 `SkillArea` 的静态第一个子级，不接收射线；Controller 无新增运行时拼装、查找或刷新逻辑。
- [通过] 框架边界审计确认继续使用现有共享 View/Controller、Builder、Resources 预加载和对象池；新增函数仅负责可重复生成同一静态层级。
- [通过] `git diff --name-only` 未发现本任务修改 `.meta`；工作区并行资源、逻辑和文档改动均保留未回退。

## 验证与文档

- [通过] Unity 离屏渲染预览确认花纹为浅金色、`12%` Alpha、左右稀疏分布且中央留空；全部三个字符字形均存在于当前源字体。
- [通过] Builder 与测试脚本校验为 0 error；Prefab 结构检查通过；4 项相关 Editor 测试通过；Unity Console 最终 0 error。按项目默认边界未进入 Play Mode。
- [通过] 已完整读取玩家视角设计文档格式与战斗系统专项格式，并同步战斗系统、备战卡池两篇当前玩家可见表现。
- [通过] 已完整读取美术文档与模块格式，并同步战斗卡牌模块的底板纹样色彩、透明度和构图；未新增图片资产列表项。
- [通过] 已完整读取程序文档与 UI 界面格式，并同步战斗、备战 UI 文档的静态层级、射线和 Builder 维护方式。

## 收尾

- [通过] 已在收尾前重新打开本清单并逐项写入状态与证据。
- [通过] 已只运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`。
- [通过] 已在清理后创建 `AutoDoc/Temp/BattleCardBasePattern-Report.md`，记录实现、验证、文档、偏差和风险。
