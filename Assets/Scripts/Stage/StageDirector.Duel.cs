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
