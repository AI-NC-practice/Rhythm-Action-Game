# Unity verify report
time: 2026-11-24 17:53:23

## EditMode : total=1 passed=1 failed=0 skipped=0 result=Passed
  - [Passed] RhythmActionGame.Tests.R39_ComboResetTests.R39_ChapterRewind_ResetsCombo_BattleRewindKeeps

## PlayMode : total=18 passed=10 failed=8 skipped=0 result=Failed(Child)
- FAIL RhythmActionGame.Tests.R37_AttractLoopPlayTests.R37_Wrap_SameFrameBeforeAndAfter_NoFade: 되풀이 전후 화면이 같아야 함 (평균 차이 0.0108) Expected: less than 0.00400000019f But was: 0.0108116372f
- FAIL RhythmActionGame.Tests.R40_WallRunPlateauPlayTests.R40_Stage_WallRun_Normal_Plateau: System.IO.DirectoryNotFoundException : Could not find a part of the path "C:\workdir\Rhythm-Action-Game\harness\reports\R40\wallrun_60_Normal_0.png".
- FAIL RhythmActionGame.Tests.R40_WallRunPlateauPlayTests.R40_Stage_WallRuns_Perfect_Plateau: System.IO.DirectoryNotFoundException : Could not find a part of the path "C:\workdir\Rhythm-Action-Game\harness\reports\R40\wallrun_32_Perfect_0.png".
- FAIL RhythmActionGame.Tests.R41_SlideClearancePlayTests.R41_Normal_CameraClearsUnderside: eave_136: 길 위에서 장애물 밑면을 찾지 못함 Expected: greater than 0 But was: 0
- FAIL RhythmActionGame.Tests.R41_SlideClearancePlayTests.R41_Perfect_CameraClearsUnderside: eave_136: 길 위에서 장애물 밑면을 찾지 못함 Expected: greater than 0 But was: 0
- FAIL RhythmActionGame.Tests.R42_LookBackBarricadePlayTests.R42_Cut0_Alley_JigeFalls_PursuersTrip: 쓰러진 Straw 아래가 비어 있음 (Infinity m) Expected: less than 0.5f But was: Infinity
- FAIL RhythmActionGame.Tests.R42_LookBackBarricadePlayTests.R42_Cut1_Market_JigeFalls_PursuersTrip: 쓰러진 Straw 아래가 비어 있음 (Infinity m) Expected: less than 0.5f But was: Infinity
- FAIL RhythmActionGame.Tests.R42_LookBackBarricadePlayTests.R42_Cut2_Roof_TilesScatter_PursuersTrip: 넘어진 무리 Pursuer_1 발밑 면과의 거리 0.95 m (공중·속) Expected: less than 0.349999994f But was: 0.954683542f
  - [Passed] RhythmActionGame.Tests.R13_RoofOverhangPlayTests.R13_Preview_ApproachAndUnder
  - [Passed] RhythmActionGame.Tests.R13_RoofOverhangPlayTests.R13_RoofEaves_AreTallerBuildingOverhang_GroundEaveUnchanged
  - [Passed] RhythmActionGame.Tests.R36_AttractPlayTests.R36_GameScene_AttractRun_ThenSeamlessStart
  - [Passed] RhythmActionGame.Tests.R36_AttractPlayTests.R36_ToTitle_FromPauseAndResult_OpensAttract
  - [Passed] RhythmActionGame.Tests.R37_AttractLoopPlayTests.R37_Start_MistLifts_Chapter1HasNormalFogAndBackdrop
  - [Failed] RhythmActionGame.Tests.R37_AttractLoopPlayTests.R37_Wrap_SameFrameBeforeAndAfter_NoFade
  - [Passed] RhythmActionGame.Tests.R38_MenuHoverPlayTests.R38_AttractMenu_EscIgnored_ButtonsReactToMouse
  - [Passed] RhythmActionGame.Tests.R38_MenuHoverPlayTests.R38_PauseAndResultButtons_ReactToMouse
  - [Passed] RhythmActionGame.Tests.R39_ChapterRewindComboPlayTests.R39_HpZero_ChapterRewind_ComboResetToZero
  - [Passed] RhythmActionGame.Tests.R40_WallRunPlateauPlayTests.R40_Profile_RisesHoldsFalls
  - [Failed] RhythmActionGame.Tests.R40_WallRunPlateauPlayTests.R40_Stage_WallRun_Normal_Plateau
  - [Failed] RhythmActionGame.Tests.R40_WallRunPlateauPlayTests.R40_Stage_WallRuns_Perfect_Plateau
  - [Failed] RhythmActionGame.Tests.R41_SlideClearancePlayTests.R41_Normal_CameraClearsUnderside
  - [Failed] RhythmActionGame.Tests.R41_SlideClearancePlayTests.R41_Perfect_CameraClearsUnderside
  - [Failed] RhythmActionGame.Tests.R42_LookBackBarricadePlayTests.R42_Cut0_Alley_JigeFalls_PursuersTrip
  - [Failed] RhythmActionGame.Tests.R42_LookBackBarricadePlayTests.R42_Cut1_Market_JigeFalls_PursuersTrip
  - [Failed] RhythmActionGame.Tests.R42_LookBackBarricadePlayTests.R42_Cut2_Roof_TilesScatter_PursuersTrip
  - [Passed] RhythmActionGame.Tests.R42_LookBackBarricadePlayTests.R42_Cut3_PalaceWall_TilesScatter_PursuersTrip

OVERALL: FAIL
