using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 적 모델의 Animator (작업 T14). EnemyActor가 공격·막기·밀림·쓰러짐·되감기를 알려 준다.
    /// - 공격: 원 등장(p=0)부터 목표 박(p=1)까지 클립 시간을 (타격 순간 − 원이 떠 있는 시간) → 타격 순간(hitSeconds)으로 맞춘다
    ///   → 원래 속도로 휘두르고, 타격 순간 = 판정 목표 박.
    ///   새 공격은 0.12초 크로스페이드로 들어오고(연속 공격), 판정이 끝나면 남은 휘두르기를 FollowThroughSeconds 보여 준 뒤 대기로.
    /// - 되감기: 마지막 공격 클립을 거꾸로 돌린다.
    /// - Animator는 박자 시계 시간으로 직접 진행 (Hit Stop 중에는 멈춤).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class EnemyAnimator : MonoBehaviour
    {
        public Animator animator;
        public BeatClock clock;
        public EnemyActor actor;
        /// <summary>상태 이름별 클립 길이와 타격 순간 (초). SceneBuilder가 채운다.</summary>
        public string[] stateNames;
        public float[] stateSeconds;
        public float[] hitSeconds;

        public string CurrentState { get; private set; } = EnemyMoves.Idle;
        /// <summary>공격 중 지금 클립 시간 (초, 테스트용).</summary>
        public float AttackClipTime { get; private set; }
        public float HitSecondsOf(string state) => hit.TryGetValue(state, out var h) ? h : 0f;
        public float LengthOf(string state) => len.TryGetValue(state, out var l) ? l : 1f;

        /// <summary>공격 진행도 p에 맞는 클립 시간 (초): p=1이면 타격 순간.</summary>
        public float ClipTimeFor(string state, float p)
        {
            float h = HitSecondsOf(state);
            float windup = Mathf.Min(h, GameConfig.ApproachBeats * GameConfig.SecondsPerBeat);
            return h - (1f - Mathf.Clamp01(p)) * windup;
        }
        /// <summary>상태가 시작된 박 (박자 시계, 테스트용).</summary>
        public double StateStartBeat { get; private set; } = double.NaN;

        enum Mode { Free, Scrub }
        Mode mode = Mode.Free;
        readonly Dictionary<string, float> len = new Dictionary<string, float>();
        readonly Dictionary<string, float> hit = new Dictionary<string, float>();
        float scrubTime;
        float blendLeft;
        float followLeft = -1f;
        double lastNow = double.NaN;

        void Awake()
        {
            if (stateNames != null)
                for (int i = 0; i < stateNames.Length; i++)
                {
                    if (stateSeconds != null && i < stateSeconds.Length) len[stateNames[i]] = stateSeconds[i];
                    if (hitSeconds != null && i < hitSeconds.Length) hit[stateNames[i]] = hitSeconds[i];
                }
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.enabled = false;                       // 직접 Update (박자 시계 시간)
                if (animator.runtimeAnimatorController != null) { animator.Rebind(); animator.Update(0f); }
            }
        }

        void Start() { if (clock == null) clock = BeatClock.Instance; }

        bool Ready => animator != null && animator.runtimeAnimatorController != null;

        void Enter(string state, float fade, float atSeconds = 0f)
        {
            CurrentState = state;
            StateStartBeat = clock != null ? clock.CurrentBeat : double.NaN;
            if (!Ready) return;
            if (fade <= 0f) { animator.Play(state, 0, atSeconds / LengthOf(state)); animator.Update(0f); }
            else animator.CrossFadeInFixedTime(state, fade, 0, atSeconds);
        }

        /// <summary>공격 진행도 p(0 = 원 등장, 1 = 목표 박)에 맞춰 휘두른다.</summary>
        public void OnAttack(string attack, float p)
        {
            string state = EnemyMoves.AttackState(attack);
            float t = ClipTimeFor(state, p);
            bool fresh = CurrentState != state || mode != Mode.Scrub || t + 1e-4f < scrubTime - 0.05f;
            scrubTime = t;
            AttackClipTime = t;
            followLeft = -1f;
            if (fresh)
            {
                mode = Mode.Scrub;
                blendLeft = GameConfig.EnemyBody.CrossFadeSeconds;
                Enter(state, GameConfig.EnemyBody.CrossFadeSeconds, t);
            }
        }

        /// <summary>판정이 끝남: 타격 순간부터 마저 휘두르고 대기로.</summary>
        public void OnEndAttack()
        {
            if (mode != Mode.Scrub) return;
            mode = Mode.Free;
            followLeft = GameConfig.EnemyBody.FollowThroughSeconds;
        }

        public void OnRewind(float u, string attack, float lastP)
        {
            string state = EnemyMoves.AttackState(attack);
            scrubTime = ClipTimeFor(state, lastP * (1f - Mathf.Clamp01(u)));
            AttackClipTime = scrubTime;
            followLeft = -1f;
            if (CurrentState != state || mode != Mode.Scrub) { CurrentState = state; mode = Mode.Scrub; blendLeft = 0f; }
        }

        public void OnGuard() { if (CurrentState != EnemyMoves.Guard) { mode = Mode.Free; followLeft = -1f; Enter(EnemyMoves.Guard, GameConfig.EnemyBody.CrossFadeSeconds); } }
        public void OnEndGuard() { if (CurrentState == EnemyMoves.Guard) Enter(EnemyMoves.Idle, GameConfig.EnemyBody.CrossFadeSeconds); }
        public void OnPushed() { mode = Mode.Free; followLeft = -1f; Enter(EnemyMoves.Hit, GameConfig.EnemyBody.CrossFadeSeconds); followLeft = LengthOf(EnemyMoves.Hit); }
        public void OnDefeat(ExecutionKind how) { mode = Mode.Free; followLeft = -1f; Enter(EnemyMoves.DefeatState(how), GameConfig.EnemyBody.CrossFadeSeconds * 0.5f); }
        public void OnReset() { mode = Mode.Free; followLeft = -1f; scrubTime = 0f; Enter(EnemyMoves.Idle, 0f); }

        void LateUpdate()
        {
            if (!Ready) return;
            double now = clock != null ? clock.Now : Time.unscaledTimeAsDouble;
            double dtd = double.IsNaN(lastNow) ? 0 : now - lastNow;
            lastNow = now;
            if (dtd < 0 || dtd > 0.25) dtd = 0;
            float speed = clock == null ? 1f : clock.IsPaused ? 0f : clock.IsRunning ? clock.SpeedMultiplier : 1f;
            if (actor != null && actor.Frozen) speed = 0f;
            float dt = (float)dtd * speed;

            if (mode == Mode.Scrub)
            {
                if (blendLeft > 0f)
                {
                    // 크로스페이드 중 (새 공격): 들어오는 공격은 원래 속도로 타격 순간을 향해 가고, 블렌드만 진행
                    blendLeft -= dt;
                    animator.Update(dt);
                }
                else
                {
                    animator.Play(CurrentState, 0, scrubTime / LengthOf(CurrentState));
                    animator.Update(0f);
                }
                return;
            }
            animator.Update(dt);
            if (followLeft >= 0f)
            {
                followLeft -= dt;
                if (followLeft < 0f && (CurrentState.StartsWith("Attack_") || CurrentState == EnemyMoves.Hit))
                    Enter(EnemyMoves.Idle, GameConfig.EnemyBody.CrossFadeSeconds);
            }
        }
    }
}
