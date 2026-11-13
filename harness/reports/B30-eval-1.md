# B30 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | PERFECT ±90 ms 그대로, NORMAL ±240 ms, −300 ms보다 이른 입력 무시 | PASS | GameConfig PerfectWindowMs=90, NormalWindowMs=240, EarlyIgnoreMs=300. `B16_Config_MatchGddBetaB84`, `B16_Boundaries`, `B16_TooEarlyIgnored_LateMissedAfter240` 통과 |
| 2 | 앞 원에 늦은 입력이 다음 원 창 안이면 키가 맞는 원, 같으면 더 가까운 원. 앞 원은 창이 지나면 FAIL | PASS | `B30_LateInput_GoesToCloserNextCue`: 앞 원 우선·다음 원 PERFECT·앞 원 FAIL·+200 ms NORMAL·키가 맞는 앞 원 우선 |
| 3 | 결투·윈드밀 FAIL → 남은 판정 즉시 지움, 0.7초 피격 연출 뒤 되감기, 체력 그대로 | PASS | `B30_DuelFail_HitPresentation_ThenRewind`, `B30_WindmillFail_HitPresentation_ThenRewind`: HitPending, 원 사라짐, BloodHit·BloodDrops·BloodSplatter·BodyHit, 비네트 > 0.4, 카메라 밀림, 시계 계속, 0.69초 연출 중·0.71초 되감기, HP 3, 윈드밀 RewindKind.Windmill |
| 4 | 기존 판정 창·되감기 테스트 갱신과 근거 주석 | PASS | B16, T01 등 240 값과 "GDD-beta B12.1" 주석, 되감기 계열 PlayMode 테스트 수정 |
| 5 | unity-verify 전체 통과 | PASS | OVERALL: PASS |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 141/141, PlayMode 129/129 (B30 테스트 3개 포함)
