# 작업 인계 문서 (beta1-1)

- 작성: 2026-11-16, **마지막 갱신: 2026-11-17** (R07 후속 뒤, 다른 계정으로 이어서 하기 위한 정리)
- 브랜치: **`beta1-1`** (GitHub `AI-NC-practice/Rhythm-Action-Game`, 비공개). `main`에는 개발자 모드까지만 들어가 있다.
- 작업 목록의 진실은 언제나 `harness/tasks.json`이다. 작업별 기록은 `harness/log.md`(맨 아래가 최신), 평가 보고서는 `harness/reports/<작업>-eval-<n>.md`.
- 마지막 전체 검증: **EditMode 202/202, PlayMode 178/178** (`harness/reports/R07-followup-verify.md`). 최종 빌드는 T15 때 것이 마지막이다(R10에서 다시 빌드).
- **지금 상태 한 줄:** R06(1인칭 카메라·동작), R07(적 달리기·공중 적·지붕 발차기)과 그 후속 수정까지 끝났다. **다음은 R17(사용자가 추격 연출 개편안 승인)** — `docs/proposals/chase-redesign.md`를 사용자에게 보여 주고 무엇을 넣을지 정한다. 2장 참고.

---

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

## 2. 지금 멈춘 곳과 다음 할 일

- 마지막 작업 커밋: R07 후속 (이 문서 갱신과 같은 커밋).
- tasks.json: done 74, pending 12(B40, R08~R18), needs_user 1(T16), blocked 1(B46 취소).
- **다음에 할 일 (순서대로)**
  1. **R17 [사용자] 추격 연출 개편안 승인** — `docs/proposals/chase-redesign.md`. 사용자에게 A~E를 보여 주고 고르게 한다(사용자 아이디어: 가끔 뒤돌아보면 적이 따라옴, 옆 건물에서 쫓아옴, 앞질러서 싸움, 아무것도 안 하는 병사는 시민으로). 시민은 VARCO 1종 약 300포인트 → 견적 승인. 승인 내용은 `docs/GDD-beta.md` 새 절로.
  2. **R18** 추격 연출 개편 구현 (R17 승인 범위)
  3. **R08** 슬로모션 성공/실패 차이 + 리듬감 2 다듬기 (사용자와 다듬을 점을 먼저 정함)
  4. **R09** 전투 동작·테스트 다듬기 (창 two_hand_attack 클립 정리, 칼 뺏기 동작 교체, 찌르기 처형 테스트)
  5. **R13** 낮은 처마 장애물이 지붕 위에서 대문처럼 보이는 문제
  6. **R14** 닫히는 대문 아슬아슬 통과 동작(몸을 날려 들어감) + 문 양옆 막기
  7. **R15** 되감기 때 나와 적을 처음 위치로 (원인 확인부터)
  8. **R16** 멈춰 싸울 때 내 몸이 제자리 달리기 하지 않게 (적 쪽은 R07에서 해결)
  9. **R10** 최적화(LOD·임포스터·GPU Resident Drawer) + 최종 빌드. 드로우콜 약 369(R03, 사용자 승인) — 여기서 줄인다
  10. **R11** [사용자] 다시 플레이 확인 → 통과하면 T16도 done
  11. **R12** 효과음·환경음(VARCO Sound, 시작 전 범위·포인트 승인) → 끝나면 B40도 done
- `/run-tasks`는 tasks.json 순서(파일 안 순서)대로 pending을 고르므로 **B40을 먼저 집는다.** B40은 R12로 미뤄졌으니 건너뛰고 R17부터 하자고 사용자에게 확인한다(2026-11-16 사용자 결정).
- 기획 결정이 필요한 것(사용자와 정할 것, log.md "제안"): 건물 밀도(1구간 등 너무 많음), 적이 뻔히 보이는데 담을 넘는 사건 배치(위에서 뛰어내려 포위당하는 식으로?), 1번째 지붕 위 적(127박) 발차기 낙하가 옆 80°라 1인칭에서 안 보임(짧은 3인칭 컷?), 옆으로 피하기 NORMAL 순간 화면 가림(0.835), 발 IK.

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

**주의(정직하게):** R03에서 "공중에 뜬 적 해결"이라고 했지만 시작 자리만 검사해서 사실이 아니었고, R07 첫 보고("뜬 프레임 0")도 보이지 않는 충돌 상자를 지지면으로 쳐서 무사를 놓쳤다. 지금 검사는 보이는 메시로 만든 검사용 충돌체만 센다. **"고쳤다"고 하기 전에 실제 장면(미리보기 PNG)으로 확인할 것.**

## 4. 세션별 한 일

| 작업 | 내용 | 커밋 |
|---|---|---|
| B45~B53, T10~T15 | (2026-11-16 이전 세션, log.md 참고) 박 펄스, 슬로모션, 효과음, 에셋 생성, 툰 셰이딩·1인칭 몸, 적 모델·전투 동작, 맵 모델 | 여러 개 |
| R01~R05 | 맵 재설계 안·새 에셋 35개(10,800포인트)·맵 다시 짓기·회색 상자 교체·T14 테스트 | 746300b~c07ed3e |
| R06 | 1인칭: BodyOffset(몸 받침), 벽타기·미끄러지기·실패·피격 때 카메라를 머리에 붙이고 몸이 카메라 궤적과 함께, 실패·피격은 몸 숨김, 목 단면·카메라 60° 돌아감·뒤집힘·목보다 낮음이면 숨김, 벽타기 45° + 달리기 클립 + 카메라 기울기, 미끄러지기 NORMAL = duck | 5c2c7bb |
| R06 후속 | 4장 포위 전투: 몸이 카메라 yaw를 따라 돎(검은 화면 해결), 작업 R13~R16 추가 | 885b6b5 |
| R07 | 적 Run 상태(속도에 맞춰), 사건 소품 적 EnemyFigureRun, 추격 적 실제 경로·발밑 프로필, 매복 적 비탈, 지붕 발차기 Mixamo Flying Back Death + 빌드 때 고른 골목 착지 + 옆집 지붕을 넘는 포물선 + 카메라 따라봄 | e5b0ffd |
| R07 후속 | 지붕 위 전투 높이 격자·옆 범위·이음매 비키기(무사 공중 해결), 돌진 적 실제 길·높이 프로필(벽 뚫기 해결), 추격 프로필 머리 위 여유, 테스트는 보이는 메시만, 추격 연출 개편안·R17·R18 | (이 커밋) |

VARCO 포인트 누적 약 17,400 (T10 2,050 + T11 1,000 + T11R 3,550 + R02 10,800). run_flow 17/45회(`harness/varco-usage.log`). 플로우 ID `harness/asset_specs.json`의 `defaults.flow_id` = `7c1eec5d-c366-4ce4-8713-0b6adb6fc1da`.
Mixamo 동작 12개(`Assets/Art/Animations/Mixamo/`, R07에 flying_back 추가).

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
- 기존: `Toon.shader`, `BodyMoves`/`EnemyMoves`, `DressingInstancer`(인스턴싱·칸 컬링), `MapPlan`, `MapSupportReport`, `EventProps.ModelOverride`, 에디터 `ToonMaterials`·`EnemyRig`·`SceneBuilder.Map.cs`·`PreviewCapture`·`DrawCallReport`.

### 측정 테스트 (새로 만든 것, 다음 작업에서 재사용)
- `R06_FirstPersonViewPlayTests`: 화면 카메라와 같은 자세로 **내 몸만** 작은 텍스처에 그려 화면 가림 비율, 목 단면 보임, 결과별 미리보기(`harness/reports/R06/`).
- `R07_EnemyRunPlayTests`: 보이는 맵 메시(인스턴싱 장식 + 정적 배칭 합친 메시)에 **검사용 충돌체**를 붙이고 실제 stage1을 자동 연주하며 모든 적의 달리기·미끄러짐·제자리 달리기·**발밑(뜸)·건물 속**을 매 프레임 셈. 지붕 발차기 착지·카메라. 미리보기 `harness/reports/R07/`.

### 알아 둔 함정
- **발밑·지지면 검사는 보이는 메시로만.** 게임의 충돌 상자(지붕 길·성문 상자)는 보이는 모델보다 넓거나 높다(성문 0.5 m).
- PlayMode 테스트에서 시간을 주입할 때는 박자 시계, 슬로모션, Hit Stop(TimeFx.RealTime)을 모두. 실제 시간으로 진행하는 연출(발차기 낙하 궤적)은 `Time.realtimeSinceStartup`으로 기다리고, **기다리는 동안에도 자동 연주를 계속**해야 한다(놓쳐서 되감기면 적이 제자리로 돌아감).
- 실행 순서: PlayerBody LateUpdate(−50)가 StageDirector LateUpdate(0)보다 먼저라 몸이 카메라 yaw를 한 프레임 늦게 따라감(회전 중에만).
- 미리보기·측정 카메라는 자세를 정한 같은 프레임에 렌더한다.
- 배치 모드에서 컴파일 오류가 나면 `unity-exec`가 끝나지 않고 멈출 수 있다(R07 후속에서도 겪음) → 10분 넘게 걸리면 `harness/reports/exec.log`의 `error CS`부터 본다.
- 프로퍼티(`MotionEnemy` 등)의 `.y`를 직접 바꿀 수 없다(CS1612).
- 메시를 합쳐 에셋으로 저장하면 용량이 커진다. 파일 하나 20 MB 넘으면 훅이 커밋을 막는다.

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

## 9. 꼭 지킬 규칙 (CLAUDE.md 요약)
- `docs/GDD.md`는 잠겨 있어 수정하지 않는다. 기획 변경은 사용자와 정해 `docs/GDD-beta.md`에 적는다.
- 씬·프리팹 YAML은 직접 편집하지 않는다. `Assets/Editor/SceneBuilder*.cs`로 만든다.
- 수치는 `Assets/Scripts/Core/GameConfig.cs` 한 곳에 둔다(`Body`, `EnemyBody`, `Map` 등 섹션).
- 작업 완료는 evaluator 서브에이전트가 PASS를 줘야 한다(프롬프트는 "<작업ID> 작업을 평가해줘."만). 끝나면 log에 기록하고 커밋·push. 같은 작업 평가 실패 3번이면 failed로 멈춘다.
- 커밋은 `git add`를 따로 실행한 뒤 `harness/.commit-msg.txt` + `git commit -F`. 끝에 `Co-Authored-By` 줄. force push 금지. push는 `git push origin beta1-1`.
- `harness/tasks.json`은 PowerShell `ConvertTo-Json` 형식(LF, BOM 없음)을 유지한다. Python으로 json.dump 하면 파일 전체 형식이 바뀐다 → 문자열로 해당 필드만 바꾸고 `newline=''`로 쓴다.
- C# 파일을 Python으로 고칠 때도 `newline=''`(원래 줄바꿈 유지), GameConfig.cs는 BOM 없음.
