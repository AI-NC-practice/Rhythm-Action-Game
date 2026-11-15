using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>곡이 차트를 따름 (GDD-beta B16.2) + 챕터 곡 위치 (B16.3).</summary>
    public class B42_SongFollowsChartTests
    {
        [System.Serializable] class Judge { public double beat; public string key; public string result; public string state; public int chapter; }
        [System.Serializable] class Chapter { public int number; public double run; public double clock; }
        [System.Serializable] class Combat { public double start; public double end; public string kind; }
        [System.Serializable] class Timeline { public Judge[] judges; public Chapter[] chapters; public Combat[] combats; public double end; }

        static Timeline Load()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), GameConfig.Music.TimelinePath);
            Assert.IsTrue(File.Exists(path), "판정 타임라인 파일: " + GameConfig.Music.TimelinePath);
            return JsonUtility.FromJson<Timeline>(File.ReadAllText(path));
        }

        [Test]
        public void B42_Timeline_MatchesChart_AllPerfect_ChaptersAndCombats()
        {
            var t = Load();
            var chart = StageChart.Load();
            Assert.AreEqual(ChartCheck.TotalJudgements(chart), t.judges.Length, "타임라인 판정 수 = 차트 판정 수");
            Assert.IsTrue(t.judges.All(j => j.result == "Perfect"), "모두 PERFECT 진행");
            Assert.GreaterOrEqual(t.judges.Count(j => j.beat % 1.0 != 0), 30, "반 박 판정 (B16.1 + 화살·윈드밀·무사)");
            for (int i = 1; i < t.judges.Length; i++) Assert.GreaterOrEqual(t.judges[i].beat, t.judges[i - 1].beat, "시계 박자 순");
            Assert.AreEqual(6, t.chapters.Length, "챕터 6개");
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(MapLayout.Sections[i].StartBeat, t.chapters[i].run, 0.05, $"챕터 {i + 1} 달리기 박자");
                Assert.AreEqual(GameConfig.Music.ChapterSongBeats[i], t.chapters[i].clock, 0.05, $"챕터 {i + 1} 곡 박 = 타임라인 시계 박자");
            }
            Assert.AreEqual(1, t.combats.Count(c => c.kind == "surround"));
            Assert.AreEqual(1, t.combats.Count(c => c.kind == "boss"));
            Assert.AreEqual(chart.Enemies.Count(), t.combats.Count(c => c.kind == "normal"), "일반 전투 수");
        }

        /// <summary>곡의 판정 박마다 강세: 판정 박의 소리 시작 세기가 판정 없는 16분 엇박보다 1.3배 이상 (첫 박 위치 보정).</summary>
        [Test]
        public void B42_Song_AccentOnEveryJudgeBeat()
        {
            var t = Load();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GameConfig.Music.ClipPath);
            Assert.IsTrue(clip.LoadAudioData());
            var data = new float[clip.samples * clip.channels];
            Assert.IsTrue(clip.GetData(data, 0));
            int rate = clip.frequency, ch = clip.channels;
            int hop = System.Math.Max(1, rate / 1000);
            int frames = clip.samples / hop;
            var e = new double[frames];
            float prev = 0;
            for (int f = 0; f < frames; f++)
            {
                double s = 0;
                for (int i = f * hop; i < (f + 1) * hop; i++)
                {
                    float m = 0; for (int c = 0; c < ch; c++) m += data[i * ch + c]; m /= ch;
                    float hp = m - prev; prev = m; s += hp * hp;
                }
                e[f] = System.Math.Log(1e-9 + s);
            }
            double At(double songBeat)
            {
                double sec = GameConfig.Music.FirstBeatSeconds + songBeat * GameConfig.SecondsPerBeat;
                int c0 = (int)(sec * rate / hop);
                double best = 0;
                for (int f = c0 - 8; f <= c0 + 8; f++) if (f >= 5 && f < frames) best = System.Math.Max(best, e[f] - e[f - 5]);
                return best;
            }
            var judgeQ = new System.Collections.Generic.HashSet<int>(t.judges.Select(j => (int)System.Math.Round(j.beat * 4)));
            double judge = t.judges.Average(j => At(j.beat));
            double half = t.judges.Where(j => j.beat % 1.0 != 0).Average(j => At(j.beat));
            var offQ = Enumerable.Range(8, (int)(t.end * 4) - 8).Where(q => q % 2 == 1 && !judgeQ.Contains(q)).ToList();
            double off = offQ.Average(q => At(q / 4.0));
            Debug.Log($"[B42] 소리 시작 세기: 판정 박 {judge:F2}, 반 박 판정 {half:F2}, 판정 없는 16분 엇박 {off:F2} (비 {judge / off:F2})");
            Assert.Greater(judge, off * 1.3, "판정 박마다 장구·꽹과리 강세");
            Assert.Greater(half, off * 1.2, "반 박 판정에도 강세 (장구 덕 + 꽹과리)");
        }

        [Test]
        public void B42_SongBeatAtRun_ChapterStartsFromTimeline()
        {
            CollectionAssert.AreEqual(new[] { 0, 56, 116, 200, 236, 300 }, GameConfig.Music.ChapterSongBeats);
            for (int i = 0; i < 6; i++)
                Assert.AreEqual(GameConfig.Music.ChapterSongBeats[i], MusicPlan.SongBeatAtRun(MapLayout.Sections[i].StartBeat), 1e-9, $"챕터 {i + 1} 시작");
            Assert.AreEqual(0.0, MusicPlan.SongBeatAtRun(0), 1e-9);
            Assert.AreEqual(60.0, MusicPlan.SongBeatAtRun(60), 1e-9, "챕터 2 안: 56 + 4");
            Assert.AreEqual(118.0, MusicPlan.SongBeatAtRun(110), 1e-9, "챕터 3 안: 116 + 2");
        }
    }
}
