using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 게임 이벤트 → 연출 연결 (GDD-beta B5, 제안서 2장 A~G). 게임 코드를 바꾸지 않고 이벤트만 듣는다.
    /// - A 공격 예고: 적 공격 목표 0.5박 전(휘두르기 직전) AttackWarn, 마무리 원이 뜨면 화면을 더 가라앉힘
    /// - B/C/D 방어·회피: PERFECT(받아 막기 ↑ = 패링, 그 밖 = 회피) / NORMAL / FAIL(피격)
    /// - E 처형: 적이 쓰러질 때 (발차기는 아래로)
    /// - F 포위: 1단계 회전 롤, 2단계 낮게·넓게, 윈드밀 화살표마다 FOV, 마무리
    /// - G 파쿠르: 도약 PERFECT, 사건이 끝날 때 착지(결과별), FAIL은 흔들림 + 붉은 테두리 0.3
    /// 슬로모션: 처형·윈드밀은 StageDirector가 이미 걸므로 여기서는 카메라·화면·이펙트만 건다.
    /// 사건 PERFECT는 StageDirector가 빈도 제한을 통과했을 때만 EventPerfectSlow, 전투 후보(포위 2단계. 무사 결투는 GDD-beta B11로 바뀌어 마무리 처형만 슬로모션)는 여기서 요청한다.
    /// </summary>
    public class PresentationHooks : MonoBehaviour
    {
        public PresentationDirector presentation;
        public StageDirector director;
        public JudgeSystem judge;
        public BeatClock clock;
        public PathFollower player;
        public VfxPool vfx;

        readonly HashSet<JudgeCue> warned = new HashSet<JudgeCue>();
        StageDirector.ObstacleRun lastSeenRun;
        readonly List<ScreenPulse> tmp = new List<ScreenPulse>();

        /// <summary>마지막으로 건 연출 이름 (테스트용).</summary>
        public readonly List<string> Log = new List<string>();

        void OnEnable()
        {
            if (judge != null) { judge.CueAppeared += OnAppeared; judge.CueJudged += OnJudged; }
            if (director != null)
            {
                director.EventSlotJudged += OnSlotJudged;
                director.EventSlowMotionStarted += OnEventSlow;
                director.EnemyDefeated += OnDefeated;
                director.StageReset += OnReset;
                director.RewindStarted += OnRewind;
                director.EncounterStarted += OnEncounter;
                director.PostureBroken += OnPostureBroken;
            }
        }

        void OnDisable()
        {
            if (judge != null) { judge.CueAppeared -= OnAppeared; judge.CueJudged -= OnJudged; }
            if (director != null)
            {
                director.EventSlotJudged -= OnSlotJudged;
                director.EventSlowMotionStarted -= OnEventSlow;
                director.EnemyDefeated -= OnDefeated;
                director.StageReset -= OnReset;
                director.RewindStarted -= OnRewind;
                director.EncounterStarted -= OnEncounter;
                director.PostureBroken -= OnPostureBroken;
            }
        }

        /// <summary>대치 연출 (GDD-beta B10.2): 멈춰서 싸우는 전투가 시작되면 카메라가 적 쪽으로 살짝 다가간다.</summary>
        void OnEncounter(StageDirector.Encounter e) => Play(GameConfig.Fx.Standoff);

        /// <summary>
        /// 되감기 연출 (GDD-beta B9.4, B10.3): 카메라 뒤로 0.3 m, 채도 −90·노출 −0.5, 거꾸로 흐르는 먹 줄기, 되감기 소리,
        /// 그리고 테이프 되감기 겹화면(먹 줄기 3개, 줄무늬, ◀◀).
        /// </summary>
        void OnRewind(RewindKind kind)
        {
            Play(GameConfig.Fx.Rewind, 1f, null, player != null ? player.transform.position : (Vector3?)null);
            if (vfx != null && presentation != null && presentation.Enabled) { vfx.Rewind.Play(); Log.Add("RewindOverlay"); }
        }

        /// <summary>무사 자세 무너짐 (GDD-beta B11.3): 붉은 번쩍임 + 충돌음.</summary>
        void OnPostureBroken(StageDirector.Encounter e) =>
            Play(GameConfig.Fx.PostureBreak, 1f, null, e.Actors != null && e.Actors.Length > 0 && e.Actors[0] != null ? e.Actors[0].transform.position + Vector3.up * 1.2f : (Vector3?)null);

        void OnReset() { warned.Clear(); lastSeenRun = null; Log.Clear(); }

        static FxPreset NoSlow(FxPreset p) { p.SlowMo = SlowMoMode.None; return p; }

        void Play(FxPreset p, float side = 1f, IList<IHitStoppable> targets = null, Vector3? pos = null)
        {
            if (presentation == null) return;
            presentation.Play(p, side, targets, pos);
            Log.Add(p.Name);
        }

        bool InSurround => director != null && director.CurrentEncounter != null && director.CurrentEncounter.Event.IsSurround;

        static float SideOf(ArrowKey k) => k == ArrowKey.Left ? -1f : k == ArrowKey.Right ? 1f : 1f;

        IList<IHitStoppable> Attackers(CombatStep s)
        {
            var enc = director != null ? director.CurrentEncounter : null;
            var list = new List<IHitStoppable>();
            if (enc == null || enc.Actors == null) return list;
            foreach (int i in s.Attackers)
                if (i >= 0 && i < enc.Actors.Length && enc.Actors[i] != null) list.Add(enc.Actors[i]);
            return list;
        }

        Vector3? AttackerPos(CombatStep s)
        {
            var enc = director != null ? director.CurrentEncounter : null;
            if (enc == null || enc.Actors == null || s.Attackers.Length == 0) return null;
            int i = s.Attackers[0];
            if (i < 0 || i >= enc.Actors.Length || enc.Actors[i] == null) return null;
            var a = enc.Actors[i];
            return a.weaponTip != null ? a.weaponTip.position : a.transform.position + Vector3.up * 1.4f;
        }

        // ---------------- A. 예고, F 포위 회전 ----------------
        void OnAppeared(JudgeCue cue)
        {
            if (!(cue.Tag is CombatStep s)) return;
            if (JudgeRingView.IsFinisher(cue) && s.Kind == CombatStepKind.Finisher && presentation != null && presentation.screenFx != null && presentation.Enabled)
            {
                // E 예고: 마무리 원이 떠 있는 동안 화면을 더 가라앉힌다 (2박)
                tmp.Clear();
                FxLooks.FinisherWarn(tmp, GameConfig.ApproachBeats * GameConfig.SecondsPerBeat / (clock != null ? clock.SpeedMultiplier : 1f));
                presentation.screenFx.Add(tmp);
                Log.Add("FinisherWarn");
            }
            if (!InSurround) return;                                     // Phase는 포위 전투 단계 (무사 결투의 Phase는 결투 단계, B11)
            if (s.Phase == 1) Play(GameConfig.Fx.SurroundTurn, SideOf(s.Key));
            else if (s.Phase == 2) Play(GameConfig.Fx.Simultaneous);
        }

        void Update()
        {
            if (judge == null || clock == null) return;
            double beat = clock.CurrentBeat;
            foreach (var c in judge.Cues)
            {
                if (c.Judged || c.Cancelled || !c.Appeared || warned.Contains(c)) continue;
                if (!(c.Tag is CombatStep s) || s.Kind != CombatStepKind.Attack) continue;
                if (beat < c.TargetBeat - GameConfig.Fx.AttackWarnLeadBeats) continue;
                warned.Add(c);
                Play(GameConfig.Fx.AttackWarn, SideOf(s.Key));
            }
            // G. 사건이 끝나면 착지 연출 (마지막 판정 결과)
            var last = director != null ? director.LastRun : null;
            if (last != null && last != lastSeenRun)
            {
                lastSeenRun = last;
                if (IsJumpKind(last) && last.HasResult)
                {
                    var r = last.Result;
                    Play(r == Judgement.Perfect ? GameConfig.Fx.LandPerfect : r == Judgement.Normal ? GameConfig.Fx.LandNormal : GameConfig.Fx.LandFail,
                        1f, null, player != null ? player.transform.position : (Vector3?)null);
                }
            }
        }

        /// <summary>도약·착지가 있는 사건 (뛰어넘기·오르기·뛰어내리기·지붕 사건).</summary>
        static bool IsJumpKind(StageDirector.ObstacleRun run)
        {
            if (run.HasCategory) return run.Category == ObstacleCategory.Vault || run.Category == ObstacleCategory.Climb || run.Category == ObstacleCategory.Drop;
            switch (run.Def.Kind)
            {
                case "long_jump": case "awning_bounce": case "banner_slide": case "rope_swing": case "tile_slip": return true;
                default: return false;
            }
        }

        // ---------------- B/C/D 전투, F 윈드밀 화살표 ----------------
        void OnJudged(JudgeCue cue, Judgement r)
        {
            warned.Remove(cue);
            if (director != null && director.Rewinding) return;   // 되감기(GDD-beta B9)가 이 실패의 연출을 대신한다
            if (!(cue.Tag is CombatStep s)) return;
            var pos = AttackerPos(s) ?? cue.WorldPoint;
            if (s.Kind == CombatStepKind.Windmill)
            {
                if (r != Judgement.Fail) Play(GameConfig.Fx.WindmillArrow, 1f, null, pos);
                else Play(GameConfig.Fx.Hit, 1f);
                return;
            }
            if (s.Kind == CombatStepKind.Finisher)
            {
                // 처형 연출은 적이 쓰러질 때(OnDefeated). FAIL은 피격
                if (r == Judgement.Fail) Play(GameConfig.Fx.Hit, 1f);
                return;
            }
            float side = SideOf(s.Key);
            if (s.Kind == CombatStepKind.Strike || s.Kind == CombatStepKind.Lock)
            {
                // 내 공격·칼 겨루기 (GDD-beta B11.2): PERFECT 칼끼리 충돌 + 무사 밀림, NORMAL 내가 밀림. FAIL(헛침)은 되감기가 대신한다
                if (r != Judgement.Fail) Play(r == Judgement.Perfect ? GameConfig.Fx.StrikePerfect : GameConfig.Fx.StrikeNormal, side, Attackers(s), pos);
                return;
            }
            if (r == Judgement.Fail) { Play(GameConfig.Fx.Hit, side, Attackers(s), pos); return; }
            if (r == Judgement.Normal) { Play(GameConfig.Fx.DefendNormal, side, Attackers(s), pos); return; }
            // PERFECT
            if (s.Phase == 2 && InSurround) { Play(GameConfig.Fx.SimultaneousPerfect, 1f, Attackers(s), pos); return; }      // 슬로모션 후보
            Play(s.Key == ArrowKey.Up ? GameConfig.Fx.ParryPerfect : GameConfig.Fx.DodgePerfect, side, Attackers(s), pos);
        }

        // ---------------- E 처형, F 윈드밀 마무리 ----------------
        void OnDefeated(StageDirector.Encounter enc, ExecutionKind how)
        {
            var targets = new List<IHitStoppable>();
            Vector3? pos = null;
            if (enc.Actors != null)
                foreach (var a in enc.Actors) if (a != null) { targets.Add(a); pos = pos ?? a.transform.position + Vector3.up; }
            switch (how)
            {
                case ExecutionKind.Windmill:
                    Play(NoSlow(GameConfig.Fx.WindmillFinish), 1f, targets, player != null ? player.transform.position : pos);
                    break;
                case ExecutionKind.KickOffRoof:
                    Play(NoSlow(GameConfig.Fx.ExecutionKick), 1f, targets, pos);
                    break;
                case ExecutionKind.DisarmSlash:
                case ExecutionKind.StabWithOwnSword:
                    Play(NoSlow(GameConfig.Fx.Execution), 1f, targets, pos);
                    break;
                case ExecutionKind.PushDown:
                    Play(GameConfig.Fx.DefendNormal, 1f, targets, pos);
                    break;
            }
        }

        // ---------------- G 파쿠르·사건 ----------------
        void OnSlotJudged(StageDirector.ObstacleRun run, int slot, Judgement r)
        {
            var at = run.Cues.Count > slot && run.Cues[slot].WorldPoint.HasValue ? run.Cues[slot].WorldPoint : null;
            if (r == Judgement.Fail) { Play(GameConfig.Fx.ParkourFail, 1f, null, player != null ? player.transform.position : at); return; }
            if (run.Def.Kind == "tile_slip" && slot == 0 && vfx != null && presentation != null && presentation.Enabled)
            {
                vfx.Play(VfxKind.TileDebris, player != null ? player.transform.position : at, 2);   // 기왓장이 빠짐
                Log.Add("TileDebris");
            }
            if (r == Judgement.Perfect && slot == 0 && IsJumpKind(run) && run.Def.SlowMoSlot != slot) Play(GameConfig.Fx.JumpPerfect, 1f);
        }

        void OnEventSlow(StageDirector.ObstacleRun run)
        {
            Play(NoSlow(GameConfig.Fx.EventPerfectSlow), 1f, null, player != null ? player.transform.position + player.transform.forward * 3f : (Vector3?)null);
        }
    }
}
