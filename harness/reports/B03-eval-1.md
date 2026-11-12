# B03 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 8종이 표의 박자와 판정 목표 박자대로 동작 | PASS | PlayMode `B03_AllNewEvents_BeatsAndTargetsPerTable_PerfectRun`가 8종을 모두 실행한다. 종류마다 판정 수, TargetBeat가 `e.runBeat + Slots[i].Offset`과 같은지(오차 0.02), 사건 길이(`def.Beats`), 전부 PERFECT인지를 단언한다. 기대값은 EventCatalog에서 읽는다. 코드 값을 GDD-beta B2.2 표와 직접 대조하는 단언은 이 테스트에는 없다. |
| 2 | PERFECT/NORMAL/FAIL이 서로 다른 카메라 동작과 상태 이름으로 구분됨 | PASS | EditMode `B03_EachKind_PerfectNormalFail_DifferentMotion`이 8종과 적 3종, 총 11종에서 세 결과의 동작 곡선이 서로 0.1 넘게 다르고 사건 끝에서 0으로 돌아오는지 검사한다. PlayMode `B03_RopeSwing_ResultsGiveDifferentCameraStates`는 상태 이름 `rope_swing_Perfect/Normal/Fail`과 카메라 위치 차이를 검사한다. 8종 전부의 PERFECT 상태 이름은 1번의 PlayMode 테스트가 확인한다. |
| 3 | 회색 상자 소품을 SceneBuilder가 코드로 생성 | PASS | `SceneBuilder.cs`의 `BuildEventProps`가 `EventProps.Build`를 호출한다. EditMode `B03_EventProps_BuildGrayBoxParts`가 밧줄, 차양, 술독, 대문 문짝, 화살, 깃발 줄 등 소품의 하위 오브젝트 이름을 단언한다. PlayMode `B03_Props_SpawnedAndAnimated`는 소품 생성과 움직임(독이 굴러옴, 문이 닫힘)을 확인한다. |
| 4 | charge, ambush, chase, 조합, 복선이 회색 상자로 동작 | PASS | PlayMode `B03_EnemyApproaches_GrayBox`가 돌진(앞에서 달려오고 PERFECT면 쓰러짐), 매복(옆에서 튀어나옴), 추격(옆 지붕에서 나란히 달림), 멈추지 않음을 단언한다. `B03_Props_SpawnedAndAnimated`는 조합(rope_swing + charge)에서 밧줄 넘기 PERFECT면 포졸이 쓰러지는 것과 복선이 보였다가 숨는 것을 단언한다. |
| 5 | 슬로모션 후보가 PERFECT일 때 요청을 냄 | PASS | PlayMode `B03_SlowMotionRequests_OnlyCandidatesOnPerfect`가 요청된 종류가 tile_slip, rope_swing, closing_gate, long_jump, charge와 정확히 같은지(AreEquivalent) 확인한다. GDD-beta B4 2·3번 후보 중 이번 작업 범위와 맞는다. NORMAL이면 요청이 0건인 것도 확인한다. |
| 6 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음.

참고: 1번 테스트는 기대값을 EventCatalog에서 가져오므로, 카탈로그 값이 GDD-beta 표와 같은지는 이 테스트가 직접 증명하지 않는다. 카탈로그 값을 GDD-beta 표와 대조하는 단언은 B04 이후에 보강하면 좋다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 96/96, PlayMode 55/55 (B03 테스트 EditMode 13건, PlayMode 5건 모두 통과)
