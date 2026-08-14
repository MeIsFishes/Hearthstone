# 应用工作区设计

## 模块说明

应用工作区是 WPF 主窗口的协调层，负责元数据路径、节点类型选择、Context、多文档标签、文件命令和只读策划案浏览。它协调 Timeline、行为树与数据文档，但不把不同编辑模型合并，也不承载具体图形编辑规则。

工作区使用 `MainViewModel` 维护全局状态，每个打开文件由独立的 `DocumentViewModel` 包装。Timeline 和行为树根据文档类型选择不同视图；当前任务选择统一交给右侧 Inspector。

## UI 交互

- 用户可以新建 Timeline 或行为树、打开旧 `.editor.json`、保存、另存为和关闭文档。
- “文件 > 打开最近”按最近成功打开或保存的顺序显示最多 10 个文件；条目显示文件名，悬停提示完整路径，同一路径再次使用后移到首位。
- 最近文件为空时显示不可点击的“暂无最近文件”。点击已不存在的最近文件时提示用户并移除该条目；启动时不主动清理失效路径，以兼容暂时不可用的移动磁盘或网络盘。
- 文档以标签形式并存；切换标签会同步当前编辑器、Context 和选中任务。
- 主窗口不显示常驻任务目录；创建 Action、Condition 或行为树节点时弹出节点类型选择窗口。
- 节点类型窗口按当前入口预先过滤兼容类型，并支持按名称、类型、标签和说明搜索；取消窗口不修改文档。
- 元数据目录可以从文件夹选择器修改，刷新后重建 Task、Context 和 Enum 目录。
- 保存和验证结果显示在统一状态区域；错误不会被静默吞掉。
- Explorer 的 Design Plan 页签按日期浏览游戏工程中的策划案；策划案头部存在 `plan` 或 `review` 关联时，正文浏览区最上方分别显示暗蓝色 `Open Plan` 与 `Open Review` 按钮，不存在关联时不占用顶部空间。
- 策划案状态在 Explorer 条目和中央页签中显示彩色徽标：`In Design` 为灰色、`Todo` 为蓝色、`In Progress` 为浅棕色、`Warning` 为黄色、`Completed` 为绿色；同日文档按 Todo、In Progress、In Design、Warning、Completed 排列。
- 点击关联按钮在编辑器中央创建只读固定页签并显示对应 Markdown；关联文档不需要位于日期索引中，页签分别命名为 `Plan: <策划案名>` 与 `Review: <策划案名>`。目标已不存在或读取失败时，在统一状态区域显示英文错误，不关闭当前策划案。

## 数据来源

- BbxEditor 程序目录 `settings.json` 中的游戏项目目录、统一元数据目录、最后文档路径和最多 10 条最近文档路径。
- `TaskCatalog` 中的 Task、TaskContext 与 Enum 定义。
- 内存中的 `TimelineDocument`、`BehaviorTreeDocument` 和对应 ViewModel。
- 用户选择的旧版 `.editor.json` 文件。
- 当前游戏工程 `AutoDoc/DesignPlan/YYYY.MM.DD/*.md` 中的 `title/state/priority` 基础头、可选 `plan/review` 关联头和 Markdown 正文；主端 `AutoDoc/...` 关联路径以游戏工程根目录解析。

## 设计约束

- Windows/WPF 是当前唯一 UI 平台。
- 首版不提供撤销/重做；每次编辑直接修改当前内存文档并标记脏状态。
- Timeline 与行为树分别维护，不共享节点、连线或时间项模型。
- 关闭脏文档必须经过用户确认；保存必须通过统一验证和成对写入流程。

## Explorer

主窗口采用较宽的左侧 Explorer、中央编辑区、右侧 Inspector 的三栏布局，中央编辑区保持最大伸缩宽度。Explorer 顶部搜索框按文件名即时过滤，只呈现当前游戏工程中能被 BbxEditor 实际打开的 `.editor.json`、`.csv` 和 BbxScriptableObject `.asset` 文件；列表正文只显示文件名，完整路径放在悬停提示中。单击选择，双击或点击 Open 打开为文档页签。

游戏工程打开或元数据刷新后，文件扫描在后台任务中执行，不阻塞 WPF UI。首轮索引完成后由 FileSystemWatcher 监听配置目录，文件批量变化通过短延迟合并成一次重建。可索引目录保存在 `settings.json` 的 `explorerDirectories`，必须位于游戏工程内。

模组归属按工程相对路径决定：`Assets/Resources/**` 与 `Mods/Native/**` 同属官方 `Native`，`Mods/<ModName>/**` 属于 `<ModName>`。Explorer 右上角 `⋯` 菜单提供 All Mods 和单个模组过滤。

`gameProjectPath` 与 `metadataPath` 可以保存相对路径；解析基准固定为 BbxEditor 程序和 `settings.json` 所在目录，因此从任意工作目录启动程序都得到相同目标。
