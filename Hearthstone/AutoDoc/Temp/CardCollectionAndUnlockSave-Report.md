# 图鉴与解锁存档任务报告

## 结果

任务已完成。主菜单新增“图鉴”与右上角红色“清除数据”；图鉴在 `MainMenuStage` 内作为默认隐藏的独立 View/Controller 打开，展示当前卡表中除 99 号分隔位和四卡融合结果外的 147 张卡。已解锁卡可放大并显示词条提示，卡外点击后以皮革音效缩至 `0.3` 并从屏幕横向中心移出底边。

永久解锁使用独立 JSON 文件，当前 Windows 实际解析路径为 `C:\Users\黄昕玮\AppData\Local\DefaultCompany\99升变\card-collection.json`。奖励批次成功应用、融合成功和备战页兼容同步当前局卡牌时登记解锁；调试清除只删除该主文件及其同名临时文件。

## 检查项状态与证据

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 卡表范围与动态总数 | 通过 | CSV 共 213 行；排除 99 号与 65 个四卡配方，得到普通卡 98、二/三卡融合 49，总数 147；`CardCollectionCatalog` 按当前数据计算 |
| MainMenu UiScene 边界 | 通过 | `MainMenu.asset` 含 `Ui/MainMenuView` 与 `Ui/CardCollectionView` 两项；图鉴 `DefaultShow=false`，两页同属 `EMainMenuUiGroup.Main` |
| View/Controller 唯一性与对象池复用 | 通过 | 新增唯一 `CardCollectionView` / `CardCollectionController`；卡池与预览均由 `UiList` 创建既有 `BattleCardItemController` |
| 锁定、计数与标准卡面 | 通过 | 未解锁项绑定 99 号封印面并显示 `??`、禁用点击；已解锁项复用完整卡面、等阶框、攻血和词条 Tooltip；左上角动态输出 `collected/total` |
| 放大与收入口袋动画 | 通过 | 灰黑 `76%` 全屏蒙板；预览 `2.0` 倍；卡外点击后 `0.36s` 内移动到实际蒙板底边 `x=0`，最终缩放 `0.3`，播放 `handleSmallLeather` |
| 主菜单入口 | 通过 | “图鉴”复用透明常态与低饱和湿润羊皮纸悬停底纹；“清除数据”为右上角透明点击区与红色 TMP 文字 |
| 持久化与安全清除 | 通过 | `CardCollectionRepository` 延迟读取、Set 去重、卡表过滤、同目录临时文件写入和原子替换；清除目标仅为精确主文件与 `.tmp` |
| 解锁写入路径 | 通过 | `PreparationStages.InitializePreparationRuntime` 仅在奖励返回 `Applied` 后登记；`PreparationController` 仅在融合返回 `Applied` 后登记；页面打开补登记当前局已有卡 |
| Builder / Scene / Asset | 通过 | `CardCollectionViewUiBuilder` 生成 Prefab；`MainMenuUiSceneBuilder` 通过 connected Prefab 生成编辑场景并重新导出 UiSceneAsset；未手工编辑 YAML 或 `.meta` |
| 文档同步 | 通过 | 更新开始界面、主菜单程序与美术文档；新增图鉴 UI、局外收藏和存档的设计/程序现状文档 |
| 框架边界审计 | 通过 | Controller 未运行时搭建静态页；未自建对象池；页面切换走 `UiApi`，资源走 `ResourceApi`/UiScene；永久状态不存于 View |
| 工作树保护 | 通过 | 保留任务开始前已有的美术、代码、Prefab、文档及项目设置改动，没有还原或覆盖无关变更 |

## 验证结果

- Unity 脚本编译通过。
- 定向 `CardCollectionTests + MainMenuTests`：`12/12` 通过。
- 完整 EditMode：`100/100` 通过。
- 最终清空测试产生的预期异常日志并重新编译后，Unity Console：`0` error。
- 按项目约定未进入 Play Mode。

## 偏差与说明

- 用户示例为 `37/136`，但当前真实卡表按需求排除四卡融合结果后总数是 147，因此界面使用动态总数而非硬编码 136。
- 存档使用 Windows 用户级本地应用数据目录；在当前环境位于 C 盘。这样无需管理员权限，同时保持与程序和 Windows 用户隔离。
- 融合卡没有独立静态随机实例数据；图鉴以配方素材类型的最低基础攻血之和和初始词条并集呈现可解释的静态卡面，永久存档仍只保存解锁卡号。

## 未解决风险

- 未按项目默认策略进入 Play Mode，故未做真人鼠标操作的视觉验收；Prefab 引用、状态规则、动画常量、Scene 导出与跳转结构已由 EditMode 测试覆盖。
- `Version=1` 已写入文件，但当前没有旧版本迁移分支；读取损坏或无效内容时会忽略并以空收藏继续。

## 清理结果

任务结束前已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，命令成功退出。清理脚本按项目自身规则保留了现有 Temp 历史文件；随后创建本报告。
