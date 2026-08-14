# 输出阶段

本阶段把已经稳定的 Task 图设计方案写成文本，输出到 `AutoDoc/Temp/`。这份文档是 `task-json-config-design` 的输入，不是正式 Task 图文档。

输出方案后，如果某个子图具有跨技能、AI、事件、演出或初始化流程复用价值，且当前执行者允许维护正式 Task 文档，则按 `task-doc-writer` 中的 [通用子图总文档](../task-doc-writer/reusable-subgraph-index.md) 将它记录到 `AutoDoc/Task/TaskContext/ReusableTaskGraphIndex.md`。如果当前执行者只允许输出临时文档，则在方案中写明“建议登记为可复用子图”，交给主代理处理。

文件名建议使用：

```text
AutoDoc/Temp/TaskGraphDesign_<功能名>.md
```

Task 图设计文档只写后续配置必须知道的信息：Task 图集、每张图的类型、Context、节点、结构或时间项、字段赋值。不要写实现过程、长篇理由或检查清单。

## 节点调用和顺序关系

行为树图用 `结构` 说明节点调用关系。`A.Tasks -> B, C` 表示 A 会按该连接点配置调用 B 和 C；如果 A 是 Sequence，B、C 按书写顺序依次执行；如果 A 是选择、循环或并行类节点，顺序含义以该节点文档为准。条件写在对应节点上，例如 `B.EnterConditions -> CanRun`。

Timeline 图用 `Timeline 时间项` 说明节点启动顺序。格式为 `开始时间 / 持续时间 -> 节点名`；开始时间越早越先启动，开始时间相同时按书写顺序启动。持续时间写 `-1` 表示由节点自身生命周期决定结束。

跨图调用用 `RunTask` 所在节点说明，例如 `PlayHitEffect.TaskKey -> FireballHitEffect`。被调用的子图应出现在 Task 图列表中，并在自己的小节里继续说明结构或时间项。

如果存在缺失节点，额外输出独立文档：

```text
AutoDoc/Temp/TaskMissingNodes_<功能名>.md
```

缺失节点文档只写交给主代理实现所需的信息：节点名、归属、节点类型建议、用途和字段需求。不要在 Task 图设计文档中单开“缺失项”章节。

## Task 图集简例

```markdown
# Task图设计：火球术

## 1. Task图集

- 图集名称：火球术
- 入口图：ActiveFireball
- 关联图：FireballHitEffect，由 PlayHitEffect 通过 RunTask 开启
- BindingContext：TaskContextActiveSkill
- 说明：播放施法动作，结算一次伤害，并在命中后播放表现。

## 2. Task图列表

- ActiveFireball：BehaviorTree，入口图
- FireballHitEffect：Timeline，由 ActiveFireball.PlayHitEffect 通过 RunTask 开启
```

## 行为树简例

```markdown
## ActiveFireball

- 类型：BehaviorTree
- 配置目录：Mods/Native/Task/
- BindingContext：TaskContextActiveSkill
- 说明：播放施法表现后造成一次智力伤害。

### 节点

- Root，BbxCommon.TaskBtRoot，入口根节点
- Sequence，BbxCommon.TaskNodeSequence，顺序执行施法、等待和伤害
- PlayAnimation，Chaos.TaskNodePlayStandardAnimation，播放标准动画
- WaitTime，BbxCommon.TaskNodeWaitForTime，等待伤害结算时间点
- TakeDamage，Chaos.TaskOnceTakeDamage，造成伤害

### 结构

- Root.Tasks -> Sequence
- Sequence.Tasks -> PlayAnimation, WaitTime, TakeDamage

### 字段

- PlayAnimation.Target：Source=Context，Value=CasterEntityId
- WaitTime.Time：Source=Value，Value=0.4
- TakeDamage.DamageTarget：Source=Context，Value=TargetEntityId
- TakeDamage.DamageAttributes：Source=Value，Value=[Intelligence]
```

## Timeline 简例

```markdown
## NormalAttack

- 类型：Timeline
- 配置目录：Mods/Native/Task/
- BindingContext：TaskContextActiveSkill
- 说明：开始时播放攻击动画，0.4 秒时结算伤害。

### 节点

- RootTimeline，BbxCommon.TaskTimeline，根时间轴
- PlayAnimation，Chaos.TaskNodePlayStandardAnimation，播放标准动画
- TakeDamage，Chaos.TaskOnceTakeDamage，造成伤害

### Timeline 时间项

- 0.0s / -1 -> PlayAnimation
- 0.4s / 0 -> TakeDamage

### 字段

- RootTimeline.Duration：Source=Value，Value=-1
- PlayAnimation.Target：Source=Context，Value=CasterEntityId
- TakeDamage.DamageTarget：Source=Context，Value=TargetEntityId
```

## 易歧义字段说明

- 行为树 `A.Tasks -> B, C` 表示 A 的 `Tasks` 连接点会调用 B、C；如果 A 是 Sequence，则 B、C 按书写顺序依次执行。
- Timeline 时间项中的 `0.4s / 0 -> TakeDamage` 表示 `开始时间 / 持续时间 -> 节点名`，不是节点字段赋值。
- Timeline 时间项持续时间为 `0` 通常表示一次性触发；持续时间为 `-1` 表示由节点自身生命周期决定结束。
- 字段段落里的 `Source=Context` 表示从当前图的 `BindingContext` 读取字段；`Source=Blackboard` 表示从 Blackboard key 读取值。

## 缺失节点文档简例

```markdown
# Task缺失节点：功能名

## 1. TaskNodeSelectTarget

- 归属：业务层 Task
- 节点类型建议：TaskOnce
- 用途：从候选目标中选出一个目标并写入 Blackboard。
- 字段需求：
  - Candidates：List<int>，Source=Context，必填
  - OutputTarget：string，Source=Value，必填，写入 Blackboard key
```
