---
name: bbxcommon-audio
description: 选择、播放、接入音效或编写游戏音乐时使用。
---

# BbxCommon 音频工作流

处理项目音效或音乐任务时，先按任务类型读取对应说明：

- 新增、修改播放代码或处理音量、循环、叠音、生命周期：读取 [playback.md](playback.md)。
- 搜索、试听、替换、导入音频资源：读取 [selection.md](selection.md)。
- 创作、改编、渲染或接入 BGM：读取 [composition.md](composition.md)，并同时读取 [playback.md](playback.md) 的循环播放部分。
- 同时包含多类工作时，读取全部相关文件，不要只执行其中一段流程。

## 核心约束

1. 运行时业务代码统一通过 `BbxCommon.AudioApi` 播放；只有明确需要取得 `AudioClip` 时才直接调用 `ResourceApi.LoadAudio`，不要另建平行加载或播放体系。
2. 音频键是无目录、无扩展名的文件 basename，例如文件 `BgmCombat8BitClear.wav` 使用键 `BgmCombat8BitClear`。`AudioApi.Play` 会自动异步调用 `ResourceApi.LoadAudio`，统一读取 `Resources/` 与 `Mods/` 的候选文件。
3. 需要唯一资源时，先确保 basename 在受管资源中唯一。发现重名先给实际文件加编号或语义后缀并更新引用，再补程序侧的空值、无效句柄和并发保护；不要用代码掩盖原始重名。
4. 第三方音效库放在 `Assets/Resources/BbxCommon/Audio/Library/`；项目生成音乐放在 `Assets/Resources/BbxCommon/Audio/GeneratedMusic/`。可维护乐谱和 MIDI 不放进 Resources。
5. 高频、循环或长生命周期声音必须明确停止时机、分组及并发策略。场景、Stage、实体或所属对象退出时，停止句柄或整个音频组。
6. 修改前核对项目当前接口和资源目录；若本文与代码不一致，以 `Assets/Scripts/BbxCommon/Api/AudioApi.cs`、`AudioManager/` 和 `ResourceManager/` 的实现为准，并同步修订本 Skill。

## 完成标准

- 资源键可唯一解析，引用已同步，授权和来源可追溯。
- 播放参数符合声音用途，高频声音设有限流或衰减，循环声音可可靠停止。
- 编曲任务保留源乐谱与 MIDI，最终 WAV 位于 `GeneratedMusic/`，且已检查结构、时长、响度与循环点。
- 除非用户明确要求，不进入游戏验证；使用静态检查、编译器校验和音频文件检查完成交付。
