# 音效选择与资源接入

## 先定义声音角色

搜索文件前先写清楚事件、材质、力度、距离感、期望时长、触发频率、是否循环，以及它在混音中的重要度。名称相似不代表声音合适；射击、蓄力、命中、飞行、引擎、爆炸和提示音要按实际听感区分。

高频短事件优先选择瞬态清晰、尾音短且不过响的素材；循环飞行或引擎声要检查首尾连续性；爆炸和重击可以更厚，但不能遮盖更重要的反馈。需要快速重复的机械节奏时，先选择适合重复的短促木质、塑料、棘轮或低噪机械瞬态，再由程序控制触发节奏，不要仅凭文件名选金属撞击声。

## 搜索范围

1. 优先搜索 `Assets/Resources/BbxCommon/Audio/Library/` 中已有第三方音效。
2. 项目原创或生成音乐位于 `Assets/Resources/BbxCommon/Audio/GeneratedMusic/`，不要混进第三方库目录。
3. 如果任务涉及 Mod 覆盖，再检查 `Mods/`。同名键按 Mod 优先级和 Native 资源顺序解析。
4. 忽略 `.meta`、许可证、Credits、预览文件和非音频资产，但保留授权文件，不要移动或删除。
5. 库内没有合适素材时，如实说明缺口，再按用户授权使用其他来源或生成方案；不要强行复用语义不符的声音。

可用 `rg --files` 按文件名初筛，再用音频工具查看格式、声道、采样率、时长和峰值。若环境允许试听，至少试听最终候选的开头、主体和尾部；循环素材还要检查循环点。

## 候选判断

为每个事件保留少量候选并比较：

- 音色和材质是否对应画面与动作。
- 起音是否能及时反馈，尾音是否与触发频率冲突。
- 响度是否与同层级声音接近，是否需要在播放参数中修正。
- 重复播放是否疲劳，是否需要多个变体或小幅随机音高。
- 是否有噪声、静音头、爆音、过长混响或明显循环接缝。
- 授权是否允许项目使用，来源能否追溯。

最终记录“事件 → AudioClip Key → 初始音量/音高 → 循环/并发策略”，再更新配置或代码，不要只把文件放进目录而不接入。

## 唯一键与重名处理

底层按 basename 建索引，代码中不得传路径或扩展名。导入或选定前检查 `Assets/Resources/` 和相关 `Mods/` 中的重名音频：

```powershell
Get-ChildItem Assets/Resources,Mods -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension -in '.ogg','.wav','.mp3','.aif','.aiff' } |
    Group-Object BaseName |
    Where-Object Count -gt 1 |
    Select-Object Name,Count
```

一个键只应对应一个预期音频。发现非刻意的重名时，先把文件改为带语义或编号后缀的唯一名称，例如 `ImpactWood001`、`ImpactWood002`，同时让 Unity 的 `.meta` 随文件移动并更新所有 CSV、ScriptableObject 与代码引用。完成资源侧修复后，再保留 `null`、无效句柄和并发上限等程序保护。

只有明确设计为 Mod 覆盖时才允许 Resources 与高优先级 Mod 共享键，并在交付说明中记录覆盖关系。不要在 Resources 内依赖同 basename 的路径差异来区分资源。

## 接入位置

- 第三方整包或整包内素材：保持包级目录，放入 `Assets/Resources/BbxCommon/Audio/Library/<PackName>/`，保留许可证与来源说明。
- 项目生成 BGM：放入 `Assets/Resources/BbxCommon/Audio/GeneratedMusic/`。
- 可维护 `.music`：放入 `Assets/Scripts/BbxCommon/AudioTools/Compositions/`。
- 生成 MIDI：放入 `Assets/Scripts/BbxCommon/AudioTools/GeneratedMidi/`，避免与最终 WAV 在 Resources 中产生同名类型冲突。

接入后用 `AudioApi.Play("唯一文件名")` 播放；具体参数、并发和生命周期遵循 [playback.md](playback.md)。
