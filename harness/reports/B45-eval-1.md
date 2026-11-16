# B45 평가 보고서
판정: PASS

기준은 두 가지다. 1번은 GDD-beta B17.2 각 항목을 테스트로 검증하는 것이고, 2번은 unity-verify 통과와 테스트 빌드 사용자 확인이다.

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1a | 박 펄스: 박마다 판정 원 테두리, HUD, 화면 가장자리가 커졌다 돌아옴. 마디 첫 박은 더 크게 | PASS | EditMode `B45_Pulse_PeaksOnBeat_BarStartBigger`는 박 순간 1, 0.35박 뒤 0, 마디 첫 박은 BarAccent(1.8) 배를 검사한다. PlayMode `B45_GameScene_PulseOnBeat_RingHudVignette_StepBob`은 8박에서 판정 원 `localScale`이 1+1.8×0.08, 하트와 속도 텍스트가 HudScale, 장구 레인 판정 표시가 LaneScale, 비네트가 0.12×1.8이다. 9박은 보통 크기이고 박 사이(10.5박)는 원래 크기다. 연출 끄기(F1)와 일시정지에서는 0이다. |
| 1b | 예고음: 판정 원이 떠 있는 동안 남은 박마다 작은 틱. 설정에서 끄기 | PASS | EditMode `B45_TickBeats_TwoAndOneBeatBefore_WithinApproach`는 목표 10박이면 8·9박, 반 박 판정이면 8.5·9.5박을 검사하고 ApproachBeats가 2인지도 확인한다. `B45_TickClip_ShortHigh_LastIsHigher`는 짧고 작은 틱이며 마지막 틱이 더 높은 음인지 본다. `B45_CueTicksSetting_DefaultOn_Saved`는 기본 켜짐과 저장을 본다. PlayMode `B45_CueTicks_TwoAndOneBeatBefore_StageOnly_NoDup_NoClash_SettingOff`는 같은 박 중복 없음, 장단 노드 제외, 다른 판정 박과 겹치는 틱 제외, 설정 끔일 때 무음을 본다. `B45_Title_CueTicksToggle_Saved`는 타이틀 설정 화면의 토글과 저장을 본다. |
| 1c | 발걸음·카메라를 박에 맞춤 | PASS | EditMode `B45_Step_OneBeatOneStep_LowestOnBeat_FeetAlternate`는 박 순간 가장 낮음, 반 박 가장 높음, 주기 1박, 왼발·오른발 교대, 멈춤 시 박마다 IdleDip을 검사한다. PlayMode 테스트는 카메라 계층(Motion→BeatBob→FxOffset→PlayerCamera)과 `fx.bob.localPosition`이 실제로 박에 맞는지 본다. |
| 2a | unity-verify 통과 | PASS | 직접 재실행했고 OVERALL: PASS다. 컴파일 OK, EditMode 170/170, PlayMode 147/147이며 B45 테스트 8개(EditMode 5, PlayMode 3)가 모두 통과했다. |
| 2b | 테스트 빌드로 사용자 확인 | HUMAN | 사용자가 직접 확인해야 하는 항목이라 판정에서 제외한다. 예고음의 크기와 거슬림 정도, 펄스와 걸음 출렁임의 체감은 사람이 봐야 한다. |

## 실패 항목과 수정 지시
- 없음.
- 참고: 수치(PulseBeats, BarAccent, RingScale 등)는 GDD에 값이 없고 `GameConfig.BeatFx`에 Claude가 정해 둔 것이다. 한 곳에만 있어 규칙 위반은 아니다. 사용자 확인 뒤 조정이 필요하면 GameConfig 값만 바꾸면 된다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 170/170, PlayMode 147/147
- 에셋·빌드 작업이 아니라서 `check-glb.ps1`과 `smoke-test-build.ps1`은 평가 단계에서 실행하지 않았다.
