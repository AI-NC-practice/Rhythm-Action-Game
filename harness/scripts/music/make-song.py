"""B10/B42 스테이지 곡: 판정 타임라인을 따라 만든다 (GDD-beta B16.2). 퓨전 국악 액션, 120 BPM.

- 곡 위치 = 시계 박자. 재도전이 없으면 판정의 시계 박자는 매번 같으므로(B16.2), 모두 PERFECT 진행의
  판정 타임라인(parse-timeline.py)을 따라 구간을 짠다:
    달리기 = 기본 박(A, 챕터마다 베이스 바꿈), 일반 전투 = 절정(B), 포위·무사 결투 = 더 센 절정,
    전투 직전 1~2마디 = 빌드업(심장박동·롤·쓸어 올리기), 전투 시작 = 터짐(크래시·큰북·징).
- 판정 박마다 장구·꽹과리 강세: 정박은 장구 "덩" + 꽹과리 열린 소리, 반 박은 장구 "덕" + 꽹과리.
  노드가 곡의 타격이 되고 히트사운드(B14)가 그 위에 겹친다.
- 끝: 클리어 뒤 엇박 강타 → 징 → 심장박동으로 잦아들어 처음으로 이어짐.
악기·조각은 song_kit.py.

사용 (프로젝트 루트):
  python harness/scripts/music/make-song.py <장구 wav> <타임라인 json> <출력 wav>
"""
import sys, os, json, math
sys.path.insert(0, os.path.dirname(__file__))
import song_kit as k

jg, tl_path, out_path = sys.argv[1], sys.argv[2], sys.argv[3]
tl = json.load(open(tl_path, encoding='utf-8'))
judges = tl['judges']
combats = tl['combats']
cleared = next((s['beat'] for s in tl['states'] if s['state'] == 'Cleared'), tl['end'] + 4)

END_BAR = int(math.ceil(cleared / 4.0))          # 클리어가 오는 마디부터 마무리
BARS = END_BAR + 3                                # 강타 1 + 징 1 + 잦아듦 1
k.init(BARS, jg)
B = k.bar_start

def combat_at(bar):
    """마디 가운데가 전투 안이면 그 전투."""
    mid = B(bar) + 2
    for c in combats:
        if c['start'] <= mid < c['end']: return c
    return None

def chapter_at(beat):
    ch = 1
    for c in tl['chapters']:
        if c['clock'] <= beat + 1e-6: ch = c['number']
    return ch

# 전투가 시작되는 마디 (터짐 자리)와 그 앞 빌드업 마디
combat_bars = {}
for c in combats:
    b0 = int(math.floor(c['start'] / 4.0))
    combat_bars[b0] = c
build_bars = set()
for b0 in combat_bars:
    for d in (1, 2):
        if b0 - d >= 1 and combat_at(b0 - d) is None and (b0 - d) not in combat_bars: build_bars.add(b0 - d)

# 1마디: 도입 (심장박동 + 롤)
for b in (0, 2): k.heartbeat(b, 0.8)
k.put('pad', k.pad_chord([46, 53, 58], 4 * k.BEAT), 0, 0.7)
k.roll(0, [(2, 3, 0.25), (3, 3.75, 0.125)])
k.put('drums', k.riser(4), 0, 0.3)
k.drop(B(1)); k.chuimsae('얼쑤', B(1) + 0.5)

TPS_ORDER = ['042', '038', '059', '027', '046', '058']
TPS_LEN = {'042': 20, '038': 24, '059': 28, '027': 12, '046': 16, '058': 28}
tps_free = 0.0
tps_i = 0

def try_tps(bar, keys, g=0.85):
    """태평소 구절을 겹치지 않게 놓는다."""
    global tps_free, tps_i
    if B(bar) < tps_free: return
    key = keys[tps_i % len(keys)]; tps_i += 1
    k.put_tps(key, bar, 0 if key != '027' else 1, TPS_LEN[key], g=g)
    tps_free = B(bar) + TPS_LEN[key] + 4

run_since = 1
for bar in range(1, END_BAR):
    c = combat_at(bar)
    if bar in combat_bars and bar > 1:
        cb = combat_bars[bar]
        k.drop(B(bar)); k.chuimsae('얼씨구' if cb['kind'] != 'normal' else '좋다', B(bar) + 0.5)
    if c is not None:
        rel = bar - int(math.floor(c['start'] / 4.0))
        if c['kind'] == 'boss':
            k.groove(bar, climax=True, chords=k.CHORDS_B, transpose=2 if bar % 2 else 0, double=(rel // 4) % 2 == 1)
            if rel % 8 == 0: try_tps(bar, ['058', '038', '059'], g=0.95)
        elif c['kind'] == 'surround':
            k.groove(bar, climax=True, chords=k.CHORDS_B, transpose=2 if bar % 2 else 0, double=rel >= 2)
            if rel == 0: try_tps(bar, ['058'], g=0.95)
        else:
            k.groove(bar, climax=True, chords=k.CHORDS_B)
        run_since = bar + 1
        continue
    if bar in build_bars:
        nxt = bar + 1
        if nxt in build_bars: continue                     # 두 마디 빌드업은 첫 마디에서 한 번에
        start = bar - 1 if (bar - 1) in build_bars else bar
        k.build(start, bars=bar - start + 1)
        continue
    ch = chapter_at(B(bar))
    k.groove(bar, riff='A2' if ch in (2, 5) else 'A')
    if (bar - run_since) % 8 == 0: try_tps(bar, TPS_ORDER)

# 판정 강세: 판정 박마다 장구·꽹과리 (노드 = 곡의 타격)
for j in judges:
    b = j['beat']
    if abs(b - round(b)) < 1e-6:
        k.hit('janggu', k.DEONG[int(b) % len(k.DEONG)], b, 1.15, pan=-0.1, label=f'판정 강세 {j["key"]}')
        k.hit('kkw', k.KKW_OPEN, b, 0.35, pan=0.3)
    else:
        k.hit('janggu', k.DEOK[int(b * 2) % len(k.DEOK)], b, 1.0, pan=0.1, label=f'판정 강세(반 박) {j["key"]}')
        k.hit('kkw', k.KKW[int(b * 2) % len(k.KKW)], b, 0.55, pan=0.35)

# 마무리: 엇박 강타 → 징 + 태평소 긴 음 → 심장박동으로 잦아들어 처음으로
k.stabs(END_BAR); k.chuimsae('하', B(END_BAR) + 3.0)
k.drop(B(END_BAR + 1))
k.put('pad', k.pad_chord([46, 53, 58, 61], 4 * k.BEAT + 2.0), B(END_BAR + 1), 1.2)
k.put('bass', k.bass_note(k.Bb0 + 12, 1.5), B(END_BAR + 1), 0.8)
k.put_tps('046', END_BAR + 1, 6, 8)
for b in (0, 2): k.heartbeat(B(END_BAR + 2) + b, 0.6)

sec = k.render(out_path, loop_beats=BARS * 4)
print('wrote', out_path, f'{sec:.1f}s, {BARS} bars ({BARS * 4} beats), end bar {END_BAR},', len(judges), 'judge accents,',
      len([1 for _, l in k.score if '추임새 자리' in l]), 'chuimsae slots empty')
