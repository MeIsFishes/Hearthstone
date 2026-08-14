---
name: add-skill
description: 添加或编辑项目、底层或通用 skill 的流程。
---

# 添加 Skill

当用户要求添加、创建或接入 skill 时，使用本 skill。先确认用户要添加哪种 skill，不要自己猜。

## 核心流程

1. 如果用户没有明确说明 skill 类型，先询问是通用 skill、项目 skill，还是底层 skill（可以使用提问功能，将不同类型 skill 的定义和描述展示给用户）。
2. 如果不是通用 skill，还要询问需要关联给哪些 agent。若用户没有明确指示，默认不关联给任何 agent。
3. 在任务开始时，将下列条目写入 `AutoDoc/Temp/` 下本次任务的 `*-Checklist.md`。这些条目不限定实际执行顺序，也不要求执行过程中逐项更新：
   - 确认清楚添加的 skill 类型，并在对应目录下建立 skill 文件。
   - 填充 skill 的 `name` 和 `description`，`name` 用英文，`description` 采用用户的语言。
   - 填充 skill 内容，采用用户的语言。
   - 检查 `description` 是否能精简，最终精简到中文不超过 40 字，英文不超过 25 词。
   - 检查 skill 内容是否有条件调用或访问；若有，拆成多个文件并建立关联。
4. 任务结束前逐项复核检查清单，并按 `project-state-preflight` 写入独立任务报告。

## Skill 类型

- 通用 skill：所有 agent 都能看见，添加位置为 `.codex/skills/`。通常不推荐，除非用户明确要求跨项目、跨 agent 通用。
- 项目 skill：专门应用于当前项目工程，添加位置为 `.codex/project-files/skills/`。添加完毕后，可按用户要求与 agent 建立关联。
- 底层 skill：应用于整套框架，添加位置为 `.codex/private-skills/`。添加完毕后，可按用户要求与主 agent 或 subagent 建立关联。

如果用户只说“添加 skill”，必须询问 skill 类型。若用户明确给出目录，可根据目录确认类型，并在任务报告中记录判断依据。

## Agent 关联方式

- 通用 skill 不需要额外询问 agent 关联。
- 非通用 skill 必须询问要关联给哪些 agent。
- 如果用户没有明确指示关联对象，默认不要关联给任何 agent。

项目 skill 的关联方式：

- 关联主 agent：编辑 `.codex/agents/MainAgent/MAIN_AGENT.md`。
- 关联底层 subagent：在 `.codex/project-files/agents/` 下创建同名 `agent-extension.md` 文件，在其中关联 skill；再在 `.codex/agents/` 下对应 `.toml` 文件中关联到这个 extension 文件。
- 关联项目 subagent：直接在 `.codex/project-files/agents/` 下对应 `.toml` 文件中关联 skill。

底层 skill 的关联方式：

- 关联主 agent：直接在 `.codex/agents/MainAgent/MAIN_AGENT.md` 中关联。
- 关联其他 subagent：直接在对应 `.toml` 文件中关联。

如果项目实际主代理文件路径与上面规则不完全一致，先读取现有目录结构和已有索引格式，再沿用当前项目中实际生效的路径与写法。不要创建平行的新索引体系。

## 编写要求

- `SKILL.md` 必须包含 YAML frontmatter，且只需要 `name` 和 `description`。
- skill 文件夹名与 `name` 保持一致，使用英文小写、数字和连字符。
- `description` 必须同时说明用途和触发场景，但要保持简短。
- 正文使用用户的语言；除非用户明确要求，否则不要额外创建 README、安装指南、变更日志等辅助文档。
- 如果 skill 内容在一次使用过程中通常都需要全部知道，不要拆分文件。
- 只有当 skill 内容存在条件调用或条件访问时，才拆成多个文件：`SKILL.md` 保留核心流程，条件性说明放到同一 skill 文件夹下的独立 Markdown 文件，并在 `SKILL.md` 中明确什么时候读取。

## 最终检查

- 检查 `name` 是否为英文小写、数字和连字符。
- 检查 `description` 是否短而清楚。
- 检查路径是否符合 skill 类型。
- 检查关联对象是否来自用户明确指示；没有明确指示时，不做关联。
- 检查新增引用路径是否能从当前文件实际访问。
