using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>히트사운드 (GDD-beta B14): Game 씬에서 입력마다 바로 소리, 효과음 풀과 따로.</summary>
    public class B36_HitSoundPlayTests
    {
        [UnityTest]
        public IEnumerator B36_GameScene_EveryInput_PlaysHitSound_SeparateFromSfxPool()
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
            Assert.IsNotNull(hit, "Game 씬에 히트사운드");
            Assert.AreEqual(boot.judge, hit.judge);
            Assert.AreEqual(GameConfig.HitSound.Voices, hit.Sources.Length, "AudioSource 4개");
            var sfx = boot.presentation.sfx;
            var sfxSources = sfx.GetComponents<AudioSource>();
            Assert.IsFalse(hit.Sources.Any(s => sfxSources.Contains(s)), "효과음 풀과 다른 AudioSource");
            Assert.IsFalse(hit.Sources.Any(s => sfx.musicSources.Contains(s)), "음악 그룹(덕킹)에 들지 않음");

            // 원 없는 입력 → 작은 "틱"
            int n0 = hit.PlayedCount;
            boot.judge.Submit(ArrowKey.Up, now);
            Assert.AreEqual(n0 + 1, hit.PlayedCount, "누르는 즉시 (같은 프레임)");
            Assert.AreEqual(SfxKind.HitEmpty, hit.LastKind);
            Assert.AreEqual(GameConfig.HitSound.EmptyVolume, hit.LastVolume, 1e-6f);

            // PERFECT → "덩", 판정 효과음도 그대로 남
            var c1 = boot.judge.Schedule(4, ArrowKey.Up);
            var c2 = boot.judge.Schedule(6, ArrowKey.Left);
            var c3 = boot.judge.Schedule(8, ArrowKey.Up);
            now = 2.0; boot.judge.Tick();
            boot.judge.Submit(ArrowKey.Up, now);
            Assert.AreEqual(Judgement.Perfect, c1.Result);
            Assert.AreEqual(SfxKind.HitPerfect, hit.LastKind, "PERFECT = 덩");
            Assert.AreEqual(1.0f, hit.LastVolume, 1e-6f);
            Assert.AreEqual(hit.LastSource.clip, sfx.Clip(SfxKind.HitPerfect), "합성 WAV 클립");
            var perfectSrc = hit.LastSource;

            // NORMAL → "딱" (목표보다 0.15초 늦게)
            now = 3.15; boot.judge.Tick();
            boot.judge.Submit(ArrowKey.Left, now);
            Assert.AreEqual(Judgement.Normal, c2.Result);
            Assert.AreEqual(SfxKind.HitNormal, hit.LastKind, "NORMAL = 딱");
            Assert.AreNotSame(perfectSrc, hit.LastSource, "다음 AudioSource를 돌려 씀 (앞 소리를 끊지 않음)");

            // 틀린 키 → FAIL "턱"
            now = 4.0; boot.judge.Tick();
            boot.judge.Submit(ArrowKey.Down, now);
            Assert.AreEqual(Judgement.Fail, c3.Result);
            Assert.AreEqual(SfxKind.HitFail, hit.LastKind, "틀린 키 = 턱");
            Assert.AreEqual(GameConfig.HitSound.FailVolume, hit.LastVolume, 1e-6f);

            // 판정 효과음(금속음)을 낸 직후 히트사운드를 내도 효과음은 끊기지 않는다
            sfx.Play(SfxKind.MetalClang);
            hit.Play(SfxKind.HitPerfect, 1f);
            Assert.AreEqual(1, sfx.ActiveCount(SfxKind.MetalClang), "효과음 풀은 그대로");
            Assert.AreEqual(SfxKind.MetalClang, sfx.History[sfx.History.Count - 1], "히트사운드는 효과음 풀을 쓰지 않음");

            yield return null;
            Assert.AreEqual(5, hit.PlayedCount - n0, "입력 4번 + 직접 1번 = 히트사운드 5번");
            boot.clock.StopClock();
        }
    }
}
