using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>판정 창 완화 (GDD-beta B8.4, B12.1): PERFECT ±90 ms, NORMAL ±240 ms (B12.1에서 180 → 240), −300 ms보다 이르면 무시.</summary>
    public class B16_JudgeWindowTests
    {
        const double T = 20.0;

        static bool J(double ms, out Judgement r, ArrowKey pressed = ArrowKey.Up) =>
            JudgeRules.TryJudge(T + ms / 1000.0, T, 0f, pressed, ArrowKey.Up, out r);

        [Test]
        public void B16_Config_MatchGddBetaB84()
        {
            Assert.AreEqual(90f, GameConfig.PerfectWindowMs);
            Assert.AreEqual(240f, GameConfig.NormalWindowMs, "beta1-1: GDD-beta B12.1");
            Assert.AreEqual(300f, GameConfig.EarlyIgnoreMs, "무시 기준은 그대로");
        }

        [TestCase(-90.0, Judgement.Perfect)]
        [TestCase(89.9, Judgement.Perfect)]
        [TestCase(-89.9, Judgement.Perfect)]
        [TestCase(90.1, Judgement.Normal)]
        [TestCase(-90.1, Judgement.Normal)]
        [TestCase(239.9, Judgement.Normal)]
        [TestCase(-240.0, Judgement.Normal)]
        [TestCase(-240.1, Judgement.Fail)]
        [TestCase(-299.9, Judgement.Fail)]
        public void B16_Boundaries(double ms, Judgement expected)
        {
            Assert.IsTrue(J(ms, out var r), ms + " ms 판정 대상");
            Assert.AreEqual(expected, r, ms + " ms");
        }

        [Test]
        public void B16_TooEarlyIgnored_LateMissedAfter240()
        {
            Assert.IsFalse(J(-300.1, out _), "−300 ms보다 이르면 무시");
            Assert.IsFalse(J(240.1, out _), "+240 ms 넘으면 판정 대상 아님 (무입력 FAIL)");
            Assert.IsFalse(JudgeRules.IsMissed(T + 0.2399, T, 0f));
            Assert.IsTrue(JudgeRules.IsMissed(T + 0.2401, T, 0f), "+240 ms까지 입력 없으면 FAIL");
            Assert.IsTrue(J(50, out var wrong, ArrowKey.Down));
            Assert.AreEqual(Judgement.Fail, wrong, "다른 키는 FAIL");
        }
    }
}
