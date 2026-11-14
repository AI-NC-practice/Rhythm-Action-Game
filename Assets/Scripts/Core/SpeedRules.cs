using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>속도 배율 규칙 (GDD 4.1). 순수 함수.</summary>
    public static class SpeedRules
    {
        /// <summary>판정 결과를 반영한 새 속도 배율.</summary>
        public static float Apply(float speed, Judgement judgement)
        {
            switch (judgement)
            {
                case Judgement.Perfect: speed = Mathf.Min(speed + GameConfig.PerfectSpeedStep, GameConfig.SpeedMax); break;
                case Judgement.Fail: speed = Mathf.Max(speed * GameConfig.FailSpeedMultiplier, GameConfig.SpeedMin); break;
            }
            // 부동소수점 누적 오차 정리 (1.05 + 0.05 ... 가 1.2999999 가 되지 않도록)
            return Mathf.Clamp(Mathf.Round(speed * 10000f) / 10000f, GameConfig.SpeedMin, GameConfig.SpeedMax);
        }

        /// <summary>속도 배율 s에서의 달리기 속도 (m/s).</summary>
        public static float MoveSpeed(float speed) => GameConfig.BaseMoveSpeed * speed;

        /// <summary>속도 배율 s에서 1박자의 실제 길이 (초).</summary>
        public static float SecondsPerBeat(float speed) => GameConfig.SecondsPerBeat / speed;

        /// <summary>속도 배율 s로 beats 박자 동안 이동한 거리 (m). s와 무관하게 beats × 2.5 m.</summary>
        public static float DistanceForBeats(float beats, float speed) => MoveSpeed(speed) * SecondsPerBeat(speed) * beats;
    }

    /// <summary>속도 적용 시점 (GDD-beta B15.2).</summary>
    public static class SpeedTiming
    {
        /// <summary>beat 뒤에 오는 다음 마디 첫 박 (beat가 마디 첫 박이면 그다음 마디).</summary>
        public static double NextBarBeat(double beat)
        {
            double bar = GameConfig.BeatsPerBar;
            return (System.Math.Floor(beat / bar + 1e-9) + 1) * bar;
        }
    }

    /// <summary>현재 속도 배율과 최고 속도를 기억하는 상태.</summary>
    public class SpeedState
    {
        public float Speed { get; private set; } = GameConfig.SpeedStart;
        public float MaxSpeed { get; private set; } = GameConfig.SpeedStart;

        public event System.Action<float> Changed;

        public void Apply(Judgement judgement)
        {
            var next = SpeedRules.Apply(Speed, judgement);
            if (Mathf.Approximately(next, Speed)) return;
            Speed = next;
            if (Speed > MaxSpeed) MaxSpeed = Speed;
            Changed?.Invoke(Speed);
        }

        public void Reset()
        {
            Speed = MaxSpeed = GameConfig.SpeedStart;
            Changed?.Invoke(Speed);
        }
    }
}
