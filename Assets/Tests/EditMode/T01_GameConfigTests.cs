using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>GameConfig 값이 docs/GDD.md의 값과 같은지 확인한다. 기대값은 GDD 문서가 기준이다.</summary>
    public class T01_GameConfigTests
    {
        const float Eps = 1e-5f;

        [Test]
        public void T01_BeatAndSpeed_MatchGdd41()
        {
            Assert.AreEqual(120f, GameConfig.Bpm, Eps);
            Assert.AreEqual(0.5f, GameConfig.SecondsPerBeat, Eps);
            Assert.AreEqual(4, GameConfig.BeatsPerBar);
            Assert.AreEqual(5f, GameConfig.BaseMoveSpeed, Eps);
            Assert.AreEqual(2.5f, GameConfig.MetersPerBeat, Eps);
            Assert.AreEqual(1.00f, GameConfig.SpeedStart, Eps);
            Assert.AreEqual(1.00f, GameConfig.SpeedMin, Eps);
            Assert.AreEqual(1.30f, GameConfig.SpeedMax, Eps);
            Assert.AreEqual(0.05f, GameConfig.PerfectSpeedStep, Eps);
            Assert.AreEqual(0.8f, GameConfig.FailSpeedMultiplier, Eps);
        }

        [Test]
        public void T01_Judge_MatchGdd42()
        {
            Assert.AreEqual(2f, GameConfig.ApproachBeats, Eps);
            Assert.AreEqual(90f, GameConfig.PerfectWindowMs, Eps);   // beta1-1: GDD-beta B8.4 (GDD 4.2는 60)
            Assert.AreEqual(240f, GameConfig.NormalWindowMs, Eps);   // beta1-1: GDD-beta B8.4 → B12.1 (GDD 4.2는 150)
            Assert.AreEqual(300f, GameConfig.EarlyIgnoreMs, Eps);
            Assert.AreEqual(-60f, GameConfig.JudgeSaturation, Eps);
            Assert.AreEqual(0.15f, GameConfig.JudgeEnterSeconds, Eps);
            Assert.AreEqual(0.2f, GameConfig.JudgeExitSeconds, Eps);
            Assert.AreEqual(70f, GameConfig.FovDefault, Eps);
            Assert.AreEqual(58f, GameConfig.FovZoom, Eps);
            Assert.AreEqual(25f, GameConfig.JudgeMaxTurnDegrees, Eps);
            Assert.AreEqual(1080f, GameConfig.UiReferenceHeight, Eps);
            Assert.AreEqual(120f, GameConfig.RingTargetDiameterPx, Eps);
            Assert.AreEqual(360f, GameConfig.RingStartDiameterPx, Eps);
            Assert.AreEqual(0.6f, GameConfig.JudgeTextSeconds, Eps);
            Assert.AreEqual(-200, GameConfig.TimingOffsetMinMs);
            Assert.AreEqual(200, GameConfig.TimingOffsetMaxMs);
            Assert.AreEqual(5, GameConfig.TimingOffsetStepMs);
            Assert.AreEqual(0, GameConfig.TimingOffsetDefaultMs);
            Assert.AreEqual(0.3f, GameConfig.SlowMotionTimeScale, Eps);
            Assert.AreEqual(0.4f, GameConfig.SlowMotionSeconds, Eps);
        }

        [Test]
        public void T01_Player_MatchGdd43()
        {
            Assert.AreEqual(1.65f, GameConfig.EyeHeight, Eps);
            Assert.AreEqual(3, GameConfig.MaxHp);
            Assert.AreEqual(1.10f, GameConfig.SpeedLinesThreshold, Eps);
            Assert.AreEqual(5f, GameConfig.SpeedFovBonusMax, Eps);
        }

        [Test]
        public void T01_ObstacleAndCombat_MatchGdd44To46()
        {
            Assert.AreEqual(4, GameConfig.ObstacleBeats);
            Assert.AreEqual(4, GameConfig.MinObstacleGapBeats);
            Assert.AreEqual(3f, GameConfig.WallRunDistance, Eps);

            Assert.AreEqual(2, GameConfig.NormalEnemyAttacks);
            Assert.AreEqual(2, GameConfig.NormalEnemyAttackIntervalBeats);
            Assert.AreEqual(5, GameConfig.BossAttacks);
            Assert.AreEqual(1, GameConfig.BossAttackIntervalMinBeats);
            Assert.AreEqual(2, GameConfig.BossAttackIntervalMaxBeats);

            Assert.AreEqual(5, GameConfig.SurroundEnemyCount);
            Assert.AreEqual(3, GameConfig.SurroundSwordCount);
            Assert.AreEqual(2, GameConfig.SurroundSpearCount);
            Assert.AreEqual(GameConfig.SurroundEnemyCount, GameConfig.SurroundSwordCount + GameConfig.SurroundSpearCount);
            Assert.AreEqual(4f, GameConfig.SurroundRadius, Eps);
            Assert.AreEqual(1f, GameConfig.SurroundSequentialIntervalBeats, Eps);
            Assert.AreEqual(3, GameConfig.SurroundSimultaneousCount);
            Assert.AreEqual(4, GameConfig.WindmillArrowCount);
            Assert.AreEqual(0.5f, GameConfig.WindmillIntervalBeats, Eps);
            Assert.AreEqual(0.2f, GameConfig.SurroundCameraTurnSeconds, Eps);
        }

        [Test]
        public void T01_ScoreUiMap_MatchGdd47To49()
        {
            Assert.AreEqual(300, GameConfig.ScorePerfect);
            Assert.AreEqual(100, GameConfig.ScoreNormal);
            Assert.AreEqual(0, GameConfig.ScoreFail);
            Assert.AreEqual(0.90f, GameConfig.RankSRatio, Eps);
            Assert.AreEqual(0.75f, GameConfig.RankARatio, Eps);
            Assert.AreEqual(0.50f, GameConfig.RankBRatio, Eps);

            Assert.AreEqual(2, GameConfig.ComboShowMin);
            Assert.AreEqual(0, GameConfig.VolumeMin);
            Assert.AreEqual(100, GameConfig.VolumeMax);
            Assert.AreEqual(80, GameConfig.VolumeDefault);

            Assert.AreEqual(650f, GameConfig.PathLengthTarget, Eps);
            Assert.AreEqual(600f, GameConfig.PathLengthMin, Eps);
            Assert.AreEqual(700f, GameConfig.PathLengthMax, Eps);
            Assert.AreEqual(3f, GameConfig.PathWidth, Eps);
            Assert.AreEqual(4f, GameConfig.RoofHeightMin, Eps);
            Assert.AreEqual(6f, GameConfig.RoofHeightMax, Eps);
            Assert.AreEqual(3f, GameConfig.RoofGapMin, Eps);
            Assert.AreEqual(5f, GameConfig.RoofGapMax, Eps);
            Assert.AreEqual(20, GameConfig.StageObstacleCount);
            Assert.AreEqual(4, GameConfig.StageNormalEnemyCount);
        }

        [Test]
        public void T01_TemplateLeftovers_Removed()
        {
            Assert.IsFalse(System.IO.File.Exists("Assets/Readme.asset"), "Readme.asset 남아 있음");
            Assert.IsFalse(System.IO.Directory.Exists("Assets/TutorialInfo"), "TutorialInfo 남아 있음");
            Assert.IsFalse(System.IO.File.Exists("Assets/Scenes/SampleScene.unity"), "SampleScene 남아 있음");
        }
    }
}
