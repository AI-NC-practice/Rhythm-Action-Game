# B16 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | PERFECT ±90 ms, NORMAL ±180 ms, 무시 기준 −300 ms 유지 (GameConfig와 EditMode 테스트) | PASS | GameConfig.cs의 PerfectWindowMs=90, NormalWindowMs=180, EarlyIgnoreMs=300. B16_JudgeWindowTests.B16_Config_MatchGddBetaB84가 세 값을 검사하고 통과했다. T01_GameConfigTests도 새 값으로 바뀌었다. |
| 2 | 경계값 테스트: ±90 ms 안 PERFECT, ±180 ms 안 NORMAL, −300~−180 ms FAIL, +180 ms가 지나면 무입력 FAIL | PASS | B16_Boundaries가 ±89.9/−90은 PERFECT, ±90.1은 NORMAL, 179.9/−180은 NORMAL, −180.1/−299.9는 FAIL인지 검사한다. B16_TooEarlyIgnored_LateMissedAfter180이 −300.1 무시, +180.1 판정 대상 아님, IsMissed(0.1799/0.1801), 다른 키 FAIL을 검사한다. 모두 통과했다. |
| 3 | 기존 ±60/±150 경계 테스트를 GDD-beta 값으로 바꾸고 근거 주석을 단다 | PASS | git diff를 보면 T01, T03, B12_SmoothClock, B12_InputTiming, B05 테스트가 새 값으로 바뀌었고 "beta1-1: GDD-beta B8.4" 주석이 있다. Scripts와 Tests를 grep으로 찾아봤을 때 ±60/±150 경계를 쓰는 곳은 남아 있지 않다. |
| 4 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 126/126, PlayMode 92/92 (재실행 후 harness/reports/verify-latest.md로 확인)
