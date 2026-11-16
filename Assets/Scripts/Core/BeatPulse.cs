using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 화면이 박에 맞춰 숨 쉬는 값 (GDD-beta B17.2). 순수 함수: 박자 시계 박자만 넣으면 된다.
    /// - 박 펄스: 박 순간 가장 크고 PulseBeats 동안 줄어든다. 마디 첫 박은 BarAccent배.
    /// - 예고음: 판정 목표 박 1박 전·2박 전.
    /// - 걸음: 1박 = 한 걸음. 박 순간 카메라가 가장 낮고, 왼발·오른발이 번갈아 좌우로 옮긴다.
    /// </summary>
    public static class BeatPulse
    {
        /// <summary>박 안 위치 (0 이상 1 미만). 음수 박자도 같은 규칙.</summary>
        public static double Phase(double beat) => beat - System.Math.Floor(beat);

        /// <summary>박자 번호(내림)가 마디 첫 박인지 (4박마다, 0박 포함).</summary>
        public static bool IsBarStart(double beat) => ClickSynth.IsAccent((long)System.Math.Floor(beat));

        /// <summary>박 펄스 모양 (0~1): 박 순간 1, PulseBeats 뒤 0 (제곱으로 빠르게 줄어듦).</summary>
        public static float Envelope(double beat)
        {
            float u = (float)(Phase(beat) / GameConfig.BeatFx.PulseBeats);
            if (u >= 1f) return 0f;
            float k = 1f - u;
            return k * k;
        }

        /// <summary>박 펄스 세기: 보통 박 0~1, 마디 첫 박 0~BarAccent.</summary>
        public static float Amount(double beat) => Envelope(beat) * (IsBarStart(beat) ? GameConfig.BeatFx.BarAccent : 1f);

        /// <summary>크기 배율 = 1 + 세기 × per (per: RingScale, HudScale, LaneScale).</summary>
        public static float Scale(double beat, float per) => 1f + Amount(beat) * per;

        /// <summary>
        /// 목표 박 target인 판정의 예고음 박들 (target−1, target−2, ... TickCount개). 판정 원이 뜨는 박(target − ApproachBeats)보다 이르면 뺀다.
        /// 반 박 판정이면 예고음도 반 박 자리에서 센다(1박 전·2박 전).
        /// </summary>
        public static IEnumerable<double> TickBeats(double target)
        {
            for (int k = GameConfig.BeatFx.TickCount; k >= 1; k--)
            {
                double b = target - k;
                if (b < target - GameConfig.ApproachBeats - 1e-6) continue;
                yield return b;
            }
        }

        /// <summary>걸음 높이 (m): 박 순간 −StepDropMeters, 반 박에서 0 (|sin| 꺾임이 발이 닿는 느낌).</summary>
        public static float StepHeight(double beat) =>
            GameConfig.BeatFx.StepDropMeters * (Mathf.Abs(Mathf.Sin((float)(Phase(beat) * System.Math.PI))) - 1f);

        /// <summary>왼발(짝수 박 −1)·오른발(홀수 박 +1).</summary>
        public static float Foot(double beat) => ((long)System.Math.Floor(beat) & 1) == 0 ? -1f : 1f;

        /// <summary>걸음 좌우 (m): 그 걸음의 발 쪽으로 반 박에서 가장 멀리.</summary>
        public static float StepSway(double beat) =>
            Foot(beat) * GameConfig.BeatFx.StepSwayMeters * Mathf.Sin((float)(Phase(beat) * System.Math.PI));

        /// <summary>걸음 기울기 (도).</summary>
        public static float StepRoll(double beat) =>
            -Foot(beat) * GameConfig.BeatFx.StepRollDegrees * Mathf.Sin((float)(Phase(beat) * System.Math.PI));

        /// <summary>
        /// 카메라 출렁임 (위치 m, 기울기 도). run = 달리는 정도 0~1: 달리면 걸음, 멈추면 박마다 살짝 내려앉음.
        /// </summary>
        public static Vector3 Bob(double beat, float run, out float roll)
        {
            run = Mathf.Clamp01(run);
            float idle = -GameConfig.BeatFx.IdleDipMeters * Amount(beat);
            roll = StepRoll(beat) * run;
            return new Vector3(StepSway(beat) * run, Mathf.Lerp(idle, StepHeight(beat), run), 0f);
        }
    }
}
