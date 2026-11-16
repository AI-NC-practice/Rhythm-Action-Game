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
    /// 작업 R18c (GDD-beta B19.3 뒷부분): 지붕 위 전투(128·152·240박)의 적은 처음부터 서 있지 않고, 플레이어 뒤쪽 옆 지붕 줄에서 나타나
    /// 앞지른 뒤 도약(Running Jump) → 공중(Falling Idle) → 착지(Hard Landing)로 전투 자리에 내려선다. 매 프레임 단계·클립·위치·발밑을 잰다.
    /// </summary>
    public class R18c_LeapInPlayTests
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
            visRoot = new GameObject("R18c_Visible");
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

        /// <summary>몸(허리 0.8 m)이 보이는 지붕·집 메시 속: 위로 2 m 안에 윗면이 있고 아래로도 면이 있음.</summary>
        bool InsideVisible(Vector3 foot)
        {
            var p = foot + Vector3.up * 0.8f;
            bool up = false, down = false;
            foreach (var h in Physics.RaycastAll(p, Vector3.up, 2f)) if (h.transform.IsChildOf(visRoot.transform) && Vector3.Dot(h.normal, Vector3.up) > 0.2f) { up = true; break; }
            if (!up) return false;
            foreach (var h in Physics.RaycastAll(p, Vector3.down, 20f)) if (h.transform.IsChildOf(visRoot.transform)) { down = true; break; }
            return down;
        }

        /// <summary>발 아래 보이는 면까지 거리 (앞뒤 0.35 m 세 점 중 가장 가까운 값).</summary>
        float FootGap(Transform root)
        {
            float best = float.PositiveInfinity;
            var fwd = Vector3.ProjectOnPlane(root.forward, Vector3.up).normalized;
            foreach (float o in new[] { 0f, 0.35f, -0.35f })
                foreach (var h in Physics.RaycastAll(root.position + fwd * o + Vector3.up * 0.3f, Vector3.down, 3f))
                    if (h.transform.IsChildOf(visRoot.transform)) best = Mathf.Min(best, Mathf.Abs(h.distance - 0.3f));
            return best;
        }

        /// <summary>지붕 위 전투 적 3명: 나타나기 전 숨김, 전투 자리에 미리 서 있지 않음, 도약·공중·착지 클립, 건물을 뚫지 않음, 착지 뒤 전투 자리·지붕 면 위, 멈추기 전에 화면에 보임.</summary>
        [UnityTest]
        public IEnumerator R18c_RoofFighters_OvertakeLeapLand()
        {
            System.IO.Directory.CreateDirectory("harness/reports/R18c");
            var lines = new List<string>();
            int checkedN = 0;
            foreach (float stop in new[] { 128f, 152f, 240f })
            {
                yield return Load(stop - 15f);
                var lp = Object.FindObjectsByType<EnemyLeapIn>(FindObjectsSortMode.None).FirstOrDefault(x => Mathf.Abs(x.stopBeat - stop) < 0.1f);
                Assert.IsNotNull(lp, $"{stop}박 지붕 위 전투 적에 앞질러 착지");
                var a = lp.actor;
                var rends = a.GetComponentsInChildren<Renderer>(true);
                var phases = new List<string>();
                var states = new HashSet<string>();
                int waitingShown = 0, nearHomeEarly = 0, airFrames = 0, airInside = 0, seen = 0, frames = 0;
                float arriveErr = float.NaN, landGap = float.NaN;
                bool shotRun = false, shotAir = false, shotLand = false;
                while (dir.CurrentEncounter == null && frames < 8000)
                {
                    yield return Step();
                    frames++;
                    if (phases.Count == 0 || phases[phases.Count - 1] != lp.Phase) phases.Add(lp.Phase);
                    if (a.anim != null) states.Add(a.anim.CurrentState);
                    var foot = a.transform.position;
                    if (lp.Phase == "Waiting" && rends.Any(r => r.enabled)) waitingShown++;
                    if ((lp.Phase == "Waiting" || lp.Phase == "Run") && rends.Any(r => r.enabled) && Vector3.Distance(foot, lp.homePos) < 3f) nearHomeEarly++;
                    if (lp.Phase == "Air") { airFrames++; if (InsideVisible(foot)) airInside++; }
                    if (lp.Phase != "Waiting" && Visible(foot)) seen++;
                    if (lp.Phase == "Done" || lp.Phase == "Land") { arriveErr = Vector3.Distance(foot, lp.homePos); landGap = FootGap(a.transform); }
                    double v = boot.player.VisualRunBeat;
                    if (!shotRun && lp.Phase == "Run" && v > lp.takeoffBeat - 4) { shotRun = true; Shots(a.transform, $"R18c/{a.name}_run"); }
                    if (!shotAir && lp.Phase == "Air" && v > (lp.takeoffBeat + lp.contactBeat) * 0.5) { shotAir = true; Shots(a.transform, $"R18c/{a.name}_air"); }
                    if (!shotLand && lp.Phase == "Land") { shotLand = true; Shots(a.transform, $"R18c/{a.name}_land"); }
                }
                checkedN++;
                string line = $"{a.name} ({stop}박): 단계 {string.Join("→", phases)}, 클립 {string.Join(",", states.Where(s => s.StartsWith("Leap_")))}, 숨김 중 보임 {waitingShown}, 전투 자리 근처에 미리 {nearHomeEarly}, 공중 {airFrames} 중 지붕 속 {airInside}, 화면에 보임 {seen}, 도착 오차 {arriveErr:0.00} m, 착지 발밑 {landGap:0.00} m";
                lines.Add(line);
                Debug.Log("[R18c] " + line);
                CollectionAssert.IsSubsetOf(new[] { "Run", "Air", "Land", "Done" }, phases, line);
                Assert.Less(phases.IndexOf("Run"), phases.IndexOf("Air"), line);
                Assert.Less(phases.IndexOf("Air"), phases.IndexOf("Land"), line);
                Assert.IsTrue(states.Contains(EnemyMoves.LeapJump) && states.Contains(EnemyMoves.LeapFall) && states.Contains(EnemyMoves.LeapLand), "도약·공중·착지 클립: " + line);
                Assert.AreEqual(0, waitingShown, "나타나기 전에는 보이지 않음: " + line);
                Assert.AreEqual(0, nearHomeEarly, "처음부터 전투 자리에 서 있지 않음: " + line);
                Assert.LessOrEqual(airInside, airFrames / 50, "날아가는 동안 지붕을 뚫지 않음: " + line);
                Assert.Greater(seen, 30, "멈추기 전에 화면에 보임 (앞지르는 모습): " + line);
                Assert.Less(arriveErr, 0.1f, "전투 자리에 착지: " + line);
                Assert.LessOrEqual(landGap, 0.35f, "착지한 발이 보이는 지붕 면 위: " + line);
            }
            Assert.AreEqual(3, checkedN);
        }

        static void Shots(Transform target, string name)
        {
            var view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            Save(view, $"harness/reports/{name}_fp.png");
            var go = new GameObject("R18cShot");
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
