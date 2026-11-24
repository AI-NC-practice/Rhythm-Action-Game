# R40 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 벽타기 PERFECT·NORMAL 카메라 높이: 올라감 → 벽 구간 유지 → 내려옴 (측정) | PASS | Traversal.WallRunProfile로 포물선 교체. R40_Profile_RisesHoldsFalls 통과. R40_Stage_WallRuns_Perfect_Plateau(32·60·206·216박)·R40_Stage_WallRun_Normal_Plateau(60박): 진행 0.25~0.75 연출 높이 변화 <0.02 m, 화면 카메라 높이 변화 <0.25 m 단언 통과 |
| 2 | 벽을 밟는 자세(R06 45°/35°)·화면 가림 기준 유지 | PASS | 벽 구간 내내 기울기 ≥ 45−3 / 35−3 단언 통과, R06 테스트 전부 통과 |
| 3 | PlayMode 테스트·미리보기 PNG(60박 1인칭) | PASS | 테스트 3개 통과, harness/reports/R40/ 15장 (wallrun_60_Perfect_1·Normal_1 직접 확인) |
| 4 | unity-verify 전체 통과 | PASS | 직접 재실행 21:01:54 — EditMode 209/209, PlayMode 229/230 (skipped 1) |

참고: 테스트 부산물 Assets/InitTestScene*.unity가 앞선 커밋에 들어갔다가 Unity가 지움 — 이 커밋에서 삭제 기록.
