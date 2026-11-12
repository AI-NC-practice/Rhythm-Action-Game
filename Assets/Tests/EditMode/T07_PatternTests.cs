using System.Linq;
using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    public class T07_PatternTests
    {
        [Test]
        public void T07_Surround_Phase1_FiveSequential_OneBeatApart()
        {
            var p1 = CombatPatterns.SurroundSteps().Where(s => s.Phase == 1).ToList();
            Assert.AreEqual(5, p1.Count);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(1, p1[i].Attackers.Length, "한 명씩");
                Assert.AreEqual(i, p1[i].Attackers[0]);
                Assert.AreEqual(CombatStepKind.Attack, p1[i].Kind);
                Assert.IsTrue(CombatRules.KeyFits(p1[i].Attack, p1[i].Key));
                if (i > 0) Assert.AreEqual(1.0, p1[i].OffsetBeats - p1[i - 1].OffsetBeats, 1e-9, "1박 간격");
            }
        }

        [Test]
        public void T07_Surround_Phase2_ThreeSimultaneous_OneCueEach()
        {
            var p2 = CombatPatterns.SurroundSteps().Where(s => s.Phase == 2).ToList();
            Assert.AreEqual(3, p2.Count, "동시 공격 3번 = 원 3개");
            foreach (var s in p2)
            {
                Assert.That(s.Attackers.Length, Is.InRange(2, 3), "2~3명이 같은 박에");
                Assert.IsTrue(CombatRules.KeyFits(s.Attack, s.Key));
            }
        }

        [Test]
        public void T07_Surround_Phase3_FourArrowsHalfBeatApart()
        {
            var all = CombatPatterns.SurroundSteps();
            var p3 = all.Where(s => s.Phase == 3).ToList();
            Assert.AreEqual(4, p3.Count);
            CollectionAssert.AreEqual(new[] { ArrowKey.Left, ArrowKey.Up, ArrowKey.Right, ArrowKey.Down }, p3.Select(s => s.Key));
            for (int i = 1; i < 4; i++) Assert.AreEqual(0.5, p3[i].OffsetBeats - p3[i - 1].OffsetBeats, 1e-9);
            Assert.Greater(p3[0].OffsetBeats, all.Where(s => s.Phase == 2).Max(s => s.OffsetBeats), "1 → 2 → 3단계 순서");
            Assert.Greater(all.Where(s => s.Phase == 2).Min(s => s.OffsetBeats), all.Where(s => s.Phase == 1).Max(s => s.OffsetBeats));
        }

        [Test]
        public void T07_Boss_FiveAttacks_Mixed1And2Beats_ThenFinisher()
        {
            var boss = StageChart.Load().events.Single(e => e.IsBoss);
            var steps = CombatPatterns.BossSteps(boss);
            var attacks = steps.Where(s => s.Kind == CombatStepKind.Attack).ToList();
            Assert.AreEqual(GameConfig.BossAttacks, attacks.Count);
            var gaps = Enumerable.Range(1, attacks.Count - 1).Select(i => attacks[i].OffsetBeats - attacks[i - 1].OffsetBeats).ToList();
            Assert.IsTrue(gaps.All(g => g >= 1 && g <= 2), "간격 1~2박");
            Assert.IsTrue(gaps.Contains(1) && gaps.Contains(2), "1박과 2박이 섞임");
            Assert.AreEqual(CombatStepKind.Finisher, steps.Last().Kind);
            Assert.AreEqual(ArrowKey.Up, steps.Last().Key);
        }
    }
}
