using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>메트로놈 클릭음 파형을 만든다 (순수 함수). 마디 첫 박은 더 높고 큰 소리.</summary>
    public static class ClickSynth
    {
        public const int SampleRate = 44100;
        public const float ClickSeconds = 0.05f;
        // GDD에 없음, Claude가 정함: 클릭음 음높이·크기 (소리 연출용 값이라 GameConfig가 아닌 여기 둔다)
        public const float NormalFrequency = 1000f;
        public const float AccentFrequency = 1600f;
        public const float NormalAmplitude = 0.5f;
        public const float AccentAmplitude = 0.9f;

        public static float[] Create(bool accent)
        {
            int n = Mathf.RoundToInt(SampleRate * ClickSeconds);
            var data = new float[n];
            float f = accent ? AccentFrequency : NormalFrequency;
            float a = accent ? AccentAmplitude : NormalAmplitude;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 80f); // 빠르게 사라지는 클릭
                data[i] = a * env * Mathf.Sin(2f * Mathf.PI * f * t);
            }
            return data;
        }

        /// <summary>박자 번호(0부터)가 마디 첫 박인지.</summary>
        public static bool IsAccent(long beatIndex) =>
            ((beatIndex % GameConfig.BeatsPerBar) + GameConfig.BeatsPerBar) % GameConfig.BeatsPerBar == 0;

        public static AudioClip CreateClip(bool accent)
        {
            var data = Create(accent);
            var clip = AudioClip.Create(accent ? "ClickAccent" : "Click", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
