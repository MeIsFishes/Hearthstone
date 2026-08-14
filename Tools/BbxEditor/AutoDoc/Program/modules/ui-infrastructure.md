# WPF UI 基础设施

## 1. 模块说明

本模块提供 `.NET/WPF` 应用所需的窗口、命令、数据绑定、对话框、统一主题和自绘控件基础。主要实现包括 `ObservableObject`、`RelayCommand`、`MainWindow`、`DialogService`、`GrayTheme.xaml`、`TimelineOverviewControl` 与 `BehaviorTreeCanvas`。业务状态保存在 Core 领域对象和 ViewModel 中，不保存在 WPF 可视树节点中。

应用采用低饱和深灰主题。语义化画刷、圆角尺寸和 Button、TextBox、ComboBox、ListBox、TabControl、Menu、ScrollBar 等通用控件模板集中在 `Themes/GrayTheme.xaml`；页面仅组合布局和引用语义资源，避免分散维护颜色与交互状态。所有用户可见静态文字和运行时生成的状态、确认、错误与校验诊断统一使用英文，不提供中英混排界面。

## 2. 对外接口

- `ObservableObject.SetProperty`、`RaisePropertyChanged`：领域对象和 ViewModel 属性通知。
- `RelayCommand`：无参数或带参数的 WPF 命令适配。
- `IDialogService`：文件、带建议名称的新建路径、目录、确认和消息对话框。
- `TimelineOverviewControl`：Timeline 区间绘制和拖动。
- `BehaviorTreeCanvas`：行为树节点、端口、连线、平移和缩放交互。
- `InspectorControl.FieldChanged`：字段编辑变化通知。
- `MainViewModel.CurrentDocument`、`SelectedExplorerFile`：中央活动文档与 Explorer 文件高亮的双向入口；内部页签同步不触发文件预览。
- `App.xaml` 合并的 `Themes/GrayTheme.xaml`：全局颜色、字体、圆角、控件模板及悬停、按下、选中、禁用状态入口。

## 3. 调用链路

应用启动时 `App.xaml` 先合并 GrayTheme 资源字典，隐式样式覆盖标准 WPF 控件，显式样式用于主操作、扁平操作、图标操作和危险操作。WPF 数据绑定再把 MainViewModel 和 DocumentViewModel 的属性/命令连接到 MainWindow 与模块视图。领域集合或属性变化通过 `INotifyCollectionChanged`、`INotifyPropertyChanged` 触发列表刷新和自绘控件 `InvalidateVisual`。用户操作由命令或控件事件进入 ViewModel，再修改 Core 对象。

系统文件对话框由 `DialogService` 包装，业务 ViewModel 只依赖 `IDialogService`。文档标签头由横向 `WrapPanel` 承载，页签总宽度超过中央工作区时自动换到下一行，标签区高度随行数增长，不使用水平滚动条。中央活动页签变化后，`MainViewModel` 按规范化绝对路径在当前 Explorer 结果中重新选择对应文件；选择由同步保护标记写入，不会再次触发预览，也不会改变 Explorer 当前滚动位置。搜索或筛选暂时隐藏当前文件时不显示错误高亮，结果恢复后会重新匹配。Timeline 与 Behavior Tree 的新建入口统一收纳在 Explorer Files 页签左上角带高对比轮廓的深色 `Create` 菜单中；该菜单占用原筛选摘要位置并与刷新、筛选按钮共用一行，不再显示 `All Mods, All Types` 等摘要。菜单和右侧两个图标按钮统一为 30px 高并沿中轴垂直居中，子项直接绑定工作区既有命令。中央标签栏不再承担新建入口。`SettingsWindow` 承载原主窗口元数据栏的配置控件。Timeline 和行为树的高频绘制交互由自定义 `FrameworkElement` 完成，Inspector 和工作区布局使用 XAML 控件与数据模板。

用户可见文字可能来自 WPF XAML、动态控件 C#、ViewModel 状态消息、对话服务和 Core 诊断。新增或修改这些入口时必须使用英文；发布前通过扫描 `src/` 中的中文字符检查静态界面与运行时提示，外部 Unity 元数据中的业务名称和说明不由 UI 基础设施翻译。

## 4. 数据来源

- MainViewModel、DocumentViewModel、当前活动文档路径和 Explorer 文件索引。
- WPF 输入事件、依赖属性、系统文件/目录对话框。
- TimelineItems、BehaviorNodes、BehaviorEdges 等可观察集合。
- `GrayTheme.xaml` 的语义画刷、控件模板，以及各视图 XAML 中的布局和数据绑定。

## 5. 与其他模块的依赖

本模块依赖 Windows Presentation Foundation 和 Core 的可观察模型；工作区、Timeline、行为树和 Inspector 均依赖它。Core 不反向依赖 WPF。
