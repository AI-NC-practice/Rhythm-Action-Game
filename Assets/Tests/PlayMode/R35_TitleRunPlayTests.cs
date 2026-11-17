using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R35: 시작화면 — 주인공이 기와지붕 위를 끝없이 달림(지붕 조각을 뒤에서 앞으로 다시 붙임, 새로 만들지 않음),
    /// 시작을 누르면 지붕에서 옆으로 떨어지며 어두워지고 게임 씬으로. 미리보기 → harness/reports/R35/.
    /// </summary>
    public class R35_TitleRunPlayTests
    {
        const string Out = "harness/reports/R35";

        static void Shot(Camera cam, string path)
        {
            var big = new RenderTexture(960, 540, 24);
            var old = cam.targetTexture;
            cam.targetTexture = big;
            cam.Render();
            cam.targetTexture = old;
            var prev = RenderTexture.active;
            RenderTexture.active = big;
            var t = new Texture2D(960, 540, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            RenderTexture.active = prev;
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.Destroy(t);
            big.Release();
        }

        [UnityTest]
        public IEnumerator R35_Title_EndlessRoofRun_ThenFallIntoGame()
        {
            Directory.CreateDirectory(Out);
            yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            var runner = Object.FindAnyObjectByType<TitleRunner>();
            Assert.IsNotNull(runner, "시작화면 지붕 달리기");
            runner.DeltaTime = () => 1f / 60f;
            var cam = Camera.main;
            Debug.Log($"[R35] 지붕 길이 {runner.ridgeLength:0.00} 높이 {runner.ridgeTop:0.00}, 자리 {string.Join(", ", System.Linq.Enumerable.Select(runner.ridge, r => r.position.ToString()))}");
            Shot(cam, $"{Out}/title_first.png");
            int objects0 =Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length;
            int frames = 0, onRidge = 0, offRidge = 0;
            float minZ = float.PositiveInfinity;
            bool shot = false;
            // 약 20초 동안 달림 (지붕 조각 길이 × 수보다 훨씬 김 — 다시 붙이기가 여러 번 일어나야 함)
            for (int f = 0; f < 1200; f++)
            {
                yield return null;
                frames++;
                var p = runner.player.position;
                if (!runner.Hopping) { if (Mathf.Abs(p.y - runner.ridgeTop) < 0.05f) onRidge++; else offRidge++; }
                // 주인공 발밑(z=0) 가까이에 지붕 조각이 있거나 틈을 뛰는 중
                float nearest = float.PositiveInfinity;
                foreach (var r in runner.ridge) nearest = Mathf.Min(nearest, Mathf.Abs(r.position.z) - runner.ridgeLength * 0.5f);
                minZ = Mathf.Min(minZ, nearest);
                if (!runner.Hopping) Assert.LessOrEqual(nearest, 0.01f, $"프레임 {f}: 달리는 발밑에 지붕이 있음");
                if (!shot && f == 300) { shot = true; Shot(cam, $"{Out}/title_run.png"); }
            }
            int objects1 = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length;
            Debug.Log($"[R35] 달리기 {frames}프레임, 다시 붙임 {runner.Recycled}번, 용마루 위 {onRidge}, 벗어남 {offRidge}, 물체 수 {objects0}→{objects1}");
            Assert.Greater(runner.Recycled, runner.ridge.Length, "지붕 조각을 뒤에서 앞으로 여러 번 다시 붙임 (끝없이 달림)");
            Assert.AreEqual(objects0, objects1, "새로 만들거나 지운 물체가 없음");
            Assert.AreEqual(0, offRidge, "뛰는 중이 아니면 발이 용마루 위");

            // 시작 → 지붕에서 떨어짐 → 어두워짐 → 게임 씬
            var menu = Object.FindAnyObjectByType<TitleMenu>();
            menu.StartButton.onClick.Invoke();
            Assert.IsTrue(runner.Falling, "시작을 누르면 떨어지기 시작");
            Assert.IsFalse(menu.MainPanel.gameObject.activeSelf, "메뉴를 숨김");
            float y0 = runner.player.position.y, x0 = runner.player.position.x;
            bool fallShot = false; float minY = y0; float maxX = x0;
            float until = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name == "Title" && Time.realtimeSinceStartup < until)
            {
                if (runner != null)
                {
                    minY = Mathf.Min(minY, runner.player.position.y);
                    maxX = Mathf.Max(maxX, runner.player.position.x);
                    if (!fallShot && runner.player.position.y < y0 - 1.0f) { fallShot = true; Shot(cam, $"{Out}/title_fall.png"); }
                }
                yield return null;
            }
            Debug.Log($"[R35] 떨어지기: 높이 {y0:0.00}→{minY:0.00}, 옆 {x0:0.00}→{maxX:0.00}");
            Assert.Greater(y0 - minY, 2.0f, "지붕 아래로 떨어짐");
            Assert.Greater(maxX - x0, 1.5f, "옆으로 미끄러져 지붕 밖으로");
            Assert.IsTrue(fallShot, "떨어지는 미리보기");
            Assert.AreEqual("Game", SceneManager.GetActiveScene().name, "떨어진 뒤 게임 시작");
        }
    }
}
