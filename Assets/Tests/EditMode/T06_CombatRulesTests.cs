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
                // GDD-beta B18.3: 공격 4회 연속기 + 마무리 ↑. 칼 0·1·2·3 → 5, 창 0·1.5·2.5·4 → 6
                Assert.AreEqual(5, steps.Count);
                for (int k = 0; k < 4; k++) Assert.AreEqual(CombatStepKind.Attack, steps[k].Kind);
                Assert.AreEqual(CombatStepKind.Finisher, steps[4].Kind);
                bool spear = ev.kind == "spear";
                var expect = spear ? new[] { 0.0, 1.5, 2.5, 4.0, 6.0 } : new[] { 0.0, 1.0, 2.0, 3.0, 5.0 };
                for (int k = 0; k < 5; k++) Assert.AreEqual(expect[k], steps[k].OffsetBeats, 1e-9, ev.kind + " " + k);
                Assert.AreEqual(ArrowKey.Up, steps[4].Key, "마무리 ↑");
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
