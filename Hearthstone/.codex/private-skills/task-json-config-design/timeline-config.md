# Timeline 配置设计

当 `.task.setting` 的 `TaskType` 为 `Timeline` 时，按本文把已经给定的节点清单写成 Timeline 中间配置。

本步骤只负责配置中间文件，不负责设计新节点、Context 字段或 Blackboard key。缺少节点、字段、时间项或 key 时，应回到前置节点设计流程补齐。

## 1. 文件结构

Timeline `.task.setting` 必须包含：

```json
{
    "TaskType": "Timeline",
    "BindingContext": "TaskContextActiveSkill",
    "Root": "RootTimeline",
    "Nodes": []
}
```

- `TaskType` 固定写 `Timeline`。
- `BindingContext` 写 Context 短类名，例如 `TaskContextActiveSkill`。
- `Root` 写根 Timeline 节点的 `Name`，并且必须指向 `Nodes` 中已声明的 Timeline 节点。
- `Nodes` 写全部节点，根 Timeline 节点、被时间轴启动的节点、条件节点都必须写入；所有时间项引用和条件引用都只能指向这里声明过的节点。
- 文件名去掉 `.task.setting` 后就是 Task key，文件内不写 `TaskKey`。
- 不写 `Schema`、`Editor`。

## 2. 命名格式

Timeline 中间文件中的命名按以下规则写：

- `BindingContext` 写 Context 短类名，不写命名空间，例如 `TaskContextActiveSkill`。
- 节点 `Type` 写 Task 节点完整类型名，必须带命名空间，例如 `BbxCommon.TaskTimeline`、`Chaos.TaskOnceTakeDamage`。
- 枚举字段值写枚举成员名，例如 `GreaterOrEqual`、`Strength`，不写枚举类型名或命名空间。

## 3. 节点结构

普通节点必须包含：

```json
{
    "Name": "TakeDamage",
    "Type": "Chaos.TaskOnceTakeDamage",
    "Fields": {},
    "Conditions": {
        "Enter": [],
        "During": [],
        "Exit": []
    }
}
```

Timeline 节点必须额外包含 `TimelineItems`：

```json
{
    "Name": "RootTimeline",
    "Type": "BbxCommon.TaskTimeline",
    "Fields": {
        "Duration": {
            "Source": "Value",
            "Value": -1
        }
    },
    "TimelineItems": [],
    "Conditions": {
        "Enter": [],
        "During": [],
        "Exit": []
    }
}
```

- `Name` 是本文件内使用的节点名，必须唯一。
- `Type` 写 Task 节点完整类型名，例如 `BbxCommon.TaskTimeline`、`Chaos.TaskOnceTakeDamage`。
- `Fields` 写普通字段赋值；没有字段时写 `{}`。
- `TimelineItems` 只写在 Timeline 节点上，且每个 Timeline 节点都必须写；不是 Timeline 的节点不要写这个字段。
- `Conditions` 固定包含 `Enter`、`During`、`Exit` 三个数组；没有条件时写空数组。

## 4. 字段写法

普通字段写在节点的 `Fields` 中。每个字段都必须包含 `Source` 和 `Value`：

```json
"Fields": {
    "Target": {
        "Source": "Context",
        "Value": "CasterEntityId"
    },
    "UseCustomAnimation": {
        "Source": "Value",
        "Value": false
    }
}
```

字段规则：

- 字段名必须来自节点定义。
- `Source` 只允许 `Value`、`Context`、`Blackboard`。
- `Source` 为 `Value` 时，`Value` 写固定值，可以是布尔值、数字、字符串、数组或字典对象。
- `Source` 为 `Context` 时，`Value` 写 Context 字段名。
- `Source` 为 `Blackboard` 时，`Value` 写 Blackboard key。
- `Context` 字段和 `Blackboard` key 必须来自前置设计中已经确认存在的字段或 key。
- 枚举值写枚举成员名，例如 `GreaterOrEqual`、`Strength`、`Intelligence`。
- 列表写 JSON 数组，例如 `["Strength", "Intelligence"]`。
- 字典写 JSON 对象；只有字段文档明确支持对象或字典时才使用。
- Timeline 节点的总时长写在 `Fields.Duration` 中。

## 5. 时间项写法

Timeline 子项写在 Timeline 节点的 `TimelineItems` 中：

```json
"TimelineItems": [
    {
        "StartTime": 0.0,
        "Duration": -1.0,
        "Node": "PlayAnimation"
    },
    {
        "StartTime": 0.4,
        "Duration": 0.0,
        "Node": "TakeDamage"
    }
]
```

- `StartTime` 表示子节点相对当前 Timeline 开始的启动时间。
- `Duration` 表示当前 Timeline 对该子节点的持续时间控制。
- `Node` 写被启动节点的 `Name`。
- 每个时间项都必须明确写出 `StartTime`、`Duration`、`Node`。
- `Node` 必须指向 `Nodes` 中已声明的节点。
- `StartTime` 通常从 `0` 开始；需要进入 Timeline 时立刻启动的子节点写 `0`。
- 多个子项可以使用相同的 `StartTime`，数组顺序就是同一时间点的配置顺序。
- `Duration < 0` 表示不由 Timeline 限制持续时间，子节点按自身逻辑结束。
- `Duration = 0` 表示只在该时间点触发，适合一次性节点。
- `Duration > 0` 表示子节点最多持续指定秒数。
- 同一个节点如果需要在多个时间点启动，优先声明多个独立节点；不要在多个时间项中复用同一个 `Node`，除非节点文档明确允许复用。

## 6. 条件

条件节点写在目标节点的 `Conditions` 中：

```json
"Conditions": {
    "Enter": [
        "CanHit"
    ],
    "During": [],
    "Exit": []
}
```

- `Enter` 表示进入目标节点前检查。
- `During` 表示目标节点运行期间检查。
- `Exit` 表示目标节点运行期间用于提前成功退出。
- 条件引用使用节点 `Name`。
- 条件节点必须同时写在 `Nodes` 中。
- Timeline 节点、普通子节点、条件节点都可以配置自己的条件；只在确实需要条件时填写。

## 7. 样例

文件名：

```text
NormalAttack.task.setting
```

内容：

```json
{
    "TaskType": "Timeline",
    "BindingContext": "TaskContextActiveSkill",
    "Root": "RootTimeline",
    "Nodes": [
        {
            "Name": "RootTimeline",
            "Type": "BbxCommon.TaskTimeline",
            "Fields": {
                "Duration": { "Source": "Value", "Value": -1 }
            },
            "TimelineItems": [
                { "StartTime": 0.0, "Duration": -1.0, "Node": "PlayAnimation" },
                { "StartTime": 0.4, "Duration": 0.0, "Node": "TakeDamage" }
            ],
            "Conditions": { "Enter": [], "During": [], "Exit": [] }
        },
        {
            "Name": "PlayAnimation",
            "Type": "Chaos.TaskNodePlayStandardAnimation",
            "Fields": {
                "Target": { "Source": "Context", "Value": "CasterEntityId" },
                "UseCustomAnimation": { "Source": "Value", "Value": false },
                "CustomAnimationName": { "Source": "Value", "Value": "" }
            },
            "Conditions": { "Enter": [], "During": [], "Exit": [] }
        },
        {
            "Name": "TakeDamage",
            "Type": "Chaos.TaskOnceTakeDamage",
            "Fields": {
                "NoSource": { "Source": "Value", "Value": false },
                "DamageSource": { "Source": "Context", "Value": "CasterEntityId" },
                "DamageTarget": { "Source": "Context", "Value": "TargetEntityId" },
                "DamageAttributes": { "Source": "Value", "Value": ["Strength"] },
                "AttributeFactors": { "Source": "Value", "Value": [1] },
                "ConstDamage": { "Source": "Value", "Value": 0 },
                "Final": { "Source": "Value", "Value": false }
            },
            "Conditions": { "Enter": [], "During": [], "Exit": [] }
        }
    ]
}
```

这个样例展示了：

- `RootTimeline` 作为根 Timeline 节点也写在 `Nodes` 中。
- `RootTimeline.Duration` 使用 `-1`，表示总时长不限制。
- `PlayAnimation` 在 `0.0` 秒启动，持续时间不由 Timeline 限制。
- `TakeDamage` 在 `0.4` 秒触发，使用 `Duration=0` 表达一次性节点。
- `PlayAnimation.Target` 从 Context 字段读取。
- `TakeDamage.DamageAttributes` 和 `TakeDamage.AttributeFactors` 使用数组值。
