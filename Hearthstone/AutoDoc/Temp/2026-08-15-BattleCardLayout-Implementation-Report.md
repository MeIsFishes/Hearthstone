# 战斗场景竖向卡牌实施报告

## 任务结果

计划已完整实施。战斗卡牌改为 `220 × 320` 的竖向长方形，由 `BattleCardItemController` 与 `UiList` 管理界面生命周期；Entity 仅保存玩法状态。玩家卡牌本地左下显示生命、右下显示攻击，上半区为留空原画区，下半区为技能说明区；无技能时隐藏文字。敌方整张卡面旋转 `180°`，文字、数值、高亮与遮罩一同倒置。

## 主要产物

- 新增 `BattleCardCsvData` 与 `Assets/Resources/Config/BattleCardCsvData.csv`，默认配置 ID `1` 提供攻击 `3`、生命 `5` 和空技能说明。
- `BattleCardRawComponent` 保存配置 ID 并由 CSV 初始化攻血；`BattleStages.InitializeBattleRuntime` 在创建战斗实体前读取配置。
- `BattleCardItemView`、`BattleCardItemController`、`BattleCardItem.prefab` 与 `BattleView.prefab` 已同步新的卡面结构、绑定、隐藏规则、方向和列表尺寸。
- Pre-load 映射为 `Hearthstone.BattleCardItemController → Ui/BattleCardItem`，资源字典包含卡牌 Prefab、CSV 与 Stage 数据注册资产。
- 新增并同步玩家设计、美术与程序现状文档。

## 运行时缺口与修复

首次实际启动暴露 `Battle card configuration 1 is missing`。诊断确认 CSV 内容可正常解析、资源键也正确，根因是项目缺少框架要求的 `Assets/Resources/BbxCommon/ScriptableObjectAssets.asset`；`GameStage.OnLoadStageData()` 在该资产为空时提前返回，因此没有执行 CSV 自动加载。

已通过官方 Unity MCP 使用 Unity Editor API 创建并初始化该空注册资产，没有手写 Unity 序列化文件，也未人工操作 `.meta`。`ProjectInitializationBuilder` 同步加入该资产的生成逻辑，防止重新初始化项目时复现。测试补充了注册资产与运行时 CSV 资源链路覆盖。

## 验证结果

- Unity MCP：v10.0.0 链路可用，目标实例为当前 Hearthstone Editor。
- 编译：改动脚本标准诊断为 0 error、0 warning；最终 Console error 为 0。
- EditMode：`Hearthstone.Tests` 最终 11 passed、0 failed、0 skipped，任务 ID `9235b2b71a284932b5251ace369552ee`。
- 实际启动：运行时配置为 `1:3:5`，战斗会话包含玩家 3 张、敌方 3 张卡牌，场景中存在 6 个 `BattleCardItemController`；未再出现配置缺失异常。
- 收尾：已退出 Play Mode；活动场景仍为 `Assets/Scenes/Main.unity`，`isDirty=false`。
- 临时文档清理：本任务仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 0；运行时补修后未重复执行。

## 偏差与风险

原计划默认不进入 Play Mode；因用户提供了实际启动异常，本次补充了最小范围的 Play 验证并已安全退出。原画区域按需求保持空白，当前没有独立位图原画资产。其余需求与框架边界无未解决项。
