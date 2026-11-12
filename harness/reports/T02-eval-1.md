# T02 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | BeatClock이 dspTime 기준으로 소수 박자를 계산하고, 일시정지/재개 시 박자가 이어진다 | PASS | `Assets/Scripts/Rhythm/BeatClock.cs`의 `TimeSource` 기본값이 `AudioSettings.dspTime`이다. `T02_BeatClock_PauseResume_UsesInjectedTime`, `T02_Timeline_PauseResume_BeatContinues`, `T02_Timeline_CountsBeatsAt120Bpm`가 통과했다. |
| 2 | Metronome이 `AudioClip.Create` 클릭음을 매 박 재생하고 4박마다 강세를 준다 | PASS | `ClickSynth.cs:38`에서 `AudioClip.Create`를 쓴다. `Metronome.cs`는 `PlayScheduled`로 매 박을 예약한다. `T02_Metronome_SchedulesEveryBeat_AccentOnBar`는 8박 동안 예약 8회와 강세 패턴을 확인한다. `T02_Click_AccentEvery4Beats`, `T02_Click_WaveformsAreAudibleAndAccentLouder`도 통과했다. |
| 3 | PERFECT는 min(s+0.05, 1.30), NORMAL은 유지, FAIL은 max(s×0.8, 1.00). 경계값 테스트 통과 | PASS | `SpeedRules.Apply`가 이 규칙을 구현한다. `T02_Perfect_AddsFivePercent`, `T02_Perfect_CapsAtMax`, `T02_Normal_KeepsSpeed`, `T02_Fail_MultipliesByPoint8_NotBelowMin`, `T02_SixPerfects_ReachExactlyMax`가 경계값을 검사하고 통과했다. 클래스 이름은 SpeedController가 아니라 `SpeedRules`/`SpeedState`이다. 기능은 같아서 감점하지 않았다. |
| 4 | 속도 배율이 음악 속도와 이동 속도(5×s)에 적용되고, 1박자 이동 거리가 s와 무관하게 2.5m임을 테스트로 확인 | PASS | `T02_BeatClock_AppliesSpeedToMusicAndMovement`가 `music.pitch == s`, `MoveSpeed == 5s`를 확인한다. `T02_DistancePerBeat_Is2_5m_ForAnySpeed`가 s 6개 값에서 1박 2.5m, 4박 10m를 확인한다. |
| 5 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 컴파일 OK, EditMode 21/21, OVERALL PASS. |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 21/21, PlayMode 0/0 (PlayMode 테스트가 아직 없음). 보고서는 `harness/reports/verify-latest.md`에 있다.
