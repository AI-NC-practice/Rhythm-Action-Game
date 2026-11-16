# B47 평가 보고서
판정: PASS (3번 기준의 사용자 확인 부분은 HUMAN으로 제외)

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 회피 슬로모션 4곳(대문, 포위 동시 공격 마지막, 무사 A37.5, A66). 1박 전부터 0.25박에 걸쳐 시계·음악·화면 0.4배. 음악은 낮고 먹먹하게. 판정 뒤 0.15초에 복귀. 곡과 판정 박의 관계 유지 | PASS | `B47_Config_MatchesGddBetaB17_4`가 값(0.4, 1박, 0.25박, 0.15초, 저역 통과 1500Hz 미만, 음악 0.6배 미만)을 GDD-beta B17.4와 맞춰 본다. `B47_Spots_Gate_SurroundLastSimultaneous_BossCounterAndLastFlurry`가 4곳이 맞고 일반 적에는 없음을 확인한다. `B47_SlowsClockMusicScreen_FromOneBeatBefore_ReturnsAfterJudge`: 목표 1박 전에 시작, 보통 판정은 느려지지 않음, clock·`Time.timeScale`·`music.pitch` 0.4배, 낮은 컷오프·음악 게인, PERFECT 뒤 0.15초에 1로, FAIL 뒤에도 복귀, 박은 끊기지 않음. `B47_GameScene_ClosingGate_BulletTime_NoPerfectSlowMo`는 실제 Game 씬 5챕터 대문에서 같은 흐름을 확인한다. |
| 2 | B4 2·3번 PERFECT 뒤 슬로모션 제거, 처형·윈드밀 슬로모션은 유지하고 음악 먹먹하게 | PASS | `PerfectCandidateSlowMo=false`가 `TimeFx.cs:61`과 `StageDirector.cs:271`에 연결. 씬 테스트는 `slowMotion.TriggerCount`가 늘지 않음을 확인. 단위 테스트는 `slow.Trigger()` 때 clock 1 유지, `Muffle` 1, `Cancel` 뒤 0을 확인. |
| 3 | unity-verify 통과 | PASS | 아래 실행 결과 참고. |
| 3b | 테스트 빌드로 사용자 확인 | HUMAN | 사용자가 직접 판단할 항목. |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 173/173, PlayMode 149/149, OVERALL: PASS. B47 테스트 5개 모두 Passed.
