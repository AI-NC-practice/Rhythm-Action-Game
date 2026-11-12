# B04 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 구간 6개 순서·이름 유지, 경로 길이 600~700 m, 구간 흐름이 제안서 0.1.3을 따름 | PASS | `B04_SectionsKept_PathLength600To700`은 구간 이름 6개의 순서, `TotalLength` 600~700, 이벤트별 구간 번호를 검사한다. `B04_Flow_FollowsProposal013`은 구간 1~6의 핵심 사건 구성, 새 사건 8종, 등장 방식 3종, 포위 전 복선을 검사한다. 이 테스트는 사건 순서는 검사하지 않는다. `stage1.json`을 직접 읽어 보니 순서도 대체로 맞다. 다만 5구간의 "매복 2명 연속"은 매복 1번에 돌진·추격이 섞인 형태라 제안서 예시와 조금 다르다. 제안서 0.1.3이 예시이므로 경미한 차이로 본다. |
| 2 | 전체 판정 수 65~75, 포위 5명·무사 1명 유지 | PASS | `B04_TotalJudgements_65To75_SurroundAndBossKept`가 전체 판정 65~75, 달리기 판정 35~50, 전투 판정 20~35, 포위 1회, 무사 1명을 검사한다. 포위 5명은 이 테스트에서는 `GameConfig` 상수로만 확인하고, 실제 5명 배치는 PlayMode `T07_Surround_Layout_FiveEnemiesRadius4`가 통과했다. |
| 3 | 1구간 앞 2개 사건 간격 8박, 사건 겹침 없음, 판정 간격 0.5박 이상 | PASS | `B04_FirstTwoEvents8BeatsApart_NoOverlap_JudgeGap05`가 앞 2개 사건 간격 8박, 사건 길이 기준 겹침 없음, 판정 간격 0.5박 이상을 검사한다. 간격 0박 구간이 실제로 있는지, 전투 위치에 사건이 걸치지 않는지도 확인한다. |
| 4 | 슬로모션 후보 PERFECT 구간(0.4초) 안에 다른 판정 목표 시점 없음 | PASS | `B04_NoJudgeInsideSlowMotionWindow`가 최대 속도 기준 1.04박 창 안에 판정이 없는지와 다음 판정까지 2박 이상인지 검사한다. 후보가 3개 이상인지도 확인한다. 전투 슬로모션 후보는 멈춘 상태라 대상에서 뺐고, 그 이유가 `ChartCheck.cs` 주석에 있다. |
| 5 | `SceneBuilder.BuildAll`로 Game 씬 재생성, 챕터 1~6 시작 지점이 새 구간과 맞음 | PASS | `B04_DevChapterStarts_MatchSections_FirstJudgeAfter2Beats`가 시작 박자 {0, 56, 108, 176, 196, 252}와 첫 판정까지 2박 이상인지 검사한다. PlayMode `T04_GameScene_HasSectionsLightAndPlaceholders`는 씬의 소품 수가 차트와 맞는지 검사한다. `Game.unity`가 재생성되었고 PlayMode 55개가 모두 통과했다. |
| 6 | unity-verify.ps1 전체 통과 | PASS | 아래 실행 결과 참고. |

참고 (판정에는 영향 없음)
- 기존 테스트 T04·T05·T06·T07의 기대값을 바꿨다. T04·T05는 GDD-beta B3이 "장애물 20" 기준을 대신하기 때문이며, 근거 주석이 달려 있고 그 범위는 B04 테스트가 맡는다. T06·T07은 새 사건을 건너뛰도록 시작 박자를 바꿨다. 기준을 약하게 만든 수정은 아니라고 판단했다.

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 102/102, PlayMode 55/55, OVERALL: PASS (직접 재실행). 에셋·빌드 작업이 아니라서 check-glb와 smoke-test-build는 실행하지 않았다.
