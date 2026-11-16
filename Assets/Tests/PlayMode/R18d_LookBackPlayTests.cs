using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R18d (GDD-beta B19.1): 뒤돌아보기 컷 — 1·2·3·5챕터 한 번씩 1박, 카메라가 150° 넘게 뒤로 돌았다 돌아옴,
    /// 컷 동안 판정 원이 떠 있지 않고 칠 노드가 없음, 컷 가운데 화면에 쫓는 적이 1명 이상 보임.
    /// </summary>
    public class R18d_LookBackPlayTests
    {
        double now;
        GameBootstrap boot;
        StageDirector dir;
        GameObject visRoot;
        Camera view;

        [TearDown]
        public void TearDown()
        {
            if (visRoot != null) Object.Destroy(visRoot);
            Physics.queriesHitBackfaces = false;
            if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel();
            Time.timeScale = 1f;
        }

        IEnumerator Load(double startBeat)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            now = 0;
            boot.clock.TimeSource = () => now;
            float realTime = Time.realtimeSinceStartup;
            dir.slowMotion.RealTime = () => realTime + (float)now;
            foreach (var fx in Object.FindObjectsByType<TimeFx>(FindObjectsSortMode.None)) fx.RealTime = () => realTime + (float)now;
            // 카메라 연출(지붕 발차기 따라보기 등)도 박자 시계와 같은 걸음으로 (실제 프레임 시간이면 자동 연주보다 느려 연출이 박자보다 길게 남는다)
            foreach (var cf in Object.FindObjectsByType<CameraFx>(FindObjectsSortMode.None)) cf.DeltaTime = () => 0.01f;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.AllowEventSlowMotion = false;
            boot.StartRun(startBeat);
            yield return null;
            if (visRoot != null) Object.Destroy(visRoot);
            BuildVisible();
            view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
        }

        IEnumerator Step()
        {
            now += 0.01;
            foreach (var c in boot.judge.Cues.ToArray())
                if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
            if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
            yield return null;
        }

        /// <summary>보이는 맵 메시(인스턴싱 장식 + 정적 배칭 + 그 밖의 보이는 메시)에 검사용 충돌체 (R07과 같은 방식).</summary>
        void BuildVisible()
        {
            visRoot = new GameObject("R18d_Visible");
            foreach (var inst in Object.FindObjectsByType<DressingInstancer>(FindObjectsSortMode.None))
                foreach (var g in inst.groups)
                {
                    if (g.subMesh != 0 || g.mesh == null || g.matrices == null) continue;
                    foreach (var m in g.matrices)
                    {
                        var go = new GameObject("vis");
                        go.transform.SetParent(visRoot.transform, false);
                        go.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
                        go.transform.localScale = m.lossyScale;
                        go.AddComponent<MeshCollider>().sharedMesh = g.mesh;
                    }
                }
            var combined = new HashSet<Mesh>();
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var r = mf.GetComponent<MeshRenderer>();
                if (mf.sharedMesh == null || r == null || mf.name == "Backdrop") continue;
                if (mf.GetComponentInParent<EnemyActor>() != null || mf.GetComponentInParent<PathFollower>() != null || mf.GetComponentInParent<EventProp>() != null) continue;
                if (r.isPartOfStaticBatch)
                {
                    if (!combined.Add(mf.sharedMesh)) continue;
                    var go = new GameObject("vis");
                    go.transform.SetParent(visRoot.transform, false);
                    go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                }
                else if (r.enabled)
                {
                    var go = new GameObject("vis");
                    go.transform.SetParent(visRoot.transform, false);
                    go.transform.SetPositionAndRotation(mf.transform.position, mf.transform.rotation);
                    go.transform.localScale = mf.transform.lossyScale;
                    go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                }
            }
            Physics.queriesHitBackfaces = true;
            Physics.SyncTransforms();
        }

        /// <summary>적(발 자리 foot)이 화면 카메라에 보임: 화면 안이고, 몸 다섯 점 중 하나라도 보이는 메시에 안 막힘.</summary>
        bool Visible(Vector3 foot)
        {
            var eye = view.transform.position;
            var vp = view.WorldToViewportPoint(foot + Vector3.up * 1.1f);
            if (vp.z <= 0f || vp.x < -0.05f || vp.x > 1.05f || vp.y < -0.05f || vp.y > 1.05f) return false;
            var side = Vector3.Cross(Vector3.up, foot - eye).normalized * 0.3f;
            foreach (var t in new[] { foot + Vector3.up * 0.5f, foot + Vector3.up * 1.1f, foot + Vector3.up * 1.7f, foot + Vector3.up * 1.2f + side, foot + Vector3.up * 1.2f - side })
            {
                bool blocked = false;
                var d = t - eye;
                foreach (var h in Physics.RaycastAll(eye, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
                    if (h.transform.IsChildOf(visRoot.transform)) { blocked = true; break; }
                if (!blocked) return true;
            }
            return false;
        }

        [UnityTest]
        public IEnumerator R18d_LookBack_OncePerChaseChapter_NoRings_EnemyVisible()
        {
            System.IO.Directory.CreateDirectory("harness/reports/R18d");
            var lines = new List<string>();
            var cuts = GameConfig.Fx.LookBackRunBeats;
            for (int k = 0; k < cuts.Length; k++)
            {
                double cut = cuts[k];
                yield return Load(cut - 8);
                var lb = Object.FindAnyObjectByType<LookBackCut>();
                Assert.IsNotNull(lb, "LookBackCut");
                int before = lb.Count, frames = 0, ringFrames = 0, activeFrames = 0, seenMid = 0;
                float maxYaw = 0f, endYaw = float.NaN;
                double clock0 = double.NaN, clock1 = double.NaN;
                var hitTargets = new List<double>();
                bool shot = false;
                while (boot.player.VisualRunBeat < cut + GameConfig.Fx.LookBackBeats + 0.6 && frames < 6000)
                {
                    // 컷 동안 누른 판정 박을 모은다
                    foreach (var c in boot.judge.Cues.ToArray())
                        if (!c.Judged && c.Appeared && now + 0.01 >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6 && lb.Active) hitTargets.Add(c.TargetBeat);
                    yield return Step();
                    frames++;
                    if (lb.Active)
                    {
                        activeFrames++;
                        if (double.IsNaN(clock0)) clock0 = boot.clock.CurrentBeat;
                        clock1 = boot.clock.CurrentBeat;
                        maxYaw = Mathf.Max(maxYaw, Mathf.Abs(lb.CurrentYaw));
                        if (boot.judge.Cues.Any(c => c.AffectsSpeed && c.Appeared && !c.Judged)) ringFrames++;
                        double u = (boot.player.VisualRunBeat - cut) / GameConfig.Fx.LookBackBeats;
                        if (u > 0.4 && u < 0.6)
                        {
                            int n = LookBackCut.Enemies().Count(t => Visible(t.position) && (t.position - view.transform.position).magnitude < 45f);
                            seenMid = Mathf.Max(seenMid, n);
                            if (!shot) { shot = true; Save(view, $"harness/reports/R18d/lookback_{k + 1}_{cut:0.0}_fp.png"); }
                        }
                    }
                    else if (!double.IsNaN(clock0) && float.IsNaN(endYaw)) endYaw = Mathf.Abs(lb.CurrentYaw);
                }
                string line = $"컷 {k + 1} (달리기 {cut}박): 횟수 +{lb.Count - before}, 최대 {maxYaw:0} 도, 끝난 뒤 {endYaw:0} 도, 컷 {activeFrames}프레임(시계 {clock0:0.00}~{clock1:0.00}박) 중 판정 원 떠 있음 {ringFrames}, 컷 동안 친 판정 {hitTargets.Count}, 컷 가운데 보인 적 {seenMid}";
                lines.Add(line);
                Debug.Log("[R18d] " + line);
                Assert.AreEqual(1, lb.Count - before, "컷 한 번: " + line);
                Assert.Greater(maxYaw, 150f, "뒤로 150° 넘게: " + line);
                Assert.AreEqual(0f, endYaw, 0.01f, "끝나면 앞으로: " + line);
                Assert.AreEqual(0, ringFrames, "판정 원이 떠 있는 동안 없음: " + line);
                Assert.AreEqual(0, hitTargets.Count, "컷 동안 칠 판정 없음: " + line);
                Assert.GreaterOrEqual(seenMid, 1, "쫓는 적이 보임: " + line);
                Assert.AreEqual(GameConfig.Fx.LookBackBeats, (clock1 - clock0), 0.15, "1박 길이: " + line);
            }
        }

        /// <summary>컷은 1·2·3·5챕터에 하나씩 (4·6챕터는 쫓는 적 없음).</summary>
        [Test]
        public void R18d_LookBack_Chapters()
        {
            double[] starts = { 0, 56, 108, 176, 196, 252 };   // 챕터 시작 달리기 박 (stage1_timeline.json)
            int Ch(double r) { int c = 0; for (int i = 0; i < starts.Length; i++) if (r >= starts[i]) c = i + 1; return c; }
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 5 }, GameConfig.Fx.LookBackRunBeats.Select(Ch).ToArray());
        }

        static void Shots(Transform target, string name)
        {
            var view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            Save(view, $"harness/reports/{name}_fp.png");
            var go = new GameObject("R18dShot");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = 55f;
            var pl = GameBootstrap.Instance.player.transform;
            var toPl = pl.position - target.position; toPl.y = 0f;
            var dir = toPl.sqrMagnitude > 0.01f ? toPl.normalized : -target.forward;
            var side = Vector3.Cross(Vector3.up, dir);
            cam.transform.position = target.position + (dir * 4f + side * 4f).normalized * 9f + Vector3.up * 6f;
            cam.transform.LookAt((target.position + pl.position) * 0.5f + Vector3.up * 0.8f);
            Save(cam, $"harness/reports/{name}_tp.png");
            Object.Destroy(go);
        }

        static void Save(Camera cam, string path)
        {
            var rt = new RenderTexture(640, 360, 24);
            var old = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = old;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var t = new Texture2D(640, 360, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
            RenderTexture.active = prev;
            System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
            Object.Destroy(t);
            rt.Release();
        }
    }
}
