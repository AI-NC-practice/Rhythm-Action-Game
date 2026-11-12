using System;

namespace RhythmActionGame
{
    /// <summary>
    /// 부드러운 오디오 시계 (GDD-beta B8.1). AudioSettings.dspTime은 DSP 버퍼 단위(약 21 ms)로 끊겨 갱신되므로,
    /// 마지막으로 바뀐 dsp 값부터 실제 시간이 흐른 만큼을 더해 매 프레임 부드럽게 만든다.
    /// - 뒤로 가지 않는다.
    /// - dsp 값과의 차이는 DSP 버퍼 한 개 길이를 넘지 않는다 ([dsp, dsp + buffer]).
    /// 순수 클래스라 dsp·실제 시간을 주입해 검사한다.
    /// </summary>
    public class SmoothClock
    {
        readonly Func<double> dsp;
        readonly Func<double> real;
        readonly double bufferSeconds;

        double anchorDsp = double.NaN, anchorReal;
        double last = double.NegativeInfinity;

        public SmoothClock(Func<double> dspSource, Func<double> realSource, double bufferSeconds)
        {
            dsp = dspSource;
            real = realSource;
            this.bufferSeconds = Math.Max(0.0, bufferSeconds);
        }

        public double BufferSeconds => bufferSeconds;

        /// <summary>지금 시각 (dsp 초 단위, 부드럽게 보간).</summary>
        public double Now()
        {
            double d = dsp(), r = real();
            if (double.IsNaN(anchorDsp) || d != anchorDsp)
            {
                // dsp가 뒤로 갔으면(장치 재시작 등) 기준을 새로 잡는다
                if (!double.IsNaN(anchorDsp) && d < anchorDsp) last = double.NegativeInfinity;
                anchorDsp = d;
                anchorReal = r;
            }
            double est = anchorDsp + (r - anchorReal);
            if (est < d) est = d;
            if (est > d + bufferSeconds) est = d + bufferSeconds;
            if (est < last) est = last;          // 뒤로 가지 않음
            last = est;
            return est;
        }

        /// <summary>
        /// 실제 시간(realtimeSinceStartup 기준, 예: Input System 이벤트 시각)을 이 시계의 시각으로 바꾼다.
        /// 지금보다 미래가 되지 않게 자른다.
        /// </summary>
        public double FromRealtime(double realTime)
        {
            double now = Now();
            if (double.IsNaN(anchorDsp)) return now;
            double t = anchorDsp + (realTime - anchorReal);
            return t > now ? now : t;
        }
    }
}
