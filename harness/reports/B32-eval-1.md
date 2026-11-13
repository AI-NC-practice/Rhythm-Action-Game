# B32 평가 보고서
판정: FAIL

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | B13.1 일반 적·무사: 판정별 이동, 0.35초, 싸움 거리, 결투 원 5 m | FAIL | 일반 적은 B32_StepMotionTests, B32_NormalEnemy_Charge_StepPerJudgement_Arena_Return로 PASS. 무사 결투의 이동·싸움 거리·결투 원을 검사하는 테스트가 없음(구현은 StageDirector.Motion.cs에 있음). |
| 2 | 틈에 20°/초 둘레 돌기, 회전 240°/초 이하 | PASS | B32_Gap_EnemyCircles20dps_FacingMax240dps |
| 3 | 전투 후 경로 위 자리·방향 복귀 | PASS | B32_NormalEnemy_..._Return |
| 4 | B13.2 5 m 돌진, 첫 원 0.5초 | PASS | 같은 테스트 + B23_FirstRing_Within05s_OfArrival |
| 5 | 판정·점수·되감기 그대로, 기존 테스트 수정·주석 | PASS | B23 주석, B28 허용 오차 사유 주석 |
| 6 | unity-verify 전체 통과 | PASS | EditMode 145/145, PlayMode 131/131 |

## 실패 항목과 수정 지시
- 무사 결투에서 MotionActive, 판정 뒤 싸움 거리 2.2 m 복귀, 결투 원 5 m 안, 판정에 따른 실제 이동을 검사하는 테스트 추가.
- 가능하면 창병 싸움 거리 2.6 m도 확인.
