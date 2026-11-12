# B13 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 방향키를 누를 때마다 가장 이른 판정 원이 반응(눌림 펄스) | PASS | JudgeSystem.Submit이 키 입력마다 InputReceived를 보내고, JudgeRingView.OnInput이 PulseAt을 기록하며, UpdateRing이 PulseScale을 적용한다. B13_EarlyPress_PulseAndTooEarlyLabel_NoJudgement가 0.06초에 배율 1.15, 0.12초 뒤 1.0을 검사한다. B13_EveryPress_ReportsOutcome은 입력 3번에 알림 3번과 PulseScale 값을 검사한다. |
| 2 | 목표 −300 ms보다 일찍 눌러 무시된 입력은 원 아래에 "빠름" 표시, 판정·점수 변화 없음 | PASS | B13_EarlyPress_PulseAndTooEarlyLabel_NoJudgement가 −500 ms 입력에서 다음을 확인한다: TooEarly 처리, 판정 0건, 원 유지, 문구 "빠름", 위치는 원 y−110, 0.4초 뒤 사라짐. 그다음 제때 누르면 정상적으로 PERFECT가 나온다. 점수를 ScoreBoard로 직접 확인하지는 않았지만, 판정 이벤트가 0건이라 점수가 바뀔 경로가 없다. |
| 3 | 원이 없을 때 누른 키는 화면 아래 가운데에 작은 키 표시 | PASS | B13_PressWithoutRing_KeyEchoBottomCenter가 문구 "←", 글자 크기 48, x=0, y=−540+120, 0.3초 뒤 사라짐을 검사한다. 아직 뜨지 않은 예약 원만 있을 때도 같은 표시가 나오는지 확인한다. |
| 4 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. 컴파일 OK, EditMode 115/115, PlayMode 89/89(B13 테스트 3개 포함). |

## 실패 항목과 수정 지시
- 없음.
- 참고(판정에 영향 없음): 판정 창이 이미 지난 원이 남아 있을 때 누르면 NoCue로 처리되어 누른 키 표시가 나온다. GDD-beta와 어긋나지 않는다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 115/115, PlayMode 89/89. 빌드·에셋 항목이 없어 smoke-test와 check-glb는 실행하지 않았다.
