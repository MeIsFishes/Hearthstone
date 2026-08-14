# Main Agent Rules

## 项目定位与相对路径

当前目录是游戏仓库中的通用任务编辑器项目，不是空项目模板，可供不同 Unity 游戏项目使用。

- 仓库根目录：`../..`
- Unity 游戏项目：`../../<游戏目录>/`，其中 `<游戏目录>` 表示当前接入的具体游戏项目目录
- Unity 导出的任务元数据：`../../ExportedTaskInfo`
- .NET 解决方案：`BbxEditor.Net.sln`
- Core、WPF、测试与发布目录：`src/`、`tests/`、`artifacts/`

当前项目是 Windows 专用的 .NET 10/WPF 编辑器，只读写旧版 `.editor.json` 和旧版游戏 runtime JSON，不引入 v2 协议。Timeline 与行为树是分离的编辑器，只共享工作区、任务目录、Inspector、元数据和旧协议转换层。行为树保持严格树语义；首版不提供撤销/重做。

CrossLibrary 的唯一源码位于 `../../<游戏目录>/Assets/Scripts/BbxCommon/CrossLibrary/`，其中 `<游戏目录>` 表示当前接入的具体游戏项目目录。BbxEditor Core 不直接引用该程序集；SmokeTests 直接链接其中的 `Api/JsonApi.cs` 和 LitJson 源码验证 Dictionary 协议。Unity 运行时 Task 读取逻辑位于 `../../<游戏目录>/Assets/Scripts/BbxCommon/GameFramework/Task/`。修改旧协议时必须同时验证 BbxEditor codec、Unity CrossLibrary 和 `TaskBase` 的行为。

主代理需要关注两类项目内规则：

1. TempPlan 步骤记录规则：`.codex/skills/temp-plan-recording/SKILL.md`
2. 程序文档写作规则：`.codex/skills/program-doc-format/SKILL.md`

如果你是子代理，只遵循你的 `.toml` 指令以及主代理直接提供给你的任务上下文。

## 使用规则

1. 当用户要求分步执行、分阶段确认，或明确要求记录步骤时，先读取并遵循 `temp-plan-recording`。
2. 当用户要求编写、更新或整理 `AutoDoc/Program/` 下的程序文档时，先读取并遵循 `program-doc-format`。
3. 需要了解项目时，可以先阅读 `AutoDoc/` 下的文档。
4. 进行了较大代码、结构或功能改动后，应同步检查并更新相关文档。
5. 较大修改后更新 `AutoDoc/`，并验证根目录 `BbxEditor.Net.sln` 的 Debug/Release 构建与 SmokeTests。
6. BbxEditor 的整个用户界面统一使用英文，具体要求见下方“界面语言”规则。
7. 除上述规则外，本项目不预设额外 skill、agent、项目流程或子代理注册表。

## 界面语言

1. 所有用户可见文字必须使用英文，不得出现中文或中英混排界面。
2. 本规则覆盖菜单、按钮、窗口标题、标签、工具提示、占位与空状态、状态栏、确认框、错误消息、校验诊断、文件/目录对话框标题，以及运行时动态生成的提示。
3. 检查范围包括 WPF XAML、动态控件 C#、ViewModel、`DialogService` 和 Core 中可能展示给用户的诊断与异常文本。

# TempPlan Step Recording

当用户或某个 skill 明确要求分步执行、分阶段执行，或者希望在步骤之间确认时，请遵循 `.codex/skills/temp-plan-recording/SKILL.md`。

核心规则：

1. 在开始实质性工作之前，创建一个新的 TempPlan 文件，路径为项目根目录下的 `AutoDoc/Temp/`。
2. 将计划步骤以检查清单的形式写入 TempPlan，步骤描述尽量完整。
3. 每完成一步，都要更新 TempPlan，将该步骤标记为完成，并补充简短总结。
4. 每完成一步并更新 TempPlan 后，立即执行项目根目录下的 `AutoDoc/CleanupTempDocs.bat`。
5. 如果下一步存在歧义、缺少必要信息，或需要用户做决定，就在执行该步骤前停下来询问用户。
6. 当进行到某个步骤时，如果有 skill 等来源要求插入步骤，则在 TempPlan 中的当前步骤后插入对应步骤。

# Program Documentation

编写或更新程序文档时，请读取并遵循 `.codex/skills/program-doc-format/SKILL.md`。

程序文档默认写入 `AutoDoc/Program/`。不再按文档类型细分子目录；如需为单篇文档创建子目录，推荐路径为 `AutoDoc/Program/<doc-name>/<doc-name>.md`。

项目总文档固定路径为 `AutoDoc/Program/project-overview.md`。

文档路径与 `.md` 文件名使用英文；标题和正文可以使用用户的语言。
