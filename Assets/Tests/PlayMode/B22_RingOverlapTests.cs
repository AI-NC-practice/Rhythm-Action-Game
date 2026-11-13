using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>화살표 겹침 없애기 (GDD-beta B10.1).</summary>
    public class B22_RingOverlapTests
    {
        double now;
        float real;
        GameObject root;
        BeatClock clock;
        JudgeSystem judge;
        JudgeRingView view;
        Camera cam;
        static readonly Vector2 Hd = new Vector2(1920, 1080);

        [SetUp]
        public void SetUp()
        {
            now = 0; real = 0;
            root = new GameObject("B22");
            clock = root.AddComponent<BeatClock>();
            clock.TimeSource = () => now;
            judge = root.AddComponent<JudgeSystem>();
            judge.clock = clock; judge.readKeyboard = false; judge.OffsetSource = () => 0f;
            var camGo = new GameObject("Cam", typeof(Camera)); camGo.transform.SetParent(root.transform);
            cam = camGo.GetComponent<Camera>();
            var canvas = UiFactory.CreateCanvas("JudgeCanvas", GameConfig.RingCanvasOrder);
            canvas.transform.SetParent(root.transform);
            view = canvas.gameObject.AddComponent<JudgeRingView>();
            view.RealTime = () => real;
            view.CanvasSize = () => Hd;
            view.displayLatencyMs = 0f;
            view.Bind(judge, clock, cam);
            clock.StartClock();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(root);
            if (view != null && view.labelLayer != null) Object.Destroy(view.labelLayer.gameObject);
        }

        void At(double beat) { now = beat * 0.5; judge.Tick(); real += 0.2f; view.Refresh(); }

        static float MinPairDistance(JudgeRingView v)
        {
            float min = float.MaxValue;
            var p = v.Rings.Select(r => r.Root.anchoredPosition).ToArray();
            for (int i = 0; i < p.Length; i++)
                for (int j = i + 1; j < p.Length; j++) min = Mathf.Min(min, Vector2.Distance(p[i], p[j]));
            return min;
        }

        [UnityTest]
        public IEnumerator B22_OneBeatCombo_SideBySide_NoOverlap()
        {
            var center = cam.transform.position + cam.transform.forward * 10f;
            // 무사 1박 콤보 모양: 1박 간격 3개, 같은 자리
            judge.Schedule(10, ArrowKey.Left, center);
            judge.Schedule(11, ArrowKey.Down, center);
            judge.Schedule(12, ArrowKey.Up, center);
            At(8.0); At(9.0); At(10.0);                         // 세 원이 모두 떠 있음
            Assert.AreEqual(3, view.Rings.Count);
            Assert.IsNotNull(view.Rings[0].Chain, "1박 간격도 묶음 (2박 미만)");
            for (int i = 1; i < 3; i++)
                Assert.AreEqual(160f, view.Rings[i].Root.anchoredPosition.x - view.Rings[i - 1].Root.anchoredPosition.x, 0.01f, "가로 160 px, 먼저 올 원이 왼쪽");
            Assert.GreaterOrEqual(MinPairDistance(view), 140f, "겹치지 않음");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B22_NonChainNewRing_ShiftsAwayFromVisibleRing()
        {
            var center = cam.transform.position + cam.transform.forward * 10f;
            judge.Schedule(10, ArrowKey.Left, center);
            judge.Schedule(12, ArrowKey.Right, center);         // 2박 간격: 묶음 아님, 하지만 10박 원이 아직 떠 있을 때 등장
            At(8.0);
            At(9.99);                                           // 12박 원은 10박에 등장 → 10박 원과 동시에
            At(10.0);
            Assert.AreEqual(2, view.Rings.Count);
            Assert.IsNull(view.Rings[1].Chain);
            var a = view.Rings[0].Root.anchoredPosition; var b = view.Rings[1].Root.anchoredPosition;
            Assert.GreaterOrEqual(Vector2.Distance(a, b), 140f, "떠 있는 원과 겹치지 않게 옮김");
            Assert.AreEqual(160f, Mathf.Abs(b.x - a.x), 0.01f, "가로 160 px");
            Assert.Greater(b.x, a.x, "오른쪽 먼저");
            Assert.LessOrEqual(Mathf.Abs(b.x), RingLayout.SafeRadii(Hd).x + 0.5f, "안전 타원 안");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B22_NextRingOpaque_WaitingDimmed()
        {
            var center = cam.transform.position + cam.transform.forward * 10f;
            judge.Schedule(10, ArrowKey.Left, center);
            judge.Schedule(11, ArrowKey.Down, center);
            At(8.0); At(9.0);
            var first = view.Rings.First(r => r.Cue.TargetBeat == 10);
            var second = view.Rings.First(r => r.Cue.TargetBeat == 11);
            Assert.AreEqual(1f, first.Group.alpha, 1e-4f, "다음에 누를 원 선명");
            Assert.AreEqual(0.55f, second.Group.alpha, 1e-4f, "기다리는 원 흐리게");
            judge.Submit(ArrowKey.Left, 5.0);                   // 10박 판정
            view.Refresh();
            Assert.AreEqual(1f, second.Group.alpha, 1e-4f, "이제 다음 원이 선명");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B22_GameScene_StageChart_NeverOverlap()
        {
            // 1박 간격이 많은 곳: 1챕터(술독·매복), 포위 1단계, 무사 콤보
            foreach (var start in new[] { 0.0, 181.0, 256.0 })
            {
                yield return SceneManager.LoadSceneAsync("Game");
                yield return null;
                var boot = GameBootstrap.Instance;
                double t = 0;
                boot.clock.TimeSource = () => t;
                boot.judge.readKeyboard = false;
                boot.judge.OffsetSource = () => 0f;
                boot.director.AllowEventSlowMotion = false;
                boot.StartRun(start);
                yield return null;
                float minDist = float.MaxValue;
                string where = "";
                int maxShown = 0;
                for (int f = 0; f < 1600; f++)                 // 16초
                {
                    t += 0.01;
                    foreach (var c in boot.judge.Cues.ToArray())
                        if (!c.Judged && c.Appeared && t >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, t); break; }
                    yield return null;
                    var rings = boot.ringView.Rings.Where(r => r.Locked).ToList();
                    maxShown = Mathf.Max(maxShown, rings.Count);
                    for (int i = 0; i < rings.Count; i++)
                        for (int j = i + 1; j < rings.Count; j++)
                        {
                            float d = Vector2.Distance(rings[i].Root.anchoredPosition, rings[j].Root.anchoredPosition);
                            if (d < minDist) { minDist = d; where = $"{rings[i].Cue.TargetBeat:0.##}{rings[i].Root.anchoredPosition} 묶음{rings[i].ChainIndex}/{rings[i].Chain?.Slots} ↔ {rings[j].Cue.TargetBeat:0.##}{rings[j].Root.anchoredPosition} 묶음{rings[j].ChainIndex}/{rings[j].Chain?.Slots}, 지금 {boot.clock.CurrentBeat:0.##}"; }
                        }
                    if (boot.director.State == StageState.Cleared) break;
                }
                Assert.GreaterOrEqual(maxShown, 2, $"{start}박부터: 원이 동시에 여러 개 뜨는 구간");
                Assert.GreaterOrEqual(minDist, 140f, $"{start}박부터: 동시에 떠 있는 원 중심 거리 140 px 이상 ({where})");
                boot.director.slowMotion.Cancel();
                Time.timeScale = 1f;
            }
        }
    }
}
