using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R03: R01 재설계 안대로 챕터별 직접 배치(MapPlan), 모퉁이 곡선, 장터 거리, 복선 적 발판, 배경 파노라마.
    /// </summary>
    public class R03_MapPlanTests
    {
        static MapPlan.Result plan;
        static MapPlan.Result Plan => plan ?? (plan = MapPlan.Build(StageChart.Load()));

        [Test]
        public void R03_Plan_SameBuildingNeverTwiceInARow_RotationAndScaleVary()
        {
            int repeats = 0;
            foreach (int side in new[] { -1, 1 })
            {
                var row1 = Plan.Buildings.Where(p => p.Row == 1 && p.Side == side).OrderBy(p => p.S0).ToList();
                for (int i = 1; i < row1.Count; i++)
                    if (row1[i].Id == row1[i - 1].Id && !MapPlan.IsWall(row1[i].Id) && row1[i].S0 - row1[i - 1].S1 < 3f) repeats++;
            }
            Assert.AreEqual(0, repeats, "같은 건물 모델이 연달아 붙지 않음 (담·울타리는 이어져도 됨)");
            var free = Plan.Buildings.Where(p => !p.Door && !p.Support).ToList();
            Assert.Greater(free.Count, 150, "건물 배치");
            Assert.IsTrue(free.All(p => Mathf.Abs(p.Yaw) <= GameConfig.Map.YawJitter + 1e-4f), "회전 ±4°");
            Assert.IsTrue(free.All(p => p.Scale >= GameConfig.Map.ScaleMin - 1e-4f && p.Scale <= GameConfig.Map.ScaleMax + 1e-4f), "크기 0.92~1.08");
            Assert.Greater(free.Select(p => Mathf.Round(p.Yaw)).Distinct().Count(), 5, "회전이 제각각");
            Assert.Greater(free.Select(p => Mathf.Round(p.Scale * 100f)).Distinct().Count(), 8, "크기가 제각각");
            // 1줄 건물 사이 틈: 붙어 이어지거나(0~1.2 m) 샛골목(3~4.5 m)
            foreach (int side in new[] { -1, 1 })
            {
                var row1 = Plan.Buildings.Where(p => p.Row == 1 && p.Side == side).OrderBy(p => p.S0).ToList();
                int alleys = 0;
                for (int i = 1; i < row1.Count; i++) { float g = row1[i].S0 - row1[i - 1].S1; if (g >= GameConfig.Map.AlleyMin - 0.01f && g <= GameConfig.Map.AlleyMax + 0.01f) alleys++; }
                Assert.Greater(alleys, 2, $"{side} 쪽 샛골목");
            }
        }

        [Test]
        public void R03_Plan_PropDensity_MatchesR01()
        {
            foreach (var z in GameConfig.Map.Zones.Where(z => z.PropsPer10m > 0 && z.Half > 0))
            {
                int n = Plan.Props.Count(p => !p.Landmark && p.S0 >= z.Start && p.S0 < z.End);
                float per10 = n / ((z.End - z.Start) / 10f);
                Assert.AreEqual(z.PropsPer10m, per10, 1.0f, $"{z.Name}: 10 m당 소품 {per10:0.0} (목표 {z.PropsPer10m})");
            }
        }

        [Test]
        public void R03_Plan_RunCorridorClear_BuildingsOffPath()
        {
            foreach (var p in Plan.Props.Where(p => !p.Landmark))
            {
                var c = MapLayout.FlatPointAt(p.S0, out var fwd);
                var q = c + MapLayout.RightOf(fwd) * p.Side * p.Off;
                Assert.GreaterOrEqual(MapLayout.DistanceToPath(q) - MapPlan.PropOf(p.Id).Footprint * 0.5f, GameConfig.Map.Corridor, $"{p.Id} {p.S0:0} m: 달리는 길(폭 3.6 m) 밖");
            }
            foreach (var b in Plan.Buildings) Assert.IsTrue(MapPlan.BuildingClear(b), $"{b.Id} {b.S0:0}~{b.S1:0} m: 모퉁이에서도 길을 막지 않음");
        }

        [Test]
        public void R03_Plan_MarketStreet_BetweenSection1And2()
        {
            var market = Plan.Items.Where(p => p.S0 >= 85f && p.S0 < 215f).ToList();
            foreach (var id in new[] { "stall_a", "stall_b", "stall_pot", "straw", "cart", "cloth_rack", "lantern" })
                Assert.IsTrue(market.Any(p => p.Id == id), $"장터에 {id}");
            Assert.Greater(market.Count(p => p.Id == "sijeon"), 4, "2층 시전 상점채 거리");
            Assert.AreEqual(1, MapLayout.SectionOf(Mathf.FloorToInt(100f / GameConfig.MetersPerBeat)).Number, "장터 큰길은 1구간 뒤");
            Assert.AreEqual(2, MapLayout.SectionOf(Mathf.FloorToInt(160f / GameConfig.MetersPerBeat)).Number, "장터 뒷골목은 2구간 앞");
        }

        [Test]
        public void R03_Path_CornersCurved_BeatsAndLengthKept()
        {
            Assert.AreEqual(StageChart.Load().PathLength, MapLayout.TotalLength, 1e-3f, "경로 길이 그대로");
            float r = GameConfig.Map.CornerRadius;
            var prevP = MapLayout.FlatPointAt(0f, out var prevF);
            float maxTurn = 0f;
            for (float d = 0.25f; d <= MapLayout.TotalLength; d += 0.25f)
            {
                var p = MapLayout.FlatPointAt(d, out var f);
                Assert.LessOrEqual(Vector3.Distance(p, prevP), 0.25f + 1e-3f, $"{d} m 연속");
                maxTurn = Mathf.Max(maxTurn, Vector3.Angle(prevF, f));
                if (MapLayout.CornerNear(d) < 0f) Assert.AreEqual(0f, Vector3.Distance(p, MapLayout.VertexPointAt(d, out _)), 1e-4f, $"{d} m: 곡선 밖 위치는 예전과 같음");
                prevP = p; prevF = f;
            }
            Assert.Less(maxTurn, 3f, "0.25 m마다 방향이 3° 넘게 꺾이지 않음 (카메라가 순간이동처럼 돌지 않음)");
            foreach (var c in MapLayout.Corners)
                Assert.Greater(Vector3.Distance(MapLayout.FlatPointAt(c, out _), MapLayout.VertexPointAt(c, out _)), 2f, $"{c} m 모퉁이가 곡선");
            Assert.Greater(r, 0f);
        }

        [Test]
        public void R03_Plan_ForeshadowStandsOnPlannedRoofOrWall()
        {
            var chart = StageChart.Load();
            var fs = chart.events.Where(e => e.IsForeshadow).ToList();
            Assert.AreEqual(3, fs.Count);
            foreach (var e in fs.Where(e => e.kind != "gate_warrior"))
                for (int i = 0; i < 3; i++)
                {
                    var stand = MapPlan.ForeshadowStand(e, i);
                    bool onIt = Plan.Buildings.Where(b => b.Support).Any(b =>
                    {
                        MapPlan.Pose(b, out var front, out _, out var away);
                        float lateral = Vector3.Dot(stand - front, away);
                        var along = Vector3.Cross(Vector3.up, away).normalized;
                        float a = Mathf.Abs(Vector3.Dot(stand - front, along));
                        return lateral >= -0.1f && lateral <= MapPlan.ModuleOf(b.Id).Depth + 0.1f && a <= (b.S1 - b.S0) * 0.5f;
                    });
                    Assert.IsTrue(onIt, $"{e.kind} {i}: 발밑에 정해 둔 지붕·담");
                }
            var gate = fs.First(e => e.kind == "gate_warrior");
            float gd = gate.Distance + GameConfig.Map.ForeshadowStands.First(s => s.Kind == "gate_warrior").Ahead;
            Assert.IsTrue(MapLayout.Roofs.Any(r => r.AssetId == "gate" && gd >= r.Start && gd <= r.End), "성문 무사는 성문 누각 위");
        }

        [Test]
        public void R03_Backdrop_PanoramaOfSixPaintings()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(EditorTools.SceneBuilder.PanoramaPath);
            Assert.IsNotNull(tex, "파노라마");
            Assert.Greater(tex.width, 6000, "6장을 이어 붙인 긴 그림");
            Assert.Greater((float)tex.width / tex.height, 8f);
        }
    }
}
