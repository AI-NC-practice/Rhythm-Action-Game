using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>사용자 설정 (GDD 4.8 설정 화면). PlayerPrefs에 저장한다.</summary>
    public static class GameSettings
    {
        public const string TimingOffsetKey = "settings.timingOffsetMs";
        public const string VolumeKey = "settings.volume";

        /// <summary>타이밍 보정 (ms, −200~+200, 5 ms 단위).</summary>
        public static int TimingOffsetMs
        {
            get => ClampOffset(PlayerPrefs.GetInt(TimingOffsetKey, GameConfig.TimingOffsetDefaultMs));
            set { PlayerPrefs.SetInt(TimingOffsetKey, ClampOffset(value)); PlayerPrefs.Save(); }
        }

        /// <summary>음량 (0~100).</summary>
        public static int Volume
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(VolumeKey, GameConfig.VolumeDefault), GameConfig.VolumeMin, GameConfig.VolumeMax);
            set { PlayerPrefs.SetInt(VolumeKey, Mathf.Clamp(value, GameConfig.VolumeMin, GameConfig.VolumeMax)); PlayerPrefs.Save(); }
        }

        /// <summary>범위로 자르고 5 ms 단위로 맞춘다.</summary>
        public static int ClampOffset(int ms)
        {
            ms = Mathf.Clamp(ms, GameConfig.TimingOffsetMinMs, GameConfig.TimingOffsetMaxMs);
            return Mathf.RoundToInt(ms / (float)GameConfig.TimingOffsetStepMs) * GameConfig.TimingOffsetStepMs;
        }

        /// <summary>표시 문자열. 예: "+15ms", "-20ms", "0ms".</summary>
        public static string FormatOffset(int ms) => ms > 0 ? $"+{ms}ms" : $"{ms}ms";
    }
}
