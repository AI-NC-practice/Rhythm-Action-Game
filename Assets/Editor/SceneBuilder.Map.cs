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

        /// <summary>
        /// 작업 R41 (사용자 피드백 2026-11-24 6번 "아래로 피하는 거 부딪히면서 가는 것 같아요"): 아래로 피하기 장애물(빨랫줄·낮은 처마)의
        /// 기둥만 늘려 밑면(달리는 길 위 가운데 ±corridorHalf m에서 가장 낮은 곳)을 target m로 올린다. 밑면보다 위(옷·처마 지붕)는 모양 그대로 올리고,
        /// 그 아래(기둥)만 세로로 늘린다. 장애물 축(root)은 길 바닥·수평 회전만이라고 본다. 같은 모양은 메시를 한 번만 만들어 나눠 쓴다.
        /// </summary>
        static void LiftOverhang(Transform root, Transform model, float corridorHalf, float target)
        {
            var mfs = model.GetComponentsInChildren<MeshFilter>(true);
            float u0 = float.PositiveInfinity;
            foreach (var mf in mfs)
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var p = root.InverseTransformPoint(mf.transform.TransformPoint(v));
                    if (Mathf.Abs(p.x) < corridorHalf && p.y > 0.15f) u0 = Mathf.Min(u0, p.y);
                }
            if (float.IsPositiveInfinity(u0) || u0 >= target) return;
            float cut = u0 - 0.02f, newCut = target - 0.02f;
            const string dir = "Assets/Art/Models/_derived";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Art/Models", "_derived");
            foreach (var mf in mfs)
            {
                var src = mf.sharedMesh;
                string path = $"{dir}/{src.name}_lift{Mathf.RoundToInt(target * 100)}_{Mathf.RoundToInt(cut * 1000)}.asset";
                var lifted = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (lifted == null)
                {
                    lifted = Object.Instantiate(src);
                    lifted.name = System.IO.Path.GetFileNameWithoutExtension(path);
                    var vs = src.vertices;
                    for (int i = 0; i < vs.Length; i++)
                    {
                        var p = root.InverseTransformPoint(mf.transform.TransformPoint(vs[i]));
                        p.y = p.y < cut ? p.y * newCut / cut : p.y + (newCut - cut);
                        vs[i] = mf.transform.InverseTransformPoint(root.TransformPoint(p));
                    }
                    lifted.vertices = vs;
                    lifted.RecalculateBounds();
                    AssetDatabase.CreateAsset(lifted, path);
                }
                mf.sharedMesh = lifted;
            }
        }

        /// <summary>
        /// 작업 R13 (사용자 피드백 2026-11-16 2번·2026-11-24 8번 "지붕 위에 밑으로 피하는 거 어색"): 지붕 길 위 아래로 피하기 장애물.
        /// 지붕 위에 기둥 달린 낮은 처마를 올려놓는 대신, 길 옆 땅에 선 더 높은 건물(id)의 처마가 지붕 길 위로 나와 있게 한다.
        /// 모델 꼭짓점으로 크기(k)와 옆 거리(s)를 찾는다: 길 가운데 ±corridorHalf m·앞뒤 ±1.5 m 위에서 가장 낮은 면 = 길 + target m(±0.12),
        /// 그 아래(길 높이 ~ target)에는 건물이 없음(기둥·난간이 길을 막지 않음). 건물 바닥은 땅(길 높이 − pathY). 찾은 (k, s)를 돌려준다.
        /// </summary>
        static (float k, float s, float u)? PlaceOverhangBuilding(Transform root, string id, float realHeight, float side, float pathY, float corridorHalf, float target)
        {
            var w = Wrap(id, root, $"Overhang_{id}");
            // 긴 축(감싼 물체 x)을 길 방향(root z)으로
            var rot = Quaternion.Euler(0f, 90f, 0f);
            var local = new List<Vector3>();
            foreach (var mf in w.Go.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = mf.sharedMesh; var vs = m.vertices; var tris = m.triangles;
                var pts = vs.Select(v => w.Go.transform.InverseTransformPoint(mf.transform.TransformPoint(v))).ToArray();
                local.AddRange(pts);
                for (int t = 0; t + 2 < tris.Length; t += 3) local.Add((pts[tris[t]] + pts[tris[t + 1]] + pts[tris[t + 2]]) / 3f);   // 큰 삼각형도 잡게 무게중심
            }
            (float k, float s, float u)? best = null;
            float unit = realHeight / Mathf.Max(0.01f, w.Size.y);   // 모델 단위 → m (실제 높이 기준), k는 그 배율 0.6~1.5
            var diag = new System.Text.StringBuilder();
            for (float kk = 0.6f; kk <= 1.5f + 1e-3f; kk += 0.025f)
                for (float s = 25f; s >= 0.5f; s -= 0.1f)
                {
                    float u = float.PositiveInfinity; bool blocked = false;
                    float k = kk * unit;
                    foreach (var v in local)
                    {
                        var p = rot * (v * k) + new Vector3(side * s, -pathY, 0f);
                        if (Mathf.Abs(p.x) >= corridorHalf || Mathf.Abs(p.z) > 1.5f || p.y < -0.3f) continue;
                        if (p.y < target - 0.12f) { blocked = true; break; }
                        u = Mathf.Min(u, p.y);
                    }
                    if (blocked) { if (Mathf.Abs(kk * 10f - Mathf.Round(kk * 10f)) < 0.01f) diag.Append($" k{kk:0.0}:막힘@{s:0.0}"); break; }   // 더 가까이 오면 더 막힌다
                    if (float.IsPositiveInfinity(u)) continue;
                    if (u > target + 0.12f && Mathf.Abs(kk * 10f - Mathf.Round(kk * 10f)) < 0.01f && Mathf.Abs(s * 2f - Mathf.Round(s * 2f)) < 0.01f && diag.Length < 4000) diag.Append($" k{kk:0.0}s{s:0.0}u{u:0.00}");
                    if (Mathf.Abs(u - target) <= 0.12f) { if (best == null || Mathf.Abs(kk - 1f) < Mathf.Abs(best.Value.k / unit - 1f)) best = (k, s, u); break; }
                }
            if (best == null) { Debug.LogWarning($"[R13] {id} 옆 {side}: 처마 자리 없음 (길 높이 {pathY:0.00}, 목표 {target:0.00}).{diag}"); Object.DestroyImmediate(w.Go); return null; }
            var b = best.Value;
            w.Go.transform.localRotation = rot;
            w.Go.transform.localPosition = new Vector3(side * b.s, -pathY, 0f);
            w.Go.transform.localScale = Vector3.one * b.k;
            foreach (var t in w.Go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
            return best;
        }

        /// <summary>
        /// 작업 R42: 뒤돌아보기 막는 것 한 조각 (움직이므로 정적 아님, 충돌체 없음). 감싼 물체 원점 = 모델 바닥 가운데.
        /// size.x·z가 0이면 높이(size.y)에 맞춰 같은 비율로, 아니면 가로·높이·깊이에 맞춘다.
        /// </summary>
        static void BarricadePiece(Transform root, string id, string name, Vector3 size)
        {
            var w = Wrap(id, root, name);
            var s = size.x <= 0f ? Vector3.one * (size.y / w.Size.y) : new Vector3(size.x / w.Size.x, size.y / w.Size.y, size.z / w.Size.z);
            w.Go.transform.localScale = s;
            foreach (var t in w.Go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = false;
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
        const float LookBackRoofStep = 0.25f;
        static (float[] d0, int n, float[] ys) LookBackRoofProfile;

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

            // 작업 R29 (평가 2회차): 지붕 위 뒤돌아보기 컷의 쫓는 무리 발 높이 — 지붕 길 충돌 상자는 보이는 기와보다 2 m 넘게 높은 곳이 있어
            // 실행 중 광선으로는 떠 보였다. 컷 구간 경로를 0.5 m마다, 무리마다 옆 거리에서 보이는 면 높이를 잰다 (없으면 NaN = 뛰어넘는 중).
            {
                var beats = GameConfig.Fx.LookBackRunBeats;
                int K = GameConfig.Fx.PursuerGaps.Length;
                float step = LookBackRoofStep;
                float maxGap = GameConfig.Fx.PursuerGaps.Max();
                int n = 0;
                var d0s = new float[beats.Length];
                for (int i = 0; i < beats.Length; i++)
                {
                    d0s[i] = float.NaN;
                    if (!LookBackCut.OnRoof(beats[i])) continue;
                    float from = (float)((beats[i] - GameConfig.Fx.PursuerLeadBeats) * GameConfig.MetersPerBeat) - maxGap - 1f;
                    float to = (float)((beats[i] + LookBackCut.LengthOf(i) + 1.5) * GameConfig.MetersPerBeat) - 2f;
                    d0s[i] = Mathf.Max(0f, from);
                    n = Mathf.Max(n, Mathf.CeilToInt((to - d0s[i]) / step) + 1);
                }
                var ys = new float[beats.Length * K * Mathf.Max(1, n)];
                for (int x = 0; x < ys.Length; x++) ys[x] = float.NaN;
                int found = 0, total = 0;
                for (int i = 0; i < beats.Length; i++)
                {
                    if (float.IsNaN(d0s[i])) continue;
                    for (int k = 0; k < K; k++)
                    {
                        float lat = GameConfig.Fx.PursuerLaterals[k % GameConfig.Fx.PursuerLaterals.Length];
                        for (int j = 0; j < n; j++)
                        {
                            float d = d0s[i] + j * step;
                            var at = MapLayout.PointAt(d, out var fwd);
                            float l = at.y > GameConfig.Fx.PursuerRoofHeight ? lat * GameConfig.Fx.PursuerRoofLateralScale : lat;
                            var q = at + MapLayout.RightOf(fwd) * l;
                            var s = Surface(q, at.y);
                            total++;
                            if (s.HasValue) { ys[(i * K + k) * n + j] = s.Value; found++; }
                        }
                    }
                }
                LookBackRoofProfile = (d0s, n, ys);
                Debug.Log($"[SceneBuilder] R29 지붕 위 뒤돌아보기 쫓는 무리 발 높이: {found}/{total}칸에 보이는 면");
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
                for (int i = 0; i < n; i++) if (col[i].HasValue) ys[i] = col[i].Value;
                // 사용자 버그 신고 2026-11-17 (2챕터 낮은 담, 5챕터 궁궐 담을 뚫음): 면이 없는 칸(길 위 장애물·담 밑)은
                // 높이 차가 작으면 그 안의 가장 높은 면을 발로 넘는 포물선으로 뛰어넘고, 크면 높은 쪽 높이를 지킨 뒤 아래 단계에서 뛰어내린다.
                for (int i = 0; i < n; i++)
                {
                    if (col[i].HasValue) continue;
                    int a = i; while (a < n && !col[a].HasValue) a++;   // [i, a) 이 빈 칸
                    int L = i - 1, R = a;
                    float yL = L >= 0 ? col[L].Value : (R < n ? col[R].Value : py[i]);
                    float yR = R < n ? col[R].Value : yL;
                    if (Mathf.Abs(yL - yR) <= 1f)
                    {
                        float apex = 0.3f;
                        for (int j = i; j < a; j++)
                        {
                            float k = (j - L) / (float)(R - L);
                            float b0 = Mathf.Lerp(yL, yR, k);
                            var pt = MapLayout.PointAt(d0 + j * step, out var fwd);
                            var tops = HeightsAt(pt + MapLayout.RightOf(fwd) * lat).Where(h => h.y > b0 - 0.5f && h.y < b0 + 3f && !IsNotGround(Owner(h.owner))).ToList();
                            if (tops.Count > 0) apex = Mathf.Max(apex, (tops.Max(h => h.y) + 0.15f - b0) / Mathf.Max(0.2f, 4f * k * (1f - k)));
                        }
                        apex = Mathf.Min(apex, 3.5f);
                        for (int j = i; j < a; j++) { float k = (j - L) / (float)(R - L); ys[j] = Mathf.Lerp(yL, yR, k) + apex * 4f * k * (1f - k); gap[j] = true; }
                    }
                    else
                    {
                        float hi = Mathf.Max(yL, yR);
                        for (int j = i; j < a; j++) { ys[j] = hi; gap[j] = true; }
                    }
                    i = a - 1;
                }
                // 높이가 1 m 넘게 갑자기 바뀌는 곳: 높은 면 끝까지 그 높이로 가고, 낮은 쪽 1.5 m에서 떨어지는 포물선 (벽 속을 비스듬히 지나가지 않게)
                for (int i = 0; i < n - 1; i++)
                {
                    if (Mathf.Abs(ys[i + 1] - ys[i]) <= 1f) continue;
                    bool upAhead = ys[i + 1] > ys[i];
                    int h = upAhead ? i + 1 : i, sgn = upAhead ? -1 : 1;
                    float hiY = ys[h];
                    // 높은 면(담·지붕 덮개)이 이어지는 칸까지는 그 위를 걷는다 (길 경사로 기준으로는 빠져도 보이는 덮개가 있으면)
                    for (int k = 1; k <= 8; k++)
                    {
                        int j = h + sgn;
                        if (j < 0 || j >= n) break;
                        var pt = MapLayout.PointAt(d0 + j * step, out var fwd);
                        var on = HeightsAt(pt + MapLayout.RightOf(fwd) * lat).Where(hh => Mathf.Abs(hh.y - hiY) < 0.4f && !IsNotGround(Owner(hh.owner))).ToList();
                        if (on.Count == 0) break;
                        ys[j] = on.Max(hh => hh.y); gap[j] = false; h = j; hiY = ys[j];
                    }
                    int last = h;
                    // 떨어지는 길이: 처마·담 지붕이 높은 면보다 넓게 나와 있으면 그 밖까지 (발이 아래 면보다 늘 위) — 1.5 m부터 늘려 봄
                    var lo0 = new float[9];
                    for (int k = 1; k <= 8; k++) { int j = h + sgn * k; lo0[k] = j >= 0 && j < n ? ys[j] : ys[h + sgn * (k - 1) >= 0 && h + sgn * (k - 1) < n ? h + sgn * (k - 1) : h]; }
                    int len = 3;
                    for (; len <= 8; len++)
                    {
                        bool clear = true;
                        for (int k = 1; k <= len && clear; k++)
                        {
                            int j = h + sgn * k;
                            if (j < 0 || j >= n) break;
                            float t = k / (float)len;
                            float y = Mathf.Max(lo0[k], hiY + 0.25f * 4f * t * (1f - t) - (hiY - lo0[k]) * t * t);
                            var pt = MapLayout.PointAt(d0 + j * step, out var fwd);
                            var above = HeightsAt(pt + MapLayout.RightOf(fwd) * lat).Where(hh => hh.y > y + 0.05f && hh.y < hiY + 1.0f && !IsNotGround(Owner(hh.owner)));
                            if (above.Any() && k < len) clear = false;
                        }
                        if (clear) break;
                    }
                    len = Mathf.Min(len, 8);
                    for (int k = 1; k <= len; k++)
                    {
                        int j = h + sgn * k;
                        if (j < 0 || j >= n) break;
                        float t = k / (float)len;
                        ys[j] = Mathf.Max(lo0[k], hiY + 0.25f * 4f * t * (1f - t) - (hiY - lo0[k]) * t * t);
                        gap[j] = true;
                        last = j;
                    }
                    i = Mathf.Max(i, Mathf.Max(h, last));
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

            // 작업 R18b (GDD-beta B19.3·B19.4): 매복 적·땅 위 전투 적이 숨어 있을 자리 — 플레이어 눈(경로 + 1.6 m)에서 보이는 메시에 가려지고,
            // 나올 자리까지 막힘 없이 달려갈 수 있는 가장 가까운 곳. 보이는 메시로 임시 충돌체를 만들어 시선을 쏜다.
            string hideFail = "";
            var rayRoot = new GameObject("R18b_HideRays");
            const int rayLayer = 30;
            foreach (var s in surfaces)
            {
                var m = new Mesh { indexFormat = s.v.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                m.vertices = s.v; m.triangles = s.tri;
                var go = new GameObject("ray") { layer = rayLayer };
                go.transform.SetParent(rayRoot.transform, false);
                go.AddComponent<MeshCollider>().sharedMesh = m;
            }
            bool oldBack = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            Physics.SyncTransforms();
            int mask = 1 << rayLayer;
            bool Blocked(Vector3 a, Vector3 b) => Physics.Linecast(a, b, mask, QueryTriggerInteraction.Ignore);
            // 발 높이: 기준 높이 근처(−3 ~ +1.5 m)의 가장 가까운 보이는 면 (나무·등 빼고), 머리 위 2 m 여유
            float? Ground(Vector3 q, float refY)
            {
                var all = HeightsAt(q);
                var hs = all.Where(h => h.y >= refY - 3f && h.y <= refY + 1.5f && !IsNotGround(Owner(h.owner))).ToList();
                if (hs.Count == 0) return null;
                float y = hs.OrderBy(h => Mathf.Abs(h.y - refY)).First().y;
                if (all.Any(h => h.y > y + 0.3f && h.y < y + 2.0f)) return null;
                return y;
            }
            // 숨은 자리 → 나올 자리 길: 0.5 m마다 발 높이가 있고 0.6 m 넘게 갑자기 바뀌지 않으며, 무릎·가슴 높이에 막힌 것이 없음
            float[] ClearPath(Vector3 from, Vector3 to)
            {
                float len = new Vector2(to.x - from.x, to.z - from.z).magnitude;
                int n = Mathf.Max(2, Mathf.CeilToInt(len / 0.5f) + 1);
                var ys = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float k = i / (float)(n - 1);
                    var q = Vector3.Lerp(from, to, k);
                    var g = Ground(q, i == 0 ? from.y : ys[i - 1]);
                    if (!g.HasValue) return null;
                    ys[i] = g.Value;
                    if (i > 0 && Mathf.Abs(ys[i] - ys[i - 1]) > 0.6f) return null;
                }
                ys[0] = from.y; ys[n - 1] = to.y;
                for (int i = 1; i < n; i++)
                {
                    var a = Vector3.Lerp(from, to, (i - 1) / (float)(n - 1)); a.y = ys[i - 1];
                    var b = Vector3.Lerp(from, to, i / (float)(n - 1)); b.y = ys[i];
                    foreach (float h in new[] { 0.6f, 1.4f })
                        if (Blocked(a + Vector3.up * h, b + Vector3.up * h) || Blocked(b + Vector3.up * h, a + Vector3.up * h)) return null;
                }
                return ys;
            }
            // 플레이어 눈 자리들 (경로 beat0~beat1, 0.5박 간격) 중 이 자리가 보이는 수: 화각(앞 ±55°) 안이고 70 m 안에서 몸 다섯 점 중 하나라도 안 가려짐
            List<(Vector3 eye, Vector3 fwd)> Eyes(float beat0, float beat1)
            {
                var list = new List<(Vector3, Vector3)>();
                for (float b = Mathf.Max(0f, beat0); b <= beat1 + 1e-3f; b += 0.5f)
                {
                    var pt = MapLayout.PointAt(b * GameConfig.MetersPerBeat, out var fwd);
                    list.Add((pt + Vector3.up * GameConfig.Map.HideEyeHeight, fwd));
                }
                return list;
            }
            int SeenCount(Vector3 foot, List<(Vector3 eye, Vector3 fwd)> eyes)
            {
                int seen = 0;
                foreach (var (eye, fwd) in eyes)
                {
                    var to = foot + Vector3.up * 1.1f - eye;
                    if (to.magnitude > 70f || Vector3.Angle(Vector3.ProjectOnPlane(to, Vector3.up), fwd) > 55f) continue;
                    var side = Vector3.Cross(Vector3.up, to).normalized * 0.3f;
                    var eyeSide = Vector3.Cross(Vector3.up, fwd).normalized * 0.6f;
                    bool any = false;
                    // 눈 자리 여럿 (카메라 높이·옆 흔들림·오르내리는 동작 여유): 하나라도 보이면 보임
                    foreach (var e2 in new[] { eye, eye + Vector3.up * 0.6f, eye - Vector3.up * 0.6f, eye + eyeSide, eye - eyeSide })
                    {
                        foreach (var t in new[] { foot + Vector3.up * 0.5f, foot + Vector3.up * 1.1f, foot + Vector3.up * 1.7f, foot + Vector3.up * 1.2f + side, foot + Vector3.up * 1.2f - side })
                            if (!Blocked(e2, t)) { any = true; break; }
                        if (any) break;
                    }
                    if (any) seen++;
                }
                return seen;
            }
            // 뛰어 넘는 길 (작업 R18b 둘째 방법): 골목·담 뒤에서 처마·담을 넘어 뛰어오름. 사이 면을 넘는 포물선, 몸(발·가슴·머리)이 막히지 않음
            float[] JumpPath(Vector3 from, Vector3 to)
            {
                const int n = 16;
                float arc = 0.6f;
                for (int i = 1; i < n; i++)
                {
                    float k = i / (float)n;
                    var q = Vector3.Lerp(from, to, k);
                    var hs = HeightsAt(q).Where(h => h.y < q.y + 8f && !IsNotGround(Owner(h.owner))).ToList();
                    if (hs.Count == 0) continue;
                    float need = hs.Max(h => h.y) + 0.4f - q.y;
                    if (need > 0f) arc = Mathf.Max(arc, need / (4f * k * (1f - k)));
                }
                if (arc > 7f) return null;
                var ys = new float[n + 1];
                for (int i = 0; i <= n; i++) { float k = i / (float)n; ys[i] = Mathf.Lerp(from.y, to.y, k) + arc * 4f * k * (1f - k); }
                for (int i = 1; i <= n; i++)
                {
                    var a = Vector3.Lerp(from, to, (i - 1) / (float)n); a.y = ys[i - 1];
                    var b = Vector3.Lerp(from, to, i / (float)n); b.y = ys[i];
                    foreach (float h in new[] { 0.3f, 1.0f, 1.6f })
                        if (Blocked(a + Vector3.up * h, b + Vector3.up * h) || Blocked(b + Vector3.up * h, a + Vector3.up * h)) return null;
                }
                return ys;
            }
            // 나오는 모습이 화면에 들어옴: revealEyes 중 하나에서 나오는 길 40~85% 자리의 몸(가슴)이 화각(앞 ±35°, 위아래 ±25°) 안이고 가려지지 않음
            bool Revealed(Vector3 from, Vector3 to, float[] ys, List<(Vector3 eye, Vector3 fwd)> revealEyes, float enemySide)
            {
                if (revealEyes == null) return true;
                foreach (var (eye, fwd) in revealEyes)
                    foreach (float k in new[] { 0.15f, 0.3f, 0.45f, 0.6f, 0.75f, 0.85f })
                    {
                        var q = Vector3.Lerp(from, to, k);
                        float f = k * (ys.Length - 1); int i = Mathf.Min((int)f, ys.Length - 2);
                        q.y = Mathf.Lerp(ys[i], ys[i + 1], f - i) + 1.1f;
                        var d = q - eye;
                        if (Vector3.Angle(Vector3.ProjectOnPlane(d, Vector3.up), fwd) > 35f || Mathf.Abs(Vector3.SignedAngle(Vector3.ProjectOnPlane(d, Vector3.up), d, Vector3.Cross(d, Vector3.up))) > 25f) continue;   // 카메라가 길 앞을 미리 보며 20°쯤 돌고 10°쯤 숙이므로 좁게
                        if (!Blocked(eye, q) && !Blocked(eye - Vector3.up * 0.8f, q)) return true;   // 낮은 눈(뛰어내리는 중)에서도 보여야 함
                    }
                return false;
            }
            (Vector3 pos, float[] ys, bool jump)? FindHide(Vector3 end, Vector3 fwd, float sideSign, float minD, float maxD, List<(Vector3 eye, Vector3 fwd)> eyes, bool anyDirection, bool allowJump = false, List<(Vector3 eye, Vector3 fwd)> revealEyes = null)
            {
                var r1 = FindHideOnce(end, fwd, sideSign, minD, maxD, eyes, anyDirection, false, revealEyes);
                if (r1.HasValue || !allowJump) return r1;
                return FindHideOnce(end, fwd, sideSign, minD, Mathf.Max(maxD, GameConfig.Map.AmbushHideMaxJump), eyes, anyDirection, true, revealEyes);
            }
            (Vector3 pos, float[] ys, bool jump)? FindHideOnce(Vector3 end, Vector3 fwd, float sideSign, float minD, float maxD, List<(Vector3 eye, Vector3 fwd)> eyes, bool anyDirection, bool jump, List<(Vector3 eye, Vector3 fwd)> revealEyes)
            {
                int noGround = 0, seenN = 0, blockedN = 0, hiddenOut = 0;
                var right = Vector3.Cross(Vector3.up, fwd).normalized;
                var cands = new List<Vector3>();
                if (anyDirection)
                    for (float r = minD; r <= maxD + 1e-3f; r += 1f)
                        for (float a = 0f; a < 360f; a += 15f)
                            cands.Add(end + Quaternion.AngleAxis(a, Vector3.up) * fwd * r);
                else
                    for (float l = 1.5f; l <= maxD + 1e-3f; l += 0.75f)
                        for (float z = 1f; z <= 8f; z += 1f)   // 끝자리보다 앞 (플레이어 쪽으로 달려 나와 화면에 들어옴)
                            cands.Add(end + right * (sideSign * l) + fwd * z);
                // 매복(나오는 모습을 봐야 함)은 판정 2박 전 눈에서 가장 앞쪽(화각 가운데에 가까운) 자리부터, 전투 적은 가장 가까운 자리부터
                var eye0 = revealEyes != null && revealEyes.Count > 0 ? revealEyes[0] : (end, fwd);
                IEnumerable<Vector3> ordered = revealEyes != null
                    ? cands.OrderBy(c => Mathf.Round(Vector3.Angle(Vector3.ProjectOnPlane(c - eye0.Item1, Vector3.up), eye0.Item2) / 5f)).ThenBy(c => (c - end).sqrMagnitude)
                    : cands.OrderBy(c => (c - end).sqrMagnitude);
                foreach (var c in ordered)
                {
                    float d = new Vector2(c.x - end.x, c.z - end.z).magnitude;
                    if (d < minD || d > maxD) continue;
                    float? g;
                    if (!jump) g = Ground(c, end.y);
                    else
                    {
                        // 아래(골목·담 밑)나 담 너머의 면
                        var all = HeightsAt(c);
                        var hs = all.Where(h => h.y >= Mathf.Max(end.y - 7f, -0.05f) && h.y <= end.y + 0.5f && !IsNotGround(Owner(h.owner))).ToList();
                        g = hs.Count > 0 ? hs.Min(h => h.y) : (float?)null;   // 가장 낮은 면 (지붕 위면 골목, 땅이면 담 너머 마당)
                        if (g.HasValue && all.Any(h => h.y > g.Value + 0.3f && (h.y < g.Value + 2.0f || (h.y < g.Value + 8.0f && h.y < end.y - 0.5f)))) g = null;   // 머리 위가 트임 (집 안이 아님). 뛰어오를 지붕·담의 처마 밑은 됨
                    }
                    if (!g.HasValue) { noGround++; continue; }
                    var foot = new Vector3(c.x, g.Value, c.z);
                    if (SeenCount(foot, eyes) > 1) { seenN++; continue; }
                    var ys = jump ? JumpPath(foot, end) : ClearPath(foot, end);
                    if (ys == null) { blockedN++; continue; }
                    if (!Revealed(foot, end, ys, revealEyes, sideSign)) { hiddenOut++; continue; }
                    return (foot, ys, jump);
                }
                hideFail = $"{(jump ? "뛰기" : "걷기")}: 면 없음 {noGround}, 보임 {seenN}, 길 막힘 {blockedN}, 나오는 모습 안 보임 {hiddenOut}";
                return null;
            }

            int ambushOk = 0, ambushAll = 0;
            foreach (var prop in eventProps.GetComponentsInChildren<EventProp>(true))
            {
                string how = prop.companion != "" ? prop.companion : prop.kind;
                if (how != EventCatalog.Ambush || prop.enemy == null) continue;
                ambushAll++;
                float side = prop.slotSides.Length > 0 ? prop.slotSides[0] : 1f;
                float slot = prop.slotOffsets.Length > 0 ? prop.slotOffsets[0] : 2f;
                float z = (slot - prop.beats * 0.5f) * GameConfig.MetersPerBeat;
                var endLocal = new Vector3(-side * 1.0f, float.IsNaN(prop.ambushEndY) ? prop.enemy.localPosition.y : prop.ambushEndY, z);
                var end = prop.transform.TransformPoint(endLocal);
                float hitBeat = prop.runBeat + slot;
                var eyes = Eyes(hitBeat - GameConfig.Map.HideCheckBeats, hitBeat - GameConfig.Map.AmbushOutBeats);
                var reveal = Eyes(hitBeat - 1.75f, hitBeat - 0.75f);
                var hide = FindHide(end, prop.transform.forward, -side, GameConfig.Map.AmbushHideMin, GameConfig.Map.AmbushHideMax, eyes, false, true, reveal);
                if (!hide.HasValue) { prop.hasAmbushHide = false; Debug.LogWarning($"[SceneBuilder] 매복 숨을 자리 없음 {prop.name} (끝자리 {end}) — {hideFail}"); continue; }
                ambushOk++;
                prop.hasAmbushHide = true;
                prop.ambushHide = prop.transform.InverseTransformPoint(hide.Value.pos);
                float baseY = prop.transform.position.y;
                prop.ambushPathY = hide.Value.ys.Select(y => y - baseY).ToArray();
                prop.ambushJump = hide.Value.jump;
                prop.enemy.localPosition = prop.ambushHide;   // 처음 자리 = 숨은 자리 (불러온 순간에도 보이지 않게)
                Debug.Log($"[SceneBuilder] 매복 숨은 자리 {prop.name}: {hide.Value.pos} → 끝자리 {end} ({Vector3.Distance(hide.Value.pos, end):0.0} m{(hide.Value.jump ? $", 뛰어오름 최고 {hide.Value.ys.Max() - Mathf.Max(hide.Value.pos.y, end.y):0.0} m" : "")})");
            }

            int approachOk = 0, approachAll = 0;
            foreach (var a in enemies.Normal.Where(x => !x.onRoof).Concat(enemies.Surround))
            {
                if (a.encounterBeat < 0f) continue;
                approachAll++;
                var home = a.transform.position;
                var eyes = Eyes(a.encounterBeat - GameConfig.Map.HideCheckBeats, a.encounterBeat - GameConfig.Map.ApproachNoticeLead);
                float maxD = GameConfig.Map.ApproachRunSpeed * (GameConfig.Map.ApproachNoticeLeadMax - GameConfig.Map.ApproachNoticeBeats - GameConfig.Map.ApproachArriveBefore);
                MapLayout.PointAt(a.encounterBeat * GameConfig.MetersPerBeat, out var fwd);
                var hide = FindHide(home, fwd, 1f, 3f, maxD, eyes, true);
                var old = a.GetComponent<EnemyApproach>();
                if (old != null) Object.DestroyImmediate(old);
                if (!hide.HasValue) { Debug.LogWarning($"[SceneBuilder] 전투 적 숨을 자리 없음 {a.name} (자리 {home})"); continue; }
                float dist = new Vector2(hide.Value.pos.x - home.x, hide.Value.pos.z - home.z).magnitude;
                float lead = Mathf.Clamp(dist / GameConfig.Map.ApproachRunSpeed + GameConfig.Map.ApproachNoticeBeats + GameConfig.Map.ApproachArriveBefore, GameConfig.Map.ApproachNoticeLead, GameConfig.Map.ApproachNoticeLeadMax);
                // 알아채는 박이 늦어지면 그만큼 숨어 있는 범위도 다시 확인 (lead가 기본보다 크면 더 이른 박까지만 숨어 있으면 됨 — 이미 확인한 범위 안)
                var ap = a.gameObject.AddComponent<EnemyApproach>();
                ap.actor = a;
                ap.stopBeat = a.encounterBeat;
                ap.hidePos = hide.Value.pos;
                ap.pathY = hide.Value.ys;   // 숨은 자리 → 전투 자리 순서
                ap.homePos = home;
                ap.homeRot = a.transform.rotation;
                ap.noticeLead = lead;
                approachOk++;
                Debug.Log($"[SceneBuilder] 전투 적 숨은 자리 {a.name}: {hide.Value.pos} → {home} ({dist:0.0} m, 멈춤 {lead:0.0}박 전 알아챔)");
            }

            // 작업 R18c (GDD-beta B19.3 뒷부분): 지붕 위 전투 적 — 플레이어 뒤쪽 옆 지붕 줄에서 나타나 앞지른 뒤 지붕을 넘어 전투 자리에 착지
            int leapOk = 0, leapAll = 0;
            foreach (var a in enemies.Normal.Where(x => x.onRoof && x.encounterBeat >= 0f))
            {
                leapAll++;
                var oldLeap = a.GetComponent<EnemyLeapIn>();
                if (oldLeap != null) Object.DestroyImmediate(oldLeap);
                float S = a.encounterBeat, mpb = GameConfig.MetersPerBeat;
                var home = a.transform.position;
                float homeY = a.ArenaSurface(home);
                if (!float.IsNaN(homeY)) home.y = homeY;
                float tRun0 = S - GameConfig.Map.LeapRunBeats, tTake = S - GameConfig.Map.LeapTakeoffBefore;
                float d0 = tRun0 * mpb - GameConfig.Map.LeapStartBehind;
                float d1 = S * mpb + GameConfig.EnemySpawnMeters + GameConfig.Map.LeapTakeoffAhead;
                int m = Mathf.Max(8, Mathf.CeilToInt((d1 - d0) / 0.5f) + 1);
                List<Vector3> bestPts = null; float?[] bestYs = null; float bestScore = -9f, bestL = 0f, bestSide = 0f;
                foreach (float side in new[] { 1f, -1f })
                    for (float L = GameConfig.Map.LeapLateralMin; L <= GameConfig.Map.LeapLateralMax + 1e-3f; L += 1f)
                    {
                        var raw = new List<Vector3>();
                        Vector3 last = Vector3.zero;
                        for (int i = 0; i < m; i++)
                        {
                            var pt = MapLayout.PointAt(Mathf.Lerp(d0, d1, i / (float)(m - 1)), out var fwd);
                            var q = pt + MapLayout.RightOf(fwd) * (side * L); q.y = 0f;
                            if (raw.Count > 0 && Vector3.Dot(q - last, fwd) < 0.05f) continue;
                            raw.Add(q); last = q;
                        }
                        if (raw.Count < 2) continue;
                        float len = 0f;
                        for (int i = 1; i < raw.Count; i++) len += Vector3.Distance(raw[i - 1], raw[i]);
                        float stretch = len / Mathf.Max(1f, d1 - d0);
                        var pts = Resample(raw, m);
                        var ys = new float?[m];
                        int hits = 0;
                        for (int i = 0; i < m; i++) { ys[i] = FarSurface(pts[i]); if (ys[i].HasValue) hits++; }
                        // 박차는 자리(끝)는 면이 있어야 함
                        if (!ys[m - 1].HasValue) continue;
                        float score = hits / (float)m - (stretch > GameConfig.Map.ChaseFarMaxStretch ? 1f : 0f) - Mathf.Abs(ys[m - 1].Value - home.y) * 0.02f;
                        if (score > bestScore + 1e-3f) { bestScore = score; bestPts = pts; bestYs = ys; bestL = L; bestSide = side; }
                    }
                if (bestPts == null) { Debug.LogWarning($"[SceneBuilder] 앞질러 착지 줄 없음 {a.name}"); continue; }
                // 발 높이: 면이 없는 칸은 앞뒤를 잇고 뛰어넘음, 1 m 넘게 바뀌는 곳은 앞뒤 3칸에 걸쳐 뛰어 오르내림 (R18 먼 줄과 같음)
                var fy = new float[m]; var fg = new bool[m];
                for (int i = 0; i < m; i++)
                {
                    if (bestYs[i].HasValue) { fy[i] = bestYs[i].Value; continue; }
                    fg[i] = true;
                    int lo = i - 1; while (lo >= 0 && !bestYs[lo].HasValue) lo--;
                    int hi = i + 1; while (hi < m && !bestYs[hi].HasValue) hi++;
                    fy[i] = lo >= 0 && hi < m ? Mathf.Lerp(bestYs[lo].Value, bestYs[hi].Value, (i - lo) / (float)(hi - lo)) : lo >= 0 ? bestYs[lo].Value : hi < m ? bestYs[hi].Value : home.y;
                }
                for (int i = 0; i < m; i++)
                {
                    if (!fg[i]) continue;
                    int lo = i; while (lo > 0 && fg[lo - 1]) lo--;
                    int hi = i; while (hi < m - 1 && fg[hi + 1]) hi++;
                    float k = (i - lo + 1) / (float)(hi - lo + 2);
                    fy[i] += Mathf.Min(1.5f, (hi - lo + 2) * 0.5f * 0.35f) * 4f * k * (1f - k);
                }
                for (int i = 0; i < m - 1; i++)
                {
                    if (Mathf.Abs(fy[i + 1] - fy[i]) <= 1f) continue;
                    int lo = Mathf.Max(0, i - 3), hi = Mathf.Min(m - 1, i + 4);
                    float y0 = fy[lo], y1 = fy[hi];
                    for (int j = lo + 1; j < hi; j++)
                    {
                        float k = (j - lo) / (float)(hi - lo);
                        fy[j] = Mathf.Max(fy[j], Mathf.Lerp(y0, y1, k) + 0.6f * 4f * k * (1f - k));
                        fg[j] = true;
                    }
                    i = hi - 1;
                }
                // 날아가는 포물선: 사이 지붕·담 위 여유 (가운데에서 다 주고 양 끝으로 줄임)
                var take = new Vector3(bestPts[m - 1].x, fy[m - 1], bestPts[m - 1].z);
                float arcH = 1.0f;
                for (int s2 = 1; s2 < 24; s2++)
                {
                    float k = s2 / 24f;
                    var q = Vector3.Lerp(take, home, k);
                    var hs = HeightsAt(q).Where(h => h.y < 14f && !IsNotGround(Owner(h.owner))).ToList();
                    if (hs.Count == 0) continue;
                    float need = hs.Max(h => h.y) + 0.6f * Mathf.Clamp01(k * (1f - k) * 8f) - q.y;
                    if (need > 0f) arcH = Mathf.Max(arcH, need / (4f * k * (1f - k)));
                }
                if (arcH > GameConfig.Map.LeapMaxArc) { Debug.LogWarning($"[SceneBuilder] 앞질러 착지 포물선이 너무 높음 {a.name} ({arcH:0.0} m)"); continue; }
                var lp = a.gameObject.AddComponent<EnemyLeapIn>();
                lp.actor = a;
                lp.stopBeat = S;
                lp.runFromBeat = tRun0;
                lp.takeoffBeat = tTake;
                lp.contactBeat = S - GameConfig.Map.LeapContactBefore;
                lp.runX = bestPts.Select(p => p.x).ToArray();
                lp.runZ = bestPts.Select(p => p.z).ToArray();
                lp.runY = fy;
                lp.runGap = fg;
                lp.homePos = home;
                lp.homeRot = a.transform.rotation;
                lp.arc = arcH;
                leapOk++;
                Debug.Log($"[SceneBuilder] 앞질러 착지 {a.name}: {(bestSide > 0 ? "오른쪽" : "왼쪽")} {bestL:0} m 줄, 면 {bestYs.Count(y => y.HasValue)}/{m}, 박차는 자리 {take} → 착지 {home} ({Vector3.Distance(take, home):0.0} m, 포물선 {arcH:0.0} m)");
            }
            Debug.Log($"[SceneBuilder] 앞질러 착지: {leapOk}/{leapAll}");
            Physics.queriesHitBackfaces = oldBack;
            Object.DestroyImmediate(rayRoot);
            Debug.Log($"[SceneBuilder] 숨은 자리: 매복 {ambushOk}/{ambushAll}, 전투 적 {approachOk}/{approachAll}");
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
            mat.renderQueue = -1;   // 작업 R20: 셰이더 큐(투명 맨 앞, 하늘 뒤)를 그대로 — 예전에 저장된 불투명 큐를 지운다
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
            go.isStatic = false;   // 작업 R37: 시작화면 되풀이 동안 AttractMode가 옮긴다 (정적 배칭이면 그려지는 자리가 안 바뀜)
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
