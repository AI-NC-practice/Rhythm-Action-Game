using System.Collections.Generic;

namespace RhythmActionGame
{
    /// <summary>사건 안의 판정 하나 (GDD-beta B2.1). Offset은 사건 시작 기준 목표 박자.</summary>
    public struct EventSlot
    {
        public double Offset;
        /// <summary>정해진 키. null이면 차트의 keys[i] (없으면 key)를 쓴다.</summary>
        public ArrowKey? Key;
        public EventSlot(double offset, ArrowKey? key = null) { Offset = offset; Key = key; }
    }

    /// <summary>사건 종류 하나의 규칙: 고정 길이, 판정 목록, 체력 제한, 슬로모션 후보 판정.</summary>
    public class EventDef
    {
        public string Kind;
        public int Beats;
        public EventSlot[] Slots;
        /// <summary>한 사건에서 체력을 최대 1만 잃는다 (rolling_jars, arrow_volley).</summary>
        public bool HpCapOne;
        /// <summary>GDD 4.4의 기존 장애물이면 그 분류 (카메라 궤적). 새 사건·적은 null.</summary>
        public ObstacleCategory? Category;
        /// <summary>PERFECT일 때 슬로모션 후보인 판정 번호 (GDD-beta B4의 2·3번). 없으면 −1.</summary>
        public int SlowMoSlot = -1;
        /// <summary>안 멈추는 적(돌진·매복·추격)인지.</summary>
        public bool IsEnemy;
    }

    /// <summary>
    /// 사건 목록 (GDD-beta B2). 기존 장애물 6종(GDD 4.4)은 4박·판정 1개(사건 시작 박자),
    /// 새 사건 8종(B2.2)과 안 멈추는 적(B2.3)은 표의 길이·판정 박자를 따른다.
    /// </summary>
    public static class EventCatalog
    {
        public const string Charge = "charge";
        public const string Ambush = "ambush";
        public const string Chase = "chase";
        public const string Duel = "duel";

        static readonly Dictionary<string, EventDef> defs = Build();

        static Dictionary<string, EventDef> Build()
        {
            var d = new Dictionary<string, EventDef>();
            void Add(EventDef e) => d[e.Kind] = e;
            EventDef Legacy(string kind) => new EventDef
            {
                Kind = kind, Beats = GameConfig.ObstacleBeats, Slots = new[] { new EventSlot(0) }, Category = Traversal.Categorize(kind),
            };
            foreach (var k in new[] { "cart", "low_wall", "roof_climb", "roof_jump", "gate_climb", "clothesline", "eave", "drop_down", "jars", "wall_run" })
                Add(Legacy(k));

            // GDD-beta B2.2 새 사건 8종
            // GDD-beta B16.1: 미끄러짐 ↓ 뒤 엇박(+3.5)에 버팀 ↑
            Add(new EventDef { Kind = "tile_slip", Beats = 6, Slots = new[] { new EventSlot(2, ArrowKey.Down), new EventSlot(3.5, ArrowKey.Up) }, SlowMoSlot = 1 });
            Add(new EventDef { Kind = "rope_swing", Beats = 4, Slots = new[] { new EventSlot(2, ArrowKey.Up) }, SlowMoSlot = 0 });
            // GDD-beta B16.1: 쿵-따닥 당김 (+1, +1.5, +2.5)
            Add(new EventDef { Kind = "rolling_jars", Beats = 4, Slots = new[] { new EventSlot(1), new EventSlot(1.5), new EventSlot(2.5) }, HpCapOne = true });
            Add(new EventDef { Kind = "awning_bounce", Beats = 4, Slots = new[] { new EventSlot(2, ArrowKey.Up) } });
            Add(new EventDef { Kind = "closing_gate", Beats = 4, Slots = new[] { new EventSlot(2, ArrowKey.Down) }, SlowMoSlot = 0 });
            Add(new EventDef { Kind = "arrow_volley", Beats = 4, Slots = new[] { new EventSlot(1.5), new EventSlot(2), new EventSlot(2.5), new EventSlot(3) }, HpCapOne = true });
            Add(new EventDef { Kind = "long_jump", Beats = 6, Slots = new[] { new EventSlot(2, ArrowKey.Up), new EventSlot(4, ArrowKey.Down) }, SlowMoSlot = 0 });
            Add(new EventDef { Kind = "banner_slide", Beats = 4, Slots = new[] { new EventSlot(2, ArrowKey.Down) } });

            // GDD-beta B2.3 안 멈추는 적 (kind = 등장 방식)
            Add(new EventDef { Kind = Charge, Beats = 4, Slots = new[] { new EventSlot(2, ArrowKey.Up) }, SlowMoSlot = 0, IsEnemy = true });
            Add(new EventDef { Kind = Ambush, Beats = 4, Slots = new[] { new EventSlot(2), new EventSlot(2.5) }, IsEnemy = true });   // GDD-beta B16.1: 따닥
            Add(new EventDef { Kind = Chase, Beats = 4, Slots = new[] { new EventSlot(2) }, IsEnemy = true });
            return d;
        }

        public static bool Has(string kind) => kind != null && defs.ContainsKey(kind);

        public static EventDef Get(string kind)
        {
            if (!Has(kind)) throw new System.ArgumentException("알 수 없는 사건: " + kind);
            return defs[kind];
        }

        public static IEnumerable<EventDef> All => defs.Values;

        /// <summary>
        /// 차트 이벤트 하나의 실제 판정 목록. 매복은 차트 keys 개수(1~2)만큼만 쓴다.
        /// 키: 정해진 키 → 차트 keys[i] → 차트 key 순서.
        /// </summary>
        public static List<EventSlot> SlotsFor(ChartEvent ev)
        {
            var def = Get(ev.EventKind);
            var list = new List<EventSlot>();
            int count = def.Slots.Length;
            if (ev.EventKind == Ambush) count = System.Math.Max(1, System.Math.Min(count, ev.keys != null && ev.keys.Length > 0 ? ev.keys.Length : 1));
            for (int i = 0; i < count; i++)
            {
                var s = def.Slots[i];
                ArrowKey key = s.Key ?? (ev.keys != null && i < ev.keys.Length ? StageChart.ParseKey(ev.keys[i]) : ev.Key);
                list.Add(new EventSlot(s.Offset, key));
            }
            return list;
        }
    }
}
