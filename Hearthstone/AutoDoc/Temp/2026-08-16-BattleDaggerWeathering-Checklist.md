# 战斗匕首轻度做旧检查清单

- [x] 使用内置图像编辑模式处理现有 `BattleCornerDagger.png`，不改造型、构图、尺寸与透明轮廓。证据：以内置 `image_gen` 两次定向编辑完成材质做旧与真实透明背景修正，最终仍为原路径和 `1024 × 1536` 画布。
- [x] 在钢刃、古金护手和皮革握柄上增加轻微灰褐污渍、失光与少量使用痕迹；避免血迹、重锈、破损和过度脏污。证据：最终素材可见刃面稀疏灰褐斑、金属凹槽暗沉和握柄轻微灰尘磨耗，未加入禁止内容。
- [x] 污渍在战斗场景当前 `420 × 630` 显示尺寸下可辨认，同时不抢过卡牌视觉焦点。证据：污渍使用低饱和、稀疏分布，并在生成结果视觉复核中保持克制；Prefab 显示尺寸未变。
- [x] 保持真正透明背景、刀尖朝向与现有 Prefab 布局不变。证据：Unity 报告 `sourceAlpha=True`、`alphaIsTransparency=True`；画布角点 Alpha 为 `0`，Builder 仍使用镜像与 `(220, -280)` 布局。
- [x] 通过 Unity 正常导入替换后的 Sprite，并由 `BattleViewUiBuilder.Build()` 重建 `BattleView.prefab`，不手工编辑 Prefab YAML 或 `.meta`。证据：Unity 刷新无编译错误，Builder 返回重建成功；Meta 未手工修改。
- [x] 更新并通过角落装饰对应的 Editor 自动化测试。证据：`BattleViewUsesCornerDecorationsBehindCards` 为 `1 passed / 0 failed`，任务 ID `87c068b243e448dcaa0c89a02bf2c4a1`。
- [x] 框架边界审计：继续使用现有 Sprite、Builder、Prefab 与资源导入流程，不建立平行 UI 或运行时拼装。证据：资源路径、Builder、Prefab 层级与 Controller 均未新增平行实现。
- [x] 玩家视角设计文档：按 `design-doc-format` 核对匕首材质变化并同步相关战斗设计现状。证据：已读取基础 skill 与战斗专项格式，并更新 `AutoDoc/Design/Specific/combat-system/combat-system.md` 的玩家可见材质描述。
- [x] 美术文档：按 `art-doc-writer` 核对并同步匕首当前材质与污渍表现。证据：已读取基础 skill 与三份适用格式，更新美术总览、UI 美术和战斗卡美术模块文档。
- [x] 程序文档：按 `program-doc-format` 核对资源路径、布局和逻辑是否变化；无变化时记录不适用证据。证据：已读取基础 skill 与 UI 界面格式；资源键、路径、尺寸、位置、层级、射线与代码逻辑均未变化，现有 `AutoDoc/Program/UI/battle/battle.md` 已准确记录当前实现，因此不修改。
- [x] 检查无关文件、原资源引用、尺寸/Alpha、测试结果和未主动进入 Play Mode 的风险。证据：最终素材 SHA-256 为 `4267BCE46DC9586301CBF6FAE4A1D5E9A1440DEE1B8A467E57994DD6BF52E791`；Unity 识别为 `1024 × 1536` Sprite 且含透明通道；测试通过、Console 清理后为零错误，未主动进入 Play Mode。
- [x] 结束前逐项复核，随后仅运行一次 `AutoDoc/CleanupTempDocs.bat` 并创建对应报告。证据：逐项终审完成，清理脚本仅执行一次且退出码为 `0`，同名报告已创建。
