using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>화려한 판정 원·판정 이펙트 (GDD-beta B8.5). 시간을 주입해 구성과 움직임을 검사한다.</summary>
    public class B17_FlashyFxTests
    {
        const string AddShader = "RhythmActionGame/UIAdditive";
        double now;
        float real;
        GameObject root;
        BeatClock clock;
        JudgeSystem judge;
        JudgeRingView view;

        [SetUp]
        public void SetUp()
        {
            now = 0; real = 0;
            root = new GameObject("B17");
            clock = root.AddComponent<BeatClock>();
            clock.TimeSource = () => now;
            judge = root.AddComponent<JudgeSystem>();
            judge.clock = clock; judge.readKeyboard = false; judge.OffsetSource = () => 0f;
            var camGo = new GameObject("Cam", typeof(Camera)); camGo.transform.SetParent(root.transform);
            var canvas = UiFactory.CreateCanvas("JudgeCanvas", GameConfig.RingCanvasOrder);
            canvas.transform.SetParent(root.transform);
            view = canvas.gameObject.AddComponent<JudgeRingView>();
            view.RealTime = () => real;
            view.CanvasSize = () => new Vector2(1920, 1080);
            view.displayLatencyMs = 0f;
            view.Bind(judge, clock, camGo.GetComponent<Camera>());
            clock.StartClock();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(root);
            if (view != null && view.labelLayer != null) Object.Destroy(view.labelLayer.gameObject);
        }

        static bool IsAdditive(UnityEngine.UI.Image i) => i != null && i.material != null && i.material.shader != null && i.material.shader.name == AddShader;

        [UnityTest]
        public IEnumerator B17_ApproachRing_ChargesUp_OrbitDots_OuterGlow()
        {
            Assert.IsNotNull(Shader.Find(AddShader), "UI 가산 셰이더");
            judge.Schedule(4, ArrowKey.Up);                       // 목표 2.0초
            now = 1.0; judge.Tick(); view.Refresh();             // 2박 전 등장
            var r = view.Rings[0];
            Assert.IsTrue(IsAdditive(r.ChargeGlow), "가산 빛");
            Assert.IsTrue(IsAdditive(r.OuterGlow));
            Assert.AreEqual(GameConfig.RingChargeMin, r.Charge, 0.02f, "등장 때 0.4");
            Assert.AreEqual(0.4f, r.ChargeGlow.color.a, 0.02f);
            Assert.AreEqual(8, r.Orbit.Length, "빛 점 8개");
            Assert.IsTrue(r.Orbit.All(IsAdditive));
            var p0 = r.Orbit[0].rectTransform.anchoredPosition;
            Assert.AreEqual(GameConfig.RingOrbitRadiusPx, p0.magnitude, 0.5f, "원 둘레");
            Assert.AreEqual(360f * 1.15f, r.OuterGlow.rectTransform.sizeDelta.x, 1f, "바깥 원 빛 (바깥 원 360 px)");

            now = 1.5; real = 0.1f; view.Refresh();
            Assert.AreEqual(0.7f, r.Charge, 0.02f, "가까워질수록 밝아짐");
            Assert.AreNotEqual(p0, r.Orbit[0].rectTransform.anchoredPosition, "빛 점이 돈다");
            Assert.AreEqual(240f * 1.15f, r.OuterGlow.rectTransform.sizeDelta.x, 1f, "바깥 원 빛이 따라 줄어듦");
            now = 2.0; view.Refresh();
            Assert.AreEqual(1f, r.Charge, 0.02f, "목표 시점 1.0");
            yield return null;
        }

        JudgeRingView.RingFx Judge(double target, double delay, bool pressed = true)
        {
            judge.Schedule(target, ArrowKey.Up);
            now = clock.TimeOfBeat(target) - 0.5; judge.Tick(); view.Refresh();
            now = clock.TimeOfBeat(target) + delay;
            if (pressed) judge.Submit(ArrowKey.Up, now); else { now += 0.2; judge.Tick(); }
            return view.RingEffects.Last();
        }

        [UnityTest]
        public IEnumerator B17_Perfect_LightningFlareShockSparksCore()
        {
            real = 1f;
            var fx = Judge(4, 0.0);
            Assert.AreEqual(Judgement.Perfect, fx.Result);
            Assert.IsTrue(IsAdditive(fx.Lightning) && fx.Lightning.sprite == VfxFactory.Lightning, "방사형 번개선");
            Assert.IsTrue(IsAdditive(fx.Flare) && fx.Flare.sprite == VfxFactory.Flare, "십자·X 섬광");
            Assert.GreaterOrEqual(fx.Flare.rectTransform.sizeDelta.x, 700f, "섬광 지름 700 px 이상");
            Assert.IsTrue(IsAdditive(fx.Shock), "충격 고리");
            Assert.IsTrue(IsAdditive(fx.Core), "중심 섬광");
            Assert.AreEqual(JudgeRingView.PerfectFxColor, fx.Core.color.WithAlpha(JudgeRingView.PerfectFxColor.a), "금빛");
            Assert.GreaterOrEqual(fx.Sparks.Count, 16, "빛 입자 16개 이상");
            Assert.IsTrue(fx.Sparks.All(s => IsAdditive(s.Img)));

            real = 1.1f; view.Refresh();
            var d1 = fx.Sparks.Select(s => s.Img.rectTransform.anchoredPosition.magnitude).ToArray();
            real = 1.25f; view.Refresh();
            var d2 = fx.Sparks.Select(s => s.Img.rectTransform.anchoredPosition.magnitude).ToArray();
            Assert.IsTrue(d1.Zip(d2, (a, b) => b > a).All(x => x), "빛 입자가 바깥으로 흩어짐");
            Assert.Greater(d2.Average(), 100f);
            real = 1.29f; view.Refresh();
            Assert.Greater(fx.Shock.rectTransform.localScale.x, 2.7f, "충격 고리 3배로 퍼짐");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B17_Normal_And_Fail_Distinct()
        {
            real = 1f;
            var n = Judge(4, 0.12);
            Assert.AreEqual(Judgement.Normal, n.Result);
            Assert.IsNotNull(n.Lightning, "작은 번개선");
            Assert.Less(n.Lightning.rectTransform.sizeDelta.x, GameConfig.FxLightningDiameterPx, "PERFECT보다 작음");
            Assert.AreEqual(GameConfig.FxSparksNormal, n.Sparks.Count, "빛 입자 8개");
            Assert.IsNull(n.Flare, "십자 섬광 없음");
            Assert.IsNotNull(n.Main, "청록 고리");

            real = 10f;
            var f = Judge(8, 0.0, pressed: false);
            Assert.AreEqual(Judgement.Fail, f.Result);
            Assert.IsNotNull(f.Crack, "붉게 금이 간 원");
            Assert.AreEqual(JudgeRingView.FailFxColor.r, f.Crack.color.r, 1e-4f);
            Assert.IsNotNull(f.Smoke, "어두운 연기");
            Assert.Less(f.Smoke.color.r, 0.3f);
            Assert.AreEqual(0, f.Sparks.Count, "빛 입자 없음");
            Assert.IsNull(f.Lightning);
            yield return null;
        }

        [UnityTest]
        public IEnumerator B17_BelowRings_GoneWithin035s()
        {
            real = 1f;
            var fx = Judge(4, 0.0);
            Assert.AreEqual(view.labelLayer, fx.Root.parent, "Canvas 10 층");
            Assert.Less(view.labelLayer.GetComponent<Canvas>().sortingOrder, view.GetComponent<Canvas>().sortingOrder, "다음 판정 원 아래");
            real = 1.351f; view.Refresh();
            Assert.AreEqual(0, view.RingEffects.Count, "0.35초 안에 사라짐");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B17_GameScene_AdditiveMaterialAsset()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var m = GameInstance().ringView.additiveMaterial;
            Assert.IsNotNull(m, "SceneBuilder가 머티리얼 에셋 연결 (빌드에 셰이더 포함)");
            Assert.AreEqual(AddShader, m.shader.name);
        }

        static GameBootstrap GameInstance() => GameBootstrap.Instance;
    }

    static class ColorExt
    {
        public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }
    }
}
