using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>점수·콤보·판정 수·최고 속도 (GDD 4.7). 순수 C#.</summary>
    public class ScoreBoard
    {
        public int Perfect { get; private set; }
        public int Normal { get; private set; }
        public int Fail { get; private set; }
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public float MaxSpeed { get; private set; } = GameConfig.SpeedStart;

        public int Total => Perfect + Normal + Fail;
        public float PerfectRatio => Total == 0 ? 0f : Perfect / (float)Total;

        public event System.Action Changed;

        public void Add(Judgement j)
        {
            switch (j)
            {
                case Judgement.Perfect: Perfect++; Score += GameConfig.ScorePerfect; Combo++; break;
                case Judgement.Normal: Normal++; Score += GameConfig.ScoreNormal; Combo++; break;
                default: Fail++; Score += GameConfig.ScoreFail; Combo = 0; break;
            }
            if (Combo > MaxCombo) MaxCombo = Combo;
            Changed?.Invoke();
        }

        public void ObserveSpeed(float s)
        {
            if (s > MaxSpeed) { MaxSpeed = s; Changed?.Invoke(); }
        }

        public void Reset()
        {
            Perfect = Normal = Fail = Score = Combo = MaxCombo = 0;
            Rewound = false;
            MaxSpeed = GameConfig.SpeedStart;
            Changed?.Invoke();
        }

        /// <summary>랭크 (클리어했을 때만). S ≥ 90% 그리고 FAIL 0 / A ≥ 75% / B ≥ 50% / C, 게임 오버면 "-".</summary>
        public static string Rank(bool cleared, int perfect, int normal, int fail)
        {
            if (!cleared) return "-";
            int total = perfect + normal + fail;
            float r = total == 0 ? 0f : perfect / (float)total;
            const float eps = 1e-6f;
            if (r + eps >= GameConfig.RankSRatio && fail == 0) return "S";
            if (r + eps >= GameConfig.RankARatio) return "A";
            if (r + eps >= GameConfig.RankBRatio) return "B";
            return "C";
        }

        /// <summary>체력 0으로 챕터 처음으로 되감은 적이 있는지 (GDD-beta B9.3). 있으면 클리어해도 랭크 "-".</summary>
        public bool Rewound { get; set; }

        public string Rank(bool cleared) => Rewound ? "-" : Rank(cleared, Perfect, Normal, Fail);
    }

    /// <summary>화면 문구 (GDD 4.8). 테스트가 문구를 그대로 비교한다.</summary>
    public static class UiText
    {
        public const string Title = "Rhythm-Action-Game";
        public const string Start = "시작";
        public const string Settings = "설정";
        public const string Quit = "종료";
        public const string TimingOffset = "타이밍 보정";
        public const string Volume = "음량";
        public const string Back = "돌아가기";
        public const string Paused = "일시정지";
        public const string Continue = "계속";
        public const string ToTitle = "타이틀로";
        public const string Cleared = "스테이지 클리어";
        public const string GameOver = "게임 오버";
        public const string Retry = "다시 하기";
        /// <summary>너무 일찍 눌러 무시된 입력 (GDD-beta B8.2).</summary>
        public const string TooEarly = "빠름";

        public static string Speed(float s) => "x" + s.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        public static string Combo(int n) => n >= GameConfig.ComboShowMin ? $"콤보 {n}" : "";
        public static string PerfectCount(int n) => $"PERFECT {n}";
        public static string NormalCount(int n) => $"NORMAL {n}";
        public static string FailCount(int n) => $"FAIL {n}";
        public static string MaxSpeed(float s) => "최고 속도 " + Speed(s);
        public static string Score(int n) => $"점수 {n}";
        public static string Rank(string r) => $"랭크 {r}";

        /// <summary>UI에 나오는 모든 한글·기호 (폰트 글자 검사용).</summary>
        public const string AllGlyphs = Title + Start + Settings + Quit + TimingOffset + Volume + Back + Paused + Continue + ToTitle + Cleared + GameOver + Retry + TooEarly
            + "콤보 최고 속도 점수 랭크 PERFECTNORMALFAIL x0123456789.+-ms SABC↑↓←→";
    }

    /// <summary>속도감 효과 (GDD 4.3). s ≥ 1.10이면 집중선, 세기 (s − 1.0) / 0.3, FOV 최대 +5°.</summary>
    public static class SpeedFxMath
    {
        public static float Intensity(float s) =>
            s + 1e-5f < GameConfig.SpeedLinesThreshold ? 0f : Mathf.Clamp01((s - GameConfig.SpeedMin) / (GameConfig.SpeedMax - GameConfig.SpeedMin));

        public static float FovBonus(float s) => GameConfig.SpeedFovBonusMax * Intensity(s);
    }
}
