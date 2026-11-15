"""B44 히트사운드 (GDD-beta B17.1): 곡에 쓴 실제 장구·꽹과리 녹음에서 잘라 만든다.

  PERFECT = 장구 "덩"(궁편) + 채편 + 꽹과리 짧게,  NORMAL = 장구 "덕"(채편) + 꽹과리 아주 작게,
  FAIL = "덩"을 3반음 낮추고 둔하게(저역 통과), 원 없음(EMPTY) = 짧은 "덕".
소리 시작이 파일 맨 앞 1 ms 안에 오도록 자른다(누르는 순간 바로 들리게).

사용 (프로젝트 루트):
  python harness/scripts/music/make-hitsounds.py <장구 wav> Assets/Audio/Hit
"""
import sys, os
import numpy as np
from fractions import Fraction
from scipy.signal import butter, sosfilt, resample_poly
sys.path.insert(0, os.path.dirname(__file__))
from demo_lib import SR, load, save

SRC = os.path.join(os.path.dirname(__file__), '..', '..', 'music-src', 'demo')
jg = load(sys.argv[1]).mean(axis=1)
kkw = load(os.path.join(SRC, '국악기_꽹과리10.wav')).mean(axis=1)
out_dir = sys.argv[2]
os.makedirs(out_dir, exist_ok=True)


def cut(x, t, dur, fade=0.012):
    a = max(0, int((t - 0.001) * SR)); seg = x[a:a + int(dur * SR)].copy()
    f = int(fade * SR); seg[-f:] *= np.linspace(1, 0, f)
    return seg / (np.abs(seg).max() + 1e-9)


def mixdown(parts, length):
    y = np.zeros(int(length * SR))
    for seg, g in parts:
        n = min(len(seg), len(y)); y[:n] += seg[:n] * g
    return y / (np.abs(y).max() + 1e-9) * 0.95


deong = cut(jg, 1.98, 0.45)        # 궁편 (곡의 DEONG[0]과 같은 타)
deok = cut(jg, 7.15, 0.2)          # 채편 (곡의 DEOK[0])
deok2 = cut(jg, 11.01, 0.12)
kkw_short = cut(kkw, 0.99, 0.15)

perfect = mixdown([(deong, 1.0), (deok, 0.45), (kkw_short, 0.3)], 0.45)
normal = mixdown([(deok, 1.0), (kkw_short, 0.12)], 0.2)
f = Fraction(2 ** (3 / 12)).limit_denominator(200)                 # 3반음 낮춤 (길이도 늘어남)
fail = resample_poly(cut(jg, 3.37, 0.2), f.numerator, f.denominator)
fail = sosfilt(butter(2, 500, 'low', fs=SR, output='sos'), fail)
fail = fail[:int(0.18 * SR)]; fail[-int(0.02 * SR):] *= np.linspace(1, 0, int(0.02 * SR))
fail = fail / (np.abs(fail).max() + 1e-9) * 0.95
empty = deok2[:int(0.08 * SR)].copy(); empty[-int(0.01 * SR):] *= np.linspace(1, 0, int(0.01 * SR)); empty *= 0.95

for name, y in [('Hit_Perfect', perfect), ('Hit_Normal', normal), ('Hit_Fail', fail), ('Hit_Empty', empty)]:
    save(os.path.join(out_dir, name + '.wav'), np.stack([y, y], axis=1)[:, :1])
    onset = np.argmax(np.abs(y) > 0.1 * np.abs(y).max()) / SR * 1000
    print(f'{name}: {len(y) / SR * 1000:.0f} ms, 소리 시작 {onset:.1f} ms')
