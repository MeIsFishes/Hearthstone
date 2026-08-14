# TaskContext 总文档说明

TaskContext 总文档输出到：

```text
AutoDoc/Task/TaskContext/TaskContextIndex.md
```

该文档是 `AutoDoc/Task/TaskContext/` 的目录级索引，用来记录每个 Context 文档名、Context 类全名、是否抽象、用途分类和一句话用途说明。它只做检索入口，不替代单个 Context 文档。

TaskContext 总文档只允许包含以下章节：

```markdown
# TaskContext 总文档

## 1. TaskContext索引
```

## 1. TaskContext索引

推荐使用表格：

```markdown
| Context文档 | Context类全名 | 是否抽象 | 用途分类 | 说明 |
| --- | --- | --- | --- | --- |
| `TaskContextActiveSkill.md` | `Chaos.TaskContextActiveSkill` | 否 | 主动技能 | 表达一次主动技能或默认普通攻击的运行输入。 |
```

字段要求：

- `Context文档`：必须填写 Context 文档文件名，包含 `.md` 后缀。
- `Context类全名`：必须填写包含命名空间的类全名。
- `是否抽象`：填写 `是` 或 `否`。
- `用途分类`：用一句短语描述 Context 对应的业务场景。
- `说明`：只用一句话描述该 Context 可承载的 Task 运行输入，不展开字段列表。

维护要求：

- 新增 TaskContext 文档时，必须在总文档新增一行。
- 删除或重命名 TaskContext 文档时，必须同步更新总文档。
- Context 类全名、抽象状态、用途分类或用途说明变化时，必须同步更新总文档。
- 总文档不记录字段列表；字段含义、必填性和边界写入对应 Context 文档。
