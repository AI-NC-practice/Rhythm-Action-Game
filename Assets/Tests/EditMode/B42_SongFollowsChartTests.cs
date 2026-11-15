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

        /// <summary>
        /// 곡의 판정 박 (GDD-beta B17.1이 B16.2를 바꿈): 판정 박에는 곡이 장구를 치지 않고 꽹과리 "준비" 소리만 둔다.
        /// 그 박의 장구는 플레이어의 히트사운드가 채운다. 곡 악보(make-song.py가 남기는 stage1_song.score.txt)로 확인한다.
        /// </summary>
        [Test]
        public void B42_Song_JudgeBeats_ReadySoundOnly_NoJanggu()
        {
            var t = Load();
            string scorePath = Path.Combine(Directory.GetCurrentDirectory(), "harness/music/stage1_song.score.txt");
            Assert.IsTrue(File.Exists(scorePath), "곡 악보 파일");
            var lines = File.ReadAllLines(scorePath).Where(l => l.Length > 0 && l[0] != '#').Select(l => l.Split('	')).ToList();
            var judgeQ = new System.Collections.Generic.HashSet<int>(t.judges.Select(j => (int)System.Math.Round(j.beat * 8)));
            int ready = lines.Count(l => l[1].StartsWith("판정 준비") && judgeQ.Contains((int)System.Math.Round(double.Parse(l[0], System.Globalization.CultureInfo.InvariantCulture) * 8)));
            Assert.AreEqual(t.judges.Length, ready, "판정 박마다 꽹과리 준비 소리");
            var jangguOnJudge = lines.Where(l => l[1].StartsWith("장구") && judgeQ.Contains((int)System.Math.Round(double.Parse(l[0], System.Globalization.CultureInfo.InvariantCulture) * 8))).ToList();
            Assert.IsEmpty(jangguOnJudge, "판정 박에는 곡의 장구가 없음 (플레이어가 친다)");
            Assert.Greater(lines.Count(l => l[1].StartsWith("장구")), 300, "판정 박이 아닌 곳에는 장구가 그대로 있음");
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
