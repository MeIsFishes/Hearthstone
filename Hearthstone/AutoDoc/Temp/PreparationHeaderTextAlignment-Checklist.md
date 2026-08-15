# 备战页面页眉文字与边框调整检查清单

- [x] 顶部“备战阶段”字号略微缩小。
- [x] 顶部标题边框高度略微增加，且文字保持框内居中。
- [x] “出战”“融合”字号略微缩小并在各自页签内真正居中。
- [x] “继续”文字在按钮内真正居中。
- [x] 修改 `PreparationViewUiBuilder` 源码并通过 Unity Editor 重建 `PreparationView.prefab`，不直接手改 Prefab YAML。
- [x] 补充或更新布局回归测试，校验字号、尺寸、锚点、偏移和对齐方式。
- [x] 完成 Unity 脚本校验、EditMode 测试及相关项目构建验证。
- [x] 检查差异范围，确认本任务未修改、创建或删除 `.meta` 文件，未覆盖无关改动。
- [x] 复核玩家视角、程序和美术现状文档，并按实际改动更新必要内容。
- [x] 审计是否涉及框架边界变更；本任务只调整既有 Builder 的静态 Prefab 布局，不涉及框架边界。
- [x] 任务结束前逐项审计，通过后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，随后生成对应报告。

## 审计证据

- Builder 与 Prefab：`TitleFrame` 为 `580 × 110`；标题字号 `46`、`Center`、Y 偏移 `+2`；两个页签字号 `31`、`Center`、Y 偏移 `+4`；继续文字字号 `40`、`Center`、Y 偏移 `+3`。Unity Editor 已执行 `Hearthstone.PreparationViewUiBuilder.Build()` 并从重建 Prefab 重新加载核对上述序列化值。
- 脚本与测试：Builder、Editor 测试脚本的 Unity 标准校验均为 0 warning、0 error；定向布局回归 1/1 通过；全量 EditMode 68/68 通过，任务 ID `953a45e8ca5549d1a0c92f1560b3f775`。
- 编译：`Hearthstone.csproj`、`Hearthstone.Ui.Editor.csproj`、`Hearthstone.Tests.csproj` 均退出码 0、0 error；各保留 8 条既有 Unity 依赖版本 warning。
- 差异：源码、测试、三份相关文档和清单的定向 `git diff --check` 通过。Prefab 中存在 Unity 序列化器固定输出的空字段尾随空格；逻辑值由 Editor 反查与测试覆盖。本任务未触碰相关 `.meta`，现有未跟踪 `.meta` 属于工作区此前内容并保持不动。
- 文档：更新备战卡池玩家视角文档、备战卡池美术模块文档、备战界面程序文档；UI 美术总览无需变更，因为没有新增或修改二维图片资产。
- 框架：沿用唯一 `PreparationViewUiBuilder` 与 `PreparationView.prefab` 配置源，没有改 View/Controller 生命周期、UiScene、UiGroup、导出 Asset 或 BbxCommon。
- 验证边界：按项目默认规则未进入 Play Mode；本次使用 Editor Prefab 重载、序列化值反查、回归测试和工程编译验收。
- 清理：`AutoDoc/CleanupTempDocs.bat` 仅执行一次，退出码 0；清理前后文件数均为 880。
