# 修改项目链路文档 Skill 核对检查清单

- [x] 通过：规则已加入 `project-state-preflight/project-modification.md`，并明确覆盖按普通方案执行项目修改。
- [x] 通过：纯“要求出方案”仍路由到 `solution.md`，明确禁止把尚未实现的方案写入当前状态文档。
- [x] 通过：项目修改后的结束门槛要求逐类核对玩家视角设计、美术、程序文档，不再只按目录笼统检查。
- [x] 通过：强制完整读取 `design-doc-format`、`art-doc-writer`、`program-doc-format` 三类基础 skill。
- [x] 通过：主题命中更具体的项目级或底层文档 skill 时，要求检索相关元数据并完整读取适用 skill 与格式引用。
- [x] 通过：文档影响必须以实际代码、配置、资源、验证证据和玩家可见结果确定，不得以需求、方案或旧文档推测。
- [x] 通过：任务检查清单必须分别列出三类文档，并记录基础/专项 skill、现有文档、现状变化、结论、目标路径和证据。
- [x] 通过：需要同步时必须实际更新；形成新独立范围且缺少对应文档时必须实际新增，禁止只留下建议或 Todo。
- [x] 通过：确实不受影响时可标记不适用，但必须基于实际修改范围和证据说明理由。
- [x] 通过：新增或更新文档必须遵循适用 skill 的路径、格式、语言、媒体、范围和当前事实边界。
- [x] 通过：`AutoDoc/DesignPlan/` 仍不是普通现状文档同步的默认读取或修改范围。
- [x] 通过：沿用现有 project-modification、框架审计、Checklist/Report 体系，没有新增平行流程或资源。
- [x] 通过：三个引用路径均存在；目标文件为 UTF-8 无 BOM、17 行；13 项语义断言通过，旧宽松表述残留为 0。
- [x] 通过：设置 `PYTHONUTF8=1` 与 `PYTHONIOENCODING=utf-8` 后，官方 `quick_validate.py` 输出 `Skill is valid!`。
- [x] 通过：目标目录未发现 `.meta`。本次只修改流程规则，没有改变游戏现状，Program、Art、Design 正式文档同步不适用。
- [x] 通过：已逐项复核；`AutoDoc/CleanupTempDocs.bat` 仅运行一次并以 0 退出，Markdown 数量 36→36；已创建 `AutoDoc/Temp/project-modification-doc-skill-audit-Report.md`。
