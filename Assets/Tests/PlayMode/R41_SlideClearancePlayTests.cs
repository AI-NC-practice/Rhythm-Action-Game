using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R41 (사용자 피드백 2026-11-24 6번 "아래로 피하는 거 부딪히면서 가는 것 같아요"): 실제 stage1에서 아래로 피하기 장애물마다
    /// PERFECT·NORMAL로 지나며, 장애물 밑면(보이는 메시로 만든 검사용 충돌체에 위로 쏜 광선)과 화면 카메라 높이를 길 방향으로 비교한다.
    /// 장애물 밑을 지나는 동안 카메라 높이 ≤ 밑면 − SlideClearance. 측정표: harness/reports/R41/clearance.md, 미리보기 PNG.
    /// </summary>
    public class R41_SlideClearancePlayTests
    {
        const string OutDir = "harness/reports/R41";
        const int SupportLayer = 30;
        /// <summary>(이름, 달리기 박): stage1의 아래로 피하기 장애물·사건.</summary>
        static readonly (string name, double beat)[] Cases =
        {
            ("clothesline_12", 12), ("eave_80", 80), ("clothesline_96", 96), ("eave_136", 136),
            ("closing_gate_202", 202), ("eave_232", 232), ("banner_slide_252", 252),
        };

        double now;
        GameBootstrap boot;
        StageDirector dir;
        Camera view;
        GameObject supportRoot;
        static readonly Dictionary<string, (float clear, float camMin, float under, int samples)> Results = new Dictionary<string, (float, float, float, int)>();

        [TearDown]
        public void TearDown()
        {
            if (supportRoot != null) Object.Destroy(supportRoot);
            if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel();
            Time.timeScale = 1f;
        }

        IEnumerator Load(double startBeat)
        {
            // 정적 배칭된 합친 메시는 읽을 수 없어 검사용 충돌체를 못 만든다 → 정적 플래그만 뺀 같은 씬(BuildAll이 만듦)
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Test/Game_NoBatch.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            now = 0;
            boot.clock.TimeSource = () => now;
            float realTime = Time.realtimeSinceStartup;
            dir.slowMotion.RealTime = () => realTime + (float)now;
            foreach (var fx in Object.FindObjectsByType<TimeFx>(FindObjectsSortMode.None)) fx.RealTime = () => realTime + (float)now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.AllowEventSlowMotion = false;
            boot.StartRun(startBeat);
            yield return null;
            view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
        }

        /// <summary>장애물 둘레의 보이는 메시에 검사용 충돌체 (충돌 상자·내 몸·적은 뺌).</summary>
        void BuildSupports(Vector3 around)
        {
            supportRoot = new GameObject("R41_Support");
            var combined = new HashSet<Mesh>();
            var area = new Bounds(around, new Vector3(12f, 12f, 12f));
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var mesh = mf.sharedMesh;
                var r = mf.GetComponent<MeshRenderer>();
                if (mesh == null || r == null || !r.enabled || mf.name == "Backdrop") continue;
                if (mf.GetComponentInParent<EnemyActor>() != null || mf.GetComponentInParent<PathFollower>() != null || mf.GetComponentInParent<EnemyFigureRun>() != null) continue;
                if (!r.bounds.Intersects(area)) continue;
                var go = new GameObject("mesh") { layer = SupportLayer };
                go.transform.SetParent(supportRoot.transform, false);
                if (r.isPartOfStaticBatch) { if (!combined.Add(mesh)) { Object.Destroy(go); continue; } }
                else { go.transform.SetPositionAndRotation(mf.transform.position, mf.transform.rotation); go.transform.localScale = mf.transform.lossyScale; }
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
            Physics.SyncTransforms();
        }

        /// <summary>길 위 점에서 위로 쏜 광선이 처음 닿는 보이는 면까지 높이 (없으면 +∞).</summary>
        static float Underside(Vector3 ground)
        {
            var q = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            float best = float.PositiveInfinity;
            if (Physics.Raycast(ground + Vector3.up * 0.3f, Vector3.up, out var h, 3.5f, 1 << SupportLayer, QueryTriggerInteraction.Ignore))
                best = h.point.y - ground.y;
            Physics.queriesHitBackfaces = q;
            return best;
        }

        IEnumerator Run(string name, double beat, double delay, string tag)
        {
            yield return Load(beat - 5);
            float d0 = (float)((beat + GameConfig.ObstacleBeats * 0.5) * GameConfig.MetersPerBeat);
            var p0 = MapLayout.PointAt(d0, out var fwd);
            BuildSupports(p0);
            // 밑면 높이: 길 방향 −2~+2 m, 0.1 m 간격 (가로 −0.3·0·0.3 m 중 가장 낮은 곳)
            var right = MapLayout.RightOf(fwd);
            var unders = new List<(float s, float u)>();
            for (float s = -2f; s <= 2f + 1e-3f; s += 0.1f)
            {
                var g = MapLayout.PointAt(d0 + s, out _);
                float u = float.PositiveInfinity;
                foreach (float lat in new[] { -0.3f, 0f, 0.3f })
                    u = Mathf.Min(u, Underside(g + right * lat));
                if (!float.IsPositiveInfinity(u)) unders.Add((s, u));
            }
            if (name.StartsWith("banner_slide"))
            {
                // 깃발 줄 타고 내려오기: 깃대·깃발은 길 옆이라 길 위(가운데 ±0.3 m, 3.5 m 안)에 부딪힐 것이 없어야 한다
                Assert.AreEqual(0, unders.Count, $"{name}: 길 위에 막는 것이 없음");
                Results[$"{name}_{tag}"] = (float.PositiveInfinity, float.NaN, float.PositiveInfinity, 99);
                Debug.Log($"[R41] {name} {tag}: 길 위 3.5 m 안에 아무것도 없음 (줄을 타고 내려오는 사건)");
                WriteTable();
                yield break;
            }
            Assert.Greater(unders.Count, 0, $"{name}: 길 위에서 장애물 밑면을 찾지 못함");
            float underMin = unders.Min(x => x.u);
            float sMin = unders.Min(x => x.s), sMax = unders.Max(x => x.s);
            double tEnd = boot.clock.TimeOfBeat(beat + GameConfig.ObstacleBeats + 1);
            float clear = float.PositiveInfinity, camMin = float.PositiveInfinity;
            int samples = 0;
            bool shot = false;
            while (now < tEnd)
            {
                now += 0.01;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) + delay - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
                yield return null;
                var cam = view.transform.position;
                float sc = Vector3.Dot(cam - p0, fwd);
                if (sc < sMin - 0.05f || sc > sMax + 0.05f) continue;
                var near = unders.OrderBy(x => Mathf.Abs(x.s - sc)).First();
                if (Mathf.Abs(near.s - sc) > 0.15f) continue;
                float h = cam.y - MapLayout.PointAt(d0 + sc, out _).y;
                samples++;
                camMin = Mathf.Min(camMin, h);
                clear = Mathf.Min(clear, near.u - h);
                if (!shot && Mathf.Abs(sc) < 0.6f)
                {
                    shot = true;
                    Directory.CreateDirectory(OutDir);
                    var rt = new RenderTexture(640, 360, 24);
                    view.targetTexture = rt; view.Render(); view.targetTexture = null;
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    var t = new Texture2D(640, 360, TextureFormat.RGB24, false);
                    t.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
                    RenderTexture.active = prev;
                    File.WriteAllBytes($"{OutDir}/{name}_{tag}.png", t.EncodeToPNG());
                    Object.Destroy(t); rt.Release();
                }
            }
            Results[$"{name}_{tag}"] = (clear, camMin, underMin, samples);
            Debug.Log($"[R41] {name} {tag}: 밑면 최저 {underMin:0.00} m, 지나는 동안 카메라 최저 {camMin:0.00} m, 여유 최소 {clear:0.00} m ({samples}프레임)");
            WriteTable();
        }

        static void WriteTable()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# R41 아래로 피하기 여유 (실제 stage1)");
            sb.AppendLine();
            sb.AppendLine($"장애물 밑(길 방향 범위)을 지나는 동안: 여유 = 밑면 높이 − 카메라 높이 (길 기준). 기준: 여유 ≥ {GameConfig.Body.SlideClearance:0.00} m.");
            sb.AppendLine();
            sb.AppendLine("| 장애물 | 결과 | 밑면 최저 (m) | 카메라 최저 (m) | 여유 최소 (m) | 프레임 |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var kv in Results.OrderBy(k => k.Key))
            {
                var i = kv.Key.LastIndexOf('_');
                sb.AppendLine($"| {kv.Key.Substring(0, i)} | {kv.Key.Substring(i + 1)} | {kv.Value.under:0.00} | {kv.Value.camMin:0.00} | {kv.Value.clear:0.00} | {kv.Value.samples} |");
            }
            Directory.CreateDirectory(OutDir);
            File.WriteAllText($"{OutDir}/clearance.md", sb.ToString(), new UTF8Encoding(false));
        }

        IEnumerator RunAll(double delay, string tag)
        {
            foreach (var c in Cases) yield return Run(c.name, c.beat, delay, tag);
            foreach (var c in Cases)
            {
                var r = Results[$"{c.name}_{tag}"];
                Assert.Greater(r.samples, 3, $"{c.name} {tag}: 장애물 밑을 지나는 프레임이 없음");
                Assert.GreaterOrEqual(r.clear, GameConfig.Body.SlideClearance,
                    $"{c.name} {tag}: 카메라가 장애물 밑면에 너무 가까움 (여유 {r.clear:0.00} m, 카메라 최저 {r.camMin:0.00}, 밑면 {r.under:0.00})");
            }
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator R41_Perfect_CameraClearsUnderside() => RunAll(0.0, "Perfect");

        [UnityTest, Timeout(900000)]
        public IEnumerator R41_Normal_CameraClearsUnderside() => RunAll(0.15, "Normal");
    }
}
