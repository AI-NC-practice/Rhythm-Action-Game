# R39 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 체력 0 챕터 되감기 → 장구 콤보 0, HUD 콤보 표시 사라짐 | PASS | ScoreKeeper.cs:58-62 `kind == RewindKind.Chapter`면 resetCombo, ScoreBoard.RewindTo(86-93) Combo=0. PlayMode R39_HpZero_ChapterRewind_ComboResetToZero(실제 Game 씬, 콤보>2 → 체력 0 → LastRewind Chapter, Combo 0, ComboText 비활성/빈 문자열) 통과. EditMode R39_ChapterRewind_ResetsCombo_BattleRewindKeeps 통과(최대 콤보 유지, 다시 1부터) |
| 2 | 무사 결투·포위 전투 FAIL 되감기는 콤보 유지 | PASS | 같은 EditMode 테스트: RewindTo(cp) 뒤 콤보 30 유지, 점수 entry−1000 |
| 3 | GDD-beta 사용자 결정 기록, EditMode·PlayMode 테스트 | PASS | docs/GDD-beta.md:476 (B22), 테스트 2개 |
| 4 | unity-verify 전체 통과 | PASS | EditMode 209/209, PlayMode 229/230 (skipped 1) |

비고: 전투 FAIL 되감기 콤보 유지는 EditMode(RewindTo 직접 호출)로만 확인. 코드 분기가 단순해 PASS.
