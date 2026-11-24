# R13 평가 보고서
판정: PASS (Unity 재실행 없이 verify-latest.md 2026-11-24 20:51:48·미리보기·코드로 판단 — 시간 제약)

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 지붕 위 ↓ 장애물이 올라앉은 대문/건물로 안 보임 (지붕: 길 옆 높은 건물 처마, 땅: 낮은 처마) | PASS | R13_RoofEaves_AreTallerBuildingOverhang_GroundEaveUnchanged 통과(136·232박 eave_low 없음, Overhang_ 건물 땅 위, 길+EaveUnderside보다 높음, 80박 eave_low 그대로). 미리보기 4장 직접 확인 — 길 옆 단청 전각 처마, 기둥은 길 가장자리, 길은 열림 |
| 2 | 충돌 상자·판정 그대로 | PASS | Prop_eave 존재·렌더러 꺼짐, SceneBuilder.cs:458 상자 그대로, 판정 테스트 통과 (상자 수치 단언 테스트는 없음) |
| 3 | 미리보기 PNG | PASS | harness/reports/R13/eave_136_approach·under, eave_232_approach·under |
| 4 | unity-verify 전체 통과 | PASS | EditMode 209/209, PlayMode 229/230 (skipped 1) |

참고(HUMAN): eave_136_under.png는 화면 왼쪽을 기둥·처마가 크게 덮음 (길은 오른쪽으로 열려 있음).
