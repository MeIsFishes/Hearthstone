# 融合页右侧两列布局检查清单

- [x] 通过——右侧第一列依次为当前点数底框、剩余点数底框、智能推荐按钮；第二列第一排为更大的融合按钮。Unity 实例检查坐标为第一列 X=-128、Y=82/0/-82，第二列融合按钮 X=122、Y=82；第一列宽 216，融合按钮尺寸 238×82。
- [x] 通过——当前点数正好 99 时保持闪光，超过 99 时切换为红色，低于 99 时恢复常态；剩余点数不参与闪光或超额变色。
- [x] 通过——已修改 `PreparationViewUiBuilder`，并通过 Unity Editor 执行 Builder 重新生成 `Assets/Resources/Ui/PreparationView.prefab`，未手写 Prefab YAML。
- [x] 通过——Unity 实例检查确认两个点数底框共用 `PreparationFusionSumPanel`，View 引用完整；资源导出回归测试同时覆盖按钮 SpriteState 与引用。
- [x] 通过——已更新 Editor 回归测试，覆盖两列的坐标、尺寸、顺序、底框资源以及当前点数颜色状态；两个相关测试均通过。
- [x] 通过——静态布局仍归 Builder，运行时状态仍归 Controller；未修改 `Preparation.asset` 或 `Assets/Scenes/Ui/Preparation.unity`。
- [x] 通过——未新增一次性字段/helper，修改范围限定于融合页相关 Builder、Controller、Prefab、测试和对应文档。
- [x] 通过——已同步玩家视角设计文档中的两列布局、99 点闪光和超过 99 点变红反馈。
- [x] 通过——已同步 UI 美术总览与备战卡池模块文档中的独立底框、位置尺寸及按钮主次关系。
- [x] 通过——已同步备战界面程序文档中的 Builder 布局参数和 Controller 状态逻辑。
- [x] 通过——Unity 脚本编译完成，最终 Console 错误为 0；Prefab 结构与引用检查通过；两个 EditMode 测试各 1/1 通过；代码与文档 `git diff --check` 通过。Prefab 的空字段尾随空格来自 Unity 序列化器，遵循 Builder 管理原则未手工改写。
- [x] 通过——结束审计后已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 0；随后创建同名 Report。
