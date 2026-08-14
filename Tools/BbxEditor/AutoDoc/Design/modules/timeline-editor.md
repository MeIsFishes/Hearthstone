# Timeline 编辑器设计

## 模块说明

Timeline 编辑器面向按时间触发的 Action 任务。文档由有序的 `TimelineItem` 组成，每项持有任务、开始时间、持续时间，以及 Enter、Normal、Exit 三组条件。Timeline 与行为树拥有独立文档模型和视图，只共享任务元数据、Inspector 与持久化服务。

负持续时间表示无限持续。运行时导出时创建 Timeline 根任务，再按文档顺序为 Action 和条件分配唯一 TaskId；引用关系由导出器统一生成。

## UI 交互

- 点击添加 Action 后弹出节点类型选择窗口，只显示带 Action 标签的任务；确认后添加时间项，并支持上移、下移和删除。
- Action 节点按行固定显示在时间轴左侧，显示名和类型名与右侧时间条一一对齐；不再在时间轴下方重复显示节点卡片。
- 选择时间项后，Inspector 最上方先显示 StartTime 和 Duration，再显示任务字段；选择该 Action 的 Condition 时保留所属时间项的时间信息。
- 时间概览条支持拖动主体修改开始时间，拖动右端修改持续时间；所有结果限制为非负开始时间，精确值仍可在字段中输入。
- 三类 Condition 管理区位于时间轴上方；每个分组支持添加、选择和删除，添加时弹出只包含 Condition 类型的选择窗口。
- 时间项或条件的选择统一驱动 Inspector，不在 Timeline 内复制字段编辑控件。

## 数据来源

- `TimelineDocument.Items` 及每个条目的三组条件集合。
- `TaskCatalog` 中的 Action、Condition 与 Timeline 标签和字段定义。
- Inspector 写回的 `TaskInstance.Fields`、`StartTime` 和 `Duration`。
- 旧 `.editor.json` 中的时间项、条件和界面状态。

## 设计约束

- 列表顺序是稳定的编辑顺序，不由时间值自动重排。
- 负 Duration 的旧协议语义必须保留，UI 拖动只产生有限非负值。
- 条件不作为独立 Timeline 项显示，但导出时拥有独立且不重复的 TaskId。
- Timeline 不依赖行为树端口、节点位置或树验证规则。
- 左侧节点列宽固定，时间换算只使用右侧轨道宽度；两侧行高和集合顺序必须一致。
