using System.Linq;
using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>손이 바쁜 리듬게임: 달리기 장단 노드·전투 연속기·소리 크기 (GDD-beta B18).</summary>
    public class B50_JangdanTests
    {
        [Test]
        public void B50_JangdanNotes_Rules_GapsAndClearZones()
        {
            var c = StageChart.Load();
            Assert.IsTrue(c.jangdan, "스테이지 차트는 장단 노드를 씀");
            var notes = RhythmNotes.Generate(c);
            var events = ChartCheck.RunTargets(c).Select(t => t.RunBeat).ToList();
            var stops = c.events.Where(e => !e.IsRunEvent && !e.IsForeshadow).Select(e => (double)e.runBeat).ToList();
            var slow = ChartCheck.SlowMoCandidates(c).Select(t => t.RunBeat).ToList();
            double boss = c.events.First(e => e.type == "boss").runBeat;
            UnityEngine.Debug.Log($"[B50] 장단 노드 {notes.Count}개 (← {notes.Count(n => n.Key == ArrowKey.Left)}, → {notes.Count(n => n.Key == ArrowKey.Right)}), 전체 판정 {ChartCheck.TotalJudgements(c)}");
            Assert.GreaterOrEqual(notes.Count, 200, "장단 노드가 곳곳에");
            Assert.IsTrue(notes.All(n => n.Key == ArrowKey.Left || n.Key == ArrowKey.Right), "← 궁편, → 채편 두 키만");
            Assert.Greater(notes.Count(n => n.RunBeat % 1.0 != 0), 30, "반 박 장단");
            for (int i = 1; i < notes.Count; i++) Assert.GreaterOrEqual(notes[i].RunBeat - notes[i - 1].RunBeat, 0.5 - 1e-9, "노드끼리 0.5박 이상");
            foreach (var n in notes)
            {
                Assert.IsTrue(events.All(e => System.Math.Abs(e - n.RunBeat) >= 0.5 - 1e-9), $"사건 판정과 0.5박 이상 ({n.RunBeat})");
                Assert.IsTrue(stops.All(e => n.RunBeat <= e - 2 + 1e-9 || n.RunBeat >= e + 2 - 1e-9), $"멈추는 전투 앞뒤 2박 비움 ({n.RunBeat})");
                Assert.IsTrue(slow.All(e => !(n.RunBeat > e - 1e-9 && n.RunBeat < e + 2 + 1e-9)), $"슬로모션 후보 뒤 2박 비움 ({n.RunBeat})");
                var sec = MapLayout.SectionOf((int)System.Math.Floor(n.RunBeat));
                Assert.GreaterOrEqual(n.RunBeat, sec.StartBeat + 2 - 1e-9, "챕터 시작 2박 비움");
                Assert.Less(n.RunBeat, boss - 2 + 1e-9, "무사 결투 앞에서 끝");
            }
            // 뒤 챕터일수록 반 박이 많다
            double HalfRatio(int ch) { var s = notes.Where(n => MapLayout.SectionOf((int)n.RunBeat).Number == ch).ToList(); return s.Count == 0 ? 0 : s.Count(n => n.RunBeat % 1.0 != 0) / (double)s.Count; }
            Assert.Greater(HalfRatio(3), HalfRatio(1), "기와지붕이 마을 골목보다 쪼갠 박이 많음");
            Assert.That(ChartCheck.TotalJudgements(c), Is.InRange(360, 440), "전체 약 400개 (사용자 결정: 약 3배)");
        }

        [Test]
        public void B50_RowSlots_InOrder_LeftToRight_Wrap6_NoOverlap()
        {
            // 장단 줄 (B18.2 표시): 노드 순서대로 왼쪽부터 6칸, 160 px 간격, 화면 위쪽 한 줄
            Assert.AreEqual(6, GameConfig.Jangdan.RowSlots);
            for (int i = 0; i < 6; i++)
            {
                var p = JudgeRingView.RowSlot(i);
                Assert.AreEqual(GameConfig.Jangdan.RowYPx, p.y, 1e-4f);
                Assert.AreEqual((i - 2.5f) * 160f, p.x, 1e-3f, "왼쪽부터 차례로");
            }
            Assert.AreEqual(JudgeRingView.RowSlot(0), JudgeRingView.RowSlot(6), "6칸을 돌려 씀");
            // 동시에 떠 있는 장단 원(2박 안, 최소 0.5박 간격 → 최대 5개)은 서로 다른 칸
            var notes = RhythmNotes.Generate(StageChart.Load());
            for (int i = 0; i < notes.Count; i++)
                for (int j = i + 1; j < notes.Count && notes[j].RunBeat - notes[i].RunBeat <= GameConfig.ApproachBeats; j++)
                    Assert.AreNotEqual(notes[i].Index % 6, notes[j].Index % 6, $"함께 뜨는 원은 다른 칸 ({notes[i].RunBeat}, {notes[j].RunBeat})");
            Assert.Greater(GameConfig.Jangdan.RowYPx - GameConfig.Jangdan.EventMaxYPx, 140f, "사건·전투 원과 장단 줄 사이 간격");
        }

        [Test]
        public void B50_ChartWithoutFlag_NoJangdan_And_RhythmHitSound()
        {
            var small = new ChartData { name = "t", bpm = 120, runBeats = 64, events = new ChartEvent[0] };
            Assert.AreEqual(0, RhythmNotes.Generate(small).Count, "테스트용 작은 차트(jangdan 없음)는 장단 노드 없음");
            Assert.AreEqual(GameConfig.HitSound.PerfectVolume, HitSoundRules.PickRhythm(Judgement.Perfect, ArrowKey.Left, out var k1), 1e-6f);
            Assert.AreEqual(SfxKind.HitPerfect, k1, "← 궁편 덩");
            Assert.AreEqual(GameConfig.HitSound.NormalVolume, HitSoundRules.PickRhythm(Judgement.Normal, ArrowKey.Right, out var k2), 1e-6f);
            Assert.AreEqual(SfxKind.HitNormal, k2, "→ 채편 덕");
            HitSoundRules.PickRhythm(Judgement.Fail, ArrowKey.Right, out var k3);
            Assert.AreEqual(SfxKind.HitFail, k3);
            Assert.AreEqual(0.45f, GameConfig.Music.Volume, 1e-6f, "음악 음량 낮춤 (B18.1)");
            Assert.AreEqual(4, GameConfig.NormalEnemyAttacks, "연속기 4회 (B18.3)");
        }
    }
}
