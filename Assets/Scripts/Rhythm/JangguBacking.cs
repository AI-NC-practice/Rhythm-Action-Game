using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 합성 장단 반주 (GDD-beta B7). 곡(Suno)이 들어오기 전까지 메트로놈 대신 박자를 알려 준다.
    /// 한 마디: 덩(궁+채) − 쿵 − 덕 − 덩 − 쿵 − 덕 (GameConfig.Fx.BackingOffsets). 마디 첫 박이 강세.
    /// 메트로놈처럼 다음 타격을 dsp 시각으로 미리 예약(PlayScheduled)해서 박자 시계와 정확히 맞춘다.
    /// AudioSource는 Music 그룹(SfxPlayer)에 속해 덕킹을 받는다.
    /// </summary>
    public class JangguBacking : MonoBehaviour
    {
        public BeatClock clock;
        public SfxPlayer sfx;

        AudioSource[] sources;
        int nextSource;
        double scheduledPos = double.NegativeInfinity;   // 마지막으로 예약한 타격의 박자
        AudioClip kung, deok;

        /// <summary>마지막으로 예약한 타격의 박자와 dsp 시각 (테스트용).</summary>
        public double LastScheduledBeat { get; private set; } = double.NaN;
        public double LastScheduledAt { get; private set; } = double.NaN;
        public int ScheduledCount { get; private set; }
        /// <summary>처음 예약한 타격 (테스트용).</summary>
        public double FirstScheduledBeat { get; private set; } = double.NaN;
        public double FirstScheduledAt { get; private set; } = double.NaN;

        void Awake() => EnsureInit();

        // 되감기(GDD-beta B9.2)로 박자가 옮겨지면 예약을 다시 한다
        void OnEnable() { if (clock != null) clock.Restarted += OnRestarted; }
        void OnDisable() { if (clock != null) clock.Restarted -= OnRestarted; }
        void OnRestarted(double beat) => ResetSchedule();

        void EnsureInit()
        {
            if (sources != null) return;
            if (clock == null) clock = BeatClock.Instance;
            kung = sfx != null ? sfx.Clip(SfxKind.JangguKung) : SfxSynth.CreateClip(SfxKind.JangguKung);
            deok = sfx != null ? sfx.Clip(SfxKind.JangguDeok) : SfxSynth.CreateClip(SfxKind.JangguDeok);
            sources = new AudioSource[6];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                if (sfx != null) sfx.RegisterMusic(sources[i]);
            }
        }

        /// <summary>박자 b 다음에 오는 첫 타격의 박자.</summary>
        public static double NextHitBeat(double b, out int index)
        {
            var offs = GameConfig.Fx.BackingOffsets;
            double bar = System.Math.Floor(b / GameConfig.BeatsPerBar) * GameConfig.BeatsPerBar;
            for (int k = 0; k < 3; k++)
            {
                for (int i = 0; i < offs.Length; i++)
                {
                    double at = bar + offs[i];
                    if (at > b + 1e-9) { index = i; return at; }
                }
                bar += GameConfig.BeatsPerBar;
            }
            index = 0;
            return bar;
        }

        void Update() => Tick();

        public void Tick()
        {
            EnsureInit();
            if (clock == null || !clock.IsRunning || clock.IsPaused) return;
            double from = System.Math.Max(scheduledPos, clock.CurrentBeat - 1e-6);
            for (int guard = 0; guard < 4; guard++)
            {
                double beat = NextHitBeat(from, out int idx);
                double at = clock.TimeOfBeat(beat);
                if (at - clock.Now > GameConfig.Fx.BackingLookAheadSeconds) return;
                var hits = GameConfig.Fx.BackingHits[idx];
                float vol = GameConfig.Fx.BackingVolumes[idx];
                foreach (var h in hits)
                {
                    var src = sources[nextSource];
                    nextSource = (nextSource + 1) % sources.Length;
                    src.clip = h == SfxKind.JangguKung ? kung : deok;
                    if (sfx != null) sfx.SetMusicBase(src, vol); else src.volume = vol;
                    src.PlayScheduled(at);
                }
                if (double.IsNaN(FirstScheduledBeat)) { FirstScheduledBeat = beat; FirstScheduledAt = at; }
                LastScheduledBeat = beat;
                LastScheduledAt = at;
                ScheduledCount++;
                scheduledPos = beat;
                from = beat;
            }
        }

        /// <summary>스테이지를 다시 시작할 때 (박자 시계가 0부터).</summary>
        public void ResetSchedule()
        {
            scheduledPos = double.NegativeInfinity;
            FirstScheduledBeat = FirstScheduledAt = LastScheduledBeat = LastScheduledAt = double.NaN;
            ScheduledCount = 0;
            if (sources != null) foreach (var s in sources) s.Stop();
        }
    }
}
