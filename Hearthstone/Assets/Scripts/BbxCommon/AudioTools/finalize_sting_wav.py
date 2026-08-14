#!/usr/bin/env python3
"""Trim a rendered 16-bit PCM sting, fade its tail, and normalize its peak."""

from __future__ import annotations

import argparse
import math
import sys
import wave
from array import array
from pathlib import Path


def finalize_sting(
    input_path: Path,
    output_path: Path,
    score_seconds: float,
    tail_seconds: float,
    master_gain: float,
) -> None:
    with wave.open(str(input_path), "rb") as source:
        if source.getsampwidth() != 2 or source.getcomptype() != "NONE":
            raise ValueError("Only uncompressed 16-bit PCM WAV input is supported.")
        channel_count = source.getnchannels()
        sample_rate = source.getframerate()
        output_frame_count = round((score_seconds + tail_seconds) * sample_rate)
        if source.getnframes() < output_frame_count:
            raise ValueError("Rendered WAV is shorter than the requested sting duration.")
        samples = array("h")
        samples.frombytes(source.readframes(output_frame_count))
        if sys.byteorder != "little":
            samples.byteswap()

    fade_in_frames = min(round(0.005 * sample_rate), output_frame_count // 8)
    for frame_index in range(fade_in_frames):
        gain = math.sin((frame_index / max(1, fade_in_frames - 1)) * math.pi * 0.5) ** 2
        for channel in range(channel_count):
            sample_index = frame_index * channel_count + channel
            samples[sample_index] = round(samples[sample_index] * gain)

    tail_frame_count = max(1, round(tail_seconds * sample_rate))
    tail_start_frame = output_frame_count - tail_frame_count
    for frame_index in range(tail_start_frame, output_frame_count):
        remaining = (output_frame_count - 1 - frame_index) / max(1, tail_frame_count - 1)
        gain = math.sin(remaining * math.pi * 0.5) ** 2
        for channel in range(channel_count):
            sample_index = frame_index * channel_count + channel
            samples[sample_index] = round(samples[sample_index] * gain)

    peak = max((abs(value) for value in samples), default=0)
    if peak <= 0:
        raise ValueError("Rendered sting is silent.")
    target_peak = round(32767 * 0.72 * master_gain)
    normalization = target_peak / peak
    for index, value in enumerate(samples):
        samples[index] = max(-32767, min(32767, round(value * normalization)))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    if sys.byteorder != "little":
        samples.byteswap()
    with wave.open(str(output_path), "wb") as output:
        output.setnchannels(channel_count)
        output.setsampwidth(2)
        output.setframerate(sample_rate)
        output.writeframes(samples.tobytes())


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--score-seconds", required=True, type=float)
    parser.add_argument("--tail-seconds", default=0.25, type=float)
    parser.add_argument("--master", required=True, type=float)
    args = parser.parse_args()
    if args.score_seconds <= 0 or args.tail_seconds <= 0:
        parser.error("score and tail durations must be positive")
    if not 0 < args.master <= 1:
        parser.error("master must be greater than 0 and at most 1")
    finalize_sting(
        args.input,
        args.output,
        args.score_seconds,
        args.tail_seconds,
        args.master,
    )
    print(f"Finalized sting WAV: {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
