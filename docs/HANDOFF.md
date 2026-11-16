# 작업 인계 문서 (beta1-1)

- 작성: 2026-11-16 (마지막 갱신: R05 끝난 뒤, 커밋 `c07ed3e` 다음)
- 브랜치: **`beta1-1`** (GitHub `AI-NC-practice/Rhythm-Action-Game`, 비공개). `main`에는 개발자 모드까지만 들어가 있다.
- 작업 목록의 진실은 언제나 `harness/tasks.json`이다. 작업별 기록은 `harness/log.md`, 평가 보고서는 `harness/reports/`에 있다.
- 마지막 전체 검증(R05): **EditMode 200/200, PlayMode 168/168 — 두 번 연속 전부 통과** (`harness/reports/R05-verify-1.md`, `-2.md`). 최종 빌드는 T15 때 것이 마지막이다(R10에서 다시 빌드).
- **지금 상태 한 줄:** 첫 플레이 피드백(3장)을 R01~R12로 나눴고 **R01~R05는 끝났다**(맵 재설계·새 에셋·회색 상자 교체·T14 테스트). **다음은 R06(1인칭 카메라·동작 정리)** 부터 — 2장 참고.

---

## 1. 다른 컴퓨터에서 이어서 시작하기

1. **설치할 것**
   - Unity Hub와 **Unity 6000.4.0f1** (Windows Build Support 포함). 새 컴퓨터에서는 **Unity Hub에서 라이선스를 먼저 활성화**한다(안 하면 배치 모드가 코드 198로 끝난다).
   - Git, GitHub CLI(`gh`, 릴리스를 올릴 때만)
   - **Python 3** (+ `numpy`, `scipy`는 곡·히트사운드를 다시 만들 때만). `harness/scripts/varco/spec-set.py`, `harness/scripts/sfx/prep-mixkit.py`는 표준 라이브러리만 쓴다.
     ```bash
     pip install numpy scipy
     ```
   - **Blender 5.2** (에셋 다시 굽기·검사·미리보기). 경로 `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`. 없으면 에셋 단계만 못 한다.
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
   - 결과는 `harness/reports/verify-latest.md`. PlayMode는 무거워져서 5~8분 걸린다.
   - `.meta`에 임포트 설정이 들어 있어 ConfigureAll은 보통 안 해도 되지만, 클립이 이상하면 한 번 돌린다.
5. **빌드**
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.BuildScript.BuildFinal
   ```
   ```bash
   powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/smoke-test-build.ps1
   ```
   - `Builds/Final/RhythmActionGame.exe` (약 288 MB, 개발자 모드 없음). 개발자 모드가 있는 빌드는 `BuildGray` → `Builds/Gray`.
   - `Builds/`는 git에 올리지 않는다. 공유할 때는 GitHub Releases.
6. **Claude Code로 이어서 하기:** 프로젝트 폴더에서 Claude Code를 열고 다음처럼 말한다.
   > `docs/handoff.md` 읽고 이어서 진행해

   작업 루프는 `/run-tasks`다. Claude의 개인 메모리는 컴퓨터마다 따로라서 넘어가지 않는다. 필요한 것은 아래 8장 "사용자 선호"에 적어 두었다.

### 이 컴퓨터에만 있고 git에 없는 것 (집에서 없어도 진행 가능)
- `harness/reports/varco/*_raw/`(동작 FBX 원본 38개, 약 340 MB), `harness/reports/varco/v2_high/`(고폴리 GLB) — 용량 때문에 `.gitignore`. 게임에 쓰는 결과물(`Assets/Art/Models/`)은 git에 있다. 다시 필요하면 VARCO 플로우에서 다시 받는다(`harness/reports/varco/t11-<id>.txt`에 동작별 해시가 있다. URL은 `https://3d.varco.ai/api/objects/<해시>.fbx`, 만료됐으면 `get_output_downloads`로 다시 얻는다).
- R02 고폴리 GLB 35개(`harness/reports/varco/r02/high/`, 각 약 10 MB)와 VARCO 이미지 원본(`r02/*.png`) — git 제외. 다시 필요하면 VARCO에서 다시 받는다.
- `git stash`에 B46("합" 주고받기, 사용자가 취소) 구현이 보관돼 있다. 취소된 작업이라 집에서는 필요 없다.
- `Builds/Final` 빌드. 집에서 5번으로 다시 만든다.

## 2. 지금 멈춘 곳

- 마지막 작업 커밋: `c07ed3e` (R05). 그 뒤 이 문서를 갱신해 올렸다.
- tasks.json 상태: done 71, pending 8(B40, R06~R12), needs_user 2(B10, T16), blocked 1(B46 취소).
- **다음에 할 일 (순서대로, 시작 전에 사용자에게 한 번 확인):**
  1. **R06** 1인칭 카메라·동작 정리 — 실패 때 머리 없는 몸, 벽타기 45° 기울기, 미끄러지기 화면 가림 (피드백 2·6·7번)
  2. **R07** 적 달리기 동작(`forward_run_1`) + 지붕 위 PERFECT 발차기 낙하(Mixamo 동작은 사용자 승인 → 사용자 로그인, Claude 다운로드). "공중에 뜬 적"은 R03에서 대부분 해결(아래 4장) — R07에서는 PlayMode 검사만 확인
  3. **R08** 슬로모션 성공/실패 차이 + 리듬감 2 다듬기 (사용자와 다듬을 점을 먼저 정함)
  4. **R09** 전투 동작·테스트 다듬기 (창 two_hand_attack 클립 정리, 칼 뺏기 동작 교체, 찌르기 처형 테스트)
  5. **R10** 최적화: LOD·임포스터·GPU Resident Drawer + 최종 빌드. **R03에서 드로우콜이 290 → 약 369로 늘었다(사용자 승인)** — 여기서 줄인다
  6. **R11** [사용자] 다시 플레이 확인 → 통과하면 T16도 done
  7. **R12** 효과음·환경음(VARCO Sound) — 사용자 결정(2026-11-16) "효과음은 나중에". 시작 전 범위·포인트 승인. 끝나면 B40도 done
- `/run-tasks`는 tasks.json 순서대로 가므로 **B40을 먼저 집는다.** B40은 R12로 미뤄졌으니 B40은 건너뛰고 R06부터 하자고 사용자에게 확인한다.
- B10(음악): 사용자가 곡을 승인했으나 `needs_user`로 남아 있다 — done으로 바꿀지 확인. B46: 사용자 결정으로 취소(blocked).
- 발 IK(지붕 비탈에 발바닥 맞추기)는 **지금 없음**. 사용자에게 장단점을 설명했고(멈춰 싸우는 적·무사만, 서 있을 때만 하는 것이 비용 대비 낫다) 별도 작업 후보로 log.md에 적어 둠. 넣을지는 사용자가 정한다.

## 3. 사용자 피드백 (2026-11-16 첫 플레이) — 다음에 할 일

사용자가 `Builds/Final`을 쳐 보고 낸 문제와 진단. **R01~R12로 나눠 tasks.json에 넣었다(사용자 승인).** 처리 상태:
- 1번(공중에 뜬 적): **R03에서 해결** — 복선 적은 실제 지붕·담 위, 적 발밑 지지면을 빌드 때 재서 검사(R07에서 확인만)
- 3번(철봉·허들), 5번(경단): **R04에서 해결** — 모델로 교체
- 8번(맵): **R03에서 해결** — 챕터별 직접 배치, 장터 거리, 모퉁이 곡선. 9번 일부(배경 반복)도 R03
- 2·6·7번 → R06, 4번 → R07, 9번 나머지(LOD 등) → R10

| # | 사용자 말 | 원인 (진단) | 고칠 방향 |
|---|---|---|---|
| 1 | 공중에 떠 있는 사람들은 뭐임? 사람들이 왜 공중에 이렇게 많아? | (추정) 복선(foreshadow)·지붕 위 감시(roof_watchers) 적이 회색 상자 시절 높이에 그대로 서 있고, T15에서 그 아래 건물을 안 놓음. EventProps의 적 위치 확인 필요 | 적 발밑에 실제 지붕 모델을 두거나 적 높이를 지면/지붕에 맞춤 |
| 2 | 실패 시 캐릭터 머리 어디로 감? | 화면엔 머리·목을 뺀 복사본을 그리고(PlayerBody), 카메라는 머리 뼈를 35%만 따라감. 3인칭용 실패 동작(hit_obstacle, heavy_hit_back, forward_fall_twist)이 몸을 크게 움직여 머리 없는 몸이 정면에 보임 | 동작 중 카메라를 머리 뼈에 붙이거나, 1인칭용 실패 연출(카메라만 흔들고 몸은 시야 밖)로 바꿈 |
| 3 | 가운데 철봉 같은 거? 허들은 또 뭐? | 빨랫줄(clothesline)·처마(eave)·밧줄(rope_swing) 장애물은 VARCO 모델이 없어 회색 상자 그대로 | 모델 만들기(VARCO, 포인트 승인) 또는 Unity 기본 도형으로 그럴듯하게 꾸미기 |
| 4 | 적 걸어나오는 애니메이션은 어디 가고 유령처럼 스윽 다가옴? | 적 Animator(EnemyRig)에 걷기·달리기 상태가 없음. 돌진·이동 때 위치만 바뀌고 대기 동작 | `forward_run_1`(이미 받아 둠)을 Run 상태로 연결, 이동 속도에 맞춰 재생 |
| 5 | 이 경단들은 뭐? | 굴러오는 항아리(rolling_jars) 사건 소품의 회색 공. 사건 소품(EventProps: 항아리·화살·깃발·문짝 등)은 아직 회색 상자 | 사건 소품을 모델로 교체(jars 모델 재사용 등) |
| 6 | 벽을 탈 거면 몸이 45도로 되어 벽을 밟아야지, 왜 옆으로 타? | PERFECT = Mixamo "Wall Run"(벽 위로 오르는 동작), NORMAL = VARCO 옆걸음. 몸 기울기 없음 | 벽타기 동안 몸(모델 루트)을 벽 쪽으로 약 45° 기울이고 달리기 클립 + 카메라 기울기 |
| 7 | 물체 아래로 피하는 건 왜 화면을 이렇게 많이 가림? | 미끄러지기 동작에서 무릎·팔이 카메라 앞으로 올라오는데 카메라는 일부만 내려감(2번과 같은 원인) | 2번과 함께: 1인칭 카메라·동작 정리, 필요하면 팔·다리를 시야에서 가리기 |
| 8 | 맵이 왜 중간중간 순간이동하고 조잡한 데가 많지? 조선 거리가 이런 느낌임? | 장식을 11 m 간격·같은 모델로 줄 맞춰 자동 배치(SceneBuilder.Map.cs). 경로가 90° 꺾임(MapLayout.Route). 사건·되감기 카메라 움직임 | 챕터별로 직접 설계: 좁고 굽은 골목, 초가·기와 섞기, 흙담·돌담, 장독대·우물·나무·흙길. 새 에셋(VARCO, 포인트 승인) 필요. 모퉁이 부드럽게 |
| 9 | 먼 건 이미지 처리했음? LOD는? 언리얼 ISM처럼 최적화했음? | 먼 곳은 반지름 300 m 원통에 코드로 그린 산·지붕 실루엣(조잡, 지붕 띠 반복). 중간 거리(임포스터)는 없음. **LOD 없음.** 길 옆 장식만 `Graphics.RenderMeshInstanced`(ISM 비슷, 인스턴스 컬링·LOD 없음). 지붕 길·담은 정적 배칭. Unity 6 GPU Resident Drawer 미사용 | 배경 그림 다시(손그림 느낌·반복 줄이기), 중간 거리 임포스터, LODGroup, GPU Resident Drawer(=ISM/HISM 자동 인스턴싱·컬링) 검토 |

**추천 순서** (사용자에게 확인할 것)
1. 1인칭 카메라·동작 정리 (2, 6, 7번) — 플레이 느낌에 가장 크게 영향
2. 적 이동 동작 + 공중에 뜬 적 (4, 1번)
3. 회색 상자 장애물·사건 소품 → 모델 (3, 5번)
4. 맵 다시 설계 (8번, 새 에셋 = 포인트 승인 필요)
5. 최적화 (9번: LOD·임포스터·GPU Resident Drawer)

**사용자가 "나중에 고치자"고 미뤄 둔 것도 함께 정리한다**
- 슬로모션 성공/실패 차이 (B47·B52, 사용자: "나중에 다 손보자")
- 리듬감 2 전체 (B49, 사용자: "별로긴 한데 일단 넘어가자")
- 창 적 `two_hand_attack` 클립이 31.9초 — 4.53초 타격까지만 씀
- 칼 뺏기 동작은 Mixamo에 맞는 게 없어 "Standing Disarm Underarm"(도끼를 겨드랑이에 끼는 동작)으로 대체
- 지평선 배경 지붕 띠 반복, 1구간 드로우콜 640 (지붕 길·골목 담 모델도 인스턴싱하면 더 줄어듦)
- 찌르기 처형(StabWithOwnSword) PlayMode 테스트 보강 (T14 평가 제안)

## 4. 이번 세션(2026-11-16)에 한 일

자세한 것은 `harness/log.md`의 각 작업 항목과 `harness/reports/<작업>-eval-*.md`.

| 작업 | 내용 | 커밋 |
|---|---|---|
| B45 | 박 펄스(HUD·레인·비네트), 예고음(dsp 시간 예약), 발걸음·카메라 박 출렁임(BeatBob) | d6a04ce |
| B46 | "합" 주고받기 — 구현했으나 사용자 취소, git stash 보관 | - |
| B47→B52 | 슬로모션을 "아슬아슬 피하기"로: PERFECT/NORMAL 회피 뒤 0.3배 0.6초, 회색 화면, 물속 같은 음악, 대문 컷(문 앞 카메라), 무사 37.5·66박 | 95dc9a9, 17db9be |
| B48 | 실제 녹음 효과음 8개(Mixkit, CREDITS.md), 적 공격 0.5박 전 쉭 소리 | d8be627 |
| B53 | 음악 끄기 옵션(타이틀·일시정지) | 4078628 |
| B49 | 리듬감 2 사용자 확인(일단 통과, 나중에 다듬기) | 140929c |
| T10 | 정적 에셋 7개(VARCO) | 9629590 |
| T11 | 캐릭터 4개 + 리깅 + VARCO 동작 38개 | 70323dd |
| T11R | 에셋 11개 다시 만들기: 4면도 검수 → 정면 한 장 → 고폴리 60k → 캐릭터 Remesh 12,800 / 정적은 바닥 면 삭제 + 로우폴리 + 색·노멀 굽기(Blender). 손가락 갈라짐 해결. 3,550 포인트 | 17f6a2f |
| T12 | Mixamo 동작 11개(Without Skin, 30fps) Humanoid 리타깃 | 9593002 |
| T13 | 툰 셰이더(3단계 + 뒤집은 외곽선), 1인칭 전신 플레이어(머리 뺀 복사본, 머리 뼈 추적 카메라, 결과별 Animator 18 상태) | e1d3c1f |
| T14 | 적 3종 모델 + 손 뼈 무기, 타격 순간(오른손 최고 속도) = 판정 목표 박, 크로스페이드, 처형 같은 박 시작·정렬, 피격·쓰러짐 | 9111c35 |
| T15 | 맵 모델 교체(상자는 충돌만), 챕터별 장식, 외곽 그림 원통, 해 질 녘·안개, 드로우콜 778 → 290 (3회차 PASS) | 42d8e15 |
| R01 | 맵 재설계 안 + 새 에셋·포인트 견적 (docs/proposals/map-redesign.html), 사용자 승인 | 746300b |
| R02 | VARCO 새 에셋 35개(건물·담·장터·마을 소품·장애물) + 배경 그림 6장. 4면도 → 정면 → 고폴리 → cull_bake. 10,800 포인트 | d6dacb5 |
| R03 | 맵 다시 짓기: MapPlan 직접 배치(구역 11개·건물 3줄·소품 밀도), 장터 거리, 모퉁이 곡선, 적 발밑 지지면(MapSupportReport), 배경 파노라마 한 장, 지붕 상자 방향 버그 수정. 드로우콜 290 → 약 369(사용자 승인) | b43657a |
| R04 | 빨랫줄·처마·사건 소품 10종·칼 막대를 모델로, 게임 화면에 회색 도형 없음 | 8acaa4f |
| R05 | T14 실패 원인: 테스트가 Hit Stop 시계(실제 시간)를 주입하지 않음 → 테스트 수정, 전체 두 번 연속 통과 | c07ed3e |

VARCO 포인트 누적 약 17,400 (T10 2,050 + T11 1,000 + T11R 3,550 + R02 10,800). run_flow 사용 17/45회(`harness/varco-usage.log`, 한도는 asset_specs `defaults.max_run_flow_calls`). 플로우 ID는 `harness/asset_specs.json`의 `defaults.flow_id` = `7c1eec5d-c366-4ce4-8713-0b6adb6fc1da`.

## 5. 이번 세션에 생긴 도구와 구조 (다음 작업에서 알아야 할 것)

### 에셋 파이프라인
- **VARCO:** `varco3d-asset` 스킬. 모든 호출에 `flowId`. 3D 생성 200점, 이미지 50점, Remesh·Rig·Animate 0점. Remesh는 목표보다 15~20% 많이 나오니 목표를 낮게(15k 상한 → 12,800).
  - 4면도(16:9) → 4면도를 참조(GenerateImage의 `reference` 포트)로 만든 정면 한 장 → Generate3D(이미지 1장만 받음).
  - 캐릭터: Generate3D(tPose) → Remesh → Rig → Animate(동작마다 노드). 노드 ID는 asset_specs의 `nodes`.
  - `python -I harness/scripts/varco/spec-set.py <id> <필드> '<json>'` — asset_specs.json 형식 그대로 한 필드만 고침(있는 필드만).
- **Blender 스크립트** (`harness/scripts/blender/`, `powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/blender-run.ps1 -Script <이름> --in ...`. R02에서 래퍼의 stderr 문제를 고쳐 이제 래퍼를 써도 된다):
  - `cull_bake.py --in --out --max-tris --tex --cull-bottom 1 [--voxel 0.012] [--cage 0.03 --ray 0.1]` — 바닥 면 삭제 → 로우폴리(목표까지 여러 번) → 색·노멀 굽기 → GLB. 가닥이 따로 떨어진 모델(짚단)은 `--voxel`, 굽기 누락(검은 얼룩)은 `--cage/--ray`를 키운다. 굴러가거나 날아가는 것은 `--cull-bottom 0`
  - `mesh_check.py --in` — 삼각형·구멍(경계 변)·비다양체·조각 수
  - `render_preview.py`, `render_closeup.py --part hand|head` — 미리보기
  - `strip_mesh_fbx.py` — 동작 FBX에서 메시 빼고 뼈+동작만
- **R02 에셋 위치:** `Assets/Art/Models/<id>/<id>.glb`(기존 11개는 `Assets/Art/Models/<id>.glb`), 툰 머티리얼 `Assets/Art/Materials/Toon/<id>.mat`(`ToonMaterials.MapAssets`, `StaticPath`). 고폴리 원본·이미지 원본은 git 제외(`harness/reports/varco/r02/high/`, `r02/*.png`) — asset_specs의 `nodes.mesh`로 다시 받는다. 검수 모음은 `harness/reports/varco/r02/contact/`, `harness/reports/preview/r02_p*.png`.
- **배경 파노라마:** `python harness/scripts/map/make-backdrop.py`(Pillow) → `Assets/Art/Backdrops/panorama.png` (R02 그림 6장을 이어 붙임).
- **Unity 임포트:** `HumanoidImport.ConfigureAll`(캐릭터 38개 + Mixamo 11개). 클립은 앞뒤·좌우 이동과 방향을 루트로 빼서 버리고(제자리) 높이만 자세에 남김, 몸 방향 기준. player 메시 FBX만 Read/Write 켬(머리 삼각형 빼기).

### 런타임 구조
- `Assets/Shaders/Toon.shader` (3단계 명암, 뒤집은 외곽선 SRPDefaultUnlit 패스, 노멀 맵은 선형 RGB, GPU 인스턴싱), `Backdrop.shader`(먼 배경, 조명·안개 없음)
- `PlayerBody` — 플레이어 Animator를 **박자 시계 시간으로 직접 진행**, 머리 뺀 복사본, 카메라 받침 머리 따라가기(LateUpdate), 사건 결과·처형·피격 상태
- `BodyMoves`, `EnemyMoves` — 상태 이름·클립 표 (Core, 테스트 가능)
- `EnemyAnimator` — 적 Animator, 공격은 (타격 − 2박) → 타격으로 클립 시간을 맞춰 원래 속도로 휘두름
- `DressingInstancer` — 길 옆 장식 GPU 인스턴싱. R03: (메시, 칸)마다 묶고 경계로 컬링(건물 320 m 칸, 작은 소품 40 m 칸·70 m 밖 안 그림, 그림자는 건물·큰 나무만)
- `MapPlan`(Core) — R01 배치 안을 옮긴 순수 배치기(GameConfig.Map의 Zones·Modules·Props). `MapLayout.FlatPointAt`은 모퉁이 앞뒤 8 m가 베지어 곡선, 곧은 상자는 `VertexPointAt`
- `MapSupportReport` — 빌드 때 잰 적·복선 발밑 지지면(보이는 메시와 수직선 교차)
- `EventProps.ModelOverride` — 에디터에서 사건 소품 부품을 모델로(빈 축 + 가운데 맞춘 모델). 실행 중 시험 차트는 회색 도형 그대로
- 에디터: `ToonMaterials`(툰 머티리얼·Player.controller), `EnemyRig`(적 모델·무기·컨트롤러·타격 순간), `SceneBuilder.Map.cs`(맵 모델·장식·배경·분위기), `PreviewCapture.CaptureT13/T14/T15`(미리보기 PNG), `DrawCallReport`(드로우콜 비교용 Game_NoBatch 씬)
- `unity-verify.ps1 -Platform PlayMode -Filter 'R03_|T15_'` — 작업 중 일부 테스트만 빠르게 (끝낼 때는 필터 없이 전체).
- 드로우콜 측정은 PlayMode `T15_DrawCallTests`가 하고 `harness/reports/drawcalls.md`에 씀. Unity 6에는 "Batches Count" 카운터가 없어 Standard·SRP Batcher·BRG Draw Calls를 더한다. 배치 모드에서는 RenderTexture에 직접 그려야 잡힌다.

### 알아 둔 함정
- PlayMode 테스트에서 시간을 주입할 때는 **박자 시계, 슬로모션, Hit Stop(TimeFx.RealTime)을 모두** 주입해야 한다 (R05: Hit Stop만 실제 시계여서 기계 속도에 따라 T14가 실패).
- 미리보기·측정 카메라는 자세를 정한 **같은 프레임에** 렌더한다. 한 프레임 지나면 카메라 연출 스크립트가 방향을 되돌린다(R03).
- 빌드 중 새로 붙인 MeshCollider가 광선에 안 잡힌 일이 있었다(R03) → 발밑 검사는 메시 삼각형 직접 계산으로.
- 길 위 지붕은 기와집을 이어 붙여 이음매마다 골이 있다 → 적 자리에 이음매가 오지 않게 조각 수를 고른다(`TileIntoBox avoid`).
- 배치 모드에서 컴파일 오류가 나면 `unity-exec`가 끝나지 않고 멈출 수 있다 → 고친 뒤 다시 실행.
- 에디터 모드에서 Humanoid 자세 미리보기는 `Animator.Rebind/Play/Update(0)`가 씬 오브젝트에서만 잘 된다. 새로 만든 인스턴스에는 `clip.SampleAnimation`(몸 방향은 다를 수 있음).
- PlayMode 테스트는 시간을 주입한다. 실제 프레임 시간(`unscaledDeltaTime`, `Time.deltaTime`)을 쓰는 연출은 씬이 무거워지면 결과가 흔들린다 → 테스트에서 고정 시간을 넣거나, 검사 시점을 프레임 끝으로(B28처럼).
- 메시를 합쳐 에셋으로 저장하면 용량이 커진다(장식 합치기 225 MB → 포기, 인스턴싱으로 대체). 파일 하나 20 MB 넘으면 훅이 커밋을 막는다.

## 6. 음악·차트 파이프라인 (곡을 다시 만들 때)

차트(`Assets/Resources/Charts/stage1.json`)나 장단 표를 바꿔 판정 박자가 달라지면 곡도 다시 만든다. 곡이 판정 박마다 장구를 치기 때문이다.

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
5. 곡 길이(404박)나 챕터 시작 박이 바뀌면 `GameConfig.Music.LoopBeats`, `ChapterSongBeats`와 B10·B42 테스트 기대값을 함께 고친다.
6. 히트사운드: `python harness/scripts/music/make-hitsounds.py <장구 wav> Assets/Audio/Hit`. 효과음 녹음: `python -I harness/scripts/sfx/prep-mixkit.py`(→ `Assets/Audio/Sfx/Rec_*.wav`).

## 7. 개발자 도구 (회색 빌드·에디터 전용, 최종 빌드에는 없음)
- **숫자 1~6:** 그 챕터 처음에서 시작(6 = 성문 위 무사 결투). **F1:** 연출 켜고 끄기.
- **판정 기록:** `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Rhythm-Action-Game\Player.log`의 `[판정기록]`
- **실행 인자:** `-devChapter N`, `-autoplay`, `-autoplayMiss N`, `-autoplayShots 폴더`, `-autoplayShotsRange 시작 끝`, `-startGame`(타이틀 건너뛰기)
- **게임 설정:** 타이틀·일시정지에 예고음 켜기/끄기, 음악 켜기/끄기(테스트용).

## 8. 사용자 선호 (Claude 메모리 대신)
- **항상 한국어로 대답한다.** 영어로 답하면 다시 한국어로 해 달라고 한다(2026-11-16에도 있었음).
- 리듬감을 중시한다. 노드는 "누르는 느낌"이 있어야 한다(노드에 슬로를 걸지 말 것).
- 슬로모션은 "칼이 머리를 스치는", "문이 닫히는 걸 간신히 피하는" 아슬아슬한 느낌.
- 추임새 기합 소리는 싫다. 노드가 화면 여러 곳에 동시에 나오면 따라가기 어렵다.
- 드로우콜은 "크게 문제가 없으면" 조금 늘어도 받아들인다(R03, +26%). 맵·연출 품질을 우선.
- 맵: 챕터 컨셉에 맞춘 배치, 먼 곳은 이미지 착시(안 되면 실제 오브젝트), 하이폴리는 로우폴리 + 노멀 맵, 같은 머티리얼은 묶어서 드로우콜 줄이기. LOD·ISM 같은 최적화에 관심이 많다.
- 에셋 품질에 민감하다(손가락 갈라짐·빈 공간을 바로 짚음). 포인트는 "많이 써도 된다"고 했지만 쓰기 전에는 항상 예상 포인트를 알리고 승인받는다.
- 비용(VARCO 포인트, 유료 결제), 다운로드(파일명·출처·용량), 공개 범위 변경은 사용자 승인 후에만 한다. Mixamo 로그인은 사용자, 다운로드는 Claude(앱 브라우저).
- "작업하지 말고 답변만"이라고 하면 코드·파일을 건드리지 않는다.

## 9. 꼭 지킬 규칙 (CLAUDE.md 요약)
- `docs/GDD.md`는 잠겨 있어 수정하지 않는다. 기획 변경은 사용자와 정해 `docs/GDD-beta.md`에 적는다.
- 씬·프리팹 YAML은 직접 편집하지 않는다. `Assets/Editor/SceneBuilder*.cs`로 만든다.
- 수치는 `Assets/Scripts/Core/GameConfig.cs` 한 곳에 둔다(`Body`, `EnemyBody`, `Map` 등 섹션).
- 작업 완료는 evaluator 서브에이전트가 PASS를 줘야 한다(프롬프트는 "<작업ID> 작업을 평가해줘."만). 작업이 끝나면 log에 기록하고 커밋·push한다. 같은 작업 평가 실패가 `max_attempts`(3)번이면 failed로 멈춘다.
- 커밋은 `git add`를 따로 실행한 뒤 `harness/.commit-msg.txt` + `git commit -F`로 한다. 끝에 `Co-Authored-By` 줄. force push 금지. push는 `git push origin beta1-1`.
- `harness/tasks.json`은 PowerShell `ConvertTo-Json` 형식(LF, BOM 없음)을 유지한다. Python으로 json.dump 하면 파일 전체 형식이 바뀐다 → 정규식으로 해당 필드만 바꾸고 `newline=''`로 쓴다.
- C# 파일을 Python으로 고칠 때도 `newline=''`(원래 줄바꿈 유지), GameConfig.cs는 BOM 없음.
