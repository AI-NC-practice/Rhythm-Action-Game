using System.Linq;
using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>사건 규칙과 이동 시간 재배치 (GDD-beta B2, B4). 기대값은 docs/GDD-beta.md가 기준이다.</summary>
    public class B02_EventRulesTests
    {
        static void AssertSlots(string kind, int beats, params double[] offsets)
        {
            var d = EventCatalog.Get(kind);
            Assert.AreEqual(beats, d.Beats, kind + " 길이");
            CollectionAssert.AreEqual(offsets, d.Slots.Select(s => s.Offset).ToArray(), kind + " 판정 박자");
            Assert.That(d.Slots.Length, Is.InRange(1, 4));
            for (int i = 1; i < d.Slots.Length; i++)
                Assert.GreaterOrEqual(d.Slots[i].Offset - d.Slots[i - 1].Offset, 0.5, kind + " 판정 간격 0.5박 이상");
            foreach (var s in d.Slots) Assert.That(s.Offset, Is.InRange(0.0, (double)beats), kind + " 판정이 사건 안");
        }

        [Test]
        public void B02_LegacyObstacles_4Beats_OneJudgeAtStart()
        {
            foreach (var k in new[] { "cart", "low_wall", "roof_climb", "roof_jump", "gate_climb", "clothesline", "eave", "drop_down", "jars", "wall_run" })
            {
                AssertSlots(k, 4, 0.0);
                Assert.AreEqual(Traversal.Categorize(k), EventCatalog.Get(k).Category);
                Assert.IsFalse(EventCatalog.Get(k).HpCapOne);
                Assert.AreEqual(-1, EventCatalog.Get(k).SlowMoSlot);
            }
        }

        [Test]
        public void B02_NewEvents_MatchGddBetaB22()
        {
            AssertSlots("tile_slip", 6, 2, 4);
            AssertSlots("rope_swing", 4, 2);
            AssertSlots("rolling_jars", 4, 1, 2, 3);
            AssertSlots("awning_bounce", 4, 2);
            AssertSlots("closing_gate", 4, 2);
            AssertSlots("arrow_volley", 4, 1.5, 2, 2.5, 3);
            AssertSlots("long_jump", 6, 2, 4);
            AssertSlots("banner_slide", 4, 2);
            Assert.AreEqual(ArrowKey.Down, EventCatalog.Get("tile_slip").Slots[0].Key);
            Assert.AreEqual(ArrowKey.Up, EventCatalog.Get("tile_slip").Slots[1].Key);
            Assert.AreEqual(ArrowKey.Up, EventCatalog.Get("long_jump").Slots[0].Key);
            Assert.AreEqual(ArrowKey.Down, EventCatalog.Get("long_jump").Slots[1].Key);
            Assert.IsNull(EventCatalog.Get("rolling_jars").Slots[0].Key, "키는 차트에서");
            Assert.IsNull(EventCatalog.Get("arrow_volley").Slots[0].Key);
        }

        [Test]
        public void B02_HpCapOne_And_SlowMoCandidates_MatchGddBeta()
        {
            var capped = EventCatalog.All.Where(d => d.HpCapOne).Select(d => d.Kind).OrderBy(k => k).ToArray();
            CollectionAssert.AreEqual(new[] { "arrow_volley", "rolling_jars" }, capped, "B2.1 체력 최대 1");
            Assert.AreEqual(1, EventCatalog.Get("tile_slip").SlowMoSlot, "두 번째 판정");
            Assert.AreEqual(0, EventCatalog.Get("rope_swing").SlowMoSlot);
            Assert.AreEqual(0, EventCatalog.Get("long_jump").SlowMoSlot, "첫 판정");
            Assert.AreEqual(0, EventCatalog.Get("closing_gate").SlowMoSlot);
            Assert.AreEqual(0, EventCatalog.Get("charge").SlowMoSlot, "돌진 적");
            var slow = EventCatalog.All.Where(d => d.SlowMoSlot >= 0).Select(d => d.Kind).OrderBy(k => k).ToArray();
            CollectionAssert.AreEqual(new[] { "charge", "closing_gate", "long_jump", "rope_swing", "tile_slip" }, slow);
        }

        [Test]
        public void B02_Enemies_ApproachKinds()
        {
            AssertSlots(EventCatalog.Charge, 4, 2);
            Assert.AreEqual(ArrowKey.Up, EventCatalog.Get(EventCatalog.Charge).Slots[0].Key);
            AssertSlots(EventCatalog.Ambush, 4, 2, 3);
            AssertSlots(EventCatalog.Chase, 4, 2);
            foreach (var k in new[] { EventCatalog.Charge, EventCatalog.Ambush, EventCatalog.Chase }) Assert.IsTrue(EventCatalog.Get(k).IsEnemy);

            var duel = new ChartEvent { type = "enemy", kind = "sword" };
            var duel2 = new ChartEvent { type = "enemy", kind = "sword", approach = "duel" };
            var charge = new ChartEvent { type = "enemy", kind = "sword", approach = "charge" };
            var obs = new ChartEvent { type = "obstacle", kind = "cart" };
            Assert.IsTrue(duel.IsDuel); Assert.IsFalse(duel.IsRunEvent);
            Assert.IsTrue(duel2.IsDuel);
            Assert.IsFalse(charge.IsDuel); Assert.IsTrue(charge.IsRunEvent); Assert.AreEqual("charge", charge.EventKind);
            Assert.IsTrue(obs.IsRunEvent); Assert.AreEqual("cart", obs.EventKind);
        }

        [Test]
        public void B02_SlotsFor_KeysFromChart()
        {
            var jars = new ChartEvent { type = "obstacle", kind = "rolling_jars", keys = new[] { "Left", "Right", "Left" } };
            CollectionAssert.AreEqual(new ArrowKey?[] { ArrowKey.Left, ArrowKey.Right, ArrowKey.Left }, EventCatalog.SlotsFor(jars).Select(s => s.Key).ToArray());
            var ambush1 = new ChartEvent { type = "enemy", approach = "ambush", keys = new[] { "Right" } };
            Assert.AreEqual(1, EventCatalog.SlotsFor(ambush1).Count, "매복 판정 1개");
            var ambush2 = new ChartEvent { type = "enemy", approach = "ambush", keys = new[] { "Left", "Right" } };
            Assert.AreEqual(2, EventCatalog.SlotsFor(ambush2).Count, "매복 판정 2개");
            var cart = new ChartEvent { type = "obstacle", kind = "cart", key = "Up" };
            Assert.AreEqual(ArrowKey.Up, EventCatalog.SlotsFor(cart)[0].Key);
            var slip = new ChartEvent { type = "obstacle", kind = "tile_slip", key = "Left" };
            Assert.AreEqual(ArrowKey.Down, EventCatalog.SlotsFor(slip)[0].Key, "정해진 키가 우선");
        }

        [Test]
        public void B02_TimeRemap_SlowsThenCatchesUp_ExactAtEnd()
        {
            var r = new TimeRemap(10, 14, 1f);         // 속도 1.0: 1초 = 2박
            Assert.AreEqual(0.8, r.SlowBeats, 1e-6, "0.4초 = 0.8박");
            Assert.AreEqual(0.3, r.SpeedFactor(0.4), 1e-6, "가운데 0.3배");
            Assert.AreEqual(1.0, r.SpeedFactor(0.0), 1e-9);
            Assert.AreEqual(0.65, r.SpeedFactor(0.08), 1e-6, "앞 경사 중간 (0.08초 = 0.16박의 절반)");
            Assert.AreEqual(0.7 * (0.8 - 0.16), r.MaxLag, 1e-6, "쌓인 지연");
            Assert.AreEqual(0, r.Lag(10), 1e-12);
            Assert.AreEqual(0, r.Lag(14), 1e-12, "사건 끝에서 지연 0");
            Assert.AreEqual(14, r.Visual(14), 1e-12);
            Assert.Less(r.Visual(10.5), 10.5);

            double prev = r.Visual(10);
            for (double b = 10; b <= 14; b += 0.01)
            {
                double v = r.Visual(b);
                Assert.GreaterOrEqual(v, prev - 1e-9, "보이는 위치는 뒤로 가지 않음");
                Assert.LessOrEqual(System.Math.Abs(v - prev), 0.02, "끊김 없음");
                prev = v;
            }

            var fast = new TimeRemap(0, 4, 1.3f);
            Assert.AreEqual(0.4 * 1.3 / 0.5, fast.SlowBeats, 1e-6, "속도 1.3: 0.4초 = 1.04박");
            Assert.AreEqual(0, fast.Lag(4), 1e-12);
        }
    }
}
