# B02 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | B2.1 사건 단위: 종류별 고정 길이(4·6박), 판정 1~3개(간격 0.5박 이상), 결과와 무관하게 끝 위치가 같음 | PASS | B02_NewEvents_MatchGddBetaB22는 8종의 박자·판정 오프셋·키를 GDD-beta 표와 대조하고 간격 0.5박 이상을 검사한다. B02_TileSlip_TwoJudges_Fixed6Beats_AnyResult는 PERFECT/NORMAL/FAIL 세 결과 모두 6박이고 끝 위치가 14임을 확인한다. 화살 세례는 판정 4개다. B2.1의 "1~3개"와는 어긋나지만 B2.2 표에 명시된 값이라 표를 따랐고, 테스트도 표를 기준으로 한다. |
| 2 | FAIL마다 체력 −1·속도 ×0.8, 체력 최대 1 제한 사건(rolling_jars, arrow_volley) | PASS | B02_Fail_EachCostsSpeed_ButCappedHpForJarsAndArrows는 술독 FAIL 3번 뒤 체력 2, 화살 FAIL 4번 뒤 체력 1을 확인한다. 기와 미끄러짐은 FAIL 2번에 체력 1이다. B02_HpCapOne_And_SlowMoCandidates_MatchGddBeta는 체력 제한 대상이 정확히 두 종류임을 확인한다. |
| 3 | 기존 장애물이 4박·판정 1개 사건으로 동작 (T05 통과) | PASS | B02_LegacyObstacles_4Beats_OneJudgeAtStart와 T05 테스트 전부 통과. |
| 4 | charge/ambush/chase는 이동을 멈추지 않고, FAIL이면 체력 −1 후 다시 하지 않고 지나감 | PASS | B02_ChargeAmbushChase_DoNotStop은 프레임마다 달리기 박자 증가량이 시계 증가량과 같고, Combat 상태로 바뀌지 않으며, 판정이 4번 일어남을 확인한다. B02_ChargeFail_HpMinusOne_ThenPasses는 무입력이면 체력 2, 판정 1개, 다시 예약하지 않음을 확인한다. |
| 5 | 달리는 중 슬로모션(0.3배, 0.4초, 앞뒤 0.08초)을 이동 시간 재배치로 처리, 끝 박자 오차 0.05 m 이하, 박자 시계 그대로 | PASS | B02_TimeRemap_SlowsThenCatchesUp_ExactAtEnd는 0.3배, 0.4초 = 0.8박, 0.08초 경사, 사건 끝 지연 0, 위치가 뒤로 가거나 끊기지 않음을 확인한다. B02_RunningSlowMotion_RemapsVisual_ExactAtEventEnd는 시간을 주입해 timeScale 0.3, 시계 증가량 불변, 끝 위치 오차 0.05 m 이하를 확인한다. |
| 6 | 대치·포위·무사 전투는 기존 테스트 그대로 통과 | PASS | T06, T07 테스트 전부 통과. |
| 7 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음. 참고: chase는 판정 1개짜리 4박 사건으로 구현되어 있다. GDD-beta의 "2~3개를 4박 사건으로 나눠 배치"는 차트에서 사건 여러 개로 배치하는 방식이라 허용 범위다. B03/B04에서 확인하면 된다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 83/83, PlayMode 50/50 (B02 EditMode 6개, PlayMode 5개 포함)
- 에셋·빌드 작업이 아니라서 check-glb와 smoke-test는 실행하지 않았다.
