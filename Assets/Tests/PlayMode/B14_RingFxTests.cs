using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RhythmActionGame.Tests
{
    /// <summary>판정 원 이펙트 (GDD-beta B8.3). 결과별로 다르고, 원 아래 층에서 0.35초 안에 사라진다.</summary>
    public class B14_RingFxTests
    {
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
            root = new GameObject("B14");
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

        JudgeRingView.RingFx JudgeOne(double target, double delay, bool pressed = true)
        {
            judge.Schedule(target, ArrowKey.Up);
            now = clock.TimeOfBeat(target) - 0.5; judge.Tick(); view.Refresh();
            now = clock.TimeOfBeat(target) + delay;
            if (pressed) judge.Submit(ArrowKey.Up, now); else judge.Tick();
            return view.RingEffects.Count > 0 ? view.RingEffects[view.RingEffects.Count - 1] : null;
        }

        [UnityTest]
        public IEnumerator B14_EachResult_DistinctEffect()
        {
            real = 1f;
            var p = JudgeOne(4, 0.0);
            Assert.AreEqual(Judgement.Perfect, p.Result);
            Assert.AreEqual(JudgeRingView.PerfectFxColor.r, p.Main.color.r, 1e-4f, "PERFECT 금빛");
            Assert.AreEqual(2, p.Parts.Length, "빛 + 붓획 조각");
            Assert.AreEqual(VfxFactory.BrushBurst, p.Parts[1].sprite, "붓획");
            real = 1f + 0.29f; view.Refresh();
            Assert.Greater(p.Main.rectTransform.localScale.x, 1.7f, "원이 터지며 1.8배로 커짐");

            real = 10f;
            var n = JudgeOne(8, 0.1);
            Assert.AreEqual(Judgement.Normal, n.Result);
            Assert.AreEqual(JudgeRingView.NormalFxColor.g, n.Main.color.g, 1e-4f, "NORMAL 청록");
            Assert.AreEqual(0, n.Parts.Length, "조각 없음 (작게)");
            real = 10f + 0.24f; view.Refresh();
            Assert.AreEqual(1.3f, n.Main.rectTransform.localScale.x, 0.02f, "작은 고리 1.3배");

            real = 20f;
            var f = JudgeOne(12, 0.0, pressed: false);        // 무입력 FAIL
            now = clock.TimeOfBeat(12) + 0.26; judge.Tick();   // beta1-1: NORMAL 창 0.24초(GDD-beta B12.1) 뒤 FAIL
            f = view.RingEffects.Last();
            Assert.AreEqual(Judgement.Fail, f.Result);
            Assert.AreEqual(JudgeRingView.FailFxColor.r, f.Parts[0].color.r, 1e-4f, "FAIL 붉은색");
            Assert.AreEqual(2, f.Parts.Length, "두 쪽으로 갈라짐");
            float maxShake = 0;
            for (float t = 0.01f; t < 0.3f; t += 0.01f)
            {
                real = 20f + t; view.Refresh();
                maxShake = Mathf.Max(maxShake, Mathf.Abs(f.Root.anchoredPosition.x - f.BasePos.x));
            }
            Assert.Greater(maxShake, 2f, "흔들림");
            Assert.Less(f.Parts[0].rectTransform.anchoredPosition.x, -5f, "왼쪽 조각이 벌어짐");
            Assert.Greater(f.Parts[1].rectTransform.anchoredPosition.x, 5f, "오른쪽 조각이 벌어짐");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B14_BelowRings_GoneWithin035s()
        {
            real = 1f;
            var p = JudgeOne(4, 0.0);
            Assert.AreEqual(view.labelLayer, p.Root.parent, "판정 문구·이펙트 층");
            Assert.AreEqual(GameConfig.JudgeFxCanvasOrder, view.labelLayer.GetComponent<Canvas>().sortingOrder);
            Assert.Less(view.labelLayer.GetComponent<Canvas>().sortingOrder, view.GetComponent<Canvas>().sortingOrder, "다음 판정 원보다 아래");
            foreach (var secs in new[] { GameConfig.RingFxPerfectSeconds, GameConfig.RingFxNormalSeconds, GameConfig.RingFxFailSeconds })
                Assert.LessOrEqual(secs, 0.35f + 1e-6f);
            real = 1f + 0.351f; view.Refresh();
            Assert.AreEqual(0, view.RingEffects.Count, "0.35초 안에 사라짐");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B14_GameScene_EffectsSpawnOnJudgement()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            double t = 0;
            boot.clock.TimeSource = () => t;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            boot.director.UseChart(new ChartData { name = "t", bpm = 120, runBeats = 264, events = new[] { new ChartEvent { runBeat = 8, type = "obstacle", kind = "cart", key = "Up" } } });
            boot.StartRun(0);
            yield return null;
            int seen = -1;
            Judgement got = Judgement.Fail;
            boot.judge.CueJudged += (c, r) => { got = r; seen = boot.ringView.RingEffects.Count(x => x.Result == r); };
            while (boot.player.RunBeat < 8.2)
            {
                t += 0.01;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && t >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, t); break; }
                yield return null;
            }
            Assert.AreEqual(Judgement.Perfect, got);
            Assert.GreaterOrEqual(seen, 1, "실제 게임 판정에서 판정 원 이펙트가 생김");
        }
    }
}
