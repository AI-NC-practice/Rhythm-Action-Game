using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 사건 소품 적(돌진·매복·추격·복선, EventProps가 옮김)의 달리기 (작업 R07, 사용자 피드백 4번 "유령처럼 스윽 다가옴").
    /// 실제 위치 변화로 수평 속도를 재서 대기 ↔ 달리기를 바꾸고, 달리기는 속도에 맞춘 배율로 재생한다. 값은 GameConfig.EnemyBody.
    /// </summary>
    public class EnemyFigureRun : MonoBehaviour
    {
        public Animator animator;

        public float MoveSpeed { get; private set; }
        public bool Running { get; private set; }
        /// <summary>작업 R18 후속: 걷는 중 (느리게 움직임).</summary>
        public bool Walking { get; private set; }
        /// <summary>달리거나 걷는 중.</summary>
        public bool Moving => Running || Walking;

        Vector3 lastPos;
        bool hasLast;
        double lastNow;

        void OnEnable() { hasLast = false; MoveSpeed = 0f; Running = false; Walking = false; }

        /// <summary>작업 R31·R32: 쓰러짐 (PERFECT). 쓰러지는 동작을 한 번 재생하고 그 자세로 누워 있는다.</summary>
        public bool Down { get; private set; }

        public void PlayDefeat()
        {
            if (Down || animator == null || animator.runtimeAnimatorController == null) return;
            Down = true;
            Running = false; Walking = false; MoveSpeed = 0f;   // 쓰러진 자리에 멈춤 (EventProp가 위치를 고정)
            animator.speed = 1f;
            int st = Animator.StringToHash(EnemyMoves.DefeatState(ExecutionKind.PushDown));
            if (animator.HasState(0, st)) animator.CrossFadeInFixedTime(st, GameConfig.EnemyBody.CrossFadeSeconds * 0.5f, 0);
        }

        public void ResetFigure()
        {
            Down = false;
            hasLast = false; MoveSpeed = 0f; Running = false; Walking = false;
            if (animator != null && animator.runtimeAnimatorController != null) { animator.speed = 1f; animator.Play(EnemyMoves.Idle, 0, 0f); }
        }

        bool hasWalk, checkedWalk;
        void Go(string state) => animator.CrossFadeInFixedTime(state, GameConfig.EnemyBody.CrossFadeSeconds, 0);

        void LateUpdate()
        {
            if (Down) { MoveSpeed = 0f; return; }
            if (animator == null || animator.runtimeAnimatorController == null) return;
            // 소품 위치는 박자 시계로 정해지므로 속도도 박자 시계 시간으로 잰다
            var clock = BeatClock.Instance;
            double nowT = clock != null ? clock.Now : Time.timeAsDouble;
            float dt = hasLast ? (float)(nowT - lastNow) : 0f;
            var p = transform.position;
            if (!hasLast || dt <= 0f || dt > 0.25f) { lastPos = p; lastNow = nowT; hasLast = true; return; }
            lastNow = nowT;
            var d = p - lastPos; d.y = 0f;
            lastPos = p;
            float v = d.magnitude / dt;
            if (v > GameConfig.EnemyBody.TeleportSpeed) v = 0f;
            MoveSpeed = Mathf.Lerp(MoveSpeed, v, 1f - Mathf.Exp(-GameConfig.EnemyBody.SpeedSmoothing * dt));
            if (!checkedWalk) { checkedWalk = true; hasWalk = animator.HasState(0, Animator.StringToHash(EnemyMoves.Walk)); }
            var b = GameConfig.EnemyBody.RunStartSpeed;
            // 대기 ↔ 걷기 ↔ 달리기 (작업 R18 후속: 느리면 걷기, 걷기 상태가 없으면 예전처럼 달리기)
            bool moving = Running || Walking;
            if (!moving && MoveSpeed > b)
            {
                if (hasWalk && MoveSpeed < GameConfig.EnemyBody.WalkToRunSpeed) { Walking = true; Go(EnemyMoves.Walk); }
                else { Running = true; Go(EnemyMoves.Run); }
            }
            else if (moving && MoveSpeed < GameConfig.EnemyBody.RunStopSpeed) { Running = false; Walking = false; Go(EnemyMoves.Idle); }
            else if (Walking && MoveSpeed > GameConfig.EnemyBody.WalkToRunSpeed) { Walking = false; Running = true; Go(EnemyMoves.Run); }
            else if (Running && hasWalk && MoveSpeed < GameConfig.EnemyBody.RunToWalkSpeed) { Running = false; Walking = true; Go(EnemyMoves.Walk); }
            animator.speed = Running ? Mathf.Clamp(MoveSpeed / GameConfig.EnemyBody.RunClipSpeed, GameConfig.EnemyBody.RunRateMin, GameConfig.EnemyBody.RunRateMax)
                : Walking ? Mathf.Clamp(MoveSpeed / GameConfig.EnemyBody.WalkClipSpeed, GameConfig.EnemyBody.WalkRateMin, GameConfig.EnemyBody.WalkRateMax) : 1f;
        }
    }
}
