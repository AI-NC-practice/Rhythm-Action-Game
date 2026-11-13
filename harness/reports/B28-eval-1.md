# B28 평가 보고서
판정: FAIL

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 컷 A(120°), B(30°), C(넓게), D(180° 슬로모션)가 정해진 구간에 활성, 중간점을 보고 반지름 3.5~5 m | FAIL | A·C·D와 반지름·중간점은 `B28_DuelCutTests`(perfect=true/false), `B28_Config_Cuts_Radius35To5_Sweeps`에서 통과. 컷 B는 속도 1.3(전부 PERFECT)에서 켜지지 않음(`B28_DuelCutTests.cs` 132행 `Assert.IsFalse(seen.Contains("B"))`). 속도 1.0에서만 켜짐. |
| 2 | 판정 원이 나타나기 0.4초 전에는 1인칭, 3인칭 동안 판정 원 없음 | PASS | `B28_DuelCutTests` 82행, 125~129행, `B28_NoCutOverlapsAnyRing_AtAnySpeed`, `B28_ActiveCut_Windows_EndBeforeRingsWithMargin` 통과. |
| 3 | 1인칭 ↔ 3인칭 0.4초 블렌드(D 진입만 바로), 3인칭 동안 몸 표시 | PASS | `B28_DuelCutTests` 80~81행, 90~99행, 113~119행. |
| 4 | BuildGray 빌드와 smoke-test 통과 | PASS | 직접 실행, 빌드 OK, 12초 실행 로그 정상. |
| 5 | unity-verify 전체 통과 | PASS | OVERALL: PASS (EditMode 141/141, PlayMode 전체 통과). |

## 실패 항목과 수정 지시
- 컷 B 구간(24–26박, 2박)은 속도 1.3에서 약 0.77초다. 진입 블렌드 0.4초 + 복귀 여유 0.4초 = 0.8초가 들어가지 않아 B가 나오지 않는다. 속도 1.0에서도 약 0.2초뿐이다.
- 수정 방향: (1) B만 블렌드·여유를 줄이거나 (2) 편차를 사용자에게 보고하고 GDD-beta B11.5를 고친다. GDD 안의 두 요구가 충돌하므로 사용자에게 묻고 진행할 것을 권한다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 141/141, PlayMode 전체 통과
- BuildGray OK, smoke-test-build 통과
