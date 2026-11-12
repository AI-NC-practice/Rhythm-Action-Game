using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>화면 효과와 이펙트 (GDD-beta B5, 제안서 2장 B·D·E·F, 4장). 시간을 주입해 검사한다.</summary>
    public class B06_ScreenAndVfxTests
    {
        double now;
        GameObject root;
        BeatClock clock;
        JudgeSystem judge;
        JudgeFx jfx;
        ScreenFx screen;
        VfxPool vfx;
        CameraFx cam;
        PresentationDirector pd;

        [SetUp]
        public void SetUp()
        {
            now = 0;
            root = new GameObject("B06");
            clock = root.AddComponent<BeatClock>();
            clock.TimeSource = () => now;
            judge = root.AddComponent<JudgeSystem>();
            judge.clock = clock; judge.readKeyboard = false; judge.OffsetSource = () => 0f;
            var camGo = new GameObject("Cam", typeof(Camera));
            camGo.transform.SetParent(root.transform);
            jfx = root.AddComponent<JudgeFx>();
            jfx.judge = judge; jfx.cam = camGo.GetComponent<Camera>(); jfx.DeltaTime = () => 0f;
            jfx.EnsureSetup();
            var sgo = new GameObject("ScreenFx"); sgo.transform.SetParent(root.transform);
            screen = sgo.AddComponent<ScreenFx>();
            screen.DeltaTime = () => 0f;
            screen.judgeFx = jfx;
            screen.judge = judge;
            screen.enabled = false; screen.enabled = true;     // 구독 다시 (judge 연결 뒤)
            var vgo = new GameObject("Vfx"); vgo.transform.SetParent(root.transform);
            vfx = vgo.AddComponent<VfxPool>();
            vfx.DeltaTime = () => 0f;
            vfx.worldCamera = camGo.GetComponent<Camera>();
            cam = root.AddComponent<CameraFx>();
            cam.DeltaTime = () => 0f;
            pd = root.AddComponent<PresentationDirector>();
            pd.cameraFx = cam; pd.screenFx = screen; pd.vfxPool = vfx;
            pd.Enabled = true;
            clock.StartClock();
        }

        [TearDown]
        public void TearDown() => Object.Destroy(root);

        [UnityTest]
        public IEnumerator B06_ScreenFx_VolumeOverrides_MaxRule()
        {
            var p = screen.Profile;
            Assert.IsTrue(p.Has<Vignette>() && p.Has<ChromaticAberration>() && p.Has<LensDistortion>() && p.Has<Bloom>() && p.Has<ColorAdjustments>());
            Assert.Greater(screen.volume.priority, jfx.volume.priority, "합친 값을 위 Volume에서 최종 적용");

            // 판정 화면 최대 (채도 −60, 노출 −0.4, 비네트 0.3)
            judge.Schedule(4, ArrowKey.Up);
            now = 1.0; judge.Tick();
            jfx.Step(0.15f);
            screen.Step(0f);
            Assert.AreEqual(-60f, screen.Saturation, 1e-3f);
            Assert.AreEqual(-0.4f, screen.Exposure, 1e-4f);
            Assert.AreEqual(0.3f, screen.VignetteIntensity, 1e-4f);

            // 펄스: 더하지 않고 항목별 최댓값 (채도는 최솟값)
            screen.Add(new ScreenPulse(ScreenChannel.Vignette, 0.2f, 1f));            // 0.3보다 작음 → 0.3
            screen.Add(new ScreenPulse(ScreenChannel.Saturation, -30f, 1f));          // −60이 더 낮음 → −60
            screen.Step(0f);
            Assert.AreEqual(0.3f, screen.VignetteIntensity, 1e-4f, "더하지 않음");
            Assert.AreEqual(-60f, screen.Saturation, 1e-3f);
            screen.Add(new ScreenPulse(ScreenChannel.Vignette, 0.45f, 1f));
            screen.Add(new ScreenPulse(ScreenChannel.Saturation, -80f, 1f));
            screen.Add(new ScreenPulse(ScreenChannel.Exposure, 0.6f, 1f));
            screen.Add(new ScreenPulse(ScreenChannel.Chromatic, 0.2f, 1f));
            screen.Add(new ScreenPulse(ScreenChannel.Chromatic, 0.4f, 1f));
            screen.Add(new ScreenPulse(ScreenChannel.LensDistortion, -0.2f, 1f));
            screen.Add(new ScreenPulse(ScreenChannel.LensDistortion, 0.1f, 1f));
            screen.Step(0f);
            Assert.AreEqual(0.45f, screen.VignetteIntensity, 1e-4f, "최댓값");
            Assert.AreEqual(-80f, screen.Saturation, 1e-3f, "채도는 최솟값");
            Assert.AreEqual(0.6f, screen.Exposure, 1e-4f);
            Assert.AreEqual(0.4f, screen.Chromatic, 1e-4f, "색수차 최댓값 (0.2+0.4가 아님)");
            Assert.AreEqual(-0.2f, screen.Lens, 1e-4f, "렌즈 왜곡은 절댓값이 큰 쪽");
            var vig = screen.Profile.TryGet<Vignette>(out var v) ? v : null;
            Assert.AreEqual(0.45f, vig.intensity.value, 1e-4f, "Volume에 실제로 적용");
            Assert.GreaterOrEqual(screen.BloomIntensity, GameConfig.Fx.BloomBase);
            yield return null;
        }

        [UnityTest]
        public IEnumerator B06_FailHit_RedVignetteShake_HalvedOnNextRing()
        {
            pd.Play(GameConfig.Fx.Hit, 1f);
            Assert.AreEqual(0.6f, cam.Trauma, 1e-4f, "흔들림 0.6");
            screen.Step(0f);
            Assert.AreEqual(0.45f, screen.VignetteIntensity, 1e-4f, "주홍 비네트 0.45");
            Assert.AreEqual(GameConfig.Fx.Vermilion, screen.VignetteColor);
            Assert.AreEqual(-80f, screen.Saturation, 1e-3f, "채도 −80 순간");
            Assert.AreEqual(-0.2f, screen.Lens, 1e-4f, "렌즈 왜곡 펄스");
            screen.Step(0.2f);
            Assert.AreEqual(0.225f, screen.VignetteIntensity, 1e-3f, "0.4초 동안 0으로 (절반 지점)");
            screen.Step(0.21f);
            Assert.AreEqual(0f, screen.VignetteIntensity, 1e-4f, "0.4초 뒤 0");

            // 다음 원이 뜨면 절반
            pd.Play(GameConfig.Fx.Hit, 1f);
            screen.Step(0.1f);
            float before = screen.VignetteIntensity;
            judge.Schedule(8, ArrowKey.Up);
            now = 3.0; judge.Tick();                       // 6박: 원 등장
            screen.Step(0f);
            Assert.AreEqual(before * 0.5f, screen.VignetteIntensity, 1e-4f, "다음 원이 뜨면 절반");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B06_VfxPool_AllKinds_Pooled_Reused()
        {
            foreach (VfxKind k in System.Enum.GetValues(typeof(VfxKind)))
                Assert.IsTrue(vfx.Play(k, root.transform.position + Vector3.forward * 5f, 5), k.ToString());
            foreach (var k in new[] { VfxKind.Embers, VfxKind.Dust, VfxKind.TileDebris, VfxKind.HanjiDebris, VfxKind.Shockwave })
                Assert.AreEqual(GameConfig.Fx.PoolSize, vfx.PooledCount(k), k + " 미리 만든 개수");

            // 같은 종류를 풀 크기만큼 돌리면 처음 것을 다시 쓴다 (새로 만들지 않음)
            vfx.Play(VfxKind.Embers);
            var first = vfx.LastParticles;
            for (int i = 0; i < GameConfig.Fx.PoolSize; i++) vfx.Play(VfxKind.Embers);
            Assert.AreSame(first, vfx.LastParticles, "재사용");
            Assert.AreEqual(GameConfig.Fx.PoolSize, vfx.PooledCount(VfxKind.Embers));
            vfx.Play(VfxKind.Embers, null, 3, GameConfig.Fx.EmbersPerfect);
            Assert.AreEqual(GameConfig.Fx.EmbersPerfect, vfx.LastParticles.particleCount, "불씨 30개");

            // 충격파: 0.4초에 반지름 6 m까지
            vfx.Play(VfxKind.Shockwave, Vector3.zero);
            var ring = vfx.LastShockwave;
            vfx.Step(0.2f);
            Assert.AreEqual(3f, VfxPool.ShockRadius(ring), 0.01f, "0.2초에 3 m");
            vfx.Step(0.19f);
            Assert.AreEqual(5.85f, VfxPool.ShockRadius(ring), 0.05f);
            vfx.Step(0.02f);
            Assert.IsFalse(ring.gameObject.activeSelf, "0.4초 뒤 풀로 돌아감");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B06_Flash_OneAtATime_HigherGradeWins_AlphaMax06()
        {
            Assert.IsTrue(vfx.Play(VfxKind.InkX, null, 5));
            Assert.AreEqual(VfxKind.InkX, vfx.ActiveFlash);
            Assert.IsFalse(vfx.Play(VfxKind.BrushBurst, null, 3), "등급 낮으면 거절");
            Assert.AreEqual(VfxKind.InkX, vfx.ActiveFlash, "동시에 하나");
            Assert.LessOrEqual(vfx.FlashImage.color.a, 0.6f + 1e-5f, "불투명도 ≤ 0.6");
            Assert.AreEqual(0.6f, vfx.FlashImage.color.a, 1e-4f);
            vfx.Step(0.13f);
            Assert.IsNull(vfx.ActiveFlash, "0.12초 뒤 끝");
            Assert.IsTrue(vfx.Play(VfxKind.BrushBurst, null, 3));
            Assert.IsTrue(vfx.Play(VfxKind.InkSlash, null, 4), "등급 높으면 바꿈");
            Assert.AreEqual(VfxKind.InkSlash, vfx.ActiveFlash);
            Assert.IsTrue(vfx.Play(VfxKind.WhiteFlash, null, 4), "같은 등급은 나중 것");
            Assert.AreEqual(VfxKind.WhiteFlash, vfx.ActiveFlash);
            for (float t = 0; t < 0.1f; t += 0.01f) { vfx.Step(0.01f); Assert.LessOrEqual(vfx.FlashImage.color.a, 0.6f + 1e-5f); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator B06_Presets_PlayScreenAndVfx()
        {
            pd.Play(GameConfig.Fx.ParryPerfect, 1f, null, root.transform.position + Vector3.forward * 3f);
            screen.Step(0f);
            Assert.AreEqual(VfxKind.BrushBurst, vfx.ActiveFlash, "PERFECT 패링: 붓획 방사 섬광");
            Assert.AreEqual(GameConfig.Fx.EmbersPerfect, vfx.LastParticles.particleCount, "불씨 30");
            Assert.AreEqual(0.6f, screen.Exposure, 1e-4f, "노출 +0.6");
            Assert.AreEqual(0.4f, screen.Chromatic, 1e-4f, "색수차 0.4");
            screen.Step(0.21f); vfx.Step(0.21f);
            pd.Play(GameConfig.Fx.WindmillFinish, 1f, null, root.transform.position);
            Assert.AreEqual(VfxKind.InkX, vfx.ActiveFlash, "윈드밀: 먹 X");
            Assert.IsTrue(vfx.LastShockwave.gameObject.activeSelf, "충격파");
            pd.Play(GameConfig.Fx.Execution, 1f);
            Assert.AreEqual(VfxKind.InkX, vfx.ActiveFlash, "등급 5 섬광이 등급 4 먹 획보다 우선");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B06_GameScene_RingCanvasAboveEffects()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            var ringCanvas = boot.ringView.GetComponent<Canvas>();
            var pres = boot.presentation;
            Assert.IsNotNull(pres.screenFx, "SceneBuilder가 화면 효과 연결");
            Assert.IsNotNull(pres.vfxPool, "SceneBuilder가 이펙트 풀 연결");
            var flashCanvas = pres.vfxPool.flashLayer.GetComponent<Canvas>();
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, ringCanvas.renderMode, "원은 Overlay라 후처리를 받지 않음");
            Assert.AreEqual(20, ringCanvas.sortingOrder);
            Assert.Less(flashCanvas.sortingOrder, ringCanvas.sortingOrder, "섬광은 원 아래");
            Assert.IsNotNull(pres.vfxPool.particleMaterial, "파티클 머티리얼 에셋 (빌드에 셰이더 포함)");
            pres.Play(GameConfig.Fx.WindmillFinish);
            Assert.AreEqual(VfxKind.InkX, pres.vfxPool.ActiveFlash);
            Assert.Less(flashCanvas.sortingOrder, ringCanvas.sortingOrder);
            Time.timeScale = 1f;
            boot.director.slowMotion.Cancel();
        }
    }
}
