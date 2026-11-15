using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>Game 씬: 실제 장구 히트사운드 + 누르는 순간 음악 덕킹 (GDD-beta B17.1).</summary>
    public class B44_RecordedHitPlayTests
    {
        [UnityTest]
        public IEnumerator B44_GameScene_UsesRecordedJanggu_AndDucksMusic()
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
            Assert.AreEqual(4, hit.recorded.Length);
            for (int i = 0; i < 4; i++) Assert.IsNotNull(hit.recorded[i], $"녹음 히트사운드 {i} 연결");
            Assert.AreEqual("Hit_Perfect", hit.ClipFor(SfxKind.HitPerfect).name);
            Assert.AreEqual("Hit_Normal", hit.ClipFor(SfxKind.HitNormal).name);
            Assert.AreEqual("Hit_Fail", hit.ClipFor(SfxKind.HitFail).name);
            Assert.AreEqual("Hit_Empty", hit.ClipFor(SfxKind.HitEmpty).name);

            var sfx = boot.presentation.sfx;
            sfx.Tick();
            Assert.AreEqual(1f, sfx.DuckFactor, 1e-4f, "누르기 전에는 음악 그대로");
            boot.judge.Schedule(4, ArrowKey.Up);
            now = 2.0; boot.judge.Tick();
            boot.judge.Submit(ArrowKey.Up, now);
            Assert.AreEqual("Hit_Perfect", hit.LastSource.clip.name, "PERFECT = 실제 장구 덩");
            sfx.Tick();
            Assert.That(sfx.DuckFactor, Is.InRange(0.65f, 0.75f), "누르는 순간 음악을 잠깐 줄임 (−3 dB ≈ 0.71배)");
            boot.clock.StopClock();
        }
    }
}
