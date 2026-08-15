# 备战卡池遮罩与空槽交互修正检查清单

- [x] 通过：定位截图中已持有卡面被深蓝空槽块遮挡的根因；旧 `PreparationPoolEmptySlot` 是位于卡面内容之上的全不透明空态图，现改为透明中心共享框，并在真实卡面显示前强制关闭全部空态层。
- [x] 通过：卡池未持有卡槽使用 `#CDD2DA` 浅灰卡底和 `#EBEEF2` 浅灰共享框，编号仍由既有空态编号层显示。
- [x] 通过：关闭空槽旧透明点击子层的监听器与射线命中，根节点继续接收 `IScrollHandler` 并转发到卡池 `ScrollRect`。
- [x] 通过：`ShowCardPresentation()` 统一调用 `HidePreparationEmptyStates()`，覆盖卡池、出战槽和融合槽三种空态的对象池复位路径。
- [x] 通过：继续使用 BbxCommon View/Controller、共享 `BattleCardItem`、`UiList` 与 Unity `ScrollRect`，未增加平行卡面或滚动实现。
- [x] 不适用：本次未修改静态 UI 层级、Prefab 或 Asset；空态视觉和射线状态由既有共享 Controller 生命周期配置，因此无需运行 Builder，也未手写 Prefab/Asset。
- [x] 通过：仅修改共享卡片 Controller、既有 Editor 测试和三份现状文档；未修改任何 `.meta`，未回退工作区内其他已有改动。
- [x] 通过：新增的两种颜色、初始化函数和空态清理函数均分别承担视觉规格、兼容输入清理和复用复位职责；未保留诊断回调或一次性分支。
- [x] 通过：定向测试覆盖颜色、空槽射线关闭、空态清理和滚轮转发；`dotnet build Hearthstone.Tests.csproj --no-restore --verbosity:minimal` 为 0 错误，`git diff --check` 通过。
- [x] 通过：已读取设计文档格式要求，并同步浅灰空槽、无不透明遮罩和空槽滚动体验。
- [x] 通过：已读取美术文档格式要求，并同步 `#CDD2DA`、`#EBEEF2` 与旧深蓝空槽资源不再用于活跃表现的现状。
- [x] 通过：已读取程序 UI 文档格式要求，并同步空槽状态、根节点滚轮命中和对象池复位逻辑。
- [x] 通过：未修改 BbxCommon、UiBuilder、UiScene、预加载映射或资源导入流程；只使用现有公开资源加载与页面滚动转发入口。
- [x] 通过：已逐项复核；`AutoDoc/CleanupTempDocs.bat` 本任务运行一次且退出码为 0；已生成对应任务报告。
