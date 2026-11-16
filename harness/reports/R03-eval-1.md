# R03 평가 보고서 (1회차)
판정: PASS (단서 2개: 기존 T14 실패 1건, 드로우콜 +27%는 사용자가 받아들인 결정)
(evaluator 서브에이전트의 보고를 호출한 쪽이 저장함 — 평가자 규칙상 파일을 직접 쓰지 않음)

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | R01 배치 안대로 챕터별 직접 배치, 수치는 GameConfig.Map | PASS | R03_MapPlanTests가 MapPlan.Build 결과를 GameConfig.Map 수치와 비교, 통과. 미리보기에서 줄 맞춘 배열이 아님 확인 |
| 2 | 같은 모델 연달아 금지, 회전·크기 변화, 소품 밀도 | PASS | 연속 반복 0, 회전 ±4°, 크기 0.92~1.08, 샛골목, 구역별 10 m당 소품 ±1.0, 달리는 길 비움 테스트 통과 |
| 3 | 장터 거리 | PASS | 85~215 m에 좌판·차양·옹기·짚단·수레·천·등, 시전 5채 초과. ch1_b_market.png |
| 4 | 모퉁이 곡선, 카메라 순간이동 없음 | PASS | 길이 유지·연속·방향 변화 3° 미만·곡선 밖 위치 동일·모퉁이 곡선화 테스트 통과 (카메라는 경로 연속성으로 간접 인정) |
| 5 | 적 발밑 실제 지붕·담·땅 | PASS | R03_Plan_ForeshadowStandsOnPlannedRoofOrWall, R03_EnemiesAndForeshadow_StandOnVisibleRoofWallOrGround 통과 |
| 6 | 먼 배경 반복 줄이기 | PASS | 파노라마 6000 px 초과·둘레 한 번 테스트 통과 |
| 7 | 챕터 미리보기 PNG, 사용자에게 보여 줌 | PASS | harness/reports/R03/*.png 9장 + all_chapters.png |
| 8 | 드로우콜 크게 늘지 않음 | PASS (단서) | 290 → 369 (+27%). 사용자 승인(2026-11-16), T15 기준 +30% |
| 9 | unity-verify 전체 통과 | PASS (단서) | EditMode 200/200, PlayMode 165/166. 실패 T14_Enemy_Model_WeaponInHand…는 R02 때부터의 기존 실패, R05 담당 |

## 남은 문제
- R05에서 T14 실패를 고치고 전체 통과 확인.
- 미리보기의 단색 문틀·기둥·회색 판(ch2_b, ch5_b, ch6)은 R04(사건 소품 회색 상자) 범위. ch2_a 오른쪽 돌담이 카메라에 가깝게 크게 보임.
- 드로우콜 +27%는 이후 최적화 작업에서 줄일 여지.
- 발 IK는 별도 작업 후보 (사용자 결정).
