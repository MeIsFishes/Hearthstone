# BbxEditor

BbxEditor 是面向 BbxCommon 数据体系的 `.NET 10 + WPF` 多文档桌面编辑器。它不加载游戏程序集，而是读取 Unity 游戏端导出的元数据，为 Task、CSV 和 `BbxScriptableObject` 提供对应的编辑界面。

当前功能包括：

- 根据当前文档页签自动显示 Timeline、行为树、CSV 或 BbxScriptableObject 编辑器。
- 读取 Unity 导出的 Task、TaskContext、Enum、CSV 和 BbxScriptableObject 元数据。
- 兼容历史 Timeline/NodeGraph `.editor.json` 与游戏 runtime JSON，不改变旧 Task 协议。
- CSV RFC 4180 引号解析、表头契约注释与未知列保留、编码/换行风格保留、类型与唯一性校验。
- 基于 MonoScript GUID 识别 BbxScriptableObject，只修改元数据声明的 Unity YAML 业务字段并保留 Unity 系统字段。
- CSV 和 BbxScriptableObject 编辑器使用高对比暗色表头、正文、输入框和选中状态。
- 多文档工作区、最近文件、Task Inspector 和按文档类型变化的辅助信息区。
- 加宽的左侧 Explorer、中央多文档编辑区和右侧 Inspector；Explorer 支持字面与向量搜索，在后台索引可编辑文件并通过 FileSystemWatcher 持续刷新。

Unity 游戏工程通过 `BbxCommon/ExportMetaData` 导出 BbxEditor 使用的统一目录：

```text
ExportedBbxEditorInfo/
├─ Task/                  # 现有 TaskExportInfo 等 JSON
├─ Csv/                   # 每个 CsvDataBase 类型一个 JSON
├─ ScriptableObject/      # 每个 BbxScriptableObject 类型一个 JSON
└─ Assets/asset-index.json
```

未提供 ScriptableObject 元数据时，`.asset` 不会以可编辑模式打开；普通 CSV 仍可使用无类型表格模式打开。

## 构建与运行

```powershell
dotnet build BbxEditor.Net.sln
dotnet run --project src/BbxEditor.Wpf/BbxEditor.Wpf.csproj
dotnet publish src/BbxEditor.Wpf/BbxEditor.Wpf.csproj -c Release
```

## SmokeTests

```powershell
dotnet run --project tests/BbxEditor.SmokeTests/BbxEditor.SmokeTests.csproj
```

SmokeTests 当前验证 Task 文件往返、CSV 解析/保存/校验、TaskBlackboardInjection 结构化读写、元数据目录读取、真实游戏 CSV 绑定、受限 Unity YAML 编辑和设置持久化。

Release 发布会在工程根目录生成单文件、自包含的 `BbxEditor.exe`。应用设置保存到该可执行文件同目录的 `settings.json`；从源码运行时同样定位到 BbxEditor 工程根目录。

`settings.json` 的 `gameProjectPath` 指向 Unity 工程，`explorerDirectories` 配置需要索引的工程内相对目录，默认是 `Assets/Resources` 和 `Mods`。Explorer 将 `Assets/Resources/**` 与 `Mods/Native/**` 都归入官方 `Native` 模组，将 `Mods/<ModName>/**` 归入对应模组；右上角 `⋯` 可按模组过滤。

向量搜索开关保存在编辑器目录的 `settings.json`。共享模型目录保存在 `%LocalAppData%/BbxCommon/settings.json`，其 JSON 当前只有 `modelDirectory` 字段，其他 Bbx 工具可以复用该目录和同一模型。Settings 中的开关与目录只有点击 `Apply` 后才生效；启用且找到 `paraphrase-multilingual-mpnet-base-v2-quint8-avx2` 模型后，编辑器会用独立的 BbxEditor 子进程执行 ONNX embedding，不需要 Python。父进程退出或 IPC 断开时子进程随即退出。

文件名向量以键值对写入编辑器目录的 `vector-index.json`。启动与 FileSystemWatcher 刷新时会按去除扩展名、`.editor.json`、`Task` 前缀以及 `CsvData`、`Data` 等后缀后的名称做增量同步：已有名称跳过、新名称入队、已经不存在的名称删除。本轮索引未完成时只进行字面搜索；完成后先列出完整字面匹配，再追加去重后的重中心化向量排序结果。

游戏工程和元数据目录允许使用相对路径，并始终以 BbxEditor 程序目录为基准解析，不依赖进程的当前工作目录。当前仓库配置使用 `../../PressAnyKey` 与 `../../ExportedBbxEditorInfo`。
