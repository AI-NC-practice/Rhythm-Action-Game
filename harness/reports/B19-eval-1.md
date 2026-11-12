# B19 평가 보고서 (1회차)
판정: FAIL

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | B9.1: 일반 적 공격 FAIL은 체력 −1·속도 ×0.8 후 다음 공격, 마무리 FAIL은 체력 −1 + 밀쳐 쓰러뜨리기, 재도전 없음 | PASS (근거 약함) | B19_Duel_AttackFailContinues_FinisherFailPushDown이 통과했다. 하지만 이 테스트는 마무리 FAIL을 검사하지 않는다(마무리를 PERFECT로 치고 PushDown이 아닌지만 본다). 속도 ×0.8도 검사하지 않는다. 마무리 FAIL → PushDown 경로는 T06 테스트에 의존한다. |
| 2 | B9.2: 윈드밀 FAIL은 체력 그대로, 첫 화살표 1마디 앞으로 되감고 3단계 다시 | PASS (간접 근거) | T07_Windmill_Fail_RetriesOnlyPhase3NextBar 통과. B19 전용 테스트는 없다. |
| 3 | B9.2: 무사 FAIL은 체력 그대로, 첫 공격 1마디 앞으로 되감고 처음부터 다시 | PASS | B19_RewindPresentation_*과 T07_Boss_Fail_RetriesFromFirstAttack 통과. |
| 4 | B9.3: 체력 0이면 챕터 시작으로 되감기, 체력 3, 점수 유지, 랭크 "-" | PASS | B19_HpZero_RewindsToChapterStart_KeepsScore_RankDash 통과. |
| 5 | B9.4: 0.8초 연출 (시계 정지, 채도 −90, 노출 −0.5, 먹 줄기, 카메라 0.3 m, 되감기 소리), 끝나면 재시작, 남은 원 없음 | PASS | B19_RewindPresentation_*, B19_Config_MatchGddBetaB9 통과. |
| 6 | 기존 재도전 테스트(T06, T07, B11)를 B9 규칙으로 바꾸고 근거 주석을 단다 | FAIL | 바꾼 B11 파일 안의 B11_AnyJudgedCue_RingGoneNextFrame이 실패한다. |
| 7 | unity-verify.ps1 전체 통과 | FAIL | 직접 재실행한 결과 OVERALL: FAIL. |

## 실패 항목과 수정 지시
- PlayMode B11_StuckRingTests.B11_AnyJudgedCue_RingGoneNextFrame 실패(B11_StuckRingTests.cs:139, AssertNoDeadRings): "안전망: 화면의 원 수 = 목록의 원 수, Expected: 0 But was: 1". 원인을 찾아 고칠 것.
- 보강할 것: 일반 적 마무리 FAIL(PushDown, 체력 −1, 다음 마디 재개)과 속도 ×0.8을 직접 검사하는 테스트, 윈드밀 FAIL 되감기 박자(첫 화살표 −4박)를 명시적으로 검사하는 테스트.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 126/126, PlayMode 100/101 (실패 1건: B11_AnyJudgedCue_RingGoneNextFrame), OVERALL FAIL
