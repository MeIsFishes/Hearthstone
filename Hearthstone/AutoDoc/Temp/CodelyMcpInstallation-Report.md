# Codely MCP 安装报告

## 结果

已将官方 Codely Bridge `1.0.75` 固定到项目 `.codex/mcp/codely-bridge-1.0.75/`，并通过 `Packages/manifest.json` 的本地 UPM 依赖接入 Unity。根目录 `AGENTS.md` 已补充 Unity Editor 操作规则。

Codely Bridge 官方实现是 TCP Socket Bridge（默认端口 `25916`），不是可由 Codex 直接以 stdio/SSE/HTTP 启动的标准 MCP Server。当前会话还需要兼容的 Codely/Codex 客户端适配层暴露 Unity Tools，才可以从对话调用 Bridge。

## 检查项与证据

1. **通过——官方来源与版本**：官方 Registry 包名为 `cn.tuanjie.codely.bridge`，下载并固定版本 `1.0.75`；来源记录在 `.codex/mcp/codely-bridge-1.0.75/SOURCE.json`。
2. **通过——协议边界**：官方 Unity Tools 文档说明 Editor 侧使用 TCP Socket；`AGENTS.md` 与 `.codex/mcp/README.md` 均已明确其不是标准 MCP 协议。
3. **通过——下载完整性**：归档大小 `26,980,537` 字节；SHA-1 为 `50786b10553761505d2eeedc2521fa52ebf967fc`，SHA-512 integrity 与官方 Registry 完全一致。
4. **通过——归档安全与结构**：归档共 `413` 个条目，均位于 `package/` 下，无绝对路径或 `..` 路径穿越；按官方目录结构完整保留，未改写包内文件。归档自带的 Unity `.meta` 文件亦保持原样，未由本任务创建或编辑。
5. **通过——Unity 包接入**：`Packages/manifest.json` 使用 `file:../.codex/mcp/codely-bridge-1.0.75/package`；从 `Packages/` 解析后路径存在，包内 `package.json` 的名称与版本和来源记录一致。
6. **通过——操作规则**：`AGENTS.md` 已写明连接前置条件、优先使用 Codely Unity Tools／MCP 适配工具、`unity_refresh` 重连方式、不可用时明确报告，以及禁止私有协议、桌面自动化和手写 Unity 资产绕过。
7. **通过——框架边界**：未新建第二套 Unity Socket/MCP 实现，未修改游戏运行时代码、Scene、Prefab 或 `.asset`。
8. **不适用——正式项目文档同步**：本次只接入开发工具，不改变程序、美术、玩家设计或策划现状，因此未修改正式项目文档。
9. **通过——范围审计**：实质改动限定为 `.codex/mcp/`、`Packages/manifest.json`、`AGENTS.md` 和本任务临时文档。
10. **通过——结束清理**：逐项审计后只运行了一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`；脚本未移除现有临时文档。

## 验证结果

- `Packages/manifest.json`、官方包 `package.json`、`SOURCE.json` 均可作为 JSON 成功解析。
- 本地 UPM 引用解析到 `.codex/mcp/codely-bridge-1.0.75/package`，目标及关键文件 `package.json`、`README.md`、`Editor/` 均存在。
- SHA-1 和 SHA-512 校验均通过；归档路径安全检查通过。
- 当前 Unity Editor 未运行，项目根目录尚无 `.com-unity-codely.json`，因此未执行 Editor 内包解析、编译和实际连接测试。Unity 下次打开并解析包后，应以该配置和 Bridge 窗口状态为准。

## 偏差与剩余风险

- 用户称其为“MCP”，但官方文档将 Codely Bridge 定义为 TCP Bridge；本任务保留这一事实，没有伪造标准 MCP 启动配置。
- 当前 Codex 会话未暴露 Codely Unity Tools／客户端适配工具，因此现在不能直接从会话操作 Editor。仅下载 Editor 包并不能替代客户端适配层。
- Unity 首次解析本地包时仍可能受到项目其他 UPM 依赖或网络状态影响；本次未启动 Unity，避免在没有可用 Bridge 适配工具时绕过新增规则。
