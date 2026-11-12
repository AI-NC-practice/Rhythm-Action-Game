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
            // beta1-1: GDD-beta B3이 "장애물 20"을 대신한다 (판정 수 65~75는 B04_ChartTests). 일반 적(대치) 4, 포위 1, 무사 1은 그대로.
            var c = StageChart.Load();
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
                if (e.IsRunEvent) Assert.DoesNotThrow(() => EventCatalog.SlotsFor(e), $"{e.runBeat} 키 오류");
                if (e.IsDuel)
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
            // beta1-1: GDD-beta B2.1 — 사건 사이 간격 0박 이상 (사건 시작부터 다음 시작까지는 최소 4박 = 가장 짧은 사건 길이)
            var c = StageChart.Load();
            var runs = c.events.Where(e => e.IsRunEvent).OrderBy(e => e.runBeat).ToArray();
            for (int i = 1; i < runs.Length; i++)
                Assert.GreaterOrEqual(runs[i].runBeat - runs[i - 1].runBeat, GameConfig.MinObstacleGapBeats, $"{runs[i - 1].runBeat} → {runs[i].runBeat}");
        }

        [Test]
        public void T04_Chart_ObstacleKeysMatchGdd3()
        {
            var c = StageChart.Load();
            foreach (var e in c.Obstacles.Where(o => EventCatalog.Get(o.kind).Category.HasValue))   // GDD 4.4 기존 장애물만 (새 사건 키는 B02·B04 테스트)
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
            // beta1-1: 사건은 종류별 길이(4·6박)만큼 차지한다 (GDD-beta B2.1). 전투는 그 자리에서 멈추므로 길이 0, 복선은 판정 없음.
            var evs = c.events.Where(e => !e.IsForeshadow).OrderBy(e => e.runBeat).ToArray();
            for (int i = 1; i < evs.Length; i++)
                Assert.GreaterOrEqual(evs[i].runBeat, evs[i - 1].runBeat + ChartCheck.Length(evs[i - 1]), $"{evs[i - 1].runBeat}와 {evs[i].runBeat} 겹침");
            for (int i = 1; i < evs.Length; i++)
                Assert.Greater(evs[i].runBeat, evs[i - 1].runBeat, "같은 박자에 두 이벤트 없음");
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
