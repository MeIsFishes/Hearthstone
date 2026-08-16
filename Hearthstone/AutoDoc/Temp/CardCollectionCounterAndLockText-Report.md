# 图鉴计数与锁定文字修正报告

## 结果

已修复图鉴左上角解锁计数缺失问题。计数现在使用明确的“已解锁 k/n”格式，放置在低饱和羊皮纸木框内，并通过 Builder 生成顺序保证其渲染层级高于卡池面板。

图鉴锁定卡在复用 99 号封印图像后，会额外清空卡名、词条和 Tooltip，因此不再显示“融合封印”。该处理仅用于图鉴绑定，不影响备战卡池原有的 99 号分隔位。

## 检查结果

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| “已解锁 k/n”静态与动态文本 | 通过 | Prefab 默认文本为“已解锁 0/0”；Controller 刷新为真实 `collected/total` |
| 计数显示层级 | 通过 | Builder 先创建卡池、后创建 Header；测试校验计数根节点 sibling index 高于卡池 |
| 移除“融合封印” | 通过 | 图鉴锁定分支调用 `HideCollectionLockedText`，清空卡名、词条与 Tooltip |
| 备战 99 号显示不变 | 通过 | `ShowLockedPreparationCard` 保持原实现，额外隐藏只在 `BindCollection` 分支发生 |
| Prefab 与 UiSceneAsset | 通过 | 已通过 Builder 重建 `CardCollectionView.prefab` 和 MainMenu 编辑场景/导出资产 |
| 文档 | 通过 | 已同步局外收藏设计、图鉴程序和 UI 美术现状 |

## 验证

- 图鉴定向 EditMode：`6/6` 通过。
- 完整 EditMode：`101/101` 通过。
- 最终 Unity Console：`0` error。
- 未进入 Play Mode。

## 清理

本任务结束前已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，命令成功退出，随后创建本报告。
