# 行为树配置设计

当 `.task.setting` 的 `TaskType` 为 `BehaviorTree` 时，按本文把已经给定的节点清单写成行为树中间配置。

本步骤只负责配置中间文件，不负责设计新节点、Context 字段或 Blackboard key。缺少节点、字段或 key 时，应回到前置节点设计流程补齐。

## 1. 文件结构

行为树 `.task.setting` 必须包含：

```json
{
    "TaskType": "BehaviorTree",
    "BindingContext": "TaskContextActiveSkill",
    "Root": "Root",
    "Nodes": []
}
```

- `TaskType` 固定写 `BehaviorTree`。
- `BindingContext` 写 Context 短类名，例如 `TaskContextActiveSkill`。
- `Root` 写根节点的 `Name`，并且必须指向 `Nodes` 中已声明的节点。
- `Nodes` 写全部节点，根节点也必须写入；所有节点引用、条件引用都只能指向这里声明过的节点。
- 文件名去掉 `.task.setting` 后就是 Task key，文件内不写 `TaskKey`。
- 不写 `Schema`、`Editor`。

## 2. 命名格式

行为树中间文件中的命名按以下规则写：

- `BindingContext` 写 Context 短类名，不写命名空间，例如 `TaskContextActiveSkill`。
- 节点 `Type` 写 Task 节点完整类型名，必须带命名空间，例如 `BbxCommon.TaskBtRoot`、`Chaos.TaskOnceTakeDamage`。
- 枚举字段值写枚举成员名，例如 `GreaterOrEqual`、`Strength`，不写枚举类型名或命名空间。

## 3. 节点结构

每个节点必须包含：

```json
{
    "Name": "PlayAnimation",
    "Type": "Chaos.TaskNodePlayStandardAnimation",
    "Fields": {},
    "ConnectPoints": {},
    "Conditions": {
        "Enter": [],
        "During": [],
        "Exit": []
    }
}
```

- `Name` 是本文件内使用的节点名，必须唯一。
- `Type` 写 Task 节点完整类型名，例如 `BbxCommon.TaskBtRoot`、`Chaos.TaskOnceTakeDamage`。
- `Fields` 写普通字段赋值；没有字段时写 `{}`。
- `ConnectPoints` 写子节点连接；没有连接时写 `{}`。
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

## 5. 连接和条件

行为树子节点写在 `ConnectPoints` 中：

```json
"ConnectPoints": {
    "Tasks": [
        "PlayAnimation",
        "TakeDamage"
    ]
}
```

- 连接点字段名必须来自节点定义，常见为 `Tasks`。
- 子节点使用节点 `Name` 引用。
- 数组顺序就是连接点的子节点顺序。

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
- 条件引用也使用节点 `Name`。
- 条件节点必须同时写在 `Nodes` 中。

## 6. 样例

文件名：

```text
ActiveFireball.task.setting
```

内容：

```json
{
    "TaskType": "BehaviorTree",
    "BindingContext": "TaskContextActiveSkill",
    "Root": "Root",
    "Nodes": [
        {
            "Name": "Root",
            "Type": "BbxCommon.TaskBtRoot",
            "Fields": {},
            "ConnectPoints": { "Tasks": ["PlayAnimation", "TakeDamage"] },
            "Conditions": { "Enter": [], "During": [], "Exit": [] }
        },
        {
            "Name": "PlayAnimation",
            "Type": "Chaos.TaskNodePlayStandardAnimation",
            "Fields": {
                "Target": { "Source": "Context", "Value": "CasterEntityId" },
                "UseCustomAnimation": { "Source": "Value", "Value": false }
            },
            "ConnectPoints": {},
            "Conditions": { "Enter": [], "During": [], "Exit": [] }
        },
        {
            "Name": "TakeDamage",
            "Type": "Chaos.TaskOnceTakeDamage",
            "Fields": {
                "DamageTarget": { "Source": "Context", "Value": "TargetEntityId" },
                "DamageAttributes": { "Source": "Value", "Value": ["Intelligence"] },
                "ConstDamage": { "Source": "Value", "Value": 0 }
            },
            "ConnectPoints": {},
            "Conditions": { "Enter": ["CanHit"], "During": [], "Exit": [] }
        },
        {
            "Name": "CanHit",
            "Type": "BbxCommon.TaskConditionCompare",
            "Fields": {
                "LeftValue": { "Source": "Blackboard", "Value": "HitRate" },
                "CompareType": { "Source": "Value", "Value": "GreaterOrEqual" },
                "RightValue": { "Source": "Value", "Value": 1.0 }
            },
            "ConnectPoints": {},
            "Conditions": { "Enter": [], "During": [], "Exit": [] }
        }
    ]
}
```

这个样例展示了：

- `Root` 作为根节点也写在 `Nodes` 中。
- `Root.Tasks` 连接两个普通子节点。
- `PlayAnimation.Target` 从 Context 字段读取。
- `TakeDamage.DamageAttributes` 使用数组值。
- `TakeDamage.Conditions.Enter` 引用条件节点 `CanHit`。
- `CanHit.LeftValue` 从 Blackboard key 读取。
- `CanHit.CompareType` 使用枚举成员名。

## 7. 生成的编辑器布局

转换脚本会根据根节点、条件引用和连接点关系自动生成 `.editor.json` 中的节点坐标，不在 `.task.setting` 中人工维护位置：

- 条件引用相对父节点前进一列；父节点存在 Condition 时，普通连接点子节点前进两列，使 Condition 位于父子行为节点之间；父节点没有 Condition 时，普通子节点只前进 `1.5` 列。
- 横向每列间距为 `250`，纵向每个叶槽间距为 `150`。
- 普通行为父节点只按 ConnectPoints 子树的叶槽纵向居中；Condition 不占用行为子树叶槽，而是附着在所属父节点旁边。
- 单个 Condition 与父节点使用相同纵向基线；多个 Condition 以父节点为中心按 Enter、During、Exit 和各数组声明顺序排列，并选择同列最近的无碰撞位置。
- 同级普通子树依照连接点中的声明顺序排列。
- 未从根节点连通的节点放在主树之后的独立列与独立叶槽中，避免覆盖主树节点。

当前编辑器节点宽度为 `190`，已配置节点最大常见高度为 `130`。单列间距会为相邻父节点和 Condition 保留 `60` 的净间隙；无 Condition 的普通父子节点间距为 `375`，有 Condition 时为 `500`，兼顾连线标签与画布占用。
