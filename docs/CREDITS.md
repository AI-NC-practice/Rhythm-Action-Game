# 출처 표기 (Credits)

## 음악
| 쓰는 곳 | 원곡 | 저작자 | 출처 | 라이선스 |
|---|---|---|---|---|
| 스테이지 배경 음악 (`Assets/Audio/Music/gugak_bgm197_loop120.wav`) | 국악 BGM #197 | 주식회사 아이티앤 | 공유마당(한국저작권위원회) https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?wrtSn=13386937&menuNo=200020 | CC BY (저작자 표시) |

- 바꾼 점: 재생 속도를 0.967배로 늦추고(게임 120 BPM 박자에 맞춤), 첫 강박부터 64초 길이로 잘라 반복되도록 끝을 처음에 이어 붙였다.
  만드는 법: `harness/scripts/music/make-loop.py` (원본 mp3는 `harness/music-src/gugak_bgm197.mp3`).
- 게임 안 표기: 타이틀 화면 아래 "음악: 국악 BGM #197 — 주식회사 아이티앤, 공유마당(한국저작권위원회), CC BY".

## 효과음
- 모두 코드로 합성했다 (`Assets/Editor/SfxSynthTool.cs`). 외부 음원 없음.
