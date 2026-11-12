using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 개발용 메트로놈 (GDD 1장 사운드). 매 박 클릭음, 마디 첫 박(4박마다) 강세.
    /// 다음 박을 dsp 시각으로 예약 재생(PlayScheduled)해서 프레임과 무관하게 정확히 맞춘다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Metronome : MonoBehaviour
    {
        public BeatClock clock;
        /// <summary>다음 클릭을 얼마나 미리 예약할지 (초). GDD에 없음, Claude가 정함.</summary>
        public const double LookAheadSeconds = 0.1;

        AudioClip click, accent;
        AudioSource[] sources;
        int nextSource;
        long scheduledBeat = -1;

        /// <summary>지금까지 예약한 클릭 수 (테스트용).</summary>
        public int ScheduledCount { get; private set; }
        /// <summary>마지막으로 예약한 박자 번호 (테스트용).</summary>
        public long LastScheduledBeat => scheduledBeat;

        /// <summary>마지막으로 예약한 클릭이 강세였는지 (테스트용).</summary>
        public bool LastWasAccent { get; private set; }

        void Awake() => EnsureInit();

        void EnsureInit()
        {
            if (sources != null) return;
            click = ClickSynth.CreateClip(false);
            accent = ClickSynth.CreateClip(true);
            // 소리가 겹쳐도 잘리지 않게 AudioSource 2개를 번갈아 쓴다.
            var first = GetComponent<AudioSource>();
            first.playOnAwake = false;
            var second = gameObject.AddComponent<AudioSource>();
            second.playOnAwake = false;
            sources = new[] { first, second };
            if (clock == null) clock = BeatClock.Instance;
        }

        void Update() => Tick();

        /// <summary>다음 박이 예약 범위 안에 들어오면 클릭을 예약한다.</summary>
        public void Tick()
        {
            EnsureInit();
            if (clock == null || !clock.IsRunning || clock.IsPaused) return;
            long next = (long)System.Math.Floor(clock.CurrentBeat) + 1;
            if (next <= scheduledBeat) return;
            double at = clock.TimeOfBeat(next);
            if (at - clock.Now > LookAheadSeconds) return;

            var src = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Length;
            LastWasAccent = ClickSynth.IsAccent(next);
            src.clip = LastWasAccent ? accent : click;
            src.PlayScheduled(at);
            scheduledBeat = next;
            ScheduledCount++;
        }
    }
}
