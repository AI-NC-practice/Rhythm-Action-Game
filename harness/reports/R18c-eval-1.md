# R18c 평가 보고서 (1회차)
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 지붕 위 전투 적이 지붕을 넘어 착지하며 시작 (처음부터 서 있지 않음) | PASS | R18c_RoofFighters_OvertakeLeapLand: 128·152·240박, Run→Air→Land 순서, 나타나기 전 렌더러 꺼짐(0), 전투 자리 3 m 안 미리 보임 0, 멈추기 전 화면에 보임 30프레임 넘음 |
| 2 | 착지 동작은 승인된 Mixamo 클립 | PASS | running_jump·falling_idle·hard_landing, 세 상태 재생 확인, HANDOFF 8장 상시 승인 |
| 3 | 궤적이 건물을 뚫지 않고 착지 발이 보이는 면 위 | PASS | 공중 지붕 속 2% 이하(실측 0), 도착 오차 < 0.1 m, 발밑 0.35 m 이하 |
| 4 | 판정 박·키·수 그대로 | PASS | GameConfig는 Leap* 상수 추가만, 차트·판정 테스트 통과 |
| 5 | 1인칭·3인칭 미리보기 | PASS | harness/reports/R18c/ run·air·land fp/tp |
| 6 | unity-verify 전체 통과 | PASS | EditMode 204/204, PlayMode 183/184 (검토용 1 건너뜀) |

참고: 동작의 자연스러움은 정지 PNG로 판단할 수 없음 — 사람이 영상으로 확인 권장.
