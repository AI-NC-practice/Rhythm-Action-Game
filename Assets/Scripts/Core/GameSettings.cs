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

        public const string CueTicksKey = "settings.cueTicks";

        /// <summary>판정 예고음 "틱" 켜기 (GDD-beta B17.2, 설정에서 끄기). 기본 켜짐.</summary>
        public static bool CueTicks
        {
            get => PlayerPrefs.GetInt(CueTicksKey, 1) != 0;
            set { PlayerPrefs.SetInt(CueTicksKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public const string MusicOnKey = "settings.musicOn";
        static int musicCache = -1;

        /// <summary>
        /// 음악 켜기 (테스트용 요청, 2026-11-16 사용자). 끄면 Music 그룹(배경 곡)만 소리가 0이 되고 효과음·히트사운드·예고음은 그대로.
        /// 곡은 계속 재생되므로 박자·판정은 바뀌지 않는다. 기본 켜짐. 매 프레임 읽으므로 값을 기억해 둔다.
        /// </summary>
        public static bool MusicOn
        {
            get { if (musicCache < 0) musicCache = PlayerPrefs.GetInt(MusicOnKey, 1) != 0 ? 1 : 0; return musicCache != 0; }
            set { musicCache = value ? 1 : 0; PlayerPrefs.SetInt(MusicOnKey, musicCache); PlayerPrefs.Save(); }
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
