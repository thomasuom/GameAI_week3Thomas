#!/usr/bin/env python3
"""Generate four original PCM sound effects. Uses only Python's standard library."""
import math
import struct
import wave
from pathlib import Path

OUTPUT = Path(__file__).resolve().parents[1] / "Assets" / "Audio" / "Level3"
RATE = 22050
SOUNDS = {
    "Collect": [(880, 0.07), (1174.66, 0.11)],
    "Damage": [(196, 0.08), (146.83, 0.08), (98, 0.12)],
    "Unlock": [(523.25, 0.10), (659.25, 0.10), (783.99, 0.18)],
    "Win": [(523.25, 0.12), (659.25, 0.12), (783.99, 0.12), (1046.5, 0.30)],
}

def generate():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, notes in SOUNDS.items():
        samples = []
        for frequency, duration in notes:
            count = int(duration * RATE)
            for i in range(count):
                t = i / RATE
                envelope = min(1, t / 0.008) * min(1, (duration - t) / 0.045)
                tone = math.sin(2 * math.pi * frequency * t)
                tone += 0.18 * math.sin(4 * math.pi * frequency * t)
                samples.append(int(32767 * 0.25 * envelope * tone))
        with wave.open(str(OUTPUT / (name + ".wav")), "wb") as sound:
            sound.setnchannels(1)
            sound.setsampwidth(2)
            sound.setframerate(RATE)
            sound.writeframes(struct.pack("<" + "h" * len(samples), *samples))
        print(name, len(samples), "samples")

if __name__ == "__main__":
    generate()
