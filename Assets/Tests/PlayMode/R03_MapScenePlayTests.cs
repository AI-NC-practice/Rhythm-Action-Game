using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R03: 다시 지은 맵 (Game 씬). 적·복선 발밑 지지면, 챕터마다 눈높이 미리보기 PNG(harness/reports/R03/), 배경 파노라마.
    /// </summary>
    public class R03_MapScenePlayTests
    {
        [UnityTest]
        public IEnumerator R03_EnemiesAndForeshadow_StandOnVisibleRoofWallOrGround()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var report = Object.FindFirstObjectByType<MapSupportReport>();
            Assert.IsNotNull(report, "빌드 때 잰 발밑 기록");
            Assert.Greater(report.entries.Count, 20);
            var bad = new System.Collections.Generic.List<string>();
            foreach (var e in report.entries)
            {
                string at = $"{e.figure} [{e.kind}] 발 {e.footY:0.00} m, 발밑 {e.support} {e.supportY:0.00} m";
                if (float.IsNaN(e.supportY)) bad.Add("발밑에 보이는 지붕·담·땅이 없음: " + at);
                else if (e.kind == "foreshadow" && e.supportY <= 1.5f) bad.Add("복선 적이 지붕·담 위가 아님: " + at);
                else if (e.kind == "runner" && !e.snapped) bad.Add("달리며 나오는 적이 발밑에 맞춰지지 않음: " + at);
                else if (e.kind == "enemy" && Mathf.Abs(e.footY - e.supportY) > GameConfig.Map.SupportTolerance) bad.Add("멈춰 싸우는 적 발밑이 멂: " + at);
            }
            Assert.IsEmpty(bad, string.Join(" | ", bad));
            foreach (var f in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name.StartsWith("Prop_foreshadow_")))
            {
                var e = report.entries.FirstOrDefault(x => x.kind == "foreshadow" && x.figure == f.name + " @" + f.parent.name);
                Assert.IsNotNull(e, f.name);
                Assert.AreEqual(e.supportY, f.position.y, 0.01f, $"{f.name}: 지붕·담 윗면에 섬");
            }
        }

        static readonly (string name, float beat)[] Views =
        {
            ("ch1_a_alley", 18), ("ch1_b_market", 42), ("ch2_a_backstreet", 62), ("ch2_b_yangban", 97),
            ("ch3_roofs", 126), ("ch4_yard", 182), ("ch5_a_palace_road", 204), ("ch5_b_wall_top", 232), ("ch6_gate", 254),
        };

        [UnityTest]
        public IEnumerator R03_ChapterPreviews_EyeHeightPng()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            if (boot != null) boot.judge.readKeyboard = false;
            var cam = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            cam.transform.SetParent(null, true);
            foreach (var b in Object.FindObjectsByType<PlayerBody>(FindObjectsSortMode.None)) b.enabled = false;
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            Directory.CreateDirectory("harness/reports/R03");
            var inst = Object.FindFirstObjectByType<DressingInstancer>();
            foreach (var (name, beat) in Views)
            {
                var p = MapLayout.PointAt(beat * GameConfig.MetersPerBeat, out var fwd);
                yield return null;
                // 자세를 정한 바로 그 프레임에 그린다 (한 프레임 지나면 카메라 연출 스크립트가 방향을 되돌린다)
                cam.transform.SetPositionAndRotation(p + Vector3.up * GameConfig.EyeHeight, Quaternion.LookRotation(fwd + Vector3.down * 0.05f));
                if (inst != null) inst.Draw();
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes($"harness/reports/R03/{name}.png", tex.EncodeToPNG());
                Object.Destroy(tex);
            }
            cam.targetTexture = null;
            Object.Destroy(rt);
            foreach (var (name, _) in Views) Assert.IsTrue(File.Exists($"harness/reports/R03/{name}.png"), name);
        }

        [UnityTest]
        public IEnumerator R03_Backdrop_GoesAroundOnce_NoRepeat()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var go = GameObject.Find("Backdrop");
            Assert.IsNotNull(go);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(1f, mesh.uv.Max(u => u.x), 1e-3f, "그림이 둘레를 한 번만 돈다 (예전 3번)");
            var tex = go.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex");
            Assert.IsNotNull(tex);
            Assert.Greater(tex.width, 6000, "R02 배경 6장을 이어 붙인 파노라마");
        }
    }
}
