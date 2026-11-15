"""B10 무료 데모 2: 퓨전 국악 액션 32초 (16마디). 악기·조각은 song_kit.py.

사용 (프로젝트 루트):
  python harness/scripts/music/make-demo.py <장구 wav> <출력 wav>
  (장구는 mp3라 harness/scripts/music/mp3towav.ps1로 먼저 wav로 바꾼다)
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
import song_kit as k

k.init(16, sys.argv[1])
k.intro(0)
k.drop(k.bar_start(2)); k.chuimsae('얼쑤', k.bar_start(2) + 0.5)
for bar in range(2, 8): k.groove(bar)
k.put_tps('042', 2, 0, 20)
k.chuimsae('좋다', k.bar_start(7) + 2.5)
k.build(8)
k.put_tps('027', 8, 1, 7, g=0.7)
k.drop(k.bar_start(10)); k.chuimsae('얼씨구', k.bar_start(10) + 0.5)
for bar in range(10, 14): k.groove(bar, climax=True, transpose=2 if bar % 2 else 0)
k.put_tps('058', 10, 1, 16, g=0.95)
k.chuimsae('어이', k.bar_start(12) + 3.5)
k.stabs(14); k.chuimsae('하', k.bar_start(14) + 3.0)
k.drop(k.bar_start(15))
k.put('pad', k.pad_chord([46, 53, 58, 61], 4 * k.BEAT + 2.0), k.bar_start(15), 1.2)
k.put('bass', k.bass_note(k.Bb0 + 12, 1.5), k.bar_start(15), 0.8)
k.put_tps('046', 15, 6, 8)
sec = k.render(sys.argv[2])
print('wrote', sys.argv[2], f'{sec:.1f}s,', len(k.score), 'events')
