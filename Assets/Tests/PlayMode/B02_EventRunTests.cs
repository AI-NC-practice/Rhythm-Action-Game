using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>사건 진행 (GDD-beta B2, B4). Game 씬에 시험용 차트를 넣고 시간을 주입해 검사한다.</summary>
    public class B02_EventRunTests
    {
        double now;
        GameBootstrap boot;
        StageDirector dir;

        [TearDown]
        public void TearDown()
        {
            if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel();
            Time.timeScale = 1f;
        }

        static ChartData Chart(params ChartEvent[] events) =>
            new ChartData { name = "test", bpm = 120, runBeats = 264, events = events };

        IEnumerator Load(ChartData chart, double startRunBeat = 0)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.UseChart(chart);
            boot.StartRun(startRunBeat);
            yield return null;
        }

        /// <summary>시간을 step초 간격으로 t까지 진행. 판정 목표 시각(press 목록)에 맞춰 키를 누른다.</summary>
        IEnumerator AdvanceTo(double t, double step = 0.01, System.Func<double, bool> pressAt = null)
        {
            while (now < t - 1e-9)
            {
                now = System.Math.Min(t, now + step);
                if (pressAt != null) pressAt(now);
                yield return null;
            }
        }

        /// <summary>예약된 판정의 목표 시각에 정답 키를 누르는 입력기. delay초 늦게 누른다.</summary>
        System.Func<double, bool> Presser(double delay)
        {
            return t =>
            {
                foreach (var c in boot.judge.Cues.ToArray())
                {
                    if (c.Judged || !c.Appeared) continue;
                    if (t >= boot.clock.TimeOfBeat(c.TargetBeat) + delay - 1e-6) { boot.judge.Submit(c.Key, t); return true; }
                }
                return false;
            };
        }

        [UnityTest]
        public IEnumerator B02_TileSlip_TwoJudges_Fixed6Beats_AnyResult()
        {
            foreach (var delay in new double?[] { 0.0, 0.1, null })
            {
                yield return Load(Chart(new ChartEvent { runBeat = 8, type = "obstacle", kind = "tile_slip" }));
                dir.AllowEventSlowMotion = false;
                yield return AdvanceTo(3.0);                      // 6박: 사건 시작 전
                var run = dir.PendingRuns.Count > 0 ? dir.PendingRuns[0] : dir.Current;
                Assert.IsNotNull(run);
                Assert.AreEqual(2, run.Cues.Count, "판정 2개");
                Assert.AreEqual(10.0, run.Cues[0].TargetBeat, 1e-6, "↓ @2");
                Assert.AreEqual(12.0, run.Cues[1].TargetBeat, 1e-6, "↑ @4");
                Assert.AreEqual(ArrowKey.Down, run.Cues[0].Key);
                Assert.AreEqual(ArrowKey.Up, run.Cues[1].Key);

                double startClock = -1, endClock = -1;
                var press = delay.HasValue ? Presser(delay.Value) : null;
                while (now < 12.0)
                {
                    now += 0.01;
                    press?.Invoke(now);
                    yield return null;
                    if (startClock < 0 && dir.Current == run) startClock = boot.clock.CurrentBeat;
                    if (startClock >= 0 && dir.Current != run) { endClock = boot.clock.CurrentBeat; break; }
                }
                Assert.AreEqual(6.0, endClock - startClock, 0.1, $"결과와 무관하게 6박 (delay {delay})");
                Assert.AreEqual(14.0, dir.LastRun.EndBeat, 1e-9);
                Assert.AreEqual(14.0, boot.player.RunBeat, 0.1, "사건 끝 위치 같음");
                var expect = delay == 0.0 ? Judgement.Perfect : delay == 0.1 ? Judgement.Normal : Judgement.Fail;
                Assert.AreEqual(expect, run.Results[0]);
                Assert.AreEqual(expect, run.Results[1]);
                Assert.AreEqual(expect == Judgement.Fail ? 1 : 3, dir.Health.Hp, "FAIL마다 체력 −1 (2번)");
                Assert.AreEqual($"tile_slip_{expect}", StageDirector.RunStateName(run));
            }
        }

        [UnityTest]
        public IEnumerator B02_Fail_EachCostsSpeed_ButCappedHpForJarsAndArrows()
        {
            yield return Load(Chart(
                new ChartEvent { runBeat = 8, type = "obstacle", kind = "rolling_jars", keys = new[] { "Left", "Right", "Left" } },
                new ChartEvent { runBeat = 12, type = "obstacle", kind = "arrow_volley", keys = new[] { "Left", "Right", "Left", "Right" } }));
            for (int i = 0; i < 6; i++) boot.clock.ApplyJudgement(Judgement.Perfect);   // 1.30
            boot.clock.ApplyPendingSpeedNow();   // GDD-beta B15.2: 속도는 다음 마디 첫 박에 들어오므로 바로 적용
            while (boot.clock.CurrentBeat < 11.5) { now += 0.01; yield return null; }
            Assert.AreEqual(2, dir.Health.Hp, "술독 FAIL 3번 → 체력 −1만");
            Assert.AreEqual(1.0f, boot.clock.TargetSpeed, 1e-4f, "속도 ×0.8은 판정마다 (1.30→1.04→1.00)");
            while (boot.clock.CurrentBeat < 15.5) { now += 0.01; yield return null; }
            Assert.AreEqual(1, dir.Health.Hp, "화살 FAIL 4번 → 체력 −1만");
            Assert.AreEqual(StageState.Running, dir.State);
        }

        [UnityTest]
        public IEnumerator B02_ChargeAmbushChase_DoNotStop()
        {
            yield return Load(Chart(
                new ChartEvent { runBeat = 8, type = "enemy", kind = "sword", approach = "charge" },
                new ChartEvent { runBeat = 12, type = "enemy", kind = "sword", approach = "ambush", keys = new[] { "Left", "Right" } },
                new ChartEvent { runBeat = 16, type = "enemy", kind = "spear", approach = "chase", key = "Left" }));
            dir.AllowEventSlowMotion = false;
            bool everCombat = false;
            dir.StateChanged += s => { if (s == StageState.Combat) everCombat = true; };
            var press = Presser(0.0);
            double lastRun = boot.player.RunBeat, lastClock = boot.clock.CurrentBeat;
            int judged = 0;
            dir.EventSlotJudged += (r, i, j) => judged++;
            while (boot.clock.CurrentBeat < 22.0)
            {
                now += 0.01;
                press(now);
                yield return null;
                double dRun = boot.player.RunBeat - lastRun, dClock = boot.clock.CurrentBeat - lastClock;
                Assert.AreEqual(dClock, dRun, 1e-6, "달리기 박자가 시계와 같이 감 (멈추지 않음)");
                lastRun = boot.player.RunBeat; lastClock = boot.clock.CurrentBeat;
            }
            Assert.IsFalse(everCombat, "멈추는 전투로 바뀌지 않음");
            Assert.AreEqual(4, judged, "돌진 1 + 매복 2 + 추격 1");
            Assert.AreEqual(3, dir.Health.Hp);
            Assert.AreEqual(StageState.Running, dir.State);
        }

        [UnityTest]
        public IEnumerator B02_ChargeFail_HpMinusOne_ThenPasses()
        {
            yield return Load(Chart(new ChartEvent { runBeat = 8, type = "enemy", kind = "sword", approach = "charge" }));
            yield return AdvanceTo(8.0);                          // 16박까지 무입력
            Assert.AreEqual(2, dir.Health.Hp);
            Assert.AreEqual(StageState.Running, dir.State);
            Assert.AreEqual(1, dir.LastRun.Cues.Count, "판정 1개, 다시 예약 안 함");
            Assert.AreEqual(0, boot.judge.Cues.Count(c => !c.Judged && !c.Cancelled));
            Assert.AreEqual(16.0, boot.player.RunBeat, 0.1);
        }

        [UnityTest]
        public IEnumerator B02_RunningSlowMotion_RemapsVisual_ExactAtEventEnd()
        {
            yield return Load(Chart(new ChartEvent { runBeat = 8, type = "obstacle", kind = "rope_swing" }));
            int requests = 0;
            dir.EventSlowMotionRequested += _ => requests++;
            var press = Presser(0.0);
            double target = boot.clock.TimeOfBeat(10);
            yield return AdvanceTo(target + 0.005, 0.005, t => press(t));
            Assert.AreEqual(1, requests, "PERFECT → 슬로모션 요청");
            Assert.IsTrue(dir.slowMotion.IsActive);
            Assert.AreEqual(GameConfig.SlowMotionTimeScale, Time.timeScale, 1e-4f);
            Assert.IsTrue(boot.player.Remap.HasValue, "이동 시간 재배치");

            double clockBefore = boot.clock.CurrentBeat;
            double tBefore = now;
            yield return AdvanceTo(now + 0.2, 0.01);
            Assert.AreEqual(clockBefore + (now - tBefore) * 2 * boot.clock.SpeedMultiplier, boot.clock.CurrentBeat, 1e-6, "박자 시계 그대로");
            Assert.Less(boot.player.VisualRunBeat, boot.player.RunBeat - 0.05, "보이는 위치가 늦어짐");
            Assert.Less(Vector3.Distance(MapLayout.PointAt(boot.player.VisualDistance), boot.player.transform.position), 0.01f, "화면 위치 = 보이는 위치");

            // 사건 끝(12박) 직전·직후
            float runBeforeEnd = 0, visBeforeEnd = 0;
            while (boot.player.RunBeat < 12.0)
            {
                runBeforeEnd = (float)boot.player.RunBeat;
                visBeforeEnd = (float)boot.player.VisualRunBeat;
                now += 0.005;
                yield return null;
            }
            Assert.LessOrEqual(Mathf.Abs(runBeforeEnd - visBeforeEnd) * GameConfig.MetersPerBeat, 0.05f, "사건 끝에서 위치 오차 0.05 m 이하");
            Assert.AreEqual(boot.player.RunBeat, boot.player.VisualRunBeat, 1e-9, "끝난 뒤 일치");
            var expected = MapLayout.PointAt(boot.player.Distance);
            Assert.Less(Vector3.Distance(expected, boot.player.transform.position), 0.05f);
        }
    }
}
