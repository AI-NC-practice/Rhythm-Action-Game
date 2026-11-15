"""B10 퓨전 국악 곡 조립 도구 (make-demo.py, make-song.py가 같이 쓴다). 120 BPM, B♭ 계면조.

재료 (harness/music-src/demo/, 공유마당):
  태평소독주_시그널음악 027·038·042·046·058·059 (CC BY), 국악기_꽹과리10 (CC BY),
  전통악기_장구_치다_보통속도_Ver.1 (공공누리 1유형, mp3 → wav로 바꿔 넘김)
추임새: harness/music-src/chuimsae/<이름>.wav 가 있으면 넣고, 없으면 악보(.score.txt)에 자리만 표시한다.
드럼·베이스·현악·징은 코드로 합성한다. 태평소는 90 BPM 녹음을 음 높이 그대로 120 BPM으로 줄인다(WSOLA).

쓰는 법: init(마디 수, 장구 wav) → 악보 함수(groove, drop, roll, heartbeat, put_tps …)로 쌓기 → render(출력 wav).
"""
import os
import numpy as np
from scipy.signal import fftconvolve, butter, sosfilt
from demo_lib import SR, load, save, wsola

BPM = 120.0
BEAT = 60.0 / BPM
SRC = os.path.join(os.path.dirname(__file__), '..', '..', 'music-src', 'demo')
CH_DIR = os.path.join(os.path.dirname(__file__), '..', '..', 'music-src', 'chuimsae')
BUSES = ['drums', 'janggu', 'kkw', 'bass', 'pad', 'tps', 'voice', 'jing']
rng = np.random.default_rng(7)

TOTAL = 0
BARS = 0
mix = {}
score = []
kicks = []


def bar_start(b): return b * 4


def put(bus, sample, beat, gain=1.0, pan=0.0):
    a = int(round(beat * BEAT * SR))
    if a >= TOTAL or gain == 0: return
    s = sample if sample.ndim == 2 else np.stack([sample, sample], axis=1)
    n = min(len(s), TOTAL - a)
    l, r = np.cos((pan + 1) * np.pi / 4), np.sin((pan + 1) * np.pi / 4)
    mix[bus][a:a + n, 0] += s[:n, 0] * gain * l * 1.414
    mix[bus][a:a + n, 1] += s[:n, 1] * gain * r * 1.414


def hit(bus, sample, beat, gain=1.0, pan=0.0, label=None):
    put(bus, sample, beat, gain, pan)
    if label: score.append((beat, label))


# ---------------- 합성 악기 ----------------
def t_(sec): return np.arange(int(sec * SR)) / SR

def kick():
    t = t_(0.35); f = 45 + 85 * np.exp(-t / 0.04)
    ph = 2 * np.pi * np.cumsum(f) / SR
    return np.tanh(2.2 * np.exp(-t / 0.18) * np.sin(ph)) * 0.9 + np.exp(-t / 0.003) * rng.uniform(-1, 1, len(t)) * 0.3

def snare():
    t = t_(0.25)
    n = sosfilt(butter(2, [1500, 7000], 'band', fs=SR, output='sos'), rng.uniform(-1, 1, len(t)))
    return n * np.exp(-t / 0.07) * 2.0 + 0.5 * np.exp(-t / 0.05) * np.sin(2 * np.pi * 190 * t)

def hat(open_=False):
    t = t_(0.25 if open_ else 0.06)
    n = sosfilt(butter(4, 7000, 'high', fs=SR, output='sos'), rng.uniform(-1, 1, len(t)))
    return n * np.exp(-t / (0.09 if open_ else 0.018)) * 0.8

def crash():
    t = t_(1.6)
    n = sosfilt(butter(4, 4500, 'high', fs=SR, output='sos'), rng.uniform(-1, 1, len(t)))
    return n * np.exp(-t / 0.5) * 0.9

def clap():
    t = t_(0.2); n = sosfilt(butter(2, [900, 5000], 'band', fs=SR, output='sos'), rng.uniform(-1, 1, len(t)))
    env = np.zeros_like(t)
    for d in (0, 0.011, 0.023): env += np.exp(-np.maximum(t - d, 0) / 0.012) * (t >= d)
    env += np.exp(-np.maximum(t - 0.03, 0) / 0.08) * (t >= 0.03)
    return n * env * 0.8

def big_drum():
    t = t_(0.9); f = 38 + 50 * np.exp(-t / 0.06)
    return np.tanh(2.5 * np.exp(-t / 0.35) * np.sin(2 * np.pi * np.cumsum(f) / SR))

def riser(beats):
    t = t_(beats * BEAT); u = t / t[-1]
    n = rng.uniform(-1, 1, len(t)); y = np.zeros_like(n); lp = 0.0
    a = 0.01 + 0.5 * u ** 2
    for i in range(len(n)): lp += a[i] * (n[i] - lp); y[i] = lp
    return y * (u ** 1.5) * 2.5

def jing():
    t = t_(3.5); w = 1 + 0.004 * np.sin(2 * np.pi * 4 * t)
    s = sum(a * np.exp(-t / d) * np.sin(2 * np.pi * 116.5 * r * t * w) for r, d, a in [(1, 2.2, 1), (1.52, 1.4, .6), (2.4, .8, .35), (3.3, .5, .2)])
    return s * np.minimum(1, t / 0.015) * 0.7

def midi_hz(m): return 440.0 * 2 ** ((m - 69) / 12)

def bass_note(m, dur):
    t = t_(dur); f = midi_hz(m)
    saw = 2 * ((t * f) % 1) - 1
    sub = np.sin(2 * np.pi * f * t)
    s = sosfilt(butter(2, 600, 'low', fs=SR, output='sos'), saw) * 0.8 + sub * 0.7
    env = np.minimum(1, t / 0.005) * np.exp(-t / 0.35)
    env[-int(0.01 * SR):] *= np.linspace(1, 0, int(0.01 * SR))
    return np.tanh(1.8 * s * env)

def pad_chord(ms, dur):
    t = t_(dur); s = np.zeros((len(t), 2))
    for m in ms:
        for k, det in enumerate([-0.12, -0.05, 0.0, 0.06, 0.13]):
            f = midi_hz(m + det)
            saw = 2 * ((t * f + rng.random()) % 1) - 1
            s[:, k % 2] += saw
    s = np.stack([sosfilt(butter(2, 1800, 'low', fs=SR, output='sos'), s[:, c]) for c in range(2)], axis=1)
    env = np.minimum(1, t / 0.35) * np.minimum(1, (dur - t) / 0.4)
    return s * env[:, None] / (len(ms) * 3)


# ---------------- 국악기 재료 ----------------
def cut(x, t, dur, fade=0.01):
    a = max(0, int((t - 0.004) * SR)); seg = x[a:a + int(dur * SR)].copy()
    f = int(fade * SR); seg[-f:] *= np.linspace(1, 0, f)[:, None]
    return seg / (np.abs(seg).max() + 1e-9)

_tps_cache = {}
def tps(name, src_start_beat90=0.0, length_beats120=None):
    """태평소 구절을 90 → 120 BPM으로 (음 높이 그대로). src_start_beat90: 원본 90 BPM 박 기준 시작."""
    key = (name, src_start_beat90, length_beats120)
    if key in _tps_cache: return _tps_cache[key]
    x = load(os.path.join(SRC, name))
    a = int(src_start_beat90 * (60 / 90) * SR); x = x[a:]
    y = wsola(x, 120 / 90)
    if length_beats120: y = y[:int(length_beats120 * BEAT * SR)]
    f = int(0.3 * SR); y[-f:] *= np.linspace(1, 0, f)[:, None]
    y = y / (np.abs(y).max() + 1e-9)
    _tps_cache[key] = y
    return y

# 태평소 구절 이름 (B♭ 중심)
TPS = {
    '027': '149.태평소독주_시그널음악_027.wav', '038': '160.태평소독주_시그널음악_038.wav',
    '042': '164.태평소독주_시그널음악_042.wav', '046': '168.태평소독주_시그널음악_046.wav',
    '058': '180.태평소독주_시그널음악_058.wav', '059': '181.태평소독주_시그널음악_059.wav',
}

KICK = SNARE = HAT = HATO = JING = CRASH = CLAP = BIG = None
KKW = KKW_OPEN = DEONG = DEOK = None
Bb1, Bb0 = 34, 22


def init(bars, janggu_wav):
    global TOTAL, BARS, mix, score, kicks, KICK, SNARE, HAT, HATO, JING, CRASH, CLAP, BIG, KKW, KKW_OPEN, DEONG, DEOK
    BARS = bars
    TOTAL = int((bars * 4 * BEAT + 2.5) * SR)
    mix = {k: np.zeros((TOTAL, 2)) for k in BUSES}
    score = []; kicks = []
    KICK, SNARE, HAT, HATO, JING = kick(), snare(), hat(), hat(True), jing()
    CRASH, CLAP, BIG = crash(), clap(), big_drum()
    kkw = load(os.path.join(SRC, '국악기_꽹과리10.wav'))
    KKW = [cut(kkw, t, 0.18) for t in (0.02, 0.99, 1.47, 1.95, 3.40)]
    KKW_OPEN = cut(kkw, 2.69, 0.45)
    jg = load(janggu_wav)
    DEONG = [cut(jg, t, 0.45) for t in (1.98, 3.37, 4.79, 21.93, 24.48, 30.79)]   # 궁편 (낮은 소리)
    DEOK = [cut(jg, t, 0.2) for t in (7.15, 8.47, 11.01, 21.08)]                  # 채편 (높은 소리)


# ---------------- 악보 조각 ----------------
CHORDS = [[58, 61, 65], [54, 58, 61], [56, 60, 63], [53, 56, 60]]   # B♭m, G♭, A♭, Fm (2마디씩)
CHORDS_B = [[58, 61, 65], [56, 60, 63], [54, 58, 61], [56, 60, 63]]  # B♭m, A♭, G♭, A♭ (절정용)
RIFFS = {
    'A': [Bb1, Bb1 + 12, Bb1, Bb1 + 12, Bb1, Bb1 + 12, Bb1, Bb1 + 12],
    'A2': [Bb1, Bb1 + 12, Bb1 + 3, Bb1 + 12, Bb1 - 2, Bb1 + 10, Bb1, Bb1 + 7],
}

def K(beat, g=0.95, label='킥'): hit('drums', KICK, beat, g, label=label); kicks.append(beat)
def heartbeat(beat, g=1.0): K(beat, g, '심장박동 쿵'); K(beat + 0.3, g * 0.65, '심장박동 쿵')

def chuimsae(name, beat, g=0.9):
    path = os.path.join(CH_DIR, name + '.wav')
    if os.path.exists(path):
        v = load(path); v = v / (np.abs(v).max() + 1e-9)
        hit('voice', v, beat, g, label=f'추임새 {name}')
    else: score.append((beat, f'추임새 자리: {name}'))

def roll(start, steps, g0=0.2, g1=0.9):
    times = []
    for (b0, b1, step) in steps: times += list(np.arange(start + b0, start + b1 - 1e-9, step))
    for n_, b in enumerate(times): hit('drums', SNARE, b, g0 + (g1 - g0) * n_ / max(1, len(times) - 1), label='스네어 롤')

def drop(beat):
    hit('drums', CRASH, beat, 0.8, label='크래시'); hit('drums', BIG, beat, 1.0, label='큰북'); K(beat, 1.0)
    hit('jing', JING, beat, 0.9, label='징'); hit('janggu', DEONG[0], beat, 1.0)

def groove(bar, climax=False, riff='A', chords=None, transpose=0, double=False):
    """한 마디 박자. climax: 3-3-2 장구·16분 꽹과리·16분 베이스. double: 절정 중 절정(16분 킥 당김 더)."""
    s = bar_start(bar)
    for b in range(4): K(s + b)
    if climax:
        for b in ((1.75, 3.75) if not double else (0.75, 1.75, 2.75, 3.75)): K(s + b, 0.6, '킥 당김')
    for b in (1, 3): hit('drums', SNARE, s + b, 0.75, label='스네어'); hit('drums', CLAP, s + b, 0.55)
    for k_ in range(16): hit('drums', HATO if k_ % 4 == 2 else HAT, s + k_ * 0.25, 0.2 if k_ % 4 == 2 else 0.1 + 0.06 * (k_ % 2 == 0), pan=-0.3)
    if climax:
        for b in (0, 0.75, 1.5, 2, 2.75, 3.5): hit('janggu', DEONG[int(b * 4) % len(DEONG)], s + b, 1.0, pan=-0.15, label='장구 덩(3-3-2)')
        for k_ in range(16):
            if k_ % 4 != 0: hit('janggu', DEOK[k_ % len(DEOK)], s + k_ * 0.25, 0.4, pan=0.15)
        for k_ in range(16): hit('kkw', KKW[k_ % len(KKW)], s + k_ * 0.25, 0.3 + 0.3 * (k_ % 2 == 1), pan=0.35, label='꽹과리 휘모리' if k_ % 2 else None)
        for k_ in range(16): put('bass', bass_note((Bb1 if k_ % 2 == 0 else Bb1 + 12) + transpose, 0.12), s + k_ * 0.25, 0.5)
    else:
        for b, kind in [(0, 'D'), (1, 'K'), (1.5, 'K'), (2, 'D'), (2.5, 'K'), (3, 'K'), (3.5, 'D')]:
            hit('janggu', DEONG[(bar + int(b)) % len(DEONG)] if kind == 'D' else DEOK[int(b * 2) % len(DEOK)], s + b, 0.95 if kind == 'D' else 0.6,
                pan=-0.15, label='장구 덩' if kind == 'D' else '장구 덕')
        for b in np.arange(0.5, 4, 0.5): hit('kkw', KKW[int(b * 2) % len(KKW)], s + b, 0.45 if b % 1 else 0.25, pan=0.35, label='꽹과리' if b % 1 else None)
        for k_, m in enumerate(RIFFS[riff]): put('bass', bass_note(m + transpose, 0.22), s + k_ * 0.5, 0.55)
    ch = chords or CHORDS
    if bar % 2 == 0: put('pad', pad_chord([m + transpose for m in ch[(bar // 2) % 4]], 8 * BEAT), s, 1.0 if not climax else 1.1)

def halftime(bar):
    """반 박자 느낌(쉼): 1·3박 킥, 3박 스네어, 장구 덩 길게, 현악."""
    s = bar_start(bar)
    K(s, 0.9); K(s + 2.5, 0.6)
    hit('drums', SNARE, s + 2, 0.6, label='스네어(반박자)'); hit('drums', CLAP, s + 2, 0.4)
    for k_ in range(8): hit('drums', HAT, s + k_ * 0.5, 0.08, pan=-0.3)
    hit('janggu', DEONG[bar % len(DEONG)], s, 0.8, label='장구 덩'); hit('janggu', DEOK[0], s + 3.5, 0.5)
    put('bass', bass_note(Bb1, 1.6), s, 0.5)
    if bar % 2 == 0: put('pad', pad_chord(CHORDS[(bar // 2) % 4], 8 * BEAT), s, 0.9)

def build(bar, bars=2):
    """빌드업: 심장박동 + 장구 덩이 세짐 + 스네어 롤 + 쓸어 올리기, 마지막 반 박 멈춤."""
    s = bar_start(bar)
    for b in range(bars * 4 // 2): heartbeat(s + b * 2 if bars > 1 else s + b, 0.85)
    for k_, b in enumerate(np.arange(s, s + bars * 4 - 0.5, 0.5)): hit('janggu', DEONG[k_ % len(DEONG)], b, 0.5 + 0.4 * k_ / (bars * 8), label='장구 덩(조이기)')
    last = bar_start(bar + bars - 1)
    roll(last, [(0, 1, 0.5), (1, 2, 0.25), (2, 3, 0.125), (3, 3.5, 0.0625)], 0.3, 1.0)
    put('drums', riser(bars * 4 - 0.5), s, 0.45)
    put('pad', pad_chord([53, 58, 61, 65], (bars * 4 - 0.5) * BEAT), s, 0.9)

def intro(bar=0):
    s = bar_start(bar)
    for b in (0, 2, 4, 6): heartbeat(s + b, 0.8 + 0.03 * b)
    put('pad', pad_chord([46, 53, 58], 8 * BEAT), s, 0.7)
    for b in (3, 7): hit('kkw', KKW_OPEN, s + b, 0.5, pan=0.3, label='꽹과리 깽~')
    roll(s + 4, [(0, 2, 0.5), (2, 3, 0.25), (3, 3.75, 0.125)])
    put('drums', riser(4), s + 4, 0.35)

def bridge(bar, bars=4):
    """다리: 심장박동만 남고 태평소 긴 음과 현악이 떠 있음."""
    for k_ in range(bars * 2): heartbeat(bar_start(bar) + k_ * 2, 0.75)
    for k_ in range(bars // 2): put('pad', pad_chord([46, 53, 58, 61], 8 * BEAT), bar_start(bar + k_ * 2), 1.0)
    for k_ in range(bars): hit('kkw', KKW_OPEN, bar_start(bar + k_) + 3, 0.35, pan=0.3)

def stabs(bar):
    s = bar_start(bar)
    for b in (0, 0.75, 1.5, 2):
        K(s + b, 1.0, '강타'); hit('drums', SNARE, s + b, 0.6); hit('drums', BIG, s + b, 0.6)
        hit('janggu', DEONG[0], s + b, 1.0); hit('kkw', KKW_OPEN, s + b, 0.5, pan=0.3)
        put('bass', bass_note(Bb1, 0.3), s + b, 0.7)
    roll(s, [(2.5, 4, 0.125)], 0.3, 1.0)

def put_tps(key, bar, src_start_beat90=0, length_beats=None, g=0.9):
    put('tps', tps(TPS[key], src_start_beat90, length_beats), bar_start(bar), g)
    score.append((bar_start(bar), f'태평소 {key}'))


# ---------------- 섞기 ----------------
def reverb(x, sec=1.6, wet=0.25):
    t = t_(sec)
    ir = rng.standard_normal((len(t), 2)) * np.exp(-t / (sec / 5))[:, None]
    ir = np.stack([sosfilt(butter(1, 5000, 'low', fs=SR, output='sos'), ir[:, c]) for c in range(2)], axis=1)
    ir /= np.sqrt((ir ** 2).sum(axis=0))
    y = np.stack([fftconvolve(x[:, c], ir[:, c])[:len(x)] for c in range(2)], axis=1)
    return x + wet * y

def pump(x, depth, rel=0.16):
    """사이드체인: 킥마다 눌렀다가 rel초에 걸쳐 돌아옴 (심장이 뛰는 듯한 출렁임)."""
    env = np.ones(TOTAL)
    n_rel = int(rel * 4 * SR)
    curve = 1 - depth * np.exp(-np.arange(n_rel) / SR / rel)
    for kb in kicks:
        a = int(kb * BEAT * SR); n = min(n_rel, TOTAL - a)
        if n > 0: env[a:a + n] = np.minimum(env[a:a + n], curve[:n])
    return x * env[:, None]

def render(out_path, tail_fade=1.5, loop_beats=None):
    """섞어서 저장한다. loop_beats를 주면 그 길이로 잘라 끝 꼬리(잔향)를 처음에 겹쳐 이음매 없이 반복되게 한다."""
    mix['bass'] = pump(mix['bass'], 0.55)
    mix['pad'] = pump(mix['pad'], 0.6)
    mix['tps'] = pump(mix['tps'], 0.25)
    gains = {'drums': 0.8, 'janggu': 0.8, 'kkw': 0.5, 'bass': 0.55, 'pad': 0.3, 'tps': 0.62, 'voice': 0.85, 'jing': 0.5}
    sends = {'drums': 0.08, 'janggu': 0.15, 'kkw': 0.2, 'bass': 0.0, 'pad': 0.35, 'tps': 0.35, 'voice': 0.3, 'jing': 0.3}
    out = np.zeros((TOTAL, 2))
    for k, v in mix.items():
        out += reverb(v, wet=sends[k]) * gains[k] if sends[k] > 0 else v * gains[k]
    out = np.tanh(out * 1.2) / np.tanh(1.2)
    if loop_beats:
        n = int(round(loop_beats * BEAT * SR))
        tail = out[n:].copy(); out = out[:n].copy()
        k = min(len(tail), n); out[:k] += tail[:k]          # 끝의 잔향을 처음에 겹침 (반복 이음매)
        out = np.tanh(out * 1.05) / np.tanh(1.05)
        f = int(0.003 * SR)                                   # 이음매 딸깍 방지: 끝 3 ms 줄이고 처음 3 ms 키움
        out[-f:] *= np.linspace(1, 0, f)[:, None]; out[:f] *= np.linspace(0, 1, f)[:, None]
    else:
        f = int(tail_fade * SR); out[-f:] *= np.linspace(1, 0, f)[:, None]
    out *= 0.95 / np.abs(out).max()
    save(out_path, out)
    with open(out_path + '.score.txt', 'w', encoding='utf-8') as fp:
        fp.write('# 박(0 = 첫 박, 120 BPM) 소리\n')
        for b, lbl in sorted(score): fp.write(f'{b:.2f}\t{lbl}\n')
    return len(out) / SR
