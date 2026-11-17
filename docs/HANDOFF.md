# 작업 인계 문서 (beta1-1)

- 작성: 2026-11-16, **마지막 갱신: 2026-11-17 14:00** (새 컴퓨터에서 이어서 작업, 제출 빌드 완료)
- 브랜치: **`beta1-1`** (GitHub `AI-NC-practice/Rhythm-Action-Game`, 비공개). `main`에는 개발자 모드까지만.
- 작업 목록의 진실은 언제나 `harness/tasks.json`. 작업별 기록은 `harness/log.md`(맨 아래가 최신). 평가 보고서는 evaluator가 돌려준 결과 — log.md에 요약.
- **지금 상태 한 줄:** done 96개(오늘 R26~R36 11개 포함). 남은 작업 14개(사람 확인 2·blocked 1 포함, 아래 "할 일"). 마지막 전체 검증 EditMode 208/208, PlayMode 211/212(실패 0, 건너뜀 1 = Explicit 리뷰 캡처).
- **제출 빌드:** 루트 `FFinalProject.zip`(2026-11-17 13:49, 422 MB, 안에 `FFinalProject/RhythmActionGame.exe`). FullHD 1920×1080 전체 화면 창. smoke test 통과. **git에 없음**(`.gitignore`: `FFinalProject.zip`, `FFinalProject/`, `tempFinal.zip`, `tempFinal/`) — 다시 만들려면 0장 3번.
- **아직 아무도 직접 못 본 것:** 빌드 exe를 창으로 띄웠을 때 시작화면(R36). 에디터·배치 모드·smoke test에서는 시작화면 없이 바로 게임이 시작된다(의도).

## 0. 다음에 바로 할 일 (순서대로)

1. 새 컴퓨터면 1장 절차(Unity Hub 라이선스 활성화 먼저 — 안 하면 배치 모드가 코드 198) → `HumanoidImport.ConfigureAll` → `SceneBuilder.BuildAll` → `unity-verify.ps1`.
2. **사용자가 FFinalProject.zip을 플레이해 보고 줄 피드백**을 받는다 (사용자: "많이 좋아지긴 했는데 아직도 아쉽다 — 나중에 이야기"). 아래 "사용자 확인 대기"도 같이 묻는다.
3. 빌드·패키징: `BuildScript.BuildFinal` → `smoke-test-build.ps1` → 루트에 `FFinalProject.zip`(안에 `FFinalProject/` 폴더)로 압축. **커밋 전에 스테이징 목록에 빌드 폴더·20 MB 넘는 파일이 없는지 확인**(오늘 두 번 사고 — 사용자가 루트에 zip을 풀어 둔 폴더가 커밋에 들어가 push 거부).
4. 남은 작업은 아래 "할 일" 표 순서 추천. evaluator는 **하나씩**(Unity 배치가 끝난 뒤 다음 — `Get-CimInstance Win32_Process`로 이 프로젝트 Unity가 0개인지 확인).

### 사용자 확인 대기 (직접 해 보고 알려 주기로 함)
- 뒤돌아보기 2·3·4번째 컷 1.5박 (GDD-beta B21은 2박, 바로 뒤 전투 때문에 줄임).
- 2·3·5챕터 뒤돌아보기 컷과 장구 노트 1~2개 겹침 (GDD-beta B19.1은 1챕터만 예외).
- R25: 체력 0 챕터 되감기도 "챕터 시작 점수 − 1000", 랭크 경계(SSS 97·SS 93·S 88·A 80·B 70·C 60·D 50 %) — Claude 제안.
- R30: 매트릭스 회피 때 화면 대부분이 하늘 (방향감).
- R26: 콤보 5·10·20·50 단계 이펙트 방법(사전 확인 없이 구현).
- R36: 시작화면 진입로·카메라·옮겨 가는 속도(1.6~3초) 느낌.

## 2026-11-17 한 일 (새 컴퓨터, 09:20~14:00)

| 작업 | 내용 | 평가 |
|---|---|---|
| 검증 실패 3건 | R06 포위 전투 화면 가림(막기 손 자리 `SwordGuardHand` 0.2·−0.22), 회피 목 단면 1프레임(`PlayerBodyLateVisibility` — 모든 LateUpdate 뒤 목 규칙 다시), 지붕 발차기 착지 못 함(`EnemyActor.LiftToClear` 위 속도 상한 10 m/s·실제 낙하 시간·수평 75%) | 전체 검증 통과 |
| R26 | 콤보 단계 이펙트 | PASS (2회차) |
| R27 | 칼 쥐기·막기·베기·불꽃 | PASS (2회차) |
| R28 | 칼 든 대기 자세 — 플레이어 대기 검사를 필수로, 무사·병사·플레이어 앞모습 미리보기 | PASS (2회차) |
| R29 | 뒤돌아보기 — 지붕 위 컷에도 쫓는 무리, 발 높이는 빌드 때 보이는 기와로 잰 `LookBackCut.roofY` | PASS (3회차) |
| R30 | 매트릭스 회피 칼날 | PASS |
| R31 | 윈드밀 3인칭 컷·쓰러진 적·칼 날아감 | PASS |
| R32 | 5챕터 돌진 적 엇갈림 | PASS |
| R33 | 벽타기 막는 수레·독 | PASS |
| R34 | 처형 팔 가림 — 일반 전투 4곳 줄이기 전·후 측정(`harness/reports/BundleE/R34_cover.md`: 10% 넘게 가린 시간 0.96 s → 0.12 s) | PASS (2회차) |
| FullHD | `GameConfig.Display` 1920×1080, `BuildScript` PlayerSettings + `DisplaySetup`(실행 시작 때) | — |
| R35 | 시작화면 지붕 위 끝없이 달리기(러닝머신 재사용) + 시작 시 떨어지며 게임 | PASS — **R36으로 대체**(사용자: "실제 게임이 시작되는 느낌"). Title 씬·`TitleRunner`는 테스트용으로 남음 |
| R36 | **시작화면 = 게임 씬 (A안)**: `AttractMode` — 메뉴 동안 시작선 뒤 진입로(`SceneBuilder.Attract.cs`)를 3인칭 뒤 따라가기로 달림, 시작 → 계속 달려 카메라가 1인칭으로 → 시작선에서 1챕터 (끊김 없음). 일시정지·결과 "타이틀로"도 여기로. 빌드 첫 씬 = Game | PASS (2회차) |

### 오늘 구조에서 바뀐 것 (다음 작업자가 알아야 할 것)
- **빌드 첫 씬이 Game**(`EditorBuildSettings`: Game, Title). `AttractMode.AttractOnLoad`가 빌드 실행(에디터·배치 모드·`-startGame`·`-devChapter` 아님)과 "타이틀로"에서 켜진다. `GameBootstrap.Start`가 켜져 있으면 `attract.Begin()`, `StartRun`은 시작화면 중이면 먼저 끝낸다. 테스트에서 시작화면을 보려면 `AttractMode.AttractOnLoad = true` 후 Game 로드(`R36_AttractPlayTests`).
- `PlayerBody.CapExecutionEnabled`(기본 켬) — R34 기준선 측정용 스위치. `PlayerBody.LateVisibilityCheck` + `PlayerBodyLateVisibility`(실행 순서 30000).
- `LookBackCut.roofY/roofD0/roofN/roofStep` — `SceneBuilder.CheckSupports`가 채움(지붕 위 컷 구간 0.25 m 간격·무리마다).
- `GameConfig.Title`(R35), `GameConfig.Attract`(R36), `GameConfig.Display`, `GameConfig.EnemyBody.KickOffRoofMaxLift/HorizontalShare`, `GameConfig.Fx.PursuerRoofLateralScale`.
- 알려진 흔들림: `R07_EventFigures_RunWhileMoving_NoGlide`에서 3챕터 Enemy_02_sword가 가끔 뜸 허용치 초과(616 중 17, 허용 12) — 다시 돌리면 통과.

## 할 일 (남은 작업 14개, tasks.json 기준)

| 순서 추천 | 작업 | 내용 | 비고 |
|---|---|---|---|
| 1 | **R11** [사용자] | 다시 플레이 확인 | 사용자 피드백 → 새 작업으로 나눔 |
| 2 | R13 | 낮은 처마 장애물을 지붕 위에서 말이 되는 모양으로 | 연출 |
| 3 | R14 | 닫히는 대문 아슬아슬 통과 동작 + 문 양옆 막기 | 연출 |
| 4 | R22 | 4장 광장이 비어 보임 (쓰러진 적이 남는 부분은 R31로 해결) | 연출 |
| 5 | R24 | 지붕 위 복선 적이 오래 가만히 서 있지 않게 | 연출 |
| 6 | R10 | 최적화: LOD + 임포스터 + GPU Resident Drawer + 최종 빌드 (+ GitHub Releases — 공개·업로드는 승인 필요) | 아래 "최적화 현황" |
| 7 | R21 | 마을 밖 빈 평지와 배경 그림 이음 | 미룸 |
| 8 | R08 | 슬로모션 성공/실패 차이 + 리듬감 다듬기 | 미룸 |
| 9 | R09 | 전투 동작·테스트 다듬기 | 미룸 |
| 10 | R18e | 시민 3종 (VARCO 약 900포인트 승인됨) | 미룸 |
| 11 | R12 | 효과음·환경음 (VARCO Sound) | 비용 견적·승인 |
| 12 | B40 | 실제 효과음으로 교체 | 미룸 |
| — | T16 [사용자] | 최종 플레이 확인 | needs_user |
| — | B46 | "합" 주고받기 | blocked(사용자 취소, git stash에 보관) |

### 최적화 현황 (R10 참고)
- **한 것:** 머티리얼 공유(SRP Batcher)·툰 셰이더 GPU 인스턴싱·맵 정적 배칭, 길가 장식 `DressingInstancer`(RenderMeshInstanced, 충돌체 없음) + 칸 컬링(건물 320 m·소품 40 m), 그림자 캐스케이드 4→2·먼 줄/작은 소품 그림자 끔, 작은 소품 70 m 밖 안 그림. 그리기 호출 780 → 294(T15) → 맵 재구성 뒤 약 365(사용자 승인, 테스트 기준 +30%). VARCO 고폴리 → Remesh(기와집 3만 → 5천 등), 먼 배경은 파노라마 한 장. 무거운 계산은 빌드 때(적 발밑 프로필·높이 격자·숨은 자리·발차기 착지·뒤돌아보기 지붕 높이). 시작화면은 이미 불러온 게임 맵을 그대로 씀(씬 다시 불러오기 없음).
- **안 한 것:** LOD·임포스터, GPU Resident Drawer·오클루전 컬링, 지붕 길·골목 담 인스턴싱, 텍스처 아틀라스, 고폴리 노멀 맵 굽기.

## 지난 상태 기록 (2026-11-17 08:45, 이전 컴퓨터)
- 묶음 A(R19·R20·R23·R16·R15)와 R25 done, 묶음 E(R26~R34) 구현 끝 → 위 표대로 오늘 모두 done.
- R27: 막기는 한 손 가로막기(손목을 꺾지 않는 범위). 두 손 막기는 왼손 방향을 맞추는 IK가 필요해 뺐다.

## 1. 다른 컴퓨터·다른 계정에서 이어서 시작하기

1. **설치할 것**
   - Unity Hub와 **Unity 6000.4.0f1** (Windows Build Support 포함). 새 컴퓨터에서는 **Unity Hub에서 라이선스를 먼저 활성화**한다(안 하면 배치 모드가 코드 198로 끝난다).
   - Git, GitHub CLI(`gh`, 릴리스를 올릴 때만)
   - **Python 3** (+ `numpy`, `scipy`는 곡·히트사운드를 다시 만들 때만, `Pillow`는 미리보기 모음·배경 그림). `harness/scripts/varco/spec-set.py`, `harness/scripts/sfx/prep-mixkit.py`는 표준 라이브러리만 쓴다.
     ```bash
     pip install numpy scipy pillow
     ```
   - **Blender 5.2 이상** (에셋 다시 굽기·검사·미리보기). 경로 `C:\Program Files\Blender Foundation\Blender 5.x\blender.exe`(project.json은 5.2 경로 — 다르면 고친다). 없으면 에셋 단계만 못 한다.
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
3. Unity Hub에서 프로젝트를 한 번 연다. 처음 열 때 `Library/`를 만들고 패키지(Cinemachine, glTFast 등)를 받는다. FBX·GLB 임포트에 시간이 꽤 걸린다. 다 열리면 **Unity Editor를 닫는다.** 에디터가 열려 있으면 아래 배치 명령이 실패한다.
   - 새 `Library`로 처음 배치 명령을 돌리면 shadergraph 컴파일 오류가 한 번 날 수 있다. 다시 돌리면 해결된다.
4. **캐릭터 Humanoid 설정과 씬 만들기** (프로젝트 루트에서, 순서대로)
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.HumanoidImport.ConfigureAll
   ```
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.SceneBuilder.BuildAll
   ```
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-verify.ps1
   ```
   - 결과는 `harness/reports/verify-latest.md`. 전체는 10분 남짓 걸린다(PlayMode가 무거움). 작업 중에는 `-Platform PlayMode -Filter 'R07_|B32_'`처럼 일부만.
   - `.meta`에 임포트 설정이 들어 있어 ConfigureAll은 보통 안 해도 되지만, 클립이 이상하면 한 번 돌린다(Mixamo 12개 포함).
   - SceneBuilder.BuildAll은 R07부터 지지면·높이 격자를 계산해 몇 분 걸린다.
5. **빌드**
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.BuildScript.BuildFinal
   ```
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/smoke-test-build.ps1
   ```
   - `Builds/Final/RhythmActionGame.exe` (약 290 MB, 개발자 모드 없음). 개발자 모드가 있는 빌드는 `BuildGray` → `Builds/Gray`.
   - `Builds/`는 git에 올리지 않는다. 공유할 때는 GitHub Releases.
6. **Claude Code로 이어서 하기:** 프로젝트 폴더에서 Claude Code를 열고 다음처럼 말한다.
   > `docs/HANDOFF.md` 읽고 이어서 진행해

   작업 루프는 `/run-tasks`다. **Claude의 개인 메모리는 계정·컴퓨터마다 따로라서 넘어가지 않는다.** 필요한 것은 아래 8장 "사용자 선호"에 모두 적었다. 새 계정에서는 VARCO MCP(`varco-3d`) 인증을 다시 해야 한다(`/mcp`, 사용자).

### 이 컴퓨터에만 있고 git에 없는 것 (없어도 진행 가능)
- `harness/reports/varco/*_raw/`(동작 FBX 원본), `harness/reports/varco/v2_high/`, `harness/reports/varco/r02/high/`(고폴리 GLB), VARCO 이미지 원본(`r02/*.png`) — 용량 때문에 `.gitignore`. 게임에 쓰는 결과물(`Assets/Art/Models/`)은 git에 있다. 다시 필요하면 VARCO 플로우에서 다시 받는다(`harness/reports/varco/t11-<id>.txt`의 해시, URL `https://3d.varco.ai/api/objects/<해시>.fbx`, 만료됐으면 `get_output_downloads`).
- `git stash`에 B46("합" 주고받기, 사용자가 취소) 구현 — 필요 없음.
- `Builds/` 빌드 — 5번으로 다시 만든다.

## 2. (지난 기록, 2026-11-17 08:45) 묶음 E 계획 — 지금 상태는 맨 위 표

- 마지막 커밋: 이 문서 갱신 커밋(묶음 E 칼 쥐기 마지막 수정 포함). 그 앞 `ae3038f`(묶음 E 구현), `ff4bcb6`(R25), `a71bdc3`(R15), `f2a387b`(묶음 A).
- tasks.json: done 86 (R15·R16·R19·R20·R23·R25 포함), pending — 묶음 E R26(시도 1)·R27(시도 1)·R28~R34(평가 전), R13·R14·R21·R22·R24, R18e·R08·R09·R12·B40, R10, 사람 R11·T16, blocked B46(취소).
- **작업 방식 (2026-11-17 사용자 요청, 마감):** 몇 개씩 묶어 한 번에 구현 → 전체 검증 한 번 → evaluator는 **하나씩**. 커밋은 묶음 끝에.
- 씬 생성 몇 분, 전체 검증 약 15분, evaluator 하나 약 5분(각자 전체 검증을 다시 돌림).

### 묶음 E (2026-11-17 오전 빌드 플레이 피드백) — 무엇을 했나
| 작업 | 사용자 말 | 한 것 | 상태 |
|---|---|---|---|
| R27 | 칼이 손에 꽂혀 있다, 손가락으로 쥐어야 한다(사진 기준), 내 칼로 같이 싸우기 | 플레이어 칼을 오른손 뼈에. 적 3종 VARCO "humanoid-fingers"로 다시 리깅(0포인트) → `<id>_rig_fingers.fbx`. 칼 붙일 때 손가락을 쥔 값으로 굽혀 **굽은 손가락 12마디 고리 가운데**에 칼자루(EnemyRig.AttachWeapon). FingerGrip(적)·PlayerBody.ApplyGrip(플레이어 두 손)이 런타임에 손가락을 쥔 값으로. 전투 중 1인칭에 칼 보임(DuelCamera가 끄던 것 고침), 막기(↑) = 손 IK로 눈앞 가로막기 + 손목 회전, 베기 = Mixamo Power Slash, 칼끼리 닿는 곳 불꽃 | 평가 1회차 FAIL(막기 때 칼 안 보임·불꽃 없음) → 고침. 손가락 모양은 사용자가 3번 지적 → 손 확대(BundleE/R27_hand_*.png, R27_block_*.png, finger_*.png)로 확인함. **재평가 필요** |
| R28 | 공격 뒤 팔 벌리고 서 있음 | 대기 = Mixamo Great Sword Idle(무사·병사·플레이어) | 평가 전 |
| R29 | 뒤돌아보기 휙휙, 따라오는 적 없음 | 2박(1.5박), 돌아오기 더 천천히, 땅 위 컷에 쫓는 무리 3명 | 평가 전 |
| R30 | 매트릭스 자세면 칼이 얼굴 앞을 스치게 | 전투 회피 슬로에서 적 칼 복사본이 눈앞 0.4 m를 가로지름 | 평가 전 |
| R31 | 윈드밀 때 내 발 안 보임, 쓰러진 병사 팔 벌리고 칼 떠 있음 | 윈드밀 3인칭 컷, 쓰러짐은 클립으로(몸 전체 회전 없앰 — 원인), 칼이 손에서 빠져 날아가 바닥에 | 평가 전 |
| R32 | 5챕터 돌진 적 같은 높이로 어떻게 피했나 | 적은 1.4 m 옆으로 비켜 오고 나는 반대로 낮춰 베며 엇갈림 | 평가 전 |
| R33 | 벽 탈 이유가 없다 | 벽타기 4곳에 길 막는 뒤집힌 수레·독 | 평가 전 |
| R34 | 적 쓰러뜨린 뒤 팔이 화면을 오래 가림 | 처형 동작 1.5박만 | 평가 전 |
| R26 | 콤보 단계마다 다른 이펙트 | 5·10·20·50 단계별 색·크기·빛 테두리·튐 | 평가 1회차 FAIL(BeatFx가 크기 덮어씀) → 고침, **재평가 필요** |

### 이전 마감 계획 (참고)
| 묶음 | 작업 | 상태 |
|---|---|---|
| A 빠른 수정 | R19·R20·R23·R16·R15 + 회색 네모(파티클 텍스처) | **done** |
| (추가) 점수 개편 | R25 (GDD-beta B20) | **done** |
| B 최종 빌드 안전판 | BuildFinal + smoke | 06:19 완료(R25까지). 묶음 E 반영 빌드는 아직 |
| C 연출 다듬기 | R13·R24·R22·R14 | 남음 (R22의 "쓰러진 적이 사라짐"은 R31로 원인 해결 — 남은 건 "광장이 비어 보임") |
| D 미루기 | R18e·R21·R08·R09·R12·B40 | 남음 |
| 마지막 | R10 빌드·Releases, R11·T16 | 남음 |

## 3. 사용자 피드백과 처리 상태

### 첫 플레이 (2026-11-16, Builds/Final) — R01~R12로 나눔
| # | 사용자 말 | 처리 |
|---|---|---|
| 1 | 공중에 떠 있는 사람들 | R03(시작 자리) → **R07·R07 후속에서 실제로 해결**: 달리는 동안 매 프레임 보이는 메시 기준 검사, 모든 적 뜬 프레임 0 |
| 2 | 실패 시 머리 어디 감? | **R06**: 실패·피격 동안 화면용 몸 숨김(1인칭 실패 연출) |
| 3 | 철봉·허들 | R04 모델 교체 (단, 지붕 위 낮은 처마가 대문처럼 보임 → R13) |
| 4 | 적이 유령처럼 다가옴 | **R07**: 이동 속도에 맞춘 달리기 |
| 5 | 경단 | R04 |
| 6 | 벽타기를 옆으로 | **R06**: 45° 눕혀 벽을 밟고 달림 + 카메라 기울기 |
| 7 | 아래로 피하기 화면 가림 | **R06**: 0.86 → 0.12 |
| 8 | 맵 조잡 | R03 |
| 9 | LOD·ISM | R10 |

### 에디터 플레이 (2026-11-16 밤) — R13~R16
1 건물이 너무 많음(기획, 나중에) · 2 지붕 위 건물 → R13 · 3 적이 보이는데 담을 넘음(기획) · 4 4장 포위 전투 화면 가림 → **해결**(몸이 카메라 방향을 따라 돎) · 5 대문 → R14 · 6 되감기 위치 → R15 · 7 제자리 달리기 → R16(내 몸), R07(적)

### 에디터 플레이 (2026-11-17) — R07 후속 + R17·R18
1 마지막 무사가 공중을 걸음 → **해결**(지붕 위 전투 높이 격자) · 2 2구간 돌진 적이 벽을 뚫음 → **해결**(실제 길 따라옴) · 3 옆에서 같이 뛰는 추격 적·서 있다 갑자기 달려오는 돌진 적·아무것도 안 하는 병사(매복 적) → **R17 제안서**

### 에디터 플레이 (2026-11-17 오전) — 추격 연출 개편·버그
| # | 사용자 말 | 처리 |
|---|---|---|
| 1 | 옆에서 같이 뛰는 적은 우리 팀? | **R18**: 옆 8 m 줄에서 쫓다 공격 2박 전 뛰어듦, 공격 뒤 뒤처짐 |
| 2 | 서 있다가 갑자기 달려오는 돌진 적 | **R18**: 서 있음→걷기(Mixamo Standard Walk)→점점 빨라져 질주 |
| 3 | 아무것도 안 하는 병사 → 시민으로 | **R18b**: 매복 적은 담·집 너머에 숨었다 2박 전 튀어나옴. 시민은 R18e(미룸) |
| 4 | 멈춰 있는 적도 나를 발견하면 뛰어와야(벽 너머에서 늦게 발견) | **R18b**: 땅 위 전투 적(92박, 4장 포위 5명) 숨음→알아챔→달려옴 |
| 5 | 앞질러서 싸우기 | **R18c**: 지붕 위 전투(128·152·240) 옆 지붕에서 앞질러 도약·공중·착지 |
| 6 | 가끔 뒤돌아보면 적이 따라옴 | **R18d**: 1·2·3·5챕터 한 번씩 1박 165° (40.5·90.5·126.5·238.5박) |
| 7 | 5챕터 벽을 뚫고 나오는 적 | **고침**: 220박 돌진 적이 궁궐 담을 비스듬히 내려옴 → 덮개 끝까지 걷고 뛰어내림 (Bug1117 테스트) |
| 8 | 2챕터 죽고 돌아간 뒤 적이 지붕 아래로 사라짐 | **고침(추정)**: 72박 돌진 적이 76박 기와 덮개 낮은 담 속을 지남 → 넘는 높이로 뜀. 사용자 화면은 직접 못 봄 |
| 9 | (전체 훑어보기, Claude) 이상한 점 6개 | **R19~R24** 작업으로 추가 (harness/reports/review/NOTES.md) |

**주의(정직하게):** R03에서 "공중에 뜬 적 해결"이라고 했지만 시작 자리만 검사해서 사실이 아니었고, R07 첫 보고("뜬 프레임 0")도 보이지 않는 충돌 상자를 지지면으로 쳐서 무사를 놓쳤다. 지금 검사는 보이는 메시로 만든 검사용 충돌체만 센다. **"고쳤다"고 하기 전에 실제 장면(미리보기 PNG)으로 확인할 것.**

### 빌드 플레이 (2026-11-17 오전, Builds/Final) — 묶음 E
| # | 사용자 말 | 처리 |
|---|---|---|
| 1 | 칼이 손에 꽂혀 있다 / 손가락으로 쥐어야(사진) / 손가락이 이상하게 꺾였다 | R27 (위 표). 마지막 확인: 굽힘 값이 음수일수록 손가락 끝이 손바닥 쪽(뒤로 안 꺾임), 이상했던 건 두 손 막기 왼손 IK → 뺌 |
| 2 | 검을 들어 같이 싸우기, 칭칭 | R27 막기·베기·불꽃 |
| 3 | 팔 벌리고 서 있는 검사 | R28 |
| 4 | 뒤돌아보기 휙휙, 따라오는 적 없음 | R29 |
| 5 | 매트릭스 자세 칼 스침 | R30 |
| 6 | 윈드밀 발 안 보임, 쓰러진 병사 어색·칼 떠 있음 | R31 |
| 7 | 5챕터 돌진 적 피하기 어색 | R32 |
| 8 | 벽타기 이유 | R33 |
| 9 | 처형 뒤 팔이 화면 오래 가림 | R34 |
| 10 | 콤보 단계 이펙트 | R26 |
| 11 | 점수·실패 규칙 (장구 50/20+콤보 보너스, 방향키 100/50, 무사·윈드밀 초입 − 1000, 랭크 SSS~F) | R25 done, GDD-beta B20 |

### 시작화면 (2026-11-17 오후, 사용자)
| # | 사용자 말 | 처리 |
|---|---|---|
| 1 | 시작화면에서 주인공이 플로우처럼 지붕 위를 계속 달리게, 2D 무한 배경처럼 최적화, 시작 누르면 지붕에서 떨어지며 시작 | R35 (done) |
| 2 | "이런 느낌이 아니라 실제 게임이 시작되는 느낌" → A안(시작선 뒤 진입로를 달리다 그대로 1챕터), 카메라 3인칭 뒤 따라가기 | R36 (done) |
| 3 | 제출 빌드는 FullHD, 루트에 FFinalProject로 압축 | FFinalProject.zip |

## 4. 세션별 한 일

| 작업 | 내용 | 커밋 |
|---|---|---|
| B45~B53, T10~T15 | (2026-11-16 이전 세션, log.md 참고) 박 펄스, 슬로모션, 효과음, 에셋 생성, 툰 셰이딩·1인칭 몸, 적 모델·전투 동작, 맵 모델 | 여러 개 |
| R01~R05 | 맵 재설계 안·새 에셋 35개(10,800포인트)·맵 다시 짓기·회색 상자 교체·T14 테스트 | 746300b~c07ed3e |
| R06 | 1인칭: BodyOffset(몸 받침), 벽타기·미끄러지기·실패·피격 때 카메라를 머리에 붙이고 몸이 카메라 궤적과 함께, 실패·피격은 몸 숨김, 목 단면·카메라 60° 돌아감·뒤집힘·목보다 낮음이면 숨김, 벽타기 45° + 달리기 클립 + 카메라 기울기, 미끄러지기 NORMAL = duck | 5c2c7bb |
| R06 후속 | 4장 포위 전투: 몸이 카메라 yaw를 따라 돎(검은 화면 해결), 작업 R13~R16 추가 | 885b6b5 |
| R07 | 적 Run 상태(속도에 맞춰), 사건 소품 적 EnemyFigureRun, 추격 적 실제 경로·발밑 프로필, 매복 적 비탈, 지붕 발차기 Mixamo Flying Back Death + 빌드 때 고른 골목 착지 + 옆집 지붕을 넘는 포물선 + 카메라 따라봄 | e5b0ffd |
| R07 후속 | 지붕 위 전투 높이 격자·옆 범위·이음매 비키기(무사 공중 해결), 돌진 적 실제 길·높이 프로필(벽 뚫기 해결), 추격 프로필 머리 위 여유, 테스트는 보이는 메시만, 추격 연출 개편안·R17·R18 | (이 커밋) |
| R17 | 사용자 승인 A~E 전부, 시민 3종 900포인트, GDD-beta B19, R18을 R18~R18e로 나눔 | eb7f992 |
| R18 | 추격 적 옆 8 m 줄 + 뛰어들기 + 뒤처짐, 돌진 적 걷기→질주 | d858024 |
| R18 후속 | Mixamo Standard Walk(걷기), 전체 훑어보기 66곳, R19~R24 추가 | deee8c0, 2d05c8a |
| R18b | 매복 적 숨은 자리(빌드 때 시선 검사), 땅 위 전투 적 숨음→알아챔→달려옴, 궁궐 담 밑 기와집 고정 자리 | 43a53dd |
| R18c | Mixamo Running Jump·Falling Idle·Hard Landing, 지붕 위 전투 적 앞질러 착지 | 975d004 |
| R18d + 버그 | 뒤돌아보기 컷, 돌진 적 담 뚫기 2건, B17 테스트 씨앗 고정 | 5d625e3 |
| 묶음 A | R19 대문 컷 낮은 자세, R20 하늘 띠(배경 위쪽 투명), R23 길 외곽선 끔·색 대비, R16 전투 대기 자세, R15 되감기 때 자리도 되감기, 파티클 둥근 점 | f2a387b, a71bdc3 |
| R25 | 점수·콤보·랭크·되감기 패널티 (GDD-beta B20), R26 추가 | ff4bcb6 |
| 묶음 E | R26~R34 (칼 쥐기·손가락 리그·칼 든 대기·뒤돌아보기·매트릭스·윈드밀 컷·쓰러짐·돌진·벽타기 짐·처형·콤보 단계) | ae3038f + 이 인계 커밋 |
| 2026-11-17 새 컴퓨터 | 검증 실패 3건, 묶음 E 평가·보강(R28·R29·R34), FullHD, R35, R36 시작화면 = 게임 씬 | 745d2cf~53b80b0 + 이 인계 커밋 |

VARCO 포인트 누적 약 17,400 (R17~R18d는 0) (T10 2,050 + T11 1,000 + T11R 3,550 + R02 10,800). run_flow 17/45회(`harness/varco-usage.log`). 플로우 ID `harness/asset_specs.json`의 `defaults.flow_id` = `7c1eec5d-c366-4ce4-8713-0b6adb6fc1da`.
Mixamo 동작 16개(`Assets/Art/Animations/Mixamo/`, R07 flying_back, R18 walk, R18c running_jump·falling_idle·hard_landing).

## 5. 다음 작업에서 알아야 할 구조와 도구

### 에셋 파이프라인
- **VARCO:** `varco3d-asset` 스킬. 모든 호출에 `flowId`. 3D 생성 200점, 이미지 50점, Remesh·Rig·Animate 0점. 4면도 → 정면 한 장(reference) → Generate3D. 캐릭터: tPose → Remesh(목표보다 15~20% 많이 나옴) → Rig → Animate.
- **Mixamo:** 사용자가 동작을 고르고 승인 → 앱 브라우저(빌트인 브라우저)에서 사용자 로그인 → Claude가 FBX Binary·**Without Skin**·30 fps로 다운로드(`~/Downloads`) → `Assets/Art/Animations/Mixamo/<이름>.fbx` → `HumanoidImport.ConfigureMixamo`. 적 클립으로 쓰려면 `EnemyMoves`에서 `MixamoPrefix + 이름`.
- **Blender 스크립트** (`harness/scripts/blender/`, `blender-run.ps1 -Script <이름> --in ...`): `cull_bake.py`, `mesh_check.py`, `render_preview.py`, `render_closeup.py`, `strip_mesh_fbx.py`.
- **배경 파노라마:** `python harness/scripts/map/make-backdrop.py`.

### 런타임 구조 (R06·R07에서 바뀐 것 중심)
- `PlayerBody` (R06): Player/BodyOffset/Model. 동작 중(actionBlend) 벽타기·미끄러지기·실패·피격은 카메라를 머리 뼈에 그대로(따라가기 1, 늦춤 없음), 몸 받침이 StageDirector.motion 위치를 따라감(높이는 클립이). 벽타기는 옆 광선으로 벽 거리를 재서 발을 벽 면에 두고 45° 눕힘. 화면용 몸 숨김 규칙 5개(`UpdateVisibility`). 몸 받침은 카메라 연출 yaw(포위 전투)도 따라 돈다. 값은 `GameConfig.Body`.
- `EnemyAnimator` (R07): 실제 위치 변화로 수평 속도(박자 시계 시간) → Idle ↔ Run, 재생 배율 = 속도/4.5. `EnemyFigureRun`은 사건 소품 적용(같은 규칙). 값은 `GameConfig.EnemyBody`.
- `EventProp` (R07): 추격·돌진 적은 `chaseY/chaseX/chaseGap` 프로필(빌드 때 `SceneBuilder.Map.CheckSupports`가 보이는 메시로 잼)을 따라 실제 경로(`MapLayout.PointAt`) 위를 달림. 틈은 뛰어넘기, 담·지붕 오르내림은 플레이어 경로 높이에 맞춤. 매복 적은 `ambushEndY`. **추격 연출 개편(R18) 때 이 프로필을 옆 지붕 줄 거리로 다시 계산하면 된다.**
- `EnemyActor` (R07): `kickLanding`(빌드 때 고른 발차기 낙하 자리), `arenaY` 격자(지붕 위 전투 자리 보이는 면 높이, `ArenaSurface`). `KickOffRoofRoutine`은 `LiftToClear`로 옆집 지붕을 넘는 포물선.
- `StageDirector.Motion` (R07 후속): 지붕 위 전투는 `ArenaLateral`(옆 범위)·`SeamShift`(이음매 비키기, 그리기만)·발을 `ArenaSurface`에.
- `DressingInstancer.TopBelow` (R07): 인스턴싱 장식(충돌체 없음)의 경계 상자 윗면 — 실행 중 대략적인 높이가 필요할 때.
- `PresentationHooks.LookAtKickFall`: 지붕 발차기 때 CameraFx 처형 주인으로 고개 돌림(최대 35°, 연출 기록 Log에는 안 남김 — B07 테스트가 마지막 기록을 봄).
- `EventProp` (R18): 추격 적 월드 XZ 프로필 `chaseWX/WZ`(옆 8~20 m 먼 줄 → 공격 2박 전 뛰어드는 포물선 → 공격 뒤 곁, 공격 뒤 0.35배 속도로 뒤처짐). 돌진 적 `ChargeAhead/ChargeSpeed`(서 있음→걷기 0.8 m/박→4박 가속→질주 4 m/박). 매복 적 `ambushHide/ambushPathY/ambushJump`(판정 2박 전 뛰쳐나옴).
- 빌드 때 숨은 자리 찾기 (R18b, `SceneBuilder.Map.CheckSupports` 끝): 보이는 메시로 임시 충돌체(레이어 30)를 만들고 플레이어 눈(경로+1.6 m, 위아래·옆 여유)에서 시선을 쏴 가려지는 자리, 나오는 길(걷기/포물선) 막힘 검사, 매복은 나오는 모습이 화면(±35°)에 들어오는 앞쪽 자리 우선. `ChargeProfile`은 빈 칸을 넘는 높이로 뛰고 큰 높이 차는 덮개 끝까지 걷고 뛰어내림(버그 수정).
- `EnemyApproach` (R18b): 땅 위 전투 적 숨은 자리→알아챔(0.5박 몸 돌림)→달려와 멈춤 0.25박 전 도착. `EnemyLeapIn` (R18c): 지붕 위 전투 적 12박 전까지 숨김→옆 지붕 줄 앞지르기→3.25박 전 박참→1.75박 전 착지, `EnemyAnimator.PlayLeap/EndLeap`으로 클립 시각을 포물선에 맞춤(열쇠 시각은 `EnemyRig.FindHits`가 발 높이로 잼).
- `LookBackCut` (R18d, `CameraOwner.LookBack`): `GameConfig.Fx.LookBackRunBeats` 자리에서 1박 165°.
- `MapPlan`: 궁궐 담 위 매복용 `giwa_s` 고정 자리(길가 건물 줄 없는 구간도 놓음).
- 기존: `Toon.shader`, `BodyMoves`/`EnemyMoves`, `DressingInstancer`(인스턴싱·칸 컬링), `MapPlan`, `MapSupportReport`, `EventProps.ModelOverride`, 에디터 `ToonMaterials`·`EnemyRig`·`SceneBuilder.Map.cs`·`PreviewCapture`·`DrawCallReport`.

### 측정 테스트 (새로 만든 것, 다음 작업에서 재사용)
- `R06_FirstPersonViewPlayTests`: 화면 카메라와 같은 자세로 **내 몸만** 작은 텍스처에 그려 화면 가림 비율, 목 단면 보임, 결과별 미리보기(`harness/reports/R06/`).
- `R07_EnemyRunPlayTests`: 보이는 맵 메시(인스턴싱 장식 + 정적 배칭 합친 메시)에 **검사용 충돌체**를 붙이고 실제 stage1을 자동 연주하며 모든 적의 달리기·미끄러짐·제자리 달리기·**발밑(뜸)·건물 속**을 매 프레임 셈. 지붕 발차기 착지·카메라. 미리보기 `harness/reports/R07/`.

- `R18_ChaseRedesignPlayTests`, `R18b_HiddenEnemiesPlayTests`(화면 카메라 시선 가림), `R18c_LeapInPlayTests`, `R18d_LookBackPlayTests`, `Bug1117_EnemyClipPlayTests`(몸이 메시 속: 여러 방향 광선의 첫 면이 뒷면), `Review_StageWalkthroughPlayTests`(Explicit, 4박마다 1인칭·3인칭 → `harness/scripts/review-sheet.py`로 모음).

### 묶음 A·R25·묶음 E에서 바뀐 구조 (2026-11-17)
- **적 모델 바탕:** `EnemyRig.BaseFbx` = `<id>_rig_fingers.fbx`(VARCO Rig humanoid-fingers, 플로우 Rig 노드 eef304be(boss)·02f9ff3b(spear)·11113720(sword)). 기존 VARCO 동작 FBX는 Humanoid로 그대로 옮겨 씀. `EnemyRig.AttachWeapon`(internal)은 T자(Rebind)에서 손가락을 쥔 값으로 굽혀 고리 가운데에 칼자루 — **플레이어 칼도 이것을 씀**(SceneBuilder).
- **손가락:** `FingerGrip`(적·사건 적, 창병은 두 손), `PlayerBody.ApplyGrip`(두 손). 값 `GameConfig.Body.GripCurl1/GripCurl23/GripThumb`(−1 = 가장 굽힘, Min으로 더 굽힌 쪽만).
- **Mixamo 추가 3개:** `great_sword_idle`(대기, 반복), `great_sword_block`, `great_sword_slash` (`Assets/Art/Animations/Mixamo/`). 적 Idle = Mixamo great_sword_idle, 윈드밀 쓰러짐 = flying_back. 플레이어 컨트롤러: CombatIdle·SwordBlock·SwordSlash 상태, 레이어 IK Pass 켬(`PlayerSwordIK`가 막기 때 오른손 IK).
- **PlayerBody:** CombatIdle 전환(R16), 처형 1.5박 자르기(R34), 칼 보이기(전투 중 매 프레임), 막기 손목 회전(UpdateSwordGuard), 부딪침 불꽃(Clash), 3인칭 컷 중 1인칭용 몸 숨김(ThirdPersonCut).
- **BulletTimeFx:** 대문 컷 낮은 자세(R19, RequestLowestPose), 매트릭스 칼날(R30, BladeSweep), 윈드밀 3인칭 컷(R31).
- **StageDirector.Motion:** 되감기 체크포인트(R15) — 결투 단계 시작·윈드밀 1마디 앞 자리(포위는 적 5명 자리·lunge까지) 기록, 되감기 동안 거꾸로 이어 옮김.
- **ScoreBoard/ScoreKeeper:** 장구·방향키 분리(StageDirector.AnyCueJudged), 체크포인트 − 1000(RewindScorePenalty), 랭크 8단계(RankNames/RankRatios), 만점 = ScoreKeeper.MaxScoreOf(stage1 33,100).
- **EnemyActor 쓰러짐:** 모델 적은 클립이 눕힘(몸 회전 없음), TossWeapon(칼 날아감, 리셋 때 RestoreWeapon). EventProp 사건 적 PERFECT = EnemyFigureRun.PlayDefeat + 그 자리 고정.
- **LookBackCut:** 컷 길이 `GameConfig.Fx.LookBackLengths`, 쫓는 무리 `pursuers`(SceneBuilder가 만듦, 땅 위 컷만), `LengthOf(i)`, `OnRoof`.
- **Backdrop.shader:** 위쪽 투명(투명 큐 맨 앞), `_TopToFog` = 1이면 예전 모습(R20 전후 비교 테스트 전용).
- **테스트:** `BundleE_PlayTests`(R26~R34, 미리보기 `harness/reports/BundleE/`), `R27_FingerCurlDiagTests`(손가락 진단), `R19_GateBodyPlayTests`(R19·R20·R23), `R15_R16_CombatPlacePlayTests`, `R25_ScoreRulesTests`/`R25_ScorePlayTests`. 최종 자세(모든 LateUpdate 뒤)를 찍을 때는 `[DefaultExecutionOrder(32000)]` 측정기 — 배치 모드에서 WaitForEndOfFrame은 안 됨.

### 알아 둔 함정
- **발밑·지지면 검사는 보이는 메시로만.** 게임의 충돌 상자(지붕 길·성문 상자)는 보이는 모델보다 넓거나 높다(성문 0.5 m).
- PlayMode 테스트에서 시간을 주입할 때는 박자 시계, 슬로모션, Hit Stop(TimeFx.RealTime)을 모두. 실제 시간으로 진행하는 연출(발차기 낙하 궤적)은 `Time.realtimeSinceStartup`으로 기다리고, **기다리는 동안에도 자동 연주를 계속**해야 한다(놓쳐서 되감기면 적이 제자리로 돌아감).
- 실행 순서: PlayerBody LateUpdate(−50)가 StageDirector LateUpdate(0)보다 먼저라 몸이 카메라 yaw를 한 프레임 늦게 따라감(회전 중에만).
- 미리보기·측정 카메라는 자세를 정한 같은 프레임에 렌더한다.
- PlayMode 테스트에서 시간을 주입할 때 `CameraFx.DeltaTime`도 (실제 프레임 시간이면 카메라 연출이 박자보다 길게 남는 착시).
- 셸 명령에 `cat >> /dev/null` 같은 표준 입력 읽기가 섞이면 멈춘다(그 뒤 명령이 안 돌았는데 모를 수 있음).
- 배치 모드에서 컴파일 오류가 나면 `unity-exec`가 끝나지 않고 멈출 수 있다(R07 후속에서도 겪음) → 10분 넘게 걸리면 `harness/reports/exec.log`의 `error CS`부터 본다.
- 프로퍼티(`MotionEnemy` 등)의 `.y`를 직접 바꿀 수 없다(CS1612).
- 메시를 합쳐 에셋으로 저장하면 용량이 커진다. 파일 하나 20 MB 넘으면 훅이 커밋을 막는다.
- **evaluator를 동시에 여러 개 띄우지 말 것** — 각자 unity-verify를 돌려 서로 "Unity Editor가 열려 있다"로 막힌다. 하나씩.
- 평가자가 백그라운드로 남긴 Unity 배치가 끝나기 전에 검증을 돌리면 막힌다 → `Get-Process Unity`가 0일 때.
- DuelCamera는 결투 내내 `playerBody` 렌더러를 1인칭이면 그림자만으로 덮어쓴다(칼은 목록에서 뺐음). BeatFx는 HUD 크기를 매 프레임 덮어쓴다(콤보는 `ComboBeatScale`로 넘김).
- 손가락 근육: HumanTrait.MuscleName은 "Right Index 1 Stretched" 형식(곡선 이름은 "RightHand.Index.1 Stretched").

## 6. 음악·차트 파이프라인 (곡을 다시 만들 때)

차트(`Assets/Resources/Charts/stage1.json`)나 장단 표를 바꿔 판정 박자가 달라지면 곡도 다시 만든다. 곡이 판정 박마다 장구를 치기 때문이다. (R17·R18 추격 개편은 판정을 바꾸지 않는 것이 원칙.)

1. 원본 샘플은 `harness/music-src/demo/`(공유마당: 태평소·꽹과리 CC BY, 장구 공공누리 1유형, 표기는 `docs/CREDITS.md`).
2. 장구 mp3 → wav
   ```bash
   mkdir -p Builds/MusicPreview
   ```
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/music/mp3towav.ps1 -In "harness/music-src/demo/전통악기_장구_치다_보통속도_Ver.1_MKH418_ST_192.mp3" -Out Builds/MusicPreview/janggu_mid.wav
   ```
3. 오토플레이로 판정 타임라인(`Builds/Gray`를 먼저 빌드)
   ```bash
   Builds/Gray/RhythmActionGame.exe -batchmode -startGame -autoplay -logFile Builds/autoplay.log
   ```
   ```bash
   python harness/scripts/music/parse-timeline.py Builds/autoplay.log harness/music/stage1_timeline.json
   ```
4. 곡 만들기 → mp3
   ```bash
   python harness/scripts/music/make-song.py Builds/MusicPreview/janggu_mid.wav harness/music/stage1_timeline.json Builds/MusicPreview/stage1_song_chart.wav
   ```
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/music/wav-to-mp3.ps1 -In Builds/MusicPreview/stage1_song_chart.wav -Out Assets/Audio/Music/stage1_song.mp3
   ```
5. 곡 길이나 챕터 시작 박이 바뀌면 `GameConfig.Music.LoopBeats`, `ChapterSongBeats`와 B10·B42 테스트 기대값을 함께 고친다.
6. 히트사운드: `python harness/scripts/music/make-hitsounds.py <장구 wav> Assets/Audio/Hit`. 효과음 녹음: `python -I harness/scripts/sfx/prep-mixkit.py`.

## 7. 개발자 도구 (회색 빌드·에디터 전용, 최종 빌드에는 없음)
- **숫자 1~6:** 그 챕터 처음에서 시작(6 = 성문 위 무사 결투). **F1:** 연출 켜고 끄기.
- **판정 기록:** `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Rhythm-Action-Game\Player.log`의 `[판정기록]`
- **실행 인자:** `-devChapter N`, `-autoplay`, `-autoplayMiss N`, `-autoplayShots 폴더`, `-autoplayShotsRange 시작 끝`, `-startGame`
- **게임 설정:** 타이틀·일시정지에 예고음 켜기/끄기, 음악 켜기/끄기.

## 8. 사용자 선호 (Claude 메모리 대신 — 새 계정은 여기만 보면 된다)
- **항상 한국어로 대답한다.** 영어로 답하면 다시 한국어로 해 달라고 한다.
- **"대답 먼저 해줘", "답변만"이면 코드·파일을 건드리지 않고 답만 한다.** 그다음 승인을 받고 작업한다.
- **정직하게:** 사용자는 "고쳤다고 뻥친 거 아니지?"라고 확인한다. 고친 것·안 고친 것·나중으로 미룬 것·제 잘못을 구분해 말한다. "해결"은 실제 장면(미리보기·측정)으로 확인한 뒤에만.
- 선택지를 낼 때는 "하나를 고르는 것"인지 "같이 적용 가능한 것"인지 분명히 쓴다.
- 리듬감을 중시한다. 노드는 "누르는 느낌"이 있어야 한다(노드에 슬로를 걸지 말 것). 노드가 화면 여러 곳에 동시에 나오면 따라가기 어렵다.
- 슬로모션은 "칼이 머리를 스치는", "문이 닫히는 걸 간신히 피하는" 아슬아슬한 느낌. 연출은 개연성이 있어야 한다(적이 갑자기 서 있음, 같은 편처럼 나란히 달림, 아무것도 안 하는 병사 → 싫어함).
- 추임새 기합 소리는 싫다.
- 드로우콜은 "크게 문제가 없으면" 조금 늘어도 받아들인다. 맵·연출 품질 우선. LOD·ISM 같은 최적화에 관심이 많다.
- 에셋 품질에 민감하다. 포인트는 "많이 써도 된다"고 했지만 쓰기 전에는 항상 예상 포인트를 알리고 승인받는다.
- 비용(VARCO 포인트, 유료 결제), 다운로드(파일명·출처·용량), 공개 범위 변경은 사용자 승인 후에만. Mixamo 로그인은 사용자, 다운로드는 Claude(앱 브라우저).
- **작업 방식 (2026-11-17, 마감 13:20):** 남은 항목을 몇 개씩 묶어 한 번에 구현하고, 그다음 한 번에 테스트한다 (2장).
- **점수 규칙 (2026-11-17 사용자 결정):** GDD-beta B20. 콤보는 장구만.
- **칼 쥐기 기준 (2026-11-17):** 사진처럼 손가락이 칼자루를 감싸 쥐어야 한다. 손을 편 채 칼만 붙어 있거나 칼자루가 손 위에 얹히면 안 된다. 손가락이 이상하게 꺾이면 바로 지적함 → **손 확대 그림을 직접 보고** 보여 줄 것.
- **VARCO 리깅·Mixamo 업로드 (2026-11-17):** "VARCO 리깅 다시 해 보고 안 되면 Mixamo" — VARCO humanoid-fingers로 해결(Mixamo 업로드 안 함).
- 빌드 공유: 루트에 `tempFinal.zip`으로 압축해 달라고 함(git에 안 올림).
- **Mixamo 동작은 상시 승인 (2026-11-17):** "공중 동작이나 처음 뛰는 동작 모두 필요하다면 언제든 믹사모에서 추천해주세요. 다 수용하겠습니다." → 필요한 Mixamo 동작은 골라서 받고(FBX Binary·Without Skin·30fps, 가능하면 In Place) 무엇을 왜 받았는지 알린다. VARCO 포인트는 여전히 견적 승인.
- **제출 빌드 (2026-11-17):** FullHD 1920×1080, 루트 `FFinalProject.zip`(안에 `FFinalProject/`). 사용자는 루트에 zip을 풀어 둔다 → 그 폴더가 커밋되지 않게(.gitignore).
- **시작화면 (2026-11-17):** "실제 게임이 시작되는 느낌" — 따로 꾸민 무대보다 실제 게임 맵·흐름을 원함.

## 9. 꼭 지킬 규칙 (CLAUDE.md 요약)
- `docs/GDD.md`는 잠겨 있어 수정하지 않는다. 기획 변경은 사용자와 정해 `docs/GDD-beta.md`에 적는다.
- 씬·프리팹 YAML은 직접 편집하지 않는다. `Assets/Editor/SceneBuilder*.cs`로 만든다.
- 수치는 `Assets/Scripts/Core/GameConfig.cs` 한 곳에 둔다(`Body`, `EnemyBody`, `Map` 등 섹션).
- 작업 완료는 evaluator 서브에이전트가 PASS를 줘야 한다(프롬프트는 "<작업ID> 작업을 평가해줘."만). 끝나면 log에 기록하고 커밋·push. 같은 작업 평가 실패 3번이면 failed로 멈춘다.
- 커밋은 `git add`를 따로 실행한 뒤 `harness/.commit-msg.txt` + `git commit -F`. 끝에 `Co-Authored-By` 줄. force push 금지. push는 `git push origin beta1-1`.
- `harness/tasks.json`은 PowerShell `ConvertTo-Json` 형식(LF, BOM 없음)을 유지한다. Python으로 json.dump 하면 파일 전체 형식이 바뀐다 → 문자열로 해당 필드만 바꾸고 `newline=''`로 쓴다.
- C# 파일을 Python으로 고칠 때도 `newline=''`(원래 줄바꿈 유지), GameConfig.cs는 BOM 없음.
