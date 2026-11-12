using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>새 사건 8종과 적 등장 방식 (GDD-beta B2.2, B2.3, B4). Game 씬 + 시험용 차트 + 시간 주입.</summary>
    public class B03_NewEventsTests
    {
        double now;
        GameBootstrap boot;
        StageDirector dir;
        EventPropDirector props;

        [TearDown]
        public void TearDown()
        {
            if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel();
            Time.timeScale = 1f;
        }

        static ChartData Chart(params ChartEvent[] events) =>
            new ChartData { name = "test", bpm = 120, runBeats = 264, events = events };

        IEnumerator Load(ChartData chart)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            props = Object.FindAnyObjectByType<EventPropDirector>();
            now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.UseChart(chart);
            boot.StartRun(0);
            yield return null;
        }

        /// <summary>목표 시각 + delay에 정답 키를 누른다 (delay null이면 무입력).</summary>
        void Press(double? delay)
        {
            if (!delay.HasValue) return;
            foreach (var c in boot.judge.Cues.ToArray())
            {
                if (c.Judged || !c.Appeared) continue;
                if (now >= boot.clock.TimeOfBeat(c.TargetBeat) + delay.Value - 1e-6) { boot.judge.Submit(c.Key, now); return; }
            }
        }

        static readonly (string kind, string[] keys)[] Kinds =
        {
            ("tile_slip", null), ("rope_swing", null), ("rolling_jars", new[] { "Left", "Right", "Left" }), ("awning_bounce", null),
            ("closing_gate", null), ("arrow_volley", new[] { "Left", "Right", "Left", "Right" }), ("long_jump", null), ("banner_slide", null),
        };

        [UnityTest]
        public IEnumerator B03_AllNewEvents_BeatsAndTargetsPerTable_PerfectRun()
        {
            // 8종을 6박 간격으로 이어 놓고 전부 PERFECT
            var events = new List<ChartEvent>();
            int b = 8;
            foreach (var (kind, keys) in Kinds)
            {
                events.Add(new ChartEvent { runBeat = b, type = "obstacle", kind = kind, keys = keys, key = "Up" });
                b += EventCatalog.Get(kind).Beats + 2;
            }
            yield return Load(Chart(events.ToArray()));
            dir.AllowEventSlowMotion = false;
            var seen = new Dictionary<int, StageDirector.ObstacleRun>();
            var states = new Dictionary<string, string>();
            var durations = new Dictionary<string, double>();
            var starts = new Dictionary<int, double>();
            var maxMotion = new Dictionary<string, float>();
            while (boot.player.RunBeat < b + 1)
            {
                now += 0.01;
                Press(0.0);
                yield return null;
                foreach (var r in dir.PendingRuns) seen[r.Event.runBeat] = r;
                var cur = dir.Current;
                if (cur != null)
                {
                    seen[cur.Event.runBeat] = cur;
                    if (!starts.ContainsKey(cur.Event.runBeat)) starts[cur.Event.runBeat] = boot.clock.CurrentBeat;
                    if (cur.HasResult) states[cur.Def.Kind] = dir.TraversalState;
                    float m = dir.motion.localPosition.magnitude + dir.motion.localEulerAngles.magnitude * 0.0001f;
                    maxMotion[cur.Def.Kind] = Mathf.Max(maxMotion.TryGetValue(cur.Def.Kind, out var x) ? x : 0f, dir.motion.localPosition.magnitude);
                }
                var last = dir.LastRun;
                if (last != null && starts.ContainsKey(last.Event.runBeat) && !durations.ContainsKey(last.Def.Kind))
                    durations[last.Def.Kind] = boot.clock.CurrentBeat - starts[last.Event.runBeat];
            }
            foreach (var e in events)
            {
                var def = EventCatalog.Get(e.kind);
                Assert.IsTrue(seen.ContainsKey(e.runBeat), e.kind);
                var run = seen[e.runBeat];
                Assert.AreEqual(def.Slots.Length, run.Cues.Count, e.kind + " 판정 수");
                for (int i = 0; i < def.Slots.Length; i++)
                    Assert.AreEqual(e.runBeat + def.Slots[i].Offset, run.Cues[i].TargetBeat, 0.02, $"{e.kind} 판정 {i} 목표 박자");
                Assert.IsTrue(run.Results.All(r => r == Judgement.Perfect), e.kind + " 전부 PERFECT");
                Assert.AreEqual(def.Beats, durations[e.kind], 0.1, e.kind + " 고정 길이");
                Assert.AreEqual(e.kind + "_Perfect", states[e.kind]);
                Assert.Greater(maxMotion[e.kind], 0.05f, e.kind + " 카메라 동작");
            }
            Assert.AreEqual(3, dir.Health.Hp);
        }

        IEnumerator RunOne(string kind, double? delay, System.Action<string, Vector3> report, string approach = null)
        {
            yield return Load(Chart(new ChartEvent { runBeat = 8, type = "obstacle", kind = kind, key = "Up", approach = approach }));
            dir.AllowEventSlowMotion = false;
            var def = EventCatalog.Get(kind);
            string state = null;
            Vector3 sample = Vector3.zero;
            double sampleAt = 8 + def.Slots[def.Slots.Length - 1].Offset + 0.5 * (def.Beats - def.Slots[def.Slots.Length - 1].Offset);
            while (boot.player.RunBeat < 8 + def.Beats + 0.5)
            {
                now += 0.01;
                Press(delay);
                yield return null;
                if (dir.Current != null && dir.Current.HasResult) state = dir.TraversalState;
                if (sample == Vector3.zero && boot.player.VisualRunBeat >= sampleAt) sample = dir.motion.localPosition + dir.motion.localEulerAngles * 0.001f;
            }
            report(state, sample);
        }

        [UnityTest]
        public IEnumerator B03_RopeSwing_ResultsGiveDifferentCameraStates()
        {
            string sP = null, sN = null, sF = null;
            Vector3 mP = default, mN = default, mF = default;
            yield return RunOne("rope_swing", 0.0, (s, m) => { sP = s; mP = m; });
            yield return RunOne("rope_swing", 0.1, (s, m) => { sN = s; mN = m; });
            yield return RunOne("rope_swing", null, (s, m) => { sF = s; mF = m; });
            Assert.AreEqual("rope_swing_Perfect", sP);
            Assert.AreEqual("rope_swing_Normal", sN);
            Assert.AreEqual("rope_swing_Fail", sF);
            Assert.Greater(Vector3.Distance(mP, mN), 0.1f);
            Assert.Greater(Vector3.Distance(mP, mF), 0.1f);
            Assert.Greater(Vector3.Distance(mN, mF), 0.1f);
        }

        [UnityTest]
        public IEnumerator B03_Props_SpawnedAndAnimated()
        {
            yield return Load(Chart(
                new ChartEvent { runBeat = 6, type = "foreshadow", span = 6 },
                new ChartEvent { runBeat = 8, type = "obstacle", kind = "rolling_jars", keys = new[] { "Left", "Right", "Left" } },
                new ChartEvent { runBeat = 14, type = "obstacle", kind = "closing_gate", key = "Down" },
                new ChartEvent { runBeat = 20, type = "obstacle", kind = "rope_swing", key = "Up", approach = "charge" }));
            dir.AllowEventSlowMotion = false;
            Assert.AreEqual(4, props.Props.Count, "시험 차트의 소품을 시작할 때 만듦");
            var fs = props.Props.First(p => p.kind == "foreshadow");
            var jars = props.Props.First(p => p.kind == "rolling_jars");
            var gate = props.Props.First(p => p.kind == "closing_gate");
            var rope = props.Props.First(p => p.kind == "rope_swing");
            Assert.IsNotNull(rope.enemy, "조합: 돌진 포졸");

            Vector3 jar0Start = jars.parts[0].localPosition;
            float doorGapStart = gate.parts[1].localPosition.x - gate.parts[0].localPosition.x;
            bool fsShown = false, fsHiddenAfter = false;
            while (boot.player.RunBeat < 26)
            {
                now += 0.01;
                Press(0.0);
                yield return null;
                double v = boot.player.VisualRunBeat;
                if (v > 7 && v < 11) fsShown |= fs.gameObject.activeSelf;
                if (v > 13) fsHiddenAfter = !fs.gameObject.activeSelf;
                if (v > 9.9 && v < 10.1) Assert.Less(jars.parts[0].localPosition.z, jar0Start.z - 1f, "독이 굴러옴");
            }
            Assert.IsTrue(fsShown, "복선이 보임");
            Assert.IsTrue(fsHiddenAfter, "복선 기간이 지나면 숨김");
            float doorGapEnd = gate.parts[1].localPosition.x - gate.parts[0].localPosition.x;
            Assert.Less(doorGapEnd, doorGapStart - 2f, "문이 닫힘");
            Assert.IsTrue(rope.EnemyDown, "밧줄 넘기 PERFECT → 포졸 쓰러짐");
            Assert.AreEqual("Enemy_Down", rope.State);
        }

        [UnityTest]
        public IEnumerator B03_EnemyApproaches_GrayBox()
        {
            yield return Load(Chart(
                new ChartEvent { runBeat = 8, type = "enemy", kind = "sword", approach = "charge" },
                new ChartEvent { runBeat = 14, type = "enemy", kind = "sword", approach = "ambush", keys = new[] { "Right" } },
                new ChartEvent { runBeat = 20, type = "enemy", kind = "spear", approach = "chase", key = "Left" }));
            dir.AllowEventSlowMotion = false;
            var charge = props.Props.First(p => p.kind == "charge");
            var ambush = props.Props.First(p => p.kind == "ambush");
            var chase = props.Props.First(p => p.kind == "chase");
            float chargeZ0 = charge.enemy.localPosition.z;
            float ambushX0 = Mathf.Abs(ambush.enemy.localPosition.x);
            bool chaseAlongside = false;
            while (boot.player.RunBeat < 25)
            {
                now += 0.01;
                Press(0.0);
                yield return null;
                double v = boot.player.VisualRunBeat;
                if (v > 21 && v < 22)
                {
                    // 추격: 옆 지붕(경로 옆 3.5 m)에서 플레이어보다 조금 앞을 나란히 달림
                    var local = chase.transform.InverseTransformPoint(boot.player.transform.position);
                    chaseAlongside = Mathf.Abs(chase.enemy.localPosition.x) > 3f && chase.enemy.localPosition.z > local.z;
                }
            }
            Assert.Less(charge.enemy.localPosition.z, chargeZ0 - 3f, "돌진: 앞에서 달려옴");
            Assert.IsTrue(charge.EnemyDown, "돌진 PERFECT → 쓰러짐");
            Assert.Less(Mathf.Abs(ambush.enemy.localPosition.x), ambushX0 - 1f, "매복: 옆에서 튀어나옴");
            Assert.IsTrue(chaseAlongside, "추격: 나란히 달림");
            Assert.AreEqual(StageState.Running, dir.State, "멈추지 않음");
        }

        [UnityTest]
        public IEnumerator B03_SlowMotionRequests_OnlyCandidatesOnPerfect()
        {
            var events = new List<ChartEvent>();
            int b = 8;
            foreach (var (kind, keys) in Kinds)
            {
                events.Add(new ChartEvent { runBeat = b, type = "obstacle", kind = kind, keys = keys, key = "Up" });
                b += EventCatalog.Get(kind).Beats + 2;
            }
            events.Add(new ChartEvent { runBeat = b, type = "enemy", kind = "sword", approach = "charge" }); b += 6;
            events.Add(new ChartEvent { runBeat = b, type = "enemy", kind = "sword", approach = "ambush", keys = new[] { "Left" } }); b += 6;
            events.Add(new ChartEvent { runBeat = b, type = "enemy", kind = "sword", approach = "chase", key = "Right" }); b += 6;
            yield return Load(Chart(events.ToArray()));
            dir.AllowEventSlowMotion = false;      // 요청만 센다 (빈도 제한은 B05)
            var requested = new List<string>();
            dir.EventSlowMotionRequested += r => requested.Add(r.Def.Kind);
            while (boot.player.RunBeat < b)
            {
                now += 0.01;
                Press(0.0);
                yield return null;
            }
            CollectionAssert.AreEquivalent(new[] { "tile_slip", "rope_swing", "closing_gate", "long_jump", "charge" }, requested,
                "GDD-beta B4 2·3번 후보만, PERFECT일 때");

            // NORMAL이면 요청 없음
            yield return Load(Chart(new ChartEvent { runBeat = 8, type = "obstacle", kind = "rope_swing", key = "Up" }));
            dir.AllowEventSlowMotion = false;
            int n = 0;
            dir.EventSlowMotionRequested += _ => n++;
            while (boot.player.RunBeat < 13) { now += 0.01; Press(0.1); yield return null; }
            Assert.AreEqual(0, n, "NORMAL은 슬로모션 없음");
        }
    }
}
