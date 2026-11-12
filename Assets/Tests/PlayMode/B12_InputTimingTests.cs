using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>입력 이벤트 시각 판정과 화면 지연 보정 (GDD-beta B8.1).</summary>
    public class B12_InputTimingTests
    {
        double now;
        GameObject root;
        BeatClock clock;
        JudgeSystem judge;
        readonly List<Judgement> results = new List<Judgement>();

        [SetUp]
        public void SetUp()
        {
            now = 0; results.Clear();
            root = new GameObject("B12");
            clock = root.AddComponent<BeatClock>();
            clock.TimeSource = () => now;
            clock.RealtimeToClock = rt => rt - 1000.0;      // 실제 시간 1000초 = 시계 0초
            judge = root.AddComponent<JudgeSystem>();
            judge.clock = clock;
            judge.readKeyboard = false;
            judge.OffsetSource = () => 0f;
            judge.CueJudged += (c, r) => results.Add(r);
            clock.StartClock();
        }

        [TearDown]
        public void TearDown() => Object.Destroy(root);

        [UnityTest]
        public IEnumerator B12_EventTime_UsedInsteadOfFrameTime()
        {
            judge.Schedule(4, ArrowKey.Up);                    // 목표 2.0초
            now = 1.5; judge.Tick();
            // 프레임은 목표 +120 ms (프레임 시각이면 NORMAL, GDD-beta B8.4 ±90), 실제로 누른 시각은 +20 ms
            now = 2.12;
            judge.SubmitRealtime(ArrowKey.Up, 1000.0 + 2.02);
            CollectionAssert.AreEqual(new[] { Judgement.Perfect }, results, "누른 시각으로 판정 → PERFECT");

            judge.Schedule(8, ArrowKey.Down);                 // 목표 4.0초 (속도 1.05: 2.0 + 4/2.1)
            double target = clock.TimeOfBeat(8);
            now = target - 0.5; judge.Tick();
            now = target + 0.03;
            judge.SubmitRealtime(ArrowKey.Down, 1000.0 + target + 0.5);    // 미래 시각 → 지금(+30 ms)으로 자름
            Assert.AreEqual(Judgement.Perfect, results[1]);

            judge.Schedule(12, ArrowKey.Left);
            target = clock.TimeOfBeat(12);
            now = target - 0.5; judge.Tick();
            now = target + 0.14;
            judge.SubmitRealtime(ArrowKey.Left, 1000.0 + target - 5.0);    // 너무 오래된 시각 → 지금 −0.1초(+40 ms)로 자름
            Assert.AreEqual(Judgement.Perfect, results[2], "오래된 이벤트 시각은 0.1초까지만 믿음");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B12_Ring_DrawnAheadByDisplayLatency()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            var view = boot.ringView;
            Assert.AreEqual(GameConfig.DisplayLatencyMs, view.displayLatencyMs, "씬 기본값 = GameConfig");
            double t = 0;
            boot.clock.TimeSource = () => t;
            boot.judge.readKeyboard = false;
            boot.director.UseChart(new ChartData { name = "t", bpm = 120, runBeats = 264, events = new ChartEvent[0] });
            boot.StartRun(0);
            var cue = boot.judge.Schedule(4, ArrowKey.Up);    // 목표 2.0초
            t = 1.5; boot.judge.Tick(); view.Refresh();
            var ring = view.Rings[0];
            t = 2.0 - GameConfig.DisplayLatencyMs / 1000.0; view.Refresh();
            Assert.AreEqual(GameConfig.RingTargetDiameterPx, ring.Outer.rectTransform.sizeDelta.x, 0.5f, "보정값만큼 일찍 두 원이 겹쳐 보임");
            // 판정 창은 그대로: 목표 시각 ±60 ms가 PERFECT
            var got = new List<Judgement>();
            boot.judge.CueJudged += (c, r) => got.Add(r);
            t = 2.055; boot.judge.Submit(ArrowKey.Up, 2.055);
            CollectionAssert.AreEqual(new[] { Judgement.Perfect }, got);
        }
    }
}
