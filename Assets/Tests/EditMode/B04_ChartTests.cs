using System.Linq;
using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>재구성한 차트 (GDD-beta B3, B4, 제안서 0.1.3). 기대값은 docs/GDD-beta.md가 기준이다.</summary>
    public class B04_ChartTests
    {
        static ChartData C => StageChart.Load();

        [Test]
        public void B04_TotalJudgements_65To75_SurroundAndBossKept()
        {
            var c = C;
            int total = ChartCheck.TotalJudgements(c);
            Assert.That(total, Is.InRange(65, 75), $"전체 판정 수 {total}");
            Assert.AreEqual(1, c.events.Count(e => e.IsSurround), "포위 전투 1회");
            Assert.AreEqual(1, c.events.Count(e => e.IsBoss), "무사 1명");
            Assert.AreEqual(GameConfig.SurroundEnemyCount, 5, "포위 5명");
            int run = ChartCheck.RunTargets(c).Count, combat = ChartCheck.CombatJudgements(c);
            Assert.That(run, Is.InRange(35, 50), $"장애물·사건 판정 약 45 ({run})");
            Assert.That(combat, Is.InRange(20, 35), $"전투 판정 약 25 ({combat})");
        }

        [Test]
        public void B04_SectionsKept_PathLength600To700()
        {
            CollectionAssert.AreEqual(new[] { "마을 골목", "돌담과 지붕 오르기", "기와지붕", "마당 포위 전투", "궁궐 담", "성문 위 결전" },
                MapLayout.Sections.Select(s => s.Name).ToArray(), "구간 6개 순서·이름 유지");
            Assert.That(MapLayout.TotalLength, Is.InRange(600f, 700f));
            Assert.AreEqual(C.PathLength, MapLayout.TotalLength, 1e-3f);
            foreach (var e in C.events)
                Assert.AreEqual(MapLayout.SectionOf(e.runBeat).Number, e.section, $"{e.runBeat} {e.kind} 구간 번호");
        }

        [Test]
        public void B04_Flow_FollowsProposal013()
        {
            var c = C;
            bool In(int sec, System.Func<ChartEvent, bool> f) => c.events.Any(e => MapLayout.SectionOf(e.runBeat).Number == sec && f(e));
            bool Kind(ChartEvent e, string k) => e.IsRunEvent && e.EventKind == k;
            // 1 마을 골목: 굴러오는 술독, 매복
            Assert.IsTrue(In(1, e => Kind(e, "rolling_jars")) && In(1, e => Kind(e, "ambush")));
            // 2 돌담과 지붕 오르기: 벽타기, 차양, 밧줄 넘기 + 돌진, 칼 포졸 대치, 지붕 오르기
            Assert.IsTrue(In(2, e => Kind(e, "wall_run")) && In(2, e => Kind(e, "awning_bounce")) && In(2, e => e.kind == "rope_swing" && e.approach == "charge")
                && In(2, e => e.IsDuel && e.kind == "sword") && In(2, e => Kind(e, "roof_climb")));
            // 3 기와지붕: 지붕 건너뛰기, 기와 미끄러짐(2구간 끝~3구간), 추격, 긴 도약, 창·칼 포졸 대치, 뛰어내리기
            Assert.IsTrue(c.events.Any(e => Kind(e, "tile_slip") && e.runBeat >= 100 && e.runBeat < 176), "기와지붕 위 기와 미끄러짐");
            Assert.IsTrue(In(3, e => Kind(e, "roof_jump")) && In(3, e => Kind(e, "chase")) && In(3, e => Kind(e, "long_jump"))
                && In(3, e => e.IsDuel && e.kind == "spear") && In(3, e => e.IsDuel && e.kind == "sword") && In(3, e => Kind(e, "drop_down")));
            // 4 마당 포위: 복선 → 포위
            Assert.IsTrue(c.events.Any(e => e.IsForeshadow && e.runBeat < 184 && e.runBeat + e.span >= 176), "포위 전 복선");
            Assert.IsTrue(In(4, e => e.IsSurround));
            // 5 궁궐 담: 화살 세례, 닫히는 대문, 궁궐 담 오르기, 창 포졸 대치, 매복
            Assert.IsTrue(In(5, e => Kind(e, "arrow_volley")) && In(5, e => Kind(e, "closing_gate")) && In(5, e => Kind(e, "roof_climb"))
                && In(5, e => e.IsDuel && e.kind == "spear") && In(5, e => Kind(e, "ambush")));
            // 6 성문 위: 깃발 줄 → 무사
            Assert.IsTrue(In(6, e => Kind(e, "banner_slide")) && In(6, e => e.IsBoss));
            // 새 사건 8종 모두, 적 등장 방식 3종 모두
            foreach (var k in new[] { "tile_slip", "rope_swing", "rolling_jars", "awning_bounce", "closing_gate", "arrow_volley", "long_jump", "banner_slide", "charge", "ambush", "chase" })
                Assert.IsTrue(c.events.Any(e => (e.IsRunEvent && e.EventKind == k) || e.approach == k), k);
        }

        [Test]
        public void B04_FirstTwoEvents8BeatsApart_NoOverlap_JudgeGap05()
        {
            var c = C;
            var runs = c.events.Where(e => e.IsRunEvent).OrderBy(e => e.runBeat).ToArray();
            Assert.AreEqual(8, runs[1].runBeat - runs[0].runBeat, "1구간 앞 2개 사건 간격 8박 (연습)");
            for (int i = 1; i < runs.Length; i++)
                Assert.GreaterOrEqual(runs[i].runBeat, runs[i - 1].runBeat + ChartCheck.Length(runs[i - 1]), $"{runs[i - 1].runBeat} 사건과 겹침");
            var t = ChartCheck.RunTargets(c);
            for (int i = 1; i < t.Count; i++)
                Assert.GreaterOrEqual(t[i].RunBeat - t[i - 1].RunBeat, 0.5 - 1e-9, $"판정 간격 {t[i - 1].RunBeat} → {t[i].RunBeat}");
            // 몰아치는 구간: 사건 사이 간격 0박이 실제로 있음
            Assert.IsTrue(Enumerable.Range(1, runs.Length - 1).Any(i => runs[i].runBeat == runs[i - 1].runBeat + ChartCheck.Length(runs[i - 1])), "간격 0박 구간");
            // 멈추는 전투 앞뒤로 사건이 겹치지 않음
            foreach (var stop in c.events.Where(e => e.IsDuel || e.IsSurround || e.IsBoss))
                Assert.IsFalse(runs.Any(r => r.runBeat < stop.runBeat && r.runBeat + ChartCheck.Length(r) > stop.runBeat), $"{stop.runBeat} 전투 위치에 사건이 걸침");
        }

        [Test]
        public void B04_NoJudgeInsideSlowMotionWindow()
        {
            var c = C;
            var all = ChartCheck.RunTargets(c);
            var candidates = ChartCheck.SlowMoCandidates(c);
            Assert.GreaterOrEqual(candidates.Count, 3, "사건 슬로모션 후보가 차트에 있음");
            // 0.4초 = 최대 속도(1.30)에서 1.04박. GDD-beta B4·제안서 2.5: 후보 판정 뒤 다음 판정까지 최소 2박
            double window = GameConfig.SlowMotionSeconds * GameConfig.SpeedMax / GameConfig.SecondsPerBeat;
            foreach (var s in candidates)
            {
                var next = all.Where(t => t.RunBeat > s.RunBeat + 1e-9).Select(t => t.RunBeat).DefaultIfEmpty(double.MaxValue).Min();
                Assert.Greater(next - s.RunBeat, window, $"{s.Event.EventKind}@{s.RunBeat}: 슬로모션 0.4초 안에 판정");
                Assert.GreaterOrEqual(next - s.RunBeat, 2.0, $"{s.Event.EventKind}@{s.RunBeat}: 다음 판정까지 2박");
                // 슬로모션이 걸린 사건 안에 멈추는 전투가 끼지 않음
                Assert.IsFalse(c.events.Any(e => (e.IsDuel || e.IsSurround || e.IsBoss) && e.runBeat > s.Event.runBeat && e.runBeat < s.Event.runBeat + ChartCheck.Length(s.Event)));
            }
        }

        [Test]
        public void B04_DevChapterStarts_MatchSections_FirstJudgeAfter2Beats()
        {
            int[] expected = { 0, 56, 108, 176, 196, 252 };
            var t = ChartCheck.RunTargets(C);
            for (int n = 1; n <= 6; n++)
            {
                int start = MapLayout.Sections[n - 1].StartBeat;
                Assert.AreEqual(expected[n - 1], start);
                // 챕터 시작 지점에서 첫 판정 원이 제때(2박 전) 뜰 수 있게 첫 목표가 시작 + 2박 이후
                var first = t.Where(x => x.Event.runBeat >= start).Select(x => x.RunBeat).DefaultIfEmpty(double.MaxValue).Min();
                var stop = C.events.Where(e => (e.IsDuel || e.IsSurround || e.IsBoss) && e.runBeat >= start).Select(e => (double)e.runBeat).DefaultIfEmpty(double.MaxValue).Min();
                Assert.GreaterOrEqual(System.Math.Min(first, stop) - start, GameConfig.ApproachBeats, $"챕터 {n}");
            }
        }
    }
}
