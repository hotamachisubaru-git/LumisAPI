"""Regenerate the MIT-licensed sample assets using only Python's standard library."""
from pathlib import Path
import math
import struct
import wave
import zlib

ROOT = Path(__file__).resolve().parent


def chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))


rows = bytearray()
for y in range(64):
    rows.append(0)
    for x in range(64):
        dx, dy = abs(x - 31.5), abs(y - 31.5)
        inside = dx + dy < 28
        core = dx + dy < 12
        rows.extend((235, 255, 248, 255) if core else (109, 226, 214, 255 if inside else 0))
png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 64, 64, 8, 6, 0, 0, 0))
png += chunk(b"IDAT", zlib.compress(rows)) + chunk(b"IEND", b"")
(ROOT / "lumis.png").write_bytes(png)


def write_tone(name, duration, notes):
    rate = 22050
    samples = bytearray()
    for i in range(int(rate * duration)):
        t = i / rate
        note_length = duration / len(notes)
        note_index = min(int(t / note_length), len(notes) - 1)
        local = t - note_index * note_length
        envelope = min(1, local / 0.015) * min(1, (note_length - local) / 0.08)
        value = 0.2 * envelope * math.sin(2 * math.pi * notes[note_index] * t)
        samples.extend(struct.pack("<h", int(32767 * value)))
    with wave.open(str(ROOT / name), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(rate)
        output.writeframes(samples)


write_tone("chime.wav", 0.45, [523.25, 659.25, 783.99])
write_tone("loop.wav", 4.0, [261.63, 329.63, 392, 329.63, 293.66, 349.23, 440, 349.23])
