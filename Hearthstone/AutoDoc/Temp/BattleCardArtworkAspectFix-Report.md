# 卡牌原画比例与宽度修正报告

## 结果

共享 `BattleCardItem` 已重新构建并将原画最终绘制区域统一为居中的 `210 × 297`。普通编号卡不再按 Unity 当前导入得到的 `1:2` Texture 比例缩成细长画面；普通卡、融合卡、战斗列表、备战卡池、出战槽和融合槽现在都使用同一稍宽构图。相对标准 `2:3`，最终宽度增加约 `6.1%`，在 `250 × 360` 卡面内左右各保留 `20 px`。

## 实现与边界

- `BattleCardItemUiBuilder` 将 `ArtworkArea` 固定为中心锚点、`210 × 297`、`Image.Type.Simple`、`preserveAspect = false`，并通过 Unity Editor 执行 `Hearthstone.BattleCardItemUiBuilder.Build()` 重建 `Assets/Resources/Ui/BattleCardItem.prefab`。
- `BattleCardItemController` 在每次绑定时继续强制 `preserveAspect = false`，避免对象池换绑后恢复旧的等比压窄表现。
- 保留共享 View/Controller、唯一预加载映射、Resources 路径和对象池，不建立战斗与备战的两套卡面绘制链路。
- 卡框底边、攻血徽章层级、浅色稀疏底纹、敌红我蓝、备战悬停黄色，以及仅备战阶段允许悬停和拖拽的行为均保持不变。
- 项目规则禁止创建、编辑或删除 `.meta`，因此本次没有修改 TextureImporter。Unity 已对 `Assets/Resources/Art/BattleCards/` 内除 UI 外的 104 张卡牌图片逐张执行 `ImportAssetOptions.ForceUpdate` 显式重导入，并完成强制刷新与重新编译；比例修正发生在共享 UI 的最终绘制阶段，不把它描述为导入设置变更。重导入后普通样本仍为 `1024 × 2048`，融合样本仍为 `1024 × 1536`，说明差异确实来自现有导入配置，而最终 UI 已稳定统一两者。

## 几何验证

在参考分辨率 `1920 × 1080` 的 Canvas 缩放链路下，通过 `Image.OnPopulateMesh` 实测：

| 样本 | Unity 导入 Texture | `preserveAspect` | Rect | 最终 Mesh | Mesh 比例 | 卡面左右边距 | 父级 X/Y 缩放 |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| 普通编号卡 | `1024 × 2048` | `false` | `210 × 297` | `210 × 297` | `0.707071` | 各 `20 px` | `1 / 1` |
| 99 融合卡 | `1024 × 1536` | `false` | `210 × 297` | `210 × 297` | `0.707071` | 各 `20 px` | `1 / 1` |

旧普通卡网格为 `148.5 × 297`，宽度仅为正确 `2:3` 显示的 `75%`；当前网格宽度为 `210`，已消除该横向压窄，并按用户要求进一步采用略宽构图。离屏实卡预览位于 `AutoDoc/Temp/BattleCardArtworkAspectFix-Preview.png`，确认立绘贴近边框内沿、两侧空隙收窄，底部外框不遮挡生命与攻击徽章。

## 测试与验证

- Unity 脚本校验：`BattleCardItemUiBuilder.cs`、`BattleCardItemController.cs`、`BattleRulesTests.cs` 均为 0 error；测试文件只有通用空值检查提示。
- 定向 EditMode 测试：5/5 通过，覆盖原画网格统一、边框底边与属性徽章、底板花纹、悬停颜色和备战限定交互、卡池与槽位共享比例。
- 卡牌规则与备战流程回归：33/33 通过，0 failed，0 skipped。
- 104 张卡牌原画显式 ForceUpdate 重导入后，核心比例测试再次 1/1 通过。
- Unity 强制刷新与重新编译完成；最终 Console 为 0 error / 0 warning。
- 按项目默认验证边界未进入 Play Mode。
- `git diff --name-only` 未发现 `.meta` 变更；并行工作区既有修改未被回退。

## 文档同步

- 玩家视角：同步战斗与备战卡池文档，记录稍宽立绘构图和一致的共享卡面表现。
- 美术：同步战斗卡牌模块文档，记录 `210 × 297`、约 `0.707:1`、左右 `20 px` 的当前规格。
- 程序：同步战斗与备战 UI 文档，记录普通 `1024 × 2048` 与融合 `1024 × 1536` 导入纹理统一生成相同最终网格的实现。

## 检查清单审计

清单中实现、共享 UI、框架边界、1920×1080 几何、普通/融合样本、测试、Console、文档和无 `.meta` 变更项均已通过。没有未通过项；Play Mode 验证为项目默认不执行边界。

## 清理结果

任务结束前已按要求仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码为 `0`。脚本按自身保留规则未删除本任务检查清单与预览图；本报告在清理完成后创建。

## 未解决风险

当前普通编号卡的 Unity 导入尺寸仍为 `1024 × 2048`，因为项目规则不允许修改对应 `.meta`。共享卡面已经在最终 UI 网格中稳定补偿该差异；若未来允许统一 TextureImporter，需同时复核本次 `preserveAspect = false` 的稍宽构图，避免导入层与 UI 层重复补偿。
