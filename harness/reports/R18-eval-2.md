# R18 평가 보고서 (2회차)
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 추격 적 옆 8~20 m, 2박 전 뛰어들기, 판정 순간 공격 자리 | PASS | R18_ChaseEnemies_FarRoofLine_LeapIn_FallBehind: 6명 이상 매 프레임, 먼 줄 8~20 m(위반 1/50 이하), 공격 순간 4.5 m 이내, 뛰어듦 10프레임 초과 |
| 2 | 피한 추격 적 뒤처짐 | PASS | 공격 4~6박 뒤 5 m 안 프레임 0 |
| 3 | 돌진 적 대기→걷기→달리기, 판정 시각 공격 자리 | PASS | EditMode·PlayMode 속도 곡선 단언, 판정 순간 길 방향 오차 0.5 m 미만 단언 (1회차 지적 해소) |
| 4 | 판정 박·키·수 그대로 | PASS | R18_Chart_Unchanged |
| 5 | 뜨지 않음·건물 뚫지 않음 | PASS | 뛰어들기 clipFrames 단언, R07 통과 |
| 6 | 1인칭·3인칭 미리보기 | PASS | harness/reports/R18/ 40장, 5장 직접 확인 — 적 식별됨 (1회차 지적 해소) |
| 7 | unity-verify 전체 통과 | PASS | EditMode 204/204, PlayMode 180/180 |

참고: untracked Assets/InitTestScene*.unity는 커밋하지 말 것.
