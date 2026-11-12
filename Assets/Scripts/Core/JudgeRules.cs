namespace RhythmActionGame
{
    /// <summary>방향키 (GDD 3장).</summary>
    public enum ArrowKey
    {
        Up,
        Down,
        Left,
        Right,
    }

    /// <summary>판정 규칙 (GDD 4.2). 시간은 모두 초, 판정 창은 실제 시간(ms)이라 속도 배율과 무관하다.</summary>
    public static class JudgeRules
    {
        /// <summary>입력 시각과 목표 시각의 차이 (ms). 음수면 이른 입력. 타이밍 보정 값을 목표 시점에 더한다.</summary>
        public static double DeltaMs(double inputTime, double targetTime, float offsetMs) =>
            (inputTime - (targetTime + offsetMs / 1000.0)) * 1000.0;

        /// <summary>
        /// 입력 하나를 판정한다. 반환값이 false면 무시된 입력(목표 −300 ms보다 이르거나, 목표 +150 ms보다 늦음 — 늦은 쪽은 이미 FAIL 처리됨).
        /// </summary>
        public static bool TryJudge(double inputTime, double targetTime, float offsetMs, ArrowKey pressed, ArrowKey expected, out Judgement result)
        {
            result = Judgement.Fail;
            double d = DeltaMs(inputTime, targetTime, offsetMs);
            if (d < -GameConfig.EarlyIgnoreMs) return false;      // 너무 이름: 무시
            if (d > GameConfig.NormalWindowMs) return false;       // 창이 지남: 무입력 FAIL로 처리됨
            if (pressed != expected) { result = Judgement.Fail; return true; }
            double a = System.Math.Abs(d);
            if (a <= GameConfig.PerfectWindowMs) result = Judgement.Perfect;
            else if (a <= GameConfig.NormalWindowMs) result = Judgement.Normal;
            else result = Judgement.Fail;                          // −300 ~ −150 ms
            return true;
        }

        /// <summary>목표 +150 ms까지 입력이 없으면 FAIL.</summary>
        public static bool IsMissed(double now, double targetTime, float offsetMs) =>
            DeltaMs(now, targetTime, offsetMs) > GameConfig.NormalWindowMs;

        /// <summary>판정 원 바깥 지름 (px, 1080p 기준). 목표 2박자 전 360 → 목표 시점 120, 선형.</summary>
        public static float OuterRingDiameter(double beatsUntilTarget)
        {
            double t = beatsUntilTarget / GameConfig.ApproachBeats;
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            return (float)(GameConfig.RingTargetDiameterPx + (GameConfig.RingStartDiameterPx - GameConfig.RingTargetDiameterPx) * t);
        }

        /// <summary>판정 문구 (GDD 4.8).</summary>
        public static string Label(Judgement j) =>
            j == Judgement.Perfect ? "PERFECT" : j == Judgement.Normal ? "NORMAL" : "FAIL";

        /// <summary>화살표 글자.</summary>
        public static string Arrow(ArrowKey k) =>
            k == ArrowKey.Up ? "↑" : k == ArrowKey.Down ? "↓" : k == ArrowKey.Left ? "←" : "→";
    }
}
