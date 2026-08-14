# Main Agent Rules

不要直接开始工作，先阅读 `.codex/private-skills/project-state-preflight/SKILL.md`，再按用户请求类型和本文件中的主代理规则决定后续流程。

Unity Editor 实时操作不强制使用 MCP。只有用户明确要求 MCP、适用 skill 明确要求本次步骤通过 MCP 执行，或主代理已经选择 MCP 作为本次操作通道时，若当前会话没有暴露 `unityMCP` 工具，或 MCP Server 启动、握手、实例发现、工具调用失败，才读取 `.codex/private-skills/recover-unity-mcp/SKILL.md`。一般 Unity 任务不得仅因 MCP 不可用而阻塞，应改用项目允许且当前环境正式提供的其他 Unity Editor 操作通道，并如实记录操作与验收方式。

主代理相关 skill 索引：

1. name: `project-state-preflight`
   desc: 将请求分为策划案编写与实现、项目修改、方案和问答并管理检查清单；实现策划案时负责选案、逐案审查与游戏验收闭环。
   path: `.codex/private-skills/project-state-preflight/SKILL.md`
2. name: `plan-output-format`
   desc: 游戏模块设计的 Plan 模式强制输出结构：按需求、数据、逻辑、UI、美术、GameStage、其他资产与补充项组织，并遵循跳过与重编号规则。编写 plan 时必须严格使用此结构，并与 game-module-design 配合使用。
   path: `.codex/private-skills/plan-output-format/SKILL.md`
3. name: `config-data-design`
   desc: 说明如何使用 DataApi，以及如何设计新的配置数据（BbxScriptableObject 与 CsvData 的选型）。
   path: `.codex/private-skills/config-data-design/SKILL.md`
4. name: `bbxcommon-ecs`
   desc: 说明 BbxCommon ECS 的业务入口、可新建类型和生命周期。
   path: `.codex/private-skills/bbxcommon-ecs/SKILL.md`
5. name: `game-module-design`
   desc: 当用户要求设计游戏模块时，阅读并遵循此文档。
   path: `.codex/private-skills/game-module-design/SKILL.md`
6. name: `game-stage`
   desc: 设计 GameStage、编写入口并进入特定游戏 StageGroup 时使用。
   path: `.codex/private-skills/game-stage/SKILL.md`
7. name: `bbxcommon-ui`
   desc: 设计、创建或修改 BbxCommon 页面、Hud 与 UiScene 时使用。
   path: `.codex/private-skills/bbxcommon-ui/SKILL.md`
8. name: `add-skill`
   desc: 添加或编辑项目、底层或通用 skill 的流程。
   path: `.codex/private-skills/add-skill/SKILL.md`
9. name: `subagent-toml`
   desc: 添加或编辑 subagent 的流程。
   path: `.codex/private-skills/subagent-toml/SKILL.md`
10. name: `bbxcommon-task`
   desc: 说明 BbxCommon Task 的使用、节点模板与底层流程。
   path: `.codex/private-skills/bbxcommon-task/SKILL.md`
11. name: `task-workflow`
   desc: 设计 Task 图集或编写 Task 节点的开发流程。
   path: `.codex/private-skills/task-workflow/SKILL.md`
12. name: `bbxcommon-ui-item`
   desc: 新增或修改 BbxCommon UI 组件时使用。
   path: `.codex/private-skills/bbxcommon-ui-item/SKILL.md`
13. name: `initialize-empty-project`
   desc: 判定空项目并搭建目录、基础文件及基础架构。用于新建或近空项目初始化。
   path: `.codex/private-skills/initialize-empty-project/SKILL.md`
14. name: `bbxcommon-resource`
   desc: 使用 ResourceApi 读取资源；修改资源或 Mod 底层时查阅。
   path: `.codex/private-skills/bbxcommon-resource/SKILL.md`
15. name: `bbxcommon-localization`
   desc: 配置语言 CSV，并通过 LocApi 与 UiLocText 使用本地化。
   path: `.codex/private-skills/bbxcommon-localization/SKILL.md`
16. name: `project-overview-config`
   desc: 项目类型理解变化时更新项目总览配置。
   path: `.codex/private-skills/project-overview-config/SKILL.md`
17. name: `test-game-content`
   desc: 进入指定游戏内容时，通过框架 StageGroup 配置参数并直接启动。
   path: `.codex/private-skills/test-game-content/SKILL.md`
18. name: `recover-unity-mcp`
   desc: 已明确选用 Unity MCP 且链路失败时，按证据诊断恢复并验证。
   path: `.codex/private-skills/recover-unity-mcp/SKILL.md`

## Subagent 使用规则

主代理准备开启 subagent 前，必须先检查本文件中的 subagent 注册表，判断是否已有相关子代理配置可以直接使用。

如果已有职责匹配的 subagent，优先使用现有配置；只有现有 subagent 不匹配、用户明确要求新建，或任务需要新的独立职责时，才考虑新增 subagent。

## 可用 Subagent 注册表

1. name: `design_doc_writer`
   desc: 根据主代理描述直接维护玩家视角设计文档。
   path: `.codex/agents/design-doc-writer.toml`
2. name: `art_doc_writer`
   desc: 根据主代理描述直接维护2D图片美术文档。
   path: `.codex/agents/art-doc-writer.toml`
3. name: `task_checker`
   desc: 检查现有 Task 节点能否拼出图集。
   path: `.codex/agents/task-checker.toml`
4. name: `design_plan_code_reviewer`
   desc: 审查策划案实现的代码质量与框架边界。
   path: `.codex/agents/design-plan-code-reviewer.toml`
5. name: `design_plan_plan_reviewer`
   desc: 审查策划案实施 Plan 的需求覆盖与框架边界。
   path: `.codex/agents/design-plan-plan-reviewer.toml`
