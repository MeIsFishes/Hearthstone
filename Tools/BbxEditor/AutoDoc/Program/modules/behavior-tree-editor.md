# 行为树编辑器

## 1. 模块说明

本模块使用 `BehaviorTreeDocument` 保存节点和边，使用 `BehaviorTreeDocumentViewModel` 执行节点、连接、顺序和页签内节点搜索，并由 `BehaviorTreeCanvas` 在 WPF 中完成分层灰色网格、圆角节点、端口、Bezier 连线、搜索高亮和视图定位。行为树保持唯一根、单父、无环和端口类型匹配的严格树语义；搜索索引是页签级临时状态，不进入任务文档或磁盘缓存。

## 2. 对外接口

- `BehaviorTreeDocument.Nodes`、`Edges`：节点和有序连接集合。
- `BehaviorTreeDocumentViewModel.AddTask`、`RemoveNode`、`TryConnect`、`NodeMoved`：图编辑入口。
- `ConnectionSource`、`SelectedPort`、`AvailablePorts`：连接源和端口选择状态。
- `BehaviorTreeCanvas.ViewModel`：画布绑定入口。
- `BehaviorTreeDocumentViewModel.FindNodesAsync`、`NodeSearchIndexStatus`：按标题/类型名检索节点，以及报告语义索引是否可用。
- `BehaviorTreeNodeSearch.Rank`：按“标题字面、类型名字面、标题向量、类型名向量”四级顺序合并并排序节点结果。
- `BehaviorTreeCanvas.CenterOnNode`：保持当前缩放比例，将目标节点中心移动到视口中心。
- `DocumentValidator.Validate`、`RuntimeExporter.Export`：最终树语义验证和运行时转换。

## 3. 调用链路

工作区创建行为树文档后，ViewModel 把 Nodes/Edges 暴露给编辑器。画布订阅集合和属性变化并重绘；普通节点和 Condition 节点使用不同的低饱和填充色，悬停、选择、连接源与拖动状态使用不同边框和光标反馈。工具栏添加命令打开节点类型选择器，排除 Timeline 类型并避免重复 Root。鼠标命中节点时更新选择，拖动节点调用 `NodeMoved`。从输出端口按下时画布进入预览状态，释放到兼容节点时调用 `TryConnect`；释放到空白处时先结束鼠标捕获，再按普通端口或 Condition 端口筛选候选类型，确认后创建节点并连接。

`TryConnect` 检查源/目标、Condition 类型、Single 上限、目标单父和可达性，成功后添加 Edge 并重排同端口 Order。保存时验证器再次检查唯一根、根无父、悬空边、自连、环、多父、端口和顺序；导出器按节点实际顺序分配唯一 TaskId，并把根实际 ID 写入 `RootTaskId`。

行为树页签固化时调用 `OnPinned`，以当前所有节点标题和规范化类型名请求一次临时向量化；全部向量完成后才计算语料中心并保存居中归一化的内存向量。节点增删或标题变化会取消旧任务并重建；页签关闭、预览替换或应用退出时取消任务并释放索引。向量尚未完成、模型未启用或查询失败时，`FindNodesAsync` 只返回不区分大小写的字面包含结果。索引可用后，对标题和类型名共享的临时语料执行居中查询排序；每个节点只保留四级匹配中顺序最靠前的一项。

编辑器内按 Ctrl+F 展开搜索栏。输入搜索词后按 Enter 或点击 Next 会循环当前结果，更新 `SelectedNode` 和独立搜索高亮，并调用 `CenterOnNode` 把节点移到视口中心。索引版本变化后下一次跳转会重新计算结果，因此在向量构建期间使用过字面搜索的同一查询，构建完成后可自然扩展到语义结果。

## 4. 数据来源

- `BehaviorTreeDocument` 中的节点、位置和 Edge。
- `TaskCatalog` 的任务标签、字段和 ConnectPoint 类型。
- 旧 NodeGraph `.editor.json` 的节点位置、端口索引和连接顺序。
- 节点类型选择器返回的 TaskDefinition 与 Inspector 写回的字段值。
- 当前固化页签中节点标题和规范化类型名生成的临时向量、完成时计算的语料中心，以及当前搜索词；这些数据不写入磁盘。

## 5. 与其他模块的依赖

本模块依赖应用工作区提供节点类型选择器、页签固化/释放生命周期和共享 embedding worker，依赖 Inspector 编辑节点字段，依赖文件格式模块恢复/保存旧 NodeGraph 形状。临时索引复用 `VectorSearchCoordinator` 的模型进程，但不使用 `vector-index.json` 或 CSV 向量缓存。它不依赖 Timeline 文档、时间项或条件容器。
