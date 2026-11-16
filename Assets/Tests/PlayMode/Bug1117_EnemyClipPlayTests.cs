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
    /// 사용자 버그 신고 (2026-11-17): 5챕터에서 벽을 뚫고 나오는 적, 2챕터에서 한 번 죽고 되감은 뒤 적이 지붕 아래로 사라짐.
    /// 보이는 맵 메시 안에 적 몸(0.5·1.0·1.5 m)이 들어간 프레임을 모두 찾아 기록한다. 안에 있음 = 여러 방향 광선의 첫 면이 뒷면(법선이 광선 쪽).
    /// </summary>
    public class Bug1117_EnemyClipPlayTests
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
            dir.RewindRealTime = () => realTime + (float)now;
            foreach (var fx in Object.FindObjectsByType<TimeFx>(FindObjectsSortMode.None)) fx.RealTime = () => realTime + (float)now;
            foreach (var cf in Object.FindObjectsByType<CameraFx>(FindObjectsSortMode.None)) cf.DeltaTime = () => 0.01f;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.AllowEventSlowMotion = false;
            boot.StartRun(startBeat);
            yield return null;
            if (visRoot != null) Object.Destroy(visRoot);
            visRoot = new GameObject("Clip_Visible");
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
                if (r.isPartOfStaticBatch) { if (!combined.Add(mf.sharedMesh)) continue; var go = new GameObject("vis"); go.transform.SetParent(visRoot.transform, false); go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh; }
                else if (r.enabled) { var go = new GameObject("vis"); go.transform.SetParent(visRoot.transform, false); go.transform.SetPositionAndRotation(mf.transform.position, mf.transform.rotation); go.transform.localScale = mf.transform.lossyScale; go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh; }
            }
            Physics.queriesHitBackfaces = true;
            Physics.SyncTransforms();
            view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
        }

        static readonly Vector3[] Dirs = { Vector3.up, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        /// <summary>점 p가 보이는 메시 속: 다섯 방향 중 셋 이상에서 첫 면이 뒷면.</summary>
        bool Inside(Vector3 p)
        {
            int back = 0;
            foreach (var d in Dirs)
            {
                RaycastHit best = default; float bd = float.PositiveInfinity;
                foreach (var h in Physics.RaycastAll(p, d, 30f))
                    if (h.transform.IsChildOf(visRoot.transform) && h.distance < bd) { bd = h.distance; best = h; }
                if (bd < float.PositiveInfinity && Vector3.Dot(best.normal, d) > 0.05f) back++;
            }
            return back >= 3;
        }

        bool OnScreen(Vector3 p)
        {
            var vp = view.WorldToViewportPoint(p + Vector3.up);
            return vp.z > 0f && vp.z < 60f && vp.x > -0.05f && vp.x < 1.05f && vp.y > -0.05f && vp.y < 1.05f;
        }

        readonly Dictionary<string, List<string>> found = new Dictionary<string, List<string>>();

        void Scan(string tag)
        {
            var roots = new List<(string name, Transform t)>();
            foreach (var a in Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))
            {
                var leap = a.GetComponent<EnemyLeapIn>();
                if (leap != null && leap.Phase == "Waiting") continue;
                if (a.gameObject.activeInHierarchy && !a.Defeated) roots.Add((a.name + (leap != null ? "/" + leap.Phase : "") + (a.GetComponent<EnemyApproach>() is EnemyApproach ap ? "/" + ap.Phase : ""), a.transform));
            }
            foreach (var p in Object.FindAnyObjectByType<EventPropDirector>().Props)
                if (p != null && p.enemy != null && p.enemy.gameObject.activeInHierarchy) roots.Add((p.name + (p.AmbushPhase != "" ? "/" + p.AmbushPhase : ""), p.enemy));
            double v = boot.player.VisualRunBeat;
            foreach (var (name, t) in roots)
            {
                var f = t.position;
                if (!OnScreen(f)) continue;
                foreach (float h in new[] { 0.5f, 1.0f, 1.5f })
                    if (Inside(f + Vector3.up * h))
                    {
                        string key = tag + " " + name.Split('/')[0];
                        if (!found.TryGetValue(key, out var list)) found[key] = list = new List<string>();
                        if (list.Count < 6) list.Add($"박 {v:0.00} {name} 발 {f} 높이 {h} 되감기 {dir.Rewinding}");
                        else if (list.Count == 6) list.Add("...");
                        break;
                    }
            }
        }

        IEnumerator Play(double end, string tag, bool miss, int maxFrames = 12000)
        {
            int frames = 0;
            while (boot.player.RunBeat < end && frames < maxFrames)
            {
                now += 0.01; frames++;
                if (!miss)
                    foreach (var c in boot.judge.Cues.ToArray())
                        if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                yield return null;
                Scan(tag);
            }
        }

        [UnityTest]
        public IEnumerator Bug1117_EnemyClip_Ch2DeathRewind_Ch5()
        {
            // 2챕터: 판정을 놓쳐 체력 0 → 챕터 되감기 → 그 뒤 자동 연주
            yield return Load(56);
            int rewinds = dir.RewindCount;
            int frames = 0;
            while (dir.RewindCount == rewinds && frames < 12000)
            {
                now += 0.01; frames++;
                yield return null;
                Scan("ch2-before");
            }
            Debug.Log($"[Clip] 2챕터 죽음 되감기: {dir.LastRewind} 박 {boot.player.RunBeat:0.0}");
            frames = 0;
            while ((dir.Rewinding || dir.HitPending) && frames < 2000) { now += 0.01; frames++; yield return null; Scan("ch2-rewind"); }
            yield return Play(100, "ch2-after", false);
            // 5챕터
            yield return Load(200);
            yield return Play(258, "ch5", false);
            foreach (var kv in found) Debug.Log($"[Clip] {kv.Key}: {kv.Value.Count}회\n  " + string.Join("\n  ", kv.Value));
            Debug.Log($"[Clip] 끝: 찾은 적 {found.Count}");
            Assert.IsEmpty(found.Keys, "보이는 적의 몸이 담·지붕·집 속에 들어간 프레임 (2챕터 낮은 담 72박 돌진 적, 5챕터 궁궐 담 220박 돌진 적 — 2026-11-17 고침)");
        }
    }
}
