# B23 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 첫 공격 = 도착 후 가장 가까운 박 + 2박, 첫 원은 도착 0.5초 안 (대치·포위·무사) | PASS | `B23_EncounterStartBeat_NearestBeatPlus2`가 박 위, 박 사이, 프레임 오차의 경계값을 검사한다. `B23_FirstRing_Within05s_OfArrival`은 89박(대치), 181박(포위), 256박(무사)에서 시작해 첫 원이 도착 후 0.52초 안에, 박 위에서 나타나는지 검사한다. T07 테스트의 기대값은 `EncounterStartBeat(enc.ArriveBeat)`로 바꿨다. 구현은 `CombatRules.cs`의 `EncounterStartBeat`와 `StageDirector.Combat.cs`의 `ScheduleEncounter`. |
| 2 | 대치 연출: FOV −4° 프리셋, 적이 0.5 m 다가옴 | PASS | `B23_Standoff_CameraAndEnemyStepForward`가 대치 연출 발동, `cameraFx.Fov`가 −4인지, 첫 공격 직전 `StandoffOffset`이 0.5인지, 플레이어와의 거리가 0.3 m 이상 줄었는지 확인한다. 상수는 `GameConfig.Fx.Standoff`(FOV −4)와 `StandoffApproachMeters`(0.5). |
| 3 | 전투 후 다음 마디 첫 박에 다시 달림 (기존 규칙 유지) | PASS | 기존 T06·T07 테스트가 모두 통과했다. `NextBar`와 `FirstTargetBeat`는 그대로 있다. |
| 4 | unity-verify 전체 통과 | PASS | 아래 실행 결과 참고. |

## 실패 항목과 수정 지시
- 없음.
- 참고(판정에 영향 없음): 적이 0.5 m 다가오는 동작은 `!IsSurround` 조건 때문에 포위 전투에서는 일어나지 않는다. FOV 연출은 포위 전투에서도 `EncounterStarted`로 발동한다. 적 접근과 FOV 테스트는 대치(89박) 한 곳에서만 한다. GDD-beta B10.2는 대치·포위·무사 모두에서 적이 다가온다고 읽힐 수도 있으니, 의도한 예외라면 문서나 로그에 남기는 게 좋다.
- 참고(판정에 영향 없음): `PresentationHooks.cs`의 `OnRewind` 위에 summary 주석이 두 개 겹쳐 있고, `FxLooks.cs`의 한 줄 끝에도 옛 주석이 남아 있다. 동작에는 영향이 없다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 130/130, PlayMode 112/112, OVERALL: PASS (직접 재실행, B23 테스트 5건 모두 통과)
