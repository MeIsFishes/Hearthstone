# 战斗卡牌动画任务报告

## 任务结果

已为战斗卡牌接入攻击前拱、按种类播放的目标帧特效、现成音效库延迟播放、受击闪红和动画完成后再推进下一单位的完整时序。`BattleCardTypeCsvData.csv` 已加入帧动画键、音效键、音效延迟和受击延迟四列。此前误生成的 `.music` 与 WAV 已全部删除，最终实现只引用用户拷入的现成音效库。

## 配置与资源

| 卡牌种类 | 帧动画资源 | 现成音效键 | 音效延迟 | 受击延迟 |
| --- | --- | --- | ---: | ---: |
| 哥布林战士 | `BattleAttackSwordSlash` | `knifeSlice2` | `0.12s` | `0.18s` |
| 哥布林弓手 | `BattleAttackArrowImpact` | `impactWood_light_002` | `0.18s` | `0.18s` |
| 哥布林投弹手 | `BattleAttackSmallExplosion` | `explosionCrunch_001` | `0.10s` | `0.12s` |
| 野猪 | `BattleAttackSmallImpact` | `impactPunch_medium_002` | `0.08s` | `0.12s` |
| 食人魔 | `BattleAttackLargeImpact` | `impactPunch_heavy_002` | `0.12s` | `0.15s` |

五个音效 basename 在 `Assets/Resources/BbxCommon/Audio/Library/` 中各出现一次；来源目录为 RPG Audio、Impact Sounds 与 Sci-fi Sounds，目录内许可证文件可追溯。战斗播放使用 `Combat` 分组、`BattleCardAttack` 并发键、最多三声并发与 `0.72` 音量衰减。

新增五张透明八帧图集：

- `BattleAttackSwordSlash.png`：`1536 × 1024`，冷白剑痕。
- `BattleAttackArrowImpact.png`：`1774 × 887`，斜上方射入的箭矢与碰撞碎屑。
- `BattleAttackSmallExplosion.png`：`1774 × 887`，橙黄小型爆炸与烟尘。
- `BattleAttackSmallImpact.png`：`1774 × 887`，紧凑轻量击打环与碎屑。
- `BattleAttackLargeImpact.png`：`1774 × 887`，更大范围的冲击波、火花与碎屑。

以上资源由内置 ImageGen 生成。最终提示词共同要求：透明背景、无文字、无水印、明亮奇幻手绘游戏特效、严格 `4 × 2` 八帧图集、按左上到右下呈现淡入/命中/消散过程；五个变体分别指定冷白剑痕、斜向箭矢、小型爆炸、小型击打和大型击打，并要求单帧主体位于安全区、适合叠加在竖向卡面上。

## 实现证据

- `BattleSystem` 使用挂起表现状态推进统一时钟；到达音效延迟时触发一次 `AudioApi.Play`，到达受击延迟时写入主伤害、反击伤害和溅射伤害，完整表现结束后才提交存活状态、胜负和下一行动。
- `BattleRules.GetAttackPresentationDuration()` 取前拱、八帧图集、音效延迟、受击延迟加闪红时长的最大值。
- `BattleCardItemController` 在池化条目初始化时创建一个可复用 `RawImage`，通过 UV 切片播放 `4 × 2` 图集；攻击者使用正弦位移前拱并回位，目标在受击点闪红并恢复原色。
- 旧格式 CSV 安全回退为空表现配置；空资源键、失效 Entity、换绑、回池和会话回收均有清理或保护。
- 未手写资源字典，也未创建、编辑或删除 `.meta`；编辑器下由既有 ResourceManager 扫描 Resources。

## 检查清单状态

所有功能、配置、资源、框架边界、文档同步和静态验收项均通过。Unity 运行画面手工验收按项目默认规则标记为不适用，本次未启动游戏；建议下次正常打开项目后目视确认图集边界和最终动效节奏。

## 验证结果

- `dotnet build Hearthstone.csproj --no-restore`：通过，0 错误；8 条既有依赖程序集版本冲突警告。
- `dotnet build Hearthstone.Tests.csproj --no-restore`：通过，0 错误；8 条既有依赖程序集版本冲突警告。
- `dotnet build Hearthstone.sln --no-restore`：未通过，既有解决方案包含重复项目名 `Hearthstone`，报 `MSB5004`；已改用两个直接项目编译完成范围验证。
- 定向 `git diff --check`：通过。
- CSV：6 条数据行，每行与表头一致为 11 列。
- 位图：5 张 PNG 均检测到 Alpha；未生成特效 `.meta`。
- 音效：5 个配置键 basename 均唯一，库内共检测到 10 个许可证文件；临时 `.music`/WAV 残留为 0。

## 文档同步

- 玩家设计：`AutoDoc/Design/Specific/combat-system/combat-system.md`
- 美术模块：`AutoDoc/Art/Modules/battle-card/battle-card.md`
- 战斗程序：`AutoDoc/Program/Specific/combat-system/combat-system.md`
- 战斗界面程序：`AutoDoc/Program/UI/battle/battle.md`

## 偏差与未解决风险

- 未进入 Unity 做运行画面验收，因此图集在真实卡面上的构图、特效节奏与主观响度仍需一次目视/试听确认。
- 新 PNG 按项目规则未创建 `.meta`；Unity 下次正常导入时会生成本地导入信息。
- 工作区原本存在大量与本任务无关的未提交修改，本任务没有回退或覆盖这些内容。

## 临时文档清理

结束审计后仅运行一次 `AutoDoc/CleanupTempDocs.bat`，退出码 `0`。清理前后 `AutoDoc/Temp/` 下 Markdown 文件数均为 `136`，未达到脚本的 `500` 文件清理阈值，因此没有删除现有临时文档。
