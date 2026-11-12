using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>입력 반응 표시 (GDD-beta B8.2). 박자·실제 시간을 주입해 검사한다.</summary>
    public class B13_InputFeedbackTests
    {
        double now;
        float real;
        GameObject root;
        BeatClock clock;
        JudgeSystem judge;
        JudgeRingView view;
        readonly List<(ArrowKey, InputOutcome)> inputs = new List<(ArrowKey, InputOutcome)>();
        readonly List<Judgement> results = new List<Judgement>();
        static readonly Vector2 Hd = new Vector2(1920, 1080);

        [SetUp]
        public void SetUp()
        {
            now = 0; real = 0; inputs.Clear(); results.Clear();
            root = new GameObject("B13");
            clock = root.AddComponent<BeatClock>();
            clock.TimeSource = () => now;
            judge = root.AddComponent<JudgeSystem>();
            judge.clock = clock; judge.readKeyboard = false; judge.OffsetSource = () => 0f;
            var camGo = new GameObject("Cam", typeof(Camera)); camGo.transform.SetParent(root.transform);
            var canvas = UiFactory.CreateCanvas("JudgeCanvas", GameConfig.RingCanvasOrder);
            canvas.transform.SetParent(root.transform);
            view = canvas.gameObject.AddComponent<JudgeRingView>();
            view.RealTime = () => real;
            view.CanvasSize = () => Hd;
            view.displayLatencyMs = 0f;
            view.Bind(judge, clock, camGo.GetComponent<Camera>());
            judge.InputReceived += (k, c, o) => inputs.Add((k, o));
            judge.CueJudged += (c, r) => results.Add(r);
            clock.StartClock();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(root);
            if (view != null && view.labelLayer != null) Object.Destroy(view.labelLayer.gameObject);
        }

        [UnityTest]
        public IEnumerator B13_EarlyPress_PulseAndTooEarlyLabel_NoJudgement()
        {
            judge.Schedule(4, ArrowKey.Up);                       // 목표 2.0초
            now = 1.2; judge.Tick(); view.Refresh();              // 원 등장
            var ring = view.Rings[0];
            Assert.AreEqual(1f, ring.Root.localScale.x, 1e-4f);
            real = 5f;
            judge.Submit(ArrowKey.Up, 1.5);                       // 목표 −500 ms: 무시
            Assert.AreEqual(InputOutcome.TooEarly, inputs[0].Item2);
            Assert.AreEqual(0, results.Count, "판정 없음 (점수 변화 없음)");
            Assert.AreEqual(1, view.Rings.Count, "원은 그대로");
            real = 5.06f; view.Refresh();
            Assert.AreEqual(GameConfig.InputPulseScale, ring.Root.localScale.x, 1e-3f, "눌림 펄스 (가장 이른 원)");
            Assert.AreEqual(1, view.EarlyLabels.Count, "\"빠름\" 표시");
            var early = view.EarlyLabels[0].Text;
            Assert.AreEqual(UiText.TooEarly, early.text);
            Assert.AreEqual("빠름", early.text);
            Assert.AreEqual(view.labelLayer, early.transform.parent, "원보다 아래 층");
            Assert.AreEqual(ring.Root.anchoredPosition.y - 110f, view.EarlyLabels[0].BasePos.y, 0.01f, "원 아래");
            real = 5.13f; view.Refresh();
            Assert.AreEqual(1f, ring.Root.localScale.x, 1e-4f, "0.12초 뒤 원래 크기");
            real = 5.41f; view.Refresh();
            Assert.AreEqual(0, view.EarlyLabels.Count, "0.4초 뒤 사라짐");

            // 그다음 제때 누르면 정상 판정
            judge.Submit(ArrowKey.Up, 2.01);
            Assert.AreEqual(InputOutcome.Judged, inputs[1].Item2);
            CollectionAssert.AreEqual(new[] { Judgement.Perfect }, results);
            yield return null;
        }

        [UnityTest]
        public IEnumerator B13_PressWithoutRing_KeyEchoBottomCenter()
        {
            real = 1f;
            judge.Submit(ArrowKey.Left, 0.1);                     // 원이 하나도 없음
            Assert.AreEqual(InputOutcome.NoCue, inputs[0].Item2);
            Assert.AreEqual(1, view.KeyEchoes.Count);
            var echo = view.KeyEchoes[0];
            Assert.AreEqual("←", echo.Text.text, "누른 키");
            Assert.AreEqual(GameConfig.KeyEchoFontSize, echo.Text.fontSize);
            Assert.AreEqual(0f, echo.BasePos.x, 1e-4f, "가운데");
            Assert.AreEqual(-540f + GameConfig.KeyEchoBottomPx, echo.BasePos.y, 0.01f, "화면 아래");

            // 예약됐지만 아직 안 뜬 원만 있을 때도 "원이 없음"
            judge.Schedule(20, ArrowKey.Up);
            judge.Submit(ArrowKey.Down, 0.2);
            Assert.AreEqual(InputOutcome.NoCue, inputs[1].Item2);
            Assert.AreEqual(2, view.KeyEchoes.Count);
            real = 1.31f; view.Refresh();
            Assert.AreEqual(0, view.KeyEchoes.Count, "0.3초 뒤 사라짐");
            Assert.AreEqual(0, results.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator B13_EveryPress_ReportsOutcome()
        {
            judge.Schedule(4, ArrowKey.Right);
            now = 1.5; judge.Tick(); view.Refresh();
            judge.Submit(ArrowKey.Right, 1.0);                    // −1000 ms → 너무 이름
            judge.Submit(ArrowKey.Left, 1.9);                     // −100 ms 다른 키 → 판정(FAIL)
            judge.Submit(ArrowKey.Up, 2.5);                       // 원 없음
            Assert.AreEqual(3, inputs.Count, "누를 때마다 알림");
            Assert.AreEqual(InputOutcome.TooEarly, inputs[0].Item2);
            Assert.AreEqual(InputOutcome.Judged, inputs[1].Item2);
            Assert.AreEqual(InputOutcome.NoCue, inputs[2].Item2);
            Assert.AreEqual(1.15f, JudgeRingView.PulseScale(0.06f), 1e-3f);
            Assert.AreEqual(1f, JudgeRingView.PulseScale(0f), 1e-4f);
            Assert.AreEqual(1f, JudgeRingView.PulseScale(0.12f), 1e-4f);
            yield return null;
        }
    }
}
