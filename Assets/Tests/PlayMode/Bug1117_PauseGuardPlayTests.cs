using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 2026-11-17 버그 수정: 일시정지(Esc)가 먹으면 안 되는 때.
    /// ① 피격 연출·되감기 중 — 실제 시간으로 흘러 박자 시계를 다시 켜므로, 일시정지 화면 뒤에서 게임이 진행됐다 → 연출이 끝난 뒤 일시정지.
    /// ② 게임 씬 안 시작화면(R36) 중 — 패널만 뜨고 달리기는 계속, 시작선에서 패널이 뜬 채 1챕터가 시작됐다 → 무시.
    /// </summary>
    public class Bug1117_PauseGuardPlayTests
    {
        double now;
        float real;
        GameBootstrap boot;
        StageDirector dir;

        [TearDown]
        public void TearDown()
        {
            if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel();
            AttractMode.AttractOnLoad = false;
            Time.timeScale = 1f;
        }

        IEnumerator AdvanceTo(double t, double step = 0.01)
        {
            while (now < t - 1e-9) { now = System.Math.Min(t, now + step); yield return null; }
        }

        /// <summary>판정 c를 무입력 FAIL로 만든다.</summary>
        IEnumerator Miss(JudgeCue c)
        {
            yield return AdvanceTo(boot.clock.TimeOfBeat(c.TargetBeat) + 0.26);
        }

        [UnityTest]
        public IEnumerator Bug1117_PauseDuringHitAndRewind_WaitsThenPauses()
        {
            AttractMode.AttractOnLoad = false;
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            now = 0; real = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.RewindRealTime = () => real;
            dir.FailHitSeconds = GameConfig.FailHitSeconds;   // 피격 연출 0.7초 → 되감기 0.8초 (실제 게임과 같게)
            dir.slowMotion.RealTime = () => real;
            boot.StartRun(256);   // 무사 결투
            yield return null;
            for (int i = 0; i < 3000 && dir.State != StageState.Combat; i++) { now += 0.02; yield return null; }
            Assert.AreEqual(StageState.Combat, dir.State, "무사 결투 시작");
            var pause = boot.pause;

            // ① 피격 연출 중 Esc → 바로 멈추지 않고 기다림
            yield return Miss(dir.CurrentEncounter.Cues.First(c => !c.Judged));
            Assert.IsTrue(dir.HitPending, "FAIL → 피격 연출");
            pause.Toggle();
            Assert.IsFalse(pause.IsPaused, "피격 연출 중에는 멈추지 않음");
            Assert.IsTrue(pause.PauseQueued, "연출 뒤에 멈추도록 기다림");
            Assert.IsFalse(pause.Panel.gameObject.activeSelf, "패널 안 뜸");
            Assert.AreEqual(1f, Time.timeScale, "화면 시간 그대로");

            real += GameConfig.FailHitSeconds + 0.01f;
            yield return null;
            Assert.IsTrue(dir.Rewinding, "피격 연출 → 되감기");
            Assert.IsFalse(pause.IsPaused, "되감기 중에도 멈추지 않음");
            Assert.IsTrue(pause.PauseQueued);

            real += GameConfig.RewindSeconds + 0.01f;
            yield return null;
            yield return null;
            Assert.IsFalse(dir.Rewinding, "되감기 끝");
            Assert.IsTrue(pause.IsPaused, "되감기가 끝나자 일시정지");
            Assert.IsFalse(pause.PauseQueued);
            Assert.IsTrue(pause.Panel.gameObject.activeSelf, "패널");
            Assert.IsTrue(boot.clock.IsPaused, "박자 시계 멈춤");
            Assert.AreEqual(0f, Time.timeScale);
            double b = boot.clock.CurrentBeat;
            now += 2.0; real += 2.0f;
            yield return null;
            Assert.AreEqual(b, boot.clock.CurrentBeat, 1e-9, "일시정지 중 박자 멈춤 (뒤에서 진행 안 됨)");
            Assert.AreEqual(0, dir.CurrentEncounter.Cues.Count(c => c.Judged && !c.Cancelled && c.Result == Judgement.Fail),
                "일시정지 동안 FAIL이 쌓이지 않음");
            pause.Resume();
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(boot.clock.IsPaused);

            // ② 기다리는 중에 한 번 더 누르면 취소
            yield return Miss(dir.CurrentEncounter.Cues.First(c => !c.Judged));
            Assert.IsTrue(dir.HitPending, "다시 FAIL → 피격 연출");
            pause.Toggle();
            pause.Toggle();
            Assert.IsFalse(pause.PauseQueued, "두 번 누르면 취소");
            real += GameConfig.FailHitSeconds + GameConfig.RewindSeconds + 0.05f;
            yield return null;
            real += GameConfig.RewindSeconds + 0.05f;
            yield return null;
            yield return null;
            Assert.IsFalse(dir.Rewinding);
            Assert.IsFalse(pause.IsPaused, "취소했으면 멈추지 않음");
        }

        [UnityTest]
        public IEnumerator Bug1117_PauseIgnoredOnAttract_WorksAfterStart()
        {
            AttractMode.AttractOnLoad = true;
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            var attract = boot.attract;
            Assert.IsTrue(attract.Active, "시작화면");
            attract.DeltaTime = () => 1f / 60f;
            var pause = boot.pause;

            // 메뉴가 떠 있는 동안
            pause.Toggle();
            Assert.IsFalse(pause.IsPaused, "시작화면 메뉴 중에는 일시정지 안 함");
            Assert.IsFalse(pause.PauseQueued);
            Assert.IsFalse(pause.Panel.gameObject.activeSelf);
            Assert.AreEqual(1f, Time.timeScale);

            // 시작을 누르고 시작선까지 달리는 동안
            attract.menu.GetComponent<TitleMenu>().StartButton.onClick.Invoke();
            Assert.IsTrue(attract.Leading);
            yield return null;
            pause.Toggle();
            Assert.IsFalse(pause.IsPaused, "시작선까지 옮겨 가는 중에도 일시정지 안 함");
            Assert.IsFalse(pause.Panel.gameObject.activeSelf);
            for (int f = 0; f < 600 && attract.Active; f++) yield return null;
            Assert.IsFalse(attract.Active, "시작선 도착");
            yield return null;
            Assert.IsTrue(boot.clock.IsRunning, "1챕터 시작");
            Assert.IsFalse(pause.Panel.gameObject.activeSelf, "패널이 뜬 채로 시작하지 않음");

            // 게임이 시작된 뒤에는 평소처럼
            pause.Toggle();
            Assert.IsTrue(pause.IsPaused, "게임 중에는 일시정지");
            Assert.IsTrue(boot.clock.IsPaused);
            pause.Toggle();
            Assert.IsFalse(pause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
        }
    }
}
