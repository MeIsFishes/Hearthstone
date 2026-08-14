---
name: subagent-toml
description: 添加或编辑 subagent 的流程。
---

# Subagent Toml

当用户要求添加、创建、修改或维护 subagent 的 `.toml` 文件时，使用本 skill。添加 subagent 前，必须先确认用户要添加哪种 subagent，不要自己猜。

## Subagent 类型

- 底层 subagent：应用于项目底层框架，文件位置为 `.codex/agents/`。
- 项目 subagent：仅当前项目工程使用，文件位置为 `.codex/project-files/agents/`。

如果用户只说“添加 subagent”或“添加 agent toml”，先询问类型。若用户明确给出目录，可根据目录确认类型，并在任务报告中记录判断依据。

## 任务检查清单

添加 subagent 时，在任务开始把以下条目加入 `AutoDoc/Temp/` 下的 `*-Checklist.md`。清单不限定实际执行顺序，也不要求执行过程中逐项更新：

1. 确认清楚添加的 agent 类型（可以使用提问功能，将 agent 定义和描述展示给用户），并在对应目录下建立 `.toml` 文件。
2. 填充基础字段：`name`、`description`。
3. 填充 `developer_instructions`，提示词使用用户的语言。
4. 根据 agent 责任填充范围、规则、执行步骤和输出要求。
5. 关联该 agent 需要的 skill。
6. 在主代理文件 `.codex/agents/MainAgent/MAIN_AGENT.md` 中注册、移除或更新该 agent 的条目。
7. 检查 `description` 是否能精简，最终精简到中文不超过 40 字，英文不超过 25 词。

任务结束前逐项复核清单并记录证据，再按 `project-state-preflight` 写独立任务报告。

## Toml 字段

subagent `.toml` 通常包含以下字段：

```toml
name = "agent_name"
description = "简短说明 agent 负责什么。"

[[skills.config]]
path = ".codex/private-skills/example/SKILL.md"
enabled = true

developer_instructions = '''
这里写子代理提示词。
'''
```

字段要求：

- `name`：使用英文小写、数字和下划线，通常与文件名对应；文件名可用连字符，例如 `example-agent.toml`，`name` 可写 `example_agent`。
- `description`：用用户的语言，说明 agent 的职责和触发场景；保持简短，最终中文不超过 40 字，英文不超过 25 词。
- `model_reasoning_effort`：非必填字段。只有用户明确要求指定推理强度时才填写；未要求时不要主动添加。
- `developer_instructions`：使用三引号字符串；内容较长或包含反引号时，优先沿用现有文件风格，可使用 `'''` 或 `"""`。
- `[[skills.config]]`：每个需要预加载或显式启用的 skill 写一个表项。

## Developer Instructions 内容

`developer_instructions` 至少包含以下内容：

- 角色定位：说明“你是专门负责什么的子代理”。
- 输入来源：说明外部代理会传入哪些上下文、文件、范围或任务。
- 范围：列出允许读取、修改或必须避免触碰的文件和目录。
- 规则：列出必须遵循的行为约束，例如使用用户语言、是否允许改代码、是否必须建立任务检查清单和报告。
- 执行步骤：如果任务存在依赖顺序，可以写成有序流程；不要把任务检查清单描述成每完成一步都要更新或暂停的逐步执行器。
- 输出要求：说明最终回复应包含哪些结论、文件路径、风险或待确认项。

如果 agent 只做评审或检查，默认不要让它修改代码或文档，除非用户明确要求。若 agent 会修改文件，必须写清可修改范围。

## Skill 关联方式

在 subagent toml 中关联 skill 时，使用项目根目录相对路径：

```toml
[[skills.config]]
path = ".codex/private-skills/skill-name/SKILL.md"
enabled = true
```

关联规则：

- 底层 subagent 可直接在 `.codex/agents/<agent>.toml` 中关联底层 skill 或其他必要 skill。
- 项目 subagent 可直接在 `.codex/project-files/agents/<agent>.toml` 中关联项目 skill 或必要 skill。
- `path` 必须使用项目根目录相对路径，不要写入本机盘符、用户目录或其他绝对路径。
- 如果底层 subagent 需要项目级扩展，按项目约定在 `.codex/project-files/agents/` 下创建同名 `agent-extension.md`，再在底层 subagent 的 toml 中关联该 extension 文件；具体字段写法需先参考项目内已有样例，若没有样例，先向用户确认。
- 只有当 agent 的提示词确实需要某个 skill 的规则或知识时才关联，不要为了完整性罗列无关 skill。

## 项目级注册表维护

新增、删除或修改 subagent 时，必须同步维护 `.codex/agents/MainAgent/MAIN_AGENT.md` 中的“可用 Subagent 注册表”：

- 新增 subagent 后，注册该 agent 的 `name`、`desc` 和 `path`。
- 删除 subagent 后，移除对应条目。
- 修改 subagent 的职责、描述、路径或类型后，同步更新对应条目。
- 注册表中的 `desc` 应与 toml 的 `description` 语义一致，并保持简短。
- 注册表中的 `path` 使用项目相对路径。

## 最终检查

- 检查 `.toml` 文件位置是否符合 subagent 类型。
- 检查 `.codex/agents/MainAgent/MAIN_AGENT.md` 中的 subagent 注册表已按本次新增、删除或修改同步更新。
- 检查 `name` 与文件名是否语义一致。
- 检查 `description` 是否短而清楚。
- 检查 `developer_instructions` 是否使用用户语言。
- 检查 agent 的可读写范围是否明确。
- 检查 skill 关联路径是否存在，且只关联必要 skill。
- 检查没有为未确认的 agent 类型或未确认的职责做过度设计。
