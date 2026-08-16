# 备战槽位、拖拽回牌库与结算文字美术检查清单

## 用户要求

- [x] 通过：`PreparationView.prefab` 的出战列表为 `1110 × 320`，步距为 `185 × 266.4`，占用卡面按 `0.76` 填充，宽约 `140.6 px`，可见间隙约 `44.4 px`。
- [x] 通过：卡牌离开原出战槽且未成功落入其他槽时调用 `TryRemoveCardFromBattleSlot()`，只清空阵容槽，持有卡仍在牌库。
- [x] 通过：使用内置 imagegen 生成黄色“已出战”旧木板透明图片，并替换运行时 TMP。
- [x] 通过：使用内置 imagegen 生成文字与底板一体的“胜利”“失败”“大获全胜”完整 UI；失败与整局胜利固定说明已画入面板。
- [x] 通过：生成完整“返回主菜单”按钮图片，Prefab 中 Button 以该 Image 为 `targetGraphic`，无程序色块、Outline 或 Label。
- [x] 通过：五张最终资产统一为陈旧羊皮纸、旧木、磨损布料、黑铁与克制古金的轻油画风格，避免塑料感与圆润玩具感。
- [x] 通过：普通胜利与整局胜利使用蓝金，失败使用暗红黑；饱和度和装饰量受做旧材质约束。
- [x] 通过：结算按钮调用 `EnterMainMenuStageGroup()`，不再调用 `RestartRun()`。
- [x] 通过：美术风格总文档、UI 美术总览及相关模块文档均已同步视觉基准、禁用特征、资产路径与接入规格。

## imagegen 资产流程

- [x] 通过：生成前检查现有羊皮纸做旧层、备战/战斗背景、槽框、按钮、卡框与卡片原画，提取材质和配色基准。
- [x] 通过：五张最终资产均由内置 imagegen 分别生成，中文逐张目检。
- [x] 通过：最终文件均为 `Format32bppArgb`，四角 Alpha 为 0；文字无额外字符或水印。
- [x] 通过：最终资产复制到 `Assets/Resources/Art/`；四张淘汰的拆分结算文字/旧重开按钮草稿通过 Unity AssetDatabase 删除。
- [x] 通过：Builder、Prefab、Controller 已接入；最终报告记录资产路径、提示词摘要与内置工具模式。

## UI 与框架边界

- [x] 通过：Preparation/Battle 保持唯一 View/Controller，静态层级由对应 UiBuilder 重建。
- [x] 通过：动态卡牌继续使用既有 `UiList`、预加载映射与对象池。
- [x] 通过：拖离逻辑复用 `UiDragable.OnBackFromTop`，并通过 `RunCardRules` 公开规则入口写状态。
- [x] 不适用：未改变 UiScene Group、DefaultShow、页面根 Position/Scale/Pivot 或导出路径，无需重新导出 UiSceneAsset。
- [x] 通过：Prefab 由 Builder 生成；`.meta` 的新增/删除均由 Unity 导入和 AssetDatabase 处理，未手写 YAML 或 `.meta`。
- [x] 通过：新增字段和函数分别承担资源引用、主菜单按钮和权威移除规则，无重复一次性抽象。

## 验证与文档

- [x] 通过：Unity 结构检查得到步距 `185`、卡面宽 `140.6`、间隙 `44.4`，比例缩放为 `0.5624`。
- [x] 通过：规则测试覆盖拖入、替换、换槽、预期卡号不匹配及移除后仍持有；源码结构测试覆盖拖离原槽判定。
- [x] 通过：Prefab 检查确认“已出战”、胜利横幅、失败面板、整局胜利面板、返回主菜单按钮均为目标 Sprite；旧标题、正文和按钮 Label 不存在。
- [x] 通过：Controller 源码测试确认两个结果弹窗共用按钮、点击后禁用并调用 `EnterMainMenuStageGroup()`；未进入 Play Mode。
- [x] 通过：EditMode `95/95`；`Hearthstone.csproj` 与 `Hearthstone.Editor.csproj` 顺序构建均 0 error；Unity 刷新编译通过，清理测试预期日志后 Console 0 error。
- [x] 通过：活动场景为 `Assets/Scenes/Main.unity`、`Dirty=False`、`Playing=False`；按项目约定未执行游戏内视觉/交互验收。
- [x] 通过：按设计文档 skill 同步战斗系统与备战卡池玩家视角现状。
- [x] 通过：按美术文档 skill 同步风格总览、UI 总览、战斗卡牌与备战卡池模块。
- [x] 通过：按程序文档 skill 同步 Battle/Preparation UI 与战斗/备战规则现状。
- [x] 通过：已重新读取并逐项审计本清单，未发现未通过项。
- [x] 通过：已仅运行一次 `AutoDoc/CleanupTempDocs.bat`；脚本正常退出，未删除仍需保留的本任务 Checklist，随后创建对应 Report。
