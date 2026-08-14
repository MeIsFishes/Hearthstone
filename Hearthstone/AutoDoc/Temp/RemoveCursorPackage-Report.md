# 移除 Cursor Unity 包报告

## 任务结果

已从 `Packages/manifest.json` 删除可选的 `com.boxqkrtm.ide.cursor` Git 依赖。项目下一次由 Unity Package Manager 解析时将不再下载或加载该包，也不会再因访问其 GitHub 仓库而阻塞包解析。

## 检查项结果与证据

1. **通过——删除顶层依赖**：`Packages/manifest.json` 已不存在 `com.boxqkrtm.ide.cursor`，文件可作为 JSON 正常解析；其他依赖未改动。
2. **通过——调用方检查**：`Assets/Scripts/` 不引用 Cursor IDE 包。`GameStageWindow.cs` 中的 `MouseCursor.ResizeHorizontal` 是 Unity 鼠标指针 API，与 Cursor 编辑器无关。
3. **待 Unity 刷新——锁文件清理**：当前 Unity Editor 未对外部 manifest 改动执行新的 UPM 解析，`Packages/packages-lock.json` 暂时仍保存旧会话锁定的 Cursor 提交。未手工改写 Unity 管理的锁文件；下次重启 Editor 后应由 UPM 自动移除。
4. **通过——当前稳定性**：修改后 Editor 日志没有新增包解析错误或编译错误，已有 `Hearthstone.dll`、`BbxCommon.dll` 和 Codely Bridge 程序集仍存在。
5. **通过——Codely Bridge**：`.com-unity-codely.json` 持续报告 `reason: ready` 且心跳更新；Cursor 包与 Bridge 无依赖关系。
6. **通过——框架边界**：仅修改 UPM 的声明源；没有删除 `Library/PackageCache`、私接 Bridge Socket、使用桌面自动化或强制关闭可能含未保存状态的 Editor。
7. **通过——范围审计**：实质改动仅为 `Packages/manifest.json` 和本任务临时文档；未修改任何 `.meta`、游戏代码、资产或无关包。
8. **不适用——正式文档**：可选 IDE 集成的移除不改变游戏或项目框架现状，无需同步程序、美术、玩家设计或策划案文档。
9. **通过——清理**：结束审计后只运行了一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`。

## 验证结果

- `Packages/manifest.json` JSON 解析通过，Cursor 顶层依赖不存在。
- 项目脚本无 Cursor IDE 包调用方。
- 修改后未发现新增 Unity 编译或包解析错误。
- Codely Bridge 运行状态为 `ready`。

## 执行偏差与剩余风险

当前 Codex 会话没有暴露 Codely Unity Tools／MCP 客户端适配工具，因此无法按项目规则从 Editor 内触发 `unity_refresh`。为避免绕过 Bridge 规则或破坏用户未保存的 Editor 状态，本任务没有强制关闭 Unity，也没有手工改写 `packages-lock.json`。在当前 Editor 重启前，Package Manager 界面可能仍显示旧缓存；重启并解析成功后该状态应消失。
