# Unity verify report
time: 2026-11-12 09:46:40

## EditMode : total=45 passed=45 failed=0 skipped=0 result=Passed
  - [Passed] RhythmActionGame.Tests.T01_GameConfigTests.T01_BeatAndSpeed_MatchGdd41
  - [Passed] RhythmActionGame.Tests.T01_GameConfigTests.T01_Judge_MatchGdd42
  - [Passed] RhythmActionGame.Tests.T01_GameConfigTests.T01_ObstacleAndCombat_MatchGdd44To46
  - [Passed] RhythmActionGame.Tests.T01_GameConfigTests.T01_Player_MatchGdd43
  - [Passed] RhythmActionGame.Tests.T01_GameConfigTests.T01_ScoreUiMap_MatchGdd47To49
  - [Passed] RhythmActionGame.Tests.T01_GameConfigTests.T01_TemplateLeftovers_Removed
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_BeatClock_AppliesSpeedToMusicAndMovement
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_BeatClock_PauseResume_UsesInjectedTime
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_Click_AccentEvery4Beats
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_Click_WaveformsAreAudibleAndAccentLouder
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_DistancePerBeat_Is2_5m_ForAnySpeed
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_Fail_MultipliesByPoint8_NotBelowMin
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_Metronome_SchedulesEveryBeat_AccentOnBar
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_Normal_KeepsSpeed
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_Perfect_AddsFivePercent
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_Perfect_CapsAtMax
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_SixPerfects_ReachExactlyMax
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_Timeline_CountsBeatsAt120Bpm
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_Timeline_PauseResume_BeatContinues
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_Timeline_SpeedChange_ContinuesBeat
  - [Passed] RhythmActionGame.Tests.T02_BeatAndSpeedTests.T02_Timeline_TimeOfBeat
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_EarlierThan300_IsIgnored(-300.1d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_EarlierThan300_IsIgnored(-1000.0d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_EarlyBetween150And300_IsFail(-150.1d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_EarlyBetween150And300_IsFail(-200.0d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_EarlyBetween150And300_IsFail(-299.9d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_JudgeSystem_AppearsTwoBeatsBefore_PerfectRaisesSpeed
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_JudgeSystem_EarlyPressIgnored_ThenJudged
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_JudgeSystem_NoInput_FailsAfter150ms
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_Labels
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_NoInputUntilPlus150_IsMissed
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_NoLegacyInputManager_InScripts
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_OuterRing_Shrinks360To120OverTwoBeats
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_SettingsOffset_ClampedAndSnappedTo5ms
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_TimingOffset_IsAddedToTarget
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_WithinNormalWindow_IsNormal(60.1d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_WithinNormalWindow_IsNormal(-60.1d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_WithinNormalWindow_IsNormal(149.9d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_WithinNormalWindow_IsNormal(-149.9d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_WithinPerfectWindow_IsPerfect(0.0d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_WithinPerfectWindow_IsPerfect(59.9d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_WithinPerfectWindow_IsPerfect(-59.9d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_WrongKey_IsFail(0.0d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_WrongKey_IsFail(100.0d)
  - [Passed] RhythmActionGame.Tests.T03_JudgeRulesTests.T03_WrongKey_IsFail(-250.0d)

## PlayMode : total=5 passed=5 failed=0 skipped=0 result=Passed
  - [Passed] RhythmActionGame.Tests.T03_JudgePresentationTests.T03_Font_HasJudgeGlyphs
  - [Passed] RhythmActionGame.Tests.T03_JudgePresentationTests.T03_Fx_GrayAndZoom_EnterAndExit
  - [Passed] RhythmActionGame.Tests.T03_JudgePresentationTests.T03_Label_ShownFor0_6Seconds
  - [Passed] RhythmActionGame.Tests.T03_JudgePresentationTests.T03_Labels_NormalAndFail
  - [Passed] RhythmActionGame.Tests.T03_JudgePresentationTests.T03_Ring_AppearsTwoBeatsBefore_AndShrinks

OVERALL: PASS
