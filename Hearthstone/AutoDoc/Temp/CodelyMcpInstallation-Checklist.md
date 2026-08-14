# Codely MCP 安装检查清单

- [x] 通过：从 Unity 中国／团结引擎官方文档和官方 Registry 确认包名 `cn.tuanjie.codely.bridge`、版本 `1.0.75`、下载地址、TCP 传输和默认端口 `25916`。
- [x] 通过：已在 `AGENTS.md` 和 `.codex/mcp/README.md` 明确 Codely Bridge 是 Unity Editor TCP Bridge，不将其伪装成可直接启动的标准 MCP Server。
- [x] 通过：官方归档及完整包保存在 `.codex/mcp/codely-bridge-1.0.75/`，`SOURCE.json` 固定来源、版本、SHA-1 与 SHA-512，README 记录升级方式。
- [x] 通过：归档按原始 `package/` 结构完整解出并由 `Packages/manifest.json` 使用合法的本地 UPM `file:` 依赖加载；未拆散或改写包内文件。
- [x] 通过：根目录 `AGENTS.md` 已明确 Unity Editor 操作的桥接优先级、前置条件、刷新方式、不可用时的报告义务和禁止绕过边界。
- [x] 通过：框架边界保持为官方 Editor 桥接公开工具；未建立第二套私有 Socket/MCP 协议，未手写 Unity 生成资产。
- [x] 通过：新增内容仅为官方包、来源说明及 JSON/UPM 配置；三个 JSON 均可解析，本地依赖路径、包名和版本均验证有效，无一次性代码抽象。
- [x] 不适用：本次未改变游戏运行时、程序架构、策划、玩家设计或美术现状，不需要同步正式项目文档。
- [x] 通过：改动限定为 `.codex/mcp/`、`Packages/manifest.json`、`AGENTS.md` 和本任务临时文档；`Packages/manifest.json` 是使下载包被 Unity 加载的直接必要配置。
- [x] 通过：已完成逐项审计；下一步只运行一次 `AutoDoc/CleanupTempDocs.bat`，随后生成同名报告。Unity 未运行，故 Editor 实际解析与连接状态留待下次启动后由 Bridge 配置确认。
