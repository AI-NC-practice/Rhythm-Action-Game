# R38 평가 보고서 (2회차)
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 시작화면(메뉴·시작선까지 옮겨 가는 중) Esc 무시, 빌드에 반영 | PASS | PauseMenu.cs InAttract 가드, R38_AttractMenu_EscIgnored_ButtonsReactToMouse 통과. Builds/Final RhythmActionGame.Game.dll 2026-11-24 20:30:57 갱신, UiHover 포함 — 1회차 FAIL 사유 해소 |
| 2 | 마우스 올림 → 밝아지고 커짐, 내리면 원래대로, 누름 반응 | PASS | CheckButton이 올림(금빛 배경·HoverScale·금빛 글자)·누름(PressedScale)·내림 값 검사. 시작·설정·종료·돌아가기·일시정지(timeScale 0)·타이틀로. 결과 화면 버튼은 UiHover 존재만 확인 |
| 3 | PlayMode 테스트·미리보기 PNG | PASS | 테스트 2개 통과, R38/menu_idle.png·menu_hover_settings.png 직접 확인 |
| 4 | unity-verify 전체 통과 | PASS | 컴파일 OK, EditMode 209/209, PlayMode 229/230 (skipped 1) |

참고(비필수): Esc 테스트는 실제 키가 아니라 pause.Toggle()을 부른다. 결과 화면 버튼은 컴포넌트 존재만 확인.
smoke-test-build: OK (12초, 로그 깨끗)
