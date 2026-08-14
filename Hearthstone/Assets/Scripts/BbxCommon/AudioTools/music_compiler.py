#!/usr/bin/env python3
"""Compile BbxCommon .music scores to MIDI and render development WAV previews.

The compiler intentionally uses only Python's standard library. FluidSynth and a
SoundFont are optional and are only needed for production-style instrument
rendering; the built-in renderer is a deterministic structural preview.
"""

from __future__ import annotations

import argparse
import math
import os
import random
import shlex
import shutil
import struct
import subprocess
import sys
import tempfile
import wave
from array import array
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable


TICKS_PER_BEAT = 480
DEFAULT_SAMPLE_RATE = 44100
SUPPORTED_WAVEFORMS = {"sine", "triangle", "square", "saw", "noise"}
NOTE_OFF_PRIORITY = 0
CONTROLLER_PRIORITY = 1
NOTE_ON_PRIORITY = 2


class ScoreError(ValueError):
    pass


@dataclass(frozen=True)
class Note:
    start: float
    duration: float
    pitch: int
    velocity: int


@dataclass
class Track:
    name: str
    channel: int = 0
    program: int = 0
    volume: float = 0.8
    pan: float = 0.0
    waveform: str = "sine"
    notes: list[Note] = field(default_factory=list)


@dataclass
class Score:
    title: str = "Untitled"
    tempo: float = 120.0
    numerator: int = 4
    denominator: int = 4
    length_beats: float | None = None
    master_gain: float = 1.0
    tracks: list[Track] = field(default_factory=list)

    @property
    def duration_beats(self) -> float:
        measured_duration = max(
            (note.start + note.duration for track in self.tracks for note in track.notes),
            default=0.0,
        )
        return self.length_beats if self.length_beats is not None else measured_duration

    @property
    def duration_seconds(self) -> float:
        return self.duration_beats * 60.0 / self.tempo


def parse_score(path: Path) -> Score:
    score = Score()
    current_track: Track | None = None

    try:
        lines = path.read_text(encoding="utf-8-sig").splitlines()
    except OSError as exc:
        raise ScoreError(f"无法读取乐谱：{path} ({exc})") from exc

    for line_number, raw_line in enumerate(lines, start=1):
        stripped = raw_line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        try:
            # Do not enable shlex comments: '#' is also the sharp marker in C#4.
            # Comment lines are handled above so pitch tokens remain intact.
            tokens = shlex.split(stripped, comments=False, posix=True)
        except ValueError as exc:
            raise ScoreError(f"第 {line_number} 行引号或转义无效：{exc}") from exc
        if not tokens:
            continue

        command = tokens[0].lower()
        try:
            if command == "title":
                _require_outside_track(current_track, command)
                if len(tokens) < 2:
                    raise ScoreError("title 后需要标题")
                score.title = " ".join(tokens[1:])
            elif command == "tempo":
                _require_outside_track(current_track, command)
                _require_count(tokens, 2, command)
                score.tempo = float(tokens[1])
                if not 20.0 <= score.tempo <= 400.0:
                    raise ScoreError("tempo 必须在 20～400 BPM 之间")
            elif command == "time":
                _require_outside_track(current_track, command)
                _require_count(tokens, 2, command)
                parts = tokens[1].split("/")
                if len(parts) != 2:
                    raise ScoreError("time 必须写成 4/4 形式")
                score.numerator, score.denominator = int(parts[0]), int(parts[1])
                if not 1 <= score.numerator <= 32:
                    raise ScoreError("拍号分子必须在 1～32 之间")
                if score.denominator not in (1, 2, 4, 8, 16, 32):
                    raise ScoreError("拍号分母必须是 1、2、4、8、16 或 32")
            elif command == "length":
                _require_outside_track(current_track, command)
                _require_count(tokens, 2, command)
                score.length_beats = float(tokens[1])
                if score.length_beats <= 0.0:
                    raise ScoreError("length 必须大于 0 拍")
            elif command == "master":
                _require_outside_track(current_track, command)
                _require_count(tokens, 2, command)
                score.master_gain = float(tokens[1])
                if not 0.0 < score.master_gain <= 1.0:
                    raise ScoreError("master 必须大于 0 且不超过 1")
            elif command == "track":
                if current_track is not None:
                    raise ScoreError("开始新轨道前必须先写 end")
                if len(tokens) < 2:
                    raise ScoreError("track 后需要轨道名称")
                current_track = _parse_track(tokens)
                score.tracks.append(current_track)
            elif command in ("note", "chord"):
                if current_track is None:
                    raise ScoreError(f"{command} 必须写在 track 与 end 之间")
                _parse_notes(current_track, tokens, command)
            elif command == "end":
                _require_count(tokens, 1, command)
                if current_track is None:
                    raise ScoreError("end 前没有打开的 track")
                current_track = None
            else:
                raise ScoreError(f"未知指令：{tokens[0]}")
        except (ScoreError, ValueError) as exc:
            if isinstance(exc, ScoreError):
                message = str(exc)
            else:
                message = f"数值格式无效：{exc}"
            raise ScoreError(f"{path.name} 第 {line_number} 行：{message}") from exc

    if current_track is not None:
        raise ScoreError(f"轨道“{current_track.name}”缺少 end")
    if not score.tracks:
        raise ScoreError("乐谱至少需要一个 track")
    if not any(track.notes for track in score.tracks):
        raise ScoreError("乐谱中没有音符")
    measured_duration = max(
        note.start + note.duration for track in score.tracks for note in track.notes
    )
    if score.length_beats is not None and measured_duration > score.length_beats + 0.000001:
        raise ScoreError(
            f"音符结束于第 {measured_duration:g} 拍，超出 length {score.length_beats:g}"
        )
    return score


def _parse_track(tokens: list[str]) -> Track:
    options: dict[str, str] = {}
    for token in tokens[2:]:
        if "=" not in token:
            raise ScoreError(f"轨道参数必须写成 key=value：{token}")
        key, value = token.split("=", 1)
        options[key.lower()] = value

    allowed = {"channel", "program", "volume", "pan", "waveform"}
    unknown = set(options).difference(allowed)
    if unknown:
        raise ScoreError(f"未知轨道参数：{', '.join(sorted(unknown))}")

    track = Track(
        name=tokens[1],
        channel=int(options.get("channel", "0")),
        program=int(options.get("program", "0")),
        volume=float(options.get("volume", "0.8")),
        pan=float(options.get("pan", "0")),
        waveform=options.get("waveform", "sine").lower(),
    )
    if not 0 <= track.channel <= 15:
        raise ScoreError("channel 必须在 0～15 之间")
    if not 0 <= track.program <= 127:
        raise ScoreError("program 必须在 0～127 之间")
    if not 0.0 <= track.volume <= 1.0:
        raise ScoreError("volume 必须在 0～1 之间")
    if not -1.0 <= track.pan <= 1.0:
        raise ScoreError("pan 必须在 -1～1 之间")
    if track.waveform not in SUPPORTED_WAVEFORMS:
        raise ScoreError(f"waveform 必须是：{', '.join(sorted(SUPPORTED_WAVEFORMS))}")
    return track


def _parse_notes(track: Track, tokens: list[str], command: str) -> None:
    _require_count(tokens, 5, command)
    start = float(tokens[1])
    duration = float(tokens[2])
    velocity = int(tokens[4])
    if start < 0.0:
        raise ScoreError("开始拍不能小于 0")
    if duration <= 0.0:
        raise ScoreError("持续拍必须大于 0")
    if not 1 <= velocity <= 127:
        raise ScoreError("力度必须在 1～127 之间")

    pitch_tokens = tokens[3].split(",") if command == "chord" else [tokens[3]]
    if command == "note" and len(pitch_tokens) != 1:
        raise ScoreError("多个音高请使用 chord 指令")
    for pitch_token in pitch_tokens:
        track.notes.append(Note(start, duration, parse_pitch(pitch_token), velocity))


def parse_pitch(token: str) -> int:
    try:
        value = int(token)
        if not 0 <= value <= 127:
            raise ScoreError("MIDI 音高编号必须在 0～127 之间")
        return value
    except ValueError:
        pass

    token = token.strip()
    if len(token) < 2:
        raise ScoreError(f"无效音高：{token}")
    letter = token[0].upper()
    semitone_by_letter = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}
    if letter not in semitone_by_letter:
        raise ScoreError(f"无效音高：{token}")

    offset = 1
    accidental = 0
    if len(token) > 1 and token[1] in ("#", "b"):
        accidental = 1 if token[1] == "#" else -1
        offset = 2
    try:
        octave = int(token[offset:])
    except ValueError as exc:
        raise ScoreError(f"无效音高：{token}") from exc
    value = (octave + 1) * 12 + semitone_by_letter[letter] + accidental
    if not 0 <= value <= 127:
        raise ScoreError(f"音高超出 MIDI 范围：{token}")
    return value


def _require_count(tokens: list[str], count: int, command: str) -> None:
    if len(tokens) != count:
        raise ScoreError(f"{command} 需要 {count - 1} 个参数，实际为 {len(tokens) - 1} 个")


def _require_outside_track(current_track: Track | None, command: str) -> None:
    if current_track is not None:
        raise ScoreError(f"{command} 不能写在 track 内")


def compile_midi(score: Score, output_path: Path) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    tracks = [_build_conductor_track(score)]
    tracks.extend(_build_midi_track(track) for track in score.tracks)
    header = b"MThd" + struct.pack(">IHHH", 6, 1, len(tracks), TICKS_PER_BEAT)
    payload = header + b"".join(b"MTrk" + struct.pack(">I", len(track)) + track for track in tracks)
    output_path.write_bytes(payload)
    inspect_midi(output_path)


def _build_conductor_track(score: Score) -> bytes:
    tempo_microseconds = round(60_000_000 / score.tempo)
    denominator_power = int(math.log2(score.denominator))
    title = score.title.encode("utf-8")
    events = [
        (0, 0, b"\xff\x03" + encode_vlq(len(title)) + title),
        (0, 1, b"\xff\x51\x03" + tempo_microseconds.to_bytes(3, "big")),
        (0, 2, bytes((0xFF, 0x58, 0x04, score.numerator, denominator_power, 24, 8))),
    ]
    return _serialize_events(events)


def _build_midi_track(track: Track) -> bytes:
    name = track.name.encode("utf-8")
    channel = track.channel
    events: list[tuple[int, int, bytes]] = [
        (0, 0, b"\xff\x03" + encode_vlq(len(name)) + name),
        (0, CONTROLLER_PRIORITY, bytes((0xB0 | channel, 7, round(track.volume * 127)))),
        (0, CONTROLLER_PRIORITY, bytes((0xB0 | channel, 10, round((track.pan + 1.0) * 63.5)))),
    ]
    if channel != 9:
        events.append((0, CONTROLLER_PRIORITY, bytes((0xC0 | channel, track.program))))

    for note in track.notes:
        start_tick = round(note.start * TICKS_PER_BEAT)
        end_tick = round((note.start + note.duration) * TICKS_PER_BEAT)
        events.append((start_tick, NOTE_ON_PRIORITY, bytes((0x90 | channel, note.pitch, note.velocity))))
        events.append((end_tick, NOTE_OFF_PRIORITY, bytes((0x80 | channel, note.pitch, 0))))
    return _serialize_events(events)


def _serialize_events(events: Iterable[tuple[int, int, bytes]]) -> bytes:
    ordered = sorted(events, key=lambda event: (event[0], event[1]))
    output = bytearray()
    previous_tick = 0
    for tick, _, payload in ordered:
        output.extend(encode_vlq(tick - previous_tick))
        output.extend(payload)
        previous_tick = tick
    output.extend(b"\x00\xff\x2f\x00")
    return bytes(output)


def encode_vlq(value: int) -> bytes:
    if value < 0:
        raise ValueError("VLQ 不能编码负数")
    buffer = value & 0x7F
    result = bytearray((buffer,))
    while value > 0x7F:
        value >>= 7
        buffer = (value & 0x7F) | 0x80
        result.insert(0, buffer)
    return bytes(result)


def inspect_midi(path: Path) -> dict[str, int]:
    data = path.read_bytes()
    if len(data) < 14 or data[:4] != b"MThd":
        raise ScoreError(f"不是有效的 MIDI 文件：{path}")
    header_size, midi_format, track_count, division = struct.unpack(">IHHH", data[4:14])
    if header_size != 6 or midi_format not in (0, 1) or division <= 0:
        raise ScoreError(f"MIDI 头无效：{path}")

    offset = 8 + header_size
    found_tracks = 0
    while offset < len(data):
        if offset + 8 > len(data) or data[offset:offset + 4] != b"MTrk":
            raise ScoreError(f"MIDI 轨道块无效，偏移 {offset}")
        length = struct.unpack(">I", data[offset + 4:offset + 8])[0]
        offset += 8 + length
        if offset > len(data):
            raise ScoreError("MIDI 轨道长度超出文件范围")
        found_tracks += 1
    if offset != len(data) or found_tracks != track_count:
        raise ScoreError(f"MIDI 轨道数不一致：头部 {track_count}，实际 {found_tracks}")
    return {"format": midi_format, "tracks": track_count, "ticks_per_beat": division, "bytes": len(data)}


def render_preview(score: Score, output_path: Path, sample_rate: int = DEFAULT_SAMPLE_RATE) -> None:
    if not 8000 <= sample_rate <= 192000:
        raise ScoreError("采样率必须在 8000～192000 Hz 之间")
    tail_seconds = 0.2
    total_samples = max(1, math.ceil((score.duration_seconds + tail_seconds) * sample_rate))
    left = array("f", [0.0]) * total_samples
    right = array("f", [0.0]) * total_samples
    seconds_per_beat = 60.0 / score.tempo

    for track_index, track in enumerate(score.tracks):
        pan_angle = (track.pan + 1.0) * math.pi / 4.0
        left_gain = math.cos(pan_angle) * track.volume
        right_gain = math.sin(pan_angle) * track.volume
        for note_index, note in enumerate(track.notes):
            start_sample = round(note.start * seconds_per_beat * sample_rate)
            duration_seconds = note.duration * seconds_per_beat
            duration_samples = max(1, round(duration_seconds * sample_rate))
            amplitude = (note.velocity / 127.0) * 0.18
            random_seed = (track_index + 1) * 1_000_003 + note_index * 1009 + note.pitch
            _mix_note(
                left,
                right,
                start_sample,
                duration_samples,
                note.pitch,
                amplitude,
                left_gain,
                right_gain,
                track.waveform,
                track.channel == 9,
                sample_rate,
                random_seed,
            )

    peak = max(max((abs(value) for value in left), default=0.0), max((abs(value) for value in right), default=0.0))
    normalization = min(1.0, 0.92 / peak) if peak > 0.0 else 1.0
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(output_path), "wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(sample_rate)
        chunk_size = 65536
        for offset in range(0, total_samples, chunk_size):
            end = min(total_samples, offset + chunk_size)
            pcm = array("h")
            for index in range(offset, end):
                pcm.append(round(max(-1.0, min(1.0, left[index] * normalization)) * 32767))
                pcm.append(round(max(-1.0, min(1.0, right[index] * normalization)) * 32767))
            if sys.byteorder != "little":
                pcm.byteswap()
            output.writeframes(pcm.tobytes())


def _mix_note(
    left: array,
    right: array,
    start_sample: int,
    duration_samples: int,
    pitch: int,
    amplitude: float,
    left_gain: float,
    right_gain: float,
    waveform: str,
    is_drum: bool,
    sample_rate: int,
    random_seed: int,
) -> None:
    release_samples = min(round(0.08 * sample_rate), duration_samples)
    render_samples = duration_samples + release_samples
    attack_samples = max(1, min(round(0.012 * sample_rate), duration_samples // 3 or 1))
    frequency = 440.0 * (2.0 ** ((pitch - 69) / 12.0))
    phase_step = frequency / sample_rate
    rng = random.Random(random_seed)

    for local_index in range(render_samples):
        target_index = start_sample + local_index
        if target_index >= len(left):
            break
        if local_index < attack_samples:
            envelope = local_index / attack_samples
        elif local_index < duration_samples:
            envelope = 1.0
        else:
            envelope = 1.0 - (local_index - duration_samples) / max(1, release_samples)

        if is_drum:
            decay = math.exp(-7.0 * local_index / max(1, render_samples))
            if pitch in (35, 36):
                phase = 2.0 * math.pi * (55.0 + 45.0 * decay) * local_index / sample_rate
                sample = math.sin(phase) * decay
            else:
                sample = (rng.random() * 2.0 - 1.0) * decay
        else:
            phase = (local_index * phase_step) % 1.0
            sample = _wave_sample(waveform, phase, rng)
        value = sample * envelope * amplitude
        left[target_index] += value * left_gain
        right[target_index] += value * right_gain


def _wave_sample(waveform: str, phase: float, rng: random.Random) -> float:
    if waveform == "sine":
        return math.sin(phase * 2.0 * math.pi)
    if waveform == "triangle":
        return 1.0 - 4.0 * abs(phase - 0.5)
    if waveform == "square":
        return 1.0 if phase < 0.5 else -1.0
    if waveform == "saw":
        return phase * 2.0 - 1.0
    return rng.random() * 2.0 - 1.0


def render_with_fluidsynth(
    executable: str,
    soundfont: Path,
    midi_path: Path,
    output_path: Path,
    sample_rate: int,
) -> None:
    resolved_executable = shutil.which(executable) if not Path(executable).is_file() else executable
    if not resolved_executable:
        raise ScoreError(f"找不到 FluidSynth：{executable}")
    if not soundfont.is_file() or soundfont.suffix.lower() not in (".sf2", ".sf3"):
        raise ScoreError(f"SoundFont 不存在或扩展名不是 .sf2/.sf3：{soundfont}")
    output_path.parent.mkdir(parents=True, exist_ok=True)
    command = [
        str(resolved_executable),
        "-ni",
        "-F",
        str(output_path),
        "-r",
        str(sample_rate),
        str(soundfont),
        str(midi_path),
    ]
    result = subprocess.run(command, capture_output=True, text=True, check=False)
    if result.returncode != 0:
        details = (result.stderr or result.stdout).strip()
        raise ScoreError(f"FluidSynth 渲染失败（退出码 {result.returncode}）：{details}")
    if not output_path.is_file() or output_path.stat().st_size <= 44:
        raise ScoreError("FluidSynth 未生成有效 WAV")


def render_loop_with_fluidsynth(
    score: Score,
    executable: str,
    soundfont: Path,
    output_path: Path,
    sample_rate: int,
) -> None:
    """Render three cycles and extract the middle one for a stable BGM loop."""
    repeated_score = repeat_score(score, 3)
    with tempfile.TemporaryDirectory(prefix="bbx_music_loop_") as temporary_directory:
        temporary_root = Path(temporary_directory)
        repeated_midi = temporary_root / "repeated.mid"
        repeated_wav = temporary_root / "repeated.wav"
        compile_midi(repeated_score, repeated_midi)
        render_with_fluidsynth(
            executable,
            soundfont,
            repeated_midi,
            repeated_wav,
            sample_rate,
        )
        _extract_normalized_loop(
            repeated_wav,
            output_path,
            score.duration_seconds,
            score.duration_seconds,
            score.master_gain,
        )


def repeat_score(score: Score, repeat_count: int) -> Score:
    if repeat_count <= 0:
        raise ScoreError("乐谱重复次数必须大于 0")
    cycle_beats = score.duration_beats
    if cycle_beats <= 0.0:
        raise ScoreError("无法重复长度为 0 的乐谱")
    repeated = Score(
        title=f"{score.title} x{repeat_count}",
        tempo=score.tempo,
        numerator=score.numerator,
        denominator=score.denominator,
        length_beats=cycle_beats * repeat_count,
        master_gain=score.master_gain,
    )
    for source_track in score.tracks:
        target_track = Track(
            name=source_track.name,
            channel=source_track.channel,
            program=source_track.program,
            volume=source_track.volume,
            pan=source_track.pan,
            waveform=source_track.waveform,
        )
        for repeat_index in range(repeat_count):
            offset = repeat_index * cycle_beats
            target_track.notes.extend(
                Note(note.start + offset, note.duration, note.pitch, note.velocity)
                for note in source_track.notes
            )
        repeated.tracks.append(target_track)
    return repeated


def _extract_normalized_loop(
    source_path: Path,
    output_path: Path,
    start_seconds: float,
    duration_seconds: float,
    master_gain: float,
) -> None:
    with wave.open(str(source_path), "rb") as source:
        if source.getsampwidth() != 2 or source.getcomptype() != "NONE":
            raise ScoreError("循环WAV整理只支持16-bit PCM")
        sample_rate = source.getframerate()
        channel_count = source.getnchannels()
        start_frame = round(start_seconds * sample_rate)
        frame_count = round(duration_seconds * sample_rate)
        if start_frame < 0 or frame_count <= 0 or start_frame + frame_count > source.getnframes():
            raise ScoreError("FluidSynth WAV 长度不足，无法提取完整循环段")
        source.setpos(start_frame)
        samples = array("h")
        samples.frombytes(source.readframes(frame_count))
        if sys.byteorder != "little":
            samples.byteswap()

    _fade_loop_edges(samples, channel_count, sample_rate)
    peak = max((abs(value) for value in samples), default=0)
    if peak <= 0:
        raise ScoreError("循环WAV为静音")
    target_peak = round(32767 * 0.72 * master_gain)
    gain = target_peak / peak
    for index, value in enumerate(samples):
        samples[index] = max(-32767, min(32767, round(value * gain)))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    if sys.byteorder != "little":
        samples.byteswap()
    with wave.open(str(output_path), "wb") as output:
        output.setnchannels(channel_count)
        output.setsampwidth(2)
        output.setframerate(sample_rate)
        output.writeframes(samples.tobytes())


def _fade_loop_edges(samples: array, channel_count: int, sample_rate: int) -> None:
    frame_count = len(samples) // channel_count
    fade_frames = min(round(sample_rate * 0.005), frame_count // 8)
    if fade_frames <= 1:
        return
    for frame_index in range(fade_frames):
        fade_in = math.sin((frame_index / (fade_frames - 1)) * math.pi * 0.5) ** 2
        fade_out = math.sin(((fade_frames - 1 - frame_index) / (fade_frames - 1)) * math.pi * 0.5) ** 2
        ending_frame = frame_count - fade_frames + frame_index
        for channel in range(channel_count):
            samples[frame_index * channel_count + channel] = round(
                samples[frame_index * channel_count + channel] * fade_in
            )
            ending_index = ending_frame * channel_count + channel
            samples[ending_index] = round(samples[ending_index] * fade_out)


def print_score_summary(score: Score) -> None:
    note_count = sum(len(track.notes) for track in score.tracks)
    print(f"标题：{score.title}")
    print(f"速度：{score.tempo:g} BPM；拍号：{score.numerator}/{score.denominator}")
    print(f"轨道：{len(score.tracks)}；音符：{note_count}")
    print(f"长度：{score.duration_beats:g} 拍 / {score.duration_seconds:.2f} 秒")


def create_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="BbxCommon 文本乐谱编译器")
    subparsers = parser.add_subparsers(dest="command", required=True)

    validate = subparsers.add_parser("validate", help="校验乐谱")
    validate.add_argument("--score", type=Path, required=True)

    build = subparsers.add_parser("build", help="生成 MIDI，并可渲染 WAV")
    build.add_argument("--score", type=Path, required=True)
    build.add_argument("--midi", type=Path, required=True)
    build.add_argument("--preview-wav", type=Path)
    build.add_argument("--wav", type=Path)
    build.add_argument("--loop-wav", type=Path)
    build.add_argument("--fluidsynth", default="fluidsynth")
    build.add_argument("--soundfont", type=Path)
    build.add_argument("--sample-rate", type=int, default=DEFAULT_SAMPLE_RATE)

    inspect = subparsers.add_parser("inspect-midi", help="检查 MIDI 容器结构")
    inspect.add_argument("--midi", type=Path, required=True)
    return parser


def main() -> int:
    args = create_parser().parse_args()
    try:
        if args.command == "validate":
            score = parse_score(args.score)
            print_score_summary(score)
            print("乐谱校验通过。")
            return 0
        if args.command == "inspect-midi":
            result = inspect_midi(args.midi)
            print(
                f"MIDI 检查通过：format={result['format']}，tracks={result['tracks']}，"
                f"ticks/beat={result['ticks_per_beat']}，bytes={result['bytes']}"
            )
            return 0

        score = parse_score(args.score)
        compile_midi(score, args.midi)
        print_score_summary(score)
        print(f"已生成 MIDI：{args.midi}")
        if args.preview_wav:
            render_preview(score, args.preview_wav, args.sample_rate)
            print(f"已生成基础预览 WAV：{args.preview_wav}")
        if args.wav and args.loop_wav:
            raise ScoreError("--wav 与 --loop-wav 不能同时使用")
        if args.wav or args.loop_wav:
            if not args.soundfont:
                raise ScoreError("使用 FluidSynth 渲染时必须传入 --soundfont")
            if args.loop_wav:
                render_loop_with_fluidsynth(
                    score,
                    args.fluidsynth,
                    args.soundfont,
                    args.loop_wav,
                    args.sample_rate,
                )
                print(f"已生成循环 FluidSynth WAV：{args.loop_wav}")
            else:
                render_with_fluidsynth(
                    args.fluidsynth,
                    args.soundfont,
                    args.midi,
                    args.wav,
                    args.sample_rate,
                )
                print(f"已生成 FluidSynth WAV：{args.wav}")
        return 0
    except (OSError, ScoreError) as exc:
        print(f"错误：{exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
