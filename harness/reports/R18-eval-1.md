# R18 평가 보고서 (1회차)
판정: FAIL

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 추격 적 옆 8~20 m, 2박 전 뛰어들기, 판정 순간 공격 자리 | PASS | R18_ChaseEnemies_FarRoofLine_LeapIn_FallBehind 통과, 추격 적 6명 이상 실측 |
| 2 | 피한 추격 적 뒤처짐 | PASS | 공격 4~6박 뒤 5 m 안 프레임 0 단언, ChaseFallBehindRate 구현 일치 |
| 3 | 돌진 적 대기→걷기→달리기, 판정 시각 공격 자리 | 일부 FAIL | 속도 곡선은 EditMode·PlayMode 단언 있음. 판정 순간 실제 위치 단언 없음 |
| 4 | 판정 박·키·수 그대로 | PASS | R18_Chart_Unchanged (MD5) |
| 5 | 뜨지 않음·건물 뚫지 않음 | PASS | R07 전체 통과, 뛰어들기 clipFrames 검사 |
| 6 | 1인칭·3인칭 미리보기 | FAIL | 돌진 적 charge_72/220 미리보기에 적이 식별되지 않음, 036 far_fp에 적 없음 |
| 7 | unity-verify 전체 통과 | PASS | EditMode 204/204, PlayMode 180/180 |

## 수정 지시
- 기준 3: 돌진 적 판정 순간 위치가 판정 자리 근처(예 0.5 m)인지 프레임에서 직접 재는 단언 추가
- 기준 6: 돌진 적이 보이도록 캡처 시점·카메라 변경(걷기·질주 따로), 추격 적 먼 줄 달리는 모습도 적이 보이는 3인칭
- 참고: untracked Assets/InitTestScene*.unity는 커밋하지 말 것
