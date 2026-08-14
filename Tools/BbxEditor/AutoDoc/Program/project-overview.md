# BbxEditor 项目总览

BbxEditor 是面向 BbxCommon 数据体系的 `.NET 10 + WPF` 多文档桌面编辑器。它从 Unity 游戏端导出的元数据建立 Task、CSV 和 BbxScriptableObject 类型目录，在不加载游戏程序集的情况下选择对应编辑界面并安全写回数据文件。

项目包含以下系统：

- 应用外壳与多文档工作区：新建、切换、打开、保存和关闭 Task/CSV/Asset 文件；当前页签决定编辑器和辅助 Inspector。
- 策划案查看器：从当前 Unity 主端项目的 `AutoDoc/DesignPlan/YYYY.MM.DD/` 浏览只读 Markdown 策划案，并显示文档链接的本地或远程图片。
- 任务元数据与 Inspector：读取 Task、Context、Enum 定义，编辑常量、Context、Blackboard、List 和 Dictionary。
- Timeline 编辑器：编辑 Action 的开始时间、持续时间以及进入、持续、退出条件。
- 行为树编辑器：以 WPF 节点画布编辑严格树结构、端口连接与子节点顺序。
- CSV 编辑器：保留表头、未知列、UTF-8 BOM 和换行风格，并按导出元数据验证字段类型、必填与唯一值。
- BbxScriptableObject 编辑器：按 MonoScript GUID 匹配导出元数据，只编辑受支持的 Unity YAML 业务字段并保留系统字段。
- 兼容与持久化：只读写既有 `.editor.json` 和旧运行时 JSON，保存时成对写入并执行验证。

Timeline 与行为树是两套独立编辑器，只共享工作区、节点类型选择器、Inspector、元数据和文件服务。源码、测试与发布产物分别位于一级目录的 `src/`、`tests/` 和 `artifacts/`。
