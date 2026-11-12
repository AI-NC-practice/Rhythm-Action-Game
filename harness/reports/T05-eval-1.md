# T05 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 장애물 6종이 GDD 3장의 키로 판정됨 | PASS | `T05_SixCategories_AndChartKeysFit`는 enum이 6종인지, 차트의 모든 장애물이 `KeyFits`를 만족하는지, 차트에 6종이 모두 나오는지 확인한다. `T05_KeysMatchGdd3`는 ↑/↓/←→ 매핑과 오답 키 거부를 확인한다. `T05_CuePoint_IsAtObstacle`는 판정 원이 장애물 위치에 뜨는지 확인한다. 키 매핑은 `Traversal.cs:37-45`에 있다. |
| 2 | 판정 결과와 관계없이 4박자가 걸림 (PlayMode, 세 결과 모두) | PASS | `T05_AllResults_Take4Beats_WithDifferentMotion`가 수레 ↑에서 PERFECT·NORMAL·무입력 FAIL을 각각 실행해 통과 상태가 유지된 박자 길이가 4.0±0.1인지 확인한다. 이 PlayMode 테스트는 수레(Vault) 한 종류만 실제로 돌린다. 나머지 종류도 `StageDirector.UpdateTraversal`의 같은 코드(`ObstacleBeats`)를 지나므로 수용한다. |
| 3 | 결과별 이동 연출 3가지가 서로 다르고 이름 있는 상태로 구분됨 | PASS | `T05_ThreeResults_HaveDifferentMotion`가 6종 × 3결과 모두에서 궤적 차이가 0.2를 넘는지 확인한다. 같은 테스트가 `StateName`이 "Vault_Perfect" 형식인지 확인한다. PlayMode 테스트는 실제 상태 이름 "Vault_Perfect/Normal/Fail"과 중간 시점 카메라 오프셋이 서로 다른지 확인한다. |
| 4 | FAIL 시 체력 −1, 속도 ×0.8(최소 1.00), 음악 정지·되감기 없음 (PlayMode) | PASS | `T05_Fail_LosesHpAndSpeed_MusicKeepsGoing`: HP 2, 속도 1.30×0.8=1.04, 시계가 계속 돌고, 박자와 위치가 되감기지 않는다. `T05_Fail_SpeedNotBelowMin`: 최소 1.00이 유지된다. |
| 5 | 체력 0이면 게임 오버 (PlayMode) | PASS | `T05_ThreeFails_GameOver`: HP 0, `StageState.GameOver`, 플레이어가 멈추고 이후 거리가 변하지 않는다. `T05_Health_ThreeFailsKill`(EditMode)도 통과했다. |
| 6 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음.
- 참고(비차단): 4박자 PlayMode 테스트가 Vault 한 종류에만 걸려 있다. 이후 작업에서 다른 종류의 4박자 검증을 추가하면 좋다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 60/60, PlayMode 13/13
- 에셋/빌드 작업이 아니므로 check-glb.ps1과 smoke-test-build.ps1은 실행하지 않았다.
