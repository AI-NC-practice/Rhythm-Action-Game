using System.Linq;
using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>무사 결투 3인칭 컷 시간 규칙 (GDD-beta B11.5). 순수 함수 검사.</summary>
    public class B28_DuelCutRuleTests
    {
        [Test]
        public void B28_Config_Cuts_Radius35To5_Sweeps()
        {
            // beta1-1: GDD-beta B13.4가 B11.5의 컷 각도를 바꿨다 — A 로우 앵글, B 칼날 클로즈업(1.2 m), C는 옆차기 다운으로, K 옆차기·S 자리 바꾸기 추가
            Assert.AreEqual(0.4f, GameConfig.DuelCutBlendSeconds, "1인칭 ↔ 3인칭 0.4초");
            Assert.AreEqual(0.4f, GameConfig.DuelCutReturnMarginSeconds, "원 0.4초 전 1인칭");
            CollectionAssert.AreEqual(new[] { "A", "B", "D", "K", "S" }, GameConfig.DuelCuts.Select(c => c.name));
            var A = GameConfig.DuelCuts[0]; var B = GameConfig.DuelCuts[1]; var D = GameConfig.DuelCuts[2]; var K = GameConfig.DuelCuts[3];
            foreach (var c in new[] { A, D, K })
            {
                Assert.That(c.r0, Is.InRange(3.5f, 5f), c.name);
                Assert.That(c.r1, Is.InRange(3.5f, 5f), c.name);
            }
            Assert.Less(A.height, A.look, "A 로우 앵글: 카메라가 바라보는 점보다 낮음");
            Assert.LessOrEqual(A.height, 0.5f);
            Assert.AreEqual(1.2f, B.r0, 1e-5f, "B 칼날 옆 1.2 m");
            Assert.AreEqual(B.look, B.height, 1e-5f, "B 칼 높이에서 수평으로");
            Assert.AreEqual(180f, D.sweep, "D 180°");
            Assert.Less(D.height, D.look, "D 낮은 각도");
            Assert.AreEqual(0.5f, GameConfig.DuelTopDownSeconds, "D 마지막 0.5초 위에서");
        }

        [Test]
        public void B28_ActiveCut_Windows_EndBeforeRingsWithMargin()
        {
            // 속도 1.0: 돌아오는 여유 0.8초 = 1.6박
            Assert.AreEqual(1.6, BossDuel.ReturnLeadBeats(1f), 1e-6);
            Assert.AreEqual(2.08, BossDuel.ReturnLeadBeats(1.3f), 1e-4, "빠르면 더 많은 박");
            Assert.AreEqual("A", BossDuel.ActiveCut(0.0, 1f, false, out _));
            Assert.AreEqual("A", BossDuel.ActiveCut(2.3, 1f, false, out var pa));
            Assert.Greater(pa, 0.9f);
            Assert.IsNull(BossDuel.ActiveCut(2.4, 1f, false, out _), "4 − 1.6 = 2.4박부터 1인칭으로 돌아감");
            Assert.IsNull(BossDuel.ActiveCut(-0.5, 1f, false, out _), "O 전(대치)은 1인칭");
            Assert.AreEqual("B", BossDuel.ActiveCut(24.1, 1f, false, out _));
            Assert.AreEqual("B", BossDuel.ActiveCut(26.3, 1f, false, out _), "B 24–28 (칼 겨루기 연장, 사용자 결정)");
            Assert.IsNull(BossDuel.ActiveCut(26.4, 1f, false, out _), "28 − 1.6 = 26.4박부터 1인칭으로");
            Assert.IsNull(BossDuel.ActiveCut(55.0, 1f, false, out _), "beta1-1: 컷 C(54–58)는 옆차기 다운 구간(B13.4)으로 바뀜");
            Assert.IsNull(BossDuel.ActiveCut(10, 1f, false, out _), "판정 구간은 1인칭");
            Assert.IsNull(BossDuel.ActiveCut(68, 1f, false, out _));
            // 속도 1.3에서도 컷 B가 나온다: 4박(1.54초) − 돌아오는 여유 2.08박 = 1.92박
            Assert.AreEqual("B", BossDuel.ActiveCut(25.5, 1.3f, false, out _), "속도 1.3에서도 컷 B");
            Assert.IsNull(BossDuel.ActiveCut(25.93, 1.3f, false, out _));
            // D: 마무리가 성공한 뒤에만, 74박까지
            Assert.IsNull(BossDuel.ActiveCut(71, 1f, false, out _), "마무리 전에는 D 없음");
            Assert.AreEqual("D", BossDuel.ActiveCut(69.8, 1f, true, out var pd0), "조금 이르게 친 마무리도 바로 D");
            Assert.AreEqual(0f, pd0);
            Assert.AreEqual("D", BossDuel.ActiveCut(72, 1f, true, out var pd));
            Assert.AreEqual(0.5f, pd, 1e-6f);
            Assert.IsNull(BossDuel.ActiveCut(74, 1f, true, out _));
        }

        [Test]
        public void B28_NoCutOverlapsAnyRing_AtAnySpeed()
        {
            // 컷이 활성인 박 + 돌아오는 블렌드 동안에는 어떤 결투 원도 떠 있지 않다
            foreach (var speed in new[] { 1f, 1.15f, 1.3f })
            {
                double lead = BossDuel.ReturnLeadBeats(speed);
                double margin = GameConfig.DuelCutReturnMarginSeconds * speed / GameConfig.SecondsPerBeat;
                for (double b = -1; b < 70; b += 0.01)
                {
                    if (BossDuel.ActiveCut(b, speed, false, out _) == null) continue;
                    foreach (var s in BossDuel.Steps())
                    {
                        double appear = s.OffsetBeats - GameConfig.ApproachBeats;
                        double gone = s.OffsetBeats + GameConfig.NormalWindowMs / 1000.0 * speed / GameConfig.SecondsPerBeat;
                        Assert.IsFalse(b >= appear - (lead - margin) - margin && b < gone, $"속도 {speed} {b:0.00}박 컷 중 {s.OffsetBeats} 원");
                    }
                }
            }
        }
    }
}
