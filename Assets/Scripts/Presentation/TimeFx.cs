using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>Hit Stop으로 잠깐 멈출 수 있는 대상 (적 몸·무기·트레일·파티클).</summary>
    public interface IHitStoppable
    {
        void SetFrozen(bool frozen);
    }

    /// <summary>
    /// 시간 연출 (GDD-beta B4, B5).
    /// - Hit Stop: 대상 오브젝트만 멈춘다. Time.timeScale과 박자 시계는 건드리지 않는다. 겹치면 끝나는 시각이 늦은 쪽 하나만 쓴다.
    ///   슬로모션 중에는 새 Hit Stop을 무시한다.
    /// - 슬로모션: SlowMotion(시간 배율 0.3, 실제 0.4초)에 맡긴다. 후보(Candidate)는 직전 슬로모션 시작 후 16박 안이면 거절한다.
    /// </summary>
    public class TimeFx : MonoBehaviour
    {
        public SlowMotion slowMotion;
        public BeatClock clock;

        /// <summary>테스트에서 바꿔 끼우는 실제 시간 (초).</summary>
        public System.Func<float> RealTime = () => Time.realtimeSinceStartup;

        readonly List<IHitStoppable> frozen = new List<IHitStoppable>();
        float hitStopUntil;
        double lastSlowBeat = double.NegativeInfinity;

        public bool HitStopActive => RealTime() < hitStopUntil;
        public float HitStopUntil => hitStopUntil;
        public IReadOnlyList<IHitStoppable> Frozen => frozen;
        public double LastSlowMotionBeat => lastSlowBeat;
        public int SlowMotionCount { get; private set; }

        /// <summary>Hit Stop을 건다. 슬로모션 중이면 false. 이미 더 늦게 끝나는 Hit Stop이 있으면 대상만 더하고 시간은 그대로.</summary>
        public bool HitStop(float seconds, IList<IHitStoppable> targets)
        {
            if (seconds <= 0f) return false;
            if (slowMotion != null && slowMotion.IsActive) return false;
            float now = RealTime();
            float end = now + seconds;
            hitStopUntil = frozen.Count == 0 || now >= hitStopUntil ? end : Mathf.Max(hitStopUntil, end);   // 겹치면 늦게 끝나는 쪽 하나
            if (targets != null)
                foreach (var t in targets)
                {
                    if (t == null || frozen.Contains(t)) continue;
                    frozen.Add(t);
                    t.SetFrozen(true);
                }
            return true;
        }

        /// <summary>
        /// 슬로모션 요청. Always(처형·윈드밀)는 항상, Candidate는 직전 슬로모션 시작 후 16박이 지나야 한다.
        /// 받아들였으면 true.
        /// </summary>
        public bool RequestSlowMotion(SlowMoMode mode)
        {
            if (mode == SlowMoMode.None) return false;
            if (mode == SlowMoMode.Candidate && !GameConfig.BulletTime.PerfectCandidateSlowMo) return false;   // GDD-beta B17.4: PERFECT 뒤 슬로모션 없앰 (회피 슬로모션으로 바꿈)
            double beat = clock != null ? clock.CurrentBeat : 0;
            if (mode == SlowMoMode.Candidate && beat - lastSlowBeat < GameConfig.Fx.SlowMoMinGapBeats) return false;
            if (slowMotion != null) slowMotion.Trigger();
            lastSlowBeat = beat;
            SlowMotionCount++;
            return true;
        }

        /// <summary>빈도 제한 기록을 지운다 (스테이지 다시 시작).</summary>
        public void ResetHistory()
        {
            lastSlowBeat = double.NegativeInfinity;
            SlowMotionCount = 0;
            Release();
        }

        void Update() => Tick();

        public void Tick()
        {
            if (frozen.Count > 0 && RealTime() >= hitStopUntil) Release();
        }

        void Release()
        {
            foreach (var t in frozen)
                if (t != null && !(t is Object o && o == null)) t.SetFrozen(false);   // 씬이 바뀌어 지워진 대상은 건너뜀
            frozen.Clear();
        }

        void OnDisable() => Release();
    }
}
