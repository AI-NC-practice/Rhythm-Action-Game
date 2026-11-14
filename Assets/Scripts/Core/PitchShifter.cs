using System;

namespace RhythmActionGame
{
    /// <summary>
    /// 실시간 피치 시프터 (GDD-beta B15.1). 지연선에서 두 갈래(탭)로 읽고, 탭마다 sin² 창으로 겹쳐 합친다.
    /// 읽는 위치가 쓰는 위치보다 ratio 배 빠르기로 움직여 음 높이가 ratio 배가 된다(길이는 그대로).
    /// ratio = 1이면 한 탭만 남겨(다른 탭 음량 0) 소리를 바꾸지 않고 일정한 지연(LatencySeconds)만 둔다.
    /// 오디오 스레드에서 부르므로 할당 없이 동작한다. 순수 C#.
    /// </summary>
    public class PitchShifter
    {
        readonly int channels;
        readonly float[] buf;          // 채널 교차 저장 링 버퍼
        readonly int frames;           // 링 버퍼 프레임 수
        readonly float window;         // 창 길이 (프레임)
        readonly float minDelay;       // 가장 짧은 지연 (프레임)
        int write;                     // 다음에 쓸 프레임
        double phase = 0.5;            // 탭 0의 창 위치 (0~1). 0.5 = 탭 0만 들림, 지연 = minDelay + window/2
        long written;
        volatile bool clearRequested;

        /// <summary>음 높이 배율 (0.5~2). 메인 스레드에서 바꿔도 된다.</summary>
        public volatile float Ratio = 1f;

        /// <summary>평균 지연 (초). 음악 재생 위치를 이만큼 앞당겨 박자와 맞춘다.</summary>
        public float LatencySeconds { get; }

        public PitchShifter(int sampleRate, int channels, float windowSeconds, float minDelaySeconds)
        {
            this.channels = Math.Max(1, channels);
            window = Math.Max(16f, windowSeconds * sampleRate);
            minDelay = Math.Max(2f, minDelaySeconds * sampleRate);
            int need = (int)(minDelay + window) + 8;
            int n = 1;
            while (n < need * 2) n <<= 1;
            frames = n;
            buf = new float[frames * this.channels];
            LatencySeconds = (minDelay + window * 0.5f) / sampleRate;
        }

        /// <summary>data(채널 교차)를 제자리에서 바꾼다.</summary>
        public void Process(float[] data, int dataChannels)
        {
            if (dataChannels != channels) return;   // 채널 수가 다르면 건드리지 않음
            if (clearRequested) { Array.Clear(buf, 0, buf.Length); written = 0; phase = 0.5; clearRequested = false; }
            float r = Ratio;
            if (r < 0.5f) r = 0.5f; else if (r > 2f) r = 2f;
            double step = (1.0 - r) / window;        // 한 프레임에 창 위치가 움직이는 양 (r < 1이면 지연이 늘어남)
            bool hold = Math.Abs(1f - r) < 1e-4f;
            int n = data.Length / channels;
            for (int f = 0; f < n; f++)
            {
                int wi = write * channels;
                for (int c = 0; c < channels; c++) buf[wi + c] = data[f * channels + c];
                write = (write + 1) & (frames - 1);
                written++;

                if (hold)
                {
                    // ratio 1: 탭 0이 창 가운데(0.5)에 오도록 천천히 옮겨 한 탭만 남긴다
                    double d = 0.5 - phase;
                    d -= Math.Round(d);
                    double glide = 0.25 / window;
                    phase += Math.Abs(d) <= glide ? d : Math.Sign(d) * glide;
                }
                else phase += step;
                phase -= Math.Floor(phase);

                double p0 = phase, p1 = phase + 0.5;
                if (p1 >= 1) p1 -= 1;
                double g0 = Math.Sin(Math.PI * p0); g0 *= g0;
                double g1 = 1.0 - g0;                   // sin²(π(p+½)) = cos²(πp)
                double d0 = minDelay + window * p0;
                double d1 = minDelay + window * p1;
                for (int c = 0; c < channels; c++)
                {
                    double v = 0;
                    if (g0 > 1e-6) v += g0 * Read(d0, c);
                    if (g1 > 1e-6) v += g1 * Read(d1, c);
                    data[f * channels + c] = (float)v;
                }
            }
        }

        /// <summary>delay 프레임 전의 값 (선형 보간). 아직 쓰지 않은 곳은 0.</summary>
        float Read(double delay, int c)
        {
            double pos = write - 1 - delay;
            if (written - 1 - delay < 0) return 0f;
            int i0 = (int)Math.Floor(pos);
            float t = (float)(pos - i0);
            int a = (i0 & (frames - 1)) * channels + c;
            int b = ((i0 + 1) & (frames - 1)) * channels + c;
            return buf[a] + (buf[b] - buf[a]) * t;
        }

        /// <summary>버퍼를 비운다 (되감기·다시 시작). 다음 Process 처음에 오디오 스레드에서 비운다.</summary>
        public void Clear() => clearRequested = true;
    }
}
