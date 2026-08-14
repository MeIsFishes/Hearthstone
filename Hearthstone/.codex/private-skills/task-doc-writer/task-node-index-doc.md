# Task 节点总文档说明

Task 节点总文档输出到：

```text
AutoDoc/Task/TaskNode/TaskNodeIndex.md
```

该文档是 `AutoDoc/Task/TaskNode/` 的目录级索引，用来记录每个 Task 节点文档相对路径、节点类全名、节点类型、用途分类和一句话用途说明。它只做检索入口，不替代单个节点文档。

Task 节点总文档只允许包含以下章节：

```markdown
# Task 节点总文档

## 1. Task节点索引
```

## 1. Task节点索引

本章节按 Task 节点逐条记录。每一条必须能对应到 `AutoDoc/Task/TaskNode/` 或其规定子目录下的一篇节点文档。

本章节必须拆成两张表：

- `底层Task节点索引`：记录 `BbxCommon` 命名空间下的底层或通用 Task 节点；节点文档必须位于 `AutoDoc/Task/TaskNode/BbxCommon/`。
- `业务Task节点索引`：记录业务命名空间下的 Task 节点，例如 `Chaos` 命名空间节点。

两张表的列项完全相同，都使用 `节点文档`、`节点类全名`、`节点类型`、`用途分类`、`说明`。

推荐使用表格：

```markdown
### 底层Task节点索引

| 节点文档 | 节点类全名 | 节点类型 | 用途分类 | 说明 |
| --- | --- | --- | --- | --- |
| `BbxCommon/TaskConditionCompare.md` | `BbxCommon.TaskConditionCompare` | `TaskConditionBase` | 条件判断 | 比较两个浮点值并根据配置的比较方式返回条件成功或失败。 |

### 业务Task节点索引

| 节点文档 | 节点类全名 | 节点类型 | 用途分类 | 说明 |
| --- | --- | --- | --- | --- |
| `TaskOnceTakeDamage.md` | `Chaos.TaskOnceTakeDamage` | `TaskOnceBase` | 伤害结算 | 根据配置和实体属性计算一次伤害请求。 |
```

字段要求：

- `节点文档`：必须填写相对于 `AutoDoc/Task/TaskNode/` 的路径并包含 `.md` 后缀；底层节点使用 `BbxCommon/<文件名>.md`，业务节点只填写文件名。
- `节点类全名`：必须填写包含命名空间的类全名。
- `节点类型`：限定填写 `TaskBase`、`TaskOnceBase`、`TaskDurationBase`、`TaskConditionBase` 或已确认的其它 Task 底层基类。
- `用途分类`：用一句短语描述节点用途，例如表现、伤害结算、治疗结算、Blackboard、流程控制、条件判断。
- `说明`：只用一句话描述该节点的用途，不记录配置引用状态，不展开字段细节和完整内部逻辑。

维护要求：

- 新增 Task 节点文档时，必须在总文档新增一行。
- 新增底层或通用 Task 节点时，必须把文档放入 `BbxCommon/` 子目录并写入 `底层Task节点索引`；新增业务 Task 节点时，必须把文档放在 `TaskNode/` 根目录并写入 `业务Task节点索引`。
- 删除或重命名 Task 节点文档时，必须同步更新总文档。
- 节点类全名、节点类型或用途分类变化时，必须同步更新总文档。
- 节点用途发生变化时，必须同步更新总文档中的 `说明`。
- 总文档不记录字段列表；字段含义、必填性和边界写入对应节点文档。
