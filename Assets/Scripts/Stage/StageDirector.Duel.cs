using System;
using System.Linq;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>무사 결투 규칙 (GDD-beta B11): 내 공격, 자세 게이지, 단계별 되감기.</summary>
    public partial class StageDirector
    {
        /// <summary>3단계가 끝나 자세가 무너졌을 때 (B11.3).</summary>
        public event Action<Encounter> PostureBroken;
        /// <summary>결투 단계 되감기 (되감는 단계 번호).</summary>
        public event Action<int> DuelPhaseRewound;
        /// <summary>B13.4: 받아치기 성공 → 두 사람 자리 바꾸기 시작.</summary>
        public event Action<Encounter> DuelSwapped;
        /// <summary>B13.4: 옆차기에 맞아 쓰러짐 (판정 없음, 체력 그대로).</summary>
        public event Action<Encounter> DuelKicked;

        /// <summary>무사가 막는 판정 (내 공격·칼 겨루기).</summary>
        static bool IsGuarded(CombatStep s) => s.Kind == CombatStepKind.Strike || s.Kind == CombatStepKind.Lock;

        void OnDuelStepJudged(CombatStep step, Judgement r)
        {
            var e = encounter;
            if (Rewinding || HitPending || e == null || e.Done) return;
            if (step.Phase > e.Phase) { e.Phase = step.Phase; e.PhasePosture = e.Posture; }
            if (r == Judgement.Fail) { RewindDuel(step.Phase); return; }

            if (step.Kind == CombatStepKind.Finisher)
            {
                var how = CombatRules.ExecutionFor(r, e.Event.onRoof, e.EnemyIndex);
                DefeatEnemies(how);
                if (r == Judgement.Perfect) TriggerStorySlowMotion();
                FinishEncounter(how);
                return;
            }

            e.Posture = Mathf.Min(GameConfig.PostureFull, e.Posture + BossDuel.PostureGain(r));
            var boss = ActorOf(step);
            // B11.2: 내 공격 PERFECT → 무사가 뒤로 밀림 (NORMAL에 내가 밀리는 것은 카메라 연출, PresentationHooks)
            if (IsGuarded(step) && r == Judgement.Perfect && boss != null && player != null)
                boss.Pushback(boss.transform.position - player.transform.position, GameConfig.StrikeBossPushMeters, GameConfig.StrikePushSeconds);

            // B13.4 사건: 받아치기 성공 → 자리 바꾸기 (이동은 ApplyMotion), 2단계 마지막 판정 뒤 → 옆차기 다운
            if (step.OffsetBeats == BossDuel.SwapBeat) { e.Swapped = true; DuelSwapped?.Invoke(e); }
            if (step.OffsetBeats == BossDuel.KickAfterBeat && !e.Kicked)
            {
                e.Kicked = true;
                e.KickRealTime = (float)clock.Now;   // 박자 시계 시각 (넘어지는 연출 기준)
                if (boss != null) boss.Kick();
                DuelKicked?.Invoke(e);
            }

            // B11.3: 3단계 마지막 판정 → 게이지를 가득 채우고 자세 무너짐
            if (step.Phase == BossDuel.PhaseFlurry && ReferenceEquals(step, e.Steps.Last(s => s.Phase == BossDuel.PhaseFlurry)))
            {
                e.Posture = GameConfig.PostureFull;
                e.PostureBroken = true;
                PostureBroken?.Invoke(e);
            }
        }

        /// <summary>결투 FAIL (B11.4): 체력은 그대로, 그 단계 처음으로 되감고 자세 게이지를 그때 값으로 돌린다.</summary>
        void RewindDuel(int phase)
        {
            var e = encounter;
            e.Phase = phase;
            e.Posture = e.PhasePosture;
            if (phase <= BossDuel.PhaseFlurry) e.PostureBroken = false;
            // B13.4: 다운 구간으로 되감으면 누운 채로 다시, 그 앞이면 아직 맞지 않은 상태
            e.Kicked = phase == BossDuel.PhaseDown;
            e.KickRealTime = -999f;
            if (phase <= BossDuel.PhaseClash) e.Swapped = false;
            if (e.Actors != null) foreach (var a in e.Actors) if (a != null && !a.Defeated) { a.EndAttack(Judgement.Fail); a.EndGuard(); }
            DuelPhaseRewound?.Invoke(phase);
            HitThenRewind(RewindKind.Boss, e.StartBeat + BossDuel.PhaseStart(phase), () =>   // B12.2: 맞는 연출 뒤 되감기
            {
                e.Attempts++;
                e.Cues.Clear();
                foreach (var s in e.Steps)
                    if (s.Phase >= phase) e.Cues.Add(judge.Schedule(e.StartBeat + s.OffsetBeats, s.Key, AttackPoint(s), s));
            });
        }
    }
}
