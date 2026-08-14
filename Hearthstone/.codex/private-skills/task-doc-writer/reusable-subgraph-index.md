# 通用子图总文档

当 Task 图设计方案中出现可复用子图时，在以下路径维护一篇总文档：

```text
AutoDoc/Task/TaskContext/ReusableTaskGraphIndex.md
```

这篇文档只记录“哪些单张 Task 图可以作为通用子图复用”。它不记录 Task 图集，不替代 `AutoDoc/Task/TaskGraph/` 下的正式 Task 图集文档，也不记录节点字段细节。

## 记录时机

输出 Task 图方案后，如果认为某个子图具有通用性，就把它加入总文档。通用性通常指：该子图能被多个技能、AI、事件、演出或初始化流程复用，而不是只服务当前单一入口。

如果只是当前图集内部的私有子图，不记录到这里。

## 文档格式

```markdown
# 通用Task子图总文档

## 1. 通用子图索引

| Task图 | 图类型 | 绑定Context | 作用 |
| --- | --- | --- | --- |
| `CommonHitEffect` | Timeline | `TaskContextActiveSkill` | 播放通用命中表现。 |
| `TryApplyDamage` | BehaviorTree | `TaskContextActiveSkill` | 判断命中条件后执行一次伤害结算。 |
```

字段说明：

- `Task图`：Task key。
- `图类型`：`Timeline` 或 `BehaviorTree`。
- `绑定Context`：该图绑定的 Context 短类名。
- `作用`：一句话说明该子图可复用的功能。
