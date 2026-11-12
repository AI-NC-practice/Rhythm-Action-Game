using UnityEngine;
using UnityEngine.UI;
namespace RhythmActionGame
{
    /// <summary>점수와 최고 속도를 모은다. 판정 하나마다 ScoreBoard에 더한다.</summary>
    public class ScoreKeeper : MonoBehaviour
    {
        public StageDirector director;
        public BeatClock clock;
        public ScoreBoard Board { get; } = new ScoreBoard();

        void OnEnable()
        {
            if (director != null) director.AnyJudged += OnJudged;
            if (clock != null) clock.SpeedChanged += Board.ObserveSpeed;
        }

        void OnDisable()
        {
            if (director != null) director.AnyJudged -= OnJudged;
            if (clock != null) clock.SpeedChanged -= Board.ObserveSpeed;
        }

        void OnJudged(Judgement j) => Board.Add(j);
    }
}
