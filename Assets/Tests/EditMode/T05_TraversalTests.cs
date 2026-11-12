using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    public class T05_TraversalTests
    {
        static readonly ObstacleCategory[] All = (ObstacleCategory[])System.Enum.GetValues(typeof(ObstacleCategory));
        static readonly Judgement[] Results = { Judgement.Perfect, Judgement.Normal, Judgement.Fail };

        [Test]
        public void T05_SixCategories_AndChartKeysFit()
        {
            Assert.AreEqual(6, All.Length);
            var chart = StageChart.Load();
            // beta1-1: GDD 4.4 기존 장애물만 분류한다 (새 사건 8종은 GDD-beta B2.2)
            var legacy = chart.Obstacles.Where(e => EventCatalog.Get(e.kind).Category.HasValue).ToArray();
            foreach (var e in legacy)
                Assert.IsTrue(Traversal.KeyFits(Traversal.Categorize(e.kind), e.Key), $"{e.kind} {e.key}");
            // 차트에 6종이 모두 나온다
            CollectionAssert.AreEquivalent(All, legacy.Select(e => Traversal.Categorize(e.kind)).Distinct());
        }

        [Test]
        public void T05_KeysMatchGdd3()
        {
            Assert.IsTrue(Traversal.KeyFits(ObstacleCategory.Vault, ArrowKey.Up));
            Assert.IsTrue(Traversal.KeyFits(ObstacleCategory.Climb, ArrowKey.Up));
            Assert.IsTrue(Traversal.KeyFits(ObstacleCategory.Slide, ArrowKey.Down));
            Assert.IsTrue(Traversal.KeyFits(ObstacleCategory.Drop, ArrowKey.Down));
            Assert.IsTrue(Traversal.KeyFits(ObstacleCategory.Dodge, ArrowKey.Left));
            Assert.IsTrue(Traversal.KeyFits(ObstacleCategory.WallRun, ArrowKey.Right));
            Assert.IsFalse(Traversal.KeyFits(ObstacleCategory.Vault, ArrowKey.Down));
            Assert.IsFalse(Traversal.KeyFits(ObstacleCategory.Slide, ArrowKey.Up));
            Assert.IsFalse(Traversal.KeyFits(ObstacleCategory.Dodge, ArrowKey.Up));
        }

        [Test]
        public void T05_Offsets_StartAndEndOnPath()
        {
            foreach (var c in All)
                foreach (var r in Results)
                    foreach (var p in new[] { 0f, 1f })
                    {
                        Traversal.Offset(c, r, 1f, p, out var pos, out var euler);
                        Assert.Less(pos.magnitude, 1e-3f, $"{c} {r} p={p} 위치");
                        float ang = Quaternion.Angle(Quaternion.identity, Quaternion.Euler(euler));
                        Assert.Less(ang, 0.5f, $"{c} {r} p={p} 회전");
                    }
        }

        [Test]
        public void T05_ThreeResults_HaveDifferentMotion()
        {
            foreach (var c in All)
            {
                var samples = Results.Select(r => Sample(c, r)).ToArray();
                for (int a = 0; a < 3; a++)
                    for (int b = a + 1; b < 3; b++)
                        Assert.Greater(Diff(samples[a], samples[b]), 0.2f, $"{c}: {Results[a]} vs {Results[b]}");
                Assert.AreNotEqual(Traversal.StateName(c, Judgement.Perfect), Traversal.StateName(c, Judgement.Fail));
            }
            Assert.AreEqual("Vault_Perfect", Traversal.StateName(ObstacleCategory.Vault, Judgement.Perfect));
        }

        // 궤적을 여러 지점에서 모은 값
        static float[] Sample(ObstacleCategory c, Judgement r)
        {
            var list = new System.Collections.Generic.List<float>();
            for (float p = 0.1f; p < 1f; p += 0.1f)
            {
                Traversal.Offset(c, r, 1f, p, out var pos, out var euler);
                list.Add(pos.x); list.Add(pos.y); list.Add(pos.z);
                list.Add(euler.x / 30f); list.Add(euler.y / 30f); list.Add(euler.z / 30f);
            }
            return list.ToArray();
        }

        static float Diff(float[] a, float[] b)
        {
            float s = 0;
            for (int i = 0; i < a.Length; i++) s += Mathf.Abs(a[i] - b[i]);
            return s;
        }

        [Test]
        public void T05_Health_ThreeFailsKill()
        {
            var h = new PlayerHealth();
            int died = 0;
            h.Died += () => died++;
            Assert.AreEqual(3, h.Hp);
            h.Damage(); Assert.AreEqual(2, h.Hp); Assert.IsFalse(h.IsDead);
            h.Damage(); Assert.AreEqual(1, h.Hp);
            h.Damage(); Assert.AreEqual(0, h.Hp); Assert.IsTrue(h.IsDead);
            h.Damage(); Assert.AreEqual(0, h.Hp, "0 아래로 내려가지 않음");
            Assert.AreEqual(1, died);
            h.Reset(); Assert.AreEqual(3, h.Hp);
        }
    }
}
