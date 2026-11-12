using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    public class T04_ChartAndLayoutTests
    {
        [Test]
        public void T04_BuildSettings_HasTitleAndGame()
        {
            var scenes = UnityEditor.EditorBuildSettings.scenes;
            Assert.AreEqual(2, scenes.Length);
            Assert.AreEqual("Assets/Scenes/Title.unity", scenes[0].path);
            Assert.AreEqual("Assets/Scenes/Game.unity", scenes[1].path);
            Assert.IsTrue(scenes.All(s => s.enabled));
            Assert.IsTrue(System.IO.File.Exists(scenes[0].path) && System.IO.File.Exists(scenes[1].path));
        }

        [Test]
        public void T04_Chart_Counts_MatchGdd49()
        {
            var c = StageChart.Load();
            Assert.AreEqual(GameConfig.StageObstacleCount, c.Obstacles.Count(), "장애물 20개");
            Assert.AreEqual(GameConfig.StageNormalEnemyCount, c.Enemies.Count(), "일반 적 4명");
            Assert.AreEqual(1, c.events.Count(e => e.IsSurround), "포위 전투 1회");
            Assert.AreEqual(1, c.events.Count(e => e.IsBoss), "무사 1명");
            Assert.AreEqual(2, c.Enemies.Count(e => e.kind == "sword"));
            Assert.AreEqual(2, c.Enemies.Count(e => e.kind == "spear"));
        }

        [Test]
        public void T04_Chart_EachEventHasBeatKindAndKey()
        {
            var c = StageChart.Load();
            foreach (var e in c.events)
            {
                Assert.GreaterOrEqual(e.runBeat, 0);
                Assert.IsFalse(string.IsNullOrEmpty(e.kind), $"{e.runBeat} 종류 없음");
                if (e.IsObstacle) Assert.DoesNotThrow(() => StageChart.ParseKey(e.key), $"{e.runBeat} 키 오류");
                if (e.IsEnemy)
                {
                    Assert.AreEqual(GameConfig.NormalEnemyAttacks, e.attacks.Length, $"{e.runBeat} 일반 적 공격 2회");
                    foreach (var a in e.attacks) Assert.DoesNotThrow(() => StageChart.ParseKey(a.key));
                }
                if (e.IsBoss) Assert.AreEqual(GameConfig.BossAttacks, e.attacks.Length, "무사 공격 5회");
            }
        }

        [Test]
        public void T04_Chart_ObstacleGapAtLeast4Beats()
        {
            var c = StageChart.Load();
            Assert.GreaterOrEqual(StageChart.MinObstacleGap(c), GameConfig.MinObstacleGapBeats);
        }

        [Test]
        public void T04_Chart_ObstacleKeysMatchGdd3()
        {
            var c = StageChart.Load();
            foreach (var e in c.Obstacles)
            {
                switch (e.kind)
                {
                    case "cart": case "low_wall": case "roof_climb": case "roof_jump": case "gate_climb":
                        Assert.AreEqual(ArrowKey.Up, e.Key, e.kind); break;
                    case "clothesline": case "eave": case "drop_down":
                        Assert.AreEqual(ArrowKey.Down, e.Key, e.kind); break;
                    case "jars": case "wall_run":
                        Assert.That(e.Key == ArrowKey.Left || e.Key == ArrowKey.Right, e.kind); break;
                    default: Assert.Fail("알 수 없는 장애물: " + e.kind); break;
                }
            }
        }

        [Test]
        public void T04_Chart_EventsDoNotOverlap()
        {
            var c = StageChart.Load();
            var evs = c.events.OrderBy(e => e.runBeat).ToArray();
            for (int i = 1; i < evs.Length; i++)
                Assert.GreaterOrEqual(evs[i].runBeat - evs[i - 1].runBeat, GameConfig.ObstacleBeats, $"{evs[i - 1].runBeat}와 {evs[i].runBeat} 겹침");
        }

        [Test]
        public void T04_Path_Length600To700_AndMatchesChart()
        {
            float len = MapLayout.TotalLength;
            Assert.That(len, Is.InRange(GameConfig.PathLengthMin, GameConfig.PathLengthMax));
            Assert.AreEqual(StageChart.Load().PathLength, len, 1e-3f, "차트 달리기 박자 × 2.5 m = 경로 길이");
            Assert.LessOrEqual(StageChart.Load().events.Max(e => e.runBeat) * GameConfig.MetersPerBeat, len);
        }

        [Test]
        public void T04_Path_HasSixSections()
        {
            Assert.AreEqual(6, MapLayout.Sections.Length);
            for (int i = 1; i < MapLayout.Sections.Length; i++)
                Assert.AreEqual(MapLayout.Sections[i - 1].EndBeat, MapLayout.Sections[i].StartBeat, "구간이 이어짐");
            Assert.AreEqual(0, MapLayout.Sections[0].StartBeat);
            Assert.AreEqual(StageChart.Load().runBeats, MapLayout.Sections[5].EndBeat);
        }

        [Test]
        public void T04_Roofs_HeightAndGapsInGddRange()
        {
            var houses = MapLayout.Roofs.Where(r => r.AssetId == "house_giwa").ToArray();
            Assert.GreaterOrEqual(houses.Length, 3);
            foreach (var r in MapLayout.Roofs)
                Assert.That(r.Height, Is.InRange(GameConfig.RoofHeightMin, GameConfig.RoofHeightMax), r.AssetId);
            for (int i = 1; i < houses.Length; i++)
                Assert.That(houses[i].Start - houses[i - 1].End, Is.InRange(GameConfig.RoofGapMin, GameConfig.RoofGapMax), $"지붕 {i} 틈");
        }

        [Test]
        public void T04_Path_PointAt_ContinuousAndFollowsRoofs()
        {
            // 경로가 끊기지 않음 (0.5 m 간격 샘플 사이 수평 거리 ≤ 0.5 m)
            var prev = MapLayout.FlatPointAt(0, out _);
            for (float d = 0.5f; d <= MapLayout.TotalLength; d += 0.5f)
            {
                var p = MapLayout.FlatPointAt(d, out _);
                Assert.LessOrEqual(Vector3.Distance(prev, p), 0.5001f, $"d={d}");
                prev = p;
            }
            // 지붕 위에서는 발 높이가 지붕 높이와 같음
            foreach (var r in MapLayout.Roofs)
            {
                float mid = (r.Start + Mathf.Min(r.End, MapLayout.TotalLength)) * 0.5f;
                Assert.AreEqual(r.Height, MapLayout.PointAt(mid).y, 0.01f, $"{r.AssetId} {r.Start}");
            }
            Assert.AreEqual(0f, MapLayout.PointAt(10f).y, 1e-4f, "골목은 지면");
        }
    }
}
