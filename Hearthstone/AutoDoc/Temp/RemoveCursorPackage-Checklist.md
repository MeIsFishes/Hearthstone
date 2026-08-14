# 移除 Cursor Unity 包检查清单

- [x] 通过：已从 `Packages/manifest.json` 删除 `com.boxqkrtm.ide.cursor` 顶层依赖；JSON 解析通过，其他依赖未改动。
- [x] 通过：`Assets/Scripts/` 没有对 Cursor IDE 包的引用；唯一命中是 Unity 编辑器的 `MouseCursor.ResizeHorizontal`，与该 UPM 包无关。
- [ ] 待 Unity 刷新：当前已运行的 Editor 没有重新解析外部 manifest 改动，`Packages/packages-lock.json` 仍保留本次会话的旧 Cursor 记录；下次重启后由 UPM 自动清理，未手改锁文件。
- [x] 通过：移除 manifest 声明后日志没有新增包解析或编译错误，既有项目程序集仍存在，Codely Bridge 持续为 `ready`。
- [x] 通过：框架边界保持为 UPM manifest；未手动删除 `Library/PackageCache`、未私接 Socket、未强制关闭可能含未保存状态的 Unity Editor，也未建立替代编辑器集成。
- [x] 通过：改动仅涉及 `Packages/manifest.json` 与本任务临时文档；没有创建、编辑或删除 `.meta`，没有改动其他包、游戏代码、资源或策划案。
- [x] 不适用：删除可选 IDE 开发工具不改变游戏、程序框架、美术或玩家视角设计现状，无需同步正式项目文档。
- [x] 通过：已完成逐项审计；下一步只运行一次 `AutoDoc/CleanupTempDocs.bat`，随后生成 `RemoveCursorPackage-Report.md`。
