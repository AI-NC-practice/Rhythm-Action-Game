# B05 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | PresentationDirector, CameraFx, TimeFx 구조. 게임 코드는 Play(event) 호출만 | PASS | Assets/Scripts/Presentation/ 아래에 세 클래스가 있다. StageDirector는 RequestSlowMotion만 호출한다(StageDirector.cs:224, 235). B05_GameScene_EventSlowMotion_RespectsLimit가 SceneBuilder 연결과 FxOffset 층을 확인한다. 아직 연결하지 않은 호출부는 B07 범위다. |
| 2 | 카메라 층 합 규칙(롤 ±10°, 고개 ±35°, FOV 45~85°, 흔들림 합 ≤1·초당 1.5 감소, 회전 주인 우선순위) | PASS | B05_Layers_ClampedPerSection4, B05_YawOwner_Priority, B05_Rules_MatchProposalSection4가 해당 수치와 우선순위를 직접 단언한다. |
| 3 | Hit Stop은 대상만 멈추고 timeScale·박자 시계 불변, 겹치면 긴 쪽 하나 | PASS | B05_HitStop_LocalOnly_LongerWins_IgnoredDuringSlowMo가 timeScale 1 유지, 박자 시계 진행, HitStopUntil이 늦게 끝나는 쪽으로 정해지는지 확인한다. EnemyActor.SetFrozen으로 무기 자세가 멈추는 것도 확인한다. |
| 4 | 슬로모션 16박 빈도 제한 | PASS | B05_SlowMotion_FrequencyLimit16Beats가 7.9박과 15.9박에서 거절, 16.01박 뒤 수락을 확인한다. B05_GameScene_EventSlowMotion_RespectsLimit는 실제 게임 씬에서 2번 수락하고 1번 거절하는 것을 확인한다. |
| 5 | Hit Stop·슬로모션 중 판정 정상(PERFECT ±60 ms) | PASS | B05_JudgeStaysAccurate_DuringHitStopAndSlowMo가 시간을 주입해 +55 ms와 +59 ms는 PERFECT, +65 ms는 NORMAL임을 확인한다. |
| 6 | 연출이 끝나고 0.5초 안에 원래 카메라로 복귀 | PASS | B05_AllPresets_RecoverWithin05s가 모든 프리셋을 재생한 뒤 층이 0이고 위치·회전·FOV가 원래대로인지 확인한다. B05_AllPresets_ReleaseWithin05s_AndEnvelope도 같은 내용을 수치로 확인한다. |
| 7 | 연출 끄기 스위치 + 개발자 모드 F1 토글 | PASS | B05_Disable_ClearsAndBlocks_F1Toggle이 끄면 층이 지워지고 재생이 막히는지 확인한다. DevMode.ToggleFx가 F1에 연결되어 있다(DevMode.cs:81). 꺼도 Always 슬로모션은 유지되는데, GDD 규칙이고 테스트도 이를 확인한다. |
| 8 | unity-verify 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 105/105, PlayMode 63/63 (B05 테스트 EditMode 3개, PlayMode 8개 모두 통과)
- 에셋·빌드 작업이 아니라서 check-glb와 smoke-test는 실행하지 않았다.
