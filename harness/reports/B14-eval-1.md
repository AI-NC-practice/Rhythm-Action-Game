# B14 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 판정마다 원 자리에 다른 이펙트. PERFECT는 금빛으로 1.8배까지 커지고 붓획 조각이 붙음. NORMAL은 작은 청록 고리. FAIL은 붉은 원이 흔들리며 두 쪽으로 갈라짐 | PASS | B14_EachResult_DistinctEffect 통과. 색, 조각 수(2/0/2), 크기(1.8배/1.3배), 흔들림, 좌우 벌어짐을 실제 값으로 검사한다. 구현은 JudgeRingView.cs의 SpawnRingFx와 PoseRingFx이고, 수치는 GameConfig.RingFx* 한 곳에 있다. |
| 2 | 이펙트는 Canvas 10(판정 문구·이펙트 층)에 있고 Canvas 20 판정 원을 가리지 않음. 0.35초 안에 사라짐 | PASS | B14_BelowRings_GoneWithin035s 통과. 이펙트의 부모가 labelLayer이고, sortingOrder는 JudgeFxCanvasOrder 10으로 RingCanvasOrder 20보다 낮다. 세 이펙트 길이는 모두 0.35초 이하(0.3/0.25/0.35)이고, 0.351초 뒤 RingEffects가 0개다. B14_GameScene_EffectsSpawnOnJudgement도 통과했다(실제 Game 씬 판정에서 이펙트가 생김). |
| 3 | BuildGray 빌드와 smoke-test-build.ps1 통과 | PASS | 직접 실행했다. BuildGray 성공, smoke-test는 12초 동안 깨끗한 로그로 통과. |
| 4 | unity-verify 전체 통과 | PASS | 직접 실행했다. 컴파일 OK, EditMode 115/115, PlayMode 92/92, OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음.

참고: 이펙트의 실제 모양은 화면 캡처로 확인하지 못했다. 모양과 느낌은 B15 사용자 확인에서 보면 된다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 115/115, PlayMode 92/92 (B14 테스트 3개 포함)
- BuildGray 빌드 OK, smoke-test OK (12초, 깨끗한 로그)
