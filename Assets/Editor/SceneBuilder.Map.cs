using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RhythmActionGame.EditorTools
{
    /// <summary>
    /// 맵 모델·꾸미기·분위기 (작업 T15, 2026-11-16 사용자 맵 디자인 요청).
    /// - 회색 상자(지붕 길·골목 담·장애물)는 충돌과 이름을 그대로 두고 그리기만 끈다. 같은 자리에 VARCO 모델을 맞춰 넣는다.
    /// - 챕터 컨셉에 맞춘 길 옆 장식(집·돌담·항아리·수레), 길에서 떨어진 곳에만 (씨앗 고정).
    /// - 먼 외곽은 그림 원통(산·궁궐·지붕 실루엣)으로 착시, 하늘은 해 질 녘 스카이박스, 안개.
    /// - 맵 모델은 모두 정적(정적 배칭) + 툰 머티리얼 공유(SRP Batcher) + GPU 인스턴싱.
    /// </summary>
    public static partial class SceneBuilder
    {
        public const string BackdropDir = "Assets/Art/Backdrop";
        public const string PanoramaPath = "Assets/Art/Backdrops/panorama.png";

        // ---------- 모델 감싸기 ----------

        class Wrapped { public GameObject Go; public Vector3 Size; }

        static readonly Dictionary<string, (Vector3 size, bool rotate, Vector3 center, float minY)> shapeCache = new Dictionary<string, (Vector3, bool, Vector3, float)>();

        /// <summary>GLB 모델을 감싼다: 감싼 물체의 원점 = 모델 바닥 가운데, 가장 긴 수평축 = x. Size는 감싼 물체 기준 크기.</summary>
        static Wrapped Wrap(string id, Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var m = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ToonMaterials.StaticPath(id)));
            PrefabUtility.UnpackPrefabInstance(m, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            m.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            m.transform.localScale = Vector3.one;
            if (!shapeCache.TryGetValue(id, out var shape))
            {
                var pts = new List<Vector3>();
                foreach (var mf in m.GetComponentsInChildren<MeshFilter>(true))
                    foreach (var v in mf.sharedMesh.vertices) pts.Add(m.transform.InverseTransformPoint(mf.transform.TransformPoint(v)));
                var mn = new Vector3(pts.Min(p => p.x), pts.Min(p => p.y), pts.Min(p => p.z));
                var mx = new Vector3(pts.Max(p => p.x), pts.Max(p => p.y), pts.Max(p => p.z));
                var sz = mx - mn;
                bool rot = sz.z > sz.x;
                shape = (rot ? new Vector3(sz.z, sz.y, sz.x) : sz, rot, (mn + mx) * 0.5f, mn.y);
                shapeCache[id] = shape;
            }
            m.transform.SetParent(root.transform, false);
            var r = shape.rotate ? Quaternion.Euler(0, 90f, 0) : Quaternion.identity;
            m.transform.localRotation = r;
            m.transform.localPosition = -(r * new Vector3(shape.center.x, shape.minY, shape.center.z));
            var mat = ToonMaterials.Static(id);
            mat.enableInstancing = true;
            foreach (var rd in m.GetComponentsInChildren<MeshRenderer>(true)) rd.sharedMaterials = Enumerable.Repeat(mat, rd.sharedMaterials.Length).ToArray();
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
            return new Wrapped { Go = root, Size = shape.size };
        }

        /// <summary>
        /// 축(pivot) 가운데에 모델을 size 크기로 맞춰 붙인다 (작업 R04: 사건 소품·장애물). 모델 긴 수평축 = 축의 x.
        /// 움직이는 부품이 많아 정적 배칭에서 뺀다.
        /// </summary>
        public static Transform FitModelCentered(Transform pivot, string id, Vector3 size)
        {
            var w = Wrap(id, pivot, $"Model_{id}");
            w.Go.transform.localRotation = Quaternion.identity;
            w.Go.transform.localPosition = new Vector3(0f, -size.y * 0.5f, 0f);
            w.Go.transform.localScale = new Vector3(size.x / w.Size.x, size.y / w.Size.y, size.z / w.Size.z);
            foreach (var t in w.Go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = false;
            return w.Go.transform;
        }

        /// <summary>회색 상자들 대신 모델 하나 (장애물): 상자는 충돌만 남기고 그리기를 끄고, 축 가운데에 모델.</summary>
        static void ReplaceBoxes(Transform root, string id, Vector3 localCenter, Vector3 size, params Transform[] boxes)
        {
            foreach (var b in boxes) { var r = b.GetComponent<MeshRenderer>(); if (r != null) r.enabled = false; }
            var pivot = new GameObject($"ModelPivot_{id}").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = localCenter;
            FitModelCentered(pivot, id, size);
        }

        /// <summary>상자 자리에 모델을 꽉 맞춘다(크기·방향). 상자는 충돌만 남기고 그리기를 끈다.</summary>
        static void FitIntoBox(Transform box, string id, int tiles = 1, string centerId = null, int centerIndex = -1)
        {
            if (centerIndex < 0) centerIndex = tiles / 2;
            var size = box.lossyScale;
            bool alongZ = size.z > size.x;
            float len = alongZ ? size.z : size.x, thick = alongZ ? size.x : size.z;
            var axis = alongZ ? box.forward : box.right;
            var yaw = alongZ ? Quaternion.Euler(0, 90f, 0) : Quaternion.identity;
            var bottom = box.position - box.up * size.y * 0.5f;
            for (int i = 0; i < tiles; i++)
            {
                string use = centerId != null && i == centerIndex ? centerId : id;
                var w = Wrap(use, box.parent, $"Model_{use}_{box.name}_{i}");
                float piece = len / tiles;
                var c = bottom + axis * (-len * 0.5f + piece * (i + 0.5f));
                w.Go.transform.SetPositionAndRotation(c, box.rotation * yaw);
                // 감싼 물체 x(긴 축)·y·z → 상자 긴 축·높이·두께
                w.Go.transform.localScale = new Vector3(piece / w.Size.x, size.y / w.Size.y, thick / w.Size.z);
            }
            var mr = box.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        /// <summary>긴 상자(지붕 길·담)를 비율이 맞는 개수로 나눠 모델을 늘어놓는다.</summary>
        /// <param name="avoid">상자 길이 방향 위치(상자 시작에서 m)들. 이 근처(±GameConfig.Map.SeamClearance)에 모델 이음매가 오지 않게 조각 수를 고른다
        /// (작업 R03: 기와집을 이어 붙인 지붕은 이음매에서 지붕 끝이 내려가 골이 생긴다 — 그 위에 서는 적이 공중에 뜨지 않게).</param>
        static void TileIntoBox(Transform box, string id, string centerId = null, IList<float> avoid = null)
        {
            var size = box.lossyScale;
            float len = Mathf.Max(size.x, size.z);
            var probe = Wrap(id, box.parent, "probe");
            float aspect = probe.Size.x / Mathf.Max(probe.Size.y, 0.01f);
            Object.DestroyImmediate(probe.Go);
            int ideal = Mathf.Max(1, Mathf.RoundToInt(len / (size.y * aspect)));
            int tiles = ideal;
            if (avoid != null && avoid.Count > 0)
            {
                int Bad(int n)
                {
                    int bad = 0;
                    for (int k = 1; k < n; k++) { float seam = len * k / n; foreach (var s in avoid) if (Mathf.Abs(s - seam) < GameConfig.Map.SeamClearance) bad++; }
                    return bad;
                }
                tiles = Enumerable.Range(Mathf.Max(1, ideal - 3), 7).OrderBy(n => Bad(n)).ThenBy(n => Mathf.Abs(n - ideal)).First();
            }
            // 가운데 모델(성문 누각)은 적이 서지 않는 칸에 — 누각 지붕은 길 높이와 맞지 않는다
            int center = tiles / 2;
            if (centerId != null && avoid != null && avoid.Count > 0)
            {
                bool Free(int i) => !avoid.Any(s => s > len * i / tiles - GameConfig.Map.SeamClearance && s < len * (i + 1) / tiles + GameConfig.Map.SeamClearance);
                var free = Enumerable.Range(0, tiles).Where(Free).OrderBy(i => Mathf.Abs(i - tiles / 2)).ToList();
                if (free.Count > 0) center = free[0];
            }
            FitIntoBox(box, id, tiles, centerId, center);
        }

        // ---------- 챕터 배치 (작업 R03: R01 재설계 안, MapPlan) ----------

        /// <summary>
        /// MapPlan의 배치를 모델로 짓는다. 이름 Dress_&lt;구간&gt;_&lt;번호&gt;_&lt;에셋&gt;_&lt;줄&gt;_s&lt;그림자 1/0&gt;.
        /// 건물: 길이 = 계획 너비, 평면 비율은 모델 그대로(담·울타리만 두께를 계획대로), 높이 = 명세 높이 × 크기 배율.
        /// 건물·담·큰 나무만 그림자를 드리운다 (드로우콜). 합치기(인스턴싱)는 적 발밑 검사 뒤 MergeDressing에서.
        /// </summary>
        static Transform BuildDressing(Transform map, ChartData chart)
        {
            var parent = new GameObject("Dressing").transform;
            parent.SetParent(map);
            parent.gameObject.isStatic = true;
            var plan = MapPlan.Build(chart);
            int n = 0;
            foreach (var p in plan.Items)
            {
                int sec = MapLayout.SectionOf(Mathf.FloorToInt(p.S / GameConfig.MetersPerBeat)).Number;
                // 그림자: 건물·담(줄 상관없이 한 묶음)과 큰 나무만. 작은 소품은 그림자 없이 — 같은 모델이 그림자 유무로 묶음이 갈리지 않게 (드로우콜, 작업 R03)
                bool shadow = p.Kind == MapPlan.Kind.Building || p.Id == "zelkova" || p.Id == "pine";
                string row = p.Kind == MapPlan.Kind.Prop ? "p" : $"r{p.Row}";
                var w = Wrap(p.Id, parent, $"Dress_{sec}_{n++}_{p.Id}_{row}_s{(shadow ? 1 : 0)}");
                if (p.Kind == MapPlan.Kind.Building)
                {
                    var mod = MapPlan.ModuleOf(p.Id);
                    float len = p.S1 - p.S0;
                    float kx = len / w.Size.x;
                    float ky = mod.Height * p.Scale / w.Size.y;
                    float kz = MapPlan.IsWall(p.Id) ? mod.Depth / w.Size.z : kx;
                    // 실제 모델 깊이로 발자국을 다시 검사 — 모퉁이 다음 길을 막으면 짓지 않는다
                    if (!MapPlan.BuildingClear(p, w.Size.z * kz)) { Object.DestroyImmediate(w.Go); continue; }
                    MapPlan.Pose(p, out var front, out var rot, out var away);
                    w.Go.transform.SetPositionAndRotation(front + away * (w.Size.z * kz * 0.5f), rot);
                    w.Go.transform.localScale = new Vector3(kx, ky, kz);
                }
                else
                {
                    var def = MapPlan.PropOf(p.Id);
                    float k = def.Height * p.Scale / w.Size.y;
                    var c = MapLayout.FlatPointAt(p.S, out var fwd);
                    var away = MapLayout.RightOf(fwd) * p.Side;
                    var pos = c + away * p.Off; pos.y = 0f;
                    w.Go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-away, Vector3.up) * Quaternion.Euler(0, p.Yaw, 0));
                    w.Go.transform.localScale = Vector3.one * k;
                }
            }
            return parent;
        }

        // ---------- 적·복선 발밑 (작업 R03) ----------

        /// <summary>
        /// 적과 복선 그림자 발밑에 실제로 보이는 지붕·담·땅이 있는지 잰다. 보이는 메시(충돌만 하는 회색 상자는 그리지 않으므로 빼고)의
        /// 삼각형과 발 위치를 지나는 수직선이 만나는 높이를 직접 계산한다 (물리 충돌체 없이 — 빌드 중 메시 충돌체가 광선에 안 잡히는 경우가 있었다).
        /// 복선 그림자와 달리며 나오는 적은 그 지지면 높이로 맞춘다. 결과는 MapSupportReport에 남긴다.
        /// </summary>
        static void CheckSupports(Transform map, Transform eventProps, EnemySet enemies)
        {
            var exclude = new HashSet<Transform>(eventProps.GetComponentsInChildren<Transform>(true));
            var enemiesRoot = map.Find("Enemies");
            if (enemiesRoot != null) foreach (var t in enemiesRoot.GetComponentsInChildren<Transform>(true)) exclude.Add(t);
            var surfaces = new List<(MeshRenderer r, Vector3[] v, int[] tri)>();
            foreach (var mr in map.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!mr.enabled || exclude.Contains(mr.transform) || mr.name == "Backdrop") continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var m = mf.sharedMesh;
                var l2w = mr.transform.localToWorldMatrix;
                surfaces.Add((mr, m.vertices.Select(x => l2w.MultiplyPoint3x4(x)).ToArray(), m.triangles));
            }
            // (x, z)를 지나는 수직선이 보이는 메시와 만나는 높이들
            List<(float y, Transform owner)> HeightsAt(Vector3 p)
            {
                var ys = new List<(float, Transform)>();
                foreach (var s in surfaces)
                {
                    var bb = s.r.bounds;
                    if (p.x < bb.min.x || p.x > bb.max.x || p.z < bb.min.z || p.z > bb.max.z) continue;
                    for (int i = 0; i < s.tri.Length; i += 3)
                    {
                        Vector3 a = s.v[s.tri[i]], b = s.v[s.tri[i + 1]], c = s.v[s.tri[i + 2]];
                        float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                        if (Mathf.Abs(d) < 1e-9f) continue;
                        float w1 = ((b.z - c.z) * (p.x - c.x) + (c.x - b.x) * (p.z - c.z)) / d;
                        float w2 = ((c.z - a.z) * (p.x - c.x) + (a.x - c.x) * (p.z - c.z)) / d;
                        float w3 = 1f - w1 - w2;
                        if (w1 < 0f || w2 < 0f || w3 < 0f) continue;
                        ys.Add((w1 * a.y + w2 * b.y + w3 * c.y, s.r.transform));
                    }
                }
                return ys;
            }
            string Owner(Transform t)
            {
                for (var x = t; x != null; x = x.parent)
                    if (x.name.StartsWith("Dress_") || x.name.StartsWith("Model_") || x.name == "Ground" || x.name.StartsWith("Path_")) return x.name;
                return t.name;
            }

            var report = map.gameObject.AddComponent<MapSupportReport>();
            // kind: foreshadow = 가장 높은 지붕·담 윗면에 세움 / runner = 달리며 나오는 적(추격·매복·돌진), 발 근처(위 3 m~아래 5 m)에서
            //       발 높이에 가장 가까운 면(지붕 비탈·땅)에 맞춤 / enemy = 멈춰 싸우는 적, 그대로 두고 발밑(±허용치)을 확인만
            void Add(Transform fig, string kind)
            {
                var f = fig.position;
                float tol = GameConfig.Map.SupportTolerance;
                var ys = HeightsAt(f);
                float lo = kind == "foreshadow" ? -1e9f : kind == "runner" ? f.y - 5f : f.y - tol;
                float hi = kind == "foreshadow" ? 1e9f : kind == "runner" ? f.y + 3f : f.y + tol;
                var inRange = ys.Where(h => h.y >= lo && h.y <= hi).ToList();
                bool ok = inRange.Count > 0;
                var best = !ok ? default : kind == "foreshadow" ? inRange.OrderByDescending(h => h.y).First() : inRange.OrderBy(h => Mathf.Abs(h.y - f.y)).First();
                report.entries.Add(new MapSupportReport.Entry { figure = fig.name + " @" + fig.parent.name, kind = kind, footY = f.y, supportY = ok ? best.y : float.NaN, support = ok ? Owner(best.owner) : "", snapped = ok && kind != "enemy" });
                if (ok && kind != "enemy") fig.position = new Vector3(f.x, best.y, f.z);
            }
            foreach (var prop in eventProps.GetComponentsInChildren<EventProp>(true))
            {
                if (prop.kind == StageChart.TypeForeshadow)
                    foreach (Transform c in prop.transform) if (c.name.StartsWith("Prop_foreshadow_")) Add(c, "foreshadow");
                if (prop.enemy != null) Add(prop.enemy, "runner");
            }
            foreach (var a in enemies.Normal) Add(a.transform, "enemy");
            foreach (var a in enemies.Surround) Add(a.transform, "enemy");
            if (enemies.Boss != null) Add(enemies.Boss.transform, "enemy");

            // 작업 R07: 추격 적 달리는 자리 프로필 — 실제 경로를 따라 0.5 m마다, 경로 높이 −3.5 m ~ +0.6 m 안의 가장 높은 보이는 면
            // (나무·등·깃발 등 빼고). 옆 거리는 사건 하나에 하나: 지붕 가장자리 안쪽부터 줄여 가며 85% 넘는 자리에서 면이 있는 가장 먼 값.
            // 면이 없는 자리(기와집 이음매 골 등)는 앞뒤 면 높이를 이어 준다.
            float? Surface(Vector3 q, float pathY)
            {
                float top = pathY < 0.3f ? pathY + 0.15f : pathY + 0.6f;   // 땅 길에서는 땅만 (집 기단 위로 달리지 않게)
                var all = HeightsAt(q);
                var hs = all.Where(h => h.y >= pathY - 3.5f && h.y <= top && !IsNotGround(Owner(h.owner))).ToList();
                if (hs.Count == 0) return null;
                float y = hs.Max(h => h.y);
                // 작업 R07 후속: 머리 위 2 m 안에 다른 면(좌판·처마·집)이 있으면 그 속을 지나가므로 못 씀
                if (all.Any(h => h.y > y + 0.3f && h.y < y + 2.0f)) return null;
                return y;
            }
            foreach (var prop in eventProps.GetComponentsInChildren<EventProp>(true))
            {
                string how = prop.companion != "" ? prop.companion : prop.kind;
                if (how == EventCatalog.Charge && prop.enemy != null) { ChargeProfile(prop); continue; }
                if (how != EventCatalog.Chase || prop.enemy == null) continue;
                float side = prop.slotSides.Length > 0 ? -prop.slotSides[0] : 1f;
                float step = 0.5f;
                float d0 = Mathf.Max(0f, (prop.runBeat - EventProp.ChaseBeatsBefore) * GameConfig.MetersPerBeat + 2f);
                float d1 = (prop.runBeat + prop.beats + EventProp.ChaseBeatsAfter) * GameConfig.MetersPerBeat + 2f;
                int n = Mathf.FloorToInt((d1 - d0) / step) + 1;
                float bestLat = 0.8f; float?[] best = null; int bestHits = -1;
                for (float l = prop.chaseLateral; l >= GameConfig.Map.ChaseMinLateral - 1e-3f; l -= 0.25f)
                {
                    var col = new float?[n];
                    int hits = 0;
                    for (int i = 0; i < n; i++)
                    {
                        var pt = MapLayout.PointAt(d0 + i * step, out var fwd);
                        col[i] = Surface(pt + MapLayout.RightOf(fwd) * (side * l), pt.y);
                        if (col[i].HasValue) hits++;
                    }
                    if (hits > bestHits) { bestHits = hits; best = col; bestLat = l; }
                    if (hits >= n * 0.97f) break;
                }
                var ys = new float[n];
                var gap = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    if (best[i].HasValue) { ys[i] = best[i].Value; continue; }
                    gap[i] = true;
                    int a = i - 1; while (a >= 0 && !best[a].HasValue) a--;
                    int b = i + 1; while (b < n && !best[b].HasValue) b++;
                    float pathY = MapLayout.PointAt(d0 + i * step).y;
                    ys[i] = a >= 0 && b < n ? Mathf.Lerp(best[a].Value, best[b].Value, (i - a) / (float)(b - a)) : a >= 0 ? best[a].Value : b < n ? best[b].Value : pathY;
                }
                prop.chaseD0 = d0;
                prop.chaseStep = step;
                prop.chaseY = ys;
                prop.chaseX = Enumerable.Repeat(bestLat, n).ToArray();
                // 면이 없는 틈(집과 집 사이)은 뛰어넘는다: 틈 길이에 맞춘 포물선으로 발을 들어 올림
                for (int i = 0; i < n; i++)
                {
                    if (!gap[i]) continue;
                    int a = i; while (a > 0 && gap[a - 1]) a--;
                    int b = i; while (b < n - 1 && gap[b + 1]) b++;
                    float len = (b - a + 2) * step;
                    float k = (i - a + 1) / (float)(b - a + 2);
                    ys[i] += Mathf.Min(GameConfig.Map.ChaseHopMax, len * 0.35f) * 4f * k * (1f - k);
                }
                // 높이가 1 m 넘게 갑자기 바뀌는 곳(담·지붕 오르내리기)은 플레이어 경로가 오르내리는 구간에 맞춰 함께 오른다
                var py = new float[n];
                for (int i = 0; i < n; i++) py[i] = MapLayout.PointAt(d0 + i * step).y;
                for (int i = 0; i < n - 1; i++)
                {
                    if (gap[i] || gap[i + 1] || Mathf.Abs(ys[i + 1] - ys[i]) <= 1f) continue;
                    int a = i, b = i + 1;
                    while (a > 0 && Mathf.Abs(py[a - 1] - py[a]) > 1e-3f) a--;
                    while (b < n - 1 && Mathf.Abs(py[b + 1] - py[b]) > 1e-3f) b++;
                    if (b - a < 4) { a = Mathf.Max(0, i - 3); b = Mathf.Min(n - 1, i + 4); }
                    float y0 = ys[a], y1 = ys[b];
                    for (int j = a + 1; j < b; j++)
                    {
                        float t = Mathf.Abs(py[b] - py[a]) > 0.1f ? (py[j] - py[a]) / (py[b] - py[a]) : (j - a) / (float)(b - a);
                        ys[j] = Mathf.Lerp(y0, y1, Mathf.Clamp01(t));
                        gap[j] = true;
                    }
                    i = b - 1;
                }
                prop.chaseGap = gap;
                Debug.Log($"[SceneBuilder] 추격 프로필 {prop.name}: {n}점, 옆 거리 {bestLat:0.00} m, 면 있음 {bestHits}/{n}");
                FarChase(prop, side, d0, step, n, bestLat);
            }

            // 작업 R18 (GDD-beta B19.2): 추격 적은 공격 ChaseLeapBeats박 전까지 옆 지붕 줄·골목(8~20 m)을 달리고, 그다음 플레이어 곁(위에서 잰 가까운 줄)으로
            // 뛰어든다. 먼 줄: 옆 거리 하나를 골라(면이 이어지고 바깥 모퉁이에서 너무 늘어나지 않는 가장 가까운 값) 경로를 옮긴 선을 접힘 없이 잇고
            // 길이를 고르게 나눠(일정한 속도) 보이는 가장 높은 면(지붕·담·땅, 머리 위 여유)에 발을 둔다. 뛰어드는 동안은 사이 지붕·담을 넘는 포물선.
            float? FarSurface(Vector3 q)
            {
                var all = HeightsAt(q);
                var hs = all.Where(h => h.y > -0.5f && h.y < 12f && !IsNotGround(Owner(h.owner))).ToList();
                if (hs.Count == 0) return null;
                float y = hs.Max(h => h.y);
                if (all.Any(h => h.y > y + 0.3f && h.y < y + 2.0f)) return null;
                return y;
            }
            void FarChase(EventProp prop, float side, float d0, float step, int n, float nearLat)
            {
                float mpb = GameConfig.MetersPerBeat;
                float hitBeat = prop.runBeat + (prop.slotOffsets.Length > 0 ? prop.slotOffsets[0] : 2f);
                float dHit = hitBeat * mpb + 2f;
                float dLeap = dHit - GameConfig.Map.ChaseLeapBeats * mpb;
                int iLeap = Mathf.Clamp(Mathf.RoundToInt((dLeap - d0) / step), 2, n - 2);
                int iHit = Mathf.Clamp(Mathf.RoundToInt((dHit - d0) / step), iLeap + 1, n - 1);
                int m = iLeap + 1;   // 먼 줄 칸 수 (0..iLeap)

                // 옆 거리 고르기
                List<Vector3> bestPts = null; float?[] bestYs = null; float bestL = 0f, bestScore = -9f;
                for (float L = GameConfig.Map.ChaseFarLateralMin; L <= GameConfig.Map.ChaseFarLateralMax + 1e-3f; L += GameConfig.Map.ChaseFarLateralStep)
                {
                    // 경로를 옮긴 선 (안쪽 모퉁이에서 뒤로 가는 점은 버림)
                    var raw = new List<Vector3>();
                    Vector3 last = Vector3.zero;
                    for (int i = 0; i < m; i++)
                    {
                        var pt = MapLayout.PointAt(d0 + i * step, out var fwd);
                        var q = pt + MapLayout.RightOf(fwd) * (side * L); q.y = 0f;
                        if (raw.Count > 0 && Vector3.Dot(q - last, fwd) < 0.05f) continue;
                        raw.Add(q); last = q;
                    }
                    if (raw.Count < 2) continue;
                    float len = 0f;
                    for (int i = 1; i < raw.Count; i++) len += Vector3.Distance(raw[i - 1], raw[i]);
                    float stretch = len / Mathf.Max(1f, (m - 1) * step);
                    var pts = Resample(raw, m);   // 길이를 고르게 m칸으로 (일정한 속도)
                    var ys = new float?[m];
                    int hits = 0;
                    for (int i = 0; i < m; i++) { ys[i] = FarSurface(pts[i]); if (ys[i].HasValue) hits++; }
                    float cover = hits / (float)m;
                    float score = cover - (stretch > GameConfig.Map.ChaseFarMaxStretch ? 1f : 0f);
                    if (score > bestScore + 1e-3f) { bestScore = score; bestPts = pts; bestYs = ys; bestL = L; }
                    if (cover >= 0.9f && stretch <= GameConfig.Map.ChaseFarMaxStretch) break;
                }
                if (bestPts == null) return;

                var wx = new float[n]; var wz = new float[n]; var lat = new float[n];
                var ysOut = (float[])prop.chaseY.Clone();
                var gapOut = (bool[])prop.chaseGap.Clone();
                // 먼 줄 높이: 면이 없는 칸은 앞뒤를 잇고 뛰어넘음
                var fy = new float[m]; var fg = new bool[m];
                for (int i = 0; i < m; i++)
                {
                    if (bestYs[i].HasValue) { fy[i] = bestYs[i].Value; continue; }
                    fg[i] = true;
                    int a = i - 1; while (a >= 0 && !bestYs[a].HasValue) a--;
                    int b = i + 1; while (b < m && !bestYs[b].HasValue) b++;
                    fy[i] = a >= 0 && b < m ? Mathf.Lerp(bestYs[a].Value, bestYs[b].Value, (i - a) / (float)(b - a)) : a >= 0 ? bestYs[a].Value : b < m ? bestYs[b].Value : 0f;
                }
                for (int i = 0; i < m; i++)
                {
                    if (!fg[i]) continue;
                    int a = i; while (a > 0 && fg[a - 1]) a--;
                    int b = i; while (b < m - 1 && fg[b + 1]) b++;
                    float glen = (b - a + 2) * step;
                    float k = (i - a + 1) / (float)(b - a + 2);
                    fy[i] += Mathf.Min(1.5f, glen * 0.35f) * 4f * k * (1f - k);
                }
                // 높이가 1 m 넘게 바뀌는 곳(지붕 ↔ 골목)은 앞뒤 3칸에 걸쳐 뛰어 오르내림
                for (int i = 0; i < m - 1; i++)
                {
                    if (Mathf.Abs(fy[i + 1] - fy[i]) <= 1f) continue;
                    int a = Mathf.Max(0, i - 3), b = Mathf.Min(m - 1, i + 4);
                    float y0 = fy[a], y1 = fy[b];
                    for (int j = a + 1; j < b; j++)
                    {
                        float k = (j - a) / (float)(b - a);
                        fy[j] = Mathf.Max(fy[j], Mathf.Lerp(y0, y1, k) + 0.6f * 4f * k * (1f - k));
                        fg[j] = true;
                    }
                    i = b - 1;
                }
                for (int i = 0; i < m; i++)
                {
                    wx[i] = bestPts[i].x; wz[i] = bestPts[i].z; ysOut[i] = fy[i]; gapOut[i] = fg[i];
                    var pt = MapLayout.PointAt(d0 + i * step, out var fwd);
                    lat[i] = Mathf.Abs(Vector3.Dot(bestPts[i] - new Vector3(pt.x, 0f, pt.z), MapLayout.RightOf(fwd)));
                }
                // 공격 뒤: 플레이어 곁 (가까운 줄, 높이는 위에서 잰 그대로)
                for (int i = iHit; i < n; i++)
                {
                    var pt = MapLayout.PointAt(d0 + i * step, out var fwd);
                    var q = pt + MapLayout.RightOf(fwd) * (side * nearLat);
                    wx[i] = q.x; wz[i] = q.z; lat[i] = nearLat;
                }
                // 뛰어들기: 먼 줄 끝 → 공격 자리, 사이 면을 넘는 포물선
                var from = new Vector3(wx[iLeap], ysOut[iLeap], wz[iLeap]);
                var to = new Vector3(wx[iHit], ysOut[iHit], wz[iHit]);
                float arc = GameConfig.Map.ChaseLeapMinArc;
                for (int s2 = 1; s2 < 24; s2++)
                {
                    float k = s2 / 24f;
                    var q = Vector3.Lerp(from, to, k);
                    var hs = HeightsAt(q).Where(h => h.y < 12f && !IsNotGround(Owner(h.owner))).ToList();
                    if (hs.Count == 0) continue;
                    // 여유는 가운데에서 다 주고 뛰는 자리·내리는 자리 쪽으로 줄인다 (발 딛은 지붕 자체를 넘으려고 포물선이 커지지 않게)
                    float need = hs.Max(h => h.y) + GameConfig.Map.ChaseLeapClearance * Mathf.Clamp01(k * (1f - k) * 8f) - q.y;
                    if (need > 0f) arc = Mathf.Max(arc, need / (4f * k * (1f - k)));
                }
                arc = Mathf.Min(arc, GameConfig.Map.ChaseLeapMaxArc);
                for (int i = iLeap + 1; i < iHit; i++)
                {
                    float k = (i - iLeap) / (float)(iHit - iLeap);
                    var q = Vector3.Lerp(from, to, k);
                    wx[i] = q.x; wz[i] = q.z;
                    ysOut[i] = q.y + arc * 4f * k * (1f - k);
                    gapOut[i] = true;
                    var pt = MapLayout.PointAt(d0 + i * step, out var fwd);
                    lat[i] = Mathf.Abs(Vector3.Dot(new Vector3(q.x - pt.x, 0f, q.z - pt.z), MapLayout.RightOf(fwd)));
                }
                prop.chaseWX = wx; prop.chaseWZ = wz; prop.chaseX = lat; prop.chaseY = ysOut; prop.chaseGap = gapOut;
                prop.chaseLeapD = d0 + iLeap * step; prop.chaseHitD = d0 + iHit * step;
                Debug.Log($"[SceneBuilder] 추격 먼 줄 {prop.name}: 옆 {bestL:0} m, 면 {bestYs.Count(y => y.HasValue)}/{m}, 뛰어들기 {iLeap}→{iHit}, 포물선 {arc:0.0} m");
            }

            // 작업 R07 후속: 돌진 적 — 옆 거리는 집 자리 그대로, 길을 따라 보이는 면 높이 (모자란 칸은 앞뒤를 잇고, 높이가 갑자기 바뀌면 플레이어 경로에 맞춰 오름)
            void ChargeProfile(EventProp prop)
            {
                float lat = prop.enemy.localPosition.x;
                float step = 0.5f;
                float d0 = Mathf.Max(0f, (prop.runBeat - EventProp.ChaseBeatsBefore) * GameConfig.MetersPerBeat - 40f);
                float d1 = (prop.runBeat + prop.beats + EventProp.ChaseBeatsAfter) * GameConfig.MetersPerBeat + 2f;
                int n = Mathf.FloorToInt((d1 - d0) / step) + 1;
                var col = new float?[n];
                var py = new float[n];
                for (int i = 0; i < n; i++)
                {
                    var pt = MapLayout.PointAt(d0 + i * step, out var fwd);
                    py[i] = pt.y;
                    col[i] = Surface(pt + MapLayout.RightOf(fwd) * lat, pt.y);
                }
                var ys = new float[n];
                var gap = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    if (col[i].HasValue) { ys[i] = col[i].Value; continue; }
                    gap[i] = true;
                    int a = i - 1; while (a >= 0 && !col[a].HasValue) a--;
                    int b = i + 1; while (b < n && !col[b].HasValue) b++;
                    ys[i] = a >= 0 && b < n ? Mathf.Lerp(col[a].Value, col[b].Value, (i - a) / (float)(b - a)) : a >= 0 ? col[a].Value : b < n ? col[b].Value : py[i];
                }
                for (int i = 0; i < n - 1; i++)
                {
                    if (gap[i] || gap[i + 1] || Mathf.Abs(ys[i + 1] - ys[i]) <= 1f) continue;
                    int a = i, b = i + 1;
                    while (a > 0 && Mathf.Abs(py[a - 1] - py[a]) > 1e-3f) a--;
                    while (b < n - 1 && Mathf.Abs(py[b + 1] - py[b]) > 1e-3f) b++;
                    if (b - a < 4) { a = Mathf.Max(0, i - 3); b = Mathf.Min(n - 1, i + 4); }
                    for (int j = a + 1; j < b; j++)
                    {
                        float t = Mathf.Abs(py[b] - py[a]) > 0.1f ? (py[j] - py[a]) / (py[b] - py[a]) : (j - a) / (float)(b - a);
                        ys[j] = Mathf.Lerp(ys[a], ys[b], Mathf.Clamp01(t));
                        gap[j] = true;
                    }
                    i = b - 1;
                }
                prop.chaseD0 = d0;
                prop.chaseStep = step;
                prop.chaseY = ys;
                prop.chaseX = Enumerable.Repeat(lat, n).ToArray();
                prop.chaseGap = gap;
                Debug.Log($"[SceneBuilder] 돌진 프로필 {prop.name}: {n}점, 옆 {lat:0.0} m, 면 있음 {col.Count(c => c.HasValue)}/{n}");
            }

            // 작업 R07: 지붕 위 적이 PERFECT 발차기에 떨어질 자리 — 플레이어 반대쪽에서 옆으로 돌린 여러 방향·거리 중 보이는 면이 가장 낮은 곳 (같으면 앞쪽)
            foreach (var a in enemies.Normal)
            {
                if (!a.onRoof) continue;
                var pos = a.transform.position;
                var away = -a.transform.forward; away.y = 0f; away.Normalize();   // 적은 플레이어를 본다
                float bestY = float.PositiveInfinity; Vector3 best = pos;
                foreach (float sd in new[] { 1f, -1f })
                    foreach (float ang in new[] { 50f, 65f, 80f, 95f, 110f })   // 앞쪽(1인칭 카메라에 보이는 쪽)부터
                        foreach (float dd in new[] { 7.5f, 9.5f, 6f, 12f })
                        {
                            var q = pos + Quaternion.AngleAxis(sd * ang, Vector3.up) * away * dd;
                            var hs = HeightsAt(q).Where(h => h.y <= pos.y - 0.3f && !IsNotGround(Owner(h.owner))).ToList();
                            float y = hs.Count > 0 ? hs.Max(h => h.y) : 0f;
                            if (y < bestY - 0.3f) { bestY = y; best = new Vector3(q.x, y, q.z); }
                        }
                a.hasKickLanding = true;
                a.kickLanding = best;
                Debug.Log($"[SceneBuilder] 지붕 발차기 착지 {a.name}: {pos} → {best} (낙차 {pos.y - best.y:0.0} m)");
            }

            // 작업 R07 후속: 지붕 위 전투(지붕 위 적·무사) 자리 둘레의 보이는 면 높이 격자 — 전투 중 발을 지붕 면에 붙이고 옆으로 갈 수 있는 곳을 정한다
            var roofFighters = enemies.Normal.Where(a => a.onRoof).ToList();
            if (enemies.Boss != null && enemies.Boss.onRoof) roofFighters.Add(enemies.Boss);
            foreach (var a in roofFighters)
            {
                int n = GameConfig.CombatArenaGridCells;
                float step = 0.5f;
                var pos = a.transform.position;
                var origin = new Vector3(pos.x - n * step * 0.5f, 0f, pos.z - n * step * 0.5f);
                var ys = new float[n * n];
                int found = 0;
                for (int z = 0; z < n; z++)
                    for (int x = 0; x < n; x++)
                    {
                        var q = new Vector3(origin.x + x * step, pos.y, origin.z + z * step);
                        var hs = HeightsAt(q).Where(h => h.y >= pos.y - 2.5f && h.y <= pos.y + 0.3f && !IsNotGround(Owner(h.owner))).ToList();
                        ys[z * n + x] = hs.Count > 0 ? hs.Max(h => h.y) : float.NaN;
                        if (hs.Count > 0) found++;
                    }
                a.arenaOrigin = origin;
                a.arenaStep = step;
                a.arenaN = n;
                a.arenaY = ys;
                Debug.Log($"[SceneBuilder] 지붕 전투 높이 격자 {a.name}: {n}×{n}, 면 있음 {found}, 가운데 {a.ArenaSurface(pos):0.00} (상자 높이 {pos.y:0.00})");
            }

            // 작업 R07: 매복 적은 옆(3.2 m)에서 길 쪽(1 m)으로 튀어나온다 — 지붕 비탈에서는 들어올수록 면이 높아지므로 끝자리 면 높이를 잰다
            foreach (var prop in eventProps.GetComponentsInChildren<EventProp>(true))
            {
                string how = prop.companion != "" ? prop.companion : prop.kind;
                if (how != EventCatalog.Ambush || prop.enemy == null) continue;
                float side = prop.slotSides.Length > 0 ? prop.slotSides[0] : 1f;
                float z = (float)((prop.slotOffsets.Length > 0 ? prop.slotOffsets[0] : 2f) - prop.beats * 0.5f) * GameConfig.MetersPerBeat;
                float homeY = prop.enemy.position.y;
                var q = prop.transform.TransformPoint(new Vector3(-side * 1.0f, 0f, z));
                var hs = HeightsAt(q).Where(h => Mathf.Abs(h.y - homeY) < 1.5f && !IsNotGround(Owner(h.owner))).ToList();
                prop.ambushEndY = hs.Count > 0 ? hs.OrderBy(h => Mathf.Abs(h.y - homeY)).First().y - prop.transform.position.y : prop.enemy.localPosition.y;
            }
        }

        /// <summary>작업 R18: 꺾인 선을 길이가 고른 count개 점으로 다시 나눈다.</summary>
        static List<Vector3> Resample(List<Vector3> pts, int count)
        {
            var cum = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++) cum[i] = cum[i - 1] + Vector3.Distance(pts[i - 1], pts[i]);
            var o = new List<Vector3>(count);
            int j = 0;
            for (int i = 0; i < count; i++)
            {
                float s = cum[cum.Length - 1] * i / Mathf.Max(1, count - 1);
                while (j < pts.Count - 2 && cum[j + 1] < s) j++;
                float seg = cum[j + 1] - cum[j];
                o.Add(Vector3.Lerp(pts[j], pts[j + 1], seg > 1e-5f ? (s - cum[j]) / seg : 0f));
            }
            return o;
        }

        /// <summary>달리는 발판으로 쓰지 않는 장식 (나무 꼭대기·등·깃발·빨랫줄 등).</summary>
        static bool IsNotGround(string owner)
        {
            foreach (var k in new[] { "pine", "zelkova", "lantern", "banner", "clothes", "rope", "jangseung", "cloth_rack", "straw", "jige", "firewood", "basket" })
                if (owner.Contains(k)) return true;
            return false;
        }

        /// <summary>
        /// 드로우콜 줄이기 (작업 T15): 장식을 (메시, 머티리얼, 그림자)마다 모아 DressingInstancer가 GPU 인스턴싱으로 그린다.
        /// 이름이 _s1로 끝나는 것(건물·담·큰 나무)만 그림자를 드리운다. 원래 장식 물체는 위치 기록용 빈 물체(Dress_*)로 남긴다.
        /// (메시를 합쳐 에셋으로 저장하면 225 MB가 되어 저장소에 맞지 않아 이 방법을 쓴다.)
        /// </summary>
        static void MergeDressing(Transform parent)
        {
            var inst = parent.gameObject.AddComponent<DressingInstancer>();
            var map = new Dictionary<(Mesh, int, Material, bool, int, int), (List<Matrix4x4> m, Bounds b)>();
            var smallProps = new HashSet<Mesh>();   // 그림자 없는 작은 소품: 먼 칸은 그리지 않음
            foreach (var t in parent.Cast<Transform>().Where(t => t.name.StartsWith("Dress_")).ToList())
            {
                bool shadow = t.name.EndsWith("_s1");
                bool small = t.name.Contains("_p_") && !shadow;
                // 작업 R03: GameConfig.Map.DressCell 칸마다 따로 묶어 칸 경계로 컬링 (뒤·그림자 거리 밖 칸은 그리지 않음)
                float cell = small ? GameConfig.Map.PropCell : GameConfig.Map.DressCell;   // 작은 소품은 잘게 나눠 먼 칸을 거리로 뺀다
                int cx = Mathf.FloorToInt(t.position.x / cell), cz = Mathf.FloorToInt(t.position.z / cell);
                foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mr == null) continue;
                    var wb = mr.bounds;
                    if (small) smallProps.Add(mf.sharedMesh);
                    for (int sm = 0; sm < mf.sharedMesh.subMeshCount; sm++)
                    {
                        var key = (mf.sharedMesh, sm, mr.sharedMaterials[Mathf.Min(sm, mr.sharedMaterials.Length - 1)], shadow, cx, cz);
                        if (!map.TryGetValue(key, out var e)) e = (new List<Matrix4x4>(), wb);
                        e.m.Add(mf.transform.localToWorldMatrix);
                        e.b.Encapsulate(wb);
                        map[key] = e;
                    }
                }
                foreach (var c in t.GetComponentsInChildren<Transform>(true).Where(c => c != t).ToList()) Object.DestroyImmediate(c.gameObject);
            }
            foreach (var kv in map)
                inst.groups.Add(new DressingInstancer.Group { mesh = kv.Key.Item1, subMesh = kv.Key.Item2, material = kv.Key.Item3, castShadows = kv.Key.Item4, matrices = kv.Value.m.ToArray(), bounds = kv.Value.b,
                    maxDistance = smallProps.Contains(kv.Key.Item1) ? GameConfig.Map.PropDrawDistance : 0f });
            parent.gameObject.isStatic = false;
        }

        // ---------- 외곽 그림 원통·하늘·해·안개 ----------

        static void BuildBackdrop(Transform map)
        {
            // 작업 R03: R02에서 그린 배경 6장을 이어 붙인 파노라마 한 장 (harness/scripts/map/make-backdrop.py) — 둘레를 한 번만 돌아 같은 그림이 되풀이되지 않는다
            Directory.CreateDirectory(BackdropDir);
            string texPath = PanoramaPath;
            if (AssetImporter.GetAtPath(texPath) is TextureImporter ti && (ti.wrapModeU != TextureWrapMode.Repeat || ti.maxTextureSize != 8192 || ti.npotScale != TextureImporterNPOTScale.None))
            {
                ti.wrapModeU = TextureWrapMode.Repeat; ti.wrapModeV = TextureWrapMode.Clamp;
                ti.mipmapEnabled = true; ti.maxTextureSize = 8192; ti.npotScale = TextureImporterNPOTScale.None;
                ti.SaveAndReimport();
            }
            string matPath = $"{BackdropDir}/Backdrop.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(Shader.Find("RhythmActionGame/Backdrop")); AssetDatabase.CreateAsset(mat, matPath); }
            mat.shader = Shader.Find("RhythmActionGame/Backdrop");
            mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetColor("_FogColor", FogColor);
            mat.SetFloat("_TopFade", GameConfig.Map.BackdropTopFade);
            EditorUtility.SetDirty(mat);

            string meshPath = $"{BackdropDir}/BackdropCylinder.asset";
            var mesh = Cylinder(96, GameConfig.Map.BackdropRadius, GameConfig.Map.BackdropHeight);
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);
            var go = new GameObject("Backdrop", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(map, false);
            var c = MapCenter();
            go.transform.position = new Vector3(c.x, -GameConfig.Map.BackdropSink, c.z);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.isStatic = true;
        }

        static Vector3 MapCenter()
        {
            var pts = Enumerable.Range(0, 40).Select(i => MapLayout.FlatPointAt(MapLayout.TotalLength * i / 39f, out _)).ToList();
            return new Vector3((pts.Min(p => p.x) + pts.Max(p => p.x)) * 0.5f, 0, (pts.Min(p => p.z) + pts.Max(p => p.z)) * 0.5f);
        }

        static Mesh Cylinder(int seg, float radius, float height)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                v.Add(d); uv.Add(new Vector2(i / (float)seg, 0f));                      // 파노라마가 둘레를 한 번 돈다 (작업 R03)
                v.Add(d + Vector3.up * height); uv.Add(new Vector2(i / (float)seg, 1f));
            }
            for (int i = 0; i < seg; i++)
            {
                int k = i * 2;
                tri.AddRange(new[] { k, k + 1, k + 2, k + 1, k + 3, k + 2 });           // 안쪽에서 보이게 (셰이더 Cull Off)
            }
            var m = new Mesh { name = "BackdropCylinder" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static readonly Color FogColor = new Color(0.86f, 0.68f, 0.56f);

        /// <summary>해 질 녘 해·주변광·하늘·안개.</summary>
        static void BuildAtmosphere(Light sun)
        {
            sun.transform.rotation = Quaternion.Euler(GameConfig.Map.SunPitch, GameConfig.Map.SunYaw, 0f);
            sun.color = new Color(1f, 0.74f, 0.52f);
            sun.intensity = GameConfig.Map.SunIntensity;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.55f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.5f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.2f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = GameConfig.Map.FogStart;
            RenderSettings.fogEndDistance = GameConfig.Map.FogEnd;
            string skyPath = $"{BackdropDir}/DuskSky.mat";
            Directory.CreateDirectory(BackdropDir);
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null) { sky = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(sky, skyPath); }
            sky.SetFloat("_SunSize", 0.05f);
            sky.SetFloat("_AtmosphereThickness", 1.6f);
            sky.SetColor("_SkyTint", new Color(0.62f, 0.55f, 0.6f));
            sky.SetColor("_GroundColor", FogColor);
            sky.SetFloat("_Exposure", 1.15f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
        }
    }
}
