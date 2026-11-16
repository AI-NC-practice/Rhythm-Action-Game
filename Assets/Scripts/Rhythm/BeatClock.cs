using System;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 게임의 박자 시계 (GDD 4.1). AudioSettings.dspTime 기준으로 BeatTimeline을 돌리고,
    /// 속도 배율을 음악 재생 속도(AudioSource.pitch)와 이동 속도에 함께 적용한다.
    /// </summary>
    [DefaultExecutionOrder(-1000)]   // 마디 첫 박 속도 변경(B15.2)을 다른 스크립트가 박자를 읽기 전에 적용
    public class BeatClock : MonoBehaviour
    {
        [Tooltip("음악을 재생하는 AudioSource. 없으면 음악 없이 박자만 센다(메트로놈 단계).")]
        public AudioSource music;
        [Tooltip("음악 음 높이 보정 (GDD-beta B15.1). 음악 AudioSource와 같은 GameObject.")]
        public MusicPitchCorrector pitchCorrector;

        static SmoothClock smooth;
        /// <summary>부드러운 오디오 시계 (GDD-beta B8.1): dspTime을 실제 시간으로 보간. 모든 BeatClock이 함께 쓴다.</summary>
        public static SmoothClock Smooth => smooth ?? (smooth = new SmoothClock(() => AudioSettings.dspTime, () => Time.realtimeSinceStartupAsDouble, DspBufferSeconds()));

        /// <summary>DSP 버퍼 한 개 길이 (초).</summary>
        public static double DspBufferSeconds()
        {
            AudioSettings.GetDSPBufferSize(out int len, out int _);
            int rate = AudioSettings.outputSampleRate;
            return rate > 0 && len > 0 ? len / (double)rate : 1024.0 / 48000.0;
        }

        /// <summary>테스트에서 바꿔 끼우는 시계. 기본은 dspTime을 보간한 부드러운 시계 (GDD-beta B8.1).</summary>
        public Func<double> TimeSource = () => Smooth.Now();

        /// <summary>
        /// 실제 시간(realtimeSinceStartup 기준, Input System 이벤트 시각)을 이 시계의 시각으로 바꾼다 (GDD-beta B8.1).
        /// 테스트에서 바꿔 끼운다.
        /// </summary>
        public Func<double, double> RealtimeToClock = rt => Smooth.FromRealtime(rt);

        readonly BeatTimeline timeline = new BeatTimeline();
        public SpeedState Speed { get; } = new SpeedState();

        public static BeatClock Instance { get; private set; }

        public double Now => TimeSource();
        public double CurrentBeat => timeline.BeatAt(Now);
        /// <summary>지금 실제로 적용 중인 속도 배율 (박자 시계·이동·음악). 판정으로 바뀐 값은 다음 마디 첫 박에 들어온다 (GDD-beta B15.2). 회피 슬로모션 배율(SlowFactor)이 곱해져 있다 (B17.4).</summary>
        public float SpeedMultiplier => timeline.Speed;
        /// <summary>판정으로 정해진 속도 배율 (HUD가 바로 보여 준다). 다음 마디 첫 박에 SpeedMultiplier가 된다.</summary>
        public float TargetSpeed => Speed.Speed;
        /// <summary>판정 속도 배율 중 지금 적용된 값 (회피 슬로모션 배율을 빼고, HUD·최고 속도용).</summary>
        public float AppliedSpeed => applied;
        /// <summary>회피 슬로모션 배율 (GDD-beta B17.4): 1이면 평소, 0.4면 박자 시계·음악이 0.4배. 음 높이 보정은 이 배율만큼 빼서 음악이 낮아진다.</summary>
        public float SlowFactor { get; private set; } = 1f;
        float applied = 1f;
        /// <summary>목표 속도를 적용할 마디 첫 박 (없으면 NaN).</summary>
        public double PendingSpeedBeat { get; private set; } = double.NaN;
        public bool IsRunning => timeline.IsRunning;
        public bool IsPaused => timeline.IsPaused;

        /// <summary>현재 달리기 속도 (m/s) = 5 × s.</summary>
        public float MoveSpeed => SpeedRules.MoveSpeed(SpeedMultiplier);

        /// <summary>실제 속도 배율이 바뀌면 호출된다 (새 배율). 판정 뒤 다음 마디 첫 박에 온다.</summary>
        public event Action<float> SpeedChanged;
        /// <summary>판정으로 목표 속도 배율이 바뀌면 바로 호출된다 (HUD 예고용).</summary>
        public event Action<float> TargetSpeedChanged;

        void Update()
        {
            TickSpeed();
            TickMusicSync();
        }

        /// <summary>
        /// 시계 0박에 해당하는 곡 박 (GDD-beta B16.3). 챕터 되감기·챕터 이동은 시계가 0박부터 다시 시작하므로
        /// 곡은 그 챕터가 시작되는 곡 위치부터 재생한다. GameBootstrap.StartRun이 정한다.
        /// </summary>
        public double MusicBeatOffset { get; set; }

        /// <summary>음악 위치를 다시 맞춘 횟수 (테스트·진단용).</summary>
        public int MusicResyncCount { get; private set; }

        /// <summary>지금 박자에서 음악이 있어야 할 파일 안 위치 (초). 음 높이 보정 지연만큼 앞당긴다 (B15.1).</summary>
        public float ExpectedMusicTime()
        {
            if (music == null || music.clip == null) return 0f;
            return MusicTimeAt(CurrentBeat + MusicBeatOffset + MusicLeadBeats(), music.clip.length);
        }

        double MusicLeadBeats() =>
            pitchCorrector != null ? MusicPitchCorrector.LatencySeconds * SpeedMultiplier / GameConfig.SecondsPerBeat : 0;

        /// <summary>
        /// 음악이 박자 시계와 ResyncThresholdSeconds 넘게 어긋나면 재생 위치를 다시 맞춘다 (B10).
        /// 곡 끝에서 처음으로 넘어갈 때(mp3 꼬리만큼 늦어짐)와 오래 재생해 쌓인 오차를 바로잡는다. Update에서 부른다.
        /// </summary>
        public void TickMusicSync()
        {
            if (music == null || music.clip == null || !IsRunning || IsPaused || !music.isPlaying) return;
            float want = ExpectedMusicTime();
            if (!NeedsResync(music.time, want)) return;
            music.time = want;
            MusicResyncCount++;
        }

        /// <summary>음악 위치 have가 want에서 ResyncThresholdSeconds 넘게 어긋났는지. 반복 경계를 넘는 차이는 짧은 쪽으로 잰다 (순수 함수).</summary>
        public static bool NeedsResync(float have, float want)
        {
            float loop = GameConfig.Music.LoopBeats * GameConfig.SecondsPerBeat;
            float diff = have - want;
            diff -= loop * Mathf.Round(diff / loop);
            return Mathf.Abs(diff) > GameConfig.Music.ResyncThresholdSeconds;
        }

        /// <summary>목표 속도를 적용할 마디 첫 박이 지났으면 그 박 시각부터 새 속도로 바꾼다 (GDD-beta B15.2). Update에서 부른다.</summary>
        public void TickSpeed()
        {
            if (double.IsNaN(PendingSpeedBeat) || !IsRunning || IsPaused) return;
            double now = Now;
            if (timeline.BeatAt(now) + 1e-9 < PendingSpeedBeat) return;
            double at = timeline.TimeOfBeat(PendingSpeedBeat, now);   // 마디 첫 박이 온 시각 (지금 또는 조금 전)
            PendingSpeedBeat = double.NaN;
            SetApplied(Speed.Speed, at);
        }

        void SetApplied(float s, double at)
        {
            bool changed = !Mathf.Approximately(applied, s);
            applied = s;
            timeline.SetSpeed(s * SlowFactor, at);
            ApplyPitch();
            if (changed) SpeedChanged?.Invoke(s);
        }

        void Awake()
        {
            Instance = this;
            ApplyPitch();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>startBeat 박자부터 시계를 시작한다. 음악이 있으면 같은 순간부터 재생한다.</summary>
        public void StartClock(double startBeat = 0)
        {
            SlowFactor = 1f;
            timeline.Start(Now, startBeat);
            PendingSpeedBeat = double.NaN;
            SetApplied(Speed.Speed, Now);
            PlayMusicAt(startBeat);
        }

        /// <summary>음악을 beat 박자 위치부터 재생한다. Play()가 재생 위치를 처음으로 돌리므로 위치는 Play() 뒤에 정한다.</summary>
        void PlayMusicAt(double beat)
        {
            if (music == null || music.clip == null) return;
            music.Play();
            // 음 높이 보정이 음악을 LatencySeconds만큼 늦추므로 재생 위치를 그만큼 앞당긴다 (GDD-beta B15.1)
            if (pitchCorrector != null) pitchCorrector.Clear();
            music.time = MusicTimeAt(beat + MusicBeatOffset + MusicLeadBeats(), music.clip.length);
        }

        /// <summary>
        /// beat 박자에 해당하는 음악 파일 안 재생 위치 (초, B10). 곡은 첫 박(GameConfig.Music.FirstBeatSeconds)부터
        /// 120 BPM 격자에 맞춰져 있고 LoopBeats(384박)마다 반복된다. 클립이 그보다 짧으면 클립 길이로 반복한다.
        /// </summary>
        public static float MusicTimeAt(double beat, float clipLength)
        {
            if (clipLength <= 0f) return 0f;
            double loop = System.Math.Min(GameConfig.Music.LoopBeats * GameConfig.SecondsPerBeat, clipLength - GameConfig.Music.FirstBeatSeconds);
            double t = beat * GameConfig.SecondsPerBeat;
            t = ((t % loop) + loop) % loop;
            return (float)(GameConfig.Music.FirstBeatSeconds + t);
        }

        public void Pause()
        {
            timeline.Pause(Now);
            if (music != null) music.Pause();
        }

        public void Resume()
        {
            timeline.Resume(Now);
            if (music != null) music.UnPause();
        }

        /// <summary>
        /// 되감기 (GDD-beta B9.2): 지금 멈춰 있든 아니든 beat 박자부터 다시 진행한다. 속도 배율은 그대로, 음악도 같은 위치로 옮긴다.
        /// </summary>
        public void RestartAt(double beat)
        {
            SlowFactor = 1f;
            timeline.Start(Now, beat);
            PendingSpeedBeat = double.NaN;   // 되감기는 그 순간의 속도 배율로 바로 시작 (B15.2)
            SetApplied(Speed.Speed, Now);
            PlayMusicAt(beat);
            Restarted?.Invoke(beat);
        }

        /// <summary>RestartAt으로 박자가 옮겨졌을 때 (반주 예약을 다시 하도록).</summary>
        public event Action<double> Restarted;

        public void StopClock()
        {
            timeline.Stop();
            if (music != null) music.Stop();
        }

        /// <summary>
        /// 판정 결과를 속도 배율에 반영한다 (GDD 4.1 표). 목표 값은 바로 바뀌고(HUD), 실제 적용은 다음 마디 첫 박 (GDD-beta B15.2).
        /// </summary>
        public void ApplyJudgement(Judgement judgement)
        {
            float before = Speed.Speed;
            Speed.Apply(judgement);
            if (Mathf.Approximately(before, Speed.Speed)) return;
            if (double.IsNaN(PendingSpeedBeat)) PendingSpeedBeat = SpeedTiming.NextBarBeat(CurrentBeat);
            TargetSpeedChanged?.Invoke(Speed.Speed);
        }

        /// <summary>기다리는 목표 속도를 지금 바로 적용한다 (테스트·개발자 도구용).</summary>
        public void ApplyPendingSpeedNow()
        {
            PendingSpeedBeat = double.NaN;
            SetApplied(Speed.Speed, Now);
        }

        /// <summary>속도 배율을 시작값(1.00)으로 되돌린다 (다시 하기). 바로 적용한다.</summary>
        public void ResetSpeed()
        {
            Speed.Reset();
            PendingSpeedBeat = double.NaN;
            SetApplied(Speed.Speed, Now);
            TargetSpeedChanged?.Invoke(Speed.Speed);
        }

        /// <summary>
        /// 회피 슬로모션 배율을 at 시각부터 바꾼다 (GDD-beta B17.4). 지금까지의 박자는 그대로 이어지고, 음악 재생 속도도 같이 바뀌어
        /// 곡 위치와 박자의 관계는 그대로다. 음 높이 보정은 판정 속도만 되돌리므로 음악은 f만큼 낮아진다.
        /// </summary>
        public void SetSlowFactor(float f, double at)
        {
            f = Mathf.Clamp(f, 0.05f, 1f);
            if (Mathf.Approximately(f, SlowFactor)) return;
            SlowFactor = f;
            if (IsRunning) timeline.SetSpeed(applied * f, at);
            ApplyPitch();
        }

        /// <summary>beat 박자가 오는 dsp 시각.</summary>
        public double TimeOfBeat(double beat) => timeline.TimeOfBeat(beat, Now);

        void ApplyPitch()
        {
            // 음악 재생 속도 = s × 슬로 배율, 음 높이는 판정 속도 s만 1/s로 되돌린다 (GDD 4.1, GDD-beta B15.1·B17.4)
            if (music != null) music.pitch = applied * SlowFactor;
            if (pitchCorrector != null) pitchCorrector.SetSpeed(applied);
        }
    }
}
