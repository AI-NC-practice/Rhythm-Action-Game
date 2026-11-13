using System.Collections.Generic;

namespace RhythmActionGame
{
    /// <summary>전투 판정 단계 종류.</summary>
    public enum CombatStepKind
    {
        Attack,     // 적 공격 방어 (가로베기·내려베기·찌르기·올려베기)
        Finisher,   // 마무리 (↑)
        Windmill,   // 포위 전투 3단계 화살표 (T07)
        Strike,     // 내 공격 (무사 결투, GDD-beta B11.2)
        Lock,       // 칼 겨루기 밀어내기 (무사 결투, GDD-beta B11.1)
    }

    /// <summary>마무리 결과 연출 (GDD 4.5 표).</summary>
    public enum ExecutionKind
    {
        None,
        KickOffRoof,        // 지붕 위 PERFECT: 발차기로 떨어뜨리기
        DisarmSlash,        // 땅 위 PERFECT: 칼 뺏어 베기
        StabWithOwnSword,   // 땅 위 PERFECT: 적의 칼을 쥔 손째로 찌르기
        PushDown,           // NORMAL: 밀쳐 쓰러뜨리기
        Windmill,           // 포위 전투 마무리 (T07)
    }

    /// <summary>전투 판정 하나. OffsetBeats는 전투 시작 박자(첫 공격 목표)부터의 거리.</summary>
    public class CombatStep
    {
        public CombatStepKind Kind;
        public double OffsetBeats;
        public ArrowKey Key;
        public string Attack;           // horizontal | overhead | thrust | upward | finisher | windmill
        public int[] Attackers = { 0 }; // 공격하는 적 번호 (동시 공격은 여러 명)
        public int Phase;               // 포위 전투 단계 (1~3), 일반 적은 0
    }

    /// <summary>전투 규칙 (GDD 4.5). 순수 함수.</summary>
    public static class CombatRules
    {
        /// <summary>b 다음 마디의 첫 박 (b가 마디 첫 박이어도 다음 마디).</summary>
        public static double NextBar(double b) =>
            System.Math.Floor(b / GameConfig.BeatsPerBar + 1e-9) * GameConfig.BeatsPerBar + GameConfig.BeatsPerBar;

        /// <summary>전투(또는 재도전)의 첫 공격 목표 박자: 다음 마디 첫 박에 원이 나타나도록 그 2박 뒤.</summary>
        public static double FirstTargetBeat(double now) => NextBar(now) + GameConfig.ApproachBeats;

        /// <summary>
        /// 멈추는 전투의 첫 공격 목표 박자 (GDD-beta B10.2): 도착 후 가장 가까운 박 + 2박.
        /// 첫 원은 가장 가까운 박(0.5초 안)에 나타난다. 프레임 오차(0.05박)로 이미 지난 박은 그 박으로 본다.
        /// </summary>
        public static double EncounterStartBeat(double now) => System.Math.Ceiling(now - 0.05) + GameConfig.ApproachBeats;

        /// <summary>공격 종류에 맞는 키 (GDD 4.5 표). 내려베기·찌르기는 차트의 ←/→를 그대로 쓴다.</summary>
        public static bool KeyFits(string attack, ArrowKey key)
        {
            switch (attack)
            {
                case "horizontal": return key == ArrowKey.Down;
                case "overhead": case "thrust": return key == ArrowKey.Left || key == ArrowKey.Right;
                case "upward": case "finisher": return key == ArrowKey.Up;
                default: return true;
            }
        }

        /// <summary>일반 적: 공격 2회(2박 간격) + 마무리(↑) 1회.</summary>
        public static List<CombatStep> NormalEnemySteps(ChartEvent ev)
        {
            var steps = new List<CombatStep>();
            double t = 0;
            foreach (var a in ev.attacks)
            {
                steps.Add(new CombatStep { Kind = CombatStepKind.Attack, OffsetBeats = t, Key = a.Key, Attack = a.attack });
                t += GameConfig.NormalEnemyAttackIntervalBeats;
            }
            steps.Add(new CombatStep { Kind = CombatStepKind.Finisher, OffsetBeats = t, Key = ArrowKey.Up, Attack = "finisher" });
            return steps;
        }

        /// <summary>마무리 결과 → 연출. 지붕 위 PERFECT는 발차기, 땅 위 PERFECT는 칼 뺏기/찌르기를 번갈아.</summary>
        public static ExecutionKind ExecutionFor(Judgement r, bool onRoof, int enemyIndex)
        {
            if (r == Judgement.Fail) return ExecutionKind.None;
            if (r == Judgement.Normal) return ExecutionKind.PushDown;
            if (onRoof) return ExecutionKind.KickOffRoof;
            return enemyIndex % 2 == 0 ? ExecutionKind.DisarmSlash : ExecutionKind.StabWithOwnSword;
        }
    }
}
