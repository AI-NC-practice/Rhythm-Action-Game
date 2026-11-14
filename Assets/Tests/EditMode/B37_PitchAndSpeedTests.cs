using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>음 높이 보정 피치 시프터와 마디 단위 속도 (GDD-beta B15.1, B15.2).</summary>
    public class B37_PitchAndSpeedTests
    {
        const int Rate = 48000;

        static float[] Sine(float hz, float seconds, int ch)
        {
            int n = (int)(seconds * Rate);
            var d = new float[n * ch];
            for (int i = 0; i < n; i++)
                for (int c = 0; c < ch; c++) d[i * ch + c] = 0.5f * Mathf.Sin(2f * Mathf.PI * hz * i / Rate);
            return d;
        }

        static float[] Run(PitchShifter ps, float[] data, int ch)
        {
            var o = (float[])data.Clone();
            const int block = 1024;                       // 오디오 콜백처럼 나눠서
            var buf = new float[block * ch];
            for (int at = 0; at < o.Length; at += block * ch)
            {
                int len = Mathf.Min(block * ch, o.Length - at);
                if (len != buf.Length) buf = new float[len];
                System.Array.Copy(o, at, buf, 0, len);
                ps.Process(buf, ch);
                System.Array.Copy(buf, 0, o, at, len);
            }
            return o;
        }

        static float Freq(float[] d, int ch, int from)
        {
            int up = 0, first = -1, last = -1;
            for (int i = from + 1; i < d.Length / ch; i++)
                if (d[(i - 1) * ch] < 0 && d[i * ch] >= 0) { up++; if (first < 0) first = i; last = i; }
            return (up - 1) / ((last - first) / (float)Rate);
        }

        static float Rms(float[] d, int ch, int from)
        {
            double s = 0; int n = 0;
            for (int i = from; i < d.Length / ch; i++) { s += d[i * ch] * d[i * ch]; n++; }
            return Mathf.Sqrt((float)(s / n));
        }

        [Test]
        public void B37_PitchShifter_SpeedX130_BringsPitchBackTo1Over130()
        {
            const int ch = 2;
            var ps = new PitchShifter(Rate, ch, GameConfig.Music.PitchShiftWindowSeconds, GameConfig.Music.PitchShiftMinDelaySeconds);
            ps.Ratio = 1f / 1.30f;
            var input = Sine(440f, 1.0f, ch);
            var o = Run(ps, input, ch);
            float f = Freq(o, ch, Rate / 5);
            Debug.Log($"[B37] 440 Hz × (1/1.30) → {f:F1} Hz");
            Assert.AreEqual(440f / 1.30f, f, 440f / 1.30f * 0.03f, "음 높이 1/s (약 338 Hz)");
            float g = Rms(o, ch, Rate / 5) / Rms(input, ch, 0);
            Assert.That(g, Is.InRange(0.8f, 1.2f), "음량 유지");
            // 두 채널을 똑같이 처리
            for (int i = Rate / 5; i < input.Length / ch; i += 997) Assert.AreEqual(o[i * ch], o[i * ch + 1], 1e-6f);
        }

        [Test]
        public void B37_PitchShifter_Speed100_OnlyConstantDelay22ms()
        {
            const int ch = 2;
            var ps = new PitchShifter(Rate, ch, GameConfig.Music.PitchShiftWindowSeconds, GameConfig.Music.PitchShiftMinDelaySeconds);
            Assert.AreEqual(0.022f, ps.LatencySeconds, 1e-4f, "평균 지연 = 2 ms + 40 ms / 2");
            Assert.AreEqual(ps.LatencySeconds, MusicPitchCorrector.LatencySeconds, 1e-4f);
            ps.Ratio = 1f;
            var input = Sine(440f, 0.5f, ch);
            var o = Run(ps, input, ch);
            int d = Mathf.RoundToInt(ps.LatencySeconds * Rate);
            float err = 0;
            for (int i = d + 10; i < input.Length / ch; i++) err = Mathf.Max(err, Mathf.Abs(o[i * ch] - input[(i - d) * ch]));
            Assert.Less(err, 1e-3f, "속도 1.00이면 소리를 바꾸지 않고 22 ms 늦추기만 함");
        }

        [Test]
        public void B37_NextBarBeat_FourBeatBars()
        {
            Assert.AreEqual(4.0, SpeedTiming.NextBarBeat(0));
            Assert.AreEqual(4.0, SpeedTiming.NextBarBeat(3.9));
            Assert.AreEqual(8.0, SpeedTiming.NextBarBeat(4.0), "마디 첫 박에 정해지면 다음 마디");
            Assert.AreEqual(8.0, SpeedTiming.NextBarBeat(5.5));
            Assert.AreEqual(104.0, SpeedTiming.NextBarBeat(101.25));
        }
    }
}
