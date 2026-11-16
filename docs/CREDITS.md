# 출처 표기 (Credits)

## 음악: 스테이지 1 곡 (`Assets/Audio/Music/stage1_song.mp3`)
퓨전 국악 액션, 120 BPM, 101마디. `harness/scripts/music/make-song.py`가 판정 타임라인(`harness/music/stage1_timeline.json`)을 따라 아래 재료를 잘라 조립했다(판정 박의 장구는 비우고 플레이어가 친다).
드럼·베이스·현악·징 소리는 코드로 합성했다(외부 음원 없음). 재료 원본은 `harness/music-src/demo/`에 있다.

| 재료 | 저작자 | 출처 | 라이선스 | 바꾼 점 |
|---|---|---|---|---|
| 태평소독주_시그널음악 027, 038, 042, 046, 058, 059 | 한국저작권위원회 | 공유마당(한국저작권위원회) https://gongu.copyright.or.kr | CC BY (저작자 표시) | 90 → 120 BPM으로 빠르기만 바꿈(음 높이 유지), 구간을 잘라 배치 |
| 국악기_꽹과리10 | 한국저작권위원회 | 공유마당 | CC BY | 한 타씩 잘라 배치 |
| 전통악기_장구_치다_보통속도_Ver.1 | (재)전주정보문화산업진흥원 | 공유마당 | 공공누리 제1유형 (출처 표시) | 덩·덕 한 타씩 잘라 배치 |

- 게임 안 표기: 타이틀 화면 아래 "음악 재료(공유마당): 한국저작권위원회 「태평소독주 시그널음악」「국악기_꽹과리」 CC BY · (재)전주정보문화산업진흥원 「전통악기 장구」 공공누리 1유형".
- 각 재료의 저작자·원문 링크는 `harness/music-src/demo/*.txt`(공유마당이 함께 주는 표기 파일)에 있다.

## 히트사운드 (`Assets/Audio/Hit/`)
- 곡과 같은 재료(위 표의 장구·꽹과리 녹음)에서 잘라 만들었다 (`harness/scripts/music/make-hitsounds.py`). 출처는 위 표와 같다.

## 효과음: 전투 소리 실제 녹음 (`Assets/Audio/Sfx/Rec_*.wav`, 작업 B48 · GDD-beta B17.5)
Mixkit(https://mixkit.co)의 무료 효과음을 2026-11-16에 받아(사용자 승인) `harness/scripts/sfx/prep-mixkit.py`로 모노·앞 무음 자르기·길이 줄이기·음량 맞추기를 했다. 원본은 `harness/sfx-src/mixkit/`에 있다.
라이선스: Mixkit Sound Effects Free License — 상업 사용 가능, 출처 표기는 필수가 아니지만 여기 적어 둔다. 원본 파일을 따로 배포하지 않고 게임 안에서만 쓴다.

| 게임 안 이름 | Mixkit 제목 | 번호 | 쓰는 곳 |
|---|---|---|---|
| Rec_SwordWhoosh | Fast sword whoosh | 2792 | 적 칼 휘두름 "쉭" (판정 반 박 전), 회피 |
| Rec_SwordSwish | Sword blade swish | 1506 | 내 칼 휘두름, 윈드밀 마무리 |
| Rec_ParryClang | Samurai Sword Impact | 2789 | 막기·공격 PERFECT "챙", 자세 무너짐 |
| Rec_BlockClang | Heavy sword smashes metal | 2795 | 막기·공격 NORMAL |
| Rec_Slash | Sword cutting flesh | 2788 | 처형·베임 |
| Rec_Punch | Body punch quick hit | 2153 | 맞음 "퍽" |
| Rec_Kick | Martial arts kick | 2163 | 무사 옆차기 |
| Rec_BodyFall | Body impact falling into the sand | 2498 | 쓰러짐 (처형·윈드밀 마무리) |

- 원본 주소: `https://assets.mixkit.co/active_storage/sfx/<번호>/<번호>.wav`

## 효과음: 합성
- 그 밖의 효과음(저음, 북, 징, 먼지, 귀 울림, 되감기 등)은 코드로 합성했다 (`Assets/Editor/SfxSynthTool.cs`). 녹음이 없을 때도 합성음으로 대신한다.
