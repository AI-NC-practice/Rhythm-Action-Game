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
        public PresentationDirector presentation;
        public JangguBacking backing;
        public HitSoundPlayer hitSound;
        public JangdanLaneView jangdanLane;
        /// <summary>박 펄스·걸음 (GDD-beta B17.2).</summary>
        public BeatFx beatFx;
        /// <summary>판정 예고음 (GDD-beta B17.2).</summary>
        public CueTickPlayer cueTicks;
        /// <summary>회피 슬로모션 (GDD-beta B17.4).</summary>
        public BulletTimeFx bulletTime;
        /// <summary>박에 맞춘 적 칼 휘두름 (GDD-beta B17.5).</summary>
        public CombatSwingSounds combatSwing;
        public bool autoStart = true;

        public static GameBootstrap Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            if (judge != null) judge.clock = clock;
            if (player != null) player.clock = clock;
            if (ringView != null) ringView.Bind(judge, clock, playerCamera);
            if (director != null) { director.clock = clock; director.judge = judge; director.player = player; director.ChapterRewindRequested += OnChapterRewind; }
        }

        /// <summary>체력 0 → 챕터 처음으로 되감기 (GDD-beta B9.3, 점수는 R25 = B20).</summary>
        void OnChapterRewind(double runBeat)
        {
            StartRun(runBeat, keepScore: true);   // 점수 패널티(챕터 시작 점수 − 1000)는 ScoreKeeper가 되감기 시작 때 (작업 R25)
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (director != null) director.ChapterRewindRequested -= OnChapterRewind;
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
        public void StartRun(double runBeat = 0) => StartRun(runBeat, keepScore: false);

        /// <summary>keepScore면 점수·판정 수를 이어 간다 (챕터 되감기).</summary>
        public void StartRun(double runBeat, bool keepScore)
        {
            Time.timeScale = 1f;
            AudioListener.volume = GameSettings.Volume / 100f;
            if (score != null && !keepScore) score.ResetRun();
            if (presentation != null) presentation.ResetAll();
            player.ResetTo(runBeat);
            clock.ResetSpeed();
            clock.MusicBeatOffset = MusicPlan.SongBeatAtRun(runBeat);   // 챕터 되감기·이동은 그 챕터의 곡 위치부터 (GDD-beta B16.3)
            clock.StartClock();
            if (backing != null) backing.ResetSchedule();
            if (cueTicks != null) cueTicks.ResetSchedule();
            if (director != null) director.ResetStage(runBeat);
        }
    }
}
