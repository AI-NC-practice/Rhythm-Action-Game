using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 맵 배치 계획 (작업 R03, R01 재설계 안 docs/proposals/map-redesign.html을 그대로 옮김).
    /// 순수 C#: 차트와 씨앗을 받아 건물(1~3줄)·소품·랜드마크 배치 목록을 만든다. SceneBuilder와 EditMode 테스트가 함께 쓴다.
    /// 위치는 경로 거리 s(m)와 길 중심에서 옆 거리 off(m), 쪽 side(-1 왼쪽, +1 오른쪽)로 나타낸다.
    /// </summary>
    public static class MapPlan
    {
        public enum Kind { Building, Prop }

        public class Placement
        {
            public Kind Kind;
            public string Id;
            /// <summary>건물: 길을 따라 [S0, S1]. 소품: S0 = S1 = 위치.</summary>
            public float S0, S1;
            public int Side;
            /// <summary>건물: 앞면(길 쪽) 옆 거리. 소품: 중심 옆 거리.</summary>
            public float Off;
            public int Row;          // 1~3 (소품 0)
            public float Yaw;        // 도
            public float Scale = 1f;
            public bool Landmark, Door, Support;
            public float S => (S0 + S1) * 0.5f;
        }

        /// <summary>사건 때문에 비워 두거나 정해 둔 자리 (벽타기 벽, 매복 대문, 궁궐 문, 복선 적 발판).</summary>
        public class Fixed
        {
            public float D0, D1; public int Side; public string Module; public float Half = -1f; public string Why;
        }

        public class Result
        {
            public readonly List<Placement> Items = new List<Placement>();
            public readonly List<Fixed> Fixed = new List<Fixed>();
            public IEnumerable<Placement> Buildings => Items.Where(p => p.Kind == Kind.Building);
            public IEnumerable<Placement> Props => Items.Where(p => p.Kind == Kind.Prop);
        }

        public static GameConfig.Map.Module ModuleOf(string id) => GameConfig.Map.Modules.First(m => m.Id == id);
        public static GameConfig.Map.PropDef PropOf(string id) => GameConfig.Map.Props.First(p => p.Id == id);
        public static bool IsWall(string id) => id.StartsWith("wall") || id == "fence";

        static int KeySide(ArrowKey k) => k == ArrowKey.Left ? -1 : k == ArrowKey.Right ? 1 : 0;

        static GameConfig.Map.Zone ZoneOf(float d)
        {
            foreach (var z in GameConfig.Map.Zones) if (d >= z.Start && d < z.End) return z;
            return GameConfig.Map.Zones[GameConfig.Map.Zones.Length - 1];
        }

        /// <summary>1줄 건물 앞면 반폭: 구역 반폭, 길 위 지붕이 있으면 지붕 반폭 + 틈 이상.</summary>
        public static float FrontHalf(float s0, float s1, float zoneHalf)
        {
            float h = zoneHalf;
            foreach (var r in MapLayout.Roofs)
                if (r.Start < s1 && r.End > s0) h = Mathf.Max(h, r.Width * 0.5f + GameConfig.Map.RoofSideGap);
            return h;
        }

        public static Result Build(ChartData chart, int seed = GameConfig.Map.DressSeed)
        {
            var rnd = new System.Random(seed);
            float U(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            T Pick<T>(IList<T> list) => list[rnd.Next(list.Count)];
            var res = new Result();
            var events = chart.events.ToList();

            // ---- 사건이 요구하는 고정 자리 ----
            foreach (var e in events)
            {
                float d = e.Distance;
                if (e.IsObstacle && e.kind == "wall_run")
                {
                    int s = KeySide(e.Key); if (s == 0) s = -1;
                    res.Fixed.Add(new Fixed { D0 = d - 4f, D1 = d + 6f, Side = s, Module = "wall_run", Half = 2f, Why = "벽타기 벽" });
                }
                bool high = (d >= 270f && d < 410f) || (d >= 560f && d < 625f);
                if (e.IsEnemy && e.approach == EventCatalog.Ambush && !high)
                {
                    var slots = EventCatalog.SlotsFor(e);
                    int ks = slots.Count > 0 && slots[0].Key.HasValue ? KeySide(slots[0].Key.Value) : 1;
                    int side = ks == 0 ? 1 : -ks;   // 적은 키 반대쪽 담 뒤에서 나온다 (EventProps)
                    res.Fixed.Add(new Fixed { D0 = d + 3f, D1 = d + 9f, Side = side, Module = "door", Why = "매복 포졸이 나오는 대문" });
                }
                // 작업 R18b: 궁궐 담 위(560~625 m) 매복 — 담 밑에 기와집 한 채, 끝자리 바로 앞에서 끝남 (포졸이 집 앞쪽 모퉁이 너머에 숨었다가 담 위로 뛰어오름)
                if (e.IsEnemy && e.approach == EventCatalog.Ambush && d >= 560f && d < 625f)
                {
                    var slots = EventCatalog.SlotsFor(e);
                    int ks = slots.Count > 0 && slots[0].Key.HasValue ? KeySide(slots[0].Key.Value) : 1;
                    int side = ks == 0 ? 1 : -ks;
                    res.Fixed.Add(new Fixed { D0 = d - 3f, D1 = d + 6f, Side = side, Module = "giwa_s", Why = "매복 포졸이 숨는 집 (궁궐 담 밑, 포졸은 집 앞쪽 모퉁이 너머)" });
                }
                if (e.IsObstacle && e.kind == "closing_gate")
                    res.Fixed.Add(new Fixed { D0 = d + 3f, D1 = d + 7f, Side = 0, Module = "palace_gate", Why = "길을 가로지르는 궁궐 문" });
                if (e.IsForeshadow)
                {
                    var st = GameConfig.Map.ForeshadowStands.FirstOrDefault(x => x.Kind == e.kind);
                    if (st.Kind == null) continue;
                    int side = st.Lateral >= 0 ? 1 : -1;
                    float a = d + st.Ahead, b = a + st.Spacing * 2f;
                    if (e.kind == "roof_watchers")
                        res.Fixed.Add(new Fixed { D0 = a - 5f, D1 = b + 5f, Side = side, Module = "sijeon", Why = "복선: 지붕 위 감시 포졸 발판" });
                    else if (e.kind == "wall_climbers")
                        res.Fixed.Add(new Fixed { D0 = a - 4f, D1 = b + 4f, Side = side, Module = "wall_stone", Half = Mathf.Abs(st.Lateral) - 0.5f, Why = "복선: 담 넘는 포졸 발판" });
                }
            }

            Fixed FixedIn(float s0, float s1, int side) =>
                res.Fixed.FirstOrDefault(f => (f.Side == side || f.Side == 0) && !(s1 <= f.D0 || s0 >= f.D1));

            // ---- 건물 1줄 ----
            var hist = new Dictionary<int, List<string>> { { -1, new List<string>() }, { 1, new List<string>() } };
            foreach (var z in GameConfig.Map.Zones)
            {
                foreach (int side in new[] { -1, 1 })
                {
                    var pal = side < 0 ? z.Left : z.Right;
                    if (pal.Length == 0)
                    {
                        // 작업 R18b: 길가 건물 줄이 없는 구간(궁궐 담 위)에도 사건이 요구한 집은 놓는다 (담 밑, 길에서 HideHouseOffset m)
                        foreach (var f in res.Fixed.Where(f => f.Module == "giwa_s" && f.Side == side && f.D0 >= z.Start && f.D0 < z.End))
                            res.Items.Add(new Placement { Kind = Kind.Building, Id = f.Module, S0 = f.D0, S1 = f.D1, Side = side, Off = GameConfig.Map.HideHouseOffset, Row = 1, Door = false, Support = true });
                        continue;
                    }
                    float s = z.Start;
                    var h = hist[side];
                    while (s < z.End - 2f)
                    {
                        var f = FixedIn(s, s + 1f, side);
                        if (f != null && s >= f.D0 - 0.01f)
                        {
                            if (f.Module == "wall_run") { s = f.D1; h.Add("fixed"); continue; }
                            if (f.Module == "door" || f.Module == "sijeon" || f.Module == "wall_stone" || f.Module == "giwa_s")
                            {
                                string did = f.Module == "door"
                                    ? (z.Start >= 140f ? new[] { "giwa_gate", "giwa_s" } : new[] { "choga", "choga_L" }).First(m => h.Count == 0 || h[h.Count - 1] != m)
                                    : f.Module;
                                float half = f.Half >= 0 ? f.Half : FrontHalf(f.D0, f.D1, z.Half);
                                res.Items.Add(new Placement { Kind = Kind.Building, Id = did, S0 = f.D0, S1 = f.D1, Side = side, Off = half, Row = 1, Door = f.Module == "door", Support = f.Module != "door" });
                                h.Add(did); s = f.D1; continue;
                            }
                            s = f.D1; h.Add("fixed"); continue;
                        }
                        var choices = pal.Where(m => !h.Skip(Math.Max(0, h.Count - 2)).Contains(m)).ToList();
                        if (choices.Count == 0) choices = pal.Where(m => h.Count == 0 || h[h.Count - 1] != m).ToList();
                        if (choices.Count == 0) choices = pal.ToList();
                        string mid = Pick(choices);
                        var mod = ModuleOf(mid);
                        float w = U(mod.WidthMin, mod.WidthMax);
                        var nf = res.Fixed.FirstOrDefault(x => (x.Side == side || x.Side == 0) && x.D0 > s && x.D0 < s + w);
                        if (nf != null) w = Mathf.Max(2f, nf.D0 - s);
                        w = Mathf.Min(w, z.End - s);
                        if (w < 2.5f) { s += w; continue; }
                        float off = FrontHalf(s, s + w, z.Half) + (mod.Depth < 2f ? 0f : U(0f, GameConfig.Map.FrontJitter));
                        res.Items.Add(new Placement
                        {
                            Kind = Kind.Building, Id = mid, S0 = s, S1 = s + w, Side = side, Off = off, Row = 1,
                            Yaw = U(-GameConfig.Map.YawJitter, GameConfig.Map.YawJitter), Scale = U(GameConfig.Map.ScaleMin, GameConfig.Map.ScaleMax),
                        });
                        h.Add(mid);
                        float gap = U(0f, GameConfig.Map.GapMax);
                        bool high = MapLayout.RoofHalfAt(s) > 0f;   // 지붕 위를 달리는 곳은 샛골목 없이 지붕을 잇는다 (추격 적 발밑)
                        if (!high && rnd.NextDouble() < GameConfig.Map.AlleyChance) gap = U(GameConfig.Map.AlleyMin, GameConfig.Map.AlleyMax);
                        s += w + gap;
                    }
                }
            }

            // ---- 2·3줄 (지붕 바다처럼 겹쳐 보이게) ----
            foreach (var z in GameConfig.Map.Zones)
                foreach (int side in new[] { -1, 1 })
                    foreach (var (row, pal0, back) in new[] { (2, z.Row2, GameConfig.Map.Row2Back), (3, z.Row3, GameConfig.Map.Row3Back) })
                    {
                        var pal = pal0;
                        if (pal.Contains("palace_hall") && side == 1) pal = new[] { "house_giwa", "giwa_long", "giwa_s" };   // 궁궐은 왼쪽에만
                        if (pal.Length == 0) continue;
                        float s = z.Start + U(0f, 4f);
                        string prev = null;
                        while (s < z.End - 3f)
                        {
                            var opts = pal.Where(m => m != prev).ToList(); if (opts.Count == 0) opts = pal.ToList();
                            string mid = Pick(opts); prev = mid;
                            var mod = ModuleOf(mid);
                            float w = Mathf.Min(U(mod.WidthMin, mod.WidthMax), z.End - s);
                            float off = FrontHalf(s, s + w, z.Half) + back + U(-GameConfig.Map.RowJitter, GameConfig.Map.RowJitter);
                            res.Items.Add(new Placement { Kind = Kind.Building, Id = mid, S0 = s, S1 = s + w, Side = side, Off = off, Row = row,
                                Yaw = U(-GameConfig.Map.YawJitter, GameConfig.Map.YawJitter), Scale = U(GameConfig.Map.ScaleMin, GameConfig.Map.ScaleMax) });
                            s += w + U(0.5f, row == 2 ? 2.5f : 5f);
                        }
                    }

            // ---- 소품: 길가(달리는 길과 건물 앞면 사이), 달리는 길 안에는 두지 않음 ----
            float corridor = GameConfig.Map.Corridor;
            foreach (var z in GameConfig.Map.Zones)
            {
                if (z.Props.Length == 0 || z.PropsPer10m <= 0f || z.Half <= 0f) continue;
                int n = Mathf.RoundToInt((z.End - z.Start) / 10f * z.PropsPer10m);
                int count = 0, tries = 0;
                while (count < n && tries < n * 60)
                {
                    tries++;
                    string pid = Pick(z.Props);
                    int side = rnd.Next(2) == 0 ? -1 : 1;
                    float fp = PropOf(pid).Footprint;
                    float s = U(z.Start + 1f, z.End - 1f);
                    float off;
                    if (z.Half > 10f) off = U(z.Half - 4f, z.Half - 1f);   // 마당: 담 쪽
                    else
                    {
                        float lo = corridor + fp * 0.5f + 0.2f;
                        off = U(lo, Mathf.Max(lo, z.Half - fp * 0.5f - 0.1f));
                    }
                    if (events.Any(ev => !ev.IsForeshadow && Mathf.Abs(ev.Distance - s) < GameConfig.Map.EventClearAlong) && off - fp * 0.5f < GameConfig.Map.EventClearHalf) continue;
                    if (FixedIn(s - fp, s + fp, side) != null) continue;
                    if (res.Items.Any(p => p.Kind == Kind.Prop && p.Side == side && Mathf.Abs(p.S0 - s) < (fp + PropOf(p.Id).Footprint) * 0.5f + 0.3f
                                           && Mathf.Abs(p.Off - off) < (fp + PropOf(p.Id).Footprint) * 0.5f)) continue;
                    if (!PropClear(s, side, off, fp)) continue;
                    res.Items.Add(new Placement { Kind = Kind.Prop, Id = pid, S0 = s, S1 = s, Side = side, Off = off, Yaw = U(-15f, 15f), Scale = U(GameConfig.Map.ScaleMin, GameConfig.Map.ScaleMax) });
                    count++;
                }
            }

            // ---- 랜드마크 (시선 끝) ----
            res.Items.Add(new Placement { Kind = Kind.Prop, Id = "jangseung", S0 = 3f, S1 = 3f, Side = -1, Off = 2.4f, Landmark = true });
            res.Items.Add(new Placement { Kind = Kind.Prop, Id = "jangseung", S0 = 3f, S1 = 3f, Side = 1, Off = 2.4f, Landmark = true });
            res.Items.Add(new Placement { Kind = Kind.Prop, Id = "zelkova", S0 = 8f, S1 = 8f, Side = 1, Off = 7.5f, Landmark = true });
            res.Items.Add(new Placement { Kind = Kind.Prop, Id = "zelkova", S0 = 140f, S1 = 140f, Side = 1, Off = 9f, Landmark = true });
            res.Items.Add(new Placement { Kind = Kind.Prop, Id = "well", S0 = 137f, S1 = 137f, Side = -1, Off = 6.5f, Landmark = true });

            // 모퉁이 곡선 쪽으로 튀어나와 길을 막는 건물은 뺀다 (발자국 네 귀퉁이·가운데가 길에서 떨어져야 함)
            res.Items.RemoveAll(p => p.Kind == Kind.Building && !BuildingClear(p));
            return res;
        }

        /// <summary>건물 발자국 사각형(여유 BuildingClearance를 더한)에 길 중심선이 하나도 들지 않는지 — 건물이 길을 걸쳐도 잡는다.
        /// depth를 안 주면 명세 깊이 × FootprintMargin (모델 평면 비율이 명세와 조금 달라도 모퉁이 다음 길을 막지 않게).</summary>
        public static bool BuildingClear(Placement p, float depth = -1f)
        {
            if (depth < 0f) depth = ModuleOf(p.Id).Depth * (IsWall(p.Id) ? 1f : GameConfig.Map.FootprintMargin);
            var c = MapLayout.FlatPointAt(p.S, out var fwd);
            var away = MapLayout.RightOf(fwd) * p.Side;
            float cl = GameConfig.Map.BuildingClearance - 0.05f;
            float u0 = p.S0 - p.S - cl, u1 = p.S1 - p.S + cl, v0 = p.Off - cl, v1 = p.Off + depth + cl;
            // 길 중심선 표본점이 (여유를 더한) 건물 사각형 안에 하나라도 들면 길을 막는 것 — 건물이 길을 걸쳐도 잡는다
            foreach (var q in MapLayout.PathSamples)
            {
                var d = q - c; d.y = 0f;
                float u = Vector3.Dot(d, fwd), v = Vector3.Dot(d, away);
                if (u >= u0 && u <= u1 && v >= v0 && v <= v1) return false;
            }
            return true;
        }

        static bool PropClear(float s, int side, float off, float fp)
        {
            var c = MapLayout.FlatPointAt(s, out var fwd);
            var q = c + MapLayout.RightOf(fwd) * side * off;
            return MapLayout.DistanceToPath(q) - fp * 0.5f >= GameConfig.Map.Corridor + 0.1f;
        }

        /// <summary>건물 배치의 월드 위치(앞면 가운데, y=0)와 길을 바라보는 회전 (모델 +z가 길 쪽).</summary>
        public static void Pose(Placement p, out Vector3 frontCenter, out Quaternion rotation, out Vector3 awayFromPath)
        {
            var c = MapLayout.FlatPointAt(p.S, out var fwd);
            var right = MapLayout.RightOf(fwd);
            awayFromPath = right * p.Side;
            frontCenter = c + awayFromPath * p.Off;
            frontCenter.y = 0f;
            rotation = Quaternion.LookRotation(-awayFromPath, Vector3.up) * Quaternion.Euler(0f, p.Yaw, 0f);
        }

        /// <summary>복선 적 한 명의 서는 자리 (월드, y는 0 — 지지면 높이는 SceneBuilder가 맞춘다).</summary>
        public static Vector3 ForeshadowStand(ChartEvent e, int i)
        {
            var st = GameConfig.Map.ForeshadowStands.First(x => x.Kind == e.kind);
            float d = e.Distance + st.Ahead + i * st.Spacing;
            var c = MapLayout.FlatPointAt(d, out var fwd);
            return c + MapLayout.RightOf(fwd) * st.Lateral;
        }
    }
}
