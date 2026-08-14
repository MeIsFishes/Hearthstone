# 战斗场景 UI 重制检查清单

## 1. 用户要求

- [通过] 将当前战斗场景 UI 调整为参考图的整体构图与质感。证据：`BattleView.prefab` 已接入木质金边羊皮纸底板，上下卡列位于 `y = ±224`，最终 `1920 × 1080` 编辑器实拍显示双方各三张卡牌。
- [通过] 补充实现所缺的美术资源。证据：新增 `BattleBoardBackground.png`；当前阵营框采用 `CardFrame-v3.png` 与并发更新后的 `CardFrameBlue-v2.png`，均位于 `Assets/Resources/Art/BattleCards/UI/`。
- [通过] 删除界面中的“我方”“敌方”文字标记。证据：`BattleView.prefab` 中 `EnemyLabel`、`PlayerLabel` 均不存在，UI 源码和相关 Prefab/场景扫描无“我方”“敌方”。
- [通过] 保持现有战斗交互与数据逻辑可用。证据：Controller 继续监听原 ECS 数据并使用原 `UiList` 生命周期；`Hearthstone.Tests.BattleRulesTests` 14/14 通过。

## 2. 项目与框架边界

- [通过] 已定位 `BattleView.prefab`、`BattleCardItem.prefab`、成对 View/Controller、`Battle.unity`、`Battle.asset`、`EBattleUiGroup.Main` 与 `BattleStage`，修改落在 View Prefab 和 Controller 合法职责内。
- [通过] 静态结构与引用保存在 Prefab。证据：卡号徽章/文本由原运行时 `new GameObject` 迁入 `BattleCardItem.prefab`，View 持有序列化引用；Controller 不再运行时拼装静态 UI。
- [通过] 资源通过 `ResourceApi.LoadSprite` 或 Prefab 直连 Sprite 使用；未访问 `ResourceManager`，未手写 Scene、Prefab 或 `.asset` YAML。
- [不适用] UI 编辑场景导出信息未变化：`UiGroup`、`DefaultShow`、场景级位置/缩放/Pivot 与 `PrefabPath` 均保持原值，因此无需重新导出；`Resources.Load<UiSceneAsset>("Ui/Battle")` 已验证非空。
- [通过] 未发现框架外平行实现或遗漏直接依赖；并发出现的 `CardFrame-v3.png`、`CardFrameBlue-v2.png` 与资源键改动已保留并纳入最终验收和文档。
- [通过] 新增 View 字段仅用于长期序列化引用，Controller 仅保留阵营资源键；已删除一次性运行时构建方法和未使用命名空间。
- [通过] 未用文件编辑工具创建、编辑或删除 `.meta`；新增图片的导入元数据仅由 Unity AssetDatabase/Importer 自动维护。

## 3. 美术资源与导入

- [通过] 已盘点并复用五张怪物原画、属性徽章、编号徽章与现有卡框；只为缺失的空战场底板使用内置 imagegen。
- [通过] 底板提示词明确精确对象移除、木质/暖金/羊皮纸材质、横向构图、中央留白、无文字/标志/水印；蓝框探索提示词明确只改红色珐琅与保持透明 Alpha。
- [通过] 已检查构图、边缘和 Alpha。证据：底板为 `1672 × 941` Bgr24；当前红蓝框均为 `1024 × 1536` Bgra32，实拍无方形底色或白边。imagegen 生成文件已落入项目，未仅留在 Codex 生成目录。
- [通过] 图片通过 Unity 导入；底板与红蓝框均为单 Sprite、关闭 Mipmap、Clamp/Bilinear，并可由 `Resources.Load<Sprite>()` 加载。

## 4. Unity MCP 与验证

- [通过] `unityMCP` 已确认使用绝对 `uvx.exe`、`mcpforunityserver==10.0.0`、stdio 与 UTF-8/SystemRoot 环境；实例为 `Hearthstone@e97c0c17`。
- [通过] 修改前探针：活动场景 `Assets/Scenes/Main.unity`、`isDirty=false`；Console 无项目编译错误。
- [通过] Scene/Prefab/GameObject/组件和导入设置均通过当前会话实际暴露的 MCP for Unity/Unity Editor API 完成；未使用替代桥、私有协议或手写 YAML。
- [通过] 修改后刷新与编译完成；最终活动场景仍为 `Main` 且 `isDirty=false`，Console error 为 0。
- [通过] 未进入 Play Mode；完成 `1920 × 1080` 编辑器 Game View 实拍、Prefab 结构、资源加载和 14 项 EditMode 测试。未做游戏内时序实测的风险记录在最终报告。

## 5. 文档同步

- [通过] 玩家视角设计文档：已读取 `design-doc-format` 及战斗系统专项格式，更新 `AutoDoc/Design/Specific/combat-system/combat-system.md` 的状态文字、阵营框与界面布局现状。
- [通过] 美术文档：已读取 `art-doc-writer`、风格总览/UI/模块格式，更新 `art-style-overview.md`、`ui-art-overview.md` 与 `battle-card.md` 的底板、红蓝卡框、尺寸、路径和当前引用。
- [通过] 程序文档：已读取 `program-doc-format` 与 UI 界面格式，更新 `AutoDoc/Program/UI/battle/battle.md` 的 Prefab 结构、监听反馈、资源键与框架边界。
- [不适用] 未新增或修改任何自定义 `BbxUiItem` 类型；只修改业务 View Prefab、View/Controller 和既有 `UiList` 配置，因此无需更新 `AutoDoc/UIItem/`。

## 6. 结束审计

- [通过] 已重新打开本清单并逐项对照 Prefab、源码、资源、实拍、文档、测试和 Unity 只读探针复核；可修正缺口均已修正。
- [不适用] `AutoDoc/CleanupTempDocs.bat` 是清单终审后的后置动作，实际退出结果记录到同名报告。
- [不适用] `BattleSceneUIRefresh-Report.md` 在一次性清理后创建，实际创建结果由报告文件本身证明。
