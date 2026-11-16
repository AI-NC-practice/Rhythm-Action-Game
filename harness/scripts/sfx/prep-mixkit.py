"""Mixkit 원본 효과음(harness/sfx-src/mixkit/<번호>.wav)을 게임용으로 다듬는다 (작업 B48, GDD-beta B17.5).

- 모노로 섞고, 소리 시작(최고값의 5%) 3 ms 전부터 자르고, 최대 길이에서 끝을 30 ms 페이드, 최고값 0.9로 맞춘다.
- 결과: Assets/Audio/Sfx/Rec_<종류>.wav (16 bit, 44.1 kHz, 모노). SfxSynthTool이 있으면 합성음 대신 쓴다.
- 파이썬 표준 라이브러리만 쓴다 (PCM 16 bit, WAVE_FORMAT_EXTENSIBLE 24 bit 읽기).

사용: python -I harness/scripts/sfx/prep-mixkit.py harness/sfx-src/mixkit Assets/Audio/Sfx
"""
import os
import struct
import sys

# 종류 이름(SfxKind) : (Mixkit 번호, 최대 길이 초)
TABLE = {
    "SwordWhoosh": (2792, 0.6),   # Fast sword whoosh — 적 휘두름 (판정 반 박 전)
    "SwordSwish": (1506, 0.6),    # Sword blade swish — 내 공격 휘두름
    "ParryClang": (2789, 1.0),    # Samurai Sword Impact — 막기 PERFECT "챙"
    "BlockClang": (2795, 0.8),    # Heavy sword smashes metal — 막기 NORMAL
    "Slash": (2788, 0.8),         # Sword cutting flesh — 베임·처형
    "Punch": (2153, 0.5),         # Body punch quick hit — 맞음 "퍽"
    "Kick": (2163, 0.6),          # Martial arts kick — 발차기
    "BodyFall": (2498, 1.0),      # Body impact falling into the sand — 쓰러짐
}
RATE = 44100


def read_wav(path):
    data = open(path, "rb").read()
    if data[0:4] != b"RIFF" or data[8:12] != b"WAVE":
        raise ValueError("WAV 아님: " + path)
    pos, fmt, raw = 12, None, None
    while pos + 8 <= len(data):
        cid, size = data[pos:pos + 4], struct.unpack("<I", data[pos + 4:pos + 8])[0]
        body = data[pos + 8:pos + 8 + size]
        if cid == b"fmt ":
            tag, ch, rate, _, _, bits = struct.unpack("<HHIIHH", body[:16])
            if tag == 0xFFFE:
                tag = struct.unpack("<H", body[24:26])[0]   # 확장 형식의 실제 형식
            fmt = (tag, ch, rate, bits)
        elif cid == b"data":
            raw = body
        pos += 8 + size + (size & 1)
    tag, ch, rate, bits = fmt
    if tag != 1 or bits not in (16, 24):
        raise ValueError(f"지원하지 않는 형식 {fmt}: {path}")
    if rate != RATE:
        raise ValueError(f"샘플레이트 {rate}: {path}")
    width = bits // 8
    frames = len(raw) // (width * ch)
    out = []
    for i in range(frames):
        s = 0.0
        for c in range(ch):
            o = (i * ch + c) * width
            if width == 2:
                v = struct.unpack("<h", raw[o:o + 2])[0] / 32768.0
            else:
                b = raw[o:o + 3]
                v = int.from_bytes(b, "little", signed=True) / 8388608.0
            s += v
        out.append(s / ch)
    return out


def prep(samples, max_seconds):
    peak = max(abs(v) for v in samples) or 1.0
    start = next(i for i, v in enumerate(samples) if abs(v) >= peak * 0.05)
    start = max(0, start - int(0.003 * RATE))
    end = min(len(samples), start + int(max_seconds * RATE))
    s = samples[start:end]
    fade = int(0.03 * RATE)
    for i in range(min(fade, len(s))):
        s[len(s) - 1 - i] *= i / fade
    p = max(abs(v) for v in s) or 1.0
    return [v * 0.9 / p for v in s], start / RATE


def write_wav(path, s):
    pcm = b"".join(struct.pack("<h", max(-32767, min(32767, int(round(v * 32767))))) for v in s)
    hdr = b"RIFF" + struct.pack("<I", 36 + len(pcm)) + b"WAVE"
    hdr += b"fmt " + struct.pack("<IHHIIHH", 16, 1, 1, RATE, RATE * 2, 2, 16)
    hdr += b"data" + struct.pack("<I", len(pcm))
    open(path, "wb").write(hdr + pcm)


def main(src, dst):
    os.makedirs(dst, exist_ok=True)
    for kind, (num, max_s) in TABLE.items():
        s, cut = prep(read_wav(os.path.join(src, f"{num}.wav")), max_s)
        out = os.path.join(dst, f"Rec_{kind}.wav")
        write_wav(out, s)
        print(f"{kind:12s} <- {num}.wav  앞 {cut * 1000:5.0f} ms 자름, 길이 {len(s) / RATE:.2f} s -> {out}")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
