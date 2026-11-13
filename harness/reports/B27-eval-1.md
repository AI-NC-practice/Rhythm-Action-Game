# B27 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | com.unity.cinemachine 패키지 추가, 컴파일 OK | PASS | `Packages/manifest.json:5`에 3.1.7이 있다. unity-verify를 직접 다시 실행했고 컴파일과 전체 테스트가 통과했다. |
| 2 | 결투 동안에만 Brain 활성, 결투 밖에서는 기존 카메라 구조 그대로 | PASS | `B27_Brain_OnlyDuringDuel` 통과. 일반 적 전투에서는 Brain 꺼짐·`CameraOverride` false, 무사 결투 중 켜짐, 결투가 끝나면 Brain 꺼지고 카메라 로컬 위치 0. `DuelCamera.cs`의 `LateUpdate`/`Deactivate`가 구현. |
| 3 | 막기 카메라(−0.1 m, FOV 62°)와 공격 카메라(+0.4 m, FOV 54°, 롤 6°)가 다음 판정 종류에 따라 바뀌고 0.3초 블렌드 | PASS | `B27_Config_MatchGddBetaB115`, `B27_GuardAndStrikeCams_SwitchByNextStep_Blend03` 통과: EaseInOut 0.3초, 두 카메라 위치·FOV, 롤 약 6°와 방향, S10이면 공격 카메라·A13이면 막기 카메라. |
| 4 | 칼이 부딪칠 때 FOV −6° 줌 펀치 0.15초, 기존 연출 층(흔들림 등)이 결투 중에도 적용 | PASS | `B27_ClashPunch_And_FxLayerOnTop` 통과: 부딪치면 −6, 0.16초 뒤 0, FAIL은 펀치 없음, fxOffset 흔들림과 연출 층 FOV가 가상 카메라에 얹힘. |
| 5 | unity-verify.ps1 전체 통과 | PASS | 아래 참고. |

참고: GDD의 막기 카메라 "무사 칼끝을 바라봄"은 B27 acceptance에 없고 테스트도 따로 검사하지 않는다. 구현은 판정 회전(`judgeFx.Turn`)을 따라간다. B28에서 확인하면 된다.

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 138/138, PlayMode 124/124, OVERALL: PASS (B27 테스트 3개 포함)
