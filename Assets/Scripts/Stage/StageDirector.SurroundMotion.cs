using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 포위 전투 움직임 (GDD-beta B13.3). 적 5명이 플레이어 둘레(반지름 4 m)를 돌다가, 공격하는 적이 원이 떠 있는 동안
    /// 플레이어 1.5 m까지 뛰어들어 목표 박에 베고, 판정 뒤 0.35초에 원으로 돌아간다. 동시 공격은 여럿이 한꺼번에 덮친다.
    /// 플레이어는 판정마다 B13.1 표대로 움직이되 중심에서 1.2 m 안에 머문다. 윈드밀(3단계)에서는 돌기를 멈춘다.
    /// </summary>
    public partial class StageDirector
    {
        /// <summary>포위 전투 움직임 중.</summary>
        public bool SurroundMotionActive { get; private set; }
        /// <summary>적 k가 뛰어든 정도 (0 = 원 위, 1 = 플레이어 1.5 m).</summary>
        public float LungeOf(int k) => lunge != null && k >= 0 && k < lunge.Length ? lunge[k] : 0f;
        /// <summary>적들이 원을 따라 돈 각도 (도).</summary>
        public float SurroundOrbitDegrees { get; private set; }

        float[] baseAngle, lunge;
        float sMoveT = 1f;
        Vector3 sFrom, sTo;
        Vector3 sFwd;

        void BeginSurroundMotion(Encounter e)
        {
            SurroundMotionActive = false;
            if (!e.Event.IsSurround || e.Actors == null || player == null) return;
            SurroundMotionActive = true;
            MotionCenter = player.transform.position;
            MotionPlayer = sFrom = sTo = MotionCenter;
            sMoveT = 1f;
            SurroundOrbitDegrees = 0f;
            sFwd = Flat(player.transform.forward).normalized;
            baseAngle = new float[e.Actors.Length];
            lunge = new float[e.Actors.Length];
            for (int k = 0; k < e.Actors.Length; k++)
            {
                if (e.Actors[k] == null) continue;
                var d = Flat(e.Actors[k].transform.position - MotionCenter);
                baseAngle[k] = Vector3.SignedAngle(sFwd, d, Vector3.up);
                e.Actors[k].MotionPosition = e.Actors[k].transform.position;
                e.Actors[k].FacePoint = MotionPlayer;
            }
        }

        void EndSurroundMotion()
        {
            SurroundMotionActive = false;
            if (player != null) BeginReturn();   // 경로 위 자리로 부드럽게 (B13.1)
        }

        /// <summary>판정 하나: 플레이어가 B13.1 표대로 움직인다 (공격한 적 쪽 기준).</summary>
        void ApplySurroundMotion(CombatStep step, Judgement r)
        {
            if (!SurroundMotionActive || step.Kind != CombatStepKind.Attack || encounter == null) return;
            var a = step.Attackers != null && step.Attackers.Length > 0 && step.Attackers[0] < encounter.Actors.Length ? encounter.Actors[step.Attackers[0]] : null;
            var f = a != null ? Flat(a.transform.position - MotionPlayer) : sFwd;
            if (f.sqrMagnitude < 1e-6f) f = sFwd;
            f.Normalize();
            StepMotion(step, r, f, Vector3.Cross(Vector3.up, f), duckSide, out var pm, out _);
            if (step.Key == ArrowKey.Down && r == Judgement.Perfect) duckSide = -duckSide;
            var to = Flat(MotionPlayer + pm - MotionCenter);
            if (to.magnitude > GameConfig.SurroundPlayerRadius) to = to.normalized * GameConfig.SurroundPlayerRadius;
            sFrom = MotionPlayer;
            sTo = MotionCenter + to;
            sTo.y = MotionCenter.y;
            sMoveT = 0f;
        }

        void UpdateSurroundMotion(float dt)
        {
            var e = encounter;
            if (e == null || !e.Event.IsSurround || e.Done) { if (SurroundMotionActive) EndSurroundMotion(); return; }
            if (sMoveT < 1f)
            {
                sMoveT = Mathf.Min(1f, sMoveT + dt / GameConfig.CombatMoveSeconds);
                MotionPlayer = Vector3.Lerp(sFrom, sTo, Ease(sMoveT));
            }
            player.CombatOffset = Flat(MotionPlayer - MotionCenter);
            player.Place();

            bool windmill = false;
            foreach (var c in e.Cues) if (c.Appeared && !c.Judged && !c.Cancelled && c.Tag is CombatStep s0 && s0.Phase == 3) windmill = true;
            if (!windmill) SurroundOrbitDegrees += GameConfig.SurroundCircleDegPerSec * dt;

            double beat = clock.CurrentBeat;
            for (int k = 0; k < e.Actors.Length; k++)
            {
                var a = e.Actors[k];
                if (a == null || a.Defeated) continue;
                // 지금 이 적이 공격 중인 원 (뛰어드는 정도 = 원 진행도)
                float want = -1f;
                foreach (var c in e.Cues)
                {
                    if (!c.Appeared || c.Judged || c.Cancelled || !(c.Tag is CombatStep s) || s.Kind != CombatStepKind.Attack) continue;
                    if (System.Array.IndexOf(s.Attackers, k) < 0) continue;
                    float p = Mathf.Clamp01((float)(1.0 - (c.TargetBeat - beat) / GameConfig.ApproachBeats));
                    want = Mathf.Max(want, Ease(p));
                }
                lunge[k] = want >= 0f ? want : Mathf.Max(0f, lunge[k] - dt / GameConfig.CombatMoveSeconds);   // 판정 뒤 0.35초에 원으로
                var dir = Quaternion.AngleAxis(baseAngle[k] + SurroundOrbitDegrees, Vector3.up) * sFwd;
                var ring = MotionCenter + dir * GameConfig.SurroundRadius;
                var toP = Flat(ring - MotionPlayer);
                var close = MotionPlayer + (toP.sqrMagnitude > 1e-6f ? toP.normalized : dir) * GameConfig.SurroundLungeMeters;
                var pos = Vector3.Lerp(ring, close, lunge[k]);
                pos.y = a.MotionPosition.HasValue ? a.MotionPosition.Value.y : a.transform.position.y;
                a.MotionPosition = pos;
                a.FacePoint = MotionPlayer;
            }
        }
    }
}
