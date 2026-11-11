---
name: project-setup
description: 템플릿으로 만든 새 프로젝트의 첫 세션 절차. 환경 확인, 프로젝트 값 채우기, 사용자와 함께 GDD 기획, 작업 목록 설계, 기획 확정(잠금)까지. 사용자가 "초기 설정해줘", "새 프로젝트 시작", "기획 같이 하자", "README 보고 설정해줘"라고 할 때, 또는 harness/project.json의 setup_done이 false일 때 사용.
---

# 새 프로젝트 시작 절차

목표: 사용자는 **기획 결정과 승인**만 하고, 나머지 준비는 Claude가 한다.
한 단계가 끝날 때마다 무엇을 했고 다음에 무엇을 할지 짧게 알린다. 사용자 결정이 필요한 곳에서는 멈추고 묻는다.

## 1. 환경 확인 (Claude)
아래를 확인하고 결과를 표로 보여 준다. 없는 것은 설치 방법을 안내한다.
| 항목 | 확인 방법 |
|---|---|
| Git, 원격 저장소 | `git remote -v` (템플릿으로 만든 저장소면 이미 연결돼 있다) |
| Unity 프로젝트 | `ProjectSettings/ProjectVersion.txt` 존재 여부와 버전 |
| Unity 에디터 | `harness/scripts/common.ps1`의 `Get-UnityExe`가 찾는지 (Hub 기본 경로) |
| VARCO 3D MCP | `mcp__varco-3d__get_account_and_pricing` 호출 가능 여부, 등급과 단가 |
| Blender (선택) | `harness/scripts/blender-run.ps1 -Check` |

- Unity 프로젝트가 없으면: 사용자에게 Unity Hub에서 **이 폴더를 위치로** 새 프로젝트를 만들어 달라고 안내한다.
  권장 템플릿은 "Universal 3D"(URP). Hub가 하위 폴더를 만들면, 그 안의 내용을 저장소 루트로 옮기는 것도 Claude가 도울 수 있다.
- Unity 에디터가 열려 있으면 배치 모드가 실패한다. 이후 검증 단계 전에는 닫아 달라고 한다.

## 2. 프로젝트 값 채우기 (Claude가 묻고 기록)
사용자에게 묻는다: 게임 제목(한글 가능), 영문 프로젝트 이름. 나머지는 Claude가 제안하고 확인받는다.
- `namespace`: PascalCase 영문 (예: `ArtRoomEscape`)
- `exe_name`: 영문, 공백 없이 (예: `ArtRoomEscape`)
- `repo_url`: `git remote get-url origin`
- `unity_version`: ProjectVersion.txt에서 읽는다
기록할 곳:
- `harness/project.json`에 값을 채운다.
- `CLAUDE.md`의 `{{...}}` 자리표시를 채운다.
- `harness/unity-templates/*.txt`의 `{{NAMESPACE}}`, `{{EXE_NAME}}`을 바꿔 아래 위치에 복사한다.
  - `Assets/Scripts/<NS>.Game.asmdef`, `Assets/Scripts/Core/GameConfig.cs`
  - `Assets/Editor/<NS>.Editor.asmdef`, `Assets/Editor/BuildScript.cs`
  - `Assets/Tests/EditMode/<NS>.Tests.EditMode.asmdef`, `Assets/Tests/PlayMode/<NS>.Tests.PlayMode.asmdef`
- `Packages/manifest.json`에 필요한 패키지를 넣는다: `com.unity.cloud.gltfast`, `com.unity.inputsystem`, `com.unity.ai.navigation`,
  `com.unity.test-framework`, `com.unity.ugui`. 이미 있으면 버전은 그대로 둔다.
- 템플릿 잔여물(Readme, TutorialInfo, SampleScene)은 T01 작업에서 지운다(여기서는 하지 않는다).

## 3. 기획 함께 쓰기 (사용자 결정 + Claude 작성)
`docs/GDD.template.md`를 `docs/GDD.md`로 복사해 채운다(복사 후 틀 파일은 지운다).
**아직 `harness/gdd.lock`이 없으므로 Claude가 편집할 수 있다.**
1. 먼저 5~7개 질문을 한 번에 묻는다 (한 번에 하나씩 묻지 않는다):
   - 어떤 게임인가(한 줄), 장르와 시점, 플레이 시간, 핵심 재미, 승리/패배 조건, 조작, 분위기/참고 작품, 에셋 사용 여부(VARCO), 움직이는 캐릭터 여부(리깅/애니메이션)
2. 답을 바탕으로 GDD 초안을 쓴다. **모든 수치는 구체적인 숫자로** 적는다(속도 m/s, 거리 m, 각도, 개수, 시간).
   정하지 않은 값은 Claude가 제안하고 "(제안)"으로 표시한다.
3. 사용자가 수정을 요청하면 반영한다. 만족할 때까지 반복한다.
4. 에셋 목록(5장)이 정해지면 `harness/asset_specs.json`의 `assets`를 채운다(id, 이름, 높이, 폴리곤, 텍스처, 프롬프트, 필요하면 rig/animations).
   움직이는 캐릭터는 `"rig": "humanoid"`, `"animations": [...]`을 넣는다. 동작 이름은 `get_animation_catalog`로 확인한다.

## 4. 작업 목록 설계 (Claude 제안 → 사용자 승인)
`harness/tasks.json`을 GDD에서 만든다. 아래 순서를 기본으로 한다(이전 프로젝트에서 끝까지 검증된 흐름):
1. 기본 설정 (asmdef, GameConfig, 패키지, 템플릿 정리, GameConfig 값 테스트)
2. 회색 상자 맵 (SceneBuilder, 배치, 조명, NavMesh)
3~6. 핵심 시스템을 기능별로 (플레이어 → 적/규칙 → 아이템/목표 → UI/메뉴)
7. **[사람] 회색 상자 플레이 확인** (빌드 + smoke-test 통과 후)
8. 에셋 생성 (VARCO, 리깅/애니메이션 포함)
9. 플레이스홀더를 모델로 교체 (+ 애니메이션 연결)
10. 분위기·마무리 + 최종 빌드 + smoke-test
11. **[사람] 최종 플레이 확인**

작업마다 지켜야 할 것:
- `acceptance`는 **확인할 수 있는 문장**으로 쓴다(테스트나 파일로 판정 가능). "자연스럽게" 같은 말은 "사람 확인" 작업으로 보낸다.
- `skill`(unity-feature / varco3d-asset / blender-tools), `depends_on`, `max_attempts`(보통 3, 사람 작업 1)를 넣는다.
- 빌드가 있는 마지막 작업의 acceptance에는 `smoke-test-build.ps1 통과`를 넣는다.

작업 목록을 표로 보여 주고 승인을 받는다. 승인 전에는 tasks.json만 고치고 루프를 시작하지 않는다.

## 5. 기획 확정 (사용자 명시적 승인 → 잠금)
- 사용자가 채팅에서 **"기획 확정"**처럼 분명하게 승인했을 때만 셸로 잠금 파일을 만든다:
  `New-Item -ItemType File harness/gdd.lock`
  잠금 이후 GDD는 사용자만 고친다(protect-gdd 훅이 막는다). 다시 함께 고치고 싶으면 사용자가 이 파일을 지운다.
- `harness/project.json`의 `setup_done`을 true로 바꾼다.
- 커밋하고 push한다(`git add -A` → 커밋 메시지 파일 → `git commit -F` → `git push origin <branch>`).
- 사용자에게 다음 단계를 알린다: "`/run-tasks`라고 하시면 T01부터 진행합니다."

## 하지 않는 것
- 사용자 대신 기획을 확정하지 않는다. "좋아 보인다"는 확정이 아니다. 확정 의사가 분명하지 않으면 묻는다.
- VARCO 포인트를 쓰는 작업(이미지·3D 생성)은 이 단계에서 하지 않는다. T08 같은 에셋 작업에서 예상 비용을 알리고 진행한다.
- 저장소를 새로 만들거나 공개 범위를 바꾸지 않는다(사용자가 한다).
