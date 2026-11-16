# R02 평가 보고서 (1회차)
판정: PASS (단서: PlayMode 기존 실패 1건은 R02와 무관, R05 담당)

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | R01 승인 목록·포인트 안에서만 생성, 모든 호출 flowId | PASS | 승인 35+배경6 = 10,800(견적, 여유 포함 최대 13,400). 실제 2,050+1,750+7,000 = 10,800. asset_specs 건물 등 R02 35개 id가 mkpage.py ASSETS와 일치(mkpage는 choga/choga_L을 한 줄 표기, 집합 비교 결과 차이 없음). varco-usage.log에 run_flow 기록, flowId는 varco-guard 훅이 강제 |
| 2 | 4면도 검수 → 정면 → 고폴리 → 바닥 삭제 + 로우폴리 + 굽기 | PASS | contact/c1~c7·f1~f3 존재, log 기록, asset_specs calls sheet/image/mesh 각 1회, status generated, T11R 방식 cull_bake 결과 GLB에 색+노멀 텍스처 2장(check-glb) |
| 3 | 미리보기 + mesh_check 통과, 파일 20MB 이하 | PASS | preview/r02 35장 + r02_p1~p3 모음(직접 확인: 모두 알아볼 수 있는 단일 물체). check-glb 재실행 choga/arrow/zelkova/straw OK(최대 9.9MB, 상한 15MB). mesh_check 재실행: 비다양체 0, 경계 변은 지운 바닥 테두리(straw 0) |
| 4 | Assets/Art/Models/<id>/ 저장, 툰 머티리얼 | PASS | 35개 GLB 모두 존재, R02_ToonMaterial_HasBakedTextures 통과, 배경 6장 Assets/Art/Backdrops/ (R02_Backdrops_InAssets 통과) |
| 5 | unity-verify 전체 통과 | PASS (단서) | EditMode 193/193(R02 테스트 4개 통과), PlayMode 162/163. 실패 T14_Enemy_Model_WeaponInHand…는 R02 시작 직전(log.md 624행)에 이미 있던 실패이며 R05 acceptance가 따로 고친다. 컨트롤러 4개가 Unity에서 다시 직렬화되어 ID만 바뀐 미커밋 변경이 원인 후보 |

## 남은 문제 (R02 판정에 영향 없음)
- PlayMode T14 실패 1건: R05에서 처리. R02 결과에서는 "회귀 없음"으로만 인정했으므로 R05에서 "전체 통과 두 번 연속"을 반드시 확인할 것.
- 테스트 R02_MapAssetTests는 상한·텍스처 존재만 보며 경계 변·조각 수는 검사하지 않음(수동 mesh_check로 확인). 필요하면 R03에서 보강.
- 로우폴리 경계 변이 큰 모델(choga 262, zelkova 345)은 바닥 테두리 외에 굽기 컷 흔적일 수 있음. 육안 미리보기는 문제 없음.

## 검증 실행 결과
- unity-verify(verify-latest.md 21:16): 컴파일 OK, EditMode 193/193, PlayMode 162/163 (기존 실패 1)
- check-glb 4건 OK, mesh_check 3건 OK (직접 재실행)
