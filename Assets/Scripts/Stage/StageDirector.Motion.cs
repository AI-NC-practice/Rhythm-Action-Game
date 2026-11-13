using System;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 움직이며 싸우는 전투 (GDD-beta B13.1·B13.2). 일반 적과 무사 결투에서 판정 하나마다 플레이어와 적이 실제로 자리를 옮긴다.
    /// 판정 타이밍·점수·되감기에는 손대지 않는 연출 층이다. 포위 전투(B13.3)는 StageDirector.SurroundMotion에서 따로 다룬다.
    /// </summary>
    public partial class StageDirector
    {
        /// <summary>이동 연출 시간 (실제 초). 테스트에서 바꿔 끼운다.</summary>
        public Func<float> MotionDeltaTime = () => Time.deltaTime;

        /// <summary>지금 움직이는 전투 중인지 (일반 적·무사).</summary>
        public bool MotionActive { get; private set; }
        /// <summary>결투 원 중심 (전투를 시작한 자리).</summary>
        public Vector3 MotionCenter { get; private set; }
        /// <summary>플레이어 자리 (발, 지금 보이는 위치).</summary>
        public Vector3 MotionPlayer { get; private set; }
        /// <summary>적 자리 (발, 지금 보이는 위치).</summary>
        public Vector3 MotionEnemy { get; private set; }
        /// <summary>판정 뒤 이동 목표 (테스트용).</summary>
        public Vector3 MotionPlayerTarget => pTo;
        public Vector3 MotionEnemyTarget => eTo;
        /// <summary>싸움 거리 (m).</summary>
        public float FightDistance { get; private set; }
        /// <summary>원이 없는 틈에 적이 도는 방향 (+1 시계 반대, −1 시계).</summary>
        public int CircleDirection { get; private set; } = 1;
        /// <summary>전투가 끝나 경로 위 자리로 돌아오는 중.</summary>
        public bool MotionReturning { get; private set; }

        EnemyActor motionActor;
        Vector3 pFrom, pTo, eFrom, eTo;
        float moveT = 1f;
        double chargeFromBeat, chargeToBeat;
        bool charging;
        Quaternion facing;
        int duckSide = 1;
        float returnT;
        Vector3 returnFromOffset;
        Quaternion returnFromFacing;

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        void ResetMotion()
        {
            if (motionActor != null) { motionActor.MotionPosition = null; motionActor.FacePoint = null; }
            motionActor = null;
            MotionActive = false;
            MotionReturning = false;
            charging = false;
            SurroundMotionActive = false;
            moveT = 1f;
            if (player != null) { player.CombatOffset = Vector3.zero; player.CombatFacing = null; }
        }

        /// <summary>전투 시작: 결투 원을 잡고, 적은 서 있던 자리(5 m 앞)에서 싸움 거리까지 돌진한다 (B13.2).</summary>
        void BeginMotion(Encounter e)
        {
            ResetMotion();
            if (e.Event.IsSurround) { BeginSurroundMotion(e); return; }   // B13.3
            if (e.Actors == null || e.Actors.Length == 0 || e.Actors[0] == null || player == null) return;
            motionActor = e.Actors[0];
            MotionActive = true;
            MotionCenter = player.transform.position;
            MotionPlayer = pFrom = pTo = MotionCenter;
            FightDistance = motionActor.kind == "spear" ? GameConfig.FightDistanceSpear : GameConfig.FightDistanceSword;
            var start = motionActor.transform.position;
            var dir = Flat(start - MotionCenter);
            if (dir.sqrMagnitude < 1e-6f) dir = player.transform.forward;
            dir.Normalize();
            MotionEnemy = eFrom = start;
            eTo = ClampArena(MotionCenter + dir * FightDistance, start.y);
            // 돌진: 일반 적은 첫 공격 목표 박 조금 전까지, 무사는 등장 컷이 끝나는 박(O+4)까지
            chargeFromBeat = clock.CurrentBeat;
            chargeToBeat = e.IsBossDuel ? e.StandoffEndBeat : e.StartBeat - 0.25;
            charging = chargeToBeat > chargeFromBeat;
            if (!charging) MotionEnemy = eTo;
            facing = player.transform.rotation;
            CircleDirection = 1;
            motionActor.MotionPosition = MotionEnemy;
            motionActor.FacePoint = MotionPlayer;
        }

        Vector3 ClampArena(Vector3 p, float y)
        {
            var d = Flat(p - MotionCenter);
            if (d.magnitude > GameConfig.CombatArenaRadius) d = d.normalized * GameConfig.CombatArenaRadius;
            var r = MotionCenter + d;
            r.y = y;
            return r;
        }

        /// <summary>
        /// 판정 결과에 따른 이동 (B13.1 표). f = 플레이어 → 적, s = 누른 쪽 옆. 이동 뒤 적은 싸움 거리를 다시 맞춘다.
        /// </summary>
        public static void StepMotion(CombatStep step, Judgement r, Vector3 f, Vector3 right, int duckSide,
            out Vector3 playerMove, out Vector3 enemyMove)
        {
            playerMove = Vector3.zero; enemyMove = Vector3.zero;
            var side = step.Key == ArrowKey.Left ? -right : right;
            bool strike = step.Kind == CombatStepKind.Strike || step.Kind == CombatStepKind.Lock;
            if (r == Judgement.Fail)
            {
                if (strike) return;                                  // 헛침: 되감기가 대신한다
                playerMove = -f * GameConfig.MoveHitBack;            // 맞아서 밀려남, 적이 따라옴
                enemyMove = playerMove;
                return;
            }
            bool perfect = r == Judgement.Perfect;
            if (strike)
            {
                playerMove = perfect ? f * GameConfig.MoveStrikePerfect : -f * GameConfig.MoveStrikeNormal;
                enemyMove = playerMove;                              // PERFECT: 적이 밀림 / NORMAL: 적이 밀고 들어옴
                return;
            }
            if (step.Kind != CombatStepKind.Attack) return;
            switch (step.Key)
            {
                case ArrowKey.Up:
                    playerMove = perfect ? f * GameConfig.MoveParryPerfect : -f * GameConfig.MoveParryNormal;
                    enemyMove = playerMove;
                    break;
                case ArrowKey.Left:
                case ArrowKey.Right:
                    playerMove = side * (perfect ? GameConfig.MoveDodgePerfect : GameConfig.MoveDodgeNormal);
                    enemyMove = -side * (perfect ? GameConfig.MoveDodgePerfectEnemy : GameConfig.MoveDodgeNormalEnemy);   // 적이 지나침
                    break;
                case ArrowKey.Down:
                    if (perfect) playerMove = f * GameConfig.MoveDuckForward + right * (duckSide * GameConfig.MoveDuckSide);   // 파고듦
                    break;
            }
        }

        /// <summary>판정 하나가 끝났을 때 (OnCombatCueJudgedImpl에서).</summary>
        void ApplyMotion(CombatStep step, Judgement r)
        {
            if (SurroundMotionActive) { ApplySurroundMotion(step, r); return; }
            if (!MotionActive || motionActor == null || step.Kind == CombatStepKind.Finisher || step.Kind == CombatStepKind.Windmill) return;
            var f = Flat(MotionEnemy - MotionPlayer);
            if (f.sqrMagnitude < 1e-6f) f = Flat(player.transform.forward);
            f.Normalize();
            var right = Vector3.Cross(Vector3.up, f);
            StepMotion(step, r, f, right, duckSide, out var pm, out var em);
            if (step.Key == ArrowKey.Down && r == Judgement.Perfect) duckSide = -duckSide;
            charging = false;
            pFrom = MotionPlayer; eFrom = MotionEnemy;
            pTo = ClampArena(MotionPlayer + pm, MotionPlayer.y);
            var e = MotionEnemy + em;
            var d = Flat(e - pTo);
            if (d.sqrMagnitude < 1e-6f) d = f;
            eTo = ClampArena(pTo + d.normalized * FightDistance, MotionEnemy.y);   // 싸움 거리 다시 맞춤
            // 적이 원 가장자리에 걸려 거리가 줄면 플레이어를 반대로 물린다
            var gap = Flat(eTo - pTo);
            if (gap.magnitude < FightDistance - 0.05f) pTo = ClampArena(eTo - gap.normalized * FightDistance, MotionPlayer.y);
            moveT = 0f;
            CircleDirection = -CircleDirection;                     // 다음 틈에는 반대로 돈다
        }

        static float Ease(float t) => t * t * (3f - 2f * t);

        /// <summary>매 프레임: 돌진·이동·둘레 돌기·바라보기, 끝나면 경로로 돌아오기.</summary>
        void UpdateMotion()
        {
            float dt = MotionDeltaTime();
            if (MotionReturning) { UpdateReturn(dt); return; }
            if (SurroundMotionActive) { UpdateSurroundMotion(dt); return; }
            if (!MotionActive || motionActor == null) return;
            if (encounter == null || encounter.Done || motionActor.Defeated) { BeginReturn(); return; }

            if (charging)
            {
                double k = (clock.CurrentBeat - chargeFromBeat) / Math.Max(1e-6, chargeToBeat - chargeFromBeat);
                MotionEnemy = Vector3.Lerp(eFrom, eTo, Ease(Mathf.Clamp01((float)k)));
                if (k >= 1.0) charging = false;
            }
            else if (moveT < 1f)
            {
                moveT = Mathf.Min(1f, moveT + dt / GameConfig.CombatMoveSeconds);
                float k = Ease(moveT);
                MotionPlayer = Vector3.Lerp(pFrom, pTo, k);
                MotionEnemy = Vector3.Lerp(eFrom, eTo, k);
            }
            else if (!RingUp())
            {
                // 원이 없는 틈: 적이 둘레를 천천히 돌며 거리를 잰다
                var d = Flat(MotionEnemy - MotionPlayer);
                if (d.sqrMagnitude < 1e-6f) d = Flat(player.transform.forward);
                d = Quaternion.AngleAxis(CircleDirection * GameConfig.CombatCircleDegPerSec * dt, Vector3.up) * d.normalized * FightDistance;
                MotionEnemy = ClampArena(MotionPlayer + d, MotionEnemy.y);
                eTo = MotionEnemy; pTo = MotionPlayer;
            }

            // 플레이어가 적을 바라본다: 부드럽게, 초당 240° 이하
            var look = Flat(MotionEnemy - MotionPlayer);
            if (look.sqrMagnitude > 1e-6f)
            {
                var want = Quaternion.LookRotation(look.normalized, Vector3.up);
                var smooth = Quaternion.Slerp(facing, want, 1f - Mathf.Exp(-dt / GameConfig.CombatFaceSmoothSeconds));
                facing = Quaternion.RotateTowards(facing, smooth, GameConfig.CombatFaceMaxDegPerSec * dt);
            }
            player.CombatOffset = Flat(MotionPlayer - MotionCenter);
            player.CombatFacing = facing;
            player.Place();
            motionActor.MotionPosition = MotionEnemy;
            motionActor.FacePoint = MotionPlayer;
        }

        bool RingUp()
        {
            foreach (var c in encounter.Cues) if (c.Appeared && !c.Judged && !c.Cancelled) return true;
            return false;
        }

        void BeginReturn()
        {
            if (motionActor != null && !motionActor.Defeated) { motionActor.MotionPosition = null; motionActor.FacePoint = null; }
            motionActor = null;
            MotionActive = false;
            MotionReturning = true;
            returnT = 0f;
            returnFromOffset = player.CombatOffset;
            returnFromFacing = player.CombatFacing ?? player.transform.rotation;
        }

        void UpdateReturn(float dt)
        {
            returnT = Mathf.Min(1f, returnT + dt / GameConfig.CombatReturnSeconds);
            float k = Ease(returnT);
            MapLayout.PointAt(player.VisualDistance, out var fwd);
            var path = Quaternion.LookRotation(fwd, Vector3.up);
            player.CombatOffset = Vector3.Lerp(returnFromOffset, Vector3.zero, k);
            player.CombatFacing = returnT >= 1f ? (Quaternion?)null : Quaternion.Slerp(returnFromFacing, path, k);
            player.Place();
            if (returnT >= 1f) MotionReturning = false;
        }
    }
}
