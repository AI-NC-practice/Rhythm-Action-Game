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

        /// <summary>테스트에서 바꿔 끼우는 시계. 기본은 AudioSettings.dspTime.</summary>
        public Func<double> TimeSource = () => AudioSettings.dspTime;

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
            if (music != null && music.clip != null)
            {
                music.time = (float)(startBeat * GameConfig.SecondsPerBeat) % music.clip.length;
                music.Play();
            }
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
