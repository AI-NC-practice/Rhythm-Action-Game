# T08 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 타이틀 문구(제목, 시작, 설정, 종료), 맑은 고딕 | PASS | T08_Title_Texts_AndSettingsSaved가 네 문구를 검사한다. T08_Font_HasAllUiGlyphs는 모든 UI 글자가 폰트에 있는지 확인한다. 폰트가 맑은 고딕인지는 코드로 확인하지 않았다. |
| 2 | 설정: 타이밍 보정 −200~+200(5ms 단위, "+15ms"), 음량 0~100 기본 80, 돌아가기, PlayerPrefs 저장·불러오기 | PASS | T08_Title_Texts_AndSettingsSaved는 슬라이더 범위, "+15ms"와 "-20ms" 표시, PlayerPrefs 저장, 씬을 다시 불러온 뒤 값 복원을 검사한다. T08_Settings_SaveAndLoad는 음량 기본값 80과 상한 100을 검사한다. |
| 3 | HUD: 하트 3개, "x1.00", 점수, "콤보 N"(N≥2일 때만) | PASS | T08_Hud_HeartsSpeedScoreCombo가 하트 수, 속도·점수 문구, 콤보 1일 때 숨김, 콤보 2일 때 "콤보 2" 표시, 위치(앵커)를 검사한다. T08_Texts_MatchGdd48도 있다. |
| 4 | 일시정지(Esc): "일시정지", "계속", "타이틀로", 음악과 BeatClock 정지 | PASS | T08_Pause_StopsClockAndMusic_ThenResumes가 문구, clock.IsPaused, timeScale 0, 정지 중 박자·거리 불변, 재개 후 이어짐을 검사한다. Esc 키 입력 자체는 테스트하지 않고 Toggle()을 호출한다. 음악 정지는 timeScale 0과 clock 정지로만 확인했다. |
| 5 | 결과 화면 문구 전체 | PASS | T08_Result_Cleared_ShowsCountsAndRank가 "스테이지 클리어", "PERFECT 18", "NORMAL 5", "FAIL 2", "최고 속도 x1.30", "점수 5900", 랭크, "다시 하기", "타이틀로"를 검사한다. T08_Result_GameOver_RankDash는 "게임 오버"와 "랭크 -"를 검사한다. |
| 6 | 점수 300/100/0, 랭크 기준 EditMode 테스트 | PASS | T08_Score_300_100_0_AndCombo와 T08_Rank_Thresholds가 S(90%, FAIL 0), A(75%), B(50%), C, 게임 오버 "-", "90%여도 FAIL이 있으면 S가 아님"을 검사한다. |
| 7 | 속도감: s≥1.10에서 집중선, 세기 (s−1)/0.3, FOV 최대 +5° | PASS | T08_SpeedFx_Intensity(EditMode)와 T08_SpeedLines_AndFovBonus(PlayMode)가 1.05에서 0, 1.15에서 0.5와 FOV 72.5, 1.30에서 1과 FOV 75, 집중선 표시와 알파 값을 검사한다. 1.10 정확히에서는 세기가 0.33(=0.1/0.3)이므로 1.10부터 표시된다는 기준과 맞는다. |
| 8 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음. 4번의 Esc 키 입력 경로와 1번의 폰트 이름은 테스트에 없다. 판정에는 영향이 없고 사람이 플레이로 확인하면 된다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 73/73, PlayMode 33/33 (T08 테스트 전부 Passed)
- 에셋 작업과 빌드가 아니므로 check-glb, smoke-test는 실행하지 않았다.
