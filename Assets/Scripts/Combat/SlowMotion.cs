using System;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 처형·윈드밀 슬로모션 (GDD 4.2): Time.timeScale 0.3을 실제 시간 0.4초 동안.
    /// 박자 시계는 dspTime 기준이라 영향받지 않는다(음악과 판정은 그대로).
    /// </summary>
    public class SlowMotion : MonoBehaviour
    {
        public Func<float> RealTime = () => Time.realtimeSinceStartup;

        public bool IsActive { get; private set; }
        public float EndsAt { get; private set; }
        public int TriggerCount { get; private set; }

        public void Trigger()
        {
            Time.timeScale = GameConfig.SlowMotionTimeScale;
            IsActive = true;
            EndsAt = RealTime() + GameConfig.SlowMotionSeconds;
            TriggerCount++;
        }

        void Update() => Tick();

        public void Tick()
        {
            if (!IsActive || RealTime() < EndsAt) return;
            IsActive = false;
            if (Time.timeScale > 0f) Time.timeScale = 1f;   // 일시정지(0) 중이면 건드리지 않는다
        }

        public void Cancel()
        {
            if (IsActive && Time.timeScale > 0f) Time.timeScale = 1f;
            IsActive = false;
        }

        void OnDestroy() => Cancel();
    }
}
