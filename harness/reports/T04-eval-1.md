# T04 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | SceneBuilder.BuildAll이 Title/Game 씬을 코드로 생성하고 Build Settings에 등록 | PASS | Assets/Scenes/Title.unity와 Game.unity가 존재한다. EditMode `T04_BuildSettings_HasTitleAndGame` 통과(씬 2개, 순서와 활성 상태 확인). Assets/Editor/SceneBuilder.cs가 생성한다. 이 평가에서 BuildAll을 따로 다시 실행하지는 않았고, 씬 파일과 PlayMode 테스트로 확인했다. |
| 2 | Game 씬에 GDD 4.9 구간 6개를 기본 도형으로 배치, 지붕 높이 4~6 m·간격 3~5 m, 경로 폭 3 m, 조명 | PASS | PlayMode `T04_GameScene_HasSectionsLightAndPlaceholders`가 Section_1~6, 방향광, 장애물 20개, 적 4명, 포위 5명, 무사, 기와집·성문·돌담 회색 상자, Ground를 확인한다. EditMode `T04_Roofs_HeightAndGapsInGddRange`와 `T04_Path_HasSixSections`도 통과. SceneBuilder.cs:169에서 `GameConfig.PathWidth`(3 m)로 길을 만든다. |
| 3 | 경로 전체 길이 600~700 m, 플레이어(캡슐+눈높이 1.65 m)가 박자에 맞춰 자동 이동, 4박자 후 10 m(±0.1 m) | PASS | EditMode `T04_Path_Length600To700_AndMatchesChart`와 `T04_Path_PointAt_ContinuousAndFollowsRoofs` 통과. PlayMode `T04_Player_EyeHeight_AndMoves10mIn4Beats`가 눈높이 1.65 m, 4박 후 경로 거리와 실제 이동 거리 10 m(±0.1), 속도 1.3배에서도 4박 10 m를 확인한다. `T04_Player_StopsWhenNotRunning`도 통과. 플레이어 캡슐은 SceneBuilder.cs:105. |
| 4 | 차트 데이터: 장애물 20, 일반 적 4, 포위 1회(5명), 무사 1, 각 이벤트의 박자·종류·키. EditMode로 개수와 장애물 간 최소 간격 4박자 확인 | PASS | EditMode `T04_Chart_Counts_MatchGdd49`, `T04_Chart_EachEventHasBeatKindAndKey`, `T04_Chart_ObstacleGapAtLeast4Beats`, `T04_Chart_ObstacleKeysMatchGdd3`, `T04_Chart_EventsDoNotOverlap` 모두 통과. 간격 테스트는 `MinObstacleGap >= GameConfig.MinObstacleGapBeats`를 단언한다. |
| 5 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행: OVERALL PASS |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 55/55, PlayMode 8/8 (OVERALL: PASS)
- 이 작업에는 빌드·에셋 기준이 없어 smoke-test와 check-glb는 실행하지 않았다.
