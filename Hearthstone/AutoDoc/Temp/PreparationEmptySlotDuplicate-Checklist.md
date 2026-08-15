# 备战空槽重复显示与细边框修正检查清单

- [x] 通过：截图对应的风险来自拖拽对象分为 `UiList` 管理的外层 Controller 和被 `UiDragable` 临时提到顶层的内层共享 View；只刷新列表槽位不能覆盖内层排队的旧局部位置请求，视觉上会形成原槽仍在而卡面向下错位。
- [x] 通过：空态刷新先统一关闭三类空态，再按当前绑定只开启一种；实际卡面完成显示后才提交占用态，拖拽回位同时直接归零 View 并提交更高优先级单帧零位置请求。
- [x] 通过：`PreparationPoolEmptySlot.png` 已替换为约 `10~14 px` 的浅银灰细框，中心和框外 Alpha 为 0；浅灰底、编号层和根节点滚轮命中保持不变。
- [x] 通过：细框只由 `PreparationEmptyState` 在卡池未持有态显示；出战槽、融合槽和战斗卡仍使用各自既有空态或共享卡面。
- [x] 通过：继续使用 BbxCommon View/Controller、共享 `BattleCardItem`、`UiList`、`UiDragable`、公开 `UiTransformSetter.PosWrapper` 和 Unity `ScrollRect`，未增加平行条目或滚动实现。
- [x] 不适用：Prefab 已通过原路径引用 `PreparationPoolEmptySlot.png`，本次只替换同一资产内容且未改变静态层级或序列化引用；无需运行 Builder，也未手写 Prefab/Asset。
- [x] 通过：仅修改空槽 PNG、共享卡片 Controller、既有 Editor 测试和三份相关现状文档；工作区状态确认 `.meta` 未修改，未回退其他已有改动。
- [x] 通过：移除了上一版运行时把共享厚卡框塞入空态的视觉替换和 `EmptySlotFrameColor`；保留的交互初始化、归位优先级和互斥清理各有独立职责。
- [x] 通过：测试覆盖空槽尺寸、Alpha 导入、Prefab Sprite 引用、拖拽零位置请求和空态互斥；测试工程编译 0 错误，PNG 像素校验与 `git diff --check` 通过。
- [x] 通过：已读取设计文档格式 skill，并同步细浅灰框、拖拽后卡面立即回槽且不残留额外空槽的玩家体验。
- [x] 通过：已读取美术文档及模块格式，更新透明细框的尺寸、线宽、色彩、Alpha 和资产用途。
- [x] 通过：已读取程序 UI 文档格式，更新空态互斥、内外层回位、公开位置请求和空槽资源路径。
- [x] 通过：未修改 BbxCommon、UiScene、预加载映射、Prefab 层级或资源导入契约；业务侧只调用框架公开位置请求接口。
- [x] 通过：已逐项复核；`AutoDoc/CleanupTempDocs.bat` 本任务运行一次且退出码为 0；已生成对应任务报告。
