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
    /// 작업 R07: 적 이동 동작 (사용자 피드백 4번 "유령처럼 스윽 다가옴", 7번 "적도 제자리에서 달림"), 공중에 뜬 적 (1번).
    /// 실제 stage1 차트를 자동 연주로 지나며 적 Animator(EnemyAnimator)와 사건 소품 적(EnemyFigureRun)을 매 프레임 본다.
    /// </summary>
    public class R07_EnemyRunPlayTests
    {
        double now;
        GameBootstrap boot;
        StageDirector dir;

        [TearDown]
        public void TearDown()
        {
            if (supportRoot != null) Object.Destroy(supportRoot);
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
            BuildSupportColliders();
        }

        class Stat
        {
            public int runFrames, movingFrames, glideFrames, inPlaceRunFrames, idleFrames, footFrames, floatFrames, insideFrames;
            public float maxSpeed, maxGap;
            public string worst;
            public override string ToString() => $"이동 {movingFrames}, 달리기 {runFrames}, 미끄러짐 {glideFrames}, 제자리 달리기 {inPlaceRunFrames}, 대기 {idleFrames}, 최고 {maxSpeed:0.0} m/s, 발밑 검사 {footFrames} 중 뜸 {floatFrames} (최대 {maxGap:0.00} m {worst}), 건물 속 {insideFrames}";
        }

        const float FootTolerance = 0.35f;
        GameObject supportRoot;

        /// <summary>
        /// 보이는 맵 메시에 검사용 충돌체를 붙인다 (장식 지붕·담은 충돌체가 없다).
        /// 길 옆 장식은 DressingInstancer의 (메시, 변환), 나머지 맵은 정적 배칭으로 합쳐진 월드 좌표 메시(원점에 둠)나 원래 메시.
        /// </summary>
        void BuildSupportColliders()
        {
            supportRoot = new GameObject("R07_Support");
            supportRootStatic = supportRoot;
            int n = 0;
            foreach (var inst in Object.FindObjectsByType<DressingInstancer>(FindObjectsSortMode.None))
                foreach (var g in inst.groups)
                {
                    if (g.subMesh != 0 || g.mesh == null || g.matrices == null) continue;
                    foreach (var m in g.matrices)
                    {
                        var go = new GameObject("dress");
                        go.transform.SetParent(supportRoot.transform, false);
                        go.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
                        go.transform.localScale = m.lossyScale;
                        go.AddComponent<MeshCollider>().sharedMesh = g.mesh;
                        n++;
                    }
                }
            var combined = new HashSet<Mesh>();
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var mesh = mf.sharedMesh;
                var r = mf.GetComponent<MeshRenderer>();
                if (mesh == null || r == null || mf.name == "Backdrop" || mf.GetComponentInParent<EnemyActor>() != null || mf.GetComponentInParent<PathFollower>() != null) continue;
                if (mf.GetComponentInParent<EnemyFigureRun>() != null) continue;
                if (r.isPartOfStaticBatch)
                {
                    if (!combined.Add(mesh)) continue;
                    var go = new GameObject("batch");
                    go.transform.SetParent(supportRoot.transform, false);
                    go.AddComponent<MeshCollider>().sharedMesh = mesh;   // 합쳐진 메시는 월드 좌표
                }
                else if (r.enabled && mf.GetComponent<Collider>() == null)
                {
                    var go = new GameObject("mesh");
                    go.transform.SetParent(supportRoot.transform, false);
                    go.transform.SetPositionAndRotation(mf.transform.position, mf.transform.rotation);
                    go.transform.localScale = mf.transform.lossyScale;
                    go.AddComponent<MeshCollider>().sharedMesh = mesh;
                }
                n++;
            }
            Physics.SyncTransforms();
            Debug.Log($"[R07] 검사용 충돌체 {n}개 (합친 메시 {combined.Count})");
        }

        /// <summary>
        /// 발(루트) 아래 지지면까지 거리 (m). 발 딛는 폭(앞뒤 0.35 m)의 세 점에서 위 0.3 m부터 아래로 쏘아 가장 가까운 값
        /// (기와집 이음매 골처럼 좁은 틈 위를 지나는 순간은 뜬 것으로 치지 않음). 없으면 무한대.
        /// </summary>
        static GameObject supportRootStatic;

        /// <summary>
        /// (작업 R07 후속) 보이는 메시로 만든 검사용 충돌체만 센다 — 게임의 보이지 않는 충돌 상자(지붕 길 상자 등)는 보이는 지붕보다 넓을 수 있어
        /// 공중에 뜬 무사를 놓쳤다 (사용자 피드백 2026-11-17).
        /// </summary>
        static float FootGap(Transform root)
        {
            float best = float.PositiveInfinity;
            var fwd = Vector3.ProjectOnPlane(root.forward, Vector3.up).normalized;
            foreach (float o in new[] { 0f, 0.35f, -0.35f })
            {
                var from = root.position + fwd * o + Vector3.up * 0.3f;
                foreach (var h in Physics.RaycastAll(from, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.transform.IsChildOf(root)) continue;
                    if (supportRootStatic != null && !h.transform.IsChildOf(supportRootStatic.transform)) continue;
                    best = Mathf.Min(best, Mathf.Abs(h.distance - 0.3f));
                }
            }
            return best;
        }

        static void CountFoot(Stat st, Transform root)
        {
            // 작업 R07 후속: 길 옆 집·담 속을 지나감 (허리 높이가 장식 윗면보다 아래이고, 발이 장식 바닥보다 위)
            var waist = root.position + Vector3.up * 1.0f;
            float top = DressingInstancer.TopBelow(waist + Vector3.up * 30f, 999f);
            if (!float.IsNegativeInfinity(top) && top > waist.y + 0.3f && InsideDressing(waist)) st.insideFrames++;
            float gap = FootGap(root);
            st.footFrames++;
            if (gap > FootTolerance)
            {
                st.floatFrames++;
                if (st.worst == null || gap > st.maxGap) { st.maxGap = Mathf.Min(gap, 99f); st.worst = $"@{root.position}"; }
            }
        }

        /// <summary>자동 연주(PERFECT)로 endBeat까지 달리며 적마다 매 프레임 상태를 모은다.</summary>
        IEnumerator Play(double endBeat, Dictionary<string, Stat> stats, System.Func<bool> stopWhen = null, int maxFrames = 6000)
        {
            int frames = 0;
            var anims = Object.FindObjectsByType<EnemyAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var figs = Object.FindObjectsByType<EnemyFigureRun>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            while (boot.player.RunBeat < endBeat && frames < maxFrames && (stopWhen == null || !stopWhen()))
            {
                now += 0.01; frames++;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
                yield return null;
                foreach (var a in anims)
                {
                    if (a == null || !a.isActiveAndEnabled || a.actor == null || !a.actor.gameObject.activeInHierarchy) continue;
                    var st = Get(stats, "E:" + a.actor.name);
                    Count(st, a.MoveSpeed, a.CurrentState == EnemyMoves.Run, a.CurrentState == EnemyMoves.Idle);
                    if (!a.CurrentState.StartsWith("Defeat_") && a.CurrentState != EnemyMoves.Hit && Near(a.actor.transform)) CountFoot(st, a.actor.transform);
                }
                foreach (var f in figs)
                {
                    if (f == null || !f.isActiveAndEnabled) continue;
                    var st = Get(stats, "F:" + f.transform.parent.name + "/" + f.name);
                    Count(st, f.MoveSpeed, f.Moving, !f.Moving);   // 작업 R18 후속: 걷기도 이동 동작
                    var prop = f.GetComponentInParent<EventProp>();
                    if ((prop == null || (!prop.EnemyDown && !prop.ChaseHopping)) && Near(f.transform)) CountFoot(st, f.transform);   // 집 사이 틈을 뛰어넘는 중은 제외
                }
            }
        }

        /// <summary>점이 보이는 길 옆 장식(검사용 충돌체) 안에 있음: 위·아래로 쏜 광선이 모두 그 메시에 맞음.</summary>
        static bool InsideDressing(Vector3 p)
        {
            if (supportRootStatic == null) return false;
            bool up = false, down = false;
            foreach (var h in Physics.RaycastAll(p, Vector3.up, 20f)) if (h.transform.IsChildOf(supportRootStatic.transform) && h.collider.name == "dress") { up = true; break; }
            foreach (var h in Physics.RaycastAll(p, Vector3.down, 20f)) if (h.transform.IsChildOf(supportRootStatic.transform) && h.collider.name == "dress") { down = true; break; }
            return up && down;
        }

        /// <summary>플레이어 앞뒤 60 m 안 (보이는 곳)만 발밑을 본다.</summary>
        bool Near(Transform t) => (t.position - boot.player.transform.position).sqrMagnitude < 60f * 60f;

        static Stat Get(Dictionary<string, Stat> d, string k) { if (!d.TryGetValue(k, out var s)) d[k] = s = new Stat(); return s; }

        static void Count(Stat st, float speed, bool running, bool idle)
        {
            st.maxSpeed = Mathf.Max(st.maxSpeed, speed);
            if (speed > 1.5f) { st.movingFrames++; if (!running && idle) st.glideFrames++; }
            if (running) st.runFrames++;
            if (idle) st.idleFrames++;
            if (running && speed < 0.2f) st.inPlaceRunFrames++;
        }

        static void Report(string tag, Dictionary<string, Stat> stats)
        {
            foreach (var kv in stats.Where(k => k.Value.movingFrames > 0 || k.Value.runFrames > 0))
                Debug.Log($"[R07] {tag} {kv.Key}: {kv.Value}");
        }

        /// <summary>1구간 추격(36박)·매복(48박): 움직이는 소품 적은 달리고, 멈추면 대기.</summary>
        [UnityTest]
        public IEnumerator R07_EventFigures_RunWhileMoving_NoGlide()
        {
            var stats = new Dictionary<string, Stat>();
            yield return Load(30);
            yield return Play(52, stats);
            Report("ch1", stats);
            var ch2 = new Dictionary<string, Stat>();   // 72박 밧줄 그네 + 돌진 적 (사용자 피드백 2026-11-17 "벽을 뚫고 오네")
            yield return Load(56);
            yield return Play(76, ch2);
            Report("ch2", ch2);
            foreach (var kv in ch2) stats["2:" + kv.Key] = kv.Value;
            var ch3 = new Dictionary<string, Stat>();
            yield return Load(112);
            yield return Play(117.6, ch3, null, 4000);
            var chaseFig = Object.FindObjectsByType<EnemyFigureRun>(FindObjectsSortMode.None).Where(f => f.Running && Near(f.transform)).OrderBy(f => (f.transform.position - boot.player.transform.position).sqrMagnitude).FirstOrDefault();
            if (chaseFig != null) { System.IO.Directory.CreateDirectory("harness/reports/R07"); Shots(chaseFig.transform, "R07/chase_run"); }
            yield return Play(166, ch3, null, 4000);
            Report("ch3", ch3);
            foreach (var kv in ch3) stats["3:" + kv.Key] = kv.Value;
            var figs = stats.Where(k => k.Key.StartsWith("F:") && k.Value.movingFrames > 20).ToList();
            Assert.Greater(figs.Count, 0, "움직이는 사건 소품 적이 있음");
            foreach (var kv in figs)
            {
                Assert.Greater(kv.Value.runFrames, kv.Value.movingFrames / 2, $"{kv.Key}: 움직일 때 달리기 ({kv.Value})");
                Assert.LessOrEqual(kv.Value.glideFrames, 8, $"{kv.Key}: 대기 자세로 미끄러짐 ({kv.Value})");
                Assert.LessOrEqual(kv.Value.inPlaceRunFrames, 8, $"{kv.Key}: 제자리 달리기 ({kv.Value})");
            }
            AssertGrounded(stats);
        }

        /// <summary>공중에 뜬 적 없음 (사용자 피드백 1번): 보이는 동안 발밑 0.35 m 안에 지붕·담·땅이 있는 프레임이 98% 이상.</summary>
        static void AssertGrounded(Dictionary<string, Stat> stats)
        {
            foreach (var kv in stats.Where(k => k.Value.footFrames > 0))
            {
                Assert.LessOrEqual(kv.Value.floatFrames, kv.Value.footFrames / 50, $"{kv.Key}: 공중에 뜸 ({kv.Value})");
                Assert.LessOrEqual(kv.Value.insideFrames, kv.Value.footFrames / 50, $"{kv.Key}: 건물을 뚫고 지나감 ({kv.Value})");
            }
        }

        /// <summary>5구간 돌진(220박)·전투, 6구간 무사(260박~): 적이 다가올 때 달리고, 멈춰 싸울 때는 제자리에서 달리지 않음.</summary>
        [UnityTest]
        public IEnumerator R07_CombatEnemies_RunWhenMoving_NotInPlace()
        {
            var stats = new Dictionary<string, Stat>();
            yield return Load(214);
            yield return Play(234, stats);
            Report("ch5", stats);
            yield return Load(256);
            var boss = new Dictionary<string, Stat>();
            yield return Play(400, boss, () => dir.State == StageState.Cleared, 4000);
            Report("ch6", boss);
            foreach (var kv in boss) stats["6:" + kv.Key] = kv.Value;

            var all = stats.Where(k => k.Value.movingFrames + k.Value.runFrames + k.Value.idleFrames > 0).ToList();
            Assert.Greater(all.Count, 1, "적을 지남");
            foreach (var kv in all)
            {
                Assert.LessOrEqual(kv.Value.inPlaceRunFrames, 8, $"{kv.Key}: 멈춰 있는데 달리기 ({kv.Value})");
                Assert.LessOrEqual(kv.Value.glideFrames, Mathf.Max(8, kv.Value.movingFrames / 4), $"{kv.Key}: 대기 자세로 미끄러짐 ({kv.Value})");
            }
            Assert.IsTrue(all.Any(k => k.Value.runFrames > 10), "다가오거나 도는 적이 달리기를 함");
            AssertGrounded(stats);
        }

        /// <summary>공중에 뜬 적 없음: 모든 적(전투 적·사건 소품 적·복선)의 발 아래 0.35 m 안에 충돌면(지붕·담·땅)이 있음 — 물리 광선 검사.</summary>
        [UnityTest]
        public IEnumerator R07_AllEnemies_FeetOnSupport_Raycast()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            BuildSupportColliders();
            yield return new WaitForFixedUpdate();
            var bad = new List<string>();
            int n = 0;
            foreach (var an in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var ctrl = an.runtimeAnimatorController;
                if (ctrl == null || !ctrl.name.StartsWith("Enemy_")) continue;
                var root = an.transform.parent;
                if (root == null) continue;
                n++;
                float gap = FootGap(root);
                bool ok = gap <= FootTolerance;
                if (!ok) bad.Add($"{root.name} @{root.parent?.name} 발 {root.position} 틈 {gap:0.00}");
            }
            Debug.Log($"[R07] 적 {n}개 발밑 검사, 실패 {bad.Count}: {string.Join(" | ", bad)}");
            Assert.Greater(n, 10, "적이 있음");
            Assert.IsEmpty(bad, "발밑에 지붕·담·땅이 없는 적");
        }

        /// <summary>
        /// 지붕 위 PERFECT 발차기 (126박 지붕 위 적): Mixamo Flying Back Death 상태로 지붕 가장자리 쪽으로 날아가
        /// 지붕 반폭(4 m)보다 멀리, 2.5 m 넘게 아래로 떨어져 아래 면에 닿고, 카메라가 그쪽으로 고개를 돌린다. 미리보기 PNG.
        /// </summary>
        [UnityTest]
        public IEnumerator R07_RoofKick_FliesOffRoofEdge_LandsBelow_CameraFollows()
        {
            yield return Load(122);
            var stats = new Dictionary<string, Stat>();
            yield return Play(140, stats, () => dir.CurrentEncounter != null && dir.CurrentEncounter.Execution == ExecutionKind.KickOffRoof, 3000);
            var enc = dir.CurrentEncounter;
            Assert.IsNotNull(enc, "지붕 위 전투");
            Assert.AreEqual(ExecutionKind.KickOffRoof, enc.Execution, "지붕 위 PERFECT 발차기");
            var actor = enc.Actors[0];
            Assert.AreEqual(EnemyMoves.DefeatState(ExecutionKind.KickOffRoof), actor.anim.CurrentState, "Flying Back Death 상태");
            Assert.AreNotEqual(Vector3.zero, actor.KickDirection, "날아가는 방향");
            var start = actor.transform.position;
            var pf = boot.player.transform;
            Vector3 p0 = pf.position, right0 = pf.right;   // 발차기 순간의 플레이어 자리·오른쪽 (그 뒤 다시 달린다)
            float startLat = Vector3.Dot(start - p0, right0);
            var hooks = Object.FindAnyObjectByType<PresentationHooks>();
            var cfx = Object.FindAnyObjectByType<CameraFx>();
            bool looked = false, shot = false;
            int clipFrames = 0;
            System.IO.Directory.CreateDirectory("harness/reports/R07");
            float until = Time.realtimeSinceStartup + 4f;   // 떨어지는 궤적은 실제 시간으로 진행 (T14와 같음)
            while (!actor.KickLanded && Time.realtimeSinceStartup < until)
            {
                now += 0.01;
                foreach (var c in boot.judge.Cues.ToArray())   // 계속 자동 연주 (놓쳐서 되감기면 적이 제자리로 돌아감)
                    if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
                yield return null;
                if (cfx != null && cfx.YawOwner == CameraOwner.Execution && Mathf.Abs(cfx.Yaw) > 5f) looked = true;
                if (!shot && actor.transform.position.y < start.y - 1.0f) { shot = true; Shots(actor.transform, "R07/kick_fall"); }
                // 날아가는 동안 길 옆 집 지붕 속을 지나지 않음 (떨어질 자리 1 m 안은 뺌)
                var ap = actor.transform.position;
                if (!actor.KickLanded && new Vector2(ap.x - actor.kickLanding.x, ap.z - actor.kickLanding.z).magnitude > 1.2f)
                {
                    float top = DressingInstancer.TopBelow(ap, 999f);
                    if (!float.IsNegativeInfinity(top) && ap.y < top - 0.05f && ap.y > top - 6f) clipFrames++;
                }
            }
            Assert.IsTrue(actor.KickLanded, "아래 면에 닿음");
            var end = actor.transform.position;
            float lat = Vector3.Dot(end - p0, right0);
            Debug.Log($"[R07] roof kick: start {start} end {end} lateral {startLat:0.00}->{lat:0.00} drop {start.y - end.y:0.00} look {hooks?.LastKickLookYaw:0.0}");
            Assert.Greater(Mathf.Abs(lat), 4.0f, "지붕 반폭(4 m) 밖으로 날아감");
            Assert.Greater(start.y - end.y, 2.5f, "지붕 아래로 떨어짐 (골목·마당·낮은 지붕)");
            Assert.IsTrue(actor.hasKickLanding, "빌드 때 고른 떨어질 자리");
            Assert.Less(new Vector2(end.x - actor.kickLanding.x, end.z - actor.kickLanding.z).magnitude, 1.5f, "고른 자리에 떨어짐");
            Assert.AreEqual(actor.kickLanding.y, end.y, 0.05f, "보이는 면 위 (집 속으로 빠지지 않음)");
            Assert.IsTrue(looked, "카메라가 떨어지는 쪽으로 고개를 돌림");
            Assert.AreEqual(0, clipFrames, "날아가는 동안 옆집 지붕을 뚫지 않음");
            Assert.Greater(Mathf.Abs(hooks.LastKickLookYaw), 5f);
            Shots(actor.transform, "R07/kick_landed");
        }

        /// <summary>미리보기: 화면 카메라(1인칭)와 적을 옆 위에서 본 3인칭.</summary>
        static void Shots(Transform target, string name)
        {
            var view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            Save(view, $"harness/reports/{name}_fp.png");
            var go = new GameObject("R07Shot");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = 55f;
            var pl = GameBootstrap.Instance.player.transform;
            var back = target.position - pl.position; back.y = 0f; back.Normalize();
            cam.transform.position = target.position - back * 6f + Vector3.Cross(Vector3.up, back) * 3f;   // 적 가까이, 플레이어 쪽 비스듬히 위 (지붕들 위)
            cam.transform.position = new Vector3(cam.transform.position.x, Mathf.Max(target.position.y, 5f) + 4.5f, cam.transform.position.z);
            cam.transform.LookAt(target.position + Vector3.up * 0.6f);
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
