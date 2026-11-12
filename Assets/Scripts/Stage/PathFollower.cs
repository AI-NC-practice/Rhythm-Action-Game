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

        double lastClockBeat;
        bool hasLast;

        void Awake()
        {
            if (clock == null) clock = BeatClock.Instance;
        }

        /// <summary>경로 위치를 runBeat로 옮긴다.</summary>
        public void ResetTo(double runBeat)
        {
            RunBeat = runBeat;
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
            Place();
        }

        void Place()
        {
            var p = MapLayout.PointAt(Distance, out var fwd);
            transform.SetPositionAndRotation(p, Quaternion.LookRotation(fwd, Vector3.up));
        }
    }
}
