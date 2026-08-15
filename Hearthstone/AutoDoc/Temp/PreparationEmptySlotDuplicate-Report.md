# 备战空槽重复显示与细边框修正报告

## 任务结果

已修复拖拽后卡面内层可能未与 `UiList` 外层槽位一同归位的问题。共享条目由外层 Controller 和内层 View 组成；`UiDragable` 临时把 View 提到顶层，结束时还会排队一次旧原始位置请求。原实现只刷新三个列表的 Controller 槽位，没有覆盖内层下一帧的位置请求，因此可能出现截图中原槽仍在、卡面向下偏移的组合视觉。

当前拖拽返回后会立即把 View 局部位置和旋转复位，并通过公开 `UiTransformSetter.PosWrapper` 提交优先级 `3000` 的单帧局部零位置请求，覆盖拖拽组件排队的旧位置；随后再刷新三个 `UiList` 和页面状态。空态刷新也改为先关闭全部空态、再只打开当前绑定需要的一种，真实卡面显示完成后才提交占用态。

卡池空槽边框已改为独立透明细框。`PreparationPoolEmptySlot.png` 保持 `1024 × 1536` 与原资源路径，中心和框外为真实透明 Alpha，只保留约 `10~14 px` 浅银灰边线与小型四角菱形；浅灰底仍由根节点 `CardBackground` 提供。

## 检查项与证据

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 重复空槽根因 | 通过 | 检查 `UiDragable` 顶层换父节点、单帧旧位置请求及 `UiList` 仅管理外层 Controller 的行为 |
| 拖拽归位 | 通过 | `OnPreparationDragReturned()` 直接归零 View，并通过 `SetLocalPositionOnce(Vector3.zero, 3000)` 覆盖旧请求 |
| 空态互斥 | 通过 | `ApplyPreparationState()` 先调用 `HidePreparationEmptyStates()`，实际卡面显示后再提交占用态 |
| 空槽细边框 | 通过 | PNG 为 `1024 × 1536`、RGBA；中心 Alpha 0、角落 Alpha 0、边框采样 Alpha 251 |
| 浅灰底与滚轮 | 通过 | `EmptySlotBackgroundColor` 保持 `#CDD2DA`，根节点射线与卡池滚轮转发未改 |
| 其他绑定模式 | 通过 | 新细框只由卡池未持有态显示，未改变战斗卡、出战空槽或融合空槽资源 |
| 框架边界 | 通过 | 复用既有 Controller、View、`UiList`、`UiDragable` 和公开 `UiTransformSetter` API；未修改 BbxCommon |
| Builder / Prefab | 不适用 | 同路径 PNG 内容替换不改变 Prefab 引用或静态层级，无需执行 Builder |
| 文档 | 通过 | 玩家设计、美术模块和程序 UI 文档均已同步当前实现 |

## 验证结果

- `dotnet build Hearthstone.Tests.csproj --no-restore --verbosity:minimal`：成功，0 错误；保留 8 条既有程序集冲突警告。
- PNG 像素校验：`1024 × 1536`、`Format32bppArgb`、中心 Alpha `0`、角落 Alpha `0`、边框采样 Alpha `251`。
- `git diff --check`：通过；仅提示工作区既有 LF/CRLF 转换警告。
- 定向测试增加空槽尺寸、Alpha 导入、Prefab Sprite 引用、状态互斥和拖拽零位置请求断言。
- Unity Editor 当前由用户占用，按项目默认约定未另行进入 Play Mode；尚未进行新的运行时截图复核。

## 图片生成记录

- 使用模式：内置 `imagegen`，`precise-object-edit` 后追加一次 `background-extraction` 修正。
- 最终项目路径：`Assets/Resources/Art/Preparation/UI/PreparationPoolEmptySlot.png`。
- 首次结果因棋盘格被烘入 RGB 且中心 Alpha 为 `255` 而被拒绝，没有写入项目。
- 最终提示词：移除所有棋盘格和背景像素，在真实透明 RGBA 的 `1024 × 1536` 画布上保留同一条浅银灰细卡框；中心与框外 Alpha 必须为 `0`，只保留边框和四角小菱形，不添加填充、阴影、文字、编号、蓝色或金色区域。

## 偏差与风险

本次没有修改 Prefab 或 `.meta`，原 Sprite 引用继续指向同一路径。代码、导入属性和像素 Alpha 已静态验证，但最终边框在实际 Canvas 缩放下的粗细以及连续拖拽后的视觉仍建议在当前 Unity 会话重新进入备战页确认。

## 文档与清理结果

已更新玩家视角设计文档、美术模块文档和备战程序 UI 文档。任务结束前已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；未创建、编辑或删除任何 `.meta` 文件。
