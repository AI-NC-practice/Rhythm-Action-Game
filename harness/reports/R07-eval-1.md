# R07 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 적 Animator에 forward_run_1 달리기 상태, 이동 속도에 맞춰 재생 (미끄러지듯 다가오지 않음) | PASS | EnemyMoves.RunClip="forward_run_1". R07_EnemyClipTests(EditMode 2: 컨트롤러 Run 상태, 사건 소품 적 달리기 컴포넌트) 통과. R07_EventFigures_RunWhileMoving_NoGlide(미끄러짐 ≤ 8프레임), R07_CombatEnemies_RunWhenMoving_NotInPlace(다가오는 적 달리기) 통과. 재생 배율 = 속도/4.5 m/s. |
| 2 | 공중에 뜬 적 없음 (PlayMode 레이캐스트 검사) | PASS | R07_AllEnemies_FeetOnSupport_Raycast 통과(실패 목록 비어 있음). R07_EventFigures가 매 프레임 발밑 검사. R03_EnemiesAndForeshadow_StandOnVisibleRoofWallOrGround도 통과. |
| 3 | 보스·멈춘 적이 제자리에서 달리지 않음 | PASS | R07_CombatEnemies_RunWhenMoving_NotInPlace(제자리 달리기 ≤ 8프레임) 통과. 0.4 m/s 아래면 대기. |
| 4 | Mixamo 동작 선정·승인·다운로드·HumanoidImport | PASS | Assets/Art/Animations/Mixamo/flying_back.fbx(+.meta). R07_EnemyControllers_HaveRun_AndKickFallFromMixamo, T12_EachMixamoClip_IsHumanoid_NoMesh 통과. 사용자가 "Flying Back Death"를 고름(log.md). |
| 5 | 그 동작으로 지붕 아래로 떨어지고 카메라가 따라봄 | PASS | R07_RoofKick_FliesOffRoofEdge_LandsBelow_CameraFollows: DefeatState(KickOffRoof), 낙차 > 2.5 m, 지정 착지 1.5 m 이내, 옆집 지붕을 뚫지 않음, 카메라 고개(LastKickLookYaw > 5°). T14_RoofKick도 통과. |
| 6 | 미리보기 PNG | PASS | harness/reports/R07/ kick_fall_*, kick_landed_*, chase_run 확인. |
| 7 | unity-verify.ps1 전체 통과 | PASS | 재실행: EditMode 202/202, PlayMode 178/178, OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음.
- 참고: 발밑 검사는 발 딛는 폭 0.35 m·아래 0.35 m 허용, 틈 뛰어넘기·오르내리기 프레임 제외(log.md에 명시).

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 202/202, PlayMode 178/178
