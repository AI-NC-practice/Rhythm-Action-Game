# B07 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 제안서 2장 A~G 상황마다 이벤트가 나가고 핵심 수치(Hit Stop, 흔들림, FOV, 섬광)가 적용됨 | PASS | B07_SituationFxTests PlayMode 테스트 7개가 모두 통과했다. A 예고: A_B_E_Duel이 0.5박 전보다 일찍 나오지 않고 0.45박 전에는 나오는지 확인한다. B 회피·패링: A_B_E_Duel, B_E_RoofParryAndKick이 적만 멈추고 timeScale은 1, 붓획 섬광, 노출 증가를 확인한다. C: C_D_NormalAndFail. D: C_D_NormalAndFail와 G_ParkourFail_Vignette03가 붉은 테두리, 흔들림, 비네트 0.3을 확인한다. E 처형: 슬로모션, 먹 획, FOV −4 미만, 롤 3 초과를 확인하고, 지붕 위에서는 발차기와 고개 5 초과를 확인한다. F: 포위 테스트. G: 도약·착지 PERFECT, FAIL, 기왓장 파편을 확인한다. |
| 2 | 슬로모션이 정해진 장면(처형, 윈드밀, 사건·전투 PERFECT)에서만 나옴 | PASS | 슬로모션 횟수를 직접 비교한다. 포위 1단계 PERFECT와 일반 장애물에서는 늘지 않는다. 동시 공격은 3번 중 1번만 늘고(16박 제한), 윈드밀·처형·무사 마지막 패링에서는 는다. 밧줄 넘기 PERFECT에서 늘고, 그 뒤 16박 안의 기와 미끄러짐은 거절된다. |
| 3 | 포위 전투 연출: 롤 3°, FOV +6°·위로 8°, 윈드밀 화살표마다 +2°(최대 +8°), 충격파 6 m | PASS | F_Surround 테스트가 롤 3°(5번), `Simultaneous` 프리셋의 FOV +6·고개 −8(3번), 화살표 i개일 때 FOV 2·i(허용 오차 0.05), 최대 +8 이하, `ShockRadius` 6 m(0.2초에 3 m, 0.4초에 6 m)를 모두 확인한다. 값은 `GameConfig.cs:294, 327`에 있다. |
| 4 | BuildGray 빌드 + smoke-test-build.ps1 통과 (-startGame 포함) | PASS | BuildGray를 직접 다시 실행해 성공했다. smoke-test-build.ps1은 "player ran 12 s with a clean log"로 끝났다. 이 스크립트는 `-startGame` 인자를 받지 않으므로, exe를 `-batchmode -startGame`으로 직접 15초 실행했고 예외·오류 로그가 없었다. 인자 처리는 `TitleMenu.cs:30`에 있다. |
| 5 | unity-verify.ps1 전체 통과 | PASS | OVERALL: PASS |

## 실패 항목과 수정 지시
- 없음.
- 참고: 제안서 2장의 "소리" 열은 B08 범위라서 평가에서 뺐다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 105/105, PlayMode 76/76 (B07 7개 포함)
- 빌드 `Builds/Gray/RhythmActionGame.exe`, smoke-test 정상. `-startGame` 직접 실행도 정상.
