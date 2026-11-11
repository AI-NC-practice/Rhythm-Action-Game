---
name: run-tasks
description: harness/tasks.json의 작업을 순서대로 실행하는 메인 루프. 사용자가 /run-tasks, "작업 진행해", "다음 작업" 등을 요청할 때 사용.
---

# 작업 루프 절차

이 절차를 **남은 작업이 없거나 멈춤 조건에 걸릴 때까지** 반복한다.

## 시작 전 확인
- `harness/gdd.lock`이 없으면 기획이 아직 확정되지 않은 것이다. 루프를 시작하지 말고 `project-setup` 스킬로 기획을 마무리하자고 안내한다.
- `harness/tasks.json`에 작업이 없거나 예시 작업만 있으면 같은 이유로 멈추고 안내한다.

## 0. 루프 모드 켜기
- 시작할 때 빈 파일 `harness/.loop-active`를 만든다.
  이 파일이 있는 동안 Stop 훅이 "남은 작업이 있으면 멈추지 못하게" 막는다.
- 멈춰야 할 때(needs_user, failed, blocked, 모두 완료)는 이 파일을 지운다. (훅도 이 경우 스스로 지운다.)

## 1. 다음 작업 고르기
- `harness/tasks.json`을 읽는다.
- `in_progress`인 작업이 있으면 그것을 이어서 한다 (이전 세션이 중단된 것).
- 없으면 `pending` 중 `depends_on`이 모두 `done`인 **첫 번째** 작업을 고른다.
- 고를 작업이 없으면 → 7번(종료)으로 간다.

## 2. 사람 작업이면 멈춤
- `type`이 `human`이면:
  - acceptance 중 Claude가 준비할 수 있는 것(예: 빌드 생성)은 먼저 해 둔다.
  - status를 `needs_user`로 바꾸고, 사용자가 할 일을 번호 목록으로 안내한 뒤 **멈춘다.**
  - 사용자가 확인을 알려주면 `done`으로 바꾸고 1번부터 다시 시작한다.

## 3. 작업 시작
- status를 `in_progress`로, `attempts`를 1 올려서 저장한다.
- `docs/GDD.md`에서 이 작업과 관련된 절을 읽는다.
- 작업의 `skill` 필드에 적힌 스킬(`unity-feature`, `varco3d-asset`, `blender-tools`) 절차를 따른다.

## 4. 자체 점검
- `unity-verify.ps1`을 실행한다. 컴파일 오류나 실패한 테스트가 있으면 **평가를 맡기기 전에** 고친다.
- 자체 점검은 attempts에 포함하지 않는다.

## 5. 독립 평가
- `evaluator` 서브에이전트를 호출한다. 프롬프트에는 **작업 ID만** 준다:
  `"T0N 작업을 평가해줘."`
  (구현 설명이나 "잘 됐다"는 말을 덧붙이지 않는다. 평가자가 선입견 없이 보게 하기 위해서다.)
- 평가 결과 전문을 `harness/reports/<작업ID>-eval-<attempts>.md`로 저장한다.

## 6. 결과 처리
- **PASS** →
  - status `done`, notes에 한 줄 요약
  - `harness/log.md`에 기록
  - 커밋과 push (아래 순서 그대로, 명령 하나씩):
    1. `git add -A`
    2. `harness/.commit-msg.txt`에 메시지 작성 (UTF-8):
       ```
       <작업ID>: <작업 제목>

       - 평가: PASS (<attempts>회차)
       - 테스트: EditMode <통과>/<전체>, PlayMode <통과>/<전체>
       - 평가 보고서: harness/reports/<작업ID>-eval-<attempts>.md

       <Claude Code가 안내하는 Co-Authored-By 줄>
       ```
    3. `git commit -F harness/.commit-msg.txt` — 훅이 막으면(비밀 정보, 큰 파일) 원인을 고치고 다시 커밋한다.
    4. `git push origin main`
       - 거부(rejected)되면: 멈추고 사용자에게 보고. pull/merge/force 하지 않는다.
       - 네트워크/인증 오류면: log.md에 "push 보류"를 남기고 계속 진행한다.
  - 1번으로 돌아가 다음 작업
- **사람 작업(human)**: 사용자가 확인을 알려줘서 `done`이 되면 같은 방식으로 커밋과 push 한다.
- **FAIL** →
  - `attempts < max_attempts`면: 평가 보고서의 실패 항목만 고치고 4번부터 다시
  - `attempts >= max_attempts`면: status `failed`, notes에 실패 이유, log 기록 후 **멈추고** 사용자에게 보고

## 7. 종료
- 모든 작업이 `done`이면 `harness/log.md`에 최종 요약을 쓰고 사용자에게 보고한다.
- 멈출 때는 항상: 지금 상태, 멈춘 이유, 사용자가 할 일을 짧게 알린다.

## 주의
- 작업 범위를 넘는 개선은 하지 않는다. 떠오른 아이디어는 log.md의 "제안" 항목에 적는다.
  사용자는 제안을 보고 넣을지 말지 결정한다. 승인되면 tasks.json에 새 작업으로 추가한다.
- 사람 확인 작업(`type: human`) 전에는 빌드를 만들고 `smoke-test-build.ps1`까지 통과시켜 둔다.
- acceptance를 바꾸고 싶으면 사용자에게 묻는다. 스스로 기준을 낮추지 않는다.
