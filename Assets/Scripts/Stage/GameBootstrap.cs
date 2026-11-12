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
        public bool autoStart = true;

        public static GameBootstrap Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            if (judge != null) judge.clock = clock;
            if (player != null) player.clock = clock;
            if (ringView != null) ringView.Bind(judge, clock, playerCamera);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (autoStart) StartRun();
        }

        public void StartRun()
        {
            player.ResetTo(0);
            clock.ResetSpeed();
            clock.StartClock();
        }
    }
}
