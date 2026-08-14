---
name: initialize-empty-project
description: 判定空项目并搭建目录、基础文件及基础架构。用于新建或近空项目初始化。
---

# 空项目初始化

## 目标

先判断项目是否属于空项目或只有占位结构的近空项目，再建立一套能直接编译和接入 Unity Editor 的 BbxCommon 基础结构。初始化必须保留已有内容、使用当前框架验证过的依赖版本，并把 Unity 或 BbxCommon 工具会自动生成的文件留给对应工具。

刚创建了目录、asmdef、GameEngine 或 Stage 占位类，但 ECS、MVC、配置和启动流程尚未接通的项目仍属于本 skill 范围。已有真实业务流程的项目不属于空项目，不得用本流程整体重搭。

## 强制原则

1. 先扫描、后分类、再修改；不要只凭文件数量判断。
2. 把 `Assets/Scripts/BbxCommon/`、第三方包、Unity 示例与业务代码分开统计。
3. 将已有文件视为用户资产。增量补齐，不覆盖、不重命名、不移动已有结构，除非用户明确要求。
4. 发现 BbxCommon 缺失时，不自行仿写框架；先确认框架来源或要求用户导入。
5. 用户没有说明模块时，默认创建一套纯占位基础文件：业务 asmdef、GameEngine、BaseStage、配置类型、ECS Singleton Component、ECS System、UiScene、View 和 Controller。用户明确说明不需要某模块时才跳过该模块。
6. 占位文件必须能说明创建、运行和释放关系，并统一使用 `Placeholder` 命名与中文注释。它们用于证明基础结构已接通，不得伪装成真实业务类型；首个真实模块建立后逐步替换。
7. 以“入口 → Stage → Component → System”作为最小可运行流程；默认同时准备 MVC/UI 占位代码，但 UI 资产仍需在 Unity Editor 中创建。
8. 不创建 Unity、Package Manager、IDE 或 BbxCommon 编辑器工具会自动生成的文件。不得手写 `.meta`、Scene/Prefab YAML、`packages-lock.json` 和框架资源索引。
9. UI 的权威运行时 Model 默认使用 ECS Component；不要为了凑齐 MVC 新建 `UiModelBase`。
10. 首次初始化必须至少建立一个命名明确的 GameStage Group 入口。该入口负责创建初始 Stage 集合并调用一次 `SetActiveGameStage(...)`；不得只留下零散的 `LoadStage` 或未被调用的 Stage 工厂。
11. 在创建或补齐项目文件前，通读所有 subagent TOML，解除其中结构无效的项目级 skill 关联；不得凭主观相关性移除仍可解析的合法关联。

## 必读与按需文档

开始时必须读取：

- [空项目判定](references/project-state-classification.md)：状态分类和停止条件。
- [目录与基础文件](references/foundation-layout.md)：默认目录和文件清单。
- [基础占位模板](references/basic-placeholder.md)：模板文件、创建命令和替换规则。
- [Unity 与依赖基线](references/unity-and-dependencies.md)：明确依赖包、版本冲突规则和自动补包命令。
- [自动生成文件边界](references/generated-files.md)：哪些文件不能由初始化流程创建。
- [增量初始化](references/incremental-initialization.md)：已有占位文件时如何补齐且不覆盖。

按项目条件读取：

| 条件 | 读取文档 |
|---|---|
| 存在 BbxCommon，准备建立入口、Stage、Group 或配置加载 | [GameEngine、Stage 与数据](references/game-engine-stage-data.md)，并读取 [game-stage](../game-stage/SKILL.md) |
| ECS 尚未建立或需要补齐最小可运行流程 | [ECS 基础体系](references/ecs-foundation.md)，并读取 [bbxcommon-ecs](../bbxcommon-ecs/SKILL.md) |
| 创建默认 MVC/UI 占位文件或真实 UI | [MVC/UI 基础体系](references/ui-mvc-foundation.md)，并读取 [bbxcommon-ui](../bbxcommon-ui/SKILL.md) |
| 准备创建启动主场景、内容场景或配置 Build Settings | [主场景搭建](references/main-scene-setup.md) |
| 准备验收或交付 | [验证与交付](references/validation-and-handoff.md) |

需要真实配置数据时额外读取 [config-data-design](../config-data-design/SKILL.md)。创建任何 GameStage Group 入口时都必须读取 [game-stage](../game-stage/SKILL.md)，不以 Stage 数量是否超过一个为条件。专项 skill 的 API 与生命周期说明优先于本 skill 的概览。

## 核心流程

### 1. 收集证据并分类

运行只读扫描，再人工阅读关键文件：

```powershell
powershell -ExecutionPolicy Bypass -File .codex/private-skills/initialize-empty-project/scripts/inspect-unity-project.ps1 -ProjectRoot .
```

依据 [空项目判定](references/project-state-classification.md) 分类：

- 非 Unity：停止，本 skill 不适用。
- 完全空白、只有框架的空项目、只有占位结构的近空项目：继续。
- 已形成项目：停止整体初始化，只报告缺失基础层并改用对应专项流程。
- 不确定：继续只读检查；若项目名、namespace、BbxCommon 来源或已有入口无法确定，先询问用户。

### 2. 清理无效的项目级 Skill 关联

在创建或补齐项目文件前，完成一次全量 subagent 关联审计：

1. 只把实际存在的 `.codex/agents/` 与 `.codex/project-files/agents/` 放入搜索范围，再使用 `rg --files <existing-agent-roots> -g '*.toml'` 枚举底层和项目 subagent；目录不存在时按空目录处理。
2. 通读枚举出的每一份 TOML，不得只搜索命中的路径片段。逐项检查所有 `[[skills.config]]` 的 `path` 与 `enabled`。
3. 把以下两类关系视为项目级 skill 关联：
   - `path` 直接指向 `.codex/project-files/skills/` 下的项目 skill；
   - `path` 指向 `.codex/project-files/agents/*-agent-extension.md`，且 extension 再关联项目 skill。
4. 只有符合下列任一条件时才判定关联无效：
   - `skills.config.path` 指向的文件不存在；
   - `.codex/agents/` 下的底层 subagent 直接指向 `.codex/project-files/skills/`，绕过项目 extension；
   - 项目 extension 不存在、没有明确引用 `.codex/project-files/skills/<skill-name>/SKILL.md` 或相对等价路径，或引用的项目 skill 文件不存在。解析相对路径时以 extension 所在目录为基准。
5. 对每个无效关系，只删除对应的完整 `[[skills.config]]` 表项；保留 TOML 中其他字段、底层 skill、有效项目 skill、有效 extension 和禁用但路径有效的配置。不得连带删除 skill 或 extension 文件。
6. 记录读取过的全部 TOML、移除的关联路径和逐项原因。TOML 语法损坏或表项边界无法安全确认时，不重写整个文件；报告具体文件并先解决语法问题。

项目 subagent 可以直接关联现存项目 skill；底层 subagent 必须通过同名 `agent-extension.md` 间接关联。不要因为某项关联与本次占位模块无关、当前没有触发，或暂时处于 `enabled = false` 就把它判为无效。

### 3. 固定项目身份与默认范围

确认项目名、C# 根 namespace、业务代码根目录和 BbxCommon 是否存在。优先读取 `AutoDoc/ProjectOverview.md`、现有 asmdef、脚本 namespace 和用户说明；不要从带连字符的仓库目录名直接生成 C# 类型名。

如果用户没有列出模块，采用默认占位范围，不再追问 ECS/UI/配置是否需要。如果用户明确说项目无 UI、无配置或不使用 asmdef，记录该决定并跳过对应模板。

### 4. 自动补齐明确依赖的 Unity 包

先 dry-run：

```powershell
python .codex/private-skills/initialize-empty-project/scripts/ensure-unity-packages.py --project-root . --dry-run
```

确认没有版本冲突后执行：

```powershell
python .codex/private-skills/initialize-empty-project/scripts/ensure-unity-packages.py --project-root .
```

脚本只向 `Packages/manifest.json` 添加缺失的 BbxCommon 基线包，不覆盖已有不同版本。出现版本冲突时停止并报告；不得直接改 `packages-lock.json`，它由 Unity Package Manager 自动更新。随后在 Unity 中等待包解析完成，再继续创建引用框架的业务脚本。

### 5. 检查 BbxCommon 与外部资产

确认 BbxCommon asmdef、GameEngine、GameStage、EcsApi 和 UI MVC 类型存在且可编译。确认 Odin Inspector 已作为外部资产导入；它不是本工程通过 manifest 安装的 UPM 包。UniTask 与 CrossLibrary 随当前 BbxCommon 源码提供，不另装包。

BbxCommon 缺失、Odin 缺失或框架编译失败时停止模板实例化，先解决框架来源与版本。

### 6. 创建默认占位基础文件

对于完全空白或只有框架的空项目，先 dry-run，再使用模板脚本：

```powershell
python .codex/private-skills/initialize-empty-project/scripts/instantiate-basic-placeholder.py --project-root . --project-name ProjectName --namespace ProjectNamespace --dry-run
python .codex/private-skills/initialize-empty-project/scripts/instantiate-basic-placeholder.py --project-root . --project-name ProjectName --namespace ProjectNamespace
```

脚本只创建 [基础占位模板](references/basic-placeholder.md) 列出的文本文件。目标文件已存在且内容不同会让整个操作停止，不会覆盖。对于近空项目，不要强行运行整套模板；先盘点已有 GameEngine、Stage、asmdef 等文件，再复制或改写缺失部分。

### 7. 检查最小可运行流程

按 [GameEngine、Stage 与数据](references/game-engine-stage-data.md) 和 [ECS 基础体系](references/ecs-foundation.md) 核对：

1. 逐个核对业务源码直接使用的 namespace、基类、接口和公开成员签名所属程序集，补齐业务 asmdef 的直接引用；不得假设 BbxCommon 的程序集引用会自动传递给业务程序集。默认模板至少应引用 `BbxCommon`、`CrossLibrary`、`Unity.Entities`、`Unity.Collections` 和 `Unity.TextMeshPro`；
2. GameEngine 加载 BaseStage；
3. GameEngine 至少提供并实际调用一个 `EnterInitialStageGroup()`，在其中创建初始 Stage 集合并调用一次 `SetActiveGameStage(...)`；
4. Stage 的 `IStageLoad` 创建占位 Singleton Component；
5. GameEngine 通过 `RegisterSystemOrder(typeof(...), ...)` 登记 Input、占位 System 与 Task 的调用顺序；
6. Stage 注册带 `[DisableAutoCreation]` 的占位 System；
7. System 把占位状态从未初始化改为已初始化；
8. Stage 卸载时移除 Singleton 并清理监听；
9. 让目标版本 Unity 实际刷新并编译业务 asmdef。出现类型或 namespace 不可见时，先修正 asmdef 直接依赖，再继续扩建或交付。

默认模板中的配置类型、UiScene、View 和 Controller 可以先没有对应 `.asset`/Prefab；代码必须编译，Editor 资产在下一步完成。

初始 Group 没有外部必需输入时，不创建空 StartupData。后续 Group 一旦缺少关卡、角色或队伍等输入便无法运行，必须在切换请求边界构造强类型 `XxxStageStartupData`，先交给 Stage 工厂保存快照，再由 DataGroup 就绪后的专用 `IStageLoad` 消费一次并转成 ECS Component；System 与 UI 不得逐帧读取 StartupData。详细约束以 [game-stage](../game-stage/SKILL.md) 为准。

### 8. 完成 Unity Editor 资产接入

严格按 [自动生成文件边界](references/generated-files.md) 和 [主场景搭建](references/main-scene-setup.md) 区分：

- Unity/Package Manager/IDE 会自动生成：等待工具生成，不创建；
- BbxCommon 工具会生成：要求用户运行对应菜单、按钮或保存流程；
- Unity 资产：可操作目标版本 Unity 时，优先通过 Editor API 创建 Scene、Prefab、ScriptableObject 并配置 Build Settings；不得手写 Unity YAML 或 `.meta`；
- 无法操作 Unity Editor 时：给出 Scene、GameObject、Canvas Prefab、UI Prefab、ScriptableObject 和 Build Settings 的具体步骤，并明确标记为待办。

默认占位 UI 的资源路径为 `Assets/Resources/Ui/Placeholder.asset`，Resources key 为 `Ui/Placeholder`。未完成 Canvas、Prefab 和 UiSceneAsset 前，不宣称 UI 已可运行。

若当前 BbxCommon 已包含 `GameStageWindow` / `GameStageEntryAsset`，首次初始化还应为初始 Group 提供至少一个 Editor Group 入口配置：入口脚本把可编辑字段转换为运行时 StartupData，并调用同一个 `EnterInitialStageGroup`。能操作 Unity Editor 时通过 Editor API 创建资产到 `Assets/Resources/Editor/`；不能操作时，在交付中列出入口脚本、资产类型、目标路径和创建步骤，不得手写 `.asset` YAML 或 `.meta`。初始 Group 没有外部必需输入时不创建空 StartupData，Editor 入口直接调用无输入的运行时 Group 入口。

### 9. 验证、记录占位项并交付

按 [验证与交付](references/validation-and-handoff.md) 检查文本结构、manifest、程序集、编译、启动、Stage、ECS、UI 和卸载。最终报告必须列出：

- 已创建的占位文件；
- 已读取的 subagent TOML，以及移除的无效项目级 skill 关联和原因；
- 已自动添加的包和未解决的版本冲突；
- Unity/BbxCommon 将自动生成的文件；
- 用户仍需在 Editor 完成的资产；
- 每个 `Placeholder*` 文件未来应由什么真实模块替换。

## 禁止项

- 不因为缺少某个可选体系就把已有玩法项目判为空项目。
- 不复制另一个项目的业务 namespace、GUID、Scene、Prefab、`.meta` 或配置资产。
- 不覆盖已有 manifest 版本，不直接编辑 `packages-lock.json`。
- 不同时保留 Assembly-CSharp 与新 asmdef 下的重复业务类型。
- 不创建独立 `Update()` 驱动器绕过 GameStage/ECS System。
- 不在 Controller 中保存玩法权威状态，不让 View 直接执行业务规则。
- 不创建默认模板之外、没有用途的 Manager/Service/Repository 占位层。
- 不把 Unity Editor 尚未完成的资产绑定写成已完成。

## 完成定义

满足以下条件后，才把“代码和配置初始化”标记完成：

- 项目状态、项目名、namespace 和默认占位范围已有记录；
- 所有 subagent TOML 已通读，结构无效的项目级 skill 关联已解除并记录，合法底层与项目关联仍保留；
- manifest 已包含明确的 BbxCommon 基线包，且没有未解决的版本冲突；
- BbxCommon、UniTask、CrossLibrary 与 Odin 的存在性已核实；
- 业务 asmdef 已按实际源码和公开类型链补齐直接程序集引用，不依赖传递引用假设；
- 默认占位文件已创建，或用户明确排除了对应模块；
- GameEngine → Stage → Component → System 的占位流程完整；
- 至少一个初始 GameStage Group 入口已由 GameEngine 实际调用，并通过 `SetActiveGameStage` 声明完整活跃集合；可用 Stage 入口框架时，对应 Editor Group 入口资产已创建，或无法操作 Editor 的具体待办已明确记录；
- 目标 Unity 已实际刷新并通过业务程序集编译，或已明确记录无法执行 Unity 编译的原因；
- Unity 自动文件、BbxCommon 生成文件和 Editor 手工资产已分别列出；
- 每个占位类型的后续替换方向明确。
