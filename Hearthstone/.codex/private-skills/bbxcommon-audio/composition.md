# 编曲、生成与渲染

项目的本地作曲工具位于 `Assets/Scripts/BbxCommon/AudioTools/`，用于把 UTF-8 `.music` 文本乐谱编译成 MIDI，并以基础预览或 FluidSynth 渲染 WAV。它只在开发期运行，运行时仍由 `AudioApi` 播放最终音频。

开始前读取：

- `Assets/Scripts/BbxCommon/AudioTools/README.md`
- `AutoDoc/Program/音乐作曲工具.md`
- 现有同用途乐谱 `Assets/Scripts/BbxCommon/AudioTools/Compositions/*.music`

## 编曲流程

1. 先确定用途、情绪、BPM、调性、拍号、循环长度和参考风格；同时确定哪些频段要为战斗音效或语音让位。
2. 用 4～8 小节建立和声循环，写一个可辨识的短动机，再安排重复、应答、移调或节奏变奏。不要用无结构的随机音符填满整曲。
3. 分层安排和声、主旋律、低音、节奏与装饰。等待界面控制密度和瞬态；战斗曲提高脉冲、切分和鼓组能量，但保留可读的主旋律。
4. 8bit 风格可用方波、三角波、短琶音和芯片式节奏建立语言，再用 General MIDI 音色、声像和较高采样率增加清晰度。避免所有轨道长期占据同一音区。
5. 后半段至少做一次编配或动机变化；循环末尾要在和声、节奏和混响状态上自然回到开头。
6. 使用 `master` 和各轨 `volume` 控制相对响度，不用削波换取响亮。最终比较同项目其他 BGM 和主要战斗音效的响度关系。

## 乐谱格式

```text
title "Example"
tempo 120
time 4/4
length 64
master 0.8

track "Lead" channel=0 program=80 volume=0.6 pan=0.1 waveform=square
note 0 0.5 D4 88
chord 4 4 D3,F3,A3 68
end
```

- 全局指令：`title`、`tempo`、`time`、可选固定拍数 `length`、大于 0 且不超过 1 的循环总增益 `master`。
- `note`：`开始拍 持续拍 音高 力度`。
- `chord`：同上，多个音高用逗号分隔。
- 音高支持 `C4`、`F#4`、`Bb3` 或 0～127 的 MIDI 编号。
- `channel` 为 0～15；通道 9 用作 General MIDI 鼓组。
- `program` 为 0～127；`volume` 为 0～1；`pan` 为 -1～1。
- 基础预览波形支持 `sine`、`triangle`、`square`、`saw`、`noise`。
- 制作循环曲时声明精确 `length`，所有音符不得越界。

## 生成命令

可以通过 Unity 菜单 `BbxCommon/Audio/Music Composition Tool` 操作，也可以从项目根目录运行：

```powershell
py -3 Assets/Scripts/BbxCommon/AudioTools/music_compiler.py validate --score Assets/Scripts/BbxCommon/AudioTools/Compositions/MyTheme.music

py -3 Assets/Scripts/BbxCommon/AudioTools/music_compiler.py build --score Assets/Scripts/BbxCommon/AudioTools/Compositions/MyTheme.music --midi Assets/Scripts/BbxCommon/AudioTools/GeneratedMidi/MyTheme.mid --loop-wav Assets/Resources/BbxCommon/Audio/GeneratedMusic/MyTheme.wav --fluidsynth Tools/AudioComposition/FluidSynth/bin/fluidsynth.exe --soundfont Tools/AudioComposition/SoundFonts/GeneralUser-GS.sf2 --sample-rate 44100

py -3 Assets/Scripts/BbxCommon/AudioTools/music_compiler.py inspect-midi --midi Assets/Scripts/BbxCommon/AudioTools/GeneratedMidi/MyTheme.mid
```

只需快速检查旋律和结构时，可用 `--preview-wav` 生成标准库基础波形预览；正式交付优先使用项目附带的 FluidSynth 与 `GeneralUser-GS.sf2`。循环曲使用 `--loop-wav`：编译器会渲染三轮、截取中间一轮、整理首尾 5 毫秒并归一化，减少混响状态变化和循环爆音。

如果通过 Python 程序生成大量音符，生成脚本也放在 `AudioTools/`，输出可维护的 `.music`，再交给同一编译器；不要直接拼写二进制 MIDI。现有 `compose_game_bgm.py` 可作为结构化生成示例。

## 输出与接入

- 源乐谱：`Assets/Scripts/BbxCommon/AudioTools/Compositions/MyTheme.music`
- MIDI：`Assets/Scripts/BbxCommon/AudioTools/GeneratedMidi/MyTheme.mid`
- 最终 WAV：`Assets/Resources/BbxCommon/Audio/GeneratedMusic/MyTheme.wav`

三者使用一致且唯一的语义名称，但 MIDI 不进入 Resources。WAV 导入 Unity 后，长循环 BGM 优先采用 Streaming、后台加载和合适的 Vorbis 质量；参考当前项目 BGM 的导入设置，不要机械覆盖用户已有 `.meta`。

播放时使用 basename：

```csharp
var options = AudioPlayOptions.Default;
options.Volume = 0.55f;
options.Loop = true;
options.GroupKey = "BGM";
options.ConcurrencyKey = "MainBgm";
options.MaxConcurrent = 1;
AudioHandle handle = AudioApi.Play("MyTheme", options);
```

完整生命周期规则见 [playback.md](playback.md)。

## 交付检查

- 运行 `validate` 和 `inspect-midi`，确认乐谱与 MIDI 容器有效。
- 核对 WAV 存在、非空，采样率、声道、时长与乐谱 BPM/拍数一致。
- 检查峰值不过载、开头无多余静音、末尾无截断，循环首尾无明显点击或节拍跳变。
- 检查旋律、和声、低音和鼓组不会互相遮蔽；试听至少一个完整循环和跨循环连接。
- 检查 basename 在 Resources 与 Mods 中唯一，更新实际使用方的资源键与播放参数。
- 保留 `Tools/AudioComposition/` 下 FluidSynth、SoundFont 的许可与第三方声明。
