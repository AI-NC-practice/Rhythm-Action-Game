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
    /// 작업 R18b (GDD-beta B19.3·B19.4): 매복 적은 판정 2박 전까지 1인칭 화면에서 가려져 있다가 뛰쳐나오고,
    /// 땅 위 전투 적은 가려진 자리에 있다가 알아채고(소리 없음) 달려와 멈춤 박에 전투 자리에 도착한다.
    /// 화면 카메라에서 적 몸 다섯 점으로 시선을 쏘아 보이는 맵 메시(검사용 충돌체)에 막히는지 매 프레임 잰다.
    /// </summary>
    public class R18b_HiddenEnemiesPlayTests
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
            visRoot = new GameObject("R18b_Visible");
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

        static string How(EventProp p) => p.companion != "" ? p.companion : p.kind;
        static float HitBeat(EventProp p) => p.runBeat + (p.slotOffsets.Length > 0 ? p.slotOffsets[0] : 2f);

        class Stat
        {
            public int hiddenFrames, seenWhileHidden, outFrames, seenOut, noticeFrames, runFrames;
            public float firstSeenBeat = float.NaN, arriveErr = float.NaN;
            public bool shot;
            public override string ToString() => $"숨은 동안 {hiddenFrames}프레임 중 보임 {seenWhileHidden}, 나온 뒤 {outFrames} 중 보임 {seenOut}, 알아챔 {noticeFrames}, 달려옴 {runFrames}, 처음 보인 박 {firstSeenBeat:0.0}, 도착 오차 {arriveErr:0.00} m";
        }

        /// <summary>매복 적 6명: 판정 2박 전까지 가려져 있음 (보인 프레임 2% 이하), 나온 뒤 판정 전에 보임.</summary>
        [UnityTest]
        public IEnumerator R18b_Ambush_HiddenUntilTwoBeatsBefore()
        {
            var stats = new Dictionary<string, Stat>();
            System.IO.Directory.CreateDirectory("harness/reports/R18b");
            foreach (var (start, end) in new[] { (6.0, 54.0), (70.0, 94.0), (114.0, 160.0), (210.0, 232.0) })
            {
                yield return Load(start);
                var props = Object.FindAnyObjectByType<EventPropDirector>().Props.Where(p => p.enemy != null && How(p) == EventCatalog.Ambush).ToList();
                foreach (var p in props) Assert.IsTrue(p.hasAmbushHide, $"{p.name}: 숨은 자리를 찾음");
                int frames = 0;
                while (boot.player.RunBeat < end && frames < 15000)
                {
                    yield return Step();
                    frames++;
                    double v = boot.player.VisualRunBeat;
                    foreach (var p in props)
                    {
                        float rem = (float)(HitBeat(p) - v);
                        if (rem > GameConfig.Map.HideCheckBeats || rem < 0f || !p.enemy.gameObject.activeInHierarchy) continue;
                        if (!stats.TryGetValue(p.name, out var st)) stats[p.name] = st = new Stat();
                        bool seen = Visible(p.enemy.position);
                        if (seen && float.IsNaN(st.firstSeenBeat)) st.firstSeenBeat = (float)v;
                        if (rem > GameConfig.Map.AmbushOutBeats + 0.05f) { st.hiddenFrames++; if (seen) st.seenWhileHidden++; }
                        else { st.outFrames++; if (seen) st.seenOut++; }
                        if (!st.shot && rem < GameConfig.Map.AmbushOutBeats + 1f) { st.shot = true; Shots(p.enemy, $"R18b/{p.name}_hidden"); }
                        if (rem < 0.6f && rem > 0.5f) Shots(p.enemy, $"R18b/{p.name}_out");
                    }
                }
            }
            foreach (var kv in stats) Debug.Log($"[R18b] 매복 {kv.Key}: {kv.Value}");
            Assert.GreaterOrEqual(stats.Count, 6, "매복 적 6명");
            foreach (var kv in stats)
            {
                var st = kv.Value;
                Assert.Greater(st.hiddenFrames, 100, $"{kv.Key}: 숨은 동안을 잼 ({st})");
                Assert.LessOrEqual(st.seenWhileHidden, st.hiddenFrames / 50, $"{kv.Key}: 판정 2박 전까지 보이지 않음 ({st})");
                Assert.Greater(st.seenOut, 0, $"{kv.Key}: 뛰쳐나와 판정 전에 보임 ({st})");
            }
        }

        /// <summary>땅 위 전투 적 (92박 일반 적, 184박 포위 5명): 가려진 자리 → 알아챔 → 달려와 멈춤 박에 전투 자리, 멈추기 전에 화면에 보임.</summary>
        [UnityTest]
        public IEnumerator R18b_GroundCombat_HiddenNoticeRunArrive()
        {
            var stats = new Dictionary<string, Stat>();
            System.IO.Directory.CreateDirectory("harness/reports/R18b");
            foreach (var (start, end) in new[] { (72.0, 92.0), (164.0, 184.0) })
            {
                yield return Load(start);
                var aps = Object.FindObjectsByType<EnemyApproach>(FindObjectsSortMode.None).Where(a => a.stopBeat > start && a.stopBeat <= end + 0.1).ToList();
                Assert.Greater(aps.Count, 0, $"{end}박 전투 적에 숨은 자리");
                int frames = 0;
                bool shotRun = false;
                while (boot.player.RunBeat < end - 0.01 && frames < 15000)
                {
                    yield return Step();
                    frames++;
                    double v = boot.player.VisualRunBeat;
                    foreach (var a in aps)
                    {
                        float rem = (float)(a.stopBeat - v);
                        if (rem > GameConfig.Map.HideCheckBeats) continue;
                        if (!stats.TryGetValue(a.name, out var st)) stats[a.name] = st = new Stat();
                        var foot = a.actor.transform.position;
                        bool seen = Visible(foot);
                        if (seen && float.IsNaN(st.firstSeenBeat)) st.firstSeenBeat = (float)v;
                        if (a.Phase == "Hidden") { st.hiddenFrames++; if (seen) st.seenWhileHidden++; }
                        else if (a.Phase == "Notice") st.noticeFrames++;
                        else if (a.Phase == "Run") { st.runFrames++; st.outFrames++; if (seen) st.seenOut++; }
                        if (rem <= GameConfig.Map.ApproachArriveBefore) st.arriveErr = Vector3.Distance(foot, a.homePos);
                        if (a.Phase == "Hidden" && !st.shot && rem < a.noticeLead + 1f) { st.shot = true; Shots(a.actor.transform, $"R18b/{a.name}_hidden"); }
                        if (!shotRun && a.Phase == "Run" && rem < 1.5f) { shotRun = true; Shots(a.actor.transform, $"R18b/{a.name}_run"); }
                    }
                }
            }
            foreach (var kv in stats) Debug.Log($"[R18b] 전투 적 {kv.Key}: {kv.Value}");
            Assert.GreaterOrEqual(stats.Count, 6, "92박 1명 + 포위 5명");
            foreach (var kv in stats)
            {
                var st = kv.Value;
                Assert.Greater(st.hiddenFrames, 100, $"{kv.Key}: 숨어 있음 ({st})");
                Assert.LessOrEqual(st.seenWhileHidden, st.hiddenFrames / 50, $"{kv.Key}: 숨어 있는 동안 보이지 않음 ({st})");
                Assert.Greater(st.noticeFrames, 10, $"{kv.Key}: 알아채는 동작 ({st})");
                Assert.Greater(st.runFrames, 20, $"{kv.Key}: 달려옴 ({st})");
                Assert.Less(st.arriveErr, 0.3f, $"{kv.Key}: 멈춤 박에 전투 자리 ({st})");
            }
            // 화면에 들어옴: 적이 여럿인 포위는 앞쪽 적만 보일 수 있으므로 전투마다 한 명 이상이 멈추기 전(판정 원은 멈춘 뒤 나옴)에 보임
            Assert.IsTrue(stats.Where(k => k.Key.StartsWith("Enemy_0")).All(k => !float.IsNaN(k.Value.firstSeenBeat)), "일반 적이 멈추기 전에 보임");
            Assert.IsTrue(stats.Where(k => k.Key.Contains("Surround")).Any(k => !float.IsNaN(k.Value.firstSeenBeat)), "포위 적이 멈추기 전에 보임");
        }

        static void Shots(Transform target, string name)
        {
            var view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            Save(view, $"harness/reports/{name}_fp.png");
            var go = new GameObject("R18bShot");
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
