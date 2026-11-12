# B06 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | ScreenFx: Volume에 Vignette, ChromaticAberration, LensDistortion, Bloom, postExposure(ColorAdjustments)를 추가하고 항목별 최댓값 규칙(채도는 최솟값)을 따른다 | PASS | B06_ScreenFx_VolumeOverrides_MaxRule이 다음을 확인한다. Profile에 override 5개가 있다. 비네트는 더하지 않고 큰 값을 쓴다(0.3과 0.2 → 0.3, 0.45가 오면 0.45). 색수차 0.2와 0.4 → 0.4. 채도 −30과 −60 → −60, −80이 오면 −80. 계산한 값이 실제로 Volume에 적용된다. |
| 2 | VfxPool: 붓획 섬광, 먹 획, 먹 X 획, 불씨, 충격파 링, 흙먼지, 기왓장·한지 파편을 코드로 생성·재사용하고, 섬광은 동시에 하나이며 불투명도 0.6 이하다 | PASS | B06_VfxPool_AllKinds_Pooled_Reused가 모든 종류의 Play 성공, 종류별 PoolSize(4)만큼 미리 생성, AreSame으로 재사용, 불씨 30개, 충격파 0.2초에 3 m·0.4초에 풀로 반환을 확인한다. B06_Flash_OneAtATime_HigherGradeWins_AlphaMax06이 섬광이 동시에 하나이고 등급 규칙대로 바뀌며 알파가 0.6 이하임을 확인한다. B06_Presets_PlayScreenAndVfx도 통과했다. |
| 3 | FAIL 피격: 흔들림 0.6, 주홍 비네트 0.45에서 0(0.4초), 다음 원이 뜨면 절반 | PASS | B06_FailHit_RedVignetteShake_HalvedOnNextRing이 흔들림 세기 0.6, 비네트 0.45와 주홍색, 채도 −80, 렌즈 왜곡 −0.2, 0.2초에 0.225, 0.41초에 0, 다음 원이 뜨면 절반을 모두 단언한다. |
| 4 | 판정 원이 모든 효과보다 위에 그려진다 (Overlay Canvas 20) | PASS | B06_GameScene_RingCanvasAboveEffects가 실제 Game 씬에서 원 Canvas가 ScreenSpaceOverlay, sortingOrder 20이고 섬광 Canvas보다 위인지 확인한다. SceneBuilder 연결과 파티클 머티리얼 에셋도 확인한다. |
| 5 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 105/105, PlayMode 69/69 (B06 테스트 5개 포함). OVERALL: PASS.
- 에셋·빌드 작업이 아니라서 check-glb와 smoke-test는 실행하지 않았다.
- B06_ScreenAndVfxTests.cs에서 단언하는 값은 GDD-beta와 제안서 수치와 같다.
