using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>남는 화살표 버그 (B11): 취소·지움·끝난 판정의 원이 화면에 남지 않아야 한다.</summary>
    public class B11_StuckRingTests
    {
        double now;
        GameBootstrap boot;
        StageDirector dir;
        JudgeRingView view;

        IEnumerator Load(double startRunBeat, ChartData chart = null)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            view = boot.ringView;
            now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            if (chart != null) boot.director.UseChart(chart);
            if (boot.bulletTime != null) boot.bulletTime.enabled = false;   // 일정한 빠르기로 입력 시각을 미리 계산하는 테스트 (회피 슬로모션은 B47 테스트)
            boot.StartRun(startRunBeat);
            yield return null;
        }

        [TearDown]
        public void TearDown() { if (dir != null) dir.slowMotion.Cancel(); Time.timeScale = 1f; }

        IEnumerator AdvanceTo(double t, double step = 0.01)
        {
            while (now < t - 1e-9) { now = System.Math.Min(t, now + step); yield return null; }
        }

        IEnumerator UntilCombat()
        {
            for (int i = 0; i < 3000 && dir.State != StageState.Combat; i++) { now += 0.02; yield return null; }
            Assert.AreEqual(StageState.Combat, dir.State);
        }

        IEnumerator Hit(JudgeCue cue, double? delay)
        {
            yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) - 0.03);
            if (delay.HasValue) { now = boot.clock.TimeOfBeat(cue.TargetBeat) + delay.Value; boot.judge.Submit(cue.Key, now); yield return null; }
            else yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) + 0.26);   // beta1-1: NORMAL 창 0.24초(GDD-beta B12.1)가 지나야 무입력 FAIL
        }

        void AssertNoDeadRings(string why)
        {
            foreach (var r in view.Rings)
            {
                Assert.IsFalse(r.Cue.Judged, $"{why}: 끝난 판정의 원이 남음 ({r.Cue.Key} @{r.Cue.TargetBeat})");
                Assert.IsTrue(boot.judge.Cues.Contains(r.Cue), $"{why}: 판정 목록에 없는 원이 남음");
            }
            var shown = view.GetComponentsInChildren<RectTransform>().Count(t => t.name == "Ring");
            Assert.AreEqual(view.Rings.Count, shown, why + ": 화면의 원 수 = 목록의 원 수");
        }

        [UnityTest]
        public IEnumerator B11_DuelFailRetry_CancelledRingsRemoved()
        {
            // beta1-1: GDD-beta B9 — 일반 적은 다시 하지 않으므로, 판정을 지우는 경로인 무사 되감기로 검사한다
            yield return Load(256);
            float rt = 50f;
            dir.RewindRealTime = () => rt;
            dir.FailHitSeconds = 0f;   // beta1-1: 피격 연출(GDD-beta B12.2)은 B30 테스트에서 따로 본다. 여기서는 바로 되감기
            yield return UntilCombat();
            var first = dir.CurrentEncounter.Cues.ToList();
            yield return AdvanceTo(boot.clock.TimeOfBeat(first[0].TargetBeat) + 0.1);    // 첫 공격 판정 창 안, 둘째 원은 이미 등장
            Assert.IsTrue(view.Rings.Any(r => r.Cue == first[1]), "둘째 원이 떠 있음");
            yield return AdvanceTo(boot.clock.TimeOfBeat(first[0].TargetBeat) + 0.26);    // 첫 공격 무입력 FAIL → 되감기   // beta1-1: NORMAL 창 0.24초(GDD-beta B12.1)가 지나야 무입력 FAIL
            yield return null;
            Assert.IsTrue(dir.Rewinding);
            Assert.IsFalse(view.Rings.Any(r => first.Contains(r.Cue)), "지운 판정의 원이 사라짐");
            AssertNoDeadRings("무사 되감기");
            rt += GameConfig.RewindSeconds + 0.01f;
            yield return null;
            var again = dir.CurrentEncounter.Cues.Where(c => !c.Judged).ToList();
            yield return AdvanceTo(boot.clock.TimeOfBeat(again[0].TargetBeat - 1.0));
            Assert.IsTrue(view.Rings.Any(r => r.Cue == again[0]), "되감은 뒤 원 등장");
            AssertNoDeadRings("되감은 뒤");
        }

        [UnityTest]
        public IEnumerator B11_WindmillFailRetry_CancelledRingsRemoved()
        {
            yield return Load(181);
            yield return UntilCombat();
            var cues = dir.CurrentEncounter.Cues.ToList();
            foreach (var c in cues.Where(c => ((CombatStep)c.Tag).Phase < 3)) yield return Hit(c, 0.0);
            var p3 = cues.Where(c => ((CombatStep)c.Tag).Phase == 3).ToList();
            yield return AdvanceTo(boot.clock.TimeOfBeat(p3[0].TargetBeat - 0.1));
            Assert.GreaterOrEqual(view.Rings.Count(r => p3.Contains(r.Cue)), 3, "윈드밀 원들이 떠 있음");
            yield return Hit(p3[0], null);                      // 첫 화살표 FAIL → 3단계 다시
            yield return null;
            Assert.IsFalse(view.Rings.Any(r => p3.Contains(r.Cue)), "취소된 윈드밀 원이 사라짐");
            AssertNoDeadRings("윈드밀 재도전");
        }

        [UnityTest]
        public IEnumerator B11_GameOverAndRestart_ClearRings()
        {
            // beta1-1: 차트가 바뀌어도 상관없게 시험 차트를 쓴다 (8박 술독: 9·10·11박 판정)
            yield return Load(0, new ChartData { name = "t", bpm = 120, runBeats = 264, events = new[] {
                new ChartEvent { runBeat = 8, type = "obstacle", kind = "rolling_jars", keys = new[] { "Left", "Right", "Left" } } } });
            yield return AdvanceTo(boot.clock.TimeOfBeat(8.5));     // 술독 원들이 떠 있음
            Assert.Greater(view.Rings.Count, 0, "원이 떠 있음");
            dir.AllowDeathRewind = false;   // beta1-1: 게임 오버 경로 (챕터 되감기는 B19 테스트)
            dir.Health.Damage(); dir.Health.Damage(); dir.Health.Damage();
            yield return null;
            Assert.AreEqual(StageState.GameOver, dir.State);
            Assert.AreEqual(0, view.Rings.Count, "게임 오버: 남는 원 없음");
            AssertNoDeadRings("게임 오버");

            now = 0;
            boot.StartRun(0);                                   // 다시 하기·챕터 이동과 같은 경로
            yield return AdvanceTo(boot.clock.TimeOfBeat(8.5));
            var old = view.Rings.Select(r => r.Cue).ToList();
            Assert.Greater(old.Count, 0);
            boot.StartRun(0);
            yield return null;
            Assert.IsFalse(view.Rings.Any(r => old.Contains(r.Cue)), "다시 시작: 이전 판정의 원이 남지 않음");
            AssertNoDeadRings("다시 시작");
        }

        [UnityTest]
        public IEnumerator B11_AnyJudgedCue_RingGoneNextFrame()
        {
            yield return Load(0);
            var cue = boot.judge.Schedule(6, ArrowKey.Up);
            yield return AdvanceTo(boot.clock.TimeOfBeat(4.5));
            Assert.IsTrue(view.Rings.Any(r => r.Cue == cue));
            cue.Judged = true;                                  // 이벤트 없이 끝난 경우 (안전망)
            yield return null;
            Assert.IsFalse(view.Rings.Any(r => r.Cue == cue), "다음 프레임에 사라짐");
            AssertNoDeadRings("안전망");
        }
    }
}
