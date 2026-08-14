# UiList

## 1. 组件用途

`UiList` 用于管理一组由 UI Controller 创建的子 UI 对象，并按照指定规则排列这些对象。

适用场景：

- 卡牌、技能图标、背包格子、头像列表等需要批量排列的 UI。
- 列表数量由运行时逻辑决定，且列表项由 `UiController` 创建和管理的 UI。
- 需要列表项在重新排列时播放简单位移动画的 UI。

`UiList` 不用于直接排列 Unity 层级中已经手动摆好的普通子物体。列表项应通过 `ItemWrapper` 添加、删除或修正数量。

## 2. 基本使用流程

1. 在作为列表容器的 UI GameObject 上挂载 `UiList`。
2. 调整该 GameObject 的 `RectTransform`，确定列表排列区域；`UiList` 的区域大小取自身 `RectTransform` 的宽高。
3. 在 Inspector 中设置排列模式、排列方向、槽位大小和位移动画配置。
4. 在 View 中暴露 `UiList` 字段，供 Controller 访问。
5. 在 Controller 中通过 `ItemWrapper.AddItem<TController>()` 添加列表项，或通过 `ItemWrapper.ModifyCount<TController>(count)` 修正列表项数量；`TController` 使用列表项对应的 `UiController` 类型。
6. 列表项数量变化后，`UiList` 按当前配置重新排列列表项。

## 3. 配置项

### LayoutType

- 描述：控制 `UiList` 使用哪一种排列规则。
- 默认行为：未显式修改时，使用组件当前 Inspector 中保存的排列规则。
- 配置值说明：
  - `ConstantSlot`：按固定槽位排列。每个列表项占用一个固定大小的槽位，当前行或列放不下时进入下一行或列。
  - `AreaFit`：按区域自适应排列。列表项在 `RectTransform` 区域内分散排布，列表项数量增加时自动压缩间距，使对象尽量落在区域范围内。

### Direction

- 描述：控制列表项优先沿哪个方向排列。
- 默认行为：未显式修改时，使用组件当前 Inspector 中保存的方向。
- 配置值说明：
  - `Horizontal`：优先横向排列。`ConstantSlot` 下先从左到右排列，当前行放不下后进入下一行；`AreaFit` 下主要沿横向分散。
  - `Vertical`：优先纵向排列。`ConstantSlot` 下先从上到下排列，当前列放不下后进入下一列；`AreaFit` 下主要沿纵向分散。

### SlotSize

- 描述：表示单个列表项占用的槽位尺寸。
- 默认行为：未显式修改时，使用组件当前 Inspector 中保存的尺寸。
- 配置值说明：填写 `Vector2`。`x` 表示槽位宽度，`y` 表示槽位高度。值应大于 0；值过小会导致列表项互相重叠，值过大会导致可容纳数量减少。

### UseTranslation

- 描述：控制列表重新排列时，列表项是否从基准位置渐变移动到目标位置。
- 默认行为：未勾选时，列表项直接出现在排列后的目标位置。
- 配置值说明：
  - `true`：启用位移动画。列表刷新时，列表项按 `TranslationCurve` 和 `TranslationTime` 移动到目标位置。
  - `false`：关闭位移动画。列表刷新时，列表项立即更新到目标位置。

### TranslationCurve

- 描述：控制启用 `UseTranslation` 后的位移动画曲线。
- 默认行为：`UseTranslation` 为 `false` 时，该配置不产生表现效果。
- 配置值说明：填写 `AnimationCurve`。曲线纵轴 `0` 表示基准位置，纵轴 `1` 表示目标排列位置。曲线越接近线性，移动越匀速；曲线起伏越大，移动节奏越明显。

### TranslationTime

- 描述：控制启用 `UseTranslation` 后的位移动画持续时间。
- 默认行为：`UseTranslation` 为 `false` 时，该配置不产生表现效果。
- 配置值说明：填写非负浮点数。`0` 表示立即到达目标位置；大于 `0` 表示在对应秒数内完成移动。

### ItemWrapper

- 描述：运行时管理列表项的入口。
- 默认行为：列表为空时，`ItemWrapper` 不包含任何列表项；只有调用添加或修正数量的方法后才会创建列表项。
- 配置值说明：在代码中通过 `m_View.UiList.ItemWrapper` 访问。使用 `AddItem<TController>()` 添加一个列表项；使用 `ModifyCount<TController>(count)` 将列表项数量修正为指定数量。`TController` 必须是列表项对应的 UI Controller 类型。

## 4. 常用 API

### ItemWrapper.AddItem<TController>()

添加一个 `TController` 对应的列表项。

调用后，新增列表项会进入 UI 生命周期，并参与 `UiList` 的下一次排列。

```csharp
m_View.UiList.ItemWrapper.AddItem<UiTestItemController>();
```

### ItemWrapper.ModifyCount<TController>(int count)

将 `TController` 对应的列表项数量修正为 `count`。

当当前数量小于 `count` 时，`UiList` 会补足列表项；当当前数量大于 `count` 时，`UiList` 会从末尾移除多余列表项。

```csharp
m_View.UiList.ItemWrapper.ModifyCount<UiTestItemController>(5);
```

## 5. 使用示例

目标：显示 5 个技能图标，并横向排列。

配置：

- `LayoutType`：`ConstantSlot`
- `Direction`：`Horizontal`
- `SlotSize`：`(150, 200)`
- `UseTranslation`：`true`
- `TranslationCurve`：从 `0` 平滑到 `1` 的曲线
- `TranslationTime`：`0.2`

Controller 调用：

```csharp
m_View.UiList.ItemWrapper.ModifyCount<UiSkillIconController>(5);
```

运行时表现：`UiList` 创建或保留 5 个 `UiSkillIconController` 对应的列表项，并按横向固定槽位排列；每次刷新排列时，列表项在 `0.2` 秒内移动到目标位置。
