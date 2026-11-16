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
    /// 작업 R18 (GDD-beta B19.2, B19.4): 추격 적은 옆 지붕 줄(8~20 m)에서 쫓아오다 공격 2박 전에 뛰어들고, 공격 뒤에는 뒤처진다.
    /// 돌진 적은 대기 → 걷기 → 점점 빨라져 질주. 실제 stage1을 자동 연주(PERFECT)로 지나며 매 프레임 잰다.
    /// </summary>
    public class R18_ChaseRedesignPlayTests
    {
        double now;
        GameBootstrap boot;
        StageDirector dir;

        [TearDown]
        public void TearDown()
        {
            if (supportRoot != null) Object.Destroy(supportRoot);
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
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.AllowEventSlowMotion = false;
            boot.StartRun(startBeat);
            yield return null;
        }

        GameObject supportRoot;

        /// <summary>보이는 맵 메시(인스턴싱 장식 + 정적 배칭 메시)에 검사용 충돌체를 붙인다 (R07과 같은 방식).</summary>
        void BuildVisibleColliders()
        {
            supportRoot = new GameObject("R18_Visible");
            foreach (var inst in Object.FindObjectsByType<DressingInstancer>(FindObjectsSortMode.None))
                foreach (var g in inst.groups)
                {
                    if (g.subMesh != 0 || g.mesh == null || g.matrices == null) continue;
                    foreach (var m in g.matrices)
                    {
                        var go = new GameObject("vis");
                        go.transform.SetParent(supportRoot.transform, false);
                        go.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
                        go.transform.localScale = m.lossyScale;
                        go.AddComponent<MeshCollider>().sharedMesh = g.mesh;
                    }
                }
            var combined = new HashSet<Mesh>();
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var r = mf.GetComponent<MeshRenderer>();
                if (mf.sharedMesh == null || r == null || !r.isPartOfStaticBatch || mf.name == "Backdrop" || !combined.Add(mf.sharedMesh)) continue;
                var go = new GameObject("vis");
                go.transform.SetParent(supportRoot.transform, false);
                go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;   // 합쳐진 메시는 월드 좌표
            }
            Physics.queriesHitBackfaces = true;
            Physics.SyncTransforms();
        }

        /// <summary>몸(허리 높이 0.8 m)이 보이는 지붕·담·집 메시 속: 위로 쏜 광선이 2 m 안에서 그 메시 안쪽 면에 맞고, 아래로도 맞음.</summary>
        bool InsideVisible(Vector3 feet)
        {
            var p = feet + Vector3.up * 0.8f;
            bool up = false, down = false;
            foreach (var h in Physics.RaycastAll(p, Vector3.up, 2f)) if (h.transform.IsChildOf(supportRoot.transform) && Vector3.Dot(h.normal, Vector3.up) > 0.2f) { up = true; break; }
            if (!up) return false;
            foreach (var h in Physics.RaycastAll(p, Vector3.down, 20f)) if (h.transform.IsChildOf(supportRoot.transform)) { down = true; break; }
            return down;
        }

        IEnumerator Step()
        {
            now += 0.01;
            foreach (var c in boot.judge.Cues.ToArray())
                if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
            if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
            yield return null;
        }

        static string How(EventProp p) => p.companion != "" ? p.companion : p.kind;
        static float HitBeat(EventProp p) => p.runBeat + (p.slotOffsets.Length > 0 ? p.slotOffsets[0] : 2f);

        /// <summary>점에서 플레이어 경로(앞뒤 범위)까지 수평 거리 (m).</summary>
        static float PathDistance(Vector3 p, float dCenter)
        {
            float best = float.PositiveInfinity;
            for (float d = Mathf.Max(0f, dCenter - 40f); d <= dCenter + 60f; d += 0.5f)
            {
                var q = MapLayout.PointAt(d);
                best = Mathf.Min(best, new Vector2(p.x - q.x, p.z - q.z).magnitude);
            }
            return best;
        }

        class ChaseStat
        {
            public int farFrames, nearFar, hopFrames, clipFrames, behindFrames, notBehind;
            public float minFar = 99f, maxFar, hitDist = 99f, minBehind = 99f;
            public bool shotFar, shotLeap;
            public override string ToString() => $"먼 줄 {farFrames}프레임 (옆 {minFar:0.0}~{maxFar:0.0} m, 8 m 안 {nearFar}), 공격 때 거리 {hitDist:0.0} m, 뛰어들기 {hopFrames} 중 지붕 속 {clipFrames}, 공격 뒤 {behindFrames} 중 곁에 남음 {notBehind} (최소 {minBehind:0.0} m)";
        }

        /// <summary>추격 적: 공격 2박 전까지 길에서 8~20 m, 공격 순간 플레이어 곁, 뛰어드는 동안 지붕을 뚫지 않음, 공격 4~6박 뒤 5 m 넘게 떨어져 있음.</summary>
        [UnityTest]
        public IEnumerator R18_ChaseEnemies_FarRoofLine_LeapIn_FallBehind()
        {
            var stats = new Dictionary<string, ChaseStat>();
            System.IO.Directory.CreateDirectory("harness/reports/R18");
            foreach (var (start, end) in new[] { (12.0, 46.0), (60.0, 96.0), (100.0, 170.0), (212.0, 252.0) })
            {
                yield return Load(start);
                if (supportRoot != null) Object.Destroy(supportRoot);
                BuildVisibleColliders();
                var props = Object.FindAnyObjectByType<EventPropDirector>().Props.Where(p => p.enemy != null && How(p) == EventCatalog.Chase).ToList();
                int frames = 0;
                while (boot.player.RunBeat < end && frames < 12000)
                {
                    yield return Step();
                    frames++;
                    double v = boot.player.VisualRunBeat;
                    var pl = boot.player.transform.position;
                    float dPlayer = (float)(v * GameConfig.MetersPerBeat);
                    foreach (var p in props)
                    {
                        if (!p.enemy.gameObject.activeInHierarchy) continue;
                        float rem = (float)(HitBeat(p) - v);
                        if (rem > 16f || rem < -7f) continue;
                        string key = p.name;
                        if (!stats.TryGetValue(key, out var st)) stats[key] = st = new ChaseStat();
                        var e = p.enemy.position;
                        if (rem > GameConfig.Map.ChaseLeapBeats + 0.3f && rem < 14f)
                        {
                            float lat = PathDistance(e, dPlayer + 2f);
                            st.farFrames++;
                            st.minFar = Mathf.Min(st.minFar, lat); st.maxFar = Mathf.Max(st.maxFar, lat);
                            if (lat < GameConfig.Map.ChaseFarLateralMin - 0.5f || lat > GameConfig.Map.ChaseFarLateralMax + 1.5f) st.nearFar++;
                            if (!st.shotFar && rem < 6f) { st.shotFar = true; Shots(p.enemy, $"R18/{key}_far"); }
                        }
                        if (Mathf.Abs(rem) < 0.04f) st.hitDist = Mathf.Min(st.hitDist, new Vector2(e.x - pl.x, e.z - pl.z).magnitude);
                        if (rem > 0f && rem < GameConfig.Map.ChaseLeapBeats && p.ChaseHopping)
                        {
                            st.hopFrames++;
                            if (InsideVisible(e)) st.clipFrames++;   // 위 면(윗면 법선이 위를 봄)의 안쪽 = 지붕·집 속
                            if (!st.shotLeap && rem < 1.1f) { st.shotLeap = true; Shots(p.enemy, $"R18/{key}_leap"); }
                        }
                        if (rem < -4f && rem > -6f)
                        {
                            float dist = new Vector2(e.x - pl.x, e.z - pl.z).magnitude;
                            st.behindFrames++;
                            st.minBehind = Mathf.Min(st.minBehind, dist);
                            if (dist < 5f) st.notBehind++;
                        }
                    }
                }
            }
            foreach (var kv in stats) Debug.Log($"[R18] 추격 {kv.Key}: {kv.Value}");
            var seen = stats.Where(k => k.Value.farFrames > 50).ToList();
            Assert.GreaterOrEqual(seen.Count, 6, "추격 적 대부분을 지남");
            foreach (var kv in seen)
            {
                var st = kv.Value;
                Assert.LessOrEqual(st.nearFar, st.farFrames / 50, $"{kv.Key}: 공격 2박 전까지 옆 8~20 m ({st})");
                Assert.LessOrEqual(st.hitDist, 4.5f, $"{kv.Key}: 공격 순간 플레이어 곁 ({st})");
                Assert.Greater(st.hopFrames, 10, $"{kv.Key}: 뛰어듦 ({st})");
                Assert.LessOrEqual(st.clipFrames, st.hopFrames / 50, $"{kv.Key}: 뛰어드는 동안 지붕을 뚫지 않음 ({st})");
                Assert.Greater(st.behindFrames, 10, $"{kv.Key}: 공격 뒤 ({st})");
                Assert.AreEqual(0, st.notBehind, $"{kv.Key}: 공격 뒤 나란히 달리지 않고 뒤처짐 ({st})");
            }
        }

        /// <summary>돌진 적 (72박 밧줄 그네, 220박): 처음엔 서 있다가 걷고, 점점 빨라져 판정 2박 전부터 질주. 판정 순간 판정 자리.</summary>
        [UnityTest]
        public IEnumerator R18_ChargeEnemies_WalkThenSprint()
        {
            var lines = new List<string>();
            int checkedProps = 0;
            System.IO.Directory.CreateDirectory("harness/reports/R18");
            foreach (var (start, end) in new[] { (52.0, 80.0), (200.0, 226.0) })
            {
                yield return Load(start);
                var props = Object.FindAnyObjectByType<EventPropDirector>().Props.Where(p => p.enemy != null && How(p) == EventCatalog.Charge).ToList();
                var last = new Dictionary<EventProp, (Vector3 pos, double beat)>();
                var speeds = new Dictionary<EventProp, List<(float rem, float v)>>();
                var hitErr = new Dictionary<EventProp, float>();
                var walkFrames = new Dictionary<EventProp, (int walk, int all)>();
                var shotWalk = new HashSet<EventProp>(); var shotSprint = new HashSet<EventProp>();
                int frames = 0;
                while (boot.player.RunBeat < end && frames < 10000)
                {
                    yield return Step();
                    frames++;
                    double v = boot.player.VisualRunBeat;
                    foreach (var p in props)
                    {
                        float rem = (float)(HitBeat(p) - v);
                        var e = p.enemy.position;
                        if (Mathf.Abs(rem) < 0.04f)
                        {
                            // 판정 순간: 길 방향으로 판정 자리(판정 박의 경로 거리)에 있음
                            float dHit = HitBeat(p) * GameConfig.MetersPerBeat;
                            var q = MapLayout.PointAt(dHit, out var fwd);
                            float along = Mathf.Abs(Vector3.Dot(new Vector3(e.x - q.x, 0f, e.z - q.z), fwd));
                            hitErr[p] = hitErr.TryGetValue(p, out var old) ? Mathf.Min(old, along) : along;
                        }
                        if (rem > 16f || rem < 0.05f) { last.Remove(p); continue; }
                        if (rem > 7f && rem < GameConfig.Map.ChargeStartBeats - 1f)
                        {
                            var fig = p.enemy.GetComponentInChildren<EnemyFigureRun>();
                            walkFrames.TryGetValue(p, out var wf);
                            walkFrames[p] = (wf.walk + (fig != null && fig.Walking ? 1 : 0), wf.all + 1);
                        }
                        if (last.TryGetValue(p, out var l) && v - l.beat >= 0.25)
                        {
                            var d = e - l.pos; d.y = 0f;
                            if (!speeds.TryGetValue(p, out var list)) speeds[p] = list = new List<(float, float)>();
                            list.Add((rem, d.magnitude / (float)(v - l.beat)));
                            last[p] = (e, v);
                        }
                        else if (!last.ContainsKey(p)) last[p] = (e, v);
                        if (rem < 9f && rem > 8f && shotWalk.Add(p)) Shots(p.enemy, $"R18/charge_{p.runBeat}_walk");
                        if (rem < 1.5f && rem > 1f && shotSprint.Add(p)) Shots(p.enemy, $"R18/charge_{p.runBeat}_sprint");
                    }
                }
                foreach (var kv in speeds)
                {
                    var s = kv.Value;
                    checkedProps++;
                    lines.Add($"{kv.Key.name}: " + string.Join(" ", s.Where((x, i) => i % 4 == 0).Select(x => $"{x.rem:0.0}:{x.v:0.0}")));
                    float idle = s.Where(x => x.rem > GameConfig.Map.ChargeStartBeats + 0.5f).Select(x => x.v).DefaultIfEmpty(0f).Max();
                    float walk = s.Where(x => x.rem > 7f && x.rem < GameConfig.Map.ChargeStartBeats - 0.5f).Select(x => x.v).DefaultIfEmpty(-1f).Max();
                    float sprint = s.Where(x => x.rem < GameConfig.Map.ChargeSprintBeats - 0.3f).Select(x => x.v).DefaultIfEmpty(0f).Min();
                    float maxJump = 0f;
                    for (int i = 1; i < s.Count; i++) maxJump = Mathf.Max(maxJump, Mathf.Abs(s[i].v - s[i - 1].v));   // 0.25박마다 잰 속도의 변화
                    Assert.Less(idle, 0.3f, $"{kv.Key.name}: 처음엔 서 있음 ({lines.Last()})");
                    Assert.That(walk, Is.InRange(0.3f, 1.6f), $"{kv.Key.name}: 걷는 속도 (m/박) ({lines.Last()})");
                    Assert.Greater(sprint, 3f, $"{kv.Key.name}: 판정 2박 전부터 질주 ({lines.Last()})");
                    Assert.IsTrue(hitErr.ContainsKey(kv.Key), $"{kv.Key.name}: 판정 순간을 잼");
                    lines.Add($"{kv.Key.name}: 판정 순간 길 방향 오차 {hitErr[kv.Key]:0.00} m");
                    Assert.Less(hitErr[kv.Key], 0.5f, $"{kv.Key.name}: 판정 순간 판정 자리에 있음");
                    walkFrames.TryGetValue(kv.Key, out var w);
                    lines.Add($"{kv.Key.name}: 걷기 동작 {w.walk}/{w.all}프레임");
                    Assert.Greater(w.walk, w.all * 0.9f, $"{kv.Key.name}: 걷는 동안 걷기 동작 (Mixamo Standard Walk)");
                    Assert.Less(maxJump, 1.0f, $"{kv.Key.name}: 속도가 갑자기 튀지 않음 (0.25박마다 변화 m/박) ({lines.Last()})");
                }
            }
            foreach (var l in lines) Debug.Log($"[R18] 돌진 {l}");
            Assert.GreaterOrEqual(checkedProps, 2, "돌진 적 2명");
        }

        /// <summary>미리보기: 화면 카메라(1인칭)와 적을 옆 위에서 본 3인칭.</summary>
        static void Shots(Transform target, string name)
        {
            var view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            Save(view, $"harness/reports/{name}_fp.png");
            var go = new GameObject("R18Shot");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = 50f;
            var pl = GameBootstrap.Instance.player.transform;
            // 적 가까이(7 m) 플레이어 쪽 비스듬히 위에서 적을 화면 가운데에 (플레이어도 가까우면 함께 보임)
            var toPl = pl.position - target.position; toPl.y = 0f;
            var dir = toPl.sqrMagnitude > 0.01f ? toPl.normalized : -target.forward;
            var side = Vector3.Cross(Vector3.up, dir);
            cam.transform.position = target.position + (dir * 5f + side * 3f).normalized * 7f + Vector3.up * 4f;
            cam.transform.LookAt(target.position + Vector3.up * 0.9f);
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
