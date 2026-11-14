using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>히트사운드 박 맞춤 규칙 (GDD-beta B15.3).</summary>
    public class B38_HitSnapTests
    {
        [Test]
        public void B38_SnapDelay_EarlyPerfectWaitsForBeat_OthersImmediate()
        {
            Assert.AreEqual(0.09f, GameConfig.HitSound.SnapMaxWaitSeconds, 1e-6f, "최대 기다림 = PERFECT 창 90 ms");
            Assert.AreEqual(0.05, HitSoundRules.SnapDelay(InputOutcome.Judged, Judgement.Perfect, 0.05), 1e-9, "이른 PERFECT: 목표 박까지 기다림");
            Assert.AreEqual(0.09, HitSoundRules.SnapDelay(InputOutcome.Judged, Judgement.Perfect, 0.2), 1e-6, "최대 90 ms");
            Assert.AreEqual(0.0, HitSoundRules.SnapDelay(InputOutcome.Judged, Judgement.Perfect, -0.04), "늦은 PERFECT는 바로");
            Assert.AreEqual(0.0, HitSoundRules.SnapDelay(InputOutcome.Judged, Judgement.Perfect, 0.0), "정확히 박이면 바로");
            Assert.AreEqual(0.0, HitSoundRules.SnapDelay(InputOutcome.Judged, Judgement.Normal, 0.15), "이른 NORMAL은 바로 (기다리면 굼뜸)");
            Assert.AreEqual(0.0, HitSoundRules.SnapDelay(InputOutcome.Judged, Judgement.Fail, 0.05), "FAIL은 바로");
            Assert.AreEqual(0.0, HitSoundRules.SnapDelay(InputOutcome.NoCue, Judgement.Perfect, 0.05), "원 없음은 바로");
            Assert.AreEqual(0.0, HitSoundRules.SnapDelay(InputOutcome.TooEarly, Judgement.Perfect, 0.05), "너무 이름은 바로");
            Assert.AreEqual(0.0, HitSoundRules.SnapDelay(InputOutcome.Judged, Judgement.Perfect, double.NaN));
        }
    }
}
