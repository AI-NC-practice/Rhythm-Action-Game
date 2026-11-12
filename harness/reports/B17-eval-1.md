# B17 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | UI 가산 셰이더·머티리얼을 코드로 만들고 SceneBuilder가 에셋으로 연결 (빌드에 포함) | PASS | B17_GameScene_AdditiveMaterialAsset가 Game 씬의 ringView.additiveMaterial이 비어 있지 않고 셰이더 이름이 RhythmActionGame/UIAdditive인지 검사한다. 에셋 파일은 Assets/Art/Materials/Gray/UIAdditive.mat과 Assets/Shaders/에 있다. BuildGray 빌드와 smoke-test를 통과했다. |
| 2 | 다가오는 원: 가산 빛 고리 0.4→1.0, 빛 점 8개가 둘레를 돎, 바깥 원 빛 | PASS | B17_ApproachRing_ChargesUp_OrbitDots_OuterGlow가 빛 세기(등장 때 0.4, 중간 0.7, 목표 시점 1.0), 가산 빛 점 8개와 위치 변화, 바깥 원 빛 크기를 검사한다. |
| 3 | PERFECT: 번개선, 십자·X 섬광(700 px 이상), 충격 고리 3배, 빛 입자 16개 이상 바깥으로 흩어짐, 금빛 중심 섬광 | PASS | B17_Perfect_LightningFlareShockSparksCore가 모두 검사한다. 섬광 크기 700 이상, 입자 거리 증가, 충격 고리 배율 2.7 초과를 확인한다. |
| 4 | NORMAL: 작은 번개선 + 입자 8개 + 청록 고리. FAIL: 붉은 금 간 원 + 어두운 연기 + 흔들림 | PASS | B17_Normal_And_Fail_Distinct가 확인하는 것은 다음과 같다. NORMAL은 PERFECT보다 작은 번개선, 입자 8개, 섬광 없음, 청록 고리다. FAIL은 붉은 금 간 원, 어두운 연기, 입자·번개선 없음이다. "흔들림"은 이 테스트가 직접 검사하지 않지만, B8.3에서 만든 흔들림이 그대로 있어 통과로 판단했다. |
| 5 | 이펙트가 Canvas 10 층에 있고 판정 원(20)을 가리지 않으며 0.35초 안에 사라짐 | PASS | B17_BelowRings_GoneWithin035s가 이펙트의 부모가 labelLayer이고 sortingOrder가 원 Canvas보다 낮으며, 1.351초에 RingEffects가 0개인지 검사한다. |
| 6 | BuildGray 빌드 + smoke-test-build 통과 | PASS | 직접 실행. 빌드 OK, "player ran 12 s with a clean log". 로그에 셰이더 오류 없음. |
| 7 | unity-verify 전체 통과 | PASS | 직접 실행. OVERALL: PASS |

## 실패 항목과 수정 지시
- 없음.
- 참고: FAIL의 "흔들림"은 B17 테스트에 직접 확인하는 단언이 없다. 필요하면 보강을 권한다. 판정에는 영향 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 126/126, PlayMode 97/97 (B17 테스트 5개 포함 전부 통과)
- BuildGray 빌드 OK, smoke-test-build OK
