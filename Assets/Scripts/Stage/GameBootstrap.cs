using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>Game 씬 시작점. SceneBuilder가 참조를 연결해 둔다. 시작하면 박자 시계를 0박부터 돌린다.</summary>
    public class GameBootstrap : MonoBehaviour
    {
        public BeatClock clock;
        public JudgeSystem judge;
        public JudgeRingView ringView;
        public JudgeFx fx;
        public PathFollower player;
        public Camera playerCamera;
        public StageDirector director;
        public ScoreKeeper score;
        public GameHud hud;
        public PauseMenu pause;
        public ResultScreen result;
        public SpeedFx speedFx;
        public bool autoStart = true;

        public static GameBootstrap Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            if (judge != null) judge.clock = clock;
            if (player != null) player.clock = clock;
            if (ringView != null) ringView.Bind(judge, clock, playerCamera);
            if (director != null) { director.clock = clock; director.judge = judge; director.player = player; }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>다음 Game 씬 시작 위치(달리기 박자). 개발자 모드 챕터 이동이 쓴다. 한 번 쓰면 0으로 돌아간다.</summary>
        public static double PendingStartBeat;

        void Start()
        {
            if (!autoStart) return;
            double b = PendingStartBeat;
            PendingStartBeat = 0;
            StartRun(b);
        }

        /// <summary>runBeat 위치(달리기 박자)부터 스테이지를 시작한다. 박자 시계는 0박부터.</summary>
        public void StartRun(double runBeat = 0)
        {
            Time.timeScale = 1f;
            AudioListener.volume = GameSettings.Volume / 100f;
            if (score != null) score.Board.Reset();
            player.ResetTo(runBeat);
            clock.ResetSpeed();
            clock.StartClock();
            if (director != null) director.ResetStage(runBeat);
        }
    }
}
