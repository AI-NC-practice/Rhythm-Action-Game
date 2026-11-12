# T03 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 판정 경계값: ±60 ms PERFECT, ±150 ms NORMAL, 다른 키/−300~−150 ms/+150 ms까지 무입력은 FAIL, −300 ms보다 이른 입력은 무시 | PASS | JudgeRules.cs:22-34를 GDD 4.2와 대조해 일치함을 확인. EditMode 테스트 T03_WithinPerfectWindow, T03_WithinNormalWindow(±60.1, ±149.9), T03_EarlyBetween150And300_IsFail, T03_EarlierThan300_IsIgnored(−300.1), T03_WrongKey_IsFail, T03_NoInputUntilPlus150_IsMissed, T03_JudgeSystem_NoInput_FailsAfter150ms가 통과함. |
| 2 | 타이밍 보정이 목표 시점에 더해짐 | PASS | JudgeRules.DeltaMs가 `targetTime + offsetMs/1000`을 쓰고, T03_TimingOffset_IsAddedToTarget과 T03_SettingsOffset_ClampedAndSnappedTo5ms가 통과함. |
| 3 | Input System Keyboard.current 방향키 4개 사용, 구 Input Manager 없음 | PASS | JudgeSystem.cs:77-83에서 Keyboard.current의 up/down/left/rightArrowKey를 사용함. Assets/Scripts를 grep했을 때 `Input.Get`이나 `UnityEngine.Input` 사용이 없음. T03_NoLegacyInputManager_InScripts 통과. |
| 4 | 판정 원 UI: 2박자 전 등장, 바깥 원 360→120 px 선형 축소, 가운데 화살표, 문구 0.6초 표시 | PASS | PlayMode T03_Ring_AppearsTwoBeatsBefore_AndShrinks가 0.99초에는 없고 1.0초에 등장함을 확인함. 지름은 360, 240, 180, 120으로 선형이고, 목표 원은 120이며 화살표는 "↑"임. T03_Label_ShownFor0_6Seconds는 0.59초에 보이고 0.61초에 사라짐을 확인함. T03_Labels_NormalAndFail이 문구 NORMAL과 FAIL을, T03_Label_ShownFor0_6Seconds가 PERFECT를 확인함. T03_Font_HasJudgeGlyphs 통과. |
| 5 | URP Volume 채도 −60%, FOV 70→58 전환(0.15초), 판정 후 0.2초 복귀 | PASS | T03_Fx_GrayAndZoom_EnterAndExit가 시작값 0과 70, 진입 중간값 −30과 64, 0.15초 뒤 −60과 58, 최대 회전 25°를 확인함. 복귀는 0.1초에 −30과 64, 0.2초에 0과 70이고 회전도 0으로 돌아옴. |
| 6 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 45/45, PlayMode 5/5.
- 참고: 이번 작업 범위에서 GLB 에셋과 빌드는 없어 check-glb와 smoke-test는 실행하지 않았음.
- 참고: GDD 4.2에는 판정 원이 화면에서 장애물 위치에 투영되어 뜬다는 항목이 있는데, 이는 T03 acceptance 범위 밖이라 판정에서 제외했음.
