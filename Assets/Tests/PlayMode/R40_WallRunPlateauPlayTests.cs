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
    /// 작업 R40 (사용자 피드백 2026-11-24 5번 "벽을 탄 다음 점프해서 가는 느낌"): 실제 stage1 벽타기(32·60·206·216박)에서
    /// 카메라 연출 높이가 포물선이 아니라 벽 구간(진행 0.25~0.75) 동안 거의 그대로인지, 화면 카메라 높이(머리 출렁임 포함)도 크게 오르내리지 않는지 잰다.
    /// 예전 포물선(1.2 m × sin)은 같은 구간에서 0.35 m 오르내렸다. 몸 기울기(R06 45°)는 벽 구간 내내. 미리보기 → harness/reports/R40/.
    /// </summary>
    public class R40_WallRunPlateauPlayTests
    {
        const string Out = "harness/reports/R40";
        double now;
        GameBootstrap boot;
        StageDirector dir;

        [TearDown]
        public void TearDown() { if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel(); Time.timeScale = 1f; }

        [Test]
        public void R40_Profile_RisesHoldsFalls()
        {
            foreach (var r in new[] { Judgement.Perfect, Judgement.Normal })
            {
                Assert.AreEqual(0f, Traversal.WallRunProfile(0f, r), 1e-4f);
                Assert.AreEqual(0f, Traversal.WallRunProfile(1f, r), 1e-4f);
                for (float p = 0.3f; p <= 0.7f; p += 0.05f) Assert.AreEqual(1f, Traversal.WallRunProfile(p, r), 1e-4f, $"{r} {p:0.00}: 벽 구간 동안 그대로");
                Traversal.Offset(ObstacleCategory.WallRun, r, 1f, 0.3f, out var a, out _);
                Traversal.Offset(ObstacleCategory.WallRun, r, 1f, 0.7f, out var b, out _);
                Traversal.Offset(ObstacleCategory.WallRun, r, 1f, 0.5f, out var c, out _);
                Assert.AreEqual(a.y, c.y, 0.01f, "포물선 아님 (0.3 = 0.5 높이)");
                Assert.AreEqual(b.y, c.y, 0.01f, "포물선 아님 (0.7 = 0.5 높이)");
            }
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

        static void Shot(Camera cam, string path)
        {
            var rt = new RenderTexture(640, 360, 24);
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(640, 360, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
            RenderTexture.active = prev;
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.Destroy(t); rt.Release();
        }

        IEnumerator WallRun(double beat, double delay, string tag)
        {
            yield return Load(beat - 4);
            var view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            var body = Object.FindAnyObjectByType<PlayerBody>();
            var motionY = new List<float>(); var camH = new List<float>(); var lean = new List<float>();
            int shots = 0;
            double endBeat = beat + GameConfig.ObstacleBeats + 0.5;
            while (boot.player.RunBeat < endBeat)
            {
                now += 0.01;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) + delay - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
                yield return null;
                var run = dir.Current;
                if (run == null || !run.HasResult || !run.HasCategory || run.Category != ObstacleCategory.WallRun) continue;
                float p = dir.TraversalProgress;
                if (p < 0.25f || p > 0.75f) continue;
                motionY.Add(dir.motion.localPosition.y);
                camH.Add(view.transform.position.y - MapLayout.PointAt((float)(boot.player.VisualRunBeat * GameConfig.MetersPerBeat)).y);
                lean.Add(Mathf.Abs(body.LeanDegrees));
                if (shots < 3 && p >= 0.3f + shots * 0.2f) { Shot(view, $"{Out}/wallrun_{beat}_{tag}_{shots}.png"); shots++; }
            }
            Assert.Greater(motionY.Count, 20, $"{beat}박 {tag}: 벽 구간을 지남");
            float mv = motionY.Max() - motionY.Min(), cv = camH.Max() - camH.Min();
            Debug.Log($"[R40] {beat}박 {tag}: 벽 구간 카메라 연출 높이 {motionY.Min():0.00}~{motionY.Max():0.00} (변화 {mv:0.000} m), 화면 카메라 높이 {camH.Min():0.00}~{camH.Max():0.00} (변화 {cv:0.000} m), 몸 기울기 최소 {lean.Min():0.0}°");
            Assert.Less(mv, 0.02f, $"{beat}박 {tag}: 벽 구간 동안 카메라 연출 높이가 그대로 (포물선이면 0.35 m)");
            Assert.Less(cv, 0.25f, $"{beat}박 {tag}: 화면 카메라가 크게 오르내리지 않음 (머리 출렁임만)");
            float want = tag == "Perfect" ? GameConfig.Body.WallRunLeanDegrees : GameConfig.Body.WallRunNormalLeanDegrees;
            Assert.Greater(lean.Min(), want - 3f, $"{beat}박 {tag}: 벽 구간 내내 몸이 벽을 밟음");
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator R40_Stage_WallRuns_Perfect_Plateau()
        {
            foreach (var b in new double[] { 32, 60, 206, 216 }) yield return WallRun(b, 0.0, "Perfect");
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator R40_Stage_WallRun_Normal_Plateau()
        {
            yield return WallRun(60, 0.15, "Normal");
        }
    }
}
