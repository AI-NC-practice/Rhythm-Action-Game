using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R42 (사용자 피드백 2026-11-24 7번): 뒤돌아보기 컷마다 플레이어 뒤 길에 막는 것(땅: 지게·짚단, 지붕: 기와 더미)이 쓰러지고,
    /// 쫓아오던 무리가 달려들다 걸려 넘어지는 장면이 돌아본 화면에 보인다. 무리는 막는 것을 넘어오지 않고, 넘어진 무리·막는 것은
    /// 보이는 면 위에 있다(공중·건물 속 아님, 보이는 메시로 만든 검사용 충돌체 기준). 미리보기 → harness/reports/R42/.
    /// </summary>
    public class R42_LookBackBarricadePlayTests
    {
        const string Out = "harness/reports/R42";
        const int SupportLayer = 30;
        double now;
        GameBootstrap boot;
        StageDirector dir;
        GameObject supportRoot;

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
        }

        void BuildSupports(Vector3 around)
        {
            supportRoot = new GameObject("R42_Support");
            var combined = new HashSet<Mesh>();
            var area = new Bounds(around, new Vector3(30f, 30f, 30f));
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var mesh = mf.sharedMesh; var r = mf.GetComponent<MeshRenderer>();
                if (mesh == null || r == null || !r.enabled || mf.name == "Backdrop") continue;
                if (mf.GetComponentInParent<EnemyActor>() != null || mf.GetComponentInParent<PathFollower>() != null || mf.GetComponentInParent<EnemyFigureRun>() != null) continue;
                if (mf.transform.root.name.StartsWith("LookBackBarricade")) continue;
                if (!r.bounds.Intersects(area)) continue;
                var go = new GameObject("mesh") { layer = SupportLayer };
                go.transform.SetParent(supportRoot.transform, false);
                if (r.isPartOfStaticBatch) { if (!combined.Add(mesh)) { Object.Destroy(go); continue; } }
                else { go.transform.SetPositionAndRotation(mf.transform.position, mf.transform.rotation); go.transform.localScale = mf.transform.lossyScale; }
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
            foreach (var inst in Object.FindObjectsByType<DressingInstancer>(FindObjectsSortMode.None))
                foreach (var g in inst.groups)
                {
                    if (g.subMesh != 0 || g.mesh == null || g.matrices == null) continue;
                    foreach (var m in g.matrices)
                    {
                        if ((((Vector3)m.GetColumn(3)) - around).sqrMagnitude > 30f * 30f) continue;
                        var go = new GameObject("dress") { layer = SupportLayer };
                        go.transform.SetParent(supportRoot.transform, false);
                        go.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
                        go.transform.localScale = m.lossyScale;
                        go.AddComponent<MeshCollider>().sharedMesh = g.mesh;
                    }
                }
            Physics.SyncTransforms();
        }

        /// <summary>점 아래(위 0.6 m부터) 가장 가까운 보이는 면까지 거리.</summary>
        static float GapBelow(Vector3 p)
        {
            if (Physics.Raycast(p + Vector3.up * 0.6f, Vector3.down, out var h, 8f, 1 << SupportLayer, QueryTriggerInteraction.Ignore)) return h.distance - 0.6f;
            return float.PositiveInfinity;
        }

        static void Shot(Camera cam, string path)
        {
            var rt = new RenderTexture(960, 540, 24);
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(960, 540, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            RenderTexture.active = prev;
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.Destroy(t); rt.Release();
        }

        IEnumerator Cut(int i)
        {
            Directory.CreateDirectory(Out);
            double b = GameConfig.Fx.LookBackRunBeats[i];
            yield return Load(b - 5);
            var lb = Object.FindAnyObjectByType<LookBackCut>();
            Assert.IsNotNull(lb.barricades, "막는 것 연결");
            Assert.IsNotNull(lb.barricades[i], $"컷 {i} 막는 것");
            var at = MapLayout.PointAt(LookBackCut.BarricadeDistance(i), out var fwd);
            BuildSupports(at);
            var view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            Assert.AreEqual(0f, lb.BarricadeFall(i), 1e-4f, "컷 전에는 길가에 서 있음");
            int shot = 0; int trippedInView = 0; bool cutSeen = false;
            float maxPast = float.NegativeInfinity;
            double end = b + LookBackCut.LengthOf(i) + 0.9;
            while (boot.player.VisualRunBeat < end)
            {
                now += 0.01;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
                yield return null;
                double v = boot.player.VisualRunBeat;
                if (lb.Active) cutSeen = true;
                if (v < b) continue;
                // 무리는 막는 것을 넘어오지 않음
                for (int k = 0; k < lb.pursuers.Length; k++)
                {
                    var p = lb.pursuers[k];
                    if (p == null || !p.gameObject.activeInHierarchy) continue;
                    float past = Vector3.Dot(p.position - at, fwd);
                    maxPast = Mathf.Max(maxPast, past);
                    if (lb.Active && lb.PursuerTripped(k))
                    {
                        var vp = view.WorldToViewportPoint(p.position + Vector3.up * 0.4f);
                        if (vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f) trippedInView = Mathf.Max(trippedInView, Enumerable.Range(0, lb.pursuers.Length).Count(lb.PursuerTripped));
                    }
                }
                float u = (float)((v - b) / LookBackCut.LengthOf(i));
                if (lb.Active && shot == 0 && u >= 0.45f) { shot++; Shot(view, $"{Out}/cut{i}_a.png"); }
                if (lb.Active && shot == 1 && u >= 0.62f) { shot++; Shot(view, $"{Out}/cut{i}_b.png"); }
            }
            Assert.IsTrue(cutSeen, $"컷 {i}가 일어남");
            Assert.AreEqual(1f, lb.BarricadeFall(i), 1e-4f, "막는 것이 다 쓰러짐");
            int n = Enumerable.Range(0, lb.pursuers.Length).Count(lb.PursuerTripped);
            Debug.Log($"[R42] 컷 {i} ({b}박): 넘어진 무리 {n}/{lb.pursuers.Length}, 컷 화면 안에서 넘어진 무리 최대 {trippedInView}, 막는 것 너머로 나온 거리 최대 {maxPast:0.00} m");
            Assert.AreEqual(lb.pursuers.Length, n, "쫓는 무리 모두 막는 것에 걸려 넘어짐");
            Assert.GreaterOrEqual(trippedInView, 1, "돌아본 컷 화면 안에서 걸려 넘어진 무리가 보임");
            Assert.Less(maxPast, 0f, "무리가 막는 것을 넘어오지 않음");
            // 넘어진 무리·쓰러진 막는 것은 보이는 면 위 (공중 아님)
            foreach (var p in lb.pursuers)
            {
                if (p == null || !p.gameObject.activeInHierarchy) continue;
                float g = GapBelow(p.position);
                Debug.Log($"[R42diag] 컷 {i} {p.name} 자리 {p.position} 길 높이 {MapLayout.PointAt(LookBackCut.BarricadeDistance(i) + Vector3.Dot(p.position - at, fwd)).y:0.00} 발밑 거리 {g:0.00}");
                Assert.Less(Mathf.Abs(g), 0.35f, $"넘어진 무리 {p.name} 발밑 면과의 거리 {g:0.00} m (공중·속)");
            }
            var root = lb.barricades[i];
            for (int j = 0; j < root.childCount; j++)
            {
                var piece = root.GetChild(j);
                var rs = piece.GetComponentsInChildren<Renderer>();
                var bnd = rs[0].bounds; foreach (var r in rs) bnd.Encapsulate(r.bounds);
                float g = GapBelow(new Vector3(bnd.center.x, bnd.min.y, bnd.center.z));
                Debug.Log($"[R42diag] 컷 {i} 조각 {piece.name} 경계 {bnd.min}~{bnd.max} 렌더러 {rs.Length}개 아래 거리 {g:0.00}");
                Assert.Less(g, 0.5f, $"쓰러진 {piece.name} 아래가 비어 있음 ({g:0.00} m)");
            }
            Shot(view, $"{Out}/cut{i}_after.png");
        }

        [UnityTest, Timeout(600000)] public IEnumerator R42_Cut0_Alley_JigeFalls_PursuersTrip() => Cut(0);
        [UnityTest, Timeout(600000)] public IEnumerator R42_Cut1_Market_JigeFalls_PursuersTrip() => Cut(1);
        [UnityTest, Timeout(600000)] public IEnumerator R42_Cut2_Roof_TilesScatter_PursuersTrip() => Cut(2);
        [UnityTest, Timeout(600000)] public IEnumerator R42_Cut3_PalaceWall_TilesScatter_PursuersTrip() => Cut(3);
    }
}
