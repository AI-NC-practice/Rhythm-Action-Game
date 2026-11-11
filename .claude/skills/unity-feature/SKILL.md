---
name: unity-feature
description: Unity 게임 기능(스크립트, 씬, UI, 빌드)을 구현할 때의 절차와 규칙. tasks.json에서 skill이 unity-feature인 작업을 할 때 사용.
---

# Unity 기능 구현 절차

프로젝트 이름, 네임스페이스, 어셈블리 이름은 `harness/project.json`과 `CLAUDE.md`를 따른다.
아래 예시의 `<NS>`는 그 네임스페이스(예: `MyGame`)다.

## 1. 설계 먼저 (테스트 가능한 구조)
- **판정 로직은 순수 C# 정적 함수/클래스**로 만든다 (예: `LightExposure.IsLit(...)`, `ItemPlacement.Pick(seed)`).
  MonoBehaviour는 입력을 모아 이 함수를 부르는 얇은 껍데기로 둔다. 그래야 EditMode 테스트로 빠르게 검증된다.
- 수치는 `Assets/Scripts/Core/GameConfig.cs`의 상수만 쓴다. 매직 넘버 금지. GDD 값과 같아야 한다.
- 이벤트는 정적 C# event 클래스(`GameEvents`)로 전달한다. 구독자는 OnDisable에서 해제한다.
- 무작위는 시드를 받는 순수 함수로 만든다. 그래야 "시드가 다르면 결과가 다름"을 테스트할 수 있다.

## 2. 씬은 코드로 만든다
- 오브젝트 배치, 컴포넌트, UI, 조명, NavMesh 베이크는 모두 `Assets/Editor/SceneBuilder*.cs`(partial 클래스)의 `BuildAll()`에 둔다.
- 실행: `powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/unity-exec.ps1 -Method <NS>.EditorTools.SceneBuilder.BuildAll`
- `BuildAll()`은 여러 번 실행해도 같은 결과여야 한다 (기존 씬을 지우고 새로 만든다).
- 배치 데이터(위치, 크기)는 `Assets/Scripts/Core/` 아래 정적 클래스(예: `MapLayout`)에 두고 SceneBuilder와 테스트가 함께 쓴다.
- 플레이스홀더 이름은 에셋 ID와 맞춘다: `Prop_<에셋ID>`. 나중에 3D 모델로 바꿀 때 이름으로 찾는다.
- GLB 모델이 있으면 모델을, 없으면 회색 상자를 쓰도록 만든다 (에셋 생성 전에도 게임이 돌아가게).
- 씬/프리팹 YAML 직접 편집 금지.

### 알려진 함정 (실제로 겪은 것)
- **NavMeshAgent는 씬에 꺼진 상태로 저장하고 `Start`에서 켠다.** 켜진 채로 저장하면 빌드에서 NavMesh 데이터보다 먼저 만들어져
  "Failed to create agent because there is no valid NavMesh"가 나고 캐릭터가 움직이지 않는다. 테스트는 Warp를 써서 이걸 놓치기 쉽다.
  → "씬을 막 불러온 상태에서 텔레포트 없이 움직이는지" 테스트를 꼭 하나 둔다.
- 메뉴가 열리며 `Time.timeScale = 0`이 되면 `WaitForSeconds`가 끝나지 않는다. 그 뒤 기다릴 때는 `WaitForSecondsRealtime`.
- 에디트 모드 테스트에서 오브젝트 크기를 바꾼 뒤 콜라이더 bounds를 읽으려면 `Physics.SyncTransforms()`가 필요하다.
- 배치 모드에는 창이 없어 `Cursor.lockState`가 적용되지 않는다. "잠금을 요청했는지"를 별도 값으로 기록해 테스트한다.
- `Mathf.SmoothStep(from, to, t)`은 GLSL `smoothstep(edge0, edge1, x)`와 다르다.
- 트리거 콜라이더(조준용 등)는 겹침 검사에서 `QueryTriggerInteraction.Ignore`로 빼야 한다.

## 3. 테스트 작성
- acceptance 항목마다 가능한 한 테스트 하나를 만든다. 테스트 이름에 작업 ID를 붙인다: `T04_Enemy_StopsWhenLit`.
- 순수 로직 → `Assets/Tests/EditMode/`
- 씬, 물리, NavMesh가 필요한 것 → `Assets/Tests/PlayMode/` (`SceneManager.LoadScene("Main")` 후 검증)
- 입력이 필요한 테스트는 입력 대신 공개 메서드(예: `PlayerController.Move(Vector2 input, float dt)`)를 호출한다.
- 테스트가 항상 통과하도록 기대값을 구현에 맞춰 바꾸지 않는다. **GDD 값이 기준**이다.
- 사람 눈으로만 판단할 수 있는 항목(분위기, 재미)은 테스트 대신 `notes`에 "사람 확인 필요"라고 적는다.
- 눈으로 확인이 필요한 것은 **캡처를 남긴다**: 테스트에서 카메라를 RenderTexture로 렌더링해
  `harness/reports/<작업ID>/*.png`로 저장하면 Claude와 평가자가 Read 도구로 직접 볼 수 있다.

## 4. 한글 UI
- uGUI `UnityEngine.UI.Text` + `Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic"}, 32)` 사용 (Windows).
- 메뉴 버튼은 `Navigation.Mode.None`, EventSystem은 `InputSystemUIInputModule`.
- 테스트: `font.RequestCharactersInTexture(문구)` 후 각 글자 `GetCharacterInfo`가 true인지.

## 5. 검증
- 씬을 바꿨으면 먼저 `unity-exec.ps1`로 BuildAll을 실행하고, 이어서 `unity-verify.ps1`을 실행한다.
- 실패하면 `harness/reports/verify-latest.md`와 로그를 읽고 고친다.
- 둘 다 통과하면 run-tasks 절차의 평가 단계로 넘어간다.

## 6. 빌드 (빌드가 필요한 작업만)
- `Assets/Editor/BuildScript.cs`(템플릿: `harness/unity-templates/BuildScript.cs.txt`)의 정적 메서드를 `unity-exec.ps1`로 실행한다.
  - `<NS>.EditorTools.BuildScript.BuildGray` → `Builds/Gray/<실행파일>.exe` (회색 상자 버전, 사람 확인용)
  - `<NS>.EditorTools.BuildScript.BuildFinal` → `Builds/Final/<실행파일>.exe`
- 빌드 후에는 **반드시** 화면 없이 실행해 로그를 검사한다:
  `powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/smoke-test-build.ps1`
  테스트는 다 통과해도 빌드에서만 생기는 문제(셰이더 누락, 초기화 순서)를 여기서 잡는다.
- glTFast 모델의 셰이더는 빌드 로그에서 Built-in 패스가 0으로 보여도 정상이다. URP의 "Universal Forward" 패스가 컴파일됐는지 확인한다.

## 7. 애니메이션 캐릭터 (리깅된 모델이 있을 때)
- VARCO Rig/Animate 결과는 **FBX**로 가져온다. 템플릿 `harness/unity-templates/HumanoidImport.cs.txt`를 참고해서 아래처럼 설정한다.
  - `ModelImporter.animationType = Human`, 아바타 `CreateFromThisModel`
  - 클립은 `loopTime`, 루트 회전·높이·위치 고정
  - Animator Controller 생성
- VARCO FBX는 텍스처를 넣지 않고 `base_color.png`, `normal.png`를 이름으로만 참조한다.
  같은 결과의 GLB에서 이미지를 꺼내 FBX 옆에 그 이름으로 두면 연결된다 (`blender-tools` 스킬로 FBX에 직접 넣어도 된다).
- 이동은 NavMeshAgent(또는 CharacterController)가 하고 `applyRootMotion = false`. 애니메이션은 제자리(inPlace)로 받는다.
- **발 미끄러짐 확인**: 이동 속도와 애니메이션 본래 걸음 속도를 비교한다.
  디딤발(더 낮은 발)이 몸 기준으로 뒤로 가는 속도가 그 애니메이션의 걸음 속도다.
  차이가 크면 `animator.speed = 이동속도 / 걸음속도`로 맞추거나 걸음 속도가 맞는 동작을 고른다.
- **진행 방향 확인**: `animator.bodyRotation * Vector3.forward`와 이동 방향의 내적이 0.9 이상인지 테스트한다.
- Mixamo 동작: 사용자가 Mixamo에서 "Without Skin" FBX로 받아 `Assets/Art/Animations/Mixamo/`에 넣는다.
  Humanoid로 가져오면 VARCO 리깅 캐릭터(Humanoid)에 그대로 리타기팅된다. Claude는 Mixamo에 로그인하거나 웹을 자동 조작하지 않는다.
