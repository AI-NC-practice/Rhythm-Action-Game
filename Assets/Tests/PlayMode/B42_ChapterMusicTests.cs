using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>챕터에서 시작하면 곡도 그 챕터 위치부터 (GDD-beta B16.3).</summary>
    public class B42_ChapterMusicTests
    {
        [UnityTest]
        public IEnumerator B42_StartRunAtChapter_MusicStartsAtChapterSongBeat()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            double now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            var music = boot.clock.music;

            boot.StartRun(0);
            yield return null;
            Assert.AreEqual(0.0, boot.clock.MusicBeatOffset, 1e-9, "처음부터 시작하면 곡도 처음");

            int ch3 = MapLayout.Sections[2].StartBeat;                    // 108박 (기와지붕)
            boot.StartRun(ch3, keepScore: true);                          // 챕터 되감기와 같은 경로
            yield return null;
            Assert.AreEqual(0.0, boot.clock.CurrentBeat, 0.01, "시계는 0박부터 다시");
            Assert.AreEqual(GameConfig.Music.ChapterSongBeats[2], boot.clock.MusicBeatOffset, 1e-9, "곡은 챕터 3 위치(116박)부터");
            float want = BeatClock.MusicTimeAt(GameConfig.Music.ChapterSongBeats[2] + MusicPitchCorrector.LatencySeconds * boot.clock.SpeedMultiplier / GameConfig.SecondsPerBeat, music.clip.length);
            Assert.AreEqual(want, music.time, 0.1f, "곡 재생 위치 = 첫 박 + 116박 × 0.5초");
            Assert.AreEqual(boot.clock.ExpectedMusicTime(), music.time, 0.1f);

            boot.clock.RestartAt(10);                                     // 그 안에서 무사·윈드밀 되감기도 오프셋 유지
            yield return null;
            Assert.AreEqual(BeatClock.MusicTimeAt(GameConfig.Music.ChapterSongBeats[2] + 10 + MusicPitchCorrector.LatencySeconds * boot.clock.SpeedMultiplier / GameConfig.SecondsPerBeat, music.clip.length),
                music.time, 0.1f, "되감기도 챕터 곡 위치 기준");
            boot.clock.StopClock();
        }
    }
}
