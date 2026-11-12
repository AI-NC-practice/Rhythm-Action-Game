using System.Linq;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>포위 전투와 최종 무사 (GDD 4.6, 4.5).</summary>
    public partial class StageDirector
    {
        /// <summary>포위 전투 적 5명 (칼 3, 창 2).</summary>
        public EnemyActor[] surroundEnemies;
        public EnemyActor boss;

        /// <summary>포위 1단계에서 공격하는 적 쪽으로 카메라가 도는 각도 (도, 진행 방향 기준 yaw).</summary>
        public float SurroundYaw { get; private set; }
        /// <summary>윈드밀 마무리 카메라 회전이 지금까지 돈 각도 (0~360).</summary>
        public float WindmillSpinDegrees { get; private set; }
        public bool WindmillSpinning { get; private set; }
        /// <summary>2단계 동시 공격 동안 카메라가 낮게 깔린 정도 (m, 음수).</summary>
        public float SurroundCameraDrop { get; private set; }

        /// <summary>윈드밀 카메라 360° 회전 시간 (실제 초). GDD에 없음, Claude가 정함.</summary>
        public const float WindmillSpinSeconds = 0.8f;
        /// <summary>동시 공격 때 카메라를 낮추는 높이 (m). GDD에 없음, Claude가 정함.</summary>
        public const float SimultaneousCameraDrop = -0.6f;

        public System.Func<float> SpinDeltaTime = () => Time.unscaledDeltaTime;

        float yawFrom, yawTo, yawT = 1f;
        int windmillFails;
        public int WindmillAttempts { get; private set; }

        partial void ResetCombatExtra()
        {
            SurroundYaw = 0; WindmillSpinDegrees = 0; WindmillSpinning = false; SurroundCameraDrop = 0;
            yawT = 1f; WindmillAttempts = 0; windmillFails = 0;
            if (surroundEnemies != null) foreach (var e in surroundEnemies) if (e != null) e.ResetActor();
            if (boss != null) boss.ResetActor();
        }

        partial void TryStartSpecialCombat(ChartEvent ev, ref bool handled)
        {
            if (ev.IsSurround)
            {
                WindmillAttempts = 1;
                StartEncounter(ev, CombatPatterns.SurroundSteps(), surroundEnemies, 0);
                handled = true;
            }
            else if (ev.IsBoss)
            {
                StartEncounter(ev, CombatPatterns.BossSteps(ev), new[] { boss }, 0);
                handled = true;
            }
        }

        bool IsSurround => encounter != null && encounter.Event.IsSurround;

        partial void OnSpecialStepJudged(CombatStep step, Judgement r, ref bool handled)
        {
            if (!IsSurround) return;
            handled = true;
            if (step.Phase == 1 || step.Phase == 2)
            {
                // 1·2단계 FAIL: 체력 −1, 전투는 이어 간다
                if (r == Judgement.Fail) Health.Damage();
                return;
            }
            // 3단계 윈드밀
            if (r == Judgement.Fail)
            {
                Health.Damage();
                if (Health.IsDead) return;
                RetryWindmill();
                return;
            }
            bool allDone = encounter.Cues.Where(c => c.Tag is CombatStep s && s.Phase == 3 && !c.Cancelled).All(c => c.Judged);
            if (!allDone) return;
            DefeatEnemies(ExecutionKind.Windmill);
            if (slowMotion != null) slowMotion.Trigger();
            WindmillSpinning = true;
            WindmillSpinDegrees = 0;
            FinishEncounter(ExecutionKind.Windmill);
        }

        /// <summary>윈드밀만 다음 마디에 다시 (남은 화살표 취소).</summary>
        void RetryWindmill()
        {
            foreach (var c in encounter.Cues)
                if (c.Tag is CombatStep s && s.Phase == 3) judge.Cancel(c);
            encounter.Cues.RemoveAll(c => c.Cancelled || (c.Tag is CombatStep s && s.Phase == 3));
            WindmillAttempts++;
            windmillFails++;
            double start = CombatRules.FirstTargetBeat(clock.CurrentBeat);
            foreach (var s in CombatPatterns.WindmillSteps(0))
                encounter.Cues.Add(judge.Schedule(start + s.OffsetBeats, s.Key, null, s));
        }

        partial void OnEncounterFinished(Encounter e)
        {
            if (e.Event.IsBoss) Clear();
        }

        /// <summary>포위 전투 카메라: 공격하는 적 쪽으로 0.2초 안에 돌고, 동시 공격 때 낮게, 윈드밀 때 360° 회전.</summary>
        void LateUpdate()
        {
            if (motion == null) return;
            float dt = SpinDeltaTime();
            if (WindmillSpinning)
            {
                WindmillSpinDegrees = Mathf.Min(360f, WindmillSpinDegrees + 360f * dt / WindmillSpinSeconds);
                if (WindmillSpinDegrees >= 360f) { WindmillSpinning = false; SurroundYaw = 0; yawT = 1f; }
            }
            if (!IsSurround || encounter.Done)
            {
                if (!WindmillSpinning && (SurroundYaw != 0 || SurroundCameraDrop != 0) && State == StageState.Running)
                {
                    SurroundYaw = 0; SurroundCameraDrop = 0;
                    motion.localRotation = Quaternion.identity; motion.localPosition = Vector3.zero;
                }
                if (WindmillSpinning || (encounter != null && encounter.Done && IsSurround))
                    motion.localRotation = Quaternion.Euler(0, SurroundYaw + WindmillSpinDegrees, 0);
                return;
            }

            var cue = judge.FirstActiveCue;
            if (cue != null && cue.Tag is CombatStep s)
            {
                float target = s.Phase == 1 && s.Attackers.Length == 1 ? YawTo(surroundEnemies[s.Attackers[0]]) : SurroundYaw;
                if (Mathf.Abs(Mathf.DeltaAngle(target, yawTo)) > 0.01f) { yawFrom = SurroundYaw; yawTo = target; yawT = 0f; }
                SurroundCameraDrop = Mathf.MoveTowards(SurroundCameraDrop, s.Phase == 2 ? SimultaneousCameraDrop : 0f, dt * 4f);
            }
            if (yawT < 1f)
            {
                yawT = Mathf.Min(1f, yawT + dt / GameConfig.SurroundCameraTurnSeconds);
                SurroundYaw = Mathf.LerpAngle(yawFrom, yawTo, Mathf.SmoothStep(0, 1, yawT));
            }
            motion.localRotation = Quaternion.Euler(0, SurroundYaw, 0);
            motion.localPosition = new Vector3(0, SurroundCameraDrop, 0);
        }

        /// <summary>플레이어 진행 방향 기준, 적이 있는 쪽의 yaw (도).</summary>
        float YawTo(EnemyActor e)
        {
            if (e == null) return SurroundYaw;
            var d = e.transform.position - player.transform.position;
            d.y = 0;
            return Vector3.SignedAngle(player.transform.forward, d, Vector3.up);
        }
    }
}
