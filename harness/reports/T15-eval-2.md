# T15 평가 보고서 (2회차)
판정: FAIL

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 회색 상자 → VARCO 모델 (충돌 유지) | PASS(부분 근거) | T15_Map_ModelsReplaceGrayBoxes_Atmosphere 통과 |
| 2 | 챕터 컨셉 배치 | HUMAN | 시각 판단 |
| 3 | 외곽 착시 | HUMAN | 시각 판단 |
| 4 | 로우폴리 remesh + 노멀 맵 | 미확인 | 이번에 확인 안 함 — 재제출 때 근거 명시 |
| 5 | 드로우콜 전후 기록 | PASS | drawcalls.md 778 → 293, 테스트 통과 |
| 6 | 조명·하늘·안개 | PASS(부분)/HUMAN | 테스트 통과 |
| 7 | BuildFinal exe | PASS | Builds/Final/RhythmActionGame.exe |
| 8 | smoke-test | PASS | 12초 깨끗한 로그 |
| 9 | unity-verify | FAIL | PlayMode 162/163 |

## 실패 항목과 수정 지시
- B28_DuelCutTests.B28_Cuts_ABCD_Timing_Blend_Body_NoRings(True) (B28_DuelCutTests.cs:115): "화면 = 3인칭 카메라" 0.0241 > 0.02. (False)는 통과. CameraFx/BeatFx 고정 뒤에도 남음 — 원인을 찾아 고칠 것, 허용치 완화 금지.
- 기준 4 근거를 재제출 보고에 명시.
