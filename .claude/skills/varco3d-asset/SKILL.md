---
name: varco3d-asset
description: VARCO 3D MCP(varco-3d)로 텍스트 프롬프트에서 3D 에셋(GLB)을 생성하고 검사하는 절차. tasks.json에서 skill이 varco3d-asset인 작업, 또는 에셋 생성/재생성 요청 시 사용.
---

# VARCO 3D 에셋 생성 절차 (MCP)

경로: `TextInput(프롬프트) → GenerateImage(이미지) → Generate3D(메시)` — 모두 VARCO 안에서 처리한다.
명세는 `harness/asset_specs.json`. 비용: 이미지 약 50포인트(기본 모델 `gpt-image-2.5-flare` 기준, 모델에 따라 다름), 3D 200포인트 (에셋당 약 250).
실제 청구액은 `run_flow` 결과의 `points`로 나오니 매번 log.md에 기록한다. 시작 전 `get_account_and_pricing`으로 단가를 확인한다.

### 알아 둘 점 (2026-11 실측)
- 이미지 출력 URL은 `/api/objects/<hash>.png` 같은 **상대 경로**로 온다. 내려받을 때 앞에 `https://3d.varco.ai`를 붙인다. 3D(glb) URL은 전체 주소로 온다.
- 같은 종류의 노드는 한 번의 `run_flow`에 묶어 실행할 수 있다 (예: 이미지 11개를 한 번에). 실행 횟수 예산을 아낀다.
- "dim room mood" 같은 분위기 문구는 배경에 가구와 선반을 그려 넣게 만든다. 이미지가 지저분하면 `single object only, nothing else in the image, isolated on a plain seamless light gray studio background`로 바꿔 다시 생성한다.

## 0. 사전 확인
- `asset_specs.json`의 `defaults.flow_id`가 비어 있으면 **멈추고** 사용자에게 이 프로젝트 전용 플로우를 만들어 달라고 한다
  (`art_refs/README.md` 참고). 다른 프로젝트의 플로우나 기존 `Untitled` 플로우는 다른 작업물이므로 절대 쓰지 않는다.
- 3D 생성 전에 예상 포인트(이미지 약 50 × 장수 + 3D 200 × 개수)를 사용자에게 알리고 승인을 받는다.
- **모든 varco-3d 도구 호출에 `flowId`를 반드시 넣는다.** 생략하면 브라우저에서 마지막에 연 플로우가 대상이 된다.
  (`varco-guard` 훅이 flowId가 없거나 다르면 막는다.)
- 처음 한 번 `get_node_catalog`로 포트 이름과 파라미터를 확인한다.

## 1. 대상 고르기
- status가 `pending` 또는 `failed`이고 `calls.mesh < 2`인 에셋.

## 2. 노드 만들기 (에셋마다 한 줄로 배치)
- 에셋 i번째는 y = i × 400, x = 0 / 400 / 800 위치에 둔다.
- `TextInput` : value = `prompt + ", " + defaults.style_suffix`
- `GenerateImage` : count 1, aspectRatio `1:1` (model은 비워 둔다)
- `Generate3D` : count 1, polygonCount = 에셋의 `polygon_count`, topology `tri`,
  textureSize = 에셋의 `texture_size`, generateTexture 1, usePbrTexture 0, tPose 0, useAlpha 0,
  license `PRIVATE`
- 연결: TextInput.out → GenerateImage.prompt, GenerateImage.out → Generate3D.image
- 만든 노드 id를 에셋의 `nodes`(text, image, mesh)에 기록한다. 이미 있으면 새로 만들지 않고 재사용한다.
- 마지막에 `get_flow_graph`로 겹치는 노드를 `move_node`로 정리한다.

## 3. 이미지 먼저 (싸게 확인)
- `run_flow` scope = [image 노드]. `get_flow_graph`로 상태를 확인한다 (약 40초).
- 결과 이미지 URL을 `harness/reports/varco/<id>.png`로 내려받아 **직접 열어 본다** (Read 도구).
  확인 항목: 물체 하나, 전체가 보임, 단순 배경, 프롬프트와 맞음.
- 문제가 있으면 프롬프트를 고쳐 다시 생성한다. 에셋당 이미지는 최대 3회 (`calls.image`).

## 4. 3D 생성
- 이미지가 괜찮을 때만 `run_flow` scope = [mesh 노드]. 약 4분, 길면 10분 이상 걸린다. 오래 걸려도 실패가 아니다.
- 호출할 때마다 `calls.mesh`를 1 올린다 (실패해도). **에셋당 최대 2회.**

## 5. 다운로드와 검사
- `get_output_downloads` : nodeId = mesh 노드, exportType `glb`, pivotToBottom true
- 받은 URL로:
  `powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/download-asset.ps1 -Id <id> -Url "<url>"`
  → `Assets/Art/Models/<id>.glb` 저장 + 삼각형 5,000개, 파일 15MB 상한 검사
- 통과하면 `status: generated`, `triangles`, `file_mb` 기록.
- 상한 초과면: 같은 메시에 `Remesh` 노드(무료, polygonCount를 낮게)를 붙여 다시 내려받는다. 3D 재생성보다 먼저 시도한다.

## 6. 실패 처리
- `calls.mesh`가 2인데도 실패하면 `failed`로 두고 notes에 이유를 남긴 뒤 다음 에셋으로 넘어간다.
- 실행 횟수 총량은 `defaults.max_run_flow_calls`로 제한된다 (훅이 강제). 초과하면 멈추고 보고한다.

## 7. Unity 확인과 보고
- 모든 대상 처리 후 `unity-verify.ps1`로 GLB 임포트 오류가 없는지 확인한다.
- 에셋별 결과 표(ID, 삼각형, 파일 MB, 이미지/3D 호출 수, 상태)를 log.md에 남긴다.
- 실제로 쓴 포인트(`run_flow` 결과의 `points` 합)도 함께 적는다.

## 8. 리깅 + 애니메이션 (에셋에 `rig`가 있을 때)
asset_specs의 에셋 항목에 `"rig": "humanoid"`(또는 `humanoid-fingers`, `quadruped`)와
`"animations": ["walk_looping", ...]`이 있으면 2~5번 대신 아래 순서로 한다. (2026-11 실험으로 확인한 절차)

1. **T자 자세 이미지**: 프롬프트에 `standing in a T-pose, both arms stretched straight out horizontally, legs straight and slightly apart, front view, full view head to feet`를 넣는다.
   `style_suffix`의 3/4 view 대신 front view를 쓴다. 이미지를 내려받아 팔이 수평인지 직접 확인한다.
2. **노드 연결**: `Generate3D(tPose: 1)` → `Rig(riggingMode)` → 애니메이션마다 `Animate(animation, inPlace: 1)` 노드 1개.
   - Rig 입력/출력 포트는 `mesh`, Animate 입력 포트도 `mesh`.
   - 애니메이션 이름은 `get_animation_catalog`로 확인한다. 결과가 매우 크다(860여 개). 파일로 저장된 결과를 grep으로 찾는다.
     `isPlus: true` 동작도 이 계정 등급에서 실행된 적이 있다. 거부되면 `isPlus: false` 동작으로 바꾼다.
3. **한 번에 실행**: 3D·Rig·Animate 노드를 하나의 `run_flow` scope에 넣는다. Rig와 Animate는 0포인트였다(3D 200만 청구).
4. **내려받기**: Animate 노드는 출력 포트가 없지만 `get_output_downloads(nodeId=Animate 노드)`로 받을 수 있다.
   - `exportType: fbx` → Unity Humanoid용 (권장). `exportType: glb` → 텍스처가 들어 있는 같은 결과.
   - 저장 위치: `Assets/Art/Models/<id>/<id>_<애니메이션>.fbx`, 원본 GLB는 `harness/reports/varco/<id>/`.
5. **텍스처**: VARCO FBX는 텍스처를 넣지 않고 `base_color.png`, `normal.png`를 이름으로만 참조한다.
   GLB에 들어 있는 이미지를 꺼내 FBX 옆에 그 이름으로 저장한다(`blender-tools` 스킬의 `extract_glb_textures`).
6. **검사**: 뼈 수(humanoid 약 22~23개), 클립 길이, 삼각형 상한. Unity에서는 `unity-feature` 7번 절차로 Humanoid 설정을 하고 걷기를 확인한다.
- 걷기 동작은 **걸음 속도**가 동작마다 다르다(예: `zombie_slow_walk_looping`은 약 0.3m/s).
  게임 속 이동 속도와 많이 다르면 발이 미끄러지니, 속도에 맞는 동작을 고르거나 재생 속도를 맞춘다.

## 9. Mixamo (선택)
- Mixamo는 공식 API가 없고 Adobe 로그인이 필요하다. **Claude는 로그인하거나 웹을 자동 조작하지 않는다.**
- 필요한 동작이 VARCO 카탈로그에 없으면 사용자에게 요청한다:
  "Mixamo에서 <동작>을 **Without Skin, FBX, 30fps**로 받아 `Assets/Art/Animations/Mixamo/`에 넣어 주세요."
- VARCO 리깅 결과가 Humanoid라서 Mixamo 동작은 Unity에서 그대로 리타기팅된다.
