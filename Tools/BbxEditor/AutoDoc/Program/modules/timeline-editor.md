# Timeline 编辑器

## 1. 模块说明

本模块由 Core `TimelineDocument`、`TimelineItem` 和 WPF `TimelineDocumentViewModel`、`TimelineEditorControl`、`TimelineOverviewControl` 组成。每个时间项持有一个 Action 任务、开始时间、持续时间，以及 EnterConditions、Conditions、ExitConditions 三组条件。它与行为树使用不同的领域模型和视图。

## 2. 对外接口

- `TimelineDocument.Items`：有序时间项集合。
- `TimelineDocumentViewModel.AddTaskCommand`、`MoveUpCommand`、`MoveDownCommand`、`DeleteCommand`：时间项操作。
- `AddEnterConditionCommand`、`AddConditionCommand`、`AddExitConditionCommand`：三类条件入口。
- `TimelineOverviewControl.Document`、`SelectedItem`、`Catalog`：左侧 Action 节点列、右侧时间轨道、选择与拖动绑定。
- `RuntimeExporter.Export`：Timeline 根任务、Action 和条件的运行时转换。

## 3. 调用链路

工作区创建 Timeline 文档后，ViewModel 把 Items 暴露给统一时间轨道。用户执行添加 Action 命令时打开节点类型选择器，候选集仅包含带 Action 标签的任务；确认后 ViewModel 创建并选中 `TimelineItem`。移动和删除命令通过顶部工具栏维护当前项及集合顺序。三类条件命令位于时间轴上方的紧凑分组，分别打开仅包含 Condition 标签任务的选择器，并把确认的实例加入对应条件集合。

`TimelineOverviewControl` 在固定左列逐行绘制 Action 显示名和类型名，在右侧根据 StartTime/Duration 绘制灰阶轨道、刻度、有限或无限持续区间，并显示悬停、选中和右端拖拽手柄状态。点击左列或时间条选择时间项；拖动条身更新开始时间，拖动右端更新有限持续时间，并通过属性通知刷新 UI。原下方时间项卡片不再存在。MainViewModel 把当前 TimelineItem 传给 Inspector，Inspector 在普通任务字段之前用双向绑定编辑 StartTime 和 Duration；选择该项的 Condition 时仍保留所属 Action 的时间字段。导出时先创建 Timeline 根任务，再按稳定顺序为 Action 和条件分配唯一 ID 并写入引用。

## 4. 数据来源

- `TimelineDocument.Items` 及三组条件集合。
- TaskCatalog 中的 Action、Condition 和 Timeline 任务定义。
- Inspector 写回的任务字段、StartTime 和 Duration。
- 旧 Timeline `.editor.json` 和对应运行时输出。

## 5. 与其他模块的依赖

本模块依赖应用工作区、节点类型选择器、Inspector 和文件格式模块；依赖 Core 的运行时导出器生成旧协议。它不依赖行为树节点、端口或树校验。
