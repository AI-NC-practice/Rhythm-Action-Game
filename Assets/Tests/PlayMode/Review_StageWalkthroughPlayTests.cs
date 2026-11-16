using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 검토용 (작업 R18 후속, 사용자 요청 2026-11-17 "다른 구간에 이치에 맞지 않는 점"): stage1 전체를 자동 연주하며 4박마다
    /// 1인칭 화면과 플레이어 뒤 위 3인칭을 harness/reports/review/에 저장한다. 판정은 하지 않는다 — 사람이(또는 Claude가) 그림을 본다.
    /// 오래 걸려 평소 검증에서는 빠진다 (Explicit).
    /// </summary>
    public class Review_StageWalkthroughPlayTests
    {
        [UnityTest, Explicit("검토용 미리보기 — 이름으로 골라 실행")]
        public IEnumerator Review_Stage1_Walkthrough_Shots()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            var dir = boot.director;
            double now = 0;
            boot.clock.TimeSource = () => now;
            float realTime = Time.realtimeSinceStartup;
            dir.slowMotion.RealTime = () => realTime + (float)now;
            foreach (var fx in Object.FindObjectsByType<TimeFx>(FindObjectsSortMode.None)) fx.RealTime = () => realTime + (float)now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.AllowEventSlowMotion = false;
            boot.StartRun(0);
            yield return null;
            string outDir = "harness/reports/review";
            if (System.IO.Directory.Exists(outDir)) System.IO.Directory.Delete(outDir, true);
            System.IO.Directory.CreateDirectory(outDir);
            var view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            var go = new GameObject("ReviewCam");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = 60f;
            double nextShot = 0;
            int frames = 0, shots = 0;
            var log = new System.Text.StringBuilder("beat\tstate\tenemiesVisible\n");
            while (dir.State != StageState.Cleared && frames < 60000)
            {
                now += 0.01; frames++;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
                yield return null;
                double v = boot.player.VisualRunBeat;
                if (v < nextShot) continue;
                nextShot = Mathf.Floor((float)v / 4f) * 4f + 4f;
                string tag = $"{(int)v:000}";
                Save(view, $"{outDir}/{tag}_fp.png");
                var pl = boot.player.transform;
                var back = -pl.forward; back.y = 0f; back.Normalize();
                cam.transform.position = pl.position + back * 7f + Vector3.up * 5f;
                cam.transform.LookAt(pl.position + pl.forward * 10f + Vector3.up * 1f);
                Save(cam, $"{outDir}/{tag}_tp.png");
                shots++;
                log.Append($"{v:0.0}\t{dir.State}\n");
            }
            System.IO.File.WriteAllText($"{outDir}/shots.tsv", log.ToString());
            Object.Destroy(go);
            Debug.Log($"[Review] {shots}장, 프레임 {frames}, 상태 {dir.State}");
            Assert.Greater(shots, 20);
        }

        static void Save(Camera cam, string path)
        {
            var rt = new RenderTexture(480, 270, 24);
            var old = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = old;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var t = new Texture2D(480, 270, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 480, 270), 0, 0);
            RenderTexture.active = prev;
            System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
            Object.Destroy(t);
            rt.Release();
        }
    }
}
