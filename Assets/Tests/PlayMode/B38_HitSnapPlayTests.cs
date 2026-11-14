using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>히트사운드 박 맞춤 (GDD-beta B15.3): Game 씬에서 이른 PERFECT는 목표 박에 예약.</summary>
    public class B38_HitSnapPlayTests
    {
        [UnityTest]
        public IEnumerator B38_GameScene_EarlyPerfectScheduledOnBeat_LateAndNormalImmediate()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            double now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            boot.director.UseChart(new ChartData { name = "t", bpm = 120, runBeats = 264, events = new ChartEvent[0] });
            boot.StartRun(0);
            yield return null;
            var hit = boot.hitSound;

            var c1 = boot.judge.Schedule(4, ArrowKey.Up);      // 2.0초
            var c2 = boot.judge.Schedule(6, ArrowKey.Up);      // 3.0초
            var c3 = boot.judge.Schedule(8, ArrowKey.Up);      // 4.0초
            now = 1.95; boot.judge.Tick();
            boot.judge.Submit(ArrowKey.Up, now);               // 50 ms 이름
            Assert.AreEqual(Judgement.Perfect, c1.Result);
            Assert.AreEqual(SfxKind.HitPerfect, hit.LastKind);
            Assert.AreEqual(0.05, hit.LastDelay, 1e-6, "목표 박(2.0초)까지 50 ms 기다려 울림");

            now = 3.04; boot.judge.Tick();
            boot.judge.Submit(ArrowKey.Up, now);               // 40 ms 늦음
            Assert.AreEqual(Judgement.Perfect, c2.Result);
            Assert.AreEqual(0.0, hit.LastDelay, "늦은 PERFECT는 바로");

            now = 3.85; boot.judge.Tick();
            boot.judge.Submit(ArrowKey.Up, now);               // 150 ms 이름 → NORMAL
            Assert.AreEqual(Judgement.Normal, c3.Result);
            Assert.AreEqual(SfxKind.HitNormal, hit.LastKind);
            Assert.AreEqual(0.0, hit.LastDelay, "이른 NORMAL은 바로");
            yield return null;
            boot.clock.StopClock();
        }
    }
}
