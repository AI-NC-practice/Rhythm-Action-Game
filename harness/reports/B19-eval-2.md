# B19 평가 보고서 (2회차)
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | B9.1 일반 적 공격 FAIL은 체력 −1·속도 ×0.8 후 다음 공격으로 넘어감. 마무리 FAIL은 체력 −1 + 밀쳐 쓰러뜨리기로 끝나고 다음 마디에 달림 (재도전 없음) | PASS | B19_Duel_AttackFailContinues_FinisherFailPushDown(B19_RewindTests.cs:59-74)과 B19_Duel_FinisherFail_PushDown_SpeedDown_ResumesNextBar(:78-97). 후자는 체력 2, 속도 1.30→1.04, PushDown, Attempts 1, ResumeBeat가 마디 첫 박인지를 직접 검사한다. 1회차에서 지적한 보강이 반영됐다. |
| 2 | B9.2 윈드밀 FAIL은 체력 그대로, 첫 화살표 4박 앞으로 되감고 3단계 다시 | PASS | B19_Windmill_Fail_RewindsToFirstArrowMinusOneBar_NoHpLoss(:101-119)가 체력 유지, 박자 시계 = 첫 화살표 − RewindLeadBeats(4), 같은 박자에 다시 나옴을 확인한다. |
| 3 | B9.2 무사 FAIL은 체력 그대로, 첫 공격 1마디 앞으로 되감고 처음부터 다시 | PASS | B19_RewindPresentation_PausedClock_FxSoundStreak(:149-178)가 체력 3 유지와 시계가 cues[0].TargetBeat − 4로 돌아가는지 확인한다. T07_Boss_Fail_RetriesFromFirstAttack도 통과했다. |
| 4 | B9.3 체력 0이면 챕터 시작으로 되감기, 체력 3, 점수 유지, 랭크 "-" | PASS | B19_HpZero_RewindsToChapterStart_KeepsScore_RankDash(:123-145)가 게임 오버 아님, RunBeat 56, 체력 3, 점수 유지, Rank(true) "-"를 검사한다. |
| 5 | B9.4 연출 0.8초(시계 정지, 채도 −90, 노출 −0.5, 먹 줄기, 카메라 0.3 m, 소리), 끝나면 재시작, 남은 원 없음 | PASS | B19_RewindPresentation_*(:155-178)가 시계 정지, TapeRewind 소리, RewindStreak 이펙트, 채도 ≤ −89.9, 노출 −0.5, 카메라 z < −0.25, 0.8초 뒤 재개, 원 0개를 확인한다. B19_Config_MatchGddBetaB9가 설정값(0.8, 4, −90, −0.5, 0.3)을 확인한다. |
| 6 | 기존 재도전 테스트(T06, T07, B11)를 B9 규칙으로 바꾸고 근거 주석을 단다 | PASS | T06, T07, B11 PlayMode 테스트가 모두 통과한다. 1회차에서 실패한 B11_AnyJudgedCue_RingGoneNextFrame은 이번 두 번의 실행 모두 통과했다. |
| 7 | unity-verify.ps1 전체 통과 | PASS | 두 번 연속 재실행해 모두 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음. 참고: B11_AnyJudgedCue_RingGoneNextFrame은 간헐 실패였을 수 있으니, 다시 실패하면 확인이 필요하다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 126/126, PlayMode 103/103, OVERALL PASS (2회 연속)
