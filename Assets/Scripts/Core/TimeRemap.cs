using System;

namespace RhythmActionGame
{
    /// <summary>
    /// 달리는 중 슬로모션의 이동 시간 재배치 (GDD-beta B4). 순수 함수.
    /// 논리 위치(RunBeat)는 박자 시계를 그대로 따르고, 화면에 보이는 위치만 늦춘 뒤 사건 끝까지 따라잡는다.
    ///   보이는 위치 = RunBeat − Lag(RunBeat)
    /// - [start, start+D]: 슬로모션 (보이는 속도 배율 f: 앞 0.08초 1→0.3, 가운데 0.3, 뒤 0.08초 0.3→1)
    /// - [start+D, end]: 쌓인 지연을 일정한 속도로 갚아 end에서 지연 0
    /// D는 실제 0.4초를 달리기 박자로 바꾼 값 (속도 배율 s일 때 1초 = 2s 박자).
    /// </summary>
    public struct TimeRemap
    {
        public double Start;     // 슬로모션 시작 (달리기 박자)
        public double End;       // 사건 끝 (달리기 박자) — 여기서 지연 0
        public double BeatsPerSecond;

        public TimeRemap(double start, double end, float speedMultiplier)
        {
            Start = start; End = end;
            BeatsPerSecond = speedMultiplier / GameConfig.SecondsPerBeat;
        }

        public double SlowBeats => GameConfig.SlowMotionSeconds * BeatsPerSecond;
        double RampBeats => GameConfig.SlowMotionRampSeconds * BeatsPerSecond;

        /// <summary>슬로모션 구간 안 x(0~D) 지점의 보이는 속도 배율.</summary>
        public double SpeedFactor(double x)
        {
            double d = SlowBeats, r = Math.Min(RampBeats, d * 0.5), lo = GameConfig.SlowMotionTimeScale;
            if (x <= 0 || x >= d) return 1.0;
            if (x < r) return 1.0 + (lo - 1.0) * (x / r);
            if (x > d - r) return lo + (1.0 - lo) * ((x - (d - r)) / r);
            return lo;
        }

        /// <summary>슬로모션 구간 안 x까지 쌓인 지연 (박자) = ∫(1 − f).</summary>
        double SlowLag(double x)
        {
            double d = SlowBeats, r = Math.Min(RampBeats, d * 0.5), k = 1.0 - GameConfig.SlowMotionTimeScale;
            x = Math.Max(0, Math.Min(d, x));
            double lag = 0;
            // 들어가는 경사: 지연 속도 k·(t/r)
            double a = Math.Min(x, r);
            lag += k * a * a / (2 * r);
            // 유지
            double holdEnd = d - r;
            if (x > r) lag += k * (Math.Min(x, holdEnd) - r);
            // 나오는 경사: 지연 속도 k·(1 − (t−holdEnd)/r)
            if (x > holdEnd)
            {
                double u = x - holdEnd;
                lag += k * (u - u * u / (2 * r));
            }
            return lag;
        }

        /// <summary>슬로모션이 끝날 때 쌓인 전체 지연 (박자).</summary>
        public double MaxLag => SlowLag(SlowBeats);

        /// <summary>논리 위치 runBeat에서의 지연 (박자). 구간 밖이면 0.</summary>
        public double Lag(double runBeat)
        {
            if (runBeat <= Start || runBeat >= End) return 0;
            double x = runBeat - Start;
            if (x <= SlowBeats) return SlowLag(x);
            double catchUp = End - (Start + SlowBeats);
            if (catchUp <= 1e-9) return 0;
            return MaxLag * (1.0 - (x - SlowBeats) / catchUp);
        }

        /// <summary>화면에 보이는 위치 (달리기 박자).</summary>
        public double Visual(double runBeat) => runBeat - Lag(runBeat);
    }
}
