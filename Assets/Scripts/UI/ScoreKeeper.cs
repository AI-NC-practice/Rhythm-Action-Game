using UnityEngine;
using UnityEngine.UI;
namespace RhythmActionGame
{
    /// <summary>
    /// 점수와 최고 속도를 모은다. 판정 하나마다 ScoreBoard에 더한다 (장구·방향키를 나눠서, 작업 R25 = GDD-beta B20).
    /// 되감기 패널티: 무사 결투·포위 전투 초입과 챕터 시작의 점수를 기억해 두고, 그 전투의 FAIL 되감기(체력 그대로)나
    /// 체력 0 챕터 되감기가 시작되면 기억한 점수 − 1000으로 돌린다. 장구 콤보는 그대로.
    /// </summary>
    public class ScoreKeeper : MonoBehaviour
    {
        public StageDirector director;
        public BeatClock clock;
        public ScoreBoard Board { get; } = new ScoreBoard();

        ScoreBoard.Checkpoint encounterCp, chapterCp;
        int chapter = -1;

        void OnEnable()
        {
            if (director != null)
            {
                director.AnyCueJudged += OnJudged;
                director.EncounterStarted += OnEncounterStarted;
                director.RewindStarted += OnRewindStarted;
            }
            if (clock != null) clock.SpeedChanged += Board.ObserveSpeed;
        }

        void OnDisable()
        {
            if (director != null)
            {
                director.AnyCueJudged -= OnJudged;
                director.EncounterStarted -= OnEncounterStarted;
                director.RewindStarted -= OnRewindStarted;
            }
            if (clock != null) clock.SpeedChanged -= Board.ObserveSpeed;
        }

        void OnJudged(JudgeCue cue, Judgement j) => Board.Add(j, cue != null && ScoreBoard.IsJangdan(cue.Key));

        /// <summary>새 판 시작 (챕터 되감기가 아닌 시작·다시 하기).</summary>
        public void ResetRun()
        {
            Board.Reset();
            Board.MaxScore = 0;
            encounterCp = default;
            chapterCp = default;
            chapter = -1;
        }

        void OnEncounterStarted(StageDirector.Encounter e)
        {
            if (e != null && (e.IsBossDuel || (e.Event != null && e.Event.IsSurround))) encounterCp = Board.Save();
        }

        void OnRewindStarted(RewindKind kind)
        {
            Board.RewindTo(kind == RewindKind.Chapter ? chapterCp : encounterCp);
        }

        void Update()
        {
            if (director == null) return;
            if (Board.MaxScore <= 0 && director.Chart != null) Board.MaxScore = MaxScoreOf(director.Chart, director.JangdanEnabled);
            var p = director.player;
            if (p == null || director.Rewinding) return;
            int n = MapLayout.SectionOf((int)p.RunBeat).Number;
            if (n != chapter) { chapter = n; chapterCp = Board.Save(); }   // 챕터에 들어설 때 점수 기억
        }

        /// <summary>만점: 장구 노드 수·방향키 판정 수(사건 + 전투)를 모두 PERFECT로.</summary>
        public static int MaxScoreOf(ChartData c, bool jangdan = true) =>
            ScoreBoard.MaxScoreFor(jangdan ? RhythmNotes.Generate(c).Count : 0, ChartCheck.RunTargets(c).Count + ChartCheck.CombatJudgements(c));
    }
}
