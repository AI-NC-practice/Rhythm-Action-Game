using System.Linq;
using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>아슬아슬 회피 슬로모션 (GDD-beta B17.4): 값, 자리 4곳, PERFECT 뒤 슬로모션 없앰.</summary>
    public class B47_BulletTimeTests
    {
        [Test]
        public void B47_Config_MatchesGddBetaB17_4_Second()
        {
            // GDD-beta B17.4 2차 (B52): 피한 직후 0.3배, 실제 0.6초, 0.15초에 복귀, 다음 판정 0.6박 전에는 복귀 시작
            Assert.AreEqual(0.3f, GameConfig.BulletTime.Factor, 1e-6f, "0.3배");
            Assert.AreEqual(0.06f, GameConfig.BulletTime.InSeconds, 1e-6f);
            Assert.AreEqual(0.6f, GameConfig.BulletTime.HoldSeconds, 1e-6f, "실제 0.6초");
            Assert.AreEqual(0.15f, GameConfig.BulletTime.ReleaseSeconds, 1e-6f, "0.15초에 복귀");
            Assert.AreEqual(0.6f, GameConfig.BulletTime.ClearBeats, 1e-6f, "다음 판정 0.6박 전");
            Assert.AreEqual(-70f, GameConfig.BulletTime.Saturation, 1e-6f, "회색 화면");
            Assert.Less(GameConfig.BulletTime.LowPassMuffledHz, 1500f, "물속처럼 먹먹하게");
            Assert.Less(GameConfig.BulletTime.MusicGain, 1f, "작게");
            Assert.IsFalse(GameConfig.BulletTime.PerfectCandidateSlowMo, "B4 2·3번 PERFECT 뒤 슬로모션 없앰");
            Assert.Less(GameConfig.Fx.NearMiss.Height, 0f, "칼 회피: 몸을 숙임");
            Assert.Less(GameConfig.Fx.NearMiss.Pitch, 0f, "위를 올려다봄");
        }

        [Test]
        public void B47_Spots_Gate_SurroundLastSimultaneous_BossCounterAndLastFlurry()
        {
            // 5챕터 닫히는 대문
            Assert.AreEqual(0, EventCatalog.Get("closing_gate").BulletSlot);
            var chart = StageChart.Load();
            var gates = chart.events.Where(e => e.IsRunEvent && e.EventKind == "closing_gate").ToList();
            Assert.AreEqual(1, gates.Count);
            Assert.AreEqual(5, MapLayout.SectionOf(gates[0].runBeat).Number, "5챕터 궁궐 담");
            Assert.AreEqual(1, chart.events.Count(e => e.IsRunEvent && EventCatalog.Get(e.EventKind).BulletSlot >= 0), "사건은 대문 하나");

            // 포위 2단계 동시 공격 3번 중 마지막
            var sur = CombatPatterns.SurroundSteps();
            var bs = sur.Where(s => s.BulletTime).ToList();
            Assert.AreEqual(1, bs.Count);
            var simul = sur.Where(s => s.Phase == 2).ToList();
            Assert.AreSame(simul.Last(), bs[0], "동시 공격 마지막");
            Assert.Greater(bs[0].Attackers.Length, 1, "여러 명이 함께 베어 옴");

            // 무사: 받아치기 A37.5, 3단계 난타 마지막 무사 공격 A66
            var boss = BossDuel.Steps().Where(s => s.BulletTime).ToList();
            CollectionAssert.AreEqual(new[] { 37.5, 66.0 }, boss.Select(s => s.OffsetBeats).ToArray());
            Assert.IsTrue(boss.All(s => s.Kind == CombatStepKind.Attack), "무사 공격을 피함");
            Assert.AreEqual(BossDuel.PhaseClash, boss[0].Phase);
            Assert.AreEqual(BossDuel.PhaseFlurry, boss[1].Phase);
            Assert.IsFalse(BossDuel.Steps().Any(s => s.Kind == CombatStepKind.Attack && s.Phase == BossDuel.PhaseFlurry && s.OffsetBeats > 66), "난타 마지막 무사 공격");

            // 일반 적에는 없음
            foreach (var e in chart.Enemies) Assert.IsFalse(CombatRules.NormalEnemySteps(e).Any(s => s.BulletTime));
        }

        [Test]
        public void B47_IsBulletCue_ByTag()
        {
            Assert.IsTrue(BulletTimeFx.IsBulletCue(new JudgeCue { Tag = new CombatStep { BulletTime = true } }));
            Assert.IsFalse(BulletTimeFx.IsBulletCue(new JudgeCue { Tag = new CombatStep() }));
            Assert.IsFalse(BulletTimeFx.IsBulletCue(new JudgeCue { Tag = new RhythmNote() }), "장단 노드는 아님");
            var run = new StageDirector.ObstacleRun { Def = EventCatalog.Get("closing_gate") };
            var cue = new JudgeCue { Tag = run };
            run.Cues.Add(cue);
            Assert.IsTrue(BulletTimeFx.IsBulletCue(cue), "대문의 ↓ 판정");
            var jars = new StageDirector.ObstacleRun { Def = EventCatalog.Get("jars") };
            var c2 = new JudgeCue { Tag = jars };
            jars.Cues.Add(c2);
            Assert.IsFalse(BulletTimeFx.IsBulletCue(c2));
        }
    }
}
