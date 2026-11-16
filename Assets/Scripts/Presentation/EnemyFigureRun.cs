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

        Vector3 lastPos;
        bool hasLast;
        double lastNow;

        void OnEnable() { hasLast = false; MoveSpeed = 0f; }

        void LateUpdate()
        {
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
            if (!Running && MoveSpeed > GameConfig.EnemyBody.RunStartSpeed) { Running = true; animator.CrossFadeInFixedTime(EnemyMoves.Run, GameConfig.EnemyBody.CrossFadeSeconds, 0); }
            else if (Running && MoveSpeed < GameConfig.EnemyBody.RunStopSpeed) { Running = false; animator.CrossFadeInFixedTime(EnemyMoves.Idle, GameConfig.EnemyBody.CrossFadeSeconds, 0); }
            animator.speed = Running ? Mathf.Clamp(MoveSpeed / GameConfig.EnemyBody.RunClipSpeed, GameConfig.EnemyBody.RunRateMin, GameConfig.EnemyBody.RunRateMax) : 1f;
        }
    }
}
