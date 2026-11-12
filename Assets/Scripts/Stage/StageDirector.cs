using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    public enum StageState
    {
        Running,
        Combat,
        GameOver,
        Cleared,
    }

    /// <summary>
    /// 차트를 따라 스테이지를 진행한다. T05: 장애물 판정 예약, 4박자 통과 연출, 체력과 게임 오버.
    /// 전투(적·포위·무사)는 CombatDirector(T06~)가 맡는다.
    /// </summary>
    public partial class StageDirector : MonoBehaviour
    {
        public BeatClock clock;
        public JudgeSystem judge;
        public PathFollower player;
        /// <summary>카메라 연출용 트랜스폼 (CameraRig 아래, PlayerCamera 위).</summary>
        public Transform motion;

        public PlayerHealth Health { get; } = new PlayerHealth();
        public StageState State { get; private set; } = StageState.Running;
        public ChartData Chart { get; private set; }

        public event Action<ChartEvent, Judgement> ObstacleResolved;
        public event Action<StageState> StateChanged;
        /// <summary>판정 하나가 끝날 때마다 (장애물·전투 모두). 점수·콤보 집계용.</summary>
        public event Action<Judgement> AnyJudged;

        /// <summary>진행 중인 장애물 하나.</summary>
        public class ObstacleRun
        {
            public ChartEvent Event;
            public JudgeCue Cue;
            public ObstacleCategory Category;
            public float Side;
            public bool Started;
            public bool HasResult;
            public Judgement Result;
        }

        readonly List<ObstacleRun> pending = new List<ObstacleRun>();
        ObstacleRun current;
        int nextEvent;

        public ObstacleRun Current => current;

        /// <summary>지금 통과 연출 상태 이름: "None", "Pending"(결과 대기), 또는 "Vault_Perfect" 같은 이름.</summary>
        public string TraversalState =>
            current == null ? "None" : !current.HasResult ? "Pending" : Traversal.StateName(current.Category, current.Result);

        /// <summary>지금 통과 진행도 (0~1).</summary>
        public float TraversalProgress => current == null ? 0f : Mathf.Clamp01((float)((player.RunBeat - current.Event.runBeat) / GameConfig.ObstacleBeats));

        void Awake()
        {
            Health.Died += OnDied;
        }

        void OnEnable() { if (judge != null) judge.CueJudged += OnCueJudged; }
        void OnDisable() { if (judge != null) judge.CueJudged -= OnCueJudged; }

        /// <summary>runBeat 위치부터 스테이지를 다시 시작한다.</summary>
        public void ResetStage(double runBeat)
        {
            if (Chart == null) Chart = StageChart.Load();
            judge.ClearAll();
            pending.Clear();
            current = null;
            Health.Reset();
            nextEvent = 0;
            while (nextEvent < Chart.events.Length && Chart.events[nextEvent].runBeat < runBeat) nextEvent++;
            ResetCombat();
            SetState(StageState.Running);
            player.Running = true;
            ApplyMotion(Vector3.zero, Vector3.zero);
        }

        partial void ResetCombat();
        /// <summary>전투 이벤트를 만나면 처리한다. 처리했으면 true (T06에서 구현).</summary>
        partial void TryStartCombat(ChartEvent ev, ref bool handled);
        partial void UpdateCombat();

        void SetState(StageState s)
        {
            if (State == s) return;
            State = s;
            StateChanged?.Invoke(s);
        }

        void Update()
        {
            if (Chart == null || clock == null || !clock.IsRunning) return;
            if (State == StageState.GameOver || State == StageState.Cleared) return;
            if (State == StageState.Running) ScheduleUpcoming();
            UpdateTraversal();
            UpdateCombat();
        }

        void ScheduleUpcoming()
        {
            while (nextEvent < Chart.events.Length)
            {
                var ev = Chart.events[nextEvent];
                double ahead = ev.runBeat - player.RunBeat;
                if (!ev.IsObstacle)
                {
                    // 전투: 적 위치에 도착하면 시작
                    if (ahead > 0) return;
                    bool handled = false;
                    TryStartCombat(ev, ref handled);
                    nextEvent++;
                    if (handled) return;
                    continue;
                }
                if (ahead > GameConfig.ApproachBeats + 1) return;
                var run = new ObstacleRun
                {
                    Event = ev,
                    Category = Traversal.Categorize(ev.kind),
                    Side = ev.Key == ArrowKey.Left ? -1f : ev.Key == ArrowKey.Right ? 1f : 0f,
                };
                var point = MapLayout.PointAt((ev.runBeat + GameConfig.ObstacleBeats * 0.5f) * GameConfig.MetersPerBeat) + Vector3.up;
                run.Cue = judge.Schedule(clock.CurrentBeat + ahead, ev.Key, point, run);
                pending.Add(run);
                nextEvent++;
            }
        }

        void OnCueJudged(JudgeCue cue, Judgement r)
        {
            AnyJudged?.Invoke(r);
            if (cue.Tag is ObstacleRun run)
            {
                run.HasResult = true;
                run.Result = r;
                ObstacleResolved?.Invoke(run.Event, r);
                if (r == Judgement.Fail) Health.Damage();
                return;
            }
            OnCombatCueJudged(cue, r);
        }

        partial void OnCombatCueJudgedImpl(JudgeCue cue, Judgement r);
        void OnCombatCueJudged(JudgeCue cue, Judgement r) => OnCombatCueJudgedImpl(cue, r);

        void UpdateTraversal()
        {
            if (current == null && pending.Count > 0 && player.RunBeat >= pending[0].Event.runBeat)
            {
                current = pending[0];
                pending.RemoveAt(0);
                current.Started = true;
            }
            if (current == null) return;

            double end = current.Event.runBeat + GameConfig.ObstacleBeats;
            if (player.RunBeat >= end)
            {
                current = null;
                ApplyMotion(Vector3.zero, Vector3.zero);
                return;
            }
            if (!current.HasResult) { ApplyMotion(Vector3.zero, Vector3.zero); return; }
            Traversal.Offset(current.Category, current.Result, current.Side, TraversalProgress, out var pos, out var euler);
            ApplyMotion(pos, euler);
        }

        void ApplyMotion(Vector3 pos, Vector3 euler)
        {
            if (motion == null) return;
            motion.localPosition = pos;
            motion.localRotation = Quaternion.Euler(euler);
        }

        void OnDied()
        {
            SetState(StageState.GameOver);
            if (player != null) player.Running = false;
            if (judge != null) judge.ClearAll();
        }

        /// <summary>스테이지 클리어 (T07에서 무사를 쓰러뜨리면 호출).</summary>
        public void Clear()
        {
            SetState(StageState.Cleared);
            if (player != null) player.Running = false;
        }

        /// <summary>전투 상태로 바꾸거나 달리기로 돌아간다 (CombatDirector가 사용).</summary>
        public void EnterCombat() { SetState(StageState.Combat); player.Running = false; }
        public void ExitCombat() { if (State == StageState.Combat) { SetState(StageState.Running); player.Running = true; } }
    }
}
