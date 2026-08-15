# UiBuilder Unity MCP 启动流程检查清单

- [ ] 用户要求：在 `UiBuilder` 流程中明确先从全部可用工具中查找 Unity MCP；发现时使用 MCP 启动 Builder。
- [ ] 用户要求：Unity MCP 只用于启动 Builder，后续流程允许不再使用 MCP。
- [ ] 保留 Unity MCP 不可用时的既有可执行通道与“不因 MCP 不可用而阻塞”边界。
- [ ] Skill 类型与位置核对：目标为既有底层 skill `.codex/private-skills/bbxcommon-ui/SKILL.md`，不新增 skill、不改变 agent 关联。
- [ ] Skill 结构核对：保留既有英文 `name`、精简中文 `description`、YAML frontmatter 与引用路径。
- [ ] 条件访问核对：新增规则仅修改 UiBuilder 执行通道选择，不引入需要拆分文件的新条件资料。
- [ ] 框架边界审计：修改不绕过 BbxCommon UI、UiBuilder、Unity Editor、UiSceneExporter 或资产配置源，不引入菜单、自动回调、手写 YAML 或平行 Builder 流程。
- [ ] 误改与依赖核对：只修改目标 UiBuilder 流程及本任务检查清单/报告，不触碰无关文件或 `.meta`。
- [ ] 玩家视角设计文档：读取格式 skill，依据实际修改判断是否需要同步。
- [ ] 美术文档：读取格式 skill，依据实际修改判断是否需要同步。
- [ ] 程序文档：读取格式 skill，依据实际修改判断是否需要同步。
- [ ] 验证：复读修改段落并检查前后规则无自相矛盾，确认目标语句可被后续代理直接执行。
- [ ] 结束审计：逐项记录证据，只运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建同名 `*-Report.md`。
