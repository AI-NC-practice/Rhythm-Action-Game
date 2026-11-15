# 작업 인계 문서 (beta1-1)

- 작성: 2026-11-15 (다른 컴퓨터로 옮기기 전 정리)
- 브랜치: **`beta1-1`** (GitHub `AI-NC-practice/Rhythm-Action-Game`, 비공개). `main`에는 개발자 모드까지만 들어가 있다.
- 작업 목록의 진실은 언제나 `harness/tasks.json`이다. 작업별 기록은 `harness/log.md`, 평가 보고서는 `harness/reports/`에 있다.
- 마지막 전체 검증(B51): EditMode 165/165, PlayMode 144/144 통과. BuildGray 빌드와 smoke-test도 통과했다.
- 마지막 테스트 빌드는 GitHub Releases의 **`beta1-1-b51`**에 zip으로 올렸다. 압축을 풀고 `RhythmActionGame.exe`를 실행하면 된다.

---

## 1. 다른 컴퓨터에서 이어서 시작하기

1. **설치할 것**
   - Unity Hub와 **Unity 6000.4.0f1** (Windows Build Support 포함)
   - Git, GitHub CLI(`gh`, 릴리스를 올릴 때만)
   - **Python 3** + `numpy`, `scipy`. 곡과 히트사운드를 다시 만들 때만 필요하다. 지금 쓰는 버전은 Python 3.14, numpy 2.4, scipy 1.17이다.
     ```bash
     pip install numpy scipy
     ```
   - (선택) Blender. 에셋 단계에서 쓴다.
2. **저장소 받기**
   ```bash
   git clone https://github.com/AI-NC-practice/Rhythm-Action-Game.git
   ```
   ```bash
   cd Rhythm-Action-Game
   ```
   ```bash
   git checkout beta1-1
   ```
3. Unity Hub에서 프로젝트를 한 번 연다. 처음 열 때 `Library/`를 만들고 패키지(Cinemachine 3.1.7 등)를 받으므로 인터넷이 필요하다. 다 열리면 **Unity Editor를 닫는다.** 에디터가 열려 있으면 아래 배치 명령이 실패한다.
   - 새 `Library`로 처음 배치 명령을 돌리면 shadergraph 컴파일 오류가 한 번 날 수 있다. 다시 돌리면 해결된다.
4. **동작 확인** (프로젝트 루트에서)
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.SceneBuilder.BuildAll
   ```
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-verify.ps1
   ```
   결과는 `harness/reports/verify-latest.md`에 요약된다.
5. **테스트 빌드**
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.BuildScript.BuildGray
   ```
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/smoke-test-build.ps1
   ```
   빌드는 `Builds/Gray/RhythmActionGame.exe`에 생긴다. `Builds/`는 git에 올리지 않으므로 공유할 때는 GitHub Releases를 쓴다.
6. **Claude Code로 이어서 하기:** 프로젝트 폴더에서 Claude Code를 열고 다음처럼 말한다.
   > `docs/HANDOFF.md` 읽고 이어서 진행해

   작업 루프는 `/run-tasks`다.
   - Claude의 개인 메모리는 컴퓨터마다 따로라서 넘어가지 않는다. 필요한 것은 아래 7장 "사용자 선호"에 적어 두었다.

## 2. 지금 멈춘 곳

- **B51까지 완료**(커밋 `132d5bb`). 사용자가 B51 테스트 빌드를 아직 직접 쳐 보지 않았다. 다음 세션은 **먼저 사용자에게 B51 플레이 소감을 묻는다.**
  - 장구 노드: Q = 궁편 "덩", W = 채편 "덕". 화면 아래 장구 레인에서 흘러온다.
  - 장애물·전투: 방향키, 판정 원.
  - 장구 노드는 장애물·전투 판정과 1박 이상 떨어져 나온다. 전체 판정은 344개다.
  - 지켜볼 것: Q 69개, W 137개로 W가 많다. 한 손만 바쁘다고 하면 `GameConfig.Jangdan`의 장단 표를 조정한다.
- **B10(음악)은 `needs_user` 상태로 남아 있다.** 곡은 사용자가 "지금 노래는 좋은데"라고 승인했다. 상태를 `done`으로 바꿀지 사용자에게 한 번 확인한다.
  - acceptance의 "Suno"는 사용자 결정으로 무료 공유마당 음원 조립으로 바뀌었다.
  - 추임새 자리 8곳은 비어 있다. 마음에 드는 무료 음원이 없었다.

## 3. 앞으로 할 일 (순서대로)

| ID | 종류 | 내용 | 주의 |
|---|---|---|---|
| B45 | Unity | 박 펄스 + 예고음 + 발걸음·카메라 박 맞춤 (GDD-beta B17.2) | 제안서 `docs/proposals/rhythm-feel-2.md`, 사용자가 "순서대로" 승인 |
| B46 | Unity | 멈춤 패턴 "합" 주고받기 (B17.3) | |
| B47 | Unity | 슬로모션 체감: 음악 먹먹하게 + 심장박동 (B17.4) | |
| B48 | Unity | 전투 소리 실제 녹음 + 박에 맞춘 쉭·챙 (B17.5) | **음원 다운로드는 파일명·출처·용량을 말하고 승인받은 뒤에 한다** |
| B49 | 사용자 | 리듬감 2 최종 확인 + 테스트 빌드 | |
| T10 | 에셋 | 정적 에셋 7개 (VARCO 3D) | **포인트 비용이 들므로 사용자 승인 후.** 모든 varco-3d 호출에 `flowId`(asset_specs의 defaults.flow_id)를 넣는다 |
| T11 | 에셋 | 캐릭터 4개 + 리깅 + VARCO 동작 | T자 자세 → Rig → Animate → FBX |
| T12 | 에셋 | Mixamo 동작 받기 | 로그인은 사용자, 다운로드는 Claude |
| T13 | Unity | 툰 셰이딩 + 1인칭 전신 플레이어 교체 | |
| T14 | Unity | 적 모델 교체 + 전투 애니메이션 연결 | |
| B40 | 에셋 | 실제 효과음으로 교체 (칼·바람·쓰러짐 등) | 사용자 요청: 적을 쓰러뜨릴 때 나는 소리가 별로라고 함 |
| T15 | Unity | 맵 모델 교체 + 분위기 + 최종 빌드 | BuildFinal에는 개발자 모드가 빠진다 |
| T16 | 사용자 | 최종 플레이 확인 | |

### 미뤄 둔 것 (사용자 결정 필요)
- 추임새 음원(사용자가 직접 녹음하거나, 나중에 좋은 음원을 찾으면 `harness/music-src/chuimsae/`에 넣는다)
- 이펙트를 더 화려하게 다듬기 (에셋 단계, GDD-beta B12.3)
- 자리 바꾸기 어깨 너머 컷, 공격 카메라 거리

## 4. 음악·차트 파이프라인 (곡을 다시 만들 때)

차트(`Assets/Resources/Charts/stage1.json`)나 장단 표를 바꿔 판정 박자가 달라지면 곡도 다시 만든다. 곡이 판정 박마다 장구를 치기 때문이다.

1. 원본 샘플은 저장소의 `harness/music-src/demo/`에 있다. 출처는 공유마당이고, 태평소·꽹과리는 CC BY, 장구는 공공누리 1유형이다. 출처 표기는 `docs/CREDITS.md`에 있다.
2. 장구 원본 mp3를 wav로 바꾼다. Windows Media Foundation을 쓴다. 새 컴퓨터에는 `Builds/MusicPreview` 폴더가 없으니 먼저 만든다.
   ```bash
   mkdir -p Builds/MusicPreview
   ```
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/music/mp3towav.ps1 -In "harness/music-src/demo/전통악기_장구_치다_보통속도_Ver.1_MKH418_ST_192.mp3" -Out Builds/MusicPreview/janggu_mid.wav
   ```
3. 오토플레이로 판정 타임라인을 뽑는다(약 157초). `Builds/Gray`를 먼저 빌드해야 한다.
   ```bash
   Builds/Gray/RhythmActionGame.exe -batchmode -startGame -autoplay -logFile Builds/autoplay.log
   ```
   ```bash
   python harness/scripts/music/parse-timeline.py Builds/autoplay.log harness/music/stage1_timeline.json
   ```
4. 곡을 만들고 mp3로 바꾼다.
   ```bash
   python harness/scripts/music/make-song.py Builds/MusicPreview/janggu_mid.wav harness/music/stage1_timeline.json Builds/MusicPreview/stage1_song_chart.wav
   ```
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/music/wav-to-mp3.ps1 -In Builds/MusicPreview/stage1_song_chart.wav -Out Assets/Audio/Music/stage1_song.mp3
   ```
5. 곡 길이(지금 404박)나 챕터 시작 박이 바뀌면 `GameConfig.Music.LoopBeats`, `ChapterSongBeats`와 B10·B42 테스트 기대값을 함께 고친다.
6. 히트사운드는 `python harness/scripts/music/make-hitsounds.py <장구 wav> Assets/Audio/Hit`로 만든다.

## 5. 개발자 도구 (회색 빌드·에디터 전용, 최종 빌드에는 없음)
- **숫자 1~6:** 그 챕터 처음에서 시작한다(6 = 성문 위 무사 결투).
- **F1:** 연출 전체를 켜고 끈다.
- **판정 기록:** `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Rhythm-Action-Game\Player.log`의 `[판정기록]`
- **실행 인자:**
  - `-devChapter N`
  - `-autoplay`
  - `-autoplayMiss N`
  - `-autoplayShots 폴더`
  - `-autoplayShotsRange 시작 끝`
  - `-startGame`: 타이틀을 건너뛴다.
  - 오토플레이 로그에는 판정, 상태, 챕터 시작, 슬로모션이 남는다.

## 6. beta1-1에서 한 일 (요약, 자세한 것은 `harness/log.md`)
- **B01~B09:** 판정 UI, 사건 18종, 차트, 연출 기반
- **B11~B35:** 판정 타이밍, 되감기, 무사 결투 개편(Cinemachine 카메라), 움직이며 싸우는 전투
- **B10 음악:** 처음에는 Suno로 하려 했지만 유료라 접었다. 공유마당 곡 #197은 사용자가 거절했다. 지금 곡은 국악기 샘플을 코드로 조립한 퓨전 국악 액션곡이다. 곡은 `harness/scripts/music/`의 파이프라인으로 만든다.
  - BeatClock이 dspTime으로 곡과 판정을 맞춘다.
  - 곡은 404박마다 반복된다.
  - 50 ms 넘게 어긋나면 다시 맞춘다.
  - 속도가 바뀌어도 음높이는 그대로 유지한다(PitchShifter).
  - 챕터를 되감으면 곡도 그 챕터 위치부터 다시 튼다.
- **B41~B42:** 차트를 반 박으로 쪼개고, 차트 판정에 맞춰 곡을 다시 만들었다.
- **B44:** 노드를 맞추면 나는 소리를 실제 장구 녹음으로 바꿨다. 이른 PERFECT는 박에 맞춰 늦춰 울린다.
- **B50:** 손이 바쁜 리듬게임을 만들었다.
  - 달리기 구간에 장단 노드를 넣었다.
  - 일반 적 연속기를 4회로 늘렸다.
  - 음악 음량을 0.45로 낮췄다.
- **B51:** 장구 키를 Q·W로 분리했다. 장애물과는 1박 이상 띄운다. 표시는 아래 장구 레인이다.
  - 사용자가 위쪽 한 줄 원 방식은 더 어렵다고 해서 아래 레인으로 되돌렸다.

## 7. 사용자 선호 (Claude 메모리 대신)
- **항상 한국어로 대답한다.** 영어로 답해 불편하다는 피드백이 있었다.
- 리듬감을 중시한다. 박을 쪼갠 패턴은 좋다. 히트사운드가 곡의 한 타처럼 들리기를 원한다.
- 추임새로 소리를 지르는 기합은 싫다고 했다.
- 노드가 화면 여러 곳에 동시에 나오면 따라가기 어렵다고 했다.
- 비용(VARCO 포인트, 유료 결제), 다운로드, 공개 범위 변경은 사용자 승인 후에만 한다.

## 8. 꼭 지킬 규칙 (CLAUDE.md 요약)
- `docs/GDD.md`는 잠겨 있어 수정하지 않는다. 기획 변경은 사용자와 정해 `docs/GDD-beta.md`에 적는다.
- 씬·프리팹 YAML은 직접 편집하지 않는다. `Assets/Editor/SceneBuilder*.cs`로 만든다.
- 수치는 `Assets/Scripts/Core/GameConfig.cs` 한 곳에 둔다.
- 작업 완료는 evaluator 서브에이전트가 PASS를 줘야 한다. 작업이 끝나면 log에 기록하고 커밋·push한다.
- 커밋은 `git add`를 따로 실행한 뒤 `harness/.commit-msg.txt` + `git commit -F`로 한다. force push는 금지다.
- `git add -A` 대신 파일을 지정해 스테이징하고, 커밋 전에 `git diff --cached --stat`으로 확인한다.
- `harness/tasks.json`은 PowerShell `ConvertTo-Json` 형식(LF, BOM 없음)을 유지한다. Python으로 다시 쓰면 파일 전체 형식이 바뀐다.
