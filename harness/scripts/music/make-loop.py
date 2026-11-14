"""B10 배경 음악: 국악 BGM #197(공유마당, CC BY)을 게임 박자(120 BPM)에 맞춘 루프 WAV로 만든다.

곡은 펄스 186.07/분, 3펄스마다 강세(12/8 느낌)다. 재생 속도를 180/186.07(약 0.967배)로 늦춰
3펄스 = 게임 2박(1초)이 되게 하고, 첫 강박을 0초에 두고, 12/8 한 마디(12펄스 = 4초 = 게임 2마디)의
정수배 길이로 자른 뒤 끝을 처음에 교차 페이드해 이음매 없이 반복되게 한다.

사용 (프로젝트 루트):
  powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/music/mp3towav.ps1 -In <원본.mp3> -Out <임시.wav>
  python harness/scripts/music/make-loop.py <임시.wav> Assets/Audio/Music/gugak_bgm197_loop120.wav
필요: numpy, scipy
"""
import sys
from fractions import Fraction
import numpy as np
from scipy.io import wavfile
from scipy.signal import resample_poly, stft

HOP = 256
TARGET_PULSE = 180.0          # 3펄스 = 게임 2박 (120 BPM)
LOOP_UNIT_PULSES = 12         # 12/8 한 마디 = 4초 = 게임 2마디
XFADE_SEC = 0.03


def onset_env(sr, mono):
    _, _, Z = stft(mono, fs=sr, nperseg=1024, noverlap=1024 - HOP)
    S = np.log1p(100 * np.abs(Z))
    d = np.maximum(0, np.diff(S, axis=1)).sum(axis=0)
    d = np.maximum(d - np.convolve(d, np.ones(16) / 16, mode='same'), 0)
    return d, sr / HOP


def best_pulse(env, fps, lo, hi):
    n = np.arange(len(env))
    best = (0, 0, 0j)
    for b in np.arange(lo, hi, 0.001):
        c = np.sum(env * np.exp(-1j * 2 * np.pi * b / 60 / fps * n))
        if abs(c) > best[1]: best = (b, abs(c), c)
    b, _, c = best
    w = 2 * np.pi * b / 60 / fps
    t0 = ((-np.angle(c)) % (2 * np.pi)) / w / fps
    return b, t0


def strongest_phase(env, fps, t0, per, k, dur):
    scores = []
    for i in range(k):
        ts = np.arange(t0 + i * per, dur, k * per)
        idx = np.clip((ts * fps).round().astype(int), 0, len(env) - 1)
        scores.append(env[idx].mean())
    return int(np.argmax(scores)), scores


def main(src, out):
    sr, x = wavfile.read(src)
    x = x.astype(np.float64) / 32768.0
    if x.ndim == 1: x = x[:, None]
    mono = x.mean(axis=1)
    env, fps = onset_env(sr, mono / (np.abs(mono).max() + 1e-9))
    dur = len(mono) / sr

    pulse, t0 = best_pulse(env, fps, 185.0, 187.0)
    per = 60.0 / pulse
    k3, s3 = strongest_phase(env, fps, t0, per, 3, dur)            # 점4분 박 (게임 2박)
    beat0 = t0 + k3 * per
    k12, s12 = strongest_phase(env, fps, beat0, 3 * per, 4, dur)   # 12/8 마디 첫 박 (게임 마디 첫 박)
    bar0 = beat0 + k12 * 3 * per
    while bar0 - 12 * per > 0: bar0 -= 12 * per                      # 곡 앞쪽의 가장 이른 마디 첫 박
    print(f"pulse={pulse:.3f}/min t0={t0:.4f}s beat-phase={k3} {np.round(s3, 3)} bar-phase={k12} {np.round(s12, 3)} bar0={bar0:.4f}s")

    ratio = Fraction(pulse / TARGET_PULSE).limit_denominator(20000)   # 새 길이 = 옛 길이 × ratio
    y = np.stack([resample_poly(x[:, c], ratio.numerator, ratio.denominator) for c in range(x.shape[1])], axis=1)
    start = int(round(bar0 * float(ratio) * sr))
    unit = LOOP_UNIT_PULSES * 60.0 / TARGET_PULSE                        # 4초
    xf = int(XFADE_SEC * sr)
    units = int((len(y) - start - xf) / sr // unit)
    n = int(round(units * unit * sr))
    loop = y[start:start + n].copy()
    tail = y[start + n:start + n + xf]
    fade = np.linspace(0, 1, xf)[:, None]
    loop[:xf] = loop[:xf] * fade + tail * (1 - fade)                  # 끝 → 처음 교차 페이드
    peak = np.abs(loop).max()
    if peak > 0.98: loop *= 0.98 / peak
    wavfile.write(out, sr, (loop * 32767).astype(np.int16))
    print(f"speed x{1 / float(ratio):.5f}, loop {units} x {unit:.0f}s = {n / sr:.3f}s ({n / sr * 2:.0f} beats @120), sr={sr}, ch={loop.shape[1]} -> {out}")


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
