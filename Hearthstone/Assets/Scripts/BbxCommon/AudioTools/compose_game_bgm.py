#!/usr/bin/env python3
"""Create the maintained .music sources for the project's initial two BGM loops."""

from __future__ import annotations

import argparse
from pathlib import Path


def number(value: float) -> str:
    return f"{value:g}"


class ScoreWriter:
    def __init__(
        self,
        title: str,
        tempo: int,
        length_beats: int,
        description: str,
        master_gain: float = 1.0,
    ) -> None:
        self.lines = [
            f"# {description}",
            f'title "{title}"',
            f"tempo {tempo}",
            "time 4/4",
            f"length {length_beats}",
            f"master {number(master_gain)}",
            "",
        ]

    def track(
        self,
        name: str,
        channel: int,
        program: int,
        volume: float,
        pan: float,
        waveform: str,
    ) -> None:
        self.lines.append(
            f'track "{name}" channel={channel} program={program} '
            f"volume={number(volume)} pan={number(pan)} waveform={waveform}"
        )

    def note(self, start: float, duration: float, pitch: str | int, velocity: int) -> None:
        self.lines.append(
            f"note {number(start)} {number(duration)} {pitch} {velocity}"
        )

    def chord(self, start: float, duration: float, pitches: str, velocity: int) -> None:
        self.lines.append(
            f"chord {number(start)} {number(duration)} {pitches} {velocity}"
        )

    def end(self) -> None:
        self.lines.extend(("end", ""))

    def write(self, path: Path) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("\n".join(self.lines).rstrip() + "\n", encoding="utf-8")


def build_waiting_theme(path: Path) -> None:
    score = ScoreWriter(
        "Soft Pixel Harbor",
        92,
        64,
        "等待界面循环BGM：柔和的芯片旋律配合清晰的钟琴、合成铺底与轻节奏。",
        0.72,
    )
    progression = [
        ("C3,E3,G3,B3", "C2", ("C5", "E5", "G5", "E5")),
        ("A2,C3,E3,G3", "A1", ("A4", "C5", "E5", "C5")),
        ("F2,A2,C3,E3", "F1", ("A4", "C5", "E5", "C5")),
        ("G2,B2,D3,A3", "G1", ("B4", "D5", "G5", "D5")),
        ("E3,G3,B3,D4", "E2", ("G4", "B4", "D5", "B4")),
        ("A2,C3,E3,G3", "A1", ("A4", "C5", "E5", "G5")),
        ("D3,F3,A3,C4", "D2", ("A4", "C5", "F5", "C5")),
        ("G2,B2,D3,F3", "G1", ("B4", "D5", "F5", "D5")),
        ("C3,E3,G3,B3", "C2", ("E5", "G5", "B5", "G5")),
        ("B2,D3,G3", "B1", ("D5", "G5", "B5", "G5")),
        ("A2,C3,E3,G3", "A1", ("C5", "E5", "A5", "E5")),
        ("E3,G3,B3,D4", "E2", ("B4", "D5", "G5", "D5")),
        ("F2,A2,C3,E3", "F1", ("A4", "C5", "F5", "E5")),
        ("D3,F3,A3,C4", "D2", ("A4", "C5", "D5", "F5")),
        ("G2,C3,D3,A3", "G1", ("C5", "D5", "G5", "D5")),
        ("G2,B2,D3,F3", "G1", ("B4", "D5", "F5", "B4")),
    ]

    score.track("Warm Pixel Pad", 0, 88, 0.42, 0.12, "triangle")
    for bar, (chord, _, _) in enumerate(progression):
        score.chord(bar * 4, 4, chord, 48 if bar < 8 else 54)
    score.end()

    score.track("Crystal Arpeggio", 1, 8, 0.58, -0.22, "triangle")
    for bar, (_, _, arpeggio) in enumerate(progression):
        base = bar * 4
        for beat, pitch in enumerate(arpeggio):
            score.note(base + beat, 0.72, pitch, 58 + (beat == 0) * 6)
    score.end()

    score.track("Soft Square Motif", 2, 80, 0.34, 0.2, "sine")
    melody = [
        (0, 1.5, "E5", 66), (2, 0.75, "G5", 62), (3, 0.75, "B5", 64),
        (4, 1.5, "A5", 66), (6, 0.75, "G5", 60), (7, 0.75, "E5", 62),
        (8, 1, "C5", 62), (9, 1, "E5", 64), (10, 1.5, "A5", 68),
        (12, 0.75, "G5", 64), (13, 0.75, "D5", 60), (14, 1.5, "B4", 62),
        (16, 1, "G4", 60), (17, 1, "B4", 62), (18, 1.5, "D5", 66),
        (20, 0.75, "E5", 64), (21, 0.75, "C5", 60), (22, 1.5, "A4", 62),
        (24, 1, "A4", 62), (25, 1, "C5", 64), (26, 0.75, "F5", 67),
        (27, 0.75, "E5", 62), (28, 1, "D5", 64), (29, 1, "B4", 60),
        (30, 1.5, "G4", 62),
        (32, 0.75, "E5", 68), (33, 0.75, "G5", 66), (34, 0.75, "B5", 70),
        (35, 0.75, "G5", 64), (36, 1.5, "D5", 66), (38, 1.5, "B4", 62),
        (40, 0.75, "C5", 64), (41, 0.75, "E5", 68), (42, 1.5, "A5", 72),
        (44, 1, "G5", 66), (45, 1, "D5", 62), (46, 1.5, "B4", 64),
        (48, 1, "A4", 62), (49, 1, "C5", 64), (50, 1, "E5", 68),
        (51, 0.75, "G5", 66), (52, 1.5, "F5", 68), (54, 1.5, "D5", 64),
        (56, 0.75, "C5", 64), (57, 0.75, "D5", 66), (58, 0.75, "G5", 70),
        (59, 0.75, "D5", 64), (60, 1, "B4", 62), (61, 1, "D5", 64),
        (62, 0.75, "A4", 60), (63, 0.75, "B4", 64),
    ]
    for note in melody:
        score.note(*note)
    score.end()

    score.track("Rounded Synth Bass", 3, 38, 0.46, 0, "square")
    for bar, (_, root, _) in enumerate(progression):
        base = bar * 4
        score.note(base, 1.65, root, 62)
        score.note(base + 2, 1.45, root, 55)
    score.end()

    score.track("Tiny Star Chimes", 4, 10, 0.32, 0.34, "sine")
    for start, pitch in ((7.5, "E6"), (15.5, "D6"), (23.5, "C6"), (31.5, "B5"),
                         (39.5, "G6"), (47.5, "E6"), (55.5, "F6"), (63.5, "D6")):
        score.note(start, 0.35, pitch, 52)
    score.end()

    score.track("Soft Electronic Pulse", 9, 0, 0.28, 0, "noise")
    for bar in range(16):
        base = bar * 4
        if bar >= 2:
            score.note(base, 0.14, 36, 54)
            score.note(base + 2, 0.12, 37, 46)
        for beat in range(4):
            score.note(base + beat, 0.08, 42, 34 + (beat == 0) * 5)
    score.end()
    score.write(path)


def build_combat_theme(path: Path) -> None:
    score = ScoreWriter(
        "Neon Circuit Assault",
        156,
        64,
        "战斗循环BGM：高速方波主题、锯齿和声、合成贝斯与清晰电子鼓。",
    )
    progression = [
        ("D3,F3,A3", "D2", ("D4", "A4", "D5", "F5", "A5", "F5", "D5", "A4")),
        ("Bb2,D3,F3", "Bb1", ("Bb3", "F4", "Bb4", "D5", "F5", "D5", "Bb4", "F4")),
        ("C3,E3,G3", "C2", ("C4", "G4", "C5", "E5", "G5", "E5", "C5", "G4")),
        ("A2,C#3,E3", "A1", ("A3", "E4", "A4", "C#5", "E5", "C#5", "A4", "E4")),
    ] * 4

    score.track("Clear Saw Chords", 0, 81, 0.34, -0.12, "saw")
    for bar, (chord, _, _) in enumerate(progression):
        score.chord(bar * 4, 3.8, chord, 60 + (bar >= 8) * 5)
    score.end()

    score.track("Pixel Arpeggiator", 1, 80, 0.42, 0.28, "square")
    for bar, (_, _, pattern) in enumerate(progression):
        base = bar * 4
        for step, pitch in enumerate(pattern):
            score.note(base + step * 0.5, 0.38, pitch, 68 + (step in (0, 4)) * 7)
    score.end()

    score.track("Driving Synth Bass", 2, 38, 0.68, -0.05, "square")
    bass_fifths = {"D2": "A2", "Bb1": "F2", "C2": "G2", "A1": "E2"}
    bass_octaves = {"D2": "D3", "Bb1": "Bb2", "C2": "C3", "A1": "A2"}
    for bar, (_, root, _) in enumerate(progression):
        base = bar * 4
        pattern = (root, root, bass_octaves[root], bass_fifths[root], root,
                   bass_octaves[root], bass_fifths[root], bass_octaves[root])
        for step, pitch in enumerate(pattern):
            score.note(base + step * 0.5, 0.42, pitch, 82 + (step in (0, 4)) * 8)
    score.end()

    score.track("Square Assault Lead", 3, 80, 0.62, 0.08, "square")
    bar_melodies = [
        ("D5", "F5", "A5", "D6", "C6", "A5", "F5", "A5"),
        ("Bb5", "A5", "F5", "D5", "F5", "A5", "Bb5", "D6"),
        ("C6", "G5", "E5", "G5", "C6", "E6", "D6", "C6"),
        ("A5", "C#6", "E6", "A6", "G6", "E6", "C#6", "A5"),
        ("D6", "A5", "F5", "D5", "F5", "A5", "C6", "D6"),
        ("F6", "D6", "Bb5", "F5", "D6", "F6", "A6", "F6"),
        ("E6", "D6", "C6", "G5", "C6", "E6", "G6", "E6"),
        ("C#6", "A5", "E5", "A5", "C#6", "E6", "G6", "A6"),
    ]
    for bar in range(16):
        base = bar * 4
        melody = bar_melodies[bar % len(bar_melodies)]
        for step, pitch in enumerate(melody):
            velocity = 88 + (bar >= 8) * 6 + (step in (0, 3, 4)) * 5
            score.note(base + step * 0.5, 0.43, pitch, velocity)
    score.end()

    score.track("Bright Counter Lead", 4, 82, 0.34, -0.32, "triangle")
    counter_notes = ("A4", "F4", "G4", "E4")
    for bar in range(16):
        if bar % 2 == 1:
            score.note(bar * 4, 1.75, counter_notes[bar % 4], 64 + (bar >= 8) * 6)
            score.note(bar * 4 + 2, 1.65, counter_notes[(bar + 1) % 4], 68 + (bar >= 8) * 6)
    score.end()

    score.track("Arcade Battle Drums", 9, 0, 0.64, 0, "noise")
    for bar in range(16):
        base = bar * 4
        if bar % 4 == 0:
            score.note(base, 0.2, 49, 96)
        for step in range(8):
            score.note(base + step * 0.5, 0.08, 42 if step < 7 else 46, 58 + (step % 2 == 0) * 8)
        for offset in (0, 1.5, 2, 3.5):
            score.note(base + offset, 0.16, 36, 88 if offset in (0, 2) else 74)
        for offset in (1, 3):
            score.note(base + offset, 0.16, 38, 92)
    for offset, pitch, velocity in ((62.5, 45, 88), (63, 47, 94), (63.5, 50, 102), (63.75, 49, 110)):
        score.note(offset, 0.12, pitch, velocity)
    score.end()
    score.write(path)


def main() -> int:
    parser = argparse.ArgumentParser(description="生成项目内置等待与战斗BGM乐谱")
    parser.add_argument("--output-directory", type=Path, required=True)
    args = parser.parse_args()
    build_waiting_theme(args.output_directory / "BgmWaiting8BitClear.music")
    build_combat_theme(args.output_directory / "BgmCombat8BitClear.music")
    print(f"已生成两份乐谱：{args.output_directory}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
