using System.Collections.Generic;
using System.Linq;

namespace RhythmActionGame
{
    /// <summary>
    /// 차트 검사 도구 (GDD-beta B3, B4). 달리며 처리하는 사건의 판정 목표(달리기 박자)와 전투 판정 수를 센다.
    /// 전투(대치·포위·무사)는 멈춰서 하므로 달리기 박자 목록에는 넣지 않고 개수만 센다.
    /// </summary>
    public static class ChartCheck
    {
        /// <summary>달리며 처리하는 사건의 판정 목표 (달리기 박자, 사건, 판정 번호).</summary>
        public struct Target
        {
            public double RunBeat;
            public ChartEvent Event;
            public int Slot;
        }

        public static List<Target> RunTargets(ChartData c)
        {
            var list = new List<Target>();
            foreach (var e in c.events.Where(e => e.IsRunEvent))
            {
                var slots = EventCatalog.SlotsFor(e);
                for (int i = 0; i < slots.Count; i++) list.Add(new Target { RunBeat = e.runBeat + slots[i].Offset, Event = e, Slot = i });
            }
            return list.OrderBy(t => t.RunBeat).ToList();
        }

        /// <summary>전투 판정 수 (일반 적: 공격 + 마무리, 포위: 3단계 전체, 무사: 공격 + 마무리).</summary>
        public static int CombatJudgements(ChartData c)
        {
            int n = 0;
            foreach (var e in c.events)
            {
                if (e.IsDuel) n += CombatRules.NormalEnemySteps(e).Count;
                else if (e.IsSurround) n += CombatPatterns.SurroundSteps().Count;
                else if (e.IsBoss) n += BossDuel.Steps().Count;   // 무사 결투 (GDD-beta B11.6)
            }
            return n;
        }

        public static int TotalJudgements(ChartData c) => RunTargets(c).Count + CombatJudgements(c);

        /// <summary>사건 길이 (달리기 박자). 사건이 아니면 0 (전투는 그 자리에서 멈춘다).</summary>
        public static int Length(ChartEvent e) => e.IsRunEvent ? EventCatalog.Get(e.EventKind).Beats : 0;

        /// <summary>
        /// 슬로모션 후보 판정(GDD-beta B4 2·3번)의 목표 박자. 사건 후보(EventDef.SlowMoSlot)만 센다
        /// (전투 후보는 멈춘 상태라 달리기 박자와 무관).
        /// </summary>
        public static List<Target> SlowMoCandidates(ChartData c) =>
            RunTargets(c).Where(t => EventCatalog.Get(t.Event.EventKind).SlowMoSlot == t.Slot).ToList();
    }
}
