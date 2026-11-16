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
        public const int PhaseDown = 4;      // 옆차기 다운 구간 (GDD-beta B13.4)
        public const int PhaseFlurry = 5;    // 3단계 난타
        public const int PhaseFinish = 6;    // 마무리

        /// <summary>받아치기(자리 바꾸기) 판정 박, 옆차기(다운) 직전 판정 박, 일어나는 박 (B13.4).</summary>
        public const double SwapBeat = 37.5;
        public const double KickAfterBeat = 52;
        public const double StandUpFrom = 58.4, StandUpTo = 59;

        /// <summary>기준점 O: 도착 후 가장 가까운 박 (프레임 오차 0.05박 안에서 이미 지난 박은 그 박으로 본다).</summary>
        public static double Origin(double now) => System.Math.Ceiling(now - 0.05);

        /// <summary>결투 전체 길이 (박).</summary>
        public const double LengthBeats = 74;

        /// <summary>단계 시작 박 (FAIL이면 여기로 되감는다, B11.4).</summary>
        public static double PhaseStart(int phase)
        {
            switch (phase)
            {
                case PhaseExplore: return 4;
                case PhaseLock: return 24;
                case PhaseClash: return 30;
                case PhaseDown: return 54;
                case PhaseFlurry: return 58;
                default: return 68;
            }
        }

        /// <summary>3인칭 컷 구간 (판정 원 없음, B11.1·B11.5). D는 마무리 판정 뒤.</summary>
        public static readonly (string name, double from, double to)[] Cuts =
        {
            ("A", 0, 4), ("B", 24, 28), ("D", 70, 74),   // 컷 C(흔들림)는 B13.4 옆차기 다운으로 바뀜
        };

        // A: 무사 공격(막기·피하기), S: 내 공격, L: 칼 겨루기, F: 마무리
        static readonly (double t, char k, ArrowKey key)[] Table =
        {
            // 1단계 탐색
            (6, 'A', ArrowKey.Left), (8, 'A', ArrowKey.Down), (10, 'S', ArrowKey.Right), (11, 'S', ArrowKey.Left),
            (13, 'A', ArrowKey.Up), (14, 'A', ArrowKey.Right), (15, 'A', ArrowKey.Down), (17, 'S', ArrowKey.Up), (18, 'S', ArrowKey.Down),
            (20, 'A', ArrowKey.Right), (21, 'A', ArrowKey.Left), (22, 'S', ArrowKey.Right), (23, 'S', ArrowKey.Up),
            // 칼 겨루기
            (30, 'L', ArrowKey.Up),
            // 2단계 격돌
            (32, 'A', ArrowKey.Down), (32.5, 'A', ArrowKey.Left), (34, 'S', ArrowKey.Right), (35, 'S', ArrowKey.Up),
            (37, 'S', ArrowKey.Left), (37.5, 'A', ArrowKey.Right),                                   // 받아치기
            (39, 'A', ArrowKey.Right), (39.5, 'A', ArrowKey.Down), (40, 'A', ArrowKey.Up),
            (42, 'S', ArrowKey.Left), (43, 'S', ArrowKey.Right), (44, 'S', ArrowKey.Down),
            (46, 'A', ArrowKey.Left), (46.5, 'A', ArrowKey.Right), (48, 'S', ArrowKey.Up), (48.5, 'S', ArrowKey.Down),
            (50, 'A', ArrowKey.Down), (51, 'S', ArrowKey.Left), (52, 'A', ArrowKey.Up),
            // 옆차기 다운 구간 (B13.4): 누운 채 ← → 구르기, ↑ 칼로 받기
            (56, 'A', ArrowKey.Left), (57, 'A', ArrowKey.Right), (58, 'A', ArrowKey.Up),
            // 3단계 난타 (A와 S가 번갈아)
            (60, 'A', ArrowKey.Left), (61, 'S', ArrowKey.Right), (62, 'A', ArrowKey.Up), (63, 'S', ArrowKey.Down),
            (64, 'A', ArrowKey.Left), (65, 'S', ArrowKey.Right), (66, 'A', ArrowKey.Up), (67, 'S', ArrowKey.Down),
            // 마무리
            (70, 'F', ArrowKey.Up),
        };

        static int PhaseOf(double t) => t < 24 ? PhaseExplore : t <= 30 ? PhaseLock : t < 54 ? PhaseClash : t < 59 ? PhaseDown : t < 68 ? PhaseFlurry : PhaseFinish;

        /// <summary>무사 공격 종류 (GDD 4.5 표: ↓ 가로베기, ←/→ 내려베기, ↑ 올려베기).</summary>
        static string AttackFor(ArrowKey k) => k == ArrowKey.Down ? "horizontal" : k == ArrowKey.Up ? "upward" : "overhead";

        public static List<CombatStep> Steps()
        {
            var list = new List<CombatStep>();
            foreach (var (t, k, key) in Table)
            {
                var s = new CombatStep { OffsetBeats = t, Key = key, Phase = PhaseOf(t), Attackers = new[] { 0 }, BulletTime = System.Array.IndexOf(GameConfig.BulletTime.BossBeats, t) >= 0 };   // 회피 슬로모션 (B17.4)
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

        /// <summary>
        /// 3인칭에서 1인칭으로 돌아가기 시작해야 하는 여유 (박): 돌아오는 블렌드 0.4초 + 원이 나타나기 전 여유 0.4초 (B11.5).
        /// 속도가 빠르면 박이 짧아지므로 더 많은 박이 필요하다.
        /// </summary>
        public static double ReturnLeadBeats(float speed) =>
            (GameConfig.DuelCutBlendSeconds + GameConfig.DuelCutReturnMarginSeconds) * speed / GameConfig.SecondsPerBeat;

        /// <summary>
        /// 지금 3인칭이어야 하는 컷 (B11.5). A·B·C는 구간 시작부터 (끝 − 돌아오는 여유)까지만, 여유가 모자라면 컷을 건너뛴다.
        /// D는 마무리가 성공한 뒤(finished) 72박까지. progress는 그 컷 안에서 0→1 (도는 각도용).
        /// </summary>
        public static string ActiveCut(double rel, float speed, bool finished, out float progress)
        {
            progress = 0f;
            foreach (var c in Cuts)
            {
                if (c.name == "D")
                {
                    if (!finished || rel >= c.to) continue;
                    progress = (float)System.Math.Max(0.0, System.Math.Min(1.0, (rel - c.from) / (c.to - c.from)));
                    return c.name;
                }
                if (finished) continue;
                double end = c.to - ReturnLeadBeats(speed);
                if (end <= c.from || rel < c.from || rel >= end) continue;
                progress = (float)((rel - c.from) / (end - c.from));
                return c.name;
            }
            return null;
        }

        /// <summary>지금 박(O 기준)이 3인칭 컷 구간이면 그 이름, 아니면 null.</summary>
        public static string CutAt(double t)
        {
            foreach (var c in Cuts) if (t >= c.from && t < c.to) return c.name;
            return null;
        }
    }
}
