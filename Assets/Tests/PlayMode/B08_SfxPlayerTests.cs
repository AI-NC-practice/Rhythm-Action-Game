using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>효과음 재생·덕킹·장단 반주 (GDD-beta B7, 제안서 4·5장).</summary>
    public class B08_SfxPlayerTests
    {
        float real;
        GameObject root;
        SfxPlayer sfx;

        [SetUp]
        public void SetUp()
        {
            real = 10f;
            root = new GameObject("B08");
            sfx = root.AddComponent<SfxPlayer>();
            sfx.RealTime = () => real;
        }

        [TearDown]
        public void TearDown() { Object.Destroy(root); Time.timeScale = 1f; }

        [UnityTest]
        public IEnumerator B08_Pool8_SameKindMax3()
        {
            Assert.AreEqual(8, sfx.PoolSize, "AudioSource 풀 8개");
            for (int i = 0; i < 5; i++) sfx.Play(SfxKind.MetalClang);
            Assert.AreEqual(3, sfx.ActiveCount(SfxKind.MetalClang), "같은 종류는 동시 3개까지");
            sfx.Play(SfxKind.LowBoom); sfx.Play(SfxKind.Whoosh);
            Assert.AreEqual(1, sfx.ActiveCount(SfxKind.LowBoom));
            Assert.AreEqual(3, sfx.ActiveCount(SfxKind.MetalClang));
            real += 1f;                                           // 금속음 0.8초 뒤 끝남
            Assert.AreEqual(0, sfx.ActiveCount(SfxKind.MetalClang));
            Assert.AreEqual(8, root.GetComponents<AudioSource>().Length, "새 AudioSource를 만들지 않음");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B08_Duck_OnlyMusicVolume_DeepestWins()
        {
            var musicGo = new GameObject("Music"); musicGo.transform.SetParent(root.transform);
            var music = musicGo.AddComponent<AudioSource>();
            music.clip = AudioClip.Create("m", 44100 * 2, 1, 44100, false);
            music.volume = 0.8f;
            music.pitch = 1.2f;
            music.time = 0.5f;
            float t0 = music.time;
            sfx.RegisterMusic(music);
            var effect = new GameObject("Fx").AddComponent<AudioSource>(); effect.transform.SetParent(root.transform);
            effect.volume = 1f;
            sfx.Tick();
            Assert.AreEqual(0.8f, music.volume, 1e-4f);

            sfx.Duck(-4f, 0.15f);
            sfx.Tick();
            Assert.AreEqual(0.8f * Mathf.Pow(10f, -4f / 20f), music.volume, 1e-4f, "−4 dB");
            sfx.Duck(-8f, 0.3f);                                  // 더 깊은 쪽
            sfx.Tick();
            Assert.AreEqual(0.8f * Mathf.Pow(10f, -8f / 20f), music.volume, 1e-4f, "가장 깊은 값 하나 (−8 dB)");
            sfx.Duck(-4f, 0.1f);                                  // 얕은 요청은 무시
            sfx.Tick();
            Assert.AreEqual(0.8f * Mathf.Pow(10f, -8f / 20f), music.volume, 1e-4f);
            Assert.AreEqual(1.2f, music.pitch, 1e-6f, "재생 속도 그대로");
            Assert.AreEqual(t0, music.time, 1e-4f, "재생 위치 그대로");
            Assert.AreEqual(1f, effect.volume, "효과음(그룹 밖) 음량은 그대로");
            real += 0.31f;
            sfx.Tick();
            Assert.AreEqual(0.8f, music.volume, 1e-4f, "끝나면 원래대로");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B08_Presets_PlaySoundsAndDuck()
        {
            var pd = root.AddComponent<PresentationDirector>();
            pd.sfx = sfx;
            pd.Enabled = true;
            pd.Play(GameConfig.Fx.ParryPerfect);
            CollectionAssert.IsSubsetOf(new[] { SfxKind.MetalClang, SfxKind.LowBoom }, sfx.History, "PERFECT 패링: 챙 + 쿵");
            sfx.Tick();
            Assert.AreEqual(Mathf.Pow(10f, -4f / 20f), sfx.DuckFactor, 1e-4f, "음악 −4 dB");
            pd.Play(GameConfig.Fx.Hit);
            CollectionAssert.IsSubsetOf(new[] { SfxKind.BodyHit, SfxKind.EarRing }, sfx.History, "피격: 타격 + 귀 울림");
            pd.Play(GameConfig.Fx.DefendNormal);
            Assert.AreEqual(SfxKind.DullClank, sfx.History[sfx.History.Count - 1], "NORMAL: 둔한 탕");
            pd.Play(GameConfig.Fx.WindmillArrow);
            Assert.AreEqual(SfxKind.Buk, sfx.History[sfx.History.Count - 1], "윈드밀 화살표: 북");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B08_GameScene_JangguBacking_ReplacesMetronome_BeatAccurate()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            Assert.IsNotNull(boot.backing, "장단 반주");
            Assert.IsNull(Object.FindAnyObjectByType<Metronome>(), "메트로놈 대신");
            Assert.IsNotNull(boot.presentation.sfx, "연출에 효과음 연결");
            Assert.Contains(boot.clock.music, boot.presentation.sfx.musicSources, "곡은 Music 그룹");
            Assert.GreaterOrEqual(boot.presentation.sfx.clips.Count, 13, "Assets/Audio/Sfx 클립 연결");

            double now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.StartRun(0);
            boot.backing.Tick();
            Assert.AreEqual(0.0, boot.backing.FirstScheduledBeat, 1e-9, "첫 박부터");
            Assert.AreEqual(boot.clock.TimeOfBeat(0), boot.backing.FirstScheduledAt, 0.010, "첫 박 오차 10 ms 이하");

            var beats = new List<double> { boot.backing.LastScheduledBeat };
            for (int i = 0; i < 400; i++)
            {
                now += 0.01;
                if (i == 150) boot.clock.ApplyJudgement(Judgement.Perfect);   // 속도가 바뀌어도
                boot.backing.Tick();
                if (boot.backing.LastScheduledBeat != beats[beats.Count - 1])
                {
                    beats.Add(boot.backing.LastScheduledBeat);
                    Assert.AreEqual(boot.clock.TimeOfBeat(boot.backing.LastScheduledBeat), boot.backing.LastScheduledAt, 0.010,
                        $"{boot.backing.LastScheduledBeat}박 예약 시각 = 박자 시계");
                }
            }
            CollectionAssert.AreEqual(new[] { 0.0, 1.0, 1.5, 2.0, 3.0, 3.5, 4.0, 5.0, 5.5 }, beats.GetRange(0, 9), "덩-쿵-덕-덩-쿵-덕");
        }
    }
}
