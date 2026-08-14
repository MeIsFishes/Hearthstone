# BbxCommon 音乐作曲工具

这套工具用于把文本乐谱编译成标准 MIDI，并可渲染为 WAV。它只在开发期运行，不参与游戏运行时播放。

## 快速使用

1. 在终端进入 `Assets/Scripts/BbxCommon/AudioTools/`。
2. 选择 `.music` 乐谱；仓库内置示例为 `Examples/BattleTheme.music`。
3. 运行下文的 `music_compiler.py build` 命令生成 MIDI 和 WAV。
4. 项目已附带便携版 FluidSynth 和 GeneralUser GS SoundFont，也可替换为其他合成器或音色库。

基础预览合成器只使用 Python 标准库，适合检查旋律、节奏和结构，不代表正式音色。正式音频应使用 FluidSynth + `.sf2`/`.sf3`，或把 MIDI 导入 DAW 后渲染。第三方组件及其许可位于 `Tools/AudioComposition/`。

MIDI 与 WAV 使用不同目录，避免 Resources 索引中出现同名的不同类型资源。生成的 WAV 可直接用不含路径和扩展名的文件名交给 `AudioApi.Play`。

## 乐谱格式

```text
title "Example"
tempo 120
time 4/4
length 16
master 1

track "Piano" channel=0 program=0 volume=0.8 pan=0 waveform=triangle
note 0 1 C4 90
chord 1 2 C4,E4,G4 76
end
```

- `note`：`开始拍 持续拍 音高 力度`。
- `chord`：与 `note` 相同，但音高用逗号分隔。
- 音高支持 `C4`、`F#4`、`Bb3` 或 MIDI 音高编号。
- `channel` 为 0～15；通道 9 按鼓组处理。
- `program` 为 General MIDI 音色编号 0～127。
- `volume` 为 0～1，`pan` 为 -1～1。
- 基础预览波形支持 `sine`、`triangle`、`square`、`saw`、`noise`。
- `length`：可选的固定总拍数；制作循环BGM时用于声明精确循环长度，音符不能越界。
- `master`：循环WAV归一化后的总增益，范围大于0且不超过1；用于保持不同曲目之间的相对响度。

## 命令行

```powershell
py -3 music_compiler.py validate --score Examples/BattleTheme.music
py -3 music_compiler.py build --score Examples/BattleTheme.music --midi BattleTheme.mid --preview-wav BattleTheme.wav
py -3 music_compiler.py inspect-midi --midi BattleTheme.mid
```

使用 FluidSynth 时追加：

```powershell
py -3 music_compiler.py build --score Examples/BattleTheme.music --midi BattleTheme.mid --wav BattleTheme.wav --fluidsynth fluidsynth --soundfont YourSoundFont.sf2
```

制作循环BGM时将 `--wav` 改为 `--loop-wav`。编译器会渲染三轮并截取中间一轮，以保留稳定的合成器与混响状态，同时整理首尾5毫秒并把峰值归一化到约 -2.9 dBFS。
