using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 플레이어를 경로를 따라 자동으로 움직인다 (GDD 4.1, 4.9). 박자 시계가 1박 갈 때 2.5 m.
    /// 속도 배율이 바뀌어도 1박자 거리는 같으므로 위치는 "달린 박자 수"만으로 정해진다.
    /// 전투 중에는 Running=false로 멈춘다.
    /// </summary>
    public class PathFollower : MonoBehaviour
    {
        public BeatClock clock;

        /// <summary>지금까지 달린 박자 수 (경로 위치 = RunBeat × 2.5 m).</summary>
        public double RunBeat { get; private set; }
        public bool Running { get; set; } = true;
        public float Distance => (float)(RunBeat * GameConfig.MetersPerBeat);

        /// <summary>달리는 중 슬로모션의 이동 시간 재배치 (GDD-beta B4). 없으면 null.</summary>
        public TimeRemap? Remap { get; private set; }
        /// <summary>화면에 보이는 위치 (달리기 박자). 재배치가 없으면 RunBeat와 같다.</summary>
        public double VisualRunBeat => VisualOverride ?? (Remap.HasValue ? Remap.Value.Visual(RunBeat) : RunBeat);
        /// <summary>되감기 동안 보이는 위치를 직접 정한다 (GDD-beta B10.3). null이면 보통대로.</summary>
        public double? VisualOverride { get; set; }
        public float VisualDistance => (float)(VisualRunBeat * GameConfig.MetersPerBeat);

        /// <summary>지금 위치부터 슬로모션 재배치를 건다. endRunBeat(사건 끝)에서 보이는 위치가 논리 위치와 다시 같아진다.</summary>
        public void BeginRemap(double endRunBeat, float speedMultiplier)
        {
            Remap = new TimeRemap(RunBeat, endRunBeat, speedMultiplier);
        }

        double lastClockBeat;
        bool hasLast;

        void Awake()
        {
            if (clock == null) clock = BeatClock.Instance;
        }

        /// <summary>
        /// 달리기 박자 runBeat에 도착하는 시계 박자 (계속 달린다고 할 때). 마지막으로 위치를 갱신한 시계 박자를 기준으로 해서
        /// 같은 프레임 안 갱신 순서와 무관하게 정확하다.
        /// </summary>
        public double ClockBeatOfRun(double runBeat)
        {
            double baseClock = hasLast ? lastClockBeat : (clock != null ? clock.CurrentBeat : 0);
            return baseClock + (runBeat - RunBeat);
        }

        /// <summary>경로 위치를 runBeat로 옮긴다.</summary>
        public void ResetTo(double runBeat)
        {
            RunBeat = runBeat;
            Remap = null;
            VisualOverride = null;
            hasLast = false;
            Place();
        }

        void Update()
        {
            if (clock == null || !clock.IsRunning) { hasLast = false; return; }
            double beat = clock.CurrentBeat;
            if (hasLast && Running && !clock.IsPaused)
            {
                double delta = beat - lastClockBeat;
                if (delta > 0) RunBeat += delta;
            }
            lastClockBeat = beat;
            hasLast = true;
            if (Remap.HasValue && RunBeat >= Remap.Value.End) Remap = null;
            Place();
        }

        /// <summary>지금 보이는 위치로 트랜스폼을 옮긴다 (되감기처럼 다른 곳에서 위치를 바꿨을 때 바로 반영).</summary>
        public void Place()
        {
            var p = MapLayout.PointAt(VisualDistance, out var fwd);
            transform.SetPositionAndRotation(p, Quaternion.LookRotation(fwd, Vector3.up));
        }
    }
}
