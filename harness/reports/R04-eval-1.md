# R04 평가 보고서 (1회차)
판정: PASS (단서 1건: 기존 T14 실패 — R05)
(evaluator 서브에이전트의 보고를 호출한 쪽이 저장함)

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 빨랫줄·처마 장애물 → R02 모델 (충돌 상자 유지, 그리기만 끔) | PASS | R04_GameScene_NoVisibleGrayPrimitive: Prop_clothesline·Prop_eave BoxCollider 켜짐, MeshRenderer 꺼짐, Model_* 있음. clothesline.png·eave.png |
| 2 | 사건 소품 → 모델, 부품 이름·개수 유지 | PASS | 모든 EventProp part에 Model_* 자식. B03_EventMotionTests(실행 중 경로) 통과. all_props.png |
| 3 | 회색 상자·공 없음 (PlayMode 검사) | PASS | 켜진 MeshRenderer 중 Cube/Sphere/Capsule/Cylinder 없음(그림자만 포함, 땅·길 띠 제외), 칼은 환도 모델 |
| 4 | 미리보기 PNG | PASS | harness/reports/R04/ 10장 + all_props.png |
| 5 | unity-verify 전체 통과 | PASS (단서) | EditMode 200/200, PlayMode 167/168. 실패 T14_Enemy_Model_WeaponInHand…는 기존 실패, R05 |

남은 문제: T14 (R05). banner_slide·tile_slip 미리보기는 지붕 위 시점이라 소품이 작게 보임 (기준 위반 아님).
