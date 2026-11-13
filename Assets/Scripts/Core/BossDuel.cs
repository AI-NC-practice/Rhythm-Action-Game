using System.Collections.Generic;

namespace RhythmActionGame
{
    /// <summary>
    /// 무사 결투 패턴 (GDD-beta B11). 순수 데이터와 함수.
    /// 박은 기준점 O(도착 후 가장 가까운 박)부터 센다. CombatStep.Phase는 결투 단계 번호다.
    /// </summary>
    public static class BossDuel
    {
        public const int PhaseExplore = 1;   // 1단계 탐색
        public const int PhaseLock = 2;      // 칼 겨루기
        public const int PhaseClash = 3;     // 2단계 격돌
        public const int PhaseFlurry = 4;    // 3단계 난타
        public const int PhaseFinish = 5;    // 마무리

        /// <summary>기준점 O: 도착 후 가장 가까운 박 (프레임 오차 0.05박 안에서 이미 지난 박은 그 박으로 본다).</summary>
        public static double Origin(double now) => System.Math.Ceiling(now - 0.05);

        /// <summary>결투 전체 길이 (박).</summary>
        public const double LengthBeats = 72;

        /// <summary>단계 시작 박 (FAIL이면 여기로 되감는다, B11.4).</summary>
        public static double PhaseStart(int phase)
        {
            switch (phase)
            {
                case PhaseExplore: return 4;
                case PhaseLock: return 24;
                case PhaseClash: return 28;
                case PhaseFlurry: return 56;
                default: return 66;
            }
        }

        /// <summary>3인칭 컷 구간 (판정 원 없음, B11.1·B11.5). D는 마무리 판정 뒤.</summary>
        public static readonly (string name, double from, double to)[] Cuts =
        {
            ("A", 0, 4), ("B", 24, 26), ("C", 52, 56), ("D", 68, 72),
        };

        // A: 무사 공격(막기·피하기), S: 내 공격, L: 칼 겨루기, F: 마무리
        static readonly (double t, char k, ArrowKey key)[] Table =
        {
            // 1단계 탐색
            (6, 'A', ArrowKey.Left), (8, 'A', ArrowKey.Down), (10, 'S', ArrowKey.Right), (11, 'S', ArrowKey.Left),
            (13, 'A', ArrowKey.Up), (14, 'A', ArrowKey.Right), (15, 'A', ArrowKey.Down), (17, 'S', ArrowKey.Up), (18, 'S', ArrowKey.Down),
            (20, 'A', ArrowKey.Right), (21, 'A', ArrowKey.Left), (22, 'S', ArrowKey.Right), (23, 'S', ArrowKey.Up),
            // 칼 겨루기
            (28, 'L', ArrowKey.Up),
            // 2단계 격돌
            (30, 'A', ArrowKey.Down), (30.5, 'A', ArrowKey.Left), (32, 'S', ArrowKey.Right), (33, 'S', ArrowKey.Up),
            (35, 'S', ArrowKey.Left), (35.5, 'A', ArrowKey.Right),                                   // 받아치기
            (37, 'A', ArrowKey.Right), (37.5, 'A', ArrowKey.Down), (38, 'A', ArrowKey.Up),
            (40, 'S', ArrowKey.Left), (41, 'S', ArrowKey.Right), (42, 'S', ArrowKey.Down),
            (44, 'A', ArrowKey.Left), (44.5, 'A', ArrowKey.Right), (46, 'S', ArrowKey.Up), (46.5, 'S', ArrowKey.Down),
            (48, 'A', ArrowKey.Down), (49, 'S', ArrowKey.Left), (50, 'A', ArrowKey.Up),
            // 3단계 난타 (A와 S가 번갈아)
            (58, 'A', ArrowKey.Left), (59, 'S', ArrowKey.Right), (60, 'A', ArrowKey.Up), (61, 'S', ArrowKey.Down),
            (62, 'A', ArrowKey.Left), (63, 'S', ArrowKey.Right), (64, 'A', ArrowKey.Up), (65, 'S', ArrowKey.Down),
            // 마무리
            (68, 'F', ArrowKey.Up),
        };

        static int PhaseOf(double t) => t < 24 ? PhaseExplore : t <= 28 ? PhaseLock : t < 56 ? PhaseClash : t < 66 ? PhaseFlurry : PhaseFinish;

        /// <summary>무사 공격 종류 (GDD 4.5 표: ↓ 가로베기, ←/→ 내려베기, ↑ 올려베기).</summary>
        static string AttackFor(ArrowKey k) => k == ArrowKey.Down ? "horizontal" : k == ArrowKey.Up ? "upward" : "overhead";

        public static List<CombatStep> Steps()
        {
            var list = new List<CombatStep>();
            foreach (var (t, k, key) in Table)
            {
                var s = new CombatStep { OffsetBeats = t, Key = key, Phase = PhaseOf(t), Attackers = new[] { 0 } };
                switch (k)
                {
                    case 'A': s.Kind = CombatStepKind.Attack; s.Attack = AttackFor(key); break;
                    case 'S': s.Kind = CombatStepKind.Strike; s.Attack = "strike"; break;
                    case 'L': s.Kind = CombatStepKind.Lock; s.Attack = "lock"; break;
                    default: s.Kind = CombatStepKind.Finisher; s.Attack = "finisher"; break;
                }
                list.Add(s);
            }
            return list;
        }

        /// <summary>마무리 전 판정 수 N (자세 게이지 계산용).</summary>
        public static int PostureSteps => Table.Length - 1;

        /// <summary>판정 하나가 채우는 자세 게이지 (B11.3): PERFECT 100/N, NORMAL 50/N, FAIL 0.</summary>
        public static float PostureGain(Judgement r) =>
            r == Judgement.Perfect ? GameConfig.PostureFull / PostureSteps
            : r == Judgement.Normal ? GameConfig.PostureFull * GameConfig.PostureNormalShare / PostureSteps : 0f;

        /// <summary>지금 박(O 기준)이 3인칭 컷 구간이면 그 이름, 아니면 null.</summary>
        public static string CutAt(double t)
        {
            foreach (var c in Cuts) if (t >= c.from && t < c.to) return c.name;
            return null;
        }
    }
}
