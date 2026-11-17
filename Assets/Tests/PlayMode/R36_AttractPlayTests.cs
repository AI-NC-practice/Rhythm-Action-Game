using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R36 (A안): 시작화면 = 게임 씬. 메뉴가 떠 있는 동안 주인공이 시작선 뒤 진입로를 달리고 3인칭 카메라가 뒤를 따라감,
    /// 시작을 누르면 계속 달려 시작선에서 1인칭으로 1챕터가 끊김 없이 시작. 미리보기 → harness/reports/R36/.
    /// </summary>
    public class R36_AttractPlayTests
    {
        const string Out = "harness/reports/R36";

        [TearDown]
        public void TearDown() { AttractMode.AttractOnLoad = false; Time.timeScale = 1f; }

        static void Shot(Camera cam, string path)
        {
            var big = new RenderTexture(960, 540, 24);
            var old = cam.targetTexture;
            bool en = cam.enabled;
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
        public IEnumerator R36_GameScene_AttractRun_ThenSeamlessStart()
        {
            Directory.CreateDirectory(Out);
            AttractMode.AttractOnLoad = true;
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            var attract = boot.attract;
            Assert.IsNotNull(attract, "게임 씬에 시작화면");
            Assert.IsTrue(attract.Active, "게임 씬이 시작화면부터");
            Assert.IsFalse(boot.clock.IsRunning, "시작 전에는 박자·음악이 돌지 않음");
            Assert.IsTrue(attract.menu.activeSelf, "메뉴가 보임");
            Assert.IsFalse(boot.playerCamera.enabled, "1인칭 카메라 대신 3인칭");
            attract.DeltaTime = () => 1f / 60f;
            var menu = attract.menu.GetComponent<TitleMenu>();

            // 진입로 달리기 (끝에 닿으면 처음부터): 앞으로 나아가고, 카메라는 주인공 뒤·위
            foreach (var g in attract.hideDuringAttract) Assert.IsFalse(g.activeSelf, $"시작화면 동안 {g.name} 숨김");
            Assert.Greater(attract.hideDuringAttract.Length, 0, "HUD를 숨길 목록");
            float d0 = attract.ApproachDistance;
            int loops = 0; float last = d0; bool shot = false; float maxFade = 0f;
            for (int f = 0; f < 900; f++)
            {
                yield return null;
                maxFade = Mathf.Max(maxFade, attract.FadeAlpha);
                bool jumped = attract.ApproachDistance < last - 5f;   // 되풀이한 프레임: 카메라는 LateUpdate에서 따라옴 (코루틴은 그 전에 깸)
                if (jumped) loops++;
                last = attract.ApproachDistance;
                if (jumped) continue;
                var pl = boot.player.transform; var cam = attract.AttractCamera.transform;
                Assert.Less(Vector3.Dot(cam.position - pl.position, pl.forward), -2f, $"프레임 {f}: 카메라가 주인공 뒤");
                Assert.Greater(cam.position.y - pl.position.y, 1f, "카메라가 주인공보다 위");
                Assert.LessOrEqual(attract.ApproachDistance, 0.01f, "시작선을 넘지 않음");
                if (!shot && f == 150) { shot = true; Shot(attract.AttractCamera, $"{Out}/attract_run.png"); }
            }
            Debug.Log($"[R36] 진입로 {d0:0.0} m부터, 되풀이 {loops}번, 지금 {attract.ApproachDistance:0.0} m");
            Assert.Greater(loops, 0, "진입로 끝에서 처음부터 되풀이");
            Assert.Greater(maxFade, 0.95f, "되풀이할 때 어두워졌다 밝아짐");
            Assert.IsFalse(boot.clock.IsRunning, "메뉴 동안 시작 안 함");

            // 시작 → 계속 달려 시작선에서 1인칭으로
            for (int f = 0; f < 60; f++) yield return null;   // 페이드 중이 아닐 때
            menu.StartButton.onClick.Invoke();
            Assert.IsTrue(attract.Leading, "시작을 누르면 시작선까지 달림");
            Assert.IsFalse(menu.MainPanel.gameObject.activeSelf, "메뉴 숨김");
            float prevD = attract.ApproachDistance; bool midShot = false;
            Vector3 lastPos = boot.player.transform.position; float maxStep = 0f;
            for (int f = 0; f < 600 && attract.Active; f++)
            {
                yield return null;
                var p = boot.player.transform.position;
                maxStep = Mathf.Max(maxStep, (p - lastPos).magnitude);
                lastPos = p;
                if (attract.Active) Assert.GreaterOrEqual(attract.ApproachDistance, prevD - 1e-3f, "멈추거나 뒤로 가지 않음");
                if (attract.Active) prevD = attract.ApproachDistance;
                if (!midShot && attract.Active && attract.ApproachDistance > -6f) { midShot = true; Shot(attract.AttractCamera, $"{Out}/attract_lead.png"); }
                if (attract.Active && attract.Leading && attract.AttractCamera != null)
                {
                    // 1인칭 몸(머리 없음)은 시작선까지 숨김 — 목 단면이 보이지 않음
                    var pb = Object.FindAnyObjectByType<PlayerBody>();
                    Assert.IsFalse(pb.VisibleBody.enabled, "옮겨 가는 동안 머리 없는 1인칭 몸은 숨김");
                }
            }
            Assert.IsFalse(attract.Active, "시작선에 닿아 시작화면이 끝남");
            yield return null;
            var p1 = boot.player.transform.position;
            maxStep = Mathf.Max(maxStep, (p1 - lastPos).magnitude);
            Debug.Log($"[R36] 시작: 한 프레임 최대 이동 {maxStep:0.00} m, 1챕터 시계 {boot.clock.CurrentBeat:0.00}박");
            Assert.IsTrue(boot.clock.IsRunning, "1챕터 박자·음악 시작");
            Assert.IsTrue(boot.playerCamera.enabled, "1인칭 카메라");
            foreach (var g in attract.hideDuringAttract) Assert.IsTrue(g.activeSelf, $"시작하면 {g.name} 다시 보임");
            Assert.IsNull(attract.AttractCamera, "3인칭 카메라 정리");
            Assert.Less(maxStep, 0.6f, "주인공이 순간이동하지 않음 (끊김 없이 이어짐)");
            Assert.Less(boot.player.RunBeat, 1.0, "시작선(0박)부터 달림");
            for (int f = 0; f < 30; f++) yield return null;
            Shot(boot.playerCamera, $"{Out}/attract_after_start_fp.png");
        }

        /// <summary>일시정지·결과의 "타이틀로" → 게임 씬 시작화면.</summary>
        [UnityTest]
        public IEnumerator R36_ToTitle_FromPauseAndResult_OpensAttract()
        {
            foreach (int which in new[] { 0, 1 })
            {
                AttractMode.AttractOnLoad = false;
                yield return SceneManager.LoadSceneAsync("Game");
                yield return null;
                Assert.IsFalse(GameBootstrap.Instance.attract.Active, "에디터 테스트는 바로 게임");
                if (which == 0) Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include).ToTitle();
                else Object.FindAnyObjectByType<ResultScreen>(FindObjectsInactive.Include).ToTitle();
                float until = Time.realtimeSinceStartup + 20f;
                yield return null;
                while ((GameBootstrap.Instance == null || GameBootstrap.Instance.attract == null || !GameBootstrap.Instance.attract.Active) && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(GameBootstrap.Instance.attract.Active, (which == 0 ? "일시정지" : "결과") + " '타이틀로' → 시작화면");
                Assert.IsFalse(GameBootstrap.Instance.clock.IsRunning, "시작화면에서는 시작 안 함");
            }
        }
    }
}
