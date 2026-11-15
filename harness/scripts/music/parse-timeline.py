"""개발 빌드 오토플레이 로그에서 판정 타임라인을 뽑는다 (GDD-beta B16.2).

  Builds\\Gray\\RhythmActionGame.exe -batchmode -startGame -autoplay -logFile <log>
  python harness/scripts/music/parse-timeline.py <log> harness/music/stage1_timeline.json

결과 JSON:
  judges:   [{beat, key, result, state, chapter}]  — beat는 시계 박자(곡 위치), state는 Running/Combat, chapter는 1~6
  states:   [{beat, state}]                         — Running/Combat/Cleared 바뀐 시계 박자
  chapters: [{number, run, clock}]                  — 구간이 바뀐 시점의 달리기·시계 박자
  combats:  [{start, end, kind}]                    — kind: normal / surround / boss (구간 4 = 포위, 6 = 무사)
  end:      마지막 판정 박
"""
import sys, re, json

log = open(sys.argv[1], encoding='utf-8', errors='replace').read().splitlines()
re_judge = re.compile(r'\[autoplay\] 판정 목표 ([\d.]+)박 (\w+) → (\w+)')
re_state = re.compile(r'\[autoplay\] 상태 (\w+) at ([\d.]+)박')
re_chap = re.compile(r'\[autoplay\] 구간 (\d+) 시작: 달리기 ([\d.]+)박, 시계 ([\d.]+)박')
re_rewind = re.compile(r'\[autoplay\] 되감기')

judges, states, chapters = [], [], []
state, chapter = 'Running', 1
for line in log:
    if re_rewind.search(line):
        raise SystemExit('되감기가 일어난 로그는 쓸 수 없다 (모두 PERFECT 진행이어야 함): ' + line)
    m = re_chap.search(line)
    if m:
        chapter = int(m.group(1))
        chapters.append({'number': chapter, 'run': float(m.group(2)), 'clock': float(m.group(3))})
        continue
    m = re_state.search(line)
    if m:
        state = m.group(1); b = float(m.group(2))
        states.append({'beat': b, 'state': state})
        if state == 'Cleared': break
        continue
    m = re_judge.search(line)
    if m:
        judges.append({'beat': float(m.group(1)), 'key': m.group(2), 'result': m.group(3), 'state': state, 'chapter': chapter})

# 클리어 박: 클리어 순간 시계가 멈추며 엉뚱한 값(예: 24박)이 찍히므로, 무사 결투 시작 O + 74박(BossDuel.LengthBeats)으로 고친다
import math
BOSS_LENGTH = 74
last_judge = max(j['beat'] for j in judges)
for s_ in states:
    if s_['state'] == 'Cleared' and s_['beat'] < last_judge:
        boss_start = max(x['beat'] for x in states if x['state'] == 'Combat')
        s_['beat'] = math.ceil(boss_start - 0.05) + BOSS_LENGTH

# 전투 구간
combats, cur = [], None
for s in states:
    if s['state'] == 'Combat': cur = s['beat']
    elif cur is not None:
        ch = max([c['number'] for c in chapters if c['clock'] <= cur + 1e-6] or [1])
        kind = 'surround' if ch == 4 else 'boss' if ch == 6 else 'normal'
        combats.append({'start': cur, 'end': s['beat'], 'kind': kind}); cur = None

bad = [j for j in judges if j['result'] != 'Perfect']
if bad: print('경고: PERFECT가 아닌 판정', len(bad), bad[:3])
out = {'judges': judges, 'states': states, 'chapters': chapters, 'combats': combats, 'end': max(j['beat'] for j in judges)}
json.dump(out, open(sys.argv[2], 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print(f"판정 {len(judges)}개 (반 박 {sum(1 for j in judges if j['beat'] % 1)}), 전투 {len(combats)}개 {[c['kind'] for c in combats]}, "
      f"구간 {[(c['number'], c['clock']) for c in chapters]}, 마지막 판정 {out['end']}박")
