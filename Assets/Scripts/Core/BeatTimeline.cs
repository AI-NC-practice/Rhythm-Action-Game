namespace RhythmActionGame
{
    /// <summary>
    /// 오디오 시계(dspTime, 초) → 박자 변환. 속도 배율이 바뀌어도 박자가 끊기지 않게 구간별로 적분한다.
    /// 순수 C# — MonoBehaviour(BeatClock)가 AudioSettings.dspTime을 넣어 준다.
    /// </summary>
    public class BeatTimeline
    {
        double anchorTime;   // 마지막 기준 시각 (dsp 초)
        double anchorBeat;   // 그 시각의 박자
        float speed = GameConfig.SpeedStart;

        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public float Speed => speed;

        /// <summary>startBeat 박자에서 시작한다 (time = 현재 dsp 초).</summary>
        public void Start(double time, double startBeat = 0)
        {
            anchorTime = time;
            anchorBeat = startBeat;
            IsRunning = true;
            IsPaused = false;
        }

        /// <summary>time 시각의 박자 (소수 포함). 일시정지 중이면 멈춘 박자.</summary>
        public double BeatAt(double time)
        {
            if (!IsRunning || IsPaused) return anchorBeat;
            return anchorBeat + (time - anchorTime) * speed / GameConfig.SecondsPerBeat;
        }

        /// <summary>beat 박자가 오는 dsp 시각 (현재 속도가 유지된다고 가정).</summary>
        public double TimeOfBeat(double beat, double now)
        {
            double cur = BeatAt(now);
            return now + (beat - cur) * GameConfig.SecondsPerBeat / speed;
        }

        /// <summary>속도 배율을 바꾼다. 지금까지의 박자는 그대로 이어진다.</summary>
        public void SetSpeed(float newSpeed, double time)
        {
            anchorBeat = BeatAt(time);
            anchorTime = time;
            speed = newSpeed;
        }

        public void Pause(double time)
        {
            if (!IsRunning || IsPaused) return;
            anchorBeat = BeatAt(time);
            anchorTime = time;
            IsPaused = true;
        }

        public void Resume(double time)
        {
            if (!IsRunning || !IsPaused) return;
            anchorTime = time;
            IsPaused = false;
        }

        public void Stop()
        {
            IsRunning = false;
            IsPaused = false;
        }
    }
}
