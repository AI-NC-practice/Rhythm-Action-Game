"""B10 스테이지 곡 (전체): 퓨전 국악 액션, 120 BPM, 96마디(384박 = 3분 12초), 반복 이음매 없이 처음으로 돌아감.

길이는 오토플레이로 잰 스테이지 길이(모두 PERFECT일 때 약 384박, 무사 결투는 약 308박부터)에 맞췄다.
게임은 박 위치로 곡 위치를 정하므로(BeatClock.MusicTimeAt) 재도전으로 길어지면 처음부터 다시 돈다.
악기·조각은 song_kit.py. 추임새 자리는 비어 있으면 .score.txt에만 적힌다.

사용 (프로젝트 루트):
  python harness/scripts/music/make-song.py <장구 wav> <출력 wav>
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
import song_kit as k

BARS = 96
k.init(BARS, sys.argv[1])
B = k.bar_start

# 1~2마디: 도입 (심장박동)
k.intro(0)

# 3~10마디 A1: 첫 달리기
k.drop(B(2)); k.chuimsae('얼쑤', B(2) + 0.5)
for bar in range(2, 10): k.groove(bar)
k.put_tps('042', 2, 0, 20)
k.put_tps('027', 7, 1, 12, g=0.8)
k.chuimsae('좋다', B(9) + 2.5)

# 11~18마디 A2: 베이스가 움직이고 다른 태평소
for bar in range(10, 18): k.groove(bar, riff='A2')
k.put_tps('038', 10, 0, 24, g=0.85)
k.put_tps('059', 16, 6, 8, g=0.8)

# 19~20마디: 빌드업
k.build(18)

# 21~28마디 B1: 절정
k.drop(B(20)); k.chuimsae('얼씨구', B(20) + 0.5)
for bar in range(20, 28): k.groove(bar, climax=True, chords=k.CHORDS_B, transpose=2 if bar % 2 else 0)
k.put_tps('058', 20, 1, 28, g=0.95)
k.chuimsae('어이', B(27) + 3.5)

# 29~32마디: 쉼 (반 박자, 태평소 독주)
for bar in range(28, 32): k.halftime(bar)
k.put_tps('046', 28, 0, 16, g=0.85)

# 33~48마디 A3·A4: 두 번째 달리기
k.drop(B(32)); k.chuimsae('얼쑤', B(32) + 0.5)
for bar in range(32, 40): k.groove(bar)
k.put_tps('059', 32, 0, 28, g=0.85)
for bar in range(40, 48): k.groove(bar, riff='A2')
k.put_tps('027', 40, 1, 12, g=0.8)
k.put_tps('038', 44, 8, 16, g=0.8)

# 49~50마디: 빌드업
k.build(48)

# 51~58마디 B2: 절정
k.drop(B(50)); k.chuimsae('얼씨구', B(50) + 0.5)
for bar in range(50, 58): k.groove(bar, climax=True, chords=k.CHORDS_B, transpose=2 if bar % 2 else 0)
k.put_tps('058', 50, 1, 28, g=0.95)

# 59~62마디: 다리 (심장박동 + 태평소 긴 음)
k.bridge(58, 4)
k.put_tps('046', 58, 6, 16, g=0.85)

# 63~70마디 A5: 세 번째 달리기 (무사 결전 앞)
k.drop(B(62)); k.chuimsae('좋다', B(62) + 0.5)
for bar in range(62, 70): k.groove(bar, riff='A2' if bar >= 66 else 'A')
k.put_tps('042', 62, 0, 20)
k.put_tps('059', 67, 6, 12, g=0.8)

# 71~72마디: 빌드업 (무사 결전 진입 무렵)
k.build(70)

# 73~84마디 B3: 가장 큰 절정 (무사 결전)
k.drop(B(72)); k.chuimsae('얼씨구', B(72) + 0.5)
for bar in range(72, 84): k.groove(bar, climax=True, chords=k.CHORDS_B, transpose=2 if bar % 2 else 0, double=bar >= 76)
k.put_tps('058', 72, 1, 28, g=0.95)
k.put_tps('038', 79, 0, 20, g=0.9)
k.chuimsae('어이', B(79) + 3.5)

# 85~92마디: 마지막 달리기
k.drop(B(84))
for bar in range(84, 92): k.groove(bar, riff='A2')
k.put_tps('059', 84, 0, 28, g=0.85)

# 93마디: 엇박 강타 → 94마디: 징 + 태평소 긴 음 → 95~96마디: 심장박동으로 잦아들며 처음으로
k.stabs(92); k.chuimsae('하', B(92) + 3.0)
k.drop(B(93))
k.put('pad', k.pad_chord([46, 53, 58, 61], 4 * k.BEAT + 2.0), B(93), 1.2)
k.put('bass', k.bass_note(k.Bb0 + 12, 1.5), B(93), 0.8)
k.put_tps('046', 93, 6, 8)
for b in (0, 2, 4, 6): k.heartbeat(B(94) + b, 0.6 + 0.03 * b)
k.put('pad', k.pad_chord([46, 53, 58], 8 * k.BEAT), B(94), 0.6)

sec = k.render(sys.argv[2], loop_beats=BARS * 4)
print('wrote', sys.argv[2], f'{sec:.1f}s,', len(k.score), 'events,', len([1 for _, l in k.score if '추임새 자리' in l]), 'chuimsae slots empty')
