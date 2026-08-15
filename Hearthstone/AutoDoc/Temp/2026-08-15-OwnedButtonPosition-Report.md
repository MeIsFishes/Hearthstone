# “查看拥有”按钮位置调整报告

## 任务结果

任务因当前 Codex 会话的 Unity MCP 传输关闭而未完成，项目没有保留半成品修改。`PreparationViewUiBuilder`、布局测试与 `PreparationView.prefab` 均保持原坐标 `(-720, 285)`。

目标位置已经确定为 `(-770, 285)`：卡池面板宽 1780，左边缘为 `-890`；按钮宽 240、中心 X 为 `-770` 时左边缘同样为 `-890`，可以顶住卡池区域左上角。Y、尺寸、文字、图片和交互不需要改变。

## MCP 诊断与验证

- 当前完整工具目录存在 `mcp__unityMCP__*` 工具。
- `codex mcp get unityMCP` 显示标准 stdio 服务已启用，Server 与 Unity 包均为 10.0.0。
- Unity Editor 正在运行；Editor 日志显示 stdio Bridge 已在端口 6401 启动。
- 标准 SDK 只读探针成功完成 initialize 与 `tools/list`，并成功调用 `manage_scene(get_active)` 和 `read_console(error)`：活动场景为 `Assets/Scenes/Main.unity`、场景未脏、Console 返回 0 条错误。
- 当前会话原生 `manage_scene` 和 `execute_code` 仍返回 `Transport closed`。这证明后端健康，断点位于当前 Codex 会话已关闭的 MCP 传输；必须重载或新建会话才能恢复原生写入工具。

按 `recover-unity-mcp` 边界，只读探针不能代替 MCP 执行 Unity 资产写操作，因此没有用探针调用 Builder，也没有手写 Prefab YAML。

## 检查项结论

| 范围 | 状态 | 证据 |
| --- | --- | --- |
| 按钮左移并贴齐 | 未通过 | 目标坐标已确定，但 MCP 无法执行 Builder，Prefab 未改变。 |
| 保持尺寸与样式 | 不适用 | 没有落地按钮改动，当前表现维持原状。 |
| 定位配置源与依赖 | 通过 | 已定位 Prefab、Builder、View、Controller、测试和三类现状文档。 |
| Builder 与 Prefab 同步 | 未通过 | 当前会话 MCP 固定返回 `Transport closed`。 |
| 框架边界 | 通过 | 未添加运行时补丁，未手写 Prefab/UiSceneAsset，未留下配置源与产物不一致。 |
| UiScene 导出 | 不适用 | 页面内部子控件坐标不改变页面整体导出信息。 |
| 玩家设计、美术、程序文档 | 不适用 | 实际界面与项目配置未改变，正式现状文档不应记录未实现目标。 |
| `.meta` 与无关文件 | 通过 | 未创建、编辑或删除项目 `.meta`；未触碰无关项目文件。 |

## 执行偏差与清理

在用户补充“不要创建副本”前曾开始创建隔离 Unity 副本；收到指令后立即停止并彻底删除，未从副本生成或回写任何资产。准备中的 Builder/测试坐标修改也已撤回，避免留下无法由 Prefab 验证的半成品。

`AutoDoc/CleanupTempDocs.bat` 已且仅已运行一次，退出码 0；清理后共有 167 个 Markdown，低于 500 个的裁剪阈值。本报告在清理后创建。

## 恢复方式

重载或新建 Codex 会话后，重新提出“把查看拥有按钮往左移，顶住左上角，并只用 MCP”。新会话应先用原生 MCP 完成只读调用，再把 Builder 坐标改为 `(-770, 285)`、执行 `Hearthstone.PreparationViewUiBuilder.Build()` 并运行相关 EditMode 测试。
