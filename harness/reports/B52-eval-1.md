# B52 평가 보고서
판정: PASS (마지막 기준의 사용자 확인은 HUMAN으로 남김)

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 노드가 다가오는 동안은 느려지지 않음. PERFECT·NORMAL 직후 0.3배 슬로(실제 0.6초), 다음 판정 0.6박 전에는 복귀, FAIL은 슬로 없음 | PASS | PlayMode `B52_NoSlowBeforePress_SlowAfterDodge_GrayMuffled_ThenBack_SkipIfNextClose_NoneOnFail`가 단계별로 검사: 누르기 전 SlowFactor 1, 직후 0.3, 0.55초까지 유지, 0.6초 뒤 복귀, 다음 판정 0.6박 전 복귀 시작, FAIL이면 StartCount가 늘지 않음. EditMode `B47_Config_MatchesGddBetaB17_4_Second`가 상수(0.3, 0.06, 0.6, 0.15, 0.6)를 GDD와 비교. NORMAL을 직접 넣은 테스트는 없으나 `BulletTimeFx.cs`가 `r == Fail`만 제외하므로 코드상 NORMAL도 포함. |
| 2 | 슬로 동안 음악 낮고 먹먹(700Hz, 0.6배) + 회색(채도 −70). 처형·윈드밀 슬로도 동일 | PASS | 같은 PlayMode 테스트: music.pitch 0.3, lowPass LowPassMuffledHz, MusicFxGain 0.6, `screen.SlowSaturation` −70, ScreenFx 채도에 합쳐짐, 복귀 시 0·22000Hz. 7번 구간에서 `slow.Trigger()` 뒤 Muffle 1과 회색 확인. |
| 3 | 대문: 문 건너편 앞 3인칭 카메라 + 끝나면 1인칭 / 칼: NearMiss 카메라 | PASS | `B52_GameScene_ClosingGate_PressAtNormalSpeed_ThenGateCameraCut`: 실제 Game 씬 5챕터에서 대문을 평소 속도로 누르고(factorBeforeGate 1) 이후 0.3배, 카메라가 GateCamPosition 0.5m 안, 몸 렌더러 보임, 끝나면 localPosition zero·몸은 그림자만. 칼 NearMiss는 `camFx.ActiveCount > 0`과 EditMode NearMiss Height<0, Pitch<0 검사. |
| 4 | 판정 원 테두리에 박 펄스 없음 | PASS | PlayMode `B45_GameScene_PulseOnBeat_RingHudVignette_StepBob`이 마디 첫 박·보통 박에서 `ring.Target` 스케일 1을 단언, HUD·장구 레인·비네트 펄스는 유지. |
| 5 | unity-verify 통과 | PASS | 직접 재실행, 컴파일 OK, OVERALL PASS. |
| 6 | 테스트 빌드로 사용자 확인 | HUMAN | 사용자 확인 항목이라 판정에서 제외. |

## 실패 항목과 수정 지시
- 없음. 참고: PERFECT 판정만 테스트하므로 NORMAL 경로 테스트를 추가하면 더 좋다(필수 아님).

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 173/173, PlayMode 149/149 (OVERALL PASS).
