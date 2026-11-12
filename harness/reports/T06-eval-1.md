# T06 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 적 등장 시 이동 정지, 전투 후 다음 마디 첫 박에 재개 | PASS | PlayMode `T06_StopsAtEnemy_AllPerfect_DisarmOnGround_ResumesNextBar`. 정지 위치 고정, `resume % 4 == 0`, 재개 직전은 Combat, 직후는 Running, 이후 이동 거리 증가를 확인한다. |
| 2 | 공격 2회(2박 간격) + 마무리 1회, 공격별 키 | PASS | EditMode `T06_NormalEnemy_TwoAttacksTwoBeatsApart_ThenFinisherUp`은 차트의 모든 적에 대해 간격 2박과 마무리 ↑를 확인한다. `T06_ChartAttackKeys_MatchGdd45`는 키 대응과 차트 전체 키 적합성을 확인한다. PlayMode 테스트도 ↓, ←, ↑와 →, ↑를 확인한다. |
| 3 | 방어/마무리 FAIL 시 HP −1, 속도 ×0.8, 다음 마디에 첫 공격부터 재시작 | PASS | `T06_DefenseFail_LosesHp_RetriesFromFirstAttackNextBar`는 HP 2, 속도 1.2에서 1.0(바닥 적용), 재시도 횟수 2, 첫 공격 키 ↓, 다음 마디 등장을 확인한다. `T06_FinisherFail_RetriesWholeEnemy`는 마무리 FAIL 시 전체 재시도를 확인한다. |
| 4 | 마무리 PERFECT는 지붕이면 발차기, 땅이면 칼 뺏기/찌르기 | PASS | EditMode `T06_Execution_ByPlaceAndResult`와 PlayMode의 땅 위 DisarmSlash, 지붕 위 KickOffRoof 테스트로 확인했다. |
| 5 | 처형 중 timeScale 0.3을 0.4초(실시간) 적용, BeatClock 영향 없음 | PASS | `T06_RoofPerfect_KickExecution_SlowMotionDoesNotAffectBeat`는 0.39초 시점에 0.3, 0.41초 시점에 1.0을 확인하고 박자 계산이 그대로임을 확인한다. GameConfig는 0.3, 0.4로 맞다. |
| 6 | 무기 TrailRenderer, 잔상, 카메라가 공격 지점 추적 | PASS | `T06_Attack_TrailAfterimages_CameraFollowsWeapon`은 TrailEmitting, 잔상 2개 이상, 상한 이하, 판정 원이 무기 끝에 붙는지, 카메라가 공격 지점을 향하는지를 확인한다. TrailRenderer 컴포넌트 자체를 직접 확인하는 단언은 없고 `TrailEmitting` 플래그로 대신한다. |
| 7 | unity-verify 전체 통과 | PASS | 직접 재실행했다. 결과는 OVERALL: PASS이다. |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 64/64, PlayMode 18/18.
- 이 작업에는 에셋과 빌드가 없어서 check-glb와 smoke-test는 실행하지 않았다.
