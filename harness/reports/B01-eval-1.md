# B01 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | B1.1 Canvas 순서: 판정 원 20, 문구·이펙트 10, HUD 5 | PASS | `B01_GameScene_CanvasOrder_RingOnTop`는 Game 씬을 실제로 로드해 sortingOrder 값을 확인한다. 메뉴 Canvas가 원보다 위인지도 확인한다. `B01_Config_MatchGddBetaB1`은 GameConfig 값을 확인한다. |
| 2 | B1.2 문구: 원보다 110 px 아래, 44 pt, 0.1초 등장(1.3→1.0), 0.35초 상승 20 px, 0.45초 뒤 제거 | PASS | `B01_Label_110Below_44pt_PopRise_045s_OnLabelLayer`는 실제 시간을 주입해 위치 −110, 글자 크기 44, 배율 1.3→1.0, 상승 중 위치, 0.44초에는 남고 0.46초에는 제거되는 것을 확인한다. `B01_LabelPose_PopThenRise_045s`는 공식을 직접 확인한다. |
| 3 | B1.2 새 원이 문구 중심 200 px 안에 뜨면 문구 알파 0.3 이하 | PASS | `B01_Label_DimmedWhenNewRingNearby`는 근처에 원이 없을 때 알파 1, 새 원이 200 px 안에 뜬 뒤 알파 0.3 이하를 확인한다. |
| 4 | B1.3 안전 타원(22%/18%) 밖이거나 카메라 뒤면 타원 경계로 이동, 0.1초 뒤 고정, 끌어온 경우 꺾쇠 표시 | PASS | `B01_SafeEllipse_RadiiAndClamp`는 반지름 422.4와 194.4, 방향 유지를 확인한다. `B01_Ring_ClampedToSafeEllipse_WithChevron`는 화면 밖, 화면 안, 카메라 뒤 세 경우의 경계 위치와 꺾쇠(40 px, 바깥쪽)를 확인한다. `B01_Ring_LockedAfter0_1s`는 0.1초 전에는 따라가고 이후에는 고정되며 바깥 원은 계속 줄어드는 것을 확인한다. |
| 5 | B1.3 1박 미만 간격으로 이어지는 원은 가로 160 px 간격, 먼저 오는 원이 왼쪽 | PASS | `B01_Chain_SubBeatRings_SideBySide160px`는 반 박 간격 4개가 160 px 간격이고 왼쪽부터 순서대로이며 타원 안에 있고 묶음 가운데가 기준점임을 확인한다. 1박 간격 원은 묶이지 않는 것도 확인한다. `B01_Chain_160pxApart_FirstLeft_InsideEllipse`는 레이아웃 함수를 확인한다. |
| 6 | B1.3 원 바깥 빛 테두리, 마무리 원은 금색 | PASS | `B01_Ring_Glow_FinisherGold`는 Glow 존재, 알파 0.5, 원보다 큼, 마무리 원은 금색임을 확인한다. |
| 7 | B1.4 판정 중 노출 −0.4, 비네트 0.3 (채도 −60, FOV 58 유지) | PASS | `B01_JudgeScreen_ExposureAndVignette`는 판정 중 값과 복귀 후 0을 확인하고, 채도 −60과 FOV 58 유지도 확인한다. |
| 8 | 새 수치는 GameConfig beta 구역에만, EditMode 테스트로 GDD-beta와 비교 | PASS | git diff에서 GameConfig.cs의 beta 구역에 상수가 추가된 것을 확인했다. `B01_Config_MatchGddBetaB1`이 GDD-beta 값과 비교한다. 메뉴 Canvas 30과 빛 테두리 지름 192 px은 GDD-beta에 없어 임의로 정한 값이고, 주석에 그 사실이 적혀 있다. |
| 9 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. 아래 참조. |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 77/77, PlayMode 45/45 (OVERALL: PASS). 직접 재실행했고 `harness/reports/verify-latest.md`와 일치한다. 이 중 B01 EditMode 4건과 PlayMode 8건이 모두 통과했다.
- 에셋과 빌드 작업이 아니라서 `check-glb.ps1`과 `smoke-test-build.ps1`은 실행하지 않았다.
