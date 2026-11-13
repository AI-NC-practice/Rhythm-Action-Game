# 작업 인계 문서 (beta1-1)

- 작성: 2026-11-13 (퇴근 전 정리)
- 브랜치: **`beta1-1`** (GitHub `AI-NC-practice/Rhythm-Action-Game`). `main`에는 개발자 모드까지만 들어가 있다.
- 작업 목록의 진실은 언제나 `harness/tasks.json`이고, 작업마다의 기록은 `harness/log.md`, 평가 보고서는 `harness/reports/`에 있다.
- 테스트(마지막 전체 검증): EditMode 145/145, PlayMode 136/136 통과. BuildGray 빌드와 smoke-test 통과.

---

## 1. 집에서 이어서 시작하기

1. 저장소 받기
   ```bash
   git clone https://github.com/AI-NC-practice/Rhythm-Action-Game.git
   cd Rhythm-Action-Game
   git checkout beta1-1
   ```
   이미 받아 둔 저장소라면 `git fetch` 후 `git checkout beta1-1` → `git pull`.
2. Unity Hub에서 **Unity 6000.4.0f1**로 프로젝트를 연다. 처음 열 때 Cinemachine 3.1.7 패키지를 자동으로 받는다(인터넷 필요). 확인이 끝나면 Unity Editor는 **닫는다**. 에디터가 열려 있으면 아래 배치 명령이 실패한다.
3. 동작 확인 (프로젝트 루트에서)
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.SceneBuilder.BuildAll
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-verify.ps1
   ```
   결과 요약은 `harness/reports/verify-latest.md`에 나온다.
4. 테스트 빌드가 필요하면
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.BuildScript.BuildGray
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/smoke-test-build.ps1
   ```
   빌드는 `Builds/Gray/RhythmActionGame.exe`에 생긴다. 테스트 패키지 zip(`RhythmActionGame-beta1-1-dev*.zip`)은 용량 때문에 **저장소에 올리지 않았다**. 집에서는 위 명령으로 다시 빌드한다.
5. Claude Code로 이어서 하려면: 프로젝트 폴더에서 Claude Code를 열고 "`docs/HANDOFF.md` 읽고 B10부터 이어서 진행해"라고 하면 된다. 작업 루프는 `/run-tasks`다.

## 2. 지금 멈춘 곳: B10 음악 (사용자 결정 대기)

- 처음 계획은 Suno였다. 로그인은 됐지만 **무료 플랜은 곡 다운로드·상업 이용이 불가**했다(Pro는 연간 결제 기준 월 ₩12,000부터). 그래서 사용자가 **무료 국악 음원(한국저작권위원회 공유마당, CC BY)**으로 바꾸기로 했다.
- 찾은 후보 (모두 CC BY: 출처만 밝히면 상업 이용도 무료). 빠르기는 미리듣기 음원을 대략 분석한 값이다.

| 후보 | 길이 | 빠르기(추정) | 저작자 | 주소 |
|---|---|---|---|---|
| **LSMBG0704_사물놀이** (추천) | 1:30 | 약 125 BPM | 한국저작권위원회 | https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?wrtSn=13312616&menuNo=200020 |
| **국악 BGM #197 사물놀이 분위기** (추천) | 1:07 | 약 128 BPM | 주식회사 아이티앤 | https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?wrtSn=13386937&menuNo=200020 |
| 국악 BGM #31 힘찬 분위기 | 1:05 | 약 107 BPM | 주식회사 아이티앤 | https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?wrtSn=13386771&menuNo=200020 |
| 국악 BGM #38 긴박한 상황 | 0:59 | 약 106 BPM | 주식회사 아이티앤 | https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?wrtSn=13386778&menuNo=200020 |
| 국악 배경음악 #55 급한 분위기 | 1:11 | 약 80 (또는 160) BPM | 공유마당 | https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?wrtSn=13379638&menuNo=200020 |
| 국악 BGM #154 긴장감 | 1:03 | 약 80 BPM | 주식회사 아이티앤 | https://gongu.copyright.or.kr/gongu/wrt/wrt/view.do?wrtSn=13386894&menuNo=200020 |

- **제안(사용자 답 대기):** 사물놀이 두 곡(LSMBG0704, BGM #197)을 받아 번갈아 이어 붙인다. 스테이지가 약 3분 30초~4분이라 한 곡만으로는 반복이 잦다.
- **승인 뒤 할 일:**
  1. 두 mp3를 `Assets/Audio/Music/`에 저장한다. 다운로드는 사이트의 "이용범위 동의 → 공유저작물 다운로드"로 한다.
  2. 정확한 BPM과 첫 박 위치를 분석한다.
  3. 120 BPM(GameConfig.Bpm, GDD 값)에 맞게 4~6% 늘린다.
  4. 장구 반주(JangguBacking) 대신 BeatClock의 music으로 연결하고 첫 박 오프셋을 맞춘다.
  5. 게임 안 크레딧과 문서에 출처를 표기한다. 예: `한국저작권위원회, "LSMBG0704_사물놀이", 공유마당, CC BY`.
  6. unity-verify를 통과시키고, 사용자가 듣고 확인한다.
- `harness/tasks.json`의 B10 제목은 아직 "Suno"로 남아 있다. 진행할 때 notes와 log에 바뀐 방법을 적는다(acceptance는 사용자 승인 없이 바꾸지 않는다).

## 3. 앞으로 할 일 (B10 다음)

| ID | 종류 | 내용 | 주의 |
|---|---|---|---|
| T10 | 에셋 | 정적 에셋 7개 (VARCO 3D) | **포인트 비용** → 사용자 승인 후. 모든 varco-3d 호출에 `flowId`(asset_specs의 defaults.flow_id). 명세는 `harness/asset_specs.json` |
| T11 | 에셋 | 캐릭터 4개 + 리깅 + VARCO 동작 | 위와 같음. T자 자세 → Rig → Animate → FBX |
| T12 | 에셋 | Mixamo 동작 받기 | 로그인은 사용자, 다운로드는 Claude |
| T13 | Unity | 툰 셰이딩 + 1인칭 전신 플레이어 교체 | 지금 플레이어는 회색 캡슐 + 칼 막대(3인칭 컷에서 보임) |
| T14 | Unity | 적 모델 교체 + 전투 애니메이션 연결 | 동적 전투(B13)의 이동·자세 상태(Guard, Pushed, Kick 등)에 동작을 입힌다 |
| T15 | Unity | 맵 모델 교체 + 분위기 + 최종 빌드 | BuildFinal에는 개발자 모드·진단 도구가 빠진다 |
| T16 | 사용자 | 최종 플레이 확인 | |

### 제안·미뤄 둔 것 (사용자 결정 필요)
- **이펙트를 더 화려하게:** 에셋을 넣은 뒤 다듬기로 사용자와 합의했다(GDD-beta B12.3). 지금 입자는 회색 상자 단계라 작은 사각형으로 보인다.
- **자리 바꾸기 어깨 너머 컷:** 지금 2단계 표에 원이 없는 틈이 없어 실제로는 나오지 않는다(1인칭 180° 회전으로 바뀜). 보이게 하려면 2단계에 1~2박 쉬는 틈을 넣어야 한다.
- **공격 카메라가 가까움:** 공격 카메라(+0.4 m)가 무사에 가까워 무사가 화면을 크게 가린다.
- **판정 진단:** 사용자가 "결투 처음 2타 판정이 안 맞는다"고 했지만 재현되지 않았고, 이후 "타이밍은 좋다"고 했다. 다시 생기면 Player.log의 `[판정기록]`을 본다(4장).

## 4. 개발자 도구 (회색 빌드·에디터 전용, 최종 빌드에는 없음)
- **숫자 1~6:** 그 챕터 처음에서 시작 (6 = 성문 위 무사 결투)
- **F1:** 연출 전체 켜기/끄기
- **판정 기록:** 키를 누를 때마다 원과의 시간 차(ms)·결과·되감기가 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Rhythm-Action-Game\Player.log`에 `[판정기록]`으로 남는다.
- **실행 인자:**
  - `-devChapter N`: N챕터에서 바로 시작
  - `-autoplay`: 목표 박에 실제 시간으로 키 입력
  - `-autoplayMiss N`: 무사 결투 N번째 판정을 한 번 놓침
  - `-autoplayShots 폴더`: 화면 저장
  - `-autoplayShotsRange 시작 끝`: 화면 저장 구간 (O 기준 박)
  - 예: `Builds\Gray\RhythmActionGame.exe -screen-fullscreen 0 -screen-width 960 -screen-height 540 -devChapter 6 -autoplay -autoplayShots C:\temp\shots -autoplayShotsRange 52 57`

## 5. beta1-1에서 한 일 (요약, 자세한 것은 `harness/log.md`)
- **기획 문서:** `docs/GDD.md`는 잠김(`harness/gdd.lock`, 수정 금지). beta에서 바뀐 것은 모두 `docs/GDD-beta.md` B1~B13에 있고, 근거 제안서는 `docs/proposals/`(dynamic-presentation, boss-duel, dynamic-combat)에 있다.
- **B01~B09 판정 UI·사건·차트·연출 기반:** 판정 원이 화면 안전 타원 안에 오고, 사건 18종과 연출 시스템(카메라·시간·화면·이펙트·효과음 합성)을 만들었다.
- **B11~B25 피드백 반영:**
  - 판정 타이밍(부드러운 시계, 입력 이벤트 시각)
  - 판정 원 FX
  - 장애물 간격 촘촘하게
  - 되감기(실패 규칙)
  - 화살표 겹침 해결
  - 대치 연출
  - 되감기 연출(실제로 거꾸로)
- **B26~B29 무사 결투 개편:**
  - 74박 단계 구조, 판정 45개
  - 내 공격 원, 자세 게이지, 단계별 되감기
  - Cinemachine 1인칭 카메라 2대와 3인칭 컷
- **B30 판정·실패 연출:** NORMAL ±240 ms(PERFECT ±90 그대로), 결투·윈드밀 실패 시 0.7초 피격 연출(피 튐) 뒤 되감기.
- **B32~B35 움직이며 싸우는 전투:**
  - 판정마다 이동하는 결투 원, 일반 적 돌진
  - 포위 적 회전·뛰어듦
  - 무사 자리 바꾸기·옆차기 다운(바닥 시점)·컷 각도 다양화
- **사용자 확인:** 테스트 패키지 1~7을 만들어 사용자가 플레이로 확인했다.

## 6. 꼭 지킬 규칙 (CLAUDE.md 요약)
- `docs/GDD.md` 수정 금지(잠김). 기획 변경은 사용자와 정해 `docs/GDD-beta.md`에 적는다.
- 씬·프리팹 YAML 직접 편집 금지 → `Assets/Editor/SceneBuilder*.cs`로 생성한다.
- 수치는 `Assets/Scripts/Core/GameConfig.cs` 한 곳에 둔다.
- 작업 완료 판정은 evaluator 서브에이전트의 PASS로만 한다. 작업이 끝나면 log 기록과 커밋·push를 한다.
- 커밋은 `git add`를 따로 한 뒤 `harness/.commit-msg.txt` + `git commit -F`로 한다. force push는 금지다.
- **`git add -A` 대신 파일을 지정해 스테이징한다.** 예전에 테스트 패키지 폴더가 커밋된 적이 있다. 커밋 전에 `git diff --cached --stat`으로 확인한다.
- 비용이 드는 일(VARCO 포인트, 유료 결제)과 공개 범위 변경은 사용자 승인 후에만 한다.
