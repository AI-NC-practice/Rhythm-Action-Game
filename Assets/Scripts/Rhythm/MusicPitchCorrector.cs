using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 음악 음 높이 보정 (GDD 4.1, GDD-beta B15.1). 음악 AudioSource와 같은 GameObject에 붙어
    /// OnAudioFilterRead에서 PitchShifter로 음 높이를 1/s로 되돌린다. 빨라져도 음 높이는 그대로다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicPitchCorrector : MonoBehaviour
    {
        PitchShifter shifter;
        int shifterChannels;
        int sampleRate = 48000;

        /// <summary>지금 적용 중인 음 높이 배율 (= 1 / 속도 배율).</summary>
        public float Ratio { get; private set; } = 1f;

        /// <summary>보정이 음악을 늦추는 평균 시간 (초). BeatClock이 재생 위치를 이만큼 앞당긴다.</summary>
        public static float LatencySeconds => GameConfig.Music.PitchShiftMinDelaySeconds + GameConfig.Music.PitchShiftWindowSeconds * 0.5f;

        void Awake()
        {
            if (AudioSettings.outputSampleRate > 0) sampleRate = AudioSettings.outputSampleRate;
            Ensure(OutputChannels());
        }

        static int OutputChannels()
        {
            var mode = AudioSettings.speakerMode;
            switch (mode)
            {
                case AudioSpeakerMode.Mono: return 1;
                case AudioSpeakerMode.Quad: return 4;
                case AudioSpeakerMode.Surround: return 5;
                case AudioSpeakerMode.Mode5point1: return 6;
                case AudioSpeakerMode.Mode7point1: return 8;
                case AudioSpeakerMode.Prologic: return 2;
                default: return 2;
            }
        }

        void Ensure(int channels)
        {
            if (shifter != null && shifterChannels == channels) return;
            shifter = new PitchShifter(sampleRate, channels, GameConfig.Music.PitchShiftWindowSeconds, GameConfig.Music.PitchShiftMinDelaySeconds);
            shifter.Ratio = Ratio;
            shifterChannels = channels;
        }

        /// <summary>속도 배율 s를 받아 음 높이를 1/s로 맞춘다.</summary>
        public void SetSpeed(float s)
        {
            Ratio = s > 0f ? 1f / s : 1f;
            if (shifter != null) shifter.Ratio = Ratio;
        }

        /// <summary>재생 위치를 옮길 때 (시작·되감기) 지난 소리를 비운다.</summary>
        public void Clear() { if (shifter != null) shifter.Clear(); }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (shifter == null || shifterChannels != channels) Ensure(channels);   // 스피커 채널 수가 다르면 한 번 다시 만든다
            shifter.Process(data, channels);
        }
    }
}
