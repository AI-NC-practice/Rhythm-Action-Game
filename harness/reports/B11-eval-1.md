# B11 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 취소된 판정(일반 적 재도전, 윈드밀 재도전)의 판정 원이 화면에서 사라짐 | PASS | `B11_DuelFailRetry_CancelledRingsRemoved`와 `B11_WindmillFailRetry_CancelledRingsRemoved`가 통과했다. 두 테스트 모두 취소 직후 해당 판정의 원이 `view.Rings`에 없는지, 화면의 "Ring" 오브젝트 수가 목록의 원 수와 같은지 확인한다. 코드에서는 `JudgeSystem.Cancel`이 `CueCancelled`를 보내고 `JudgeRingView.RemoveRing`이 원을 지운다. |
| 2 | ClearAll(게임 오버, 다시 시작, 챕터 이동) 뒤 남는 원이 없음 | PASS | `B11_GameOverAndRestart_ClearRings`가 통과했다. 게임 오버 뒤 원이 0개인지, `StartRun`을 다시 불렀을 때 이전 판정의 원이 남지 않는지 확인한다. 코드에서는 `ClearAll`도 `CueCancelled`를 보낸다. 챕터 이동은 따로 테스트하지 않았고 같은 `StartRun` 경로로 확인했다. |
| 3 | 판정이 끝난(Judged) 원은 어떤 경로로든 다음 프레임까지 화면에서 사라짐 | PASS | `B11_AnyJudgedCue_RingGoneNextFrame`가 통과했다. 이벤트 없이 `Judged=true`로 바꾸고 한 프레임 지나면 원이 사라진다. `Refresh`의 안전망 코드가 처리한다. |
| 4 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 컴파일 OK, EditMode 111/111, PlayMode 84/84. |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 111/111, PlayMode 84/84 (B11 테스트 4건 모두 통과, OVERALL: PASS)
