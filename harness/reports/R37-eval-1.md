# R37 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 어두워지거나 처음으로 돌아가는 연출 없음, 계속 달림, 뒤에서 따라가기 | PASS | R37_Wrap_SameFrameBeforeAndAfter_NoFade: AttractFade 없음, 600프레임 동안 거리가 되풀이 구간 안·안개 그대로·Wraps>1. wrap_before.png 3인칭 |
| 2 | 되풀이 전후 같은 장면 (같은 프레임 비교), 실제 맵이 보이지 않음 | PASS | 4자리에서 평균 차이 <0.004, 큰 차이 픽셀 <0.002, 대조(반 칸) >0.02. wrap_before/after 육안 동일. 실제 맵은 안개(주석·안개 거리 단언)로 가림 — 직접 측정 테스트는 없음 |
| 3 | 시작 → 시작선까지 달려 1인칭 1챕터, 평소 안개·배경 | PASS | R37_Start_MistLifts_Chapter1HasNormalFogAndBackdrop: 안개 걷히기만, 1챕터 시작 시 FogStart/FogEnd·_Mist 0·배경 제자리. start_fp.png 목 단면·머리카락 없음 |
| 4 | PlayMode 테스트·미리보기 PNG | PASS | harness/reports/R37/ 5장, 테스트 2개 통과 |
| 5 | unity-verify 전체 통과 | PASS | EditMode 209/209, PlayMode 229/230 (skipped 1) |
