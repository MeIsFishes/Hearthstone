# 行为树编辑器设计

## 模块说明

行为树编辑器使用 WPF 自绘画布编辑 `BehaviorNode` 和 `BehaviorEdge`。节点保存任务实例与二维位置；连线保存源节点、源端口、目标节点和同端口下的顺序。运行时以唯一的 `TaskBtRoot` 节点为根，但不要求根节点排在任务列表第一项。

编辑器保持严格树语义：唯一根、根无父节点、每个非根节点至多一个父节点、禁止自连和环、Single 端口最多一个目标，并区分 Condition 端口与普通 ConnectPoint。

## UI 交互

- 点击添加节点后弹出类型选择窗口，排除 Timeline 类型并避免重复创建 Root；节点可直接拖动，画布支持平移和缩放。
- 从节点端口拖线到现有兼容节点可建立连接；拖到空白处会按端口类别弹出选择窗口，确认兼容类型后在释放位置创建并连接节点。
- `Esc` 或失去捕获时取消拖线，不产生半完成边。
- 用户可以断开目标节点的入边，并调整同一源端口下子节点的先后顺序。
- 选中节点后由共享 Inspector 编辑非 ConnectPoint 字段。

## 数据来源

- `BehaviorTreeDocument.Nodes`、`Edges` 和节点位置。
- `TaskCatalog` 中的 Drive、Action、Condition 标签与 ConnectPoint Single/Multiple 字段。
- 旧 NodeGraph `.editor.json` 中的节点、位置、端口索引和有序连接。
- `DocumentValidator` 返回的根、父子、端口、环和可达性诊断。

## 设计约束

- TaskId 只要求在导出结果中唯一；`RootTaskId` 指向根节点实际分配到的 ID。
- 连接创建和最终保存都必须执行相同的树语义校验，不能只依赖 UI 阻止非法操作。
- Condition 节点不能作为连接源；三类 Condition 端口只能连接 Condition。
- Timeline 与行为树分别维护，行为树不复用 Timeline 的顺序或时间模型。
