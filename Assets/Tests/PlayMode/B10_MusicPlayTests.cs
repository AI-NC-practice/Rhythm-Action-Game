using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RhythmActionGame.Tests
{
    /// <summary>배경 음악 (B10): Game 씬의 음악 연결, 박자 위치 맞춤, 타이틀 출처 표기.</summary>
    public class B10_MusicPlayTests
    {
        [UnityTest]
        public IEnumerator B10_GameScene_MusicOnBeatClock_BackingOff_FollowsRewindAndSpeed()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            var music = boot.clock.music;
            Assert.IsNotNull(music.clip, "BeatClock.music에 곡 연결");
            Assert.AreEqual("stage1_song", music.clip.name);
            Assert.IsTrue(music.loop, "곡이 반복됨");
            Assert.IsFalse(music.playOnAwake);
            Assert.Contains(music, boot.presentation.sfx.musicSources, "Music 그룹(덕킹)");
            Assert.IsNotNull(boot.backing);
            Assert.IsFalse(boot.backing.enabled, "곡이 있으면 합성 장단 반주는 꺼짐");

            double now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.director.UseChart(new ChartData { name = "t", bpm = 120, runBeats = 264, events = new ChartEvent[0] });   // 차트 사건(멈춤)과 상관없이 음악만 본다
            boot.StartRun(0);
            yield return null;
            Assert.IsTrue(music.isPlaying, "시작하면 재생");
            Assert.AreEqual(GameConfig.Music.FirstBeatSeconds, music.time, 0.1f, "0박 = 곡 첫 박");
            Assert.AreEqual(0, boot.backing.ScheduledCount, "반주 예약 없음");

            boot.clock.RestartAt(150);                                   // 되감기: 150박 = 75초
            yield return null;
            Assert.AreEqual(boot.clock.ExpectedMusicTime(), music.time, 0.1f, "되감기 박자 위치로 옮김");
            Assert.AreEqual(GameConfig.Music.FirstBeatSeconds + 75f, BeatClock.MusicTimeAt(150, music.clip.length), 1e-3f);

            // 어긋나면 다시 맞춤 (반복 이음매·mp3 꼬리, B10)
            int resync = boot.clock.MusicResyncCount;
            music.time = boot.clock.ExpectedMusicTime() + 0.3f;
            Assert.IsFalse(boot.clock.IsPaused);
            yield return null;                                           // 재생 위치는 다음 프레임에 반영 → Update가 다시 맞춤
            yield return null;
            Assert.Greater(boot.clock.MusicResyncCount, resync, "0.3초 어긋남 → 다시 맞춤");
            Assert.AreEqual(boot.clock.ExpectedMusicTime(), music.time, 0.1f);

            boot.clock.ApplyJudgement(Judgement.Perfect);
            Assert.AreEqual(boot.clock.SpeedMultiplier, music.pitch, 1e-5f, "속도 배율 = 재생 속도");
            boot.clock.Pause();
            Assert.IsFalse(music.isPlaying, "일시정지");
            boot.clock.Resume();
            Assert.IsTrue(music.isPlaying);
            boot.clock.StopClock();
        }

        [UnityTest]
        public IEnumerator B10_Title_ShowsMusicCredit()
        {
            yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            var texts = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t => t.text).ToList();
            CollectionAssert.Contains(texts, GameConfig.Music.Credit, "타이틀 화면에 CC BY 출처");
        }
    }
}
