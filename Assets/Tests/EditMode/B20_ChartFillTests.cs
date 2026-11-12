using System.Linq;
using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>빈 구간 채우기 (GDD-beta B3 보충, B20). 기대값은 docs/GDD-beta.md가 기준이다.</summary>
    public class B20_ChartFillTests
    {
        static ChartData C => StageChart.Load();

        [Test]
        public void B20_FirstEventAt4_PracticeGap8()
        {
            var runs = C.events.Where(e => e.IsRunEvent).OrderBy(e => e.runBeat).ToArray();
            Assert.AreEqual(4, runs[0].runBeat, "첫 사건 4박");
            Assert.AreEqual(12, runs[1].runBeat, "연습 간격 8박은 앞의 2개만");
            for (int i = 2; i < runs.Length; i++)
                if (runs[i].runBeat < 56) Assert.LessOrEqual(runs[i].runBeat, runs[i - 1].runBeat + ChartCheck.Length(runs[i - 1]), "1구간 연습 뒤로는 쉬지 않고 이어짐 (간격 0박)");
        }

        [Test]
        public void B20_NoEmptyStretches_MaxGap4Beats()
        {
            // 사건 끝 → 다음 이벤트(사건·대치·무사) 시작 간격이 4박 이하. 포위 전투 뒤(전투가 실제로 길다)와 복선은 제외.
            var evs = C.events.Where(e => !e.IsForeshadow).OrderBy(e => e.runBeat).ToArray();
            for (int i = 1; i < evs.Length; i++)
            {
                var a = evs[i - 1]; var b = evs[i];
                if (a.IsSurround) continue;
                int gap = b.runBeat - (a.runBeat + ChartCheck.Length(a));
                Assert.LessOrEqual(gap, 4, $"{a.runBeat} {a.kind} → {b.runBeat} {b.kind}: 빈 구간 {gap}박");
            }
            // 1구간 36~60박 사이 (예전엔 20박 비어 있었다)
            var mid = C.events.Where(e => e.IsRunEvent && e.runBeat >= 36 && e.runBeat < 60).ToArray();
            Assert.GreaterOrEqual(mid.Length, 4, "1구간 36~60박에 사건 4개 이상");
        }

        [Test]
        public void B20_CombatSurroundedByEvents()
        {
            var runs = C.events.Where(e => e.IsRunEvent).ToArray();
            foreach (var duel in C.events.Where(e => e.IsDuel))
            {
                Assert.IsTrue(runs.Any(r => r.runBeat + ChartCheck.Length(r) == duel.runBeat), $"{duel.runBeat} 대치 바로 앞 사건");
                Assert.IsTrue(runs.Any(r => r.runBeat - duel.runBeat > 0 && r.runBeat - duel.runBeat <= 4), $"{duel.runBeat} 대치 바로 뒤 사건");
            }
        }

        [Test]
        public void B20_Totals_And_KeptRules()
        {
            var c = C;
            Assert.That(ChartCheck.TotalJudgements(c), Is.InRange(85, 95), "전체 판정 85~95");
            Assert.That(ChartCheck.RunTargets(c).Count, Is.InRange(55, 65), "장애물·사건 판정 55~65");
            // 전투 위치 그대로
            CollectionAssert.AreEqual(new[] { 92, 128, 152, 240 }, c.events.Where(e => e.IsDuel).Select(e => e.runBeat).ToArray());
            Assert.AreEqual(184, c.events.Single(e => e.IsSurround).runBeat);
            Assert.AreEqual(260, c.events.Single(e => e.IsBoss).runBeat);
            // 높이가 바뀌는 기존 사건 위치 그대로 (지붕 높이 키프레임과 맞물림)
            foreach (var (beat, kind) in new[] { (100, "roof_climb"), (112, "roof_jump"), (120, "roof_jump"), (164, "drop_down"), (224, "roof_climb"), (248, "gate_climb") })
                Assert.IsTrue(c.events.Any(e => e.runBeat == beat && e.kind == kind), $"{beat} {kind}");
            Assert.IsTrue(c.events.Any(e => e.runBeat == 142 && e.kind == "long_jump"), "144 지붕 틈 = 긴 도약");
        }
    }
}
