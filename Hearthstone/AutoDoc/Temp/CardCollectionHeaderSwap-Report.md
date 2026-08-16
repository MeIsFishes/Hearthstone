# 图鉴顶部布局交换报告

## 结果

图鉴返回按钮已移动到左上角；“已解锁 k/n”计数框已移动到右上角，内部文字同步改为右对齐。两者继续保持在卡池面板上层。

## 实现与验证

- `CardCollectionViewUiBuilder` 将返回按钮锚点调整为左上，将计数框锚点调整为右上。
- 已通过 Builder 重建 `CardCollectionView.prefab`、MainMenu 编辑场景和 UiSceneAsset。
- 测试新增左右锚点和计数对齐校验。
- 完整 EditMode：`101/101` 通过。
- 最终 Unity Console：`0` error。
- 未进入 Play Mode。

## 文档与清理

已同步图鉴玩家设计、程序界面和 UI 美术文档。本任务结束前已且仅已运行一次 `AutoDoc/CleanupTempDocs.bat`，随后创建本报告。
