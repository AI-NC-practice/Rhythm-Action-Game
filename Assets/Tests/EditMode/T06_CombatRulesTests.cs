using System.Linq;
using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    public class T06_CombatRulesTests
    {
        [Test]
        public void T06_NextBar()
        {
            Assert.AreEqual(4.0, CombatRules.NextBar(0.0));
            Assert.AreEqual(8.0, CombatRules.NextBar(4.0), "마디 첫 박이면 다음 마디");
            Assert.AreEqual(8.0, CombatRules.NextBar(4.01));
            Assert.AreEqual(8.0, CombatRules.NextBar(7.99));
            Assert.AreEqual(12.0, CombatRules.FirstTargetBeat(9.5) - 2.0, "다음 마디 첫 박에 원이 나타남");
        }

        [Test]
        public void T06_NormalEnemy_TwoAttacksTwoBeatsApart_ThenFinisherUp()
        {
            foreach (var ev in StageChart.Load().Enemies)
            {
                var steps = CombatRules.NormalEnemySteps(ev);
                Assert.AreEqual(3, steps.Count);
                Assert.AreEqual(CombatStepKind.Attack, steps[0].Kind);
                Assert.AreEqual(CombatStepKind.Attack, steps[1].Kind);
                Assert.AreEqual(CombatStepKind.Finisher, steps[2].Kind);
                Assert.AreEqual(0.0, steps[0].OffsetBeats);
                Assert.AreEqual(2.0, steps[1].OffsetBeats);
                Assert.AreEqual(4.0, steps[2].OffsetBeats);
                Assert.AreEqual(ArrowKey.Up, steps[2].Key, "마무리 ↑");
            }
        }

        [Test]
        public void T06_ChartAttackKeys_MatchGdd45()
        {
            Assert.IsTrue(CombatRules.KeyFits("horizontal", ArrowKey.Down));
            Assert.IsFalse(CombatRules.KeyFits("horizontal", ArrowKey.Up));
            Assert.IsTrue(CombatRules.KeyFits("overhead", ArrowKey.Left));
            Assert.IsTrue(CombatRules.KeyFits("thrust", ArrowKey.Right));
            Assert.IsFalse(CombatRules.KeyFits("thrust", ArrowKey.Down));
            Assert.IsTrue(CombatRules.KeyFits("upward", ArrowKey.Up));
            var chart = StageChart.Load();
            foreach (var e in chart.events.Where(e => e.attacks != null))
                foreach (var a in e.attacks)
                    Assert.IsTrue(CombatRules.KeyFits(a.attack, a.Key), $"{e.runBeat} {a.attack} {a.key}");
        }

        [Test]
        public void T06_Execution_ByPlaceAndResult()
        {
            Assert.AreEqual(ExecutionKind.KickOffRoof, CombatRules.ExecutionFor(Judgement.Perfect, true, 0));
            Assert.AreEqual(ExecutionKind.KickOffRoof, CombatRules.ExecutionFor(Judgement.Perfect, true, 1));
            Assert.AreEqual(ExecutionKind.DisarmSlash, CombatRules.ExecutionFor(Judgement.Perfect, false, 0));
            Assert.AreEqual(ExecutionKind.StabWithOwnSword, CombatRules.ExecutionFor(Judgement.Perfect, false, 1));
            Assert.AreEqual(ExecutionKind.PushDown, CombatRules.ExecutionFor(Judgement.Normal, true, 0));
            Assert.AreEqual(ExecutionKind.PushDown, CombatRules.ExecutionFor(Judgement.Normal, false, 0));
            Assert.AreEqual(ExecutionKind.None, CombatRules.ExecutionFor(Judgement.Fail, false, 0));
        }
    }
}
