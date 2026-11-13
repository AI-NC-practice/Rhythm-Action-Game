# B22 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 2박 미만 간격 원은 가로 160 px로 나란히 (1박 간격 포함) | PASS | GameConfig.RingChainMaxGapBeats=2. B22_OneBeatCombo_SideBySide_NoOverlap가 1박 간격 원 3개가 한 묶음인지, x 간격이 160인지, 모든 쌍의 거리가 140 이상인지 검사한다. 술독과 포위 1단계 모양은 GameScene 테스트가 챕터 시작 위치(0, 181, 256박)에서 달리며 간접 확인한다. |
| 2 | 묶음 아닌 새 원이 떠 있는 원과 140 px 안이면 옆으로 이동 (안전 타원 안, 오른쪽 먼저) | PASS | JudgeRingView.AvoidOverlap 구현. B22_NonChainNewRing_ShiftsAwayFromVisibleRing가 거리 140 이상, 가로 160, 오른쪽 방향, 타원 안을 검사한다. |
| 3 | 가장 이른 원 불투명도 1.0, 나머지 0.55 | PASS | B22_NextRingOpaque_WaitingDimmed가 판정 뒤 다음 원이 1.0으로 바뀌는 것까지 확인한다. |
| 4 | 실제 Game 씬 차트로 달릴 때 동시에 뜬 두 원의 거리가 항상 140 px 이상 | PASS | B22_GameScene_StageChart_NeverOverlap이 세 구간에서 각각 16초씩 진행하며, 원이 2개 이상 동시에 뜨는 순간이 있는지와 최소 거리가 140 이상인지 확인한다. |
| 5 | unity-verify 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. |

참고(판정에 영향 없음): 4번 테스트는 위치가 고정된(Locked) 원만 비교한다. 시작 박 0/181/256이 술독·포위 1단계 구간에 해당하는지는 챕터 구성으로 직접 확인하지 않았다.

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 130/130, PlayMode 107/107 (B22 테스트 4개 모두 통과)
