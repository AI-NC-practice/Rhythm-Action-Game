using System.Collections.Generic;

namespace RhythmActionGame
{
    /// <summary>포위 전투와 무사 전투의 판정 순서 (GDD 4.5, 4.6). 순수 함수.</summary>
    public static class CombatPatterns
    {
        // 1단계 순차 공격: 적 0~4가 1박 간격. 공격 종류와 키 (GDD에 없음, Claude가 정함: 키 4개가 고루 나오게)
        static readonly (string attack, ArrowKey key)[] Sequential =
        {
            ("horizontal", ArrowKey.Down),
            ("overhead", ArrowKey.Left),
            ("upward", ArrowKey.Up),
            ("thrust", ArrowKey.Right),
            ("horizontal", ArrowKey.Down),
        };

        // 2단계 동시 공격 3번: 공격하는 적들, 키 (GDD에 없음, Claude가 정함)
        static readonly (int[] who, string attack, ArrowKey key)[] Simultaneous =
        {
            (new[] { 0, 1 }, "horizontal", ArrowKey.Down),
            (new[] { 2, 3, 4 }, "upward", ArrowKey.Up),
            (new[] { 1, 3 }, "horizontal", ArrowKey.Down),
        };

        // 3단계 윈드밀 화살표 4개 (GDD 4.6 예: ← ↑ → ↓)
        public static readonly ArrowKey[] WindmillKeys = { ArrowKey.Left, ArrowKey.Up, ArrowKey.Right, ArrowKey.Down };

        /// <summary>단계 사이 간격 (박자). GDD에 없음, Claude가 정함.</summary>
        public const double PhaseGapBeats = 2.0;
        /// <summary>2단계 동시 공격 사이 간격 (박자). GDD에 없음, Claude가 정함.</summary>
        public const double SimultaneousIntervalBeats = 2.0;

        /// <summary>포위 전투 1·2·3단계 판정 전체. OffsetBeats는 첫 공격 기준.</summary>
        public static List<CombatStep> SurroundSteps()
        {
            var steps = new List<CombatStep>();
            double t = 0;
            for (int k = 0; k < GameConfig.SurroundEnemyCount; k++)
            {
                var s = Sequential[k % Sequential.Length];
                steps.Add(new CombatStep { Kind = CombatStepKind.Attack, Phase = 1, OffsetBeats = t, Key = s.key, Attack = s.attack, Attackers = new[] { k } });
                t += GameConfig.SurroundSequentialIntervalBeats;
            }
            t += PhaseGapBeats - GameConfig.SurroundSequentialIntervalBeats;
            for (int i = 0; i < GameConfig.SurroundSimultaneousCount; i++)
            {
                var s = Simultaneous[i % Simultaneous.Length];
                steps.Add(new CombatStep { Kind = CombatStepKind.Attack, Phase = 2, OffsetBeats = t, Key = s.key, Attack = s.attack, Attackers = s.who });
                t += SimultaneousIntervalBeats;
            }
            t += PhaseGapBeats - SimultaneousIntervalBeats;
            steps.AddRange(WindmillSteps(t));
            return steps;
        }

        /// <summary>3단계 윈드밀 화살표 4개 (반 박자 간격).</summary>
        public static List<CombatStep> WindmillSteps(double start)
        {
            var steps = new List<CombatStep>();
            for (int i = 0; i < GameConfig.WindmillArrowCount; i++)
                steps.Add(new CombatStep
                {
                    Kind = CombatStepKind.Windmill,
                    Phase = 3,
                    OffsetBeats = start + i * GameConfig.WindmillIntervalBeats,
                    Key = WindmillKeys[i % WindmillKeys.Length],
                    Attack = "windmill",
                    Attackers = new[] { 0, 1, 2, 3, 4 },
                });
            return steps;
        }

        /// <summary>무사: 차트의 공격 5회 (간격 gap, 1~2박) + 마무리(↑, 2박 뒤).</summary>
        public static List<CombatStep> BossSteps(ChartEvent ev)
        {
            var steps = new List<CombatStep>();
            double t = 0;
            for (int i = 0; i < ev.attacks.Length; i++)
            {
                var a = ev.attacks[i];
                if (i > 0) t += a.gap > 0 ? a.gap : GameConfig.BossAttackIntervalMaxBeats;
                steps.Add(new CombatStep { Kind = CombatStepKind.Attack, OffsetBeats = t, Key = a.Key, Attack = a.attack });
            }
            t += GameConfig.BossAttackIntervalMaxBeats;
            steps.Add(new CombatStep { Kind = CombatStepKind.Finisher, OffsetBeats = t, Key = ArrowKey.Up, Attack = "finisher" });
            return steps;
        }
    }
}
