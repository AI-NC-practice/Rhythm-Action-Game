using System;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>효과음 종류 (GDD-beta B7, 제안서 2장 소리 열).</summary>
    public enum SfxKind
    {
        MetalClang,   // 금속 "챙" (PERFECT 패링)
        LowBoom,      // 저음 "쿵"
        DullClank,    // 둔한 "탕" (NORMAL)
        BodyHit,      // 타격 (피격·처형)
        Whoosh,       // 바람 (휘두름·회피·도약)
        DustPuff,     // 먼지 (착지)
        EarRing,      // 귀 울림 (피격)
        Buk,          // 북 (윈드밀 화살표·사건)
        Jing,         // 징 (처형·윈드밀 마무리 잔향)
        Glint,        // 칼날 번쩍 "칭" (공격 예고)
        UiTick,       // UI
        JangguKung,   // 장구 궁편 (낮은 소리) — 반주
        JangguDeok,   // 장구 채편 (높은 소리) — 반주
        TapeRewind,   // 되감기 (GDD-beta B9.4)
        HitPerfect,   // 히트사운드 "덩" — 궁편 + 채편 (GDD-beta B14)
        HitNormal,    // 히트사운드 "딱" — 채편
        HitFail,      // 히트사운드 "턱" — 둔한 소리
        HitEmpty,     // 히트사운드 "틱" — 원 없는 입력 (아주 작게)
        // ---- 실제 녹음 (GDD-beta B17.5, Mixkit — docs/CREDITS.md). 녹음이 없으면 Fallback의 합성음 ----
        SwordWhoosh,  // 적 칼 휘두름 "쉭" (판정 반 박 전)
        SwordSwish,   // 내 칼 휘두름
        ParryClang,   // 막기 PERFECT "챙"
        BlockClang,   // 막기 NORMAL (둔한 "챙")
        Slash,        // 베임·처형
        Punch,        // 맞음 "퍽"
        Kick,         // 발차기
        BodyFall,     // 쓰러짐
    }

    /// <summary>
    /// 효과음 파형을 코드로 합성한다 (GDD-beta B7: 무료·자체 제작). 순수 함수, 44.1 kHz 모노, 최고값 0.9로 맞춤.
    /// 금속음은 비정수배 배음 + 짧은 노이즈, 저음은 피치가 떨어지는 사인 + 클릭, 바람은 노이즈에 필터를 쓸어 올린다.
    /// 음색 수치는 소리 설계값이라 여기 둔다 (ClickSynth와 같은 방식).
    /// </summary>
    public static class SfxSynth
    {
        public const int SampleRate = 44100;
        const float Peak = 0.9f;

        /// <summary>실제 녹음 종류(B17.5)의 합성 대체음. 그 밖의 종류는 그대로.</summary>
        public static SfxKind Fallback(SfxKind k)
        {
            switch (k)
            {
                case SfxKind.SwordWhoosh: case SfxKind.SwordSwish: return SfxKind.Whoosh;
                case SfxKind.ParryClang: return SfxKind.MetalClang;
                case SfxKind.BlockClang: return SfxKind.DullClank;
                case SfxKind.Slash: case SfxKind.Punch: case SfxKind.Kick: case SfxKind.BodyFall: return SfxKind.BodyHit;
                default: return k;
            }
        }

        public static float Seconds(SfxKind k)
        {
            k = Fallback(k);
            switch (k)
            {
                case SfxKind.MetalClang: return 0.8f;
                case SfxKind.LowBoom: return 0.7f;
                case SfxKind.DullClank: return 0.3f;
                case SfxKind.BodyHit: return 0.25f;
                case SfxKind.Whoosh: return 0.35f;
                case SfxKind.DustPuff: return 0.3f;
                case SfxKind.EarRing: return 0.35f;
                case SfxKind.Buk: return 0.5f;
                case SfxKind.Jing: return 2.0f;
                case SfxKind.Glint: return 0.2f;
                case SfxKind.UiTick: return 0.04f;
                case SfxKind.JangguKung: return 0.3f;
                case SfxKind.JangguDeok: return 0.1f;
                case SfxKind.TapeRewind: return 0.8f;
                case SfxKind.HitPerfect: return 0.3f;
                case SfxKind.HitNormal: return 0.12f;
                case SfxKind.HitFail: return 0.12f;
                case SfxKind.HitEmpty: return 0.03f;
                default: return 0.2f;
            }
        }

        /// <summary>배음 합 (비정수배 비율, 배음마다 감쇠 시간).</summary>
        static float Partials(float t, float f0, float[] ratios, float[] decays, float[] amps)
        {
            float s = 0;
            for (int i = 0; i < ratios.Length; i++) s += amps[i] * Mathf.Exp(-t / decays[i]) * Mathf.Sin(2f * Mathf.PI * f0 * ratios[i] * t);
            return s;
        }

        public static float[] Create(SfxKind k)
        {
            k = Fallback(k);
            int n = Mathf.RoundToInt(Seconds(k) * SampleRate);
            var d = new float[n];
            var rnd = new System.Random(17 + (int)k * 31);
            float Noise() => (float)(rnd.NextDouble() * 2.0 - 1.0);
            float lp = 0, lp2 = 0, hpPrev = 0, hpOut = 0;
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float v = 0;
                switch (k)
                {
                    case SfxKind.MetalClang:
                        v = Partials(t, 1800f, new[] { 1f, 2.76f, 5.40f, 8.93f }, new[] { 0.40f, 0.30f, 0.20f, 0.15f }, new[] { 1f, 0.7f, 0.45f, 0.3f });
                        if (t < 0.006f) v += Noise() * (1f - t / 0.006f) * 1.2f;
                        break;
                    case SfxKind.LowBoom:
                    {
                        float f = 45f + 75f * Mathf.Exp(-t / 0.08f);       // 120 Hz → 45 Hz
                        phase += 2.0 * Math.PI * f / SampleRate;
                        v = Mathf.Exp(-t / 0.25f) * (float)Math.Sin(phase);
                        if (t < 0.002f) v += Noise() * 0.8f;
                        break;
                    }
                    case SfxKind.DullClank:
                        lp += 0.15f * (Noise() - lp);
                        v = Partials(t, 600f, new[] { 1f, 2.3f, 3.9f }, new[] { 0.08f, 0.05f, 0.03f }, new[] { 1f, 0.5f, 0.3f }) + lp * Mathf.Exp(-t / 0.04f) * 1.5f;
                        break;
                    case SfxKind.BodyHit:
                        lp += 0.08f * (Noise() - lp);
                        v = lp * Mathf.Exp(-t / 0.03f) * 4f + 0.8f * Mathf.Exp(-t / 0.08f) * Mathf.Sin(2f * Mathf.PI * 160f * t);
                        break;
                    case SfxKind.Whoosh:
                    {
                        float u = t / Seconds(k);
                        float cut = Mathf.Lerp(0.02f, 0.35f, u);           // 필터를 위로 쓸어 올림
                        lp += cut * (Noise() - lp);
                        lp2 += cut * (lp - lp2);
                        v = lp2 * Mathf.Sin(u * Mathf.PI) * 3f;
                        break;
                    }
                    case SfxKind.DustPuff:
                        lp += 0.05f * (Noise() - lp);
                        v = lp * Mathf.Sin(Mathf.Min(1f, t / 0.02f) * Mathf.PI * 0.5f) * Mathf.Exp(-t / 0.1f) * 5f;
                        break;
                    case SfxKind.EarRing:
                        v = 0.3f * Mathf.Exp(-t / 0.15f) * Mathf.Sin(2f * Mathf.PI * 3800f * t);
                        break;
                    case SfxKind.Buk:
                    {
                        float f = 70f + 25f * Mathf.Exp(-t / 0.05f);
                        phase += 2.0 * Math.PI * f / SampleRate;
                        lp += 0.2f * (Noise() - lp);
                        v = Mathf.Exp(-t / 0.18f) * (float)Math.Sin(phase) + lp * Mathf.Exp(-t / 0.02f) * 0.8f;
                        break;
                    }
                    case SfxKind.Jing:
                    {
                        float wob = 1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 4f * t);   // 징의 울렁임
                        v = Partials(t * wob, 220f, new[] { 1f, 1.52f, 2.4f, 3.3f }, new[] { 1.2f, 0.9f, 0.5f, 0.3f }, new[] { 1f, 0.6f, 0.35f, 0.2f })
                            * Mathf.Min(1f, t / 0.01f);
                        break;
                    }
                    case SfxKind.Glint:
                        v = Partials(t, 3200f, new[] { 1f, 2.1f }, new[] { 0.06f, 0.04f }, new[] { 1f, 0.5f });
                        break;
                    case SfxKind.UiTick:
                        v = Mathf.Exp(-t / 0.008f) * Mathf.Sin(2f * Mathf.PI * 1200f * t);
                        break;
                    case SfxKind.JangguKung:
                    {
                        float f = 80f + 30f * Mathf.Exp(-t / 0.04f);
                        phase += 2.0 * Math.PI * f / SampleRate;
                        v = Mathf.Exp(-t / 0.12f) * (float)Math.Sin(phase);
                        if (t < 0.003f) v += Noise() * 0.5f;
                        break;
                    }
                    case SfxKind.TapeRewind:
                    {
                        // 테이프를 거꾸로 감는 소리: 높아지며 빨라지는 떨림 + 바람 노이즈, 끝에 뚝 끊김
                        float u = t / Seconds(k);
                        float f = Mathf.Lerp(180f, 1400f, u * u);
                        phase += 2.0 * Math.PI * f / SampleRate;
                        lp += 0.12f * (Noise() - lp);
                        float flutter = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(8f, 30f, u) * t);
                        v = ((float)Math.Sin(phase) * 0.5f + lp * 2f) * flutter * Mathf.Min(1f, t / 0.03f) * (u < 0.92f ? 1f : (1f - u) / 0.08f);
                        break;
                    }
                    case SfxKind.HitPerfect:
                    {
                        // 덩: 궁편(피치가 떨어지는 저음) + 채편(맑은 "딱", 고역 노이즈 + 배음)
                        float f = 85f + 40f * Mathf.Exp(-t / 0.03f);
                        phase += 2.0 * Math.PI * f / SampleRate;
                        float x = Noise();
                        hpOut = 0.9f * (hpOut + x - hpPrev);
                        hpPrev = x;
                        v = 1.0f * Mathf.Exp(-t / 0.11f) * (float)Math.Sin(phase)
                            + 0.9f * hpOut * Mathf.Exp(-t / 0.012f)
                            + Partials(t, 1150f, new[] { 1f, 2.27f }, new[] { 0.035f, 0.02f }, new[] { 0.6f, 0.3f });
                        break;
                    }
                    case SfxKind.HitNormal:
                    {
                        // 딱: 채편만, 덕보다 조금 더 맑고 짧게
                        float x = Noise();
                        hpOut = 0.9f * (hpOut + x - hpPrev);
                        hpPrev = x;
                        v = hpOut * Mathf.Exp(-t / 0.01f) + Partials(t, 950f, new[] { 1f, 2.27f }, new[] { 0.03f, 0.018f }, new[] { 0.6f, 0.25f });
                        break;
                    }
                    case SfxKind.HitFail:
                    {
                        // 턱: 저역 통과 노이즈 + 낮은 사인, 울림 없이 짧게
                        lp += 0.1f * (Noise() - lp);
                        v = lp * Mathf.Exp(-t / 0.025f) * 3f + 0.7f * Mathf.Exp(-t / 0.04f) * Mathf.Sin(2f * Mathf.PI * 220f * t);
                        break;
                    }
                    case SfxKind.HitEmpty:
                        v = Mathf.Exp(-t / 0.004f) * Mathf.Sin(2f * Mathf.PI * 2000f * t);
                        break;
                    case SfxKind.JangguDeok:
                    {
                        float x = Noise();
                        hpOut = 0.9f * (hpOut + x - hpPrev);                 // 고역 통과
                        hpPrev = x;
                        v = hpOut * Mathf.Exp(-t / 0.015f) + 0.6f * Mathf.Exp(-t / 0.03f) * Mathf.Sin(2f * Mathf.PI * 700f * t);
                        break;
                    }
                }
                d[i] = v;
            }
            Normalize(d);
            // 끝을 0으로 (딸깍 소리 방지)
            int fade = Mathf.Min(n, SampleRate / 200);
            for (int i = 0; i < fade; i++) d[n - 1 - i] *= i / (float)fade;
            return d;
        }

        static void Normalize(float[] d)
        {
            float m = 0;
            foreach (var x in d) m = Mathf.Max(m, Mathf.Abs(x));
            if (m < 1e-6f) return;
            float g = Peak / m;
            for (int i = 0; i < d.Length; i++) d[i] *= g;
        }

        public static AudioClip CreateClip(SfxKind k)
        {
            var data = Create(k);
            var clip = AudioClip.Create("Sfx_" + k, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>16비트 PCM 모노 WAV 파일 바이트.</summary>
        public static byte[] ToWav(float[] data, int rate = SampleRate)
        {
            int bytes = data.Length * 2;
            var b = new byte[44 + bytes];
            void W(int at, string s) { for (int i = 0; i < 4; i++) b[at + i] = (byte)s[i]; }
            void I32(int at, int v) { b[at] = (byte)v; b[at + 1] = (byte)(v >> 8); b[at + 2] = (byte)(v >> 16); b[at + 3] = (byte)(v >> 24); }
            void I16(int at, int v) { b[at] = (byte)v; b[at + 1] = (byte)(v >> 8); }
            W(0, "RIFF"); I32(4, 36 + bytes); W(8, "WAVE");
            W(12, "fmt "); I32(16, 16); I16(20, 1); I16(22, 1); I32(24, rate); I32(28, rate * 2); I16(32, 2); I16(34, 16);
            W(36, "data"); I32(40, bytes);
            for (int i = 0; i < data.Length; i++)
            {
                int s = Mathf.Clamp(Mathf.RoundToInt(data[i] * 32767f), -32768, 32767);
                I16(44 + i * 2, s);
            }
            return b;
        }
    }
}
