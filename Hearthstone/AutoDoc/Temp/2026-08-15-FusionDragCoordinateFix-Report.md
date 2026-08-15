# 融合拖拽坐标修复报告

## 1. 任务结果

已修复融合素材拖离融合区后卡牌视觉错位的问题。融合槽外松手仍按规则退回牌库；卡牌 View 回到原 Controller 后恢复拖拽前的局部位置，不再保留释放点产生的偏移。通用 `UiDragable` 同时改为把指针屏幕坐标换算到当前 UI 平面，兼容 CanvasScaler 非 1 缩放。

## 2. 实现内容

- `UiDragable` 改为继承 `BbxUiItem`，公开配置与 Wrapper 接口保持不变。
- 拖拽开始和持续阶段通过 `RectTransformUtility.ScreenPointToWorldPointInRectangle()` 获取指针在当前 Canvas UI 平面上的世界坐标。
- 非固定偏移模式继续保留按下点与卡牌之间的世界偏移；固定偏移模式把配置的局部 UI 偏移通过完整 Transform 换算到世界空间。
- 拖拽结束时先归还 Top 层节点，再以 `UiTransformSetter` 恢复拖拽开始时的局部坐标。
- 删除 `BattleCardItemController` 中读取释放后 `m_View.transform.localPosition` 并以优先级 3000 重写的错误补偿。
- 保留融合素材在 `FusionSlotList` 区域外松手时调用 `RemoveFusionMaterial()` 的规则，区域内仍支持原槽回落和槽间移动。

## 3. 检查项结果与证据

- 用户行为：通过。素材拖离融合区域由融合 session 清除，牌库卡面随统一刷新恢复。
- View/Controller 坐标：通过。View 只恢复自身原始局部位置；`UiList` 继续只排列其持有的 Controller 外层节点。
- 屏幕/世界坐标：通过。`UiDragable` 已无 `eventData.position.AsVector3XY()`，改用 Unity UI 坐标换算 API。
- 兼容性：通过。公开字段、事件、Top 层切换入口和 `UiTransformSetter` 优先级契约未改变。
- 框架边界：通过。通用能力位于 BbxCommon 组件，玩法 Controller 只负责融合区域规则，没有保留平行拖拽定位实现。
- UI 组件文档：通过。已同步 `AutoDoc/UIItem/UiDragable/UiDragable.md`；总索引已有该组件，无需改名或新增条目。
- 玩家设计文档：通过且无需修改。现有备战卡池设计文档已经描述“拖离融合区退回牌库且不消失/错位”，修复后实现与文档一致。
- 美术文档：不适用。本次没有视觉资产、Prefab 布局或美术规格变化。
- 程序文档：通过。已更新备战 UI 程序文档中的坐标换算、View 归位和 UiList 职责说明。

## 4. 验证结果

- 使用 Unity 2022.3.62f3c1 自带 Roslyn 和现有 Bee 响应文件独立编译 `BbxCommon`：成功；仅出现项目原有的 `ResourceManager.ToString()` 隐藏成员与枚举 switch 不完整两条警告。
- 独立编译 `Hearthstone`：成功，无新增警告或错误。
- 独立编译 `Hearthstone.Tests`：成功，无新增警告或错误。
- 手工执行源码回归断言：通过，确认区域外回收规则存在、旧释放偏移补偿已删除、屏幕坐标不再直接当世界坐标、归还父节点发生在局部位置恢复之前。
- `git diff --check`：通过，只有仓库既有的 CRLF 转换提示。

## 5. 偏差与未解决风险

- 按项目默认规则未进入 Play Mode，因此没有在 Game View 中实际拖动卡牌验收。
- 当前 Unity Editor 在验证期间没有自动刷新程序集；本次使用同版本 Unity 的编译器和项目现有响应文件完成独立编译。Editor Test 源码已编译，但未通过 Unity Test Runner 实际执行。
- 工作区原本存在大量与本任务无关的用户修改和未跟踪资源；本次没有还原或改写这些内容。

## 6. 文档处理

- 更新：`AutoDoc/UIItem/UiDragable/UiDragable.md`
- 更新：`AutoDoc/Program/UI/preparation/preparation.md`
- 核对但未修改：`AutoDoc/UIItem/UiItemIndex.md`
- 核对但未修改：`AutoDoc/Design/Specific/preparation-card-pool/preparation-card-pool.md`
- 美术文档不受影响。

## 7. 清理结果

任务结束审计后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`。该脚本没有匹配独立编译产生的 `CoordinateValidation` DLL/PDB，因此随后按本任务的九个明确文件名删除这些临时二进制，没有再次运行清理脚本；随后创建本报告。
