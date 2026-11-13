using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 전투 진행 (GDD 4.5). 적 위치에 도착하면 멈추고, 다음 마디부터 공격 판정을 예약한다.
    /// FAIL이면 체력 −1 후 다음 마디부터 그 적의 첫 공격부터 다시. 마무리에 성공하면 쓰러뜨리고 다음 마디 첫 박에 다시 달린다.
    /// </summary>
    public partial class StageDirector
    {
        /// <summary>차트 순서대로 일반 적 4명 (SceneBuilder가 연결).</summary>
        public EnemyActor[] normalEnemies;
        public SlowMotion slowMotion;

        public class Encounter
        {
            public ChartEvent Event;
            public EnemyActor[] Actors;
            public List<CombatStep> Steps;
            public int EnemyIndex;
            public double StartBeat;          // 첫 공격 목표 박자
            public double ArriveBeat;         // 적 앞에 도착한 박자 (대치 연출 시작)
            public readonly List<JudgeCue> Cues = new List<JudgeCue>();
            public int Attempts;              // 시작 = 1, 재도전마다 +1
            public bool Done;
            public double ResumeBeat;
            public ExecutionKind Execution;
        }

        Encounter encounter;
        int normalEnemyCount;

        public Encounter CurrentEncounter => encounter;
        /// <summary>마지막으로 끝난 전투 (테스트·연출용).</summary>
        public Encounter LastEncounter { get; private set; }

        public event Action<Encounter> EncounterStarted;
        public event Action<Encounter, ExecutionKind> EnemyDefeated;

        partial void ResetCombat()
        {
            encounter = null;
            LastEncounter = null;
            normalEnemyCount = 0;
            if (slowMotion != null) slowMotion.Cancel();
            if (normalEnemies != null) foreach (var e in normalEnemies) if (e != null) e.ResetActor();
            // 시작 위치보다 앞에 있던 적 수를 세어 번호를 맞춘다
            if (Chart != null)
                for (int i = 0; i < nextEvent && i < Chart.events.Length; i++)
                    if (Chart.events[i].IsDuel) normalEnemyCount++;
            ResetCombatExtra();
        }

        partial void ResetCombatExtra();
        partial void TryStartSpecialCombat(ChartEvent ev, ref bool handled);

        partial void TryStartCombat(ChartEvent ev, ref bool handled)
        {
            if (ev.IsDuel)
            {
                int index = normalEnemyCount++;
                var actor = normalEnemies != null && index < normalEnemies.Length ? normalEnemies[index] : null;
                StartEncounter(ev, CombatRules.NormalEnemySteps(ev), new[] { actor }, index);
                handled = true;
                return;
            }
            TryStartSpecialCombat(ev, ref handled);
        }

        /// <summary>전투를 시작한다 (포위·무사도 같은 흐름, T07).</summary>
        public void StartEncounter(ChartEvent ev, List<CombatStep> steps, EnemyActor[] actors, int index)
        {
            EnterCombat();
            encounter = new Encounter { Event = ev, Steps = steps, Actors = actors, EnemyIndex = index, ArriveBeat = clock.CurrentBeat };
            ScheduleEncounter();
            EncounterStarted?.Invoke(encounter);
        }

        void ScheduleEncounter()
        {
            var e = encounter;
            e.Attempts++;
            e.Cues.Clear();
            e.StartBeat = CombatRules.EncounterStartBeat(clock.CurrentBeat);   // GDD-beta B10.2 (GDD 4.5의 다음 마디 대신)
            foreach (var s in e.Steps)
                e.Cues.Add(judge.Schedule(e.StartBeat + s.OffsetBeats, s.Key, AttackPoint(s), s));
        }

        Vector3? AttackPoint(CombatStep s)
        {
            var a = ActorOf(s);
            if (a == null) return null;
            if (a.weaponTip != null && s.Kind == CombatStepKind.Attack) return a.weaponTip.position;
            return a.transform.position + Vector3.up * 0.4f;   // 마무리: 적의 가슴
        }

        EnemyActor ActorOf(CombatStep s)
        {
            if (encounter == null || encounter.Actors == null || s.Attackers == null || s.Attackers.Length == 0) return null;
            int i = s.Attackers[0];
            return i >= 0 && i < encounter.Actors.Length ? encounter.Actors[i] : null;
        }

        partial void UpdateCombat()
        {
            if (encounter == null) return;
            if (encounter.Done)
            {
                if (clock.CurrentBeat >= encounter.ResumeBeat)
                {
                    LastEncounter = encounter;
                    encounter = null;
                    ExitCombat();
                }
                return;
            }
            double beat = clock.CurrentBeat;
            // 대치 (GDD-beta B10.2): 첫 공격 전까지 적이 0.5 m 다가오며 칼을 든다 (포위 전투 제외)
            if (!encounter.Event.IsSurround && encounter.Actors != null && beat < encounter.StartBeat)
            {
                double span = System.Math.Max(0.25, encounter.StartBeat - encounter.ArriveBeat);
                float sp = Mathf.Clamp01((float)((beat - encounter.ArriveBeat) / span));
                foreach (var a in encounter.Actors) if (a != null) a.Standoff(player.transform.position, sp);
            }
            // 원이 떠 있는 공격: 적이 무기를 휘두르고, 판정 원과 카메라가 무기 끝을 따라간다
            foreach (var cue in encounter.Cues)
            {
                if (cue.Judged || !cue.Appeared || !(cue.Tag is CombatStep s) || s.Kind != CombatStepKind.Attack) continue;
                float p = (float)(1.0 - (cue.TargetBeat - beat) / GameConfig.ApproachBeats);
                foreach (var idx in s.Attackers)
                    if (encounter.Actors != null && idx < encounter.Actors.Length && encounter.Actors[idx] != null)
                        encounter.Actors[idx].Attack(s.Attack, p);
                var a = ActorOf(s);
                if (a != null && a.weaponTip != null) cue.WorldPoint = a.weaponTip.position;
            }
        }

        partial void OnCombatCueJudgedImpl(JudgeCue cue, Judgement r)
        {
            if (encounter == null || encounter.Done || !(cue.Tag is CombatStep step)) return;
            if (!encounter.Cues.Contains(cue)) return;

            if (step.Kind == CombatStepKind.Attack)
                foreach (var idx in step.Attackers)
                    if (encounter.Actors != null && idx < encounter.Actors.Length && encounter.Actors[idx] != null)
                        encounter.Actors[idx].EndAttack(r);

            OnCombatStepJudged(step, r);
        }

        /// <summary>판정 하나의 결과를 전투 규칙에 반영한다. 포위 전투(T07)는 별도 규칙을 덧붙인다.</summary>
        void OnCombatStepJudged(CombatStep step, Judgement r)
        {
            bool handled = false;
            OnSpecialStepJudged(step, r, ref handled);
            if (handled) return;

            if (r == Judgement.Fail)
            {
                // GDD-beta B9.2: 무사는 체력을 깎지 않고 첫 공격 1마디 앞으로 되감는다
                if (encounter.Event.IsBoss) { RewindBoss(); return; }
                // GDD-beta B9.1: 일반 적은 체력 −1 후 다음 공격으로 (다시 하지 않음). 마무리 FAIL은 밀쳐 쓰러뜨리기로 끝
                Health.Damage();
                if (Health.IsDead || Rewinding || encounter == null) return;
                if (step.Kind == CombatStepKind.Finisher)
                {
                    DefeatEnemies(ExecutionKind.PushDown);
                    FinishEncounter(ExecutionKind.PushDown);
                }
                return;
            }
            if (step.Kind == CombatStepKind.Finisher)
            {
                var how = CombatRules.ExecutionFor(r, encounter.Event.onRoof, encounter.EnemyIndex);
                DefeatEnemies(how);
                if (r == Judgement.Perfect) TriggerStorySlowMotion();
                FinishEncounter(how);
            }
        }

        partial void OnSpecialStepJudged(CombatStep step, Judgement r, ref bool handled);

        /// <summary>무사 FAIL (GDD-beta B9.2): 첫 공격 1마디 앞으로 되감고, 같은 박자에 공격을 처음부터 다시 예약한다.</summary>
        void RewindBoss()
        {
            var e = encounter;
            if (e.Actors != null) foreach (var a in e.Actors) if (a != null && !a.Defeated) a.EndAttack(Judgement.Fail);
            BeginRewind(RewindKind.Boss, e.StartBeat - GameConfig.RewindLeadBeats, () =>
            {
                e.Attempts++;
                e.Cues.Clear();
                foreach (var s in e.Steps) e.Cues.Add(judge.Schedule(e.StartBeat + s.OffsetBeats, s.Key, AttackPoint(s), s));
            });
        }

        /// <summary>남은 판정을 취소하고 다음 마디부터 첫 공격부터 다시.</summary>
        public void RetryEncounter()
        {
            foreach (var c in encounter.Cues) judge.Cancel(c);
            if (encounter.Actors != null) foreach (var a in encounter.Actors) if (a != null && !a.Defeated) a.EndAttack(Judgement.Fail);
            ScheduleEncounter();
        }

        void DefeatEnemies(ExecutionKind how)
        {
            if (encounter.Actors == null) return;
            foreach (var a in encounter.Actors)
                if (a != null && !a.Defeated)
                    a.Defeat(how, a.transform.position - player.transform.position);
        }

        void FinishEncounter(ExecutionKind how)
        {
            encounter.Done = true;
            encounter.Execution = how;
            encounter.ResumeBeat = CombatRules.NextBar(clock.CurrentBeat);
            foreach (var c in encounter.Cues) judge.Cancel(c);
            EnemyDefeated?.Invoke(encounter, how);
            OnEncounterFinished(encounter);
        }

        partial void OnEncounterFinished(Encounter e);
    }
}
