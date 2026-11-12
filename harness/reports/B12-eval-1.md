# B12 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 박자 시계가 dspTime을 실제 시간으로 보간해 매 프레임 부드럽게 진행하고, 뒤로 가지 않으며, dsp와의 차이가 DSP 버퍼 한 개 이하 (EditMode, dsp·실제 시간 주입) | PASS | `Assets/Scripts/Core/SmoothClock.cs`는 추정값을 [dsp, dsp+버퍼] 범위로 자르고 직전 값보다 작아지지 않게 한다. `B12_SmoothClockTests.B12_Smooth_AdvancesEveryFrame_WithinOneBuffer_NeverBackwards`가 5 ms 프레임 400번 동안 뒤로 가지 않음, dsp 이상, 차이 ≤ 버퍼, 95% 이상의 프레임에서 값이 늘어남을 검사한다. `B12_Smooth_StallCapped_DspBackwardsResets`도 통과했다. `BeatClock.TimeSource` 기본값은 `Smooth.Now()`이고, 버퍼 길이는 `AudioSettings.GetDSPBufferSize`로 구한다. |
| 2 | 키 입력은 Input System 이벤트 시각을 박자 시계 시각으로 바꿔 판정 (프레임보다 앞선 입력 시각으로 PERFECT) | PASS | `JudgeSystem.ReadKeyboard`가 `kb.lastUpdateTime`을 `SubmitRealtime`에 넘긴다. 이 함수는 `RealtimeToClock`으로 시각을 바꾸고, 미래 시각과 0.1초보다 오래된 시각은 잘라 쓴다. `B12_InputTimingTests.B12_EventTime_UsedInsteadOfFrameTime`: 프레임 시각은 +70 ms(NORMAL 구간)지만 이벤트 시각 +20 ms로 PERFECT 판정. 미래·과거 자르기 경우도 검사했고 통과했다. |
| 3 | 화면 표시 지연 보정 기본값이 GameConfig에 있고 판정 창(±60/±150 ms)은 그대로 | PASS | `GameConfig.DisplayLatencyMs = 16`. `JudgeRingView`가 `lead`만큼 원을 앞당겨 그린다. `B12_Config_JudgeWindowsUnchanged_LatencyDefaults`가 60/150/16을 확인한다. `B12_Ring_DrawnAheadByDisplayLatency`가 보정값만큼 일찍 두 원이 겹치는지와 +55 ms PERFECT를 확인한다. |
| 4 | 음악·반주 PlayScheduled는 실제 dspTime 기준 그대로, 기존 B08 테스트 통과 | PASS | Metronome과 JangguBacking의 예약 코드는 바뀌지 않았다(git diff에 없음). 시계 값도 dsp 시각을 보간한 것이라 기준이 같다. 기존 B08 테스트가 모두 통과했다. |
| 5 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. |

참고(판정에 영향 없음): 보간 시계는 실제 dsp보다 최대 버퍼 한 개(약 21 ms) 앞설 수 있어서, PlayScheduled 예약 시각도 그만큼 앞선 값이 될 수 있다. 다만 시계 자체가 dsp 추정치라 기준은 일관된다.

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 115/115, PlayMode 86/86 (OVERALL: PASS)
- 에셋·빌드 작업이 아니라서 check-glb와 smoke-test는 실행하지 않았다.
