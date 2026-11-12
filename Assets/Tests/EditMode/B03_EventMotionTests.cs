using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>새 사건 8종·적 등장 방식의 결과별 카메라 동작과 회색 상자 소품 (GDD-beta B2.2, B2.3, 제안서 0.1.1).</summary>
    public class B03_EventMotionTests
    {
        static readonly string[] NewKinds = { "tile_slip", "rope_swing", "rolling_jars", "awning_bounce", "closing_gate", "arrow_volley", "long_jump", "banner_slide" };
        static readonly string[] EnemyKinds = { EventCatalog.Charge, EventCatalog.Ambush, EventCatalog.Chase };

        static Vector3[] Curve(string kind, Judgement r)
        {
            var pts = new Vector3[81];
            for (int i = 0; i <= 80; i++)
            {
                EventMotion.OffsetUniform(kind, r, i / 80f, out var pos, out var euler);
                pts[i] = pos + euler * 0.02f;
            }
            return pts;
        }

        static float Diff(Vector3[] a, Vector3[] b) => a.Zip(b, (x, y) => Vector3.Distance(x, y)).Max();

        [Test]
        public void B03_EachKind_PerfectNormalFail_DifferentMotion([ValueSource(nameof(AllKinds))] string kind)
        {
            var p = Curve(kind, Judgement.Perfect);
            var n = Curve(kind, Judgement.Normal);
            var f = Curve(kind, Judgement.Fail);
            Assert.Greater(p.Max(v => v.magnitude), 0.1f, kind + " PERFECT 동작 있음");
            Assert.Greater(Diff(p, n), 0.1f, kind + " PERFECT/NORMAL 다름");
            Assert.Greater(Diff(p, f), 0.1f, kind + " PERFECT/FAIL 다름");
            Assert.Greater(Diff(n, f), 0.1f, kind + " NORMAL/FAIL 다름");
            Assert.Less(p[80].magnitude, 1e-3f, kind + " 사건 끝에서 0");
            Assert.Less(f[80].magnitude, 1e-3f);
            // 첫 판정 전에는 움직이지 않음
            float first = (float)(EventCatalog.Get(kind).Slots[0].Offset / EventCatalog.Get(kind).Beats);
            EventMotion.OffsetUniform(kind, Judgement.Perfect, first * 0.5f, out var pre, out var preE);
            Assert.Less(pre.magnitude + preE.magnitude, 1e-4f, kind + " 판정 전 0");
        }

        public static string[] AllKinds => NewKinds.Concat(EnemyKinds).ToArray();

        [Test]
        public void B03_Motion_WaitsForResult()
        {
            var offs = new[] { 2.0, 4.0 };
            var keys = new[] { ArrowKey.Down, ArrowKey.Up };
            EventMotion.Offset("tile_slip", offs, keys, new Judgement?[] { null, null }, 6, 0.5f, out var a, out var ae);
            Assert.AreEqual(Vector3.zero, a, "결과 전 0");
            EventMotion.Offset("tile_slip", offs, keys, new Judgement?[] { Judgement.Perfect, null }, 6, 0.5f, out var b, out _);
            Assert.AreNotEqual(Vector3.zero, b, "첫 판정 결과로 움직임");
        }

        /// <summary>사건 (장애물 계열). approach가 있으면 함께 나오는 적.</summary>
        static ChartEvent Ev(string kind, string approach = null, params string[] keys) =>
            new ChartEvent { runBeat = 20, type = "obstacle", kind = kind, approach = approach, keys = keys.Length > 0 ? keys : null, key = "Up" };

        /// <summary>안 멈추는 적.</summary>
        static ChartEvent Foe(string how, params string[] keys) =>
            new ChartEvent { runBeat = 20, type = "enemy", kind = "sword", approach = how, keys = keys.Length > 0 ? keys : null, key = "Left" };

        [Test]
        public void B03_EventProps_BuildGrayBoxParts()
        {
            var root = new GameObject("PropsTest").transform;
            try
            {
                void Has(EventProp p, params string[] names)
                {
                    foreach (var n in names) Assert.IsNotNull(p.transform.Find(n), $"{p.kind}: {n}");
                }
                Has(EventProps.Build(Ev("tile_slip"), root), "Prop_tile_0", "Prop_tile_2", "Prop_eave_edge");
                Has(EventProps.Build(Ev("rope_swing"), root), "Prop_rope", "Prop_rope_PostL", "Prop_rope_PostR");
                var jars = EventProps.Build(Ev("rolling_jars", null, "Left", "Right", "Left"), root);
                Has(jars, "Prop_jar_0", "Prop_jar_1", "Prop_jar_2");
                Assert.AreEqual(3, jars.parts.Length);
                Has(EventProps.Build(Ev("awning_bounce"), root), "Prop_awning");
                Has(EventProps.Build(Ev("closing_gate"), root), "Prop_gate_door_L", "Prop_gate_door_R");
                var arrows = EventProps.Build(Ev("arrow_volley", null, "Left", "Right", "Left", "Right"), root);
                Has(arrows, "Prop_arrow_0", "Prop_arrow_3");
                Has(EventProps.Build(Ev("long_jump"), root), "Prop_gap_edge_near", "Prop_gap_edge_far");
                Has(EventProps.Build(Ev("banner_slide"), root), "Prop_banner_line", "Prop_banner");

                var combo = EventProps.Build(Ev("rope_swing", "charge"), root);
                Assert.IsNotNull(combo.enemy, "조합: 밧줄 넘기 + 돌진 포졸");
                Assert.AreEqual("charge", combo.companion);
                foreach (var how in EnemyKinds)
                {
                    var e = EventProps.Build(Foe(how, "Left", "Right"), root);
                    Assert.IsNotNull(e.enemy, how + " 적");
                    Assert.AreEqual(how, e.kind);
                }
                var fs = EventProps.Build(new ChartEvent { runBeat = 30, type = "foreshadow", span = 8 }, root);
                Has(fs, "Prop_foreshadow_0");
                Assert.IsFalse(fs.gameObject.activeSelf, "복선은 처음엔 숨김");

                Assert.IsTrue(EventProps.Needs(Ev("tile_slip")));
                Assert.IsFalse(EventProps.Needs(new ChartEvent { type = "obstacle", kind = "cart" }), "기존 장애물은 SceneBuilder.BuildObstacles");
                Assert.IsFalse(EventProps.Needs(new ChartEvent { type = "enemy", kind = "sword" }), "대치는 적 배치에서");
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }
    }
}
