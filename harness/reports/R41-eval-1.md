# R41 평가 보고서
판정: PASS (Unity 재실행 없이 verify-latest.md 20:51·PlayMode 로그·측정표·미리보기로 판단 — 시간 제약. 20:50 이후 Assets 코드 변경 없음)

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 아래로 피하기 PERFECT·NORMAL 카메라 ≤ 밑면 − 0.3 m (측정) | PASS | R41_Perfect/Normal_CameraClearsUnderside 통과 — 7개 사건(빨랫줄 12·96, 처마 80·136·232, 닫히는 대문 202, 깃발 줄 252) r.clear ≥ SlideClearance 0.3. 252박은 길 위에 막는 것이 없음을 확인. 여유 0.41~0.68 m |
| 2 | 충돌 상자 판정·R06 화면 가림 그대로 | PASS | R06 PlayMode 전부 통과, 판정 EditMode 통과 (상자 수치 직접 단언은 없음) |
| 3 | PlayMode 측정 테스트·미리보기 PNG | PASS | harness/reports/R41/ PNG 13장. closing_gate_202_Normal·eave_136_Perfect 확인, clothesline_12_Perfect는 빨랫줄이 화면에 잘 안 보임(수치 측정으로 충족) |
| 4 | unity-verify 전체 통과 | PASS | EditMode 209/209, PlayMode 229/230 (skipped 1) |

참고: clearance.md는 다른 실행이 덮어써 일부 행만 남음 (테스트가 실행마다 새로 씀).
