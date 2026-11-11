# unity-harness-template

Claude Code로 **Unity 게임을 작업 목록 단위로 자동 진행**하기 위한 하네스 템플릿입니다.
기획 → 작업 목록 → (구현 → 테스트 → 독립 평가 → 커밋) 반복 → 사람 확인 → 에셋(VARCO 3D) → 마무리 → 빌드.

"미술실 창고 탈출"(1인칭 공포, Unity 6 URP) 프로젝트에서 실제로 끝까지 써 보고 다듬은 구성입니다.
작업 11개를 모두 1회차 평가로 통과했고, 그 과정에서 찾은 문제와 요령을 스킬과 훅에 반영했습니다.

## 사용자가 하는 일 / Claude가 하는 일
| 사용자 | Claude |
|---|---|
| 무엇을 만들지 정하고 기획을 승인 ("기획 확정") | 질문하고 GDD 초안 작성, 수치 제안 |
| 작업 목록 승인 | GDD에서 작업 목록과 완료 기준 설계 |
| 제안을 넣을지 말지 결정 | 범위 밖 아이디어는 `log.md`의 "제안"에 적기 |
| 중간·최종 플레이 확인 | 구현, 테스트, 씬 생성, 빌드, 빌드 점검 |
| VARCO 플로우 만들기, 포인트 사용 승인 | 에셋 생성(이미지 확인 → 3D → 리깅/애니메이션), 검사 |
| Mixamo 동작 받기 (쓰는 경우) | Unity Humanoid로 가져와 리타기팅 |
| 저장소 만들기, 공개 범위 | 커밋과 push (force push는 훅이 막음) |

## 준비물
- Windows 10/11, PowerShell 5.1 이상
- **Unity Hub + Unity 6** (Windows Build Support). 버전은 프로젝트마다 자유이고, 스크립트가 자동으로 찾습니다.
- **Git**과 GitHub 계정
- **Claude Code** (데스크톱 앱 또는 CLI)
- **VARCO 3D MCP** 연결 (에셋을 만들 때)
- (선택) **Blender 4.2 이상**: 모델 미리보기, GLB→FBX 변환, 삼각형 줄이기. 없으면 해당 기능만 건너뜁니다.

## 새 프로젝트 시작하기
1. GitHub에서 이 저장소의 **Use this template → Create a new repository** (비공개 권장)
2. 새 저장소를 PC에 clone
3. **Unity Hub → New project** → 템플릿 "Universal 3D" → 위치를 **clone한 폴더**로 지정
   (Hub가 하위 폴더를 만들면 Claude가 내용을 루트로 옮기는 것을 도울 수 있습니다)
4. VARCO 3D에서 **이 프로젝트 전용 새 플로우** 만들기 (`art_refs/README.md`)
5. Claude Code에서 **그 폴더로 새 세션**을 열고:
   > 초기 설정해줘. 이런 게임을 만들고 싶어: …
6. Claude와 함께 기획(GDD)과 작업 목록을 정하고, 마음에 들면 **"기획 확정"**
7. `/run-tasks` — 사람 확인 단계에서 멈추면 플레이해 보고 알려 주세요

## 폴더 구성
```
.claude/
  settings.json            권한 + 훅 등록
  agents/evaluator.md      독립 평가자 (코드 수정 권한 없음)
  skills/
    project-setup/         첫 세션: 환경 확인, 기획, 작업 목록, 기획 확정
    run-tasks/             메인 루프 (/run-tasks)
    unity-feature/         Unity 구현 규칙 + 알려진 함정
    varco3d-asset/         VARCO 에셋 생성 (리깅/애니메이션, Mixamo 포함)
    blender-tools/         (선택) Blender 도구
docs/
  GDD.template.md          기획서 틀 (첫 세션에서 docs/GDD.md로 복사)
  하네스_구조.md            각 파일의 역할과 설계 이유
harness/
  project.json             프로젝트 값 (첫 세션에서 채움)
  tasks.json               작업 목록 (형식 예시)
  asset_specs.json         에셋 명세 (형식 예시)
  log.md                   작업 로그와 제안
  scripts/                 Unity 검증·실행·빌드 점검, VARCO 다운로드·검사, Blender 실행
  scripts/hooks/           세션 시작, GDD 보호, git 보호, VARCO 예산, 루프 유지
  unity-templates/         asmdef, GameConfig, BuildScript, Humanoid 임포트 예시
art_refs/README.md         VARCO 플로우 준비와 포인트, Mixamo 안내
```

## 이전 프로젝트에서 배운 것 (스킬에 반영됨)
- **빌드에서만 생기는 문제가 있다.** 테스트를 모두 통과했는데도 빌드에서 적 캐릭터가 움직이지 않았다
  (NavMeshAgent가 NavMesh보다 먼저 생성됨). → 사람 확인 전에 `smoke-test-build.ps1`로 exe를 화면 없이 실행해 로그를 검사한다.
- **VARCO 실제 비용은 이미지 약 50, 3D 200, 리깅·애니메이션 0포인트.** 이미지를 먼저 확인하고, 같은 종류 노드는 한 번에 묶어 실행한다.
- **"분위기" 문구는 배경에 잡동사니를 그린다.** 단독 물체와 스튜디오 배경 문구가 3D 변환에 안전하다.
- **VARCO FBX는 텍스처를 넣지 않는다.** GLB에서 텍스처를 꺼내 옆에 두거나 Blender로 FBX에 넣는다.
- **걷기 동작마다 걸음 속도가 다르다.** 이동 속도와 많이 다르면 발이 미끄러지니, 테스트로 재서 맞춘다.
- **PowerShell 스크립트에 한글을 쓰면 UTF-8 BOM으로 저장한다.** Windows PowerShell 5.1은 BOM이 없으면 CP949로 읽어 줄이 사라질 수 있다.

## 템플릿을 고칠 때
- 템플릿으로 만든 프로젝트는 그 시점의 복사본이라, 템플릿을 고쳐도 기존 프로젝트에 자동으로 반영되지 않습니다.
- 프로젝트에서 하네스를 개선했다면 이 저장소에도 반영해 두세요 (Claude에게 "이 개선을 템플릿에도 반영해줘"라고 요청).
