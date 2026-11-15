using System.Linq;
using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>차트 박 쪼개기 (GDD-beta B16.1).</summary>
    public class B41_ChartSplitTests
    {
        [Test]
        public void B41_RunEvents_HaveHalfBeatJudgements_GapAtLeastHalfBeat()
        {
            var c = StageChart.Load();
            var targets = ChartCheck.RunTargets(c);
            int half = targets.Count(t => System.Math.Abs(t.RunBeat % 1.0 - 0.5) < 1e-9);
            int jarsHalf = targets.Count(t => t.Event.kind == "rolling_jars" && t.RunBeat % 1.0 != 0);
            int ambushHalf = targets.Count(t => t.Event.EventKind == EventCatalog.Ambush && t.RunBeat % 1.0 != 0);
            int slipHalf = targets.Count(t => t.Event.kind == "tile_slip" && t.RunBeat % 1.0 != 0);
            UnityEngine.Debug.Log($"[B41] 달리기 판정 {targets.Count}개 중 반 박 {half}개 (술독 {jarsHalf}, 매복 {ambushHalf}, 기와 {slipHalf})");
            Assert.Greater(jarsHalf, 0, "술독 당김");
            Assert.Greater(ambushHalf, 0, "매복 따닥 (적 둘)");
            Assert.Greater(slipHalf, 0, "기와 엇박");
            Assert.GreaterOrEqual(half, 8, "반 박 판정이 곳곳에 있음 (화살 포함)");
            for (int i = 1; i < targets.Count; i++)
                Assert.GreaterOrEqual(targets[i].RunBeat - targets[i - 1].RunBeat, 0.5 - 1e-9, $"판정 간격 0.5박 이상 ({targets[i].RunBeat})");
            Assert.That(ChartCheck.TotalJudgements(c), Is.InRange(330, 440), "판정 수는 그대로");
            Assert.AreEqual(4, EventCatalog.Get("rolling_jars").Beats, "사건 길이 그대로");
            Assert.AreEqual(4, EventCatalog.Get(EventCatalog.Ambush).Beats);
            Assert.AreEqual(6, EventCatalog.Get("tile_slip").Beats);
        }

        [Test]
        public void B41_SpearDuel_FastDoubleThrust_SwordUnchanged()
        {
            // GDD-beta B18.3이 B16.1을 넓힘: 4회 연속기. 창의 첫 간격은 여전히 빠른 1.5박
            Assert.AreEqual(1.5f, GameConfig.SpearAttackIntervalBeats, 1e-6f);
            var duels = StageChart.Load().Enemies.ToList();
            Assert.IsTrue(duels.Any(e => e.kind == "spear"), "차트에 창 든 포졸 전투가 있음");
            Assert.IsTrue(duels.Any(e => e.kind == "sword"));
            foreach (var ev in duels)
            {
                var o = CombatRules.NormalEnemySteps(ev).Select(s => s.OffsetBeats).ToArray();
                if (ev.kind == "spear") CollectionAssert.AreEqual(new[] { 0.0, 1.5, 2.5, 4.0, 6.0 }, o, "창: 0·1.5·2.5·4 → 마무리 6 (B18.3)");
                else CollectionAssert.AreEqual(new[] { 0.0, 1.0, 2.0, 3.0, 5.0 }, o, "칼: 0·1·2·3 → 마무리 5 (B18.3)");
            }
        }
    }
}
