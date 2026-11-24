using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R37 (사용자 피드백 2026-11-24 2번): 시작화면이 어두워졌다 처음으로 돌아가지 않고 계속 달린다.
    /// 되풀이(TilePeriod만큼 뒤로 옮김) 직전·직후 자리에서 같은 프레임에 두 번 그려 화면이 같은지 픽셀로 비교한다.
    /// 대조: 반 칸만 옮긴 화면은 달라야 한다 (비교가 차이를 잡아낸다는 증거). 시작을 누르면 안개가 걷히고 1챕터는 평소 안개·배경.
    /// 미리보기 → harness/reports/R37/.
    /// </summary>
    public class R37_AttractLoopPlayTests
    {
        const string Out = "harness/reports/R37";
        const int W = 480, H = 270;

        [TearDown]
        public void TearDown() { AttractMode.AttractOnLoad = false; Time.timeScale = 1f; }

        static Color32[] Grab(Camera cam, string path = null)
        {
            var rt = new RenderTexture(W, H, 24);
            var old = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = old;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var t = new Texture2D(W, H, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            RenderTexture.active = prev;
            if (path != null) File.WriteAllBytes(path, t.EncodeToPNG());
            var px = t.GetPixels32();
            Object.Destroy(t);
            rt.Release();
            return px;
        }

        /// <summary>평균 차이 (0~1)와 크게(0.1 넘게) 다른 픽셀 비율.</summary>
        static (float mean, float bigFrac) Diff(Color32[] a, Color32[] b)
        {
            double sum = 0; int big = 0;
            for (int i = 0; i < a.Length; i++)
            {
                int d = Mathf.Max(Mathf.Abs(a[i].r - b[i].r), Mathf.Max(Mathf.Abs(a[i].g - b[i].g), Mathf.Abs(a[i].b - b[i].b)));
                sum += d / 255.0;
                if (d > 25) big++;
            }
            return ((float)(sum / a.Length), big / (float)a.Length);
        }

        IEnumerator LoadAttract()
        {
            AttractMode.AttractOnLoad = true;
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator R37_Wrap_SameFrameBeforeAndAfter_NoFade()
        {
            Directory.CreateDirectory(Out);
            yield return LoadAttract();
            var boot = GameBootstrap.Instance;
            var attract = boot.attract;
            Assert.IsTrue(attract.Active, "시작화면");
            Assert.AreEqual(1f, attract.Mist, 1e-4f, "시작화면은 새벽 안개");
            Assert.AreEqual(GameConfig.Attract.MistFogEnd, RenderSettings.fogEndDistance, 0.01f, "안개 끝 거리");
            Assert.IsNotNull(attract.backdrop, "먼 배경 그림 연결");
            Assert.IsFalse(attract.backdrop.isPartOfStaticBatch, "먼 배경 그림은 옮길 수 있게 정적 배칭 아님");
            Assert.IsNull(GameObject.Find("AttractFade"), "어두워지는 덮개(R36)가 없음");
            attract.DeltaTime = () => 0f;   // 멈춰 두고 자리만 바꿔 그린다
            yield return null;
            var cam = attract.AttractCamera;
            float T = GameConfig.Attract.TilePeriod, near = -GameConfig.Attract.LoopNear;
            float worstMean = 0f, worstBig = 0f;
            foreach (float a in new[] { near - 0.01f, near - 4f, near - 9f, near - 14f })
            {
                attract.SetApproachDistanceForTest(a);
                var before = Grab(cam, a > near - 0.1f ? $"{Out}/wrap_before.png" : null);
                attract.SetApproachDistanceForTest(a - T);
                var after = Grab(cam, a > near - 0.1f ? $"{Out}/wrap_after.png" : null);
                var (m, b) = Diff(before, after);
                Debug.Log($"[R37] 되풀이 전후 ({a:0.0} m ↔ {a - T:0.0} m): 평균 차이 {m:0.0000}, 크게 다른 픽셀 {b:0.0000}");
                worstMean = Mathf.Max(worstMean, m); worstBig = Mathf.Max(worstBig, b);
            }
            // 대조: 반 칸(집 한 채)만 옮기면 화면이 달라야 한다
            attract.SetApproachDistanceForTest(near - 4f);
            var c0 = Grab(cam);
            attract.SetApproachDistanceForTest(near - 4f - T * 0.5f);
            var c1 = Grab(cam, $"{Out}/control_half_shift.png");
            var (cm, cb) = Diff(c0, c1);
            Debug.Log($"[R37] 대조 (반 칸 옮김): 평균 차이 {cm:0.0000}, 크게 다른 픽셀 {cb:0.0000}");
            Assert.Greater(cb, 0.02f, "대조: 반 칸 옮긴 화면은 달라야 함 (비교가 차이를 잡음)");
            Assert.Less(worstMean, 0.004f, $"되풀이 전후 화면이 같아야 함 (평균 차이 {worstMean:0.0000})");
            Assert.Less(worstBig, 0.002f, $"되풀이 전후 크게 다른 픽셀 {worstBig:0.0000}");

            // 실제 달리기: 되풀이가 일어나고 거리는 되풀이 구간 안
            attract.SetApproachDistanceForTest(-GameConfig.Attract.ApproachLength);
            attract.DeltaTime = () => 1f / 60f;
            int wraps0 = attract.Wraps;
            for (int f = 0; f < 600; f++)
            {
                yield return null;
                Assert.LessOrEqual(attract.ApproachDistance, near + 0.01f);
                Assert.GreaterOrEqual(attract.ApproachDistance, near - T - 0.01f);
                Assert.AreEqual(1f, attract.Mist, 1e-4f, "메뉴 동안 안개 그대로");
            }
            Assert.Greater(attract.Wraps - wraps0, 1, "10초 동안 되풀이가 여러 번 (어두워지지 않고)");
        }

        [UnityTest]
        public IEnumerator R37_Start_MistLifts_Chapter1HasNormalFogAndBackdrop()
        {
            Directory.CreateDirectory(Out);
            yield return LoadAttract();
            var boot = GameBootstrap.Instance;
            var attract = boot.attract;
            attract.DeltaTime = () => 1f / 60f;
            var bd = attract.backdrop.transform;
            for (int f = 0; f < 200; f++) yield return null;   // 되풀이 구간 어딘가에서
            attract.menu.GetComponent<TitleMenu>().StartButton.onClick.Invoke();
            Assert.IsTrue(attract.Leading);
            float lastMist = attract.Mist; bool mid = false;
            for (int f = 0; f < 600 && attract.Active; f++)
            {
                yield return null;
                if (!attract.Active) break;
                Assert.LessOrEqual(attract.Mist, lastMist + 1e-4f, "안개는 걷히기만 함");
                lastMist = attract.Mist;
                if (!mid && attract.Mist < 0.5f) { mid = true; Grab(attract.AttractCamera, $"{Out}/lead_mist_lifting.png"); }
            }
            Assert.IsFalse(attract.Active, "시작선에서 1챕터");
            Assert.IsTrue(boot.clock.IsRunning);
            Assert.AreEqual(GameConfig.Map.FogStart, RenderSettings.fogStartDistance, 0.01f, "1챕터 안개 시작 = 평소");
            Assert.AreEqual(GameConfig.Map.FogEnd, RenderSettings.fogEndDistance, 0.01f, "1챕터 안개 끝 = 평소");
            var mpb = new MaterialPropertyBlock();
            attract.backdrop.GetPropertyBlock(mpb);
            Assert.AreEqual(0f, mpb.GetFloat("_Mist"), 1e-4f, "먼 배경 그림 안개 없음");
            Assert.Less((bd.position - attract.BackdropHome).magnitude, 0.001f, "먼 배경 그림 제자리");
            Assert.AreEqual(-GameConfig.Map.BackdropSink, bd.position.y, 0.01f, "먼 배경 그림 높이");
            Debug.Log($"[R37] 1챕터 시작: 안개 {RenderSettings.fogStartDistance}~{RenderSettings.fogEndDistance} m, 배경 원통 {bd.position}");
            for (int f = 0; f < 20; f++) yield return null;
            Grab(boot.playerCamera, $"{Out}/start_fp.png");
        }
    }
}
