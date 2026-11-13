using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>무사 결투 패턴 (GDD-beta B11). 기대값은 docs/GDD-beta.md B11.1 표를 그대로 옮긴 것이다.</summary>
    public class B26_BossDuelPatternTests
    {
        // B11.1 표: 종류+목표 박 (O 기준)
        const string Table =
            "A6 A8 S10 S11 A13 A14 A15 S17 S18 A20 A21 S22 S23 " +
            "L30 " +
            "A32 A32.5 S34 S35 S37 A37.5 A39 A39.5 A40 S42 S43 S44 A46 A46.5 S48 S48.5 A50 S51 A52 " +
            "A56 A57 A58 " +   // beta1-1: 옆차기 다운 구간 (GDD-beta B13.4)
            "A60 S61 A62 S63 A64 S65 A66 S67 " +
            "F70";   // beta1-1: 칼 겨루기 연장(B28 사용자 결정) — L28 이후 2박씩 뒤로

        static char Letter(CombatStep s) =>
            s.Kind == CombatStepKind.Attack ? 'A' : s.Kind == CombatStepKind.Strike ? 'S' : s.Kind == CombatStepKind.Lock ? 'L' : s.Kind == CombatStepKind.Finisher ? 'F' : '?';

        [Test]
        public void B26_Steps_MatchTable_KindsAndBeats()
        {
            var steps = BossDuel.Steps();
            var want = Table.Split(' ');
            Assert.AreEqual(want.Length, steps.Count);
            for (int i = 0; i < want.Length; i++)
            {
                Assert.AreEqual(want[i][0], Letter(steps[i]), $"{i}번 종류");
                Assert.AreEqual(double.Parse(want[i].Substring(1), System.Globalization.CultureInfo.InvariantCulture), steps[i].OffsetBeats, 1e-9, $"{i}번 박");
            }
        }

        [Test]
        public void B26_Counts_PerPhase_Total45()
        {
            var steps = BossDuel.Steps();
            Assert.AreEqual(45, steps.Count, "B11.6 → B13.4: 결투 판정 45 (다운 구간 3 추가)");
            Assert.AreEqual(13, steps.Count(s => s.Phase == BossDuel.PhaseExplore), "1단계");
            Assert.AreEqual(1, steps.Count(s => s.Phase == BossDuel.PhaseLock), "칼 겨루기");
            Assert.AreEqual(19, steps.Count(s => s.Phase == BossDuel.PhaseClash), "2단계");
            Assert.AreEqual(8, steps.Count(s => s.Phase == BossDuel.PhaseFlurry), "3단계");
            Assert.AreEqual(1, steps.Count(s => s.Phase == BossDuel.PhaseFinish), "마무리");
            Assert.AreEqual(44, BossDuel.PostureSteps, "마무리 전 판정 수 N");
            Assert.AreEqual(3, steps.Count(s => s.Phase == BossDuel.PhaseDown), "다운 구간 (B13.4)");
        }

        [Test]
        public void B26_PhaseStarts_And_Length74()
        {
            Assert.AreEqual(4, BossDuel.PhaseStart(BossDuel.PhaseExplore));
            Assert.AreEqual(24, BossDuel.PhaseStart(BossDuel.PhaseLock));
            Assert.AreEqual(30, BossDuel.PhaseStart(BossDuel.PhaseClash));
            Assert.AreEqual(54, BossDuel.PhaseStart(BossDuel.PhaseDown), "다운 구간 처음 (B13.4)");
            Assert.AreEqual(58, BossDuel.PhaseStart(BossDuel.PhaseFlurry));
            Assert.AreEqual(68, BossDuel.PhaseStart(BossDuel.PhaseFinish));
            Assert.AreEqual(74, BossDuel.LengthBeats);
            var steps = BossDuel.Steps();
            Assert.LessOrEqual(steps.Max(s => s.OffsetBeats), BossDuel.LengthBeats, "74박 안에 끝남");
            foreach (var s in steps)
                Assert.GreaterOrEqual(s.OffsetBeats - GameConfig.ApproachBeats, BossDuel.PhaseStart(s.Phase), $"{s.OffsetBeats}: 원은 자기 단계가 시작된 뒤 나타남 (되감으면 다시 보임)");
            Assert.AreEqual(10.0, BossDuel.Origin(9.96), 1e-9, "O: 0.05박 안에서 막 지난 박은 그 박");
            Assert.AreEqual(11.0, BossDuel.Origin(10.2), 1e-9, "O: 도착 후 가장 가까운 박");
        }

        [Test]
        public void B26_NoRingDuringCutsABC()
        {
            // 원은 목표 2박 전에 나타나고 판정 창(+180 ms ≈ 0.36박)까지 남는다. 컷 A·B·C 구간과 겹치면 안 된다
            double tail = GameConfig.NormalWindowMs / 1000.0 / GameConfig.SecondsPerBeat;
            foreach (var cut in BossDuel.Cuts.Where(c => c.name != "D"))
                foreach (var s in BossDuel.Steps())
                {
                    double from = s.OffsetBeats - GameConfig.ApproachBeats, to = s.OffsetBeats + tail;
                    Assert.IsFalse(from < cut.to - 1e-9 && to > cut.from + 1e-9, $"컷 {cut.name} {cut.from}–{cut.to}에 {s.Kind} {s.OffsetBeats} 원");
                }
            Assert.AreEqual("A", BossDuel.CutAt(1)); Assert.AreEqual("B", BossDuel.CutAt(25)); Assert.AreEqual("B", BossDuel.CutAt(27)); Assert.IsNull(BossDuel.CutAt(55), "beta1-1: 컷 C는 옆차기 다운 구간(B13.4)으로 바뀜");
            Assert.AreEqual("D", BossDuel.CutAt(72)); Assert.IsNull(BossDuel.CutAt(10));
            Assert.Greater(BossDuel.Cuts.Single(c => c.name == "D").from, BossDuel.Steps().Last().OffsetBeats - 1e-9, "컷 D는 마무리 판정 뒤");
        }

        [Test]
        public void B26_AttackKeys_FollowGdd45_Flurry_Alternates()
        {
            foreach (var s in BossDuel.Steps().Where(s => s.Kind == CombatStepKind.Attack))
                Assert.IsTrue(CombatRules.KeyFits(s.Attack, s.Key), $"{s.OffsetBeats} {s.Attack} {s.Key} (GDD 4.5 표)");
            var p3 = BossDuel.Steps().Where(s => s.Phase == BossDuel.PhaseFlurry).ToList();
            CollectionAssert.AreEqual(new[] { ArrowKey.Left, ArrowKey.Right, ArrowKey.Up, ArrowKey.Down, ArrowKey.Left, ArrowKey.Right, ArrowKey.Up, ArrowKey.Down }, p3.Select(s => s.Key));
            for (int i = 0; i < p3.Count; i++)
            {
                Assert.AreEqual(60 + i, p3[i].OffsetBeats, 1e-9, "1박마다");
                Assert.AreEqual(i % 2 == 0 ? CombatStepKind.Attack : CombatStepKind.Strike, p3[i].Kind, "A와 S가 번갈아");
            }
            Assert.AreEqual(ArrowKey.Up, BossDuel.Steps().Single(s => s.Kind == CombatStepKind.Lock).Key, "칼 겨루기 ↑");
            Assert.AreEqual(ArrowKey.Up, BossDuel.Steps().Last().Key, "마무리 ↑");
            // 받아치기: S37 바로 반 박 뒤 A37.5
            var st = BossDuel.Steps();
            int k = st.FindIndex(s => s.OffsetBeats == 37);
            Assert.AreEqual(CombatStepKind.Strike, st[k].Kind);
            Assert.AreEqual(CombatStepKind.Attack, st[k + 1].Kind);
            Assert.AreEqual(0.5, st[k + 1].OffsetBeats - st[k].OffsetBeats, 1e-9);
        }

        [Test]
        public void B26_Posture_PerfectFullAtEnd_NormalHalf()
        {
            Assert.AreEqual(100f, GameConfig.PostureFull);
            int n = BossDuel.PostureSteps;
            Assert.AreEqual(100f / n, BossDuel.PostureGain(Judgement.Perfect), 1e-5f, "PERFECT +100/N");
            Assert.AreEqual(50f / n, BossDuel.PostureGain(Judgement.Normal), 1e-5f, "NORMAL +50/N");
            Assert.AreEqual(0f, BossDuel.PostureGain(Judgement.Fail));
            float sum = 0; for (int i = 0; i < n; i++) sum += BossDuel.PostureGain(Judgement.Perfect);
            Assert.AreEqual(100f, sum, 1e-3f, "모두 PERFECT면 3단계 끝에 정확히 가득");
        }

        [Test]
        public void B26_ChainSlots_Rule_And_StayInsideSafeEllipse()
        {
            Assert.AreEqual(3, RingLayout.ChainSlots(new double[] { 58, 59, 60, 61, 62, 63, 64, 65 }), "1박 간격 8개 → 2/1+1 = 3자리");
            Assert.AreEqual(5, RingLayout.ChainSlots(new double[] { 44, 44.5, 46, 46.5, 48, 49, 50 }), "반 박 간격 → 2/0.5+1 = 5자리");
            Assert.AreEqual(2, RingLayout.ChainSlots(new double[] { 10, 11 }), "원 수보다 많지 않음");
            Assert.AreEqual(5, RingLayout.ChainSlots(Enumerable.Range(0, 12).Select(i => i * 0.25).ToList()), "최대 5");
            var radii = RingLayout.SafeRadii(new Vector2(1920, 1080));
            foreach (var t in new[] { new double[] { 58, 59, 60, 61, 62, 63, 64, 65 }, new double[] { 44, 44.5, 46, 46.5, 48, 49, 50 } })
            {
                int slots = RingLayout.ChainSlots(t);
                foreach (var ax in new[] { -radii.x, 0f, radii.x })
                    for (int i = 0; i < t.Length; i++)
                    {
                        var p = RingLayout.ChainSlot(new Vector2(ax, 0), i % slots, slots, radii);
                        Assert.IsTrue(RingLayout.Inside(p, radii), $"묶음 {t.Length}개 {i}번 원 {p}: 안전 타원 안");
                    }
                // 같은 자리를 다시 쓰는 원은 앞 원이 판정(목표 + 판정 창)된 뒤에 나타난다
                for (int i = slots; i < t.Length; i++)
                    Assert.Greater(t[i] - GameConfig.ApproachBeats, t[i - slots] + GameConfig.NormalWindowMs / 1000.0 / GameConfig.SecondsPerBeat - 1e-9,
                        $"{i}번 원은 {i - slots}번 원이 끝난 뒤 그 자리에");
            }
        }

        [Test]
        public void B26_Chart_Totals_115To130()
        {
            var c = StageChart.Load();
            Assert.AreEqual(45, ChartCheck.CombatJudgements(c) - ChartCheck.CombatJudgements(new ChartData { events = c.events.Where(e => !e.IsBoss).ToArray() }), "무사 결투 45 (B13.4)");
            Assert.That(ChartCheck.TotalJudgements(c), Is.InRange(115, 130), "B11.6 스테이지 전체 115~130");
        }
    }
}
