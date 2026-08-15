# 完成策划案流程 Agent 分工调整检查清单

- [通过] 用户要求：取消策划案实施中 plan 阶段和 code 阶段的子 agent/并行要求，保留 review 阶段使用子 agent。
  - 证据：`design-plan-implementation.md` 规定多篇策划案逐篇完成；步骤 a/c 不再启动或交接执行子代理，步骤 b/d 仍调用 `design_plan_plan_reviewer` 与 `design_plan_code_reviewer`。
- [通过] 表述约束：只删除或收敛子 agent 执行 plan、code 的相关内容，不反复强调“由主 agent 执行”。
  - 证据：步骤标题改为“输出实施 Plan”“执行已通过审查的 Plan”，正文直接描述操作；未新增重复的 plan/code 主代理归属声明。
- [通过] 定位所有直接描述“完成/实现策划案”流程及其 agent 分工的生效文件，避免遗漏直接依赖。
  - 证据：检索 `.codex` 后修改 `project-state-preflight/SKILL.md` 与 `design-plan-implementation.md`；两个 reviewer TOML 仅定义只读审查职责，保持不变。
- [通过] 按现有 skill 结构修改相关流程，不建立平行流程、无关文件或额外辅助文档。
  - 证据：仅在既有 preflight 主文件及其条件流程文件中修改；没有新增 `.codex` 流程或 agent 配置。
- [通过] Skill 审计：保持既有名称、frontmatter、路径、条件引用和主 agent 关联有效；确认描述仍精简且与实际流程一致。
  - 证据：`project-state-preflight` 的 `name`、`description` 与路径未改；`game-module-design`、`plan-output-format`、`test-game-content` 和两个 reviewer TOML 引用均通过 `Test-Path`。
- [通过] Review 审计：plan review 与 code review 仍由对应 review 子 agent 执行，审查门槛不被削弱。
  - 证据：步骤 b 保留独立 Plan 审查及硬阻塞门槛；步骤 d 和验收返修保留代码审查/复审、轮次及上限规则。
- [通过] 框架边界审计：修改仅发生在现有 `.codex` 流程和 agent 体系内，不绕过或重复建立框架能力。
  - 证据：沿用 preflight 路由、注册表 reviewer、现有 Plan/Review 路径和 a～h 门槛，未新增平行机制。
- [通过] 无关改动审计：不创建、编辑或删除任何 `.meta` 文件，不误改策划案正文、游戏代码或其他流程。
  - 证据：本任务定向状态仅包含两份 `.codex` 流程文件与本检查清单；工作区已有未跟踪融合特效 `.meta` 均保持任务外原状。
- [不适用] 玩家视角设计文档：根据实际修改核对是否受影响，并记录基础/专项 skill、相关文档、处理结论与证据。
  - 证据：已完整读取 `design-doc-format/SKILL.md`；检索 `AutoDoc/Design/` 未发现策划案执行代理流程相关文档。本次只改变内部 agent 工作流，不改变玩家可见现状，无专项 skill 或目标文档。
- [不适用] 美术文档：根据实际修改核对是否受影响，并记录基础/专项 skill、相关文档、处理结论与证据。
  - 证据：已完整读取 `art-doc-writer/SKILL.md`；检索 `AutoDoc/Art/` 未发现相关流程文档。本次没有资产或视觉现状变化，无专项 skill 或目标文档。
- [不适用] 程序文档：根据实际修改核对是否受影响，并记录基础/专项 skill、相关文档、处理结论与证据。
  - 证据：已完整读取 `program-doc-format/SKILL.md`；检索 `AutoDoc/Program/` 未发现相关流程文档。本次没有代码、配置、接口或运行流程变化，无专项格式或目标文档。
- [通过] 验证修改后的相关文档中不再要求 plan/code 子 agent 或两阶段并行，同时 review 子 agent 描述仍完整。
  - 证据：定向 `rg` 对“执行子代理/并行策划案/并行组/并行分组/合并验收”无命中；步骤 a/c 存在，步骤 b/d 及两 reviewer 标识均存在；`git diff --check` 对目标文件通过。
- [通过] 结束前逐项复核并记录证据。
  - 证据：已重新打开本清单，逐项核对目标文件、定向检索、引用路径、差异检查和文档影响；没有可修正缺口或未解决风险。

复核完成后只运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建对应 Report。
