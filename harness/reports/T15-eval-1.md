# T15 평가 보고서
판정: FAIL

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 회색 상자 → VARCO 모델 (충돌 유지) | PASS | T15_Map_ModelsReplaceGrayBoxes_Atmosphere 통과, t15_ch1_24.png·t15_ch3_140.png |
| 2 | 챕터 컨셉 배치 | PASS | 6구간 Dress_n_ 장식, 미리보기 |
| 3 | 외곽 착시 | PASS | Backdrop 원통, t15_overview.png |
| 4 | 로우폴리 + 노멀 맵 | PASS | T11R 굽기, T13 노멀 맵 검사 |
| 5 | 드로우콜 전후 측정·기록 | PASS | T15_DrawCalls 통과, drawcalls.md 평균 778→292, SetPass 675→269 |
| 6 | 해 질 녘 조명·하늘·안개 | PASS | 안개·skybox·해 각도·색 검사 (색감은 HUMAN) |
| 7 | BuildFinal exe | PASS | Builds/Final/RhythmActionGame.exe |
| 8 | smoke-test | PASS | 12초 깨끗한 로그 |
| 9 | unity-verify 전체 통과 | FAIL | PlayMode 162/163 (2회 재현) |

## 실패 항목과 수정 지시
- B28_DuelCutTests.B28_Cuts_ABCD_Timing_Blend_Body_NoRings(True): "화면 = 3인칭 카메라 Expected: less than 0.02 But was: 0.0230 / 0.0242" — 원인을 찾아 고칠 것. 근거 없이 허용치만 완화하지 말 것. 고친 뒤 PlayMode 163/163 확인.
