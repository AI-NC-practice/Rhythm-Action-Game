using System;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 게임의 박자 시계 (GDD 4.1). AudioSettings.dspTime 기준으로 BeatTimeline을 돌리고,
    /// 속도 배율을 음악 재생 속도(AudioSource.pitch)와 이동 속도에 함께 적용한다.
    /// </summary>
    public class BeatClock : MonoBehaviour
    {
        [Tooltip("음악을 재생하는 AudioSource. 없으면 음악 없이 박자만 센다(메트로놈 단계).")]
        public AudioSource music;

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
        public float SpeedMultiplier => Speed.Speed;
        public bool IsRunning => timeline.IsRunning;
        public bool IsPaused => timeline.IsPaused;

        /// <summary>현재 달리기 속도 (m/s) = 5 × s.</summary>
        public float MoveSpeed => SpeedRules.MoveSpeed(Speed.Speed);

        /// <summary>속도 배율이 바뀌면 호출된다 (새 배율).</summary>
        public event Action<float> SpeedChanged;

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
            timeline.Start(Now, startBeat);
            timeline.SetSpeed(Speed.Speed, Now);
            PlayMusicAt(startBeat);
        }

        /// <summary>음악을 beat 박자 위치부터 재생한다. Play()가 재생 위치를 처음으로 돌리므로 위치는 Play() 뒤에 정한다.</summary>
        void PlayMusicAt(double beat)
        {
            if (music == null || music.clip == null) return;
            music.Play();
            music.time = MusicTimeAt(beat, music.clip.length);
        }

        /// <summary>
        /// beat 박자에 해당하는 음악 파일 안 재생 위치 (초, B10). 곡은 첫 박(GameConfig.Music.FirstBeatSeconds)부터
        /// 120 BPM 격자에 맞춰져 있고 clipLength마다 반복된다.
        /// </summary>
        public static float MusicTimeAt(double beat, float clipLength)
        {
            if (clipLength <= 0f) return 0f;
            double loop = clipLength - GameConfig.Music.FirstBeatSeconds;
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
            timeline.Start(Now, beat);
            timeline.SetSpeed(Speed.Speed, Now);
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

        /// <summary>판정 결과를 속도 배율에 반영한다 (GDD 4.1 표).</summary>
        public void ApplyJudgement(Judgement judgement)
        {
            float before = Speed.Speed;
            Speed.Apply(judgement);
            if (!Mathf.Approximately(before, Speed.Speed)) OnSpeedChanged(Speed.Speed);
        }

        /// <summary>속도 배율을 시작값(1.00)으로 되돌린다 (다시 하기).</summary>
        public void ResetSpeed()
        {
            Speed.Reset();
            OnSpeedChanged(Speed.Speed);
        }

        /// <summary>beat 박자가 오는 dsp 시각.</summary>
        public double TimeOfBeat(double beat) => timeline.TimeOfBeat(beat, Now);

        void OnSpeedChanged(float s)
        {
            timeline.SetSpeed(s, Now);
            ApplyPitch();
            SpeedChanged?.Invoke(s);
        }

        void ApplyPitch()
        {
            // 음악 재생 속도 = s. 음 높이 보정(Pitch Shifter 1/s)은 실제 곡을 넣을 때 AudioMixer로 추가한다.
            if (music != null) music.pitch = Speed.Speed;
        }
    }
}
