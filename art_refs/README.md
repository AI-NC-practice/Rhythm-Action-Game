# VARCO 3D 준비 안내 (사용자용)

에셋은 VARCO 3D MCP로 **텍스트에서 바로** 만듭니다 (텍스트 → 이미지 → 3D [→ 리깅 → 애니메이션]).
PNG 이미지를 따로 준비할 필요가 없습니다. 프롬프트는 `harness/asset_specs.json`에 있습니다.

## 1. 이 프로젝트 전용 플로우 만들기 (한 번만, 에셋 작업 전까지)
다른 프로젝트의 플로우나 기존 `Untitled` 플로우에는 다른 작업물이 있으니, 하네스는 지정한 플로우만 쓰도록 막혀 있습니다.

1. 브라우저에서 VARCO 3D(https://3d.varco.ai)에 로그인하고 **새 플로우를 만듭니다.**
2. 이름을 프로젝트 이름으로 바꿉니다.
3. 그 플로우를 한 번 연 상태에서 Claude에게 "VARCO 플로우 만들었어"라고 알려 주세요.
   Claude가 플로우 ID를 찾아 `asset_specs.json`의 `flow_id`에 기록합니다.

## 2. 포인트 (2026-11 실측)
| 단계 | 포인트 |
|---|---|
| 이미지 1장 (기본 이미지 모델) | 약 50 |
| 3D 생성 1개 | 200 |
| 리깅(Rig), 애니메이션(Animate) | 0 |

- 에셋 1개 ≈ 250포인트. 예: 정적 에셋 11개 ≈ 2,750, 이미지 재생성이 몇 번 있으면 조금 더.
- Claude는 3D 생성 전에 예상 포인트를 알리고 승인을 받습니다. 실제 청구액은 log.md에 기록합니다.
- 총 실행 횟수는 `max_run_flow_calls`로 제한됩니다 (포인트가 아니라 실행 횟수 상한).
- 남은 포인트는 VARCO 3D 웹사이트에서 확인하세요.

## 3. 움직이는 캐릭터
- `asset_specs.json`의 에셋에 `"rig": "humanoid"`, `"animations": ["walk_looping", ...]`을 넣으면
  T자 자세 이미지 → 3D → 리깅 → 애니메이션까지 자동으로 진행하고, Unity에는 Humanoid(FBX)로 들어갑니다.
- 애니메이션 이름은 Claude가 VARCO 카탈로그(860여 종)에서 찾아 제안합니다.

## 4. Mixamo 동작을 쓰고 싶을 때
Claude는 Mixamo(Adobe 로그인)에 접근하지 않습니다. 필요한 동작이 있으면 직접 받아 주세요.
1. https://www.mixamo.com 에서 동작을 고르고 **Download**
2. Format **FBX**, Skin **Without Skin**, 30 fps
3. 받은 파일을 `Assets/Art/Animations/Mixamo/`에 넣고 Claude에게 알려 주세요. VARCO 캐릭터에 리타기팅합니다.

## 5. 이 폴더(art_refs)는?
직접 만든 이미지로 에셋을 만들고 싶을 때만 씁니다. 이 경우 Claude에게 "art_refs/xxx.png로 만들어 줘"라고 요청하면
업로드(upload_image) 후 이미지 → 3D로 생성합니다.
