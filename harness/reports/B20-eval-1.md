# B20 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 첫 사건 4박, 앞 2개 사건 간격 8박 (GDD-beta B3 보충) | PASS | `B20_FirstEventAt4_PracticeGap8`가 첫 사건 4박, 둘째 12박, 그 뒤 56박 전까지 사건이 쉬지 않고 이어지는지 검사한다. 직접 재실행해 통과했다. |
| 2 | 1구간 36~60박 사이 사건 간격 4박 이하, 전투 앞뒤 빈 4박 채움 | PASS | `B20_NoEmptyStretches_MaxGap4Beats`가 복선을 뺀 모든 이벤트의 "끝 → 다음 시작" 간격이 4박 이하인지, 36~60박 사이 사건이 4개 이상인지 검사한다. `B20_CombatSurroundedByEvents`가 모든 대치 바로 앞의 사건과 4박 안에 오는 뒤 사건을 검사한다. 간격 검사는 포위 전투(184박) 뒤를 뺀다. 포위 전투는 길이가 정해져 있지 않아 밝혀 둔 예외이고, 기준 문구가 이를 막지 않으므로 허용했다. |
| 3 | 전체 판정 85~95, 장애물·사건 판정 55~65 | PASS | `B20_Totals_And_KeptRules`가 `ChartCheck.TotalJudgements` 85~95, `RunTargets` 55~65를 검사하고 통과했다. |
| 4 | 지붕 높이·전투 위치·슬로모션 후보 뒤 2박 규칙·챕터 시작 2박 규칙 유지 | PASS | 전투 위치(92/128/152/240, 포위 184, 무사 260)와 높이가 바뀌는 사건(100, 112, 120, 164, 224, 248, 142)은 `B20_Totals_And_KeptRules`가 검사한다. 슬로모션 후보 뒤 2박 규칙과 챕터 시작 2박 규칙은 기존 B04_ChartTests 등이 확인하며, 이번 전체 실행에서 모두 통과했다. |
| 5 | SceneBuilder.BuildAll로 Game 씬 재생성 | PASS | `Assets/Scenes/Game.unity`가 바뀌었다(git M, 수정 17:51, verify 17:53보다 앞섬). 차트는 Resources JSON이라 씬 재생성과 별개로 반영된다. |
| 6 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 130/130, PlayMode 103/103 (OVERALL: PASS)
- B20 테스트 4개(`B20_FirstEventAt4_PracticeGap8`, `B20_NoEmptyStretches_MaxGap4Beats`, `B20_CombatSurroundedByEvents`, `B20_Totals_And_KeptRules`) 모두 통과.
