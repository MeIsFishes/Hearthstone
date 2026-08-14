# UiList

## 1. 组件用途

`UiList` 用于管理一组由 UI Controller 创建的子 UI 对象，并按固定槽位、区域自适应或调用方手动定位规则管理这些对象。

适合用于技能图标、背包格子、卡牌、头像列表等运行时数量会变化的 UI 列表。

## 2. 基本使用流程

1. 在列表容器 GameObject 上挂载 `UiList`。
2. 调整该 GameObject 的 `RectTransform`，确定列表排列区域。
3. 在 Inspector 中配置排列模式；自动排列模式继续配置方向、槽位大小和位移动画，手动模式由 Controller 设置条目位置。
4. 在 View 中暴露 `UiList` 字段。
5. 在 Controller 中通过 `m_View.UiList.ItemWrapper` 添加、移除或修正列表项数量。

## 3. 配置项

### ArragementType

- 描述：选择列表排列规则。
- 默认行为：使用 Inspector 当前保存的枚举值。
- 配置值说明：
  - `ConstantSlot`：按固定槽位排列，当前行或列放不下时进入下一行或列。
  - `AreaFit`：在 `RectTransform` 区域内分散排列，数量较多时压缩间距。
  - `Manual`：`UiList` 只管理列表项的创建、移除与 UI 生命周期，不修改调用方设置的条目位置；后续添加、移除或刷新布局时仍保留现有位置。

### ConstantSlotDirection

- 描述：`ArragementType` 为 `ConstantSlot` 时的优先排列方向。
- 默认行为：使用 Inspector 当前保存的方向。
- 配置值说明：
  - `Horizontal`：优先从左到右排列。
  - `Vertical`：优先从上到下排列。

### ConstantSlotSize

- 描述：`ConstantSlot` 模式下每个槽位的宽高。
- 默认行为：使用 Inspector 当前保存的 `Vector2`。
- 配置值说明：`x` 表示槽位宽度，`y` 表示槽位高度；值过小会导致列表项重叠，值过大会减少单行或单列容量。

### AreaDirection

- 描述：`ArragementType` 为 `AreaFit` 时的分散方向。
- 默认行为：使用 Inspector 当前保存的方向。
- 配置值说明：
  - `Horizontal`：沿横向分散列表项。
  - `Vertical`：沿纵向分散列表项。

### AreaSlotSize

- 描述：`AreaFit` 模式下单个列表项占用的参考尺寸。
- 默认行为：使用 Inspector 当前保存的 `Vector2`。
- 配置值说明：横向排列主要使用 `x`，纵向排列主要使用 `y`；该值决定对象之间的最小间隔和边界留距。

### UseTranslation

- 描述：控制列表重新排列时是否播放位移动画。
- 默认行为：`false` 时列表项直接移动到目标位置；`Manual` 模式下不产生位置变化。
- 配置值说明：
  - `true`：按 `TranslationCurve` 和 `TranslationTime` 渐变移动。
  - `false`：立即更新列表项位置。

### TranslationCurve

- 描述：控制位移动画的采样曲线。
- 默认行为：`UseTranslation` 为 `false` 时不生效。
- 配置值说明：曲线纵轴 `0` 表示起点，`1` 表示目标位置；曲线形状决定移动节奏。

### TranslationTime

- 描述：控制位移动画持续时间。
- 默认行为：`UseTranslation` 为 `false` 时不生效。
- 配置值说明：填写非负秒数；`0` 表示立即完成，大于 `0` 表示在对应秒数内完成移动。

### ItemWrapper

- 描述：运行时管理列表项的代码入口。
- 默认行为：初始没有列表项，调用 API 后才创建或移除。
- 配置值说明：通过 `m_View.UiList.ItemWrapper` 访问；列表项类型必须是 `UiControllerBase` 派生类。

## 4. 常用 API

- `ItemWrapper.Count`：获取当前列表项数量。
- `ItemWrapper.AddItem<T>()`：在末尾添加一个 `T` 类型列表项。
- `ItemWrapper.AddItem<T>(int index)`：在指定位置插入一个 `T` 类型列表项。
- `ItemWrapper.GetItem<T>(int index)`：获取指定位置的列表项 Controller。
- `ItemWrapper.RemoveItem(int index)`：移除指定位置列表项。
- `ItemWrapper.ClearItems()`：清空全部列表项。
- `ItemWrapper.ModifyCount<T>(int count)`：把列表项数量修正为指定数量。
- `RefreshLayout()`：重新应用当前排列规则；`ConstantSlot` 和 `AreaFit` 会重新计算所有条目，`Manual` 保留调用方设置的位置。

## 5. 使用示例

```csharp
m_View.SkillList.ItemWrapper.ModifyCount<UiSkillIconController>(5);
var firstIcon = m_View.SkillList.ItemWrapper.GetItem<UiSkillIconController>(0);
```

需要使用业务坐标时，将 `ArragementType` 配置为 `Manual`，添加条目后即可直接定位；后续添加其他条目不会覆盖已经设置的位置：

```csharp
var item = m_View.NodeList.ItemWrapper.AddItem<UpgradeNodeController>();
item.transform.localPosition = nodePosition;
```
