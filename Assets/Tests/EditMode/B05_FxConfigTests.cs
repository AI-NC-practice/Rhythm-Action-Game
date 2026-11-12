using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>연출 수치 (GDD-beta B4·B5, 제안서 2~4장). 기대값은 제안서 표가 기준이다.</summary>
    public class B05_FxConfigTests
    {
        const float Eps = 1e-5f;

        [Test]
        public void B05_Rules_MatchProposalSection4()
        {
            Assert.AreEqual(10f, GameConfig.Fx.RollLimit, Eps);
            Assert.AreEqual(35f, GameConfig.Fx.PitchLimit, Eps);
            Assert.AreEqual(45f, GameConfig.Fx.FovMin, Eps);
            Assert.AreEqual(85f, GameConfig.Fx.FovMax, Eps);
            Assert.AreEqual(1f, GameConfig.Fx.TraumaMax, Eps);
            Assert.AreEqual(1.5f, GameConfig.Fx.TraumaDecayPerSecond, Eps);
            Assert.AreEqual(0.08f, GameConfig.Fx.ShakeMaxPosition, Eps);
            Assert.AreEqual(3f, GameConfig.Fx.ShakeMaxRotation, Eps);
            Assert.AreEqual(0.5f, GameConfig.Fx.RecoverSeconds, Eps);
            Assert.AreEqual(16f, GameConfig.Fx.SlowMoMinGapBeats, Eps);
            Assert.AreEqual(0.08f, GameConfig.SlowMotionRampSeconds, Eps);
        }

        [Test]
        public void B05_Presets_MatchProposalSection2()
        {
            var p = GameConfig.Fx.ParryPerfect;
            Assert.AreEqual(0.07f, p.HitStop, Eps); Assert.AreEqual(0.35f, p.Trauma, Eps); Assert.AreEqual(25f, p.ShakeHz, Eps);
            Assert.AreEqual(0.05f, p.KickBack, Eps); Assert.AreEqual(-1.5f, p.Pitch, Eps); Assert.AreEqual(-3f, p.Fov, Eps); Assert.AreEqual(3, p.Grade);
            var d = GameConfig.Fx.DodgePerfect;
            Assert.AreEqual(0.05f, d.HitStop, Eps); Assert.AreEqual(0.1f, d.SideShift, Eps); Assert.AreEqual(4f, d.Roll, Eps);
            var n = GameConfig.Fx.DefendNormal;
            Assert.AreEqual(0.03f, n.HitStop, Eps); Assert.AreEqual(0.12f, n.KickBack, Eps); Assert.AreEqual(2f, n.YawWobble, Eps); Assert.AreEqual(12f, n.ShakeHz, Eps);
            Assert.AreEqual(0.2f, n.Trauma, Eps); Assert.AreEqual(-0.08f, n.Height, Eps);
            var h = GameConfig.Fx.Hit;
            Assert.AreEqual(0.05f, h.HitStop, Eps); Assert.AreEqual(0.6f, h.Trauma, Eps); Assert.AreEqual(6f, h.Roll, Eps); Assert.AreEqual(4f, h.Pitch, Eps); Assert.AreEqual(4f, h.Fov, Eps);
            var e = GameConfig.Fx.Execution;
            Assert.AreEqual(0.1f, e.HitStop, Eps); Assert.AreEqual(SlowMoMode.Always, e.SlowMo); Assert.AreEqual(-0.3f, e.KickBack, Eps); Assert.AreEqual(-8f, e.Fov, Eps); Assert.AreEqual(8f, e.Roll, Eps); Assert.AreEqual(4, e.Grade);
            Assert.AreEqual(10f, GameConfig.Fx.ExecutionKick.Pitch, Eps);
            Assert.AreEqual(3f, GameConfig.Fx.SurroundTurn.Roll, Eps);
            Assert.AreEqual(6f, GameConfig.Fx.Simultaneous.Fov, Eps); Assert.AreEqual(-8f, GameConfig.Fx.Simultaneous.Pitch, Eps);
            Assert.AreEqual(SlowMoMode.Candidate, GameConfig.Fx.SimultaneousPerfect.SlowMo);
            Assert.AreEqual(2f, GameConfig.Fx.WindmillArrow.Fov, Eps);
            Assert.AreEqual(0.08f, GameConfig.Fx.WindmillFinish.HitStop, Eps); Assert.AreEqual(SlowMoMode.Always, GameConfig.Fx.WindmillFinish.SlowMo); Assert.AreEqual(5, GameConfig.Fx.WindmillFinish.Grade);
            Assert.AreEqual(SlowMoMode.Candidate, GameConfig.Fx.ParryPerfectFinale.SlowMo);
            Assert.AreEqual(SlowMoMode.Candidate, GameConfig.Fx.EventPerfectSlow.SlowMo);
            Assert.AreEqual(0.1f, GameConfig.Fx.LandPerfect.Trauma, Eps); Assert.AreEqual(0.25f, GameConfig.Fx.LandNormal.Trauma, Eps); Assert.AreEqual(0.5f, GameConfig.Fx.LandFail.Trauma, Eps);
        }

        [Test]
        public void B05_AllPresets_ReleaseWithin05s_AndEnvelope()
        {
            foreach (var p in GameConfig.Fx.All)
            {
                Assert.LessOrEqual(p.Release, GameConfig.Fx.RecoverSeconds + Eps, p.Name + " 복귀 0.5초 이하");
                Assert.LessOrEqual(p.Trauma / GameConfig.Fx.TraumaDecayPerSecond, p.Total + GameConfig.Fx.RecoverSeconds, p.Name + " 흔들림도 끝나고 0.5초 안에 0");
                Assert.AreEqual(0f, CameraFx.Envelope(p, p.Total), 1e-4f, p.Name);
                Assert.AreEqual(1f, CameraFx.Envelope(p, p.Rise + p.Hold * 0.5f), 1e-4f, p.Name);
                Assert.That(p.Grade, Is.InRange(0, 5));
            }
            var a = GameConfig.Fx.ParryPerfect;
            Assert.AreEqual(0.5f, CameraFx.Envelope(a, a.Rise * 0.5f), 1e-4f, "올라가는 중");
            Assert.AreEqual(0.5f, CameraFx.Envelope(a, a.Rise + a.Hold + a.Release * 0.5f), 1e-4f, "내려오는 중");
        }
    }
}
