# BbxEditor 设计总览

BbxEditor 是 Chaos Combat 任务系统的 Windows 桌面编辑器，当前产品实现统一定位为 `.NET 10 + WPF`。编辑器从 Unity 导出的类信息建立任务元数据目录，并分别提供 Timeline 与行为树两套编辑体验；两者共享多文档工作区、节点类型选择、Context、Inspector、验证和旧协议文件服务。

项目主要包含以下系统：

- 应用工作区：管理元数据目录、多文档、当前 Context、节点类型选择和文件命令。
- Timeline 编辑器：编辑 Action 的时间区间以及进入、持续、退出条件。
- 行为树编辑器：编辑严格树结构、任务节点、端口连接和子节点顺序。
- 元数据与 Inspector：依据导出类型编辑常量、Context、Blackboard、List 和 Dictionary。
- 统一视觉系统：使用低饱和深灰色板、圆角控件和一致的悬停、焦点、选中与拖拽反馈。
- 兼容与持久化：只读写既有 `.editor.json` 和游戏运行时 JSON，并保持 CrossLibrary 协议一致。

项目一级目录直接承载 solution、源码、测试、文档和发布目录。编辑器仍兼容历史文件中的旧类型标签，但不保留或依赖旧 UI 工程。
