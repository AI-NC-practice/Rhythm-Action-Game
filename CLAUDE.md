# Rhythm-Action-Game — Claude 작업 규칙

> 이 파일은 템플릿에서 왔다. `{{...}}` 자리표시가 남아 있으면 아직 초기 설정 전이다 → `project-setup` 스킬을 먼저 실행한다.

Unity 6000.4.0f1 게임. 기획은 `docs/GDD.md`, 작업 목록은 `harness/tasks.json`, 프로젝트 값은 `harness/project.json`.
GitHub: `https://github.com/AI-NC-practice/Rhythm-Action-Game`, 브랜치 `main`.

## 진행 방식 (가장 중요)
- **새 프로젝트 첫 세션**: `harness/project.json`의 `setup_done`이 false면 `project-setup` 스킬로 시작한다
  (환경 확인 → 프로젝트 값 → 기획 함께 쓰기 → 작업 목록 승인 → 기획 확정).
- 사용자가 `/run-tasks`라고 하면 `run-tasks` 스킬의 절차대로 작업 목록을 순서대로 처리한다.
- **항상 `harness/tasks.json`이 진실이다.** 세션을 시작하면 이 파일부터 읽고 이어서 진행한다.
- 한 번에 작업 하나만 `in_progress`로 둔다.
- 작업 완료 판정은 **내가 하지 않는다.** 반드시 `evaluator` 서브에이전트가 PASS를 줘야 `done`이 된다.
- 같은 작업에서 평가 실패가 `max_attempts`번 쌓이면 `failed`로 바꾸고 멈춰서 사용자에게 보고한다.
- `type: "human"` 작업은 `needs_user`로 바꾸고, 사용자가 할 일을 알려준 뒤 멈춘다.
- 작업마다 `harness/log.md`에 한 단락 기록한다 (무엇을 했고, 평가 결과, 다음 작업).
- 작업이 `done`이 되면 커밋하고 push 한다 (형식은 run-tasks 스킬 6번).
- 범위 밖 아이디어는 `log.md`의 "제안"에 적는다. 넣을지는 사용자가 정한다.

## 절대 규칙
- `docs/GDD.md`는 기획 확정(`harness/gdd.lock`) 뒤에는 수정하지 않는다 (훅이 막는다). 기획과 다른 판단이 필요하면 사용자에게 묻는다.
  확정 전에는 사용자와 함께 쓴다. 잠금 파일은 사용자가 채팅에서 확정을 말했을 때만 만들고, 지우지 않는다.
- 씬(.unity)과 프리팹(.prefab) YAML을 직접 편집하지 않는다. 씬은 `Assets/Editor/SceneBuilder*.cs`로 코드 생성한다.
- 수치(속도, 거리, 각도)는 `Assets/Scripts/Core/GameConfig.cs` 한 곳에만 둔다. GDD 값과 일치해야 한다.
- Unity Editor가 이 프로젝트를 열고 있으면 배치 모드 검증이 실패한다. 그때는 사용자에게 Unity를 닫아 달라고 한다.
- 비용이 드는 일(VARCO 포인트)과 외부에 공개하는 일(저장소 생성, 공개 범위 변경)은 사용자 승인 후에만 한다.

## VARCO 3D (MCP `varco-3d`)
- 에셋은 `varco3d-asset` 스킬 절차로만 만든다. 명세는 `harness/asset_specs.json`.
- **모든 varco-3d 호출에 `flowId`(= asset_specs의 `defaults.flow_id`)를 넣는다.** 다른 플로우는 사용자의 다른 작업물이다.
- 포인트 비용(2026-11 실측): 이미지 약 50(기본 이미지 모델), 3D 생성 200, Rig/Animate 0. 실제 청구액은 run_flow 결과의 `points`로 확인한다.
  에셋당 3D 생성 최대 2회, 이미지 최대 3회. 전체 run_flow 횟수는 훅이 제한한다.
- 움직이는 캐릭터는 T자 자세 → Rig → Animate → FBX (varco3d-asset 8번). Mixamo 동작은 사용자가 받아 넣는다(9번).

## Blender (선택)
- 설치돼 있으면 `blender-tools` 스킬: 미리보기 렌더링, GLB→FBX(텍스처 포함), 삼각형 줄이기. 없으면 건너뛴다.

## Git / GitHub
- 커밋 전에 `git add`를 **별도 명령**으로 먼저 실행한다 (훅이 스테이징된 내용을 검사하기 때문).
- 커밋 메시지는 `harness/.commit-msg.txt`에 쓰고 `git commit -F harness/.commit-msg.txt`로 커밋한다 (한글/여러 줄 안전).
- force push 금지 (훅이 막는다). push가 거부되면 pull/merge 하지 말고 멈춰서 사용자에게 보고한다.
- push가 네트워크 오류로 실패하면 log.md에 남기고 계속 진행한다. 다음 push 때 함께 올라간다.
- Git LFS는 쓰지 않는다. 파일 하나가 20MB를 넘으면 훅이 커밋을 막는다.
- 비밀 정보(키, 토큰)가 스테이징되면 훅이 커밋을 막는다.

## 명령어 (모두 프로젝트 루트에서)
```
powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-verify.ps1        # 컴파일 + EditMode/PlayMode 테스트
powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.SceneBuilder.BuildAll   # 씬 생성
powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.BuildScript.BuildFinal  # 빌드
powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/smoke-test-build.ps1    # 빌드한 exe를 화면 없이 실행해 로그 검사
powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/download-asset.ps1 -Id <id> -Url "<url>"   # 생성된 GLB 내려받기 + 검사
powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/check-glb.ps1 -Path Assets/Art/Models/<id>.glb  # 삼각형/용량 검사
powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/extract-glb-textures.ps1 -Glb <glb> -OutDir <폴더>   # GLB 텍스처 꺼내기
powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/blender-run.ps1 -Check   # Blender 확인 (선택)
```
Unity 배치 모드는 1~3분 걸린다. 결과 요약은 `harness/reports/verify-latest.md`.
PowerShell 스크립트에 한글을 쓰면 **UTF-8 BOM**으로 저장한다 (Windows PowerShell 5.1은 BOM이 없으면 CP949로 읽어 줄이 사라질 수 있다).

## 코드 구조
- `Assets/Scripts/` (asmdef `RhythmActionGame.Game`, 네임스페이스 `RhythmActionGame`) — 게임 코드
- `Assets/Editor/` (asmdef `RhythmActionGame.Editor`) — SceneBuilder, 빌드 스크립트
- `Assets/Tests/EditMode/`, `Assets/Tests/PlayMode/` — 테스트
- `Assets/Art/Models/` — VARCO 3D GLB/FBX, `Assets/Art/Animations/Mixamo/` — 사용자가 넣은 Mixamo 동작
- 입력은 Input System (`Keyboard.current`, `Mouse.current`) 사용. 구 Input Manager 금지.
