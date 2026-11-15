using System.Collections.Generic;
using System.Linq;

namespace RhythmActionGame
{
    /// <summary>달리기 장단 노드 하나 (GDD-beta B18.2). 판정 원의 꼬리표로도 쓴다.</summary>
    public class RhythmNote
    {
        public double RunBeat;
        public ArrowKey Key;
    }

    /// <summary>
    /// 달리기 장단 노드를 만든다 (GDD-beta B18.2). 순수 함수. 챕터 장단 표(GameConfig.Jangdan)를 마디마다 돌려 놓고,
    /// 사건 판정과 0.5박 안, 멈추는 전투 자리 앞뒤, 슬로모션 후보 뒤 2박, 챕터 시작 2박 안, 무사 결투 뒤는 비운다.
    /// </summary>
    public static class RhythmNotes
    {
        public static List<RhythmNote> Generate(ChartData c)
        {
            if (c == null || !c.jangdan) return new List<RhythmNote>();
            var events = ChartCheck.RunTargets(c).Select(t => t.RunBeat).ToList();
            var slow = ChartCheck.SlowMoCandidates(c).Select(t => t.RunBeat).ToList();
            var stops = c.events.Where(e => !e.IsRunEvent && !e.IsForeshadow).Select(e => (double)e.runBeat).ToList();
            double boss = c.events.Where(e => e.type == "boss").Select(e => (double)e.runBeat).DefaultIfEmpty(c.runBeats).Min();
            var notes = new List<RhythmNote>();
            for (int bar = 0; bar * GameConfig.BeatsPerBar < c.runBeats; bar++)
            {
                double bs = bar * GameConfig.BeatsPerBar;
                var sec = MapLayout.SectionOf((int)bs);
                var table = GameConfig.Jangdan.Bars[System.Math.Max(0, System.Math.Min(GameConfig.Jangdan.Bars.Length - 1, sec.Number - 1))];
                int barInSec = (int)((bs - sec.StartBeat) / GameConfig.BeatsPerBar);
                foreach (var cell in table[((barInSec % table.Length) + table.Length) % table.Length].Split(' '))
                {
                    double r = bs + double.Parse(cell.Substring(0, cell.Length - 1), System.Globalization.CultureInfo.InvariantCulture);
                    var key = cell[cell.Length - 1] == 'L' ? ArrowKey.Kung : ArrowKey.Deok;   // L = Q 궁편, R = W 채편 (B18.5)
                    var s2 = MapLayout.SectionOf((int)System.Math.Floor(r));
                    if (r < s2.StartBeat + GameConfig.Jangdan.ChapterLeadBeats) continue;
                    if (r >= boss - GameConfig.Jangdan.CombatClearBeats) continue;
                    if (events.Any(e => System.Math.Abs(e - r) < GameConfig.Jangdan.EventClearBeats - 1e-9)) continue;   // 사건 판정과 1박 (B18.5)
                    if (stops.Any(e => r > e - GameConfig.Jangdan.CombatClearBeats - 1e-9 && r < e + GameConfig.Jangdan.CombatClearBeats - 1e-9)) continue;
                    if (slow.Any(e => r > e - 1e-9 && r < e + GameConfig.Jangdan.SlowMoClearBeats + 1e-9)) continue;
                    if (notes.Count > 0 && r - notes[notes.Count - 1].RunBeat < GameConfig.Jangdan.MinGapBeats - 1e-9) continue;
                    notes.Add(new RhythmNote { RunBeat = r, Key = key });
                }
            }
            return notes;
        }
    }
}
