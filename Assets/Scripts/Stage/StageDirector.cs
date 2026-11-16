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
        /// <summary>판정 하나 (원 포함, 작업 R25: 장구·방향키 점수를 나누려고).</summary>
        public event Action<JudgeCue, Judgement> AnyCueJudged;

        /// <summary>진행 중인 사건 하나 (GDD-beta B2.1). 장애물·새 사건·안 멈추는 적 모두.</summary>
        public class ObstacleRun
        {
            public ChartEvent Event;
            public EventDef Def;
            public List<EventSlot> Slots;
            public readonly List<JudgeCue> Cues = new List<JudgeCue>();
            public Judgement?[] Results;
            /// <summary>첫 판정 (기존 코드·테스트 호환).</summary>
            public JudgeCue Cue => Cues.Count > 0 ? Cues[0] : null;
            /// <summary>GDD 4.4 기존 장애물 분류. 새 사건이면 기본값(Vault)이고 HasCategory=false.</summary>
            public ObstacleCategory Category;
            public bool HasCategory;
            public float Side;
            public bool Started;
            /// <summary>판정이 하나라도 끝났는지.</summary>
            public bool HasResult;
            /// <summary>가장 최근에 끝난 판정 결과.</summary>
            public Judgement Result;
            /// <summary>이 사건에서 잃은 체력.</summary>
            public int HpLost;
            public double StartBeat => Event.runBeat;
            public double EndBeat => Event.runBeat + Def.Beats;
            public List<double> SlotOffsets = new List<double>();
            public List<ArrowKey> SlotKeys = new List<ArrowKey>();
            public bool AllJudged { get { foreach (var r in Results) if (!r.HasValue) return false; return true; } }
        }

        readonly List<ObstacleRun> pending = new List<ObstacleRun>();
        ObstacleRun current;
        int nextEvent;

        public ObstacleRun Current => current;
        /// <summary>마지막으로 끝난 사건 (테스트·연출용).</summary>
        public ObstacleRun LastRun { get; private set; }
        /// <summary>예약됐지만 아직 시작하지 않은 사건들.</summary>
        public IReadOnlyList<ObstacleRun> PendingRuns => pending;

        /// <summary>사건 안 판정 하나가 끝날 때 (사건, 판정 번호, 결과).</summary>
        public event Action<ObstacleRun, int, Judgement> EventSlotJudged;
        /// <summary>PERFECT 슬로모션 후보 판정이 PERFECT일 때 (GDD-beta B4). 연출 시스템(B05)이 빈도 제한을 건다.</summary>
        public event Action<ObstacleRun> EventSlowMotionRequested;
        /// <summary>사건 슬로모션이 받아들여져 시작됐을 때 (빈도 제한 통과).</summary>
        public event Action<ObstacleRun> EventSlowMotionStarted;

        /// <summary>지금 통과 연출 상태 이름: "None", "Pending"(결과 대기), 또는 "Vault_Perfect" 같은 이름.</summary>
        public string TraversalState =>
            current == null ? "None" : !current.HasResult ? "Pending" : RunStateName(current);

        /// <summary>사건 상태 이름. 기존 장애물은 "Vault_Perfect", 새 사건·적은 "rope_swing_Perfect" 꼴.</summary>
        public static string RunStateName(ObstacleRun run) =>
            run.HasCategory ? Traversal.StateName(run.Category, run.Result) : $"{run.Def.Kind}_{run.Result}";

        /// <summary>지금 통과 진행도 (0~1, 사건 길이 기준). 화면에 보이는 위치로 계산한다.</summary>
        public float TraversalProgress => current == null ? 0f : Mathf.Clamp01((float)((player.VisualRunBeat - current.StartBeat) / current.Def.Beats));

        void Awake()
        {
            Health.Died += OnDied;
        }

        void OnEnable() { if (judge != null) judge.CueJudged += OnCueJudged; }
        void OnDisable() { if (judge != null) judge.CueJudged -= OnCueJudged; }

        /// <summary>테스트·도구용: 다음 ResetStage부터 이 차트를 쓴다.</summary>
        public void UseChart(ChartData chart) => Chart = chart;

        /// <summary>runBeat 위치부터 스테이지를 다시 시작한다.</summary>
        public void ResetStage(double runBeat)
        {
            if (Chart == null) Chart = StageChart.Load();
            judge.ClearAll();
            pending.Clear();
            current = null;
            LastRun = null;
            Rewinding = false;
            HitPending = false;
            Health.Reset();
            nextEvent = 0;
            while (nextEvent < Chart.events.Length && Chart.events[nextEvent].runBeat < runBeat) nextEvent++;
            rhythm = JangdanEnabled ? RhythmNotes.Generate(Chart) : new System.Collections.Generic.List<RhythmNote>();   // 달리기 장단 노드 (GDD-beta B18.2)
            nextRhythm = 0;
            while (nextRhythm < rhythm.Count && rhythm[nextRhythm].RunBeat < runBeat) nextRhythm++;
            ResetCombat();
            SetState(StageState.Running);
            player.Running = true;
            ApplyMotion(Vector3.zero, Vector3.zero);
            StageReset?.Invoke();
        }

        /// <summary>ResetStage가 끝났을 때 (사건 소품 초기화용).</summary>
        public event Action StageReset;

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
            if (Chart == null || clock == null) return;
            UpdateMotion();                                  // 움직이며 싸우는 전투 (GDD-beta B13)
            UpdateHitPending();                              // 피격 연출 뒤 되감기 (GDD-beta B12.2)
            if (Rewinding) { UpdateRewind(); return; }   // 되감기 연출 중 (GDD-beta B9.4)
            if (!clock.IsRunning) return;
            if (State == StageState.GameOver || State == StageState.Cleared) return;
            if (State == StageState.Running) ScheduleUpcoming();
            UpdateTraversal();
            UpdateCombat();
        }

        System.Collections.Generic.List<RhythmNote> rhythm = new System.Collections.Generic.List<RhythmNote>();
        int nextRhythm;

        /// <summary>달리기 장단 노드를 쓰는지 (기본 켬). 다른 규칙만 보는 테스트가 끈다. ResetStage에서 반영.</summary>
        public bool JangdanEnabled = true;

        /// <summary>이 스테이지의 달리기 장단 노드 (GDD-beta B18.2, 테스트용).</summary>
        public System.Collections.Generic.IReadOnlyList<RhythmNote> RhythmNoteList => rhythm;

        /// <summary>
        /// 달리기 장단 노드를 3박 앞까지 예약한다 (GDD-beta B18.2). 다음 멈추는 전투 자리 뒤의 노드는 그 전투가 끝난 뒤에 예약한다
        /// (전투 동안 달리기 박자가 멈추므로 시계 박자가 그만큼 뒤로 밀린다). 결과는 속도 배율에 반영하지 않는다.
        /// </summary>
        void ScheduleRhythm()
        {
            double block = double.MaxValue;
            for (int i = nextEvent; i < Chart.events.Length; i++)
            {
                var e = Chart.events[i];
                if (!e.IsRunEvent && !e.IsForeshadow) { block = e.runBeat; break; }
            }
            while (nextRhythm < rhythm.Count)
            {
                var n = rhythm[nextRhythm];
                if (n.RunBeat >= block) return;
                if (n.RunBeat < player.RunBeat + 0.01) { nextRhythm++; continue; }   // 이미 지난 자리
                if (n.RunBeat - player.RunBeat > GameConfig.Jangdan.ScheduleAheadBeats) return;
                var point = MapLayout.PointAt((float)((n.RunBeat + 1) * GameConfig.MetersPerBeat)) + Vector3.up * 1.2f;
                var cue = judge.Schedule(player.ClockBeatOfRun(n.RunBeat), n.Key, point, n);
                cue.AffectsSpeed = false;
                nextRhythm++;
            }
        }

        void ScheduleUpcoming()
        {
            ScheduleRhythm();
            while (nextEvent < Chart.events.Length)
            {
                var ev = Chart.events[nextEvent];
                double ahead = ev.runBeat - player.RunBeat;
                if (ev.IsForeshadow) { nextEvent++; continue; }   // 복선은 판정 없음 (EventPropDirector가 보여 준다)
                if (!ev.IsRunEvent)
                {
                    // 멈추는 전투(대치·포위·무사): 적 위치에 도착하면 시작
                    if (ahead > 0) return;
                    bool handled = false;
                    TryStartCombat(ev, ref handled);
                    nextEvent++;
                    if (handled) return;
                    continue;
                }
                if (ahead > GameConfig.ApproachBeats + 1) return;
                pending.Add(CreateRun(ev));
                nextEvent++;
            }
        }

        /// <summary>사건 하나를 만들고 판정을 모두 예약한다. 판정 목표 = 사건 시작 + 판정 박자 (달리기 박자 → 시계 박자).</summary>
        ObstacleRun CreateRun(ChartEvent ev)
        {
            var def = EventCatalog.Get(ev.EventKind);
            var run = new ObstacleRun
            {
                Event = ev,
                Def = def,
                Slots = EventCatalog.SlotsFor(ev),
                HasCategory = def.Category.HasValue,
                Category = def.Category ?? ObstacleCategory.Vault,
                Side = ev.Key == ArrowKey.Left ? -1f : ev.Key == ArrowKey.Right ? 1f : 0f,
            };
            run.Results = new Judgement?[run.Slots.Count];
            var point = MapLayout.PointAt((ev.runBeat + def.Beats * 0.5f) * GameConfig.MetersPerBeat) + Vector3.up;
            foreach (var s in run.Slots)
            {
                run.SlotOffsets.Add(s.Offset);
                run.SlotKeys.Add(s.Key.Value);
                double runTarget = ev.runBeat + s.Offset;
                run.Cues.Add(judge.Schedule(player.ClockBeatOfRun(runTarget), s.Key.Value, point, run));
            }
            return run;
        }

        void OnCueJudged(JudgeCue cue, Judgement r)
        {
            AnyJudged?.Invoke(r);
            AnyCueJudged?.Invoke(cue, r);
            if (cue.Tag is ObstacleRun run)
            {
                int slot = run.Cues.IndexOf(cue);
                if (slot >= 0) run.Results[slot] = r;
                run.HasResult = true;
                run.Result = r;
                ObstacleResolved?.Invoke(run.Event, r);
                if (r == Judgement.Fail && (!run.Def.HpCapOne || run.HpLost == 0))
                {
                    run.HpLost++;
                    Health.Damage();
                }
                EventSlotJudged?.Invoke(run, slot, r);
                if (r == Judgement.Perfect && slot == run.Def.SlowMoSlot) RequestEventSlowMotion(run);
                return;
            }
            OnCombatCueJudged(cue, r);
        }

        /// <summary>
        /// 사건 PERFECT 슬로모션 (GDD-beta B4): 시간 배율 0.3을 실제 0.4초, 이동은 사건 끝까지 재배치.
        /// 빈도 제한(16박)은 연출 시스템(B05)이 맡는다. 여기서는 기능만 제공한다.
        /// </summary>
        public bool AllowEventSlowMotion = true;

        void RequestEventSlowMotion(ObstacleRun run)
        {
            EventSlowMotionRequested?.Invoke(run);
            if (!AllowEventSlowMotion || State != StageState.Running) return;
            bool accepted;
            if (presentation != null) accepted = presentation.RequestSlowMotion(SlowMoMode.Candidate);   // 16박 빈도 제한 (B05)
            else { accepted = GameConfig.BulletTime.PerfectCandidateSlowMo; if (accepted && slowMotion != null) slowMotion.Trigger(); }   // B17.4: PERFECT 뒤 슬로모션 없앰
            if (accepted) { player.BeginRemap(run.EndBeat, clock.SpeedMultiplier); EventSlowMotionStarted?.Invoke(run); }
        }

        /// <summary>연출 시스템 (GDD-beta B5). 없으면 슬로모션을 직접 건다.</summary>
        public PresentationDirector presentation;

        /// <summary>처형·윈드밀 슬로모션 (GDD 4.2): 빈도 제한 없이 항상. 연출 시스템이 있으면 거기에 기록한다.</summary>
        void TriggerStorySlowMotion()
        {
            if (presentation != null) presentation.RequestSlowMotion(SlowMoMode.Always);
            else if (slowMotion != null) slowMotion.Trigger();
        }

        partial void OnCombatCueJudgedImpl(JudgeCue cue, Judgement r);
        void OnCombatCueJudged(JudgeCue cue, Judgement r) => OnCombatCueJudgedImpl(cue, r);

        void UpdateTraversal()
        {
            if (current != null && player.RunBeat >= current.EndBeat)
            {
                LastRun = current;
                current = null;
                ApplyMotion(Vector3.zero, Vector3.zero);
            }
            if (current == null && pending.Count > 0 && player.RunBeat >= pending[0].StartBeat)
            {
                current = pending[0];
                pending.RemoveAt(0);
                current.Started = true;
            }
            if (current == null) return;
            if (!current.HasResult) { ApplyMotion(Vector3.zero, Vector3.zero); return; }
            Vector3 pos, euler;
            if (current.HasCategory)
                Traversal.Offset(current.Category, current.Result, current.Side, TraversalProgress, out pos, out euler);
            else
                EventMotion.Offset(current.Def.Kind, current.SlotOffsets, current.SlotKeys, current.Results, current.Def.Beats, TraversalProgress, out pos, out euler);
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
            if (AllowDeathRewind) { RewindToChapter(); return; }   // GDD-beta B9.3: 게임 오버 대신 챕터 처음으로
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
