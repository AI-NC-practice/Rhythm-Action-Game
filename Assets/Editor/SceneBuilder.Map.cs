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

        // ---------- 모델 감싸기 ----------

        class Wrapped { public GameObject Go; public Vector3 Size; }

        static readonly Dictionary<string, (Vector3 size, bool rotate, Vector3 center, float minY)> shapeCache = new Dictionary<string, (Vector3, bool, Vector3, float)>();

        /// <summary>GLB 모델을 감싼다: 감싼 물체의 원점 = 모델 바닥 가운데, 가장 긴 수평축 = x. Size는 감싼 물체 기준 크기.</summary>
        static Wrapped Wrap(string id, Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var m = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/{id}.glb"));
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

        /// <summary>상자 자리에 모델을 꽉 맞춘다(크기·방향). 상자는 충돌만 남기고 그리기를 끈다.</summary>
        static void FitIntoBox(Transform box, string id, int tiles = 1, string centerId = null)
        {
            var size = box.lossyScale;
            bool alongZ = size.z > size.x;
            float len = alongZ ? size.z : size.x, thick = alongZ ? size.x : size.z;
            var axis = alongZ ? box.forward : box.right;
            var yaw = alongZ ? Quaternion.Euler(0, 90f, 0) : Quaternion.identity;
            var bottom = box.position - box.up * size.y * 0.5f;
            for (int i = 0; i < tiles; i++)
            {
                string use = centerId != null && i == tiles / 2 ? centerId : id;
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
        static void TileIntoBox(Transform box, string id, string centerId = null)
        {
            var size = box.lossyScale;
            float len = Mathf.Max(size.x, size.z);
            var probe = Wrap(id, box.parent, "probe");
            float aspect = probe.Size.x / Mathf.Max(probe.Size.y, 0.01f);
            Object.DestroyImmediate(probe.Go);
            int tiles = Mathf.Max(1, Mathf.RoundToInt(len / (size.y * aspect)));
            FitIntoBox(box, id, tiles, centerId);
        }

        // ---------- 챕터 꾸미기 ----------

        struct Dress { public string Id; public float Offset, Length, Height, Depth; public bool Uniform; }

        static Dress House(float offset) => new Dress { Id = "house_giwa", Offset = offset, Height = GameConfig.Map.DressHouseHeight, Uniform = true };
        static Dress Wall(float offset, float h, float len) => new Dress { Id = "wall_stone", Offset = offset, Height = h, Length = len, Depth = 1.0f };
        static Dress Prop(string id, float offset) => new Dress { Id = id, Offset = offset, Height = id == "cart" ? 1.2f : 1.0f, Uniform = true };

        /// <summary>챕터(구간)별 길 옆 장식 세트: 가까운 줄, 먼 줄.</summary>
        static Dress[] DressFor(int section, System.Random rnd)
        {
            float near = GameConfig.Map.DressNear, far = GameConfig.Map.DressFar, farthest = GameConfig.Map.DressFarthest;
            switch (section)
            {
                case 1: return new[] { House(near), House(far), House(farthest) };                                                // 마을 골목: 담 너머 초가·기와
                case 2: return new[] { Wall(near - 2f, 2.2f, 7f), House(far) };                                  // 돌담과 지붕
                case 3: return new[] { House(near), House(far), House(farthest) };                                                // 기와지붕 바다
                case 4: return new[] { Prop(rnd.Next(2) == 0 ? "jars" : "cart", near + 2f), Wall(far, 2.6f, 9f) }; // 마당: 항아리·수레, 둘레 담
                case 5: return new[] { Wall(near - 1f, GameConfig.Map.PalaceWallHeight, 10f), House(far + 2f), House(farthest + 2f) };  // 궁궐 담
                default: return new[] { Wall(near - 1f, GameConfig.Map.PalaceWallHeight, 10f) };                 // 성문 위
            }
        }

        static List<Vector3> routeSamples;

        static float RouteDistance(Vector3 p)
        {
            if (routeSamples == null)
            {
                routeSamples = new List<Vector3>();
                for (float d = -10f; d <= MapLayout.TotalLength + 10f; d += 1.5f) routeSamples.Add(MapLayout.FlatPointAt(d, out _));
            }
            float best = float.MaxValue;
            foreach (var s in routeSamples) { float dx = s.x - p.x, dz = s.z - p.z; best = Mathf.Min(best, dx * dx + dz * dz); }
            return Mathf.Sqrt(best);
        }

        static void BuildDressing(Transform map)
        {
            var parent = new GameObject("Dressing").transform;
            parent.SetParent(map);
            parent.gameObject.isStatic = true;
            var rnd = new System.Random(GameConfig.Map.DressSeed);
            routeSamples = null;
            int n = 0;
            foreach (var sec in MapLayout.Sections)
            {
                float a = sec.StartBeat * GameConfig.MetersPerBeat, b = sec.EndBeat * GameConfig.MetersPerBeat;
                for (float d = a + 4f; d < b; d += GameConfig.Map.DressSpacing)
                {
                    var center = MapLayout.FlatPointAt(d, out var fwd);
                    var right = MapLayout.RightOf(fwd);
                    foreach (var side in new[] { -1f, 1f })
                        foreach (var (dr, row) in DressFor(sec.Number, rnd).Select((x, i) => (x, i)))
                        {
                            float jitter = (float)(rnd.NextDouble() - 0.5) * 2.5f;
                            var p = center + right * side * (dr.Offset + jitter) + fwd * (float)(rnd.NextDouble() - 0.5) * 3f;
                            p.y = 0f;
                            var w = Wrap(dr.Id, parent, $"Dress_{sec.Number}_{n++}_{dr.Id}_r{row}");
                            Vector3 scale;
                            if (dr.Uniform) { float k = dr.Height / w.Size.y; scale = Vector3.one * k; }
                            else scale = new Vector3(dr.Length / w.Size.x, dr.Height / w.Size.y, dr.Depth / w.Size.z);
                            // 길에서 떨어졌는지: 모델의 길 쪽 두께 절반 + 여유를, 길이 방향 양끝·가운데에서 모두 검사 (모퉁이에서 길을 막지 않게)
                            float halfLen = 0.5f * w.Size.x * scale.x, halfDepth = 0.5f * w.Size.z * scale.z;
                            var along = Vector3.Cross(Vector3.up, -right * side).normalized;
                            bool clear = true;
                            foreach (var k in new[] { -1f, 0f, 1f })
                                if (RouteDistance(p + along * halfLen * k) < GameConfig.Map.DressClearance + halfDepth) clear = false;
                            if (!clear) { Object.DestroyImmediate(w.Go); continue; }
                            w.Go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(-right * side, Vector3.up));   // 길을 바라봄
                            w.Go.transform.localScale = scale;
                        }
                }
            }
            MergeDressing(parent);
        }

        /// <summary>
        /// 드로우콜 줄이기 (작업 T15): 장식을 (메시, 머티리얼, 그림자)마다 모아 DressingInstancer가 GPU 인스턴싱으로 그린다.
        /// 먼 줄(1번 줄부터)은 그림자를 드리우지 않는다. 원래 장식 물체는 위치 기록용 빈 물체(Dress_*)로 남긴다.
        /// (메시를 합쳐 에셋으로 저장하면 225 MB가 되어 저장소에 맞지 않아 이 방법을 쓴다.)
        /// </summary>
        static void MergeDressing(Transform parent)
        {
            var inst = parent.gameObject.AddComponent<DressingInstancer>();
            var map = new Dictionary<(Mesh, int, Material, bool), List<Matrix4x4>>();
            foreach (var t in parent.Cast<Transform>().Where(t => t.name.StartsWith("Dress_")).ToList())
            {
                bool shadow = t.name.EndsWith("_r0");
                foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mr == null) continue;
                    for (int sm = 0; sm < mf.sharedMesh.subMeshCount; sm++)
                    {
                        var key = (mf.sharedMesh, sm, mr.sharedMaterials[Mathf.Min(sm, mr.sharedMaterials.Length - 1)], shadow);
                        if (!map.TryGetValue(key, out var list)) map[key] = list = new List<Matrix4x4>();
                        list.Add(mf.transform.localToWorldMatrix);
                    }
                }
                foreach (var c in t.GetComponentsInChildren<Transform>(true).Where(c => c != t).ToList()) Object.DestroyImmediate(c.gameObject);
            }
            foreach (var kv in map)
                inst.groups.Add(new DressingInstancer.Group { mesh = kv.Key.Item1, subMesh = kv.Key.Item2, material = kv.Key.Item3, castShadows = kv.Key.Item4, matrices = kv.Value.ToArray() });
            parent.gameObject.isStatic = false;
        }

        // ---------- 외곽 그림 원통·하늘·해·안개 ----------

        static void BuildBackdrop(Transform map)
        {
            Directory.CreateDirectory(BackdropDir);
            string texPath = $"{BackdropDir}/backdrop.png";
            File.WriteAllBytes(texPath, PaintBackdrop(2048, 512).EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);
            if (AssetImporter.GetAtPath(texPath) is TextureImporter ti)
            {
                ti.wrapModeU = TextureWrapMode.Repeat; ti.wrapModeV = TextureWrapMode.Clamp;
                ti.mipmapEnabled = true; ti.maxTextureSize = 2048;
                ti.SaveAndReimport();
            }
            string matPath = $"{BackdropDir}/Backdrop.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(Shader.Find("RhythmActionGame/Backdrop")); AssetDatabase.CreateAsset(mat, matPath); }
            mat.shader = Shader.Find("RhythmActionGame/Backdrop");
            mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetColor("_FogColor", FogColor);
            EditorUtility.SetDirty(mat);

            string meshPath = $"{BackdropDir}/BackdropCylinder.asset";
            var mesh = Cylinder(64, GameConfig.Map.BackdropRadius, GameConfig.Map.BackdropHeight);
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
                v.Add(d); uv.Add(new Vector2(i / (float)seg * 3f, 0f));                 // 그림이 둘레를 3번 돈다
                v.Add(d + Vector3.up * height); uv.Add(new Vector2(i / (float)seg * 3f, 1f));
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

        /// <summary>해 질 녘 하늘, 먼 산 세 겹, 궁궐·기와지붕 실루엣 (가로로 이어지게 sin 합으로).</summary>
        static Texture2D PaintBackdrop(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
            var px = new Color[w * h];
            Color skyTop = new Color(0.42f, 0.5f, 0.68f), skyLow = new Color(0.96f, 0.72f, 0.52f);
            float Ridge(float u, int seed, float baseV, float amp)
            {
                float s = 0f;
                for (int k = 1; k <= 6; k++)
                {
                    float ph = (seed * 13.7f + k * 3.1f) % 6.283f;
                    s += Mathf.Sin(2f * Mathf.PI * (k + seed % 3) * u + ph) / (k * 0.9f);
                }
                return baseV + amp * (0.5f + 0.25f * s);
            }
            // 기와지붕 실루엣: 일정 간격 집, 처마가 휜 지붕
            float Roofs(float u)
            {
                const int houses = 46;
                float x = u * houses, f = x - Mathf.Floor(x);
                int idx = Mathf.FloorToInt(x);
                float hgt = 0.11f + 0.035f * Mathf.Abs(Mathf.Sin(idx * 2.3f));
                float t = Mathf.Abs(f - 0.5f) * 2f;                                   // 0 가운데 → 1 가장자리
                float curve = 1f - Mathf.Pow(t, 1.6f) * 0.55f + (t > 0.85f ? (t - 0.85f) * 0.9f : 0f);   // 처마 끝이 살짝 들림
                return f < 0.06f || f > 0.94f ? 0.06f : 0.06f + hgt * curve;
            }
            // 궁궐: 둘레 한 곳에 크게
            float Palace(float u)
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(u * 360f, 110f)) / 360f;
                if (d > 0.045f) return 0f;
                float t = d / 0.045f;
                return 0.30f * (1f - Mathf.Pow(t, 1.4f) * 0.5f) + (t > 0.9f ? (t - 0.9f) * 0.8f : 0f);
            }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w, v = y / (float)h;
                    var c = Color.Lerp(skyLow, skyTop, Mathf.SmoothStep(0.15f, 0.95f, v));
                    if (v < Ridge(u, 1, 0.20f, 0.30f)) c = Color.Lerp(new Color(0.66f, 0.6f, 0.7f), skyLow, 0.35f);   // 먼 산
                    if (v < Ridge(u, 2, 0.14f, 0.22f)) c = new Color(0.5f, 0.45f, 0.55f);                             // 중간 산
                    if (v < Ridge(u, 3, 0.09f, 0.12f)) c = new Color(0.36f, 0.33f, 0.38f);                            // 가까운 산
                    if (v < Mathf.Max(Roofs(u), Palace(u))) c = new Color(0.2f, 0.18f, 0.2f);                        // 지붕·궁궐 실루엣
                    px[y * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

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
