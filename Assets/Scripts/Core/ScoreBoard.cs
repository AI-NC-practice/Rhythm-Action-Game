using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 점수·콤보·판정 수·최고 속도 (GDD 4.7, 작업 R25 = GDD-beta B20에서 바꿈). 순수 C#.
    /// - 장구(쿵·덕): PERFECT 50 / NORMAL 20 + 장구 콤보 보너스(5↑ +10, 10↑ +20, 20↑ +30, 50↑ +50). 장구 FAIL만 콤보를 끊는다.
    /// - 방향키(사건·전투): PERFECT 100 / NORMAL 50, 콤보와 무관.
    /// - 되감기 패널티: 기억해 둔 점수(전투 초입·챕터 시작)로 돌아가고 1,000점을 뺀다(0 아래로 안 감). 콤보는 그대로.
    /// - 랭크: 만점 대비 점수 비율로 SSS SS S A B C D F.
    /// </summary>
    public class ScoreBoard
    {
        public int Perfect { get; private set; }
        public int Normal { get; private set; }
        public int Fail { get; private set; }
        public int Score { get; private set; }
        /// <summary>장구 콤보 (방향키는 세지 않음).</summary>
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public float MaxSpeed { get; private set; } = GameConfig.SpeedStart;
        /// <summary>되감기 패널티를 받은 횟수.</summary>
        public int Rewinds { get; private set; }
        /// <summary>이 스테이지 만점 (ScoreKeeper가 차트에서 계산해 넣는다). 0이면 랭크 계산 불가 → 판정 비율로 대신.</summary>
        public int MaxScore { get; set; }

        public int Total => Perfect + Normal + Fail;
        public float PerfectRatio => Total == 0 ? 0f : Perfect / (float)Total;

        public event System.Action Changed;

        /// <summary>장구 콤보 보너스 (이 판정을 더한 뒤의 콤보 수 기준).</summary>
        public static int JangdanBonus(int combo)
        {
            int bonus = 0;
            for (int i = 0; i < GameConfig.JangdanComboBonusAt.Length; i++)
                if (combo >= GameConfig.JangdanComboBonusAt[i]) bonus = GameConfig.JangdanComboBonus[i];
            return bonus;
        }

        public static bool IsJangdan(ArrowKey k) => k == ArrowKey.Kung || k == ArrowKey.Deok;

        /// <summary>방향키 판정 (콤보와 무관).</summary>
        public void Add(Judgement j) => Add(j, false);

        public void Add(Judgement j, bool jangdan)
        {
            switch (j)
            {
                case Judgement.Perfect: Perfect++; break;
                case Judgement.Normal: Normal++; break;
                default: Fail++; break;
            }
            if (jangdan)
            {
                if (j == Judgement.Fail) Combo = 0;
                else
                {
                    Combo++;
                    Score += (j == Judgement.Perfect ? GameConfig.ScoreJangdanPerfect : GameConfig.ScoreJangdanNormal) + JangdanBonus(Combo);
                }
                if (Combo > MaxCombo) MaxCombo = Combo;
            }
            else if (j != Judgement.Fail)
                Score += j == Judgement.Perfect ? GameConfig.ScoreArrowPerfect : GameConfig.ScoreArrowNormal;
            Changed?.Invoke();
        }

        /// <summary>장구 jangdan개·방향키 arrows개를 모두 PERFECT로 쳤을 때 (장구 콤보가 처음부터 이어짐).</summary>
        public static int MaxScoreFor(int jangdan, int arrows)
        {
            int s = arrows * GameConfig.ScoreArrowPerfect;
            for (int c = 1; c <= jangdan; c++) s += GameConfig.ScoreJangdanPerfect + JangdanBonus(c);
            return s;
        }

        // ---------------- 되감기 패널티 ----------------
        public struct Checkpoint { public int Score, Perfect, Normal, Fail; public bool Valid; }

        public Checkpoint Save() => new Checkpoint { Score = Score, Perfect = Perfect, Normal = Normal, Fail = Fail, Valid = true };

        /// <summary>
        /// 기억한 점수·판정 수로 돌아가고 패널티를 뺀다 (0 아래로 안 감). 전투 되감기(체력 그대로)는 콤보 그대로,
        /// 체력 0 챕터 되감기(resetCombo)는 콤보 0 (작업 R39, 사용자 결정 2026-11-24 = GDD-beta B22).
        /// </summary>
        public void RewindTo(Checkpoint cp, bool resetCombo = false)
        {
            if (cp.Valid) { Score = cp.Score; Perfect = cp.Perfect; Normal = cp.Normal; Fail = cp.Fail; }
            if (resetCombo) Combo = 0;
            Score = Mathf.Max(0, Score - GameConfig.RewindScorePenalty);
            Rewinds++;
            Changed?.Invoke();
        }

        public void ObserveSpeed(float s)
        {
            if (s > MaxSpeed) { MaxSpeed = s; Changed?.Invoke(); }
        }

        public void Reset()
        {
            Perfect = Normal = Fail = Score = Combo = MaxCombo = Rewinds = 0;
            MaxSpeed = GameConfig.SpeedStart;
            Changed?.Invoke();
        }

        /// <summary>점수 비율로 랭크 (만점 대비). 게임 오버면 "-".</summary>
        public static string Rank(bool cleared, int score, int maxScore)
        {
            if (!cleared) return "-";
            float r = maxScore <= 0 ? 0f : score / (float)maxScore;
            const float eps = 1e-6f;
            for (int i = 0; i < GameConfig.RankRatios.Length; i++)
                if (r + eps >= GameConfig.RankRatios[i]) return GameConfig.RankNames[i];
            return GameConfig.RankNames[GameConfig.RankNames.Length - 1];
        }

        public string Rank(bool cleared) => Rank(cleared, Score, MaxScore);
    }

    public static class UiText
    {
        public const string Title = "Rhythm-Action-Game";
        public const string Start = "시작";
        public const string Settings = "설정";
        public const string Quit = "종료";
        public const string TimingOffset = "타이밍 보정";
        public const string Volume = "음량";
        public const string Back = "돌아가기";
        /// <summary>판정 예고음 설정 (GDD-beta B17.2).</summary>
        public const string CueTicks = "예고음";
        /// <summary>음악 켜기/끄기 설정 (효과음만 듣기).</summary>
        public const string Music = "음악";
        public const string On = "켜짐";
        public const string Off = "꺼짐";
        public const string Paused = "일시정지";
        public const string Continue = "계속";
        public const string ToTitle = "타이틀로";
        public const string Cleared = "스테이지 클리어";
        public const string GameOver = "게임 오버";
        public const string Retry = "다시 하기";
        /// <summary>너무 일찍 눌러 무시된 입력 (GDD-beta B8.2).</summary>
        public const string TooEarly = "빠름";
        /// <summary>무사 자세 게이지 제목 (GDD-beta B11.3).</summary>
        public const string PostureTitle = "무사 자세";

        public static string Speed(float s) => "x" + s.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        public static string Combo(int n) => n >= GameConfig.ComboShowMin ? $"콤보 {n}" : "";
        public static string PerfectCount(int n) => $"PERFECT {n}";
        public static string NormalCount(int n) => $"NORMAL {n}";
        public static string FailCount(int n) => $"FAIL {n}";
        public static string MaxSpeed(float s) => "최고 속도 " + Speed(s);
        public static string Score(int n) => $"점수 {n}";
        public static string Rank(string r) => $"랭크 {r}";

        /// <summary>UI에 나오는 모든 한글·기호 (폰트 글자 검사용).</summary>
        public const string AllGlyphs = Title + Start + Settings + Quit + TimingOffset + Volume + Back + CueTicks + Music + On + Off + Paused + Continue + ToTitle + Cleared + GameOver + Retry + TooEarly
            + GameConfig.Music.Credit + "콤보 최고 속도 점수 랭크 PERFECTNORMALFAIL x0123456789.+-ms SABC↑↓←→";
    }

    /// <summary>속도감 효과 (GDD 4.3). s ≥ 1.10이면 집중선, 세기 (s − 1.0) / 0.3, FOV 최대 +5°.</summary>
    public static class SpeedFxMath
    {
        public static float Intensity(float s) =>
            s + 1e-5f < GameConfig.SpeedLinesThreshold ? 0f : Mathf.Clamp01((s - GameConfig.SpeedMin) / (GameConfig.SpeedMax - GameConfig.SpeedMin));

        public static float FovBonus(float s) => GameConfig.SpeedFovBonusMax * Intensity(s);
    }
}
