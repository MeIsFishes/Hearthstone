# Unity MCP 与 UiScene Skill 更新报告

## 1. 任务结果

两项要求均已完成：

1. `recover-unity-mcp` 已加入工具不可用的完整分层排查，并同步既有故障矩阵 A 分支。流程会先检查当前环境的完整工具目录和延迟工具，只有直接与延迟工具均不存在时才进入 MCP 注册、Server 握手、会话快照、Editor 实例等后续排查。
2. `bbxcommon-ui` 已把新增 UiScene 写成可直接执行的完整流程，覆盖代码与 View Prefab 准备、UI 编辑场景创建、Exporter/Group 配置、Prefab 连接、保存导出、动态条目预加载、Stage 注册、Resources 验收和自动化收尾。

## 2. 修改文件

- `.codex/private-skills/recover-unity-mcp/SKILL.md`
  - 新增“工具不可用排查”。
  - 明确顶层工具展示不等于完整工具目录。
  - 明确 `tool_search`、延迟目录、`functions.exec` / `ALL_TOOLS`、MCP resources 和只读实调用的证据顺序。
  - 修正验证闭环，使直接或延迟工具的实际发现与调用成为判定依据。
- `.codex/private-skills/recover-unity-mcp/references/failure-matrix.md`
  - 更新 A 分支进入条件与步骤；延迟工具调用成功时退出“工具缺失”分支，不重载或重装。
- `.codex/private-skills/bbxcommon-ui/SKILL.md`
  - 将 §2.4 拆分为准备代码与 View Prefab、创建 UI 编辑场景、导出与运行时接入、验收与收尾。
  - 写明手工 Editor 与 Unity MCP 自动化的共同资产边界。

## 3. 检查项与证据

1. **通过——底层 skill 类型。** 两个对象都位于 `.codex/private-skills/`，未新建 skill 或改变 agent 关联。
2. **通过——工具不可用排查。** 已区分完整工具目录、延迟 schema、MCP 注册、Server initialize/tools/list、Editor 实例和调用错误。
3. **通过——延迟工具误判防护。** 明确 `ALL_TOOLS` 中的 `mcp__unityMCP__*` 也属于当前会话正式工具；读取 schema、resources 和只读调用成功后不得报告不可用。
4. **通过——故障矩阵一致性。** 主 skill 与 A 分支使用相同证据门槛，未保留“只看顶层列表就要求重载”的旧流程。
5. **通过——UiScene 创建流程。** 已覆盖 UiGroup/UiScene 类、View Prefab、Pre-UiInit、动态条目预加载、编辑场景、Canvas、Exporter、Group、Prefab、导出、Stage 和验收。
6. **通过——框架边界。** UI 编辑场景保持唯一配置源；禁止文件工具手写 Scene、Prefab、Asset YAML 或直接写 `UiObjectDatas`。
7. **通过——skill 格式。** name 分别为 `recover-unity-mcp` 和 `bbxcommon-ui`；description 长度为 32、40，符合中文不超过 40 字符；正文为中文。
8. **通过——引用。** 两个主 skill 中检查到的全部相对 Markdown 链接均指向现有文件。
9. **通过——条件性拆分。** 具体故障分支继续使用现有 failure matrix，完整导出细节继续使用现有 `ui-scene-export.md`，未创建重复文档。
10. **通过——修改范围。** 未触碰 Unity 资产、游戏源码、`.meta` 或 `AutoDoc/DesignPlan/`。
11. **不适用——正式现状文档。** 本次只改变代理操作规范，没有游戏实现、玩家表现或美术现状变化。

详细逐项审计见 `AutoDoc/Temp/UnityMcpAndUiSceneSkillUpdate-Checklist.md`。

## 4. 验证结果

- 完整重新读取新增章节及相邻的验证闭环、边界和修改 UiScene 章节，未发现流程矛盾。
- `recover-unity-mcp` 必需内容检查通过：完整工具目录、`ALL_TOOLS`、MCP resources、只读调用和分层故障结论均存在。
- `bbxcommon-ui` 必需内容检查通过：`ExportPath`、`FullUiGroupType`、`GenerateUiGroups`、Connected Prefab、`Resources.Load<UiSceneAsset>`、Stage 接入和禁止手写 Unity YAML 均存在。
- frontmatter 和相对引用检查通过。
- 本次为纯 Markdown skill 修改，不需要 Unity Editor、编译、Play Mode 或测试运行。

## 5. 执行偏差

无。用户明确指定维护既有 Unity MCP skill 与底层 UI skill，因此无需询问 skill 类型或新增 agent 关联。

## 6. 未解决风险

无已知未解决风险。工具发现入口属于不同客户端的实现细节，文档已使用“若环境提供”限定 `tool_search`、`ALL_TOOLS` 与 MCP resources，避免把本会话机制硬编码为所有环境的永久约定。

## 7. 文档与清理

- 正式程序、美术、玩家视角文档：不适用。
- `AutoDoc/CleanupTempDocs.bat`：仅执行一次，退出码 0。
- 清理后 `AutoDoc/Temp/` 中有 34 个 Markdown 文件，未达到清理阈值，未删除历史文档。
