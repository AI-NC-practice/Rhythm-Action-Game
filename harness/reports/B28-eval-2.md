# B28 평가 보고서 (2회차)
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 컷 A(120°)·B(30°)·C(넓게)·D(180° 슬로모션)가 정해진 구간에 활성, 두 사람 중간점을 보고 반지름 3.5~5 m | PASS | `B28_DuelCutTests.B28_Cuts_ABCD_Timing_Blend_Body_NoRings(True/False)` 통과. 속도 1.3에서도 컷 B 확인. 반지름·중간점·회전각 검사, `B28_Config_Cuts_Radius35To5_Sweeps` 통과. 컷 값(GameConfig 327~333행)이 GDD-beta B11.1/B11.5와 일치(컷 B 24–28 사용자 결정 반영). |
| 2 | 판정 원이 나타나기 0.4초 전에는 1인칭, 3인칭 동안 판정 원 없음 | PASS | `B28_DuelCutTests`, `B28_NoCutOverlapsAnyRing_AtAnySpeed`, `B28_ActiveCut_Windows_EndBeforeRingsWithMargin` 통과. |
| 3 | 1인칭 ↔ 3인칭 0.4초 블렌드(D 진입만 바로), 3인칭 동안 몸 표시 | PASS | D 진입 IsBlending false, 나머지 0.4초, 복귀 블렌드와 몸 shadowCastingMode 검사. |
| 4 | BuildGray + smoke-test | PASS | 직접 실행, 정상. |
| 5 | unity-verify 전체 통과 | PASS | OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음. 1회차 실패(컷 B 2박)는 GDD-beta를 고쳐 컷 B를 24–28박으로 늘리면서 해소. 속도 1.0과 1.3 양쪽에서 확인.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 141/141, PlayMode 126/126
- BuildGray OK, smoke-test-build 통과
