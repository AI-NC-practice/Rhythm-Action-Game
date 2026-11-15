"""데모 곡 조립용 공통 함수 (B10 무료 데모). numpy, scipy만 쓴다."""
import numpy as np
from scipy.io import wavfile
from scipy.signal import stft, resample_poly

SR = 44100


def load(path, sr=SR):
    r, x = wavfile.read(path)
    if x.dtype == np.int16: x = x.astype(np.float64) / 32768
    elif x.dtype == np.int32: x = x.astype(np.float64) / 2147483648
    else: x = x.astype(np.float64)
    if x.ndim == 1: x = np.stack([x, x], axis=1)
    x = x[:, :2]
    if r != sr:
        from fractions import Fraction
        f = Fraction(sr, r).limit_denominator(1000)
        x = np.stack([resample_poly(x[:, c], f.numerator, f.denominator) for c in range(2)], axis=1)
    return x


def save(path, x, sr=SR):
    x = np.clip(x, -1, 1)
    wavfile.write(path, sr, (x * 32767).astype(np.int16))


def onset_env(mono, sr=SR, hop=256):
    _, _, Z = stft(mono, fs=sr, nperseg=1024, noverlap=1024 - hop)
    S = np.log1p(100 * np.abs(Z))
    d = np.maximum(0, np.diff(S, axis=1)).sum(axis=0)
    return d, sr / hop


def onsets(mono, sr=SR, thresh=0.3, min_gap=0.06):
    env, fps = onset_env(mono, sr)
    env = env / (env.max() + 1e-9)
    peaks = []
    for i in range(1, len(env) - 1):
        if env[i] > thresh and env[i] >= env[i - 1] and env[i] > env[i + 1]:
            t = i / fps
            if not peaks or t - peaks[-1][0] > min_gap: peaks.append((t, env[i]))
            elif env[i] > peaks[-1][1]: peaks[-1] = (t, env[i])
    return peaks


def centroid(seg, sr=SR):
    m = seg.mean(axis=1) if seg.ndim > 1 else seg
    S = np.abs(np.fft.rfft(m * np.hanning(len(m))))
    f = np.fft.rfftfreq(len(m), 1 / sr)
    return float((S * f).sum() / (S.sum() + 1e-9))


def f0_track(mono, sr=SR, win=0.04, hop=0.02, fmin=150, fmax=1800, rthresh=0.85):
    W, H = int(win * sr), int(hop * sr)
    out = []
    for i in range(0, len(mono) - W - int(sr / fmin), H):
        seg = mono[i:i + W + int(sr / fmin)]
        e = np.mean(seg[:W] ** 2)
        if e < 1e-5: out.append(np.nan); continue
        best, bl = -1, 0
        a = seg[:W]
        for lag in range(int(sr / fmax), int(sr / fmin)):
            b = seg[lag:lag + W]
            r = np.dot(a, b) / np.sqrt(np.dot(a, a) * np.dot(b, b) + 1e-12)
            if r > best: best, bl = r, lag
        out.append(sr / bl if best > rthresh else np.nan)
    return np.array(out), hop


def wsola(x, rate, sr=SR, win=0.05, tol=0.012):
    """음 높이를 그대로 두고 빠르기를 rate배로 (rate > 1 = 빨라짐, 짧아짐). WSOLA."""
    W = int(win * sr); Hs = W // 2; Ha = int(round(Hs * rate)); T = int(tol * sr)
    win_f = np.hanning(W)[:, None]
    n_out = int(len(x) / rate) + W
    y = np.zeros((n_out, x.shape[1])); norm = np.zeros((n_out, 1))
    pos_in, pos_out, prev = 0, 0, None
    while pos_in + W + T < len(x) and pos_out + W < n_out:
        if prev is None: k = 0
        else:
            best, k = -1e9, 0
            ref = prev
            for d in range(-T, T + 1, 4):
                s = pos_in + d
                if s < 0: continue
                c = np.sum(ref * x[s:s + Hs].mean(axis=1))
                if c > best: best, k = c, d
        s = pos_in + k
        seg = x[s:s + W] * win_f
        y[pos_out:pos_out + W] += seg; norm[pos_out:pos_out + W] += win_f
        prev = x[s + Hs:s + Hs + Hs].mean(axis=1)
        pos_in += Ha; pos_out += Hs
    norm[norm < 1e-3] = 1
    return (y / norm)[:pos_out]


def pitch_shift(x, semitones, sr=SR):
    """음 높이만 semitones만큼 (길이 유지): 재표본화 + WSOLA."""
    if abs(semitones) < 1e-3: return x
    from fractions import Fraction
    r = 2 ** (semitones / 12)
    f = Fraction(1 / r).limit_denominator(2000)
    y = np.stack([resample_poly(x[:, c], f.numerator, f.denominator) for c in range(2)], axis=1)  # 길이 1/r, 높이 r
    return wsola(y, 1 / r, sr)
