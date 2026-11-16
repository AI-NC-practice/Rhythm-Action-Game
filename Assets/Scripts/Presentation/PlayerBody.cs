using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RhythmActionGame
{
    /// <summary>
    /// 1인칭 전신 플레이어 (작업 T13, GDD 4.3·6).
    /// - Animator: 기본 Run(반복, 두 걸음 = RunCycleBeats 박). 사건 판정 결과마다 BodyMoves 상태로 크로스페이드하고,
    ///   클립 길이를 ActionBeats(4박) 길이에 맞게 재생 속도를 바꾼다. 끝나면 컨트롤러의 전이로 Run으로 돌아온다.
    ///   박자 시계 속도 배율(판정 속도·회피 슬로모션)을 Animator 속도에 곱한다.
    /// - 머리: 원래 메시는 그림자만(ShadowsOnly) 그리고, 머리·목 뼈 삼각형을 뺀 복사본을 화면에 그린다 → 카메라에 머리가 안 걸린다.
    /// - 카메라: 눈높이(EyeHeight)를 기준으로 머리 뼈 움직임을 일부(HeadFollowWeight) 부드럽게 따라간다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PlayerBody : MonoBehaviour
    {
        public Animator animator;
        public SkinnedMeshRenderer body;
        public StageDirector director;
        public BeatClock clock;
        /// <summary>카메라 받침 (Player/CameraRig). 머리 뼈를 따라 움직인다.</summary>
        public Transform cameraRig;
        /// <summary>몸 받침 (Player/BodyOffset, 작업 R06). 장애물 동작 동안 몸을 카메라 궤적과 함께 옮기고 벽타기에서 눕힌다.</summary>
        public Transform bodyOffset;
        /// <summary>화면 카메라 (PlayerCamera, 작업 R06). 목 단면이 화면에 들어오면 몸을 숨긴다.</summary>
        public Camera viewCamera;
        /// <summary>상태 이름별 클립 길이 (초). SceneBuilder가 채운다.</summary>
        public string[] stateNames;
        public float[] stateSeconds;

        public const string MirrorParam = "Mirror";
        public const string ActionSpeedParam = "ActionSpeed";
        public const string RunSpeedParam = "RunSpeed";

        /// <summary>마지막으로 들어간 상태 (테스트·디버그).</summary>
        public string LastState { get; private set; } = BodyMoves.Run;
        public bool LastMirror { get; private set; }
        /// <summary>화면에 그리는 머리 없는 복사본.</summary>
        public SkinnedMeshRenderer VisibleBody { get; private set; }
        public int RemovedHeadTriangles { get; private set; }

        readonly Dictionary<string, float> seconds = new Dictionary<string, float>();
        Transform head;
        Vector3 headRest;
        Vector3 rigOffset;
        bool hooked;
        /// <summary>동작(장애물·피격) 중이면 1, 달리기면 0 (작업 R06). 부드럽게 바뀐다.</summary>
        float actionBlend;
        /// <summary>남은 동작 시간 (초, 박자 시계 시간 × 속도 배율).</summary>
        float actionLeft;
        /// <summary>지금 동작이 실패·피격인지 (작업 R06: 1인칭 실패 연출 = 카메라만 흔들고 몸은 화면에서 숨김).</summary>
        bool failAction;
        /// <summary>카메라를 머리에 그대로 붙이는 동작인지 (벽타기·미끄러지기·실패·피격).</summary>
        bool followFull;
        StageDirector.ObstacleRun wallRunFor;
        float wallFootOffset = GameConfig.Body.WallRunFootOffset;

        /// <summary>벽타기 발 옆 거리 (m, 테스트용).</summary>
        public float WallFootOffset => wallFootOffset;

        /// <summary>벽 안쪽 면까지 옆 거리 (허리 높이에서 벽 쪽으로 광선). 못 찾으면 GameConfig 값.</summary>
        float MeasureWall(float side)
        {
            var from = transform.position + Vector3.up * 1.0f;
            var dir = transform.right * Mathf.Sign(side);
            foreach (var h in Physics.RaycastAll(from, dir, GameConfig.Body.WallRunMaxSearch, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                if (!h.transform.IsChildOf(transform) && h.distance > 0.5f) return Mathf.Max(0.5f, h.distance - GameConfig.Body.WallRunFootGap);
            return GameConfig.Body.WallRunFootOffset;
        }
        bool inverted;

        /// <summary>화면에 그리는 몸을 숨겼는지 (테스트용).</summary>
        public bool BodyHidden { get; private set; }

        /// <summary>지금 동작 중 비율 (0 달리기 ~ 1 동작, 테스트용).</summary>
        public float ActionBlend => actionBlend;
        /// <summary>벽타기 몸 기울기 (도, 테스트용). 양수 = 왼쪽으로 눕힘.</summary>
        public float LeanDegrees { get; private set; }
        /// <summary>몸이 카메라 연출을 따라 돈 각도 (도, 테스트용).</summary>
        public float BodyYaw { get; private set; }

        void Awake()
        {
            if (stateNames != null && stateSeconds != null)
                for (int i = 0; i < Mathf.Min(stateNames.Length, stateSeconds.Length); i++) seconds[stateNames[i]] = stateSeconds[i];
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.enabled = false;                          // 직접 Update (박자 시계 시간)
                if (animator.runtimeAnimatorController != null) { animator.Rebind(); animator.Update(0f); }
                head =animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            }
            BuildHeadlessCopy();
        }

        void Start()
        {
            if (head != null) headRest = transform.InverseTransformPoint(head.position);
            Hook();
            UpdateRunSpeed();
        }

        void OnDestroy() => Unhook();

        void Hook()
        {
            if (hooked || director == null) return;
            director.EventSlotJudged += OnSlotJudged;
            director.StageReset += OnReset;
            director.EnemyDefeated += OnEnemyDefeated;
            director.PlayerHit += OnPlayerHit;
            if (director.judge != null) director.judge.CueJudged += OnCueJudged;
            hooked = true;
        }

        void Unhook()
        {
            if (!hooked || director == null) return;
            director.EventSlotJudged -= OnSlotJudged;
            director.StageReset -= OnReset;
            director.EnemyDefeated -= OnEnemyDefeated;
            director.PlayerHit -= OnPlayerHit;
            if (director.judge != null) director.judge.CueJudged -= OnCueJudged;
            hooked = false;
        }

        void OnSlotJudged(StageDirector.ObstacleRun run, int slot, Judgement r)
        {
            var key = slot >= 0 && slot < run.SlotKeys.Count ? run.SlotKeys[slot] : run.Event.Key;
            Play(run.HasCategory ? run.Category : (ObstacleCategory?)null, key, r);
        }

        void OnReset() => PlayRun();

        /// <summary>처형이 시작된 박 (박자 시계, 테스트용). 적의 쓰러짐 동작과 같은 박이다.</summary>
        public double ExecStartBeat { get; private set; } = double.NaN;

        void OnEnemyDefeated(StageDirector.Encounter e, ExecutionKind how) => PlayOneShot(BodyMoves.ExecState(how));

        void OnPlayerHit(RewindKind k) => PlayOneShot(BodyMoves.Hit);

        /// <summary>적 공격을 못 막음 (FAIL) = 맞음.</summary>
        void OnCueJudged(JudgeCue cue, Judgement r)
        {
            if (r == Judgement.Fail && cue.Tag is CombatStep s && s.Kind == CombatStepKind.Attack) PlayOneShot(BodyMoves.Hit);
        }

        /// <summary>원래 속도로 한 번 재생하고 Run으로 (처형·피격).</summary>
        public void PlayOneShot(string state)
        {
            LastState = state;
            LastMirror = false;
            if (state.StartsWith("Exec_")) ExecStartBeat = clock != null ? clock.CurrentBeat : double.NaN;
            if (state == BodyMoves.Hit) { BeginAction(seconds.TryGetValue(state, out var hs) ? hs : 1f); failAction = true; }   // R06: 맞는 동안 1인칭 실패 연출
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetBool(MirrorParam, false);
            animator.CrossFadeInFixedTime(state, GameConfig.Body.CrossFadeSeconds, 0);
        }

        /// <summary>판정 결과 하나에 맞는 동작을 재생한다.</summary>
        public void Play(ObstacleCategory? category, ArrowKey key, Judgement r)
        {
            BeginAction(GameConfig.Body.ActionBeats * GameConfig.SecondsPerBeat);
            failAction = r == Judgement.Fail;
            string action = category.HasValue ? BodyMoves.ActionOf(category.Value) : BodyMoves.ActionOfKey(key);
            string state = BodyMoves.State(action, r);
            bool mirror = BodyMoves.Mirror(action, key);
            LastState = state;
            LastMirror = mirror;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            float len = seconds.TryGetValue(state, out var s) ? s : 1f;
            animator.SetBool(MirrorParam, mirror);
            // 벽타기 PERFECT/NORMAL은 달리기 클립 → 달리기 빠르기 그대로 (2박에 끝나면 같은 클립의 Run으로 이어짐)
            bool wallRunning = action == "WallRun" && r != Judgement.Fail;
            animator.SetFloat(ActionSpeedParam, BodyMoves.SpeedToFit(len, wallRunning ? GameConfig.Body.RunCycleBeats : GameConfig.Body.ActionBeats, GameConfig.SecondsPerBeat));
            animator.CrossFadeInFixedTime(state, GameConfig.Body.CrossFadeSeconds, 0);
        }

        public void PlayRun()
        {
            actionLeft = 0f;
            LastState = BodyMoves.Run;
            LastMirror = false;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetBool(MirrorParam, false);
            animator.CrossFadeInFixedTime(BodyMoves.Run, GameConfig.Body.CrossFadeSeconds, 0);
        }

        void UpdateRunSpeed()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            float len = seconds.TryGetValue(BodyMoves.Run, out var s) ? s : 1f;
            animator.SetFloat(RunSpeedParam, BodyMoves.SpeedToFit(len, GameConfig.Body.RunCycleBeats, GameConfig.SecondsPerBeat));
        }

        double lastNow = double.NaN;

        /// <summary>동작 시간 동안 카메라를 머리에 붙인다 (Animator와 같은 시간: 속도 배율·일시정지 반영).</summary>
        void BeginAction(float seconds) => actionLeft = seconds;

        /// <summary>
        /// Animator는 직접 돌린다: 박자 시계와 같은 시간(clock.Now)으로 진행해 음악·판정과 어긋나지 않는다.
        /// 속도 배율(판정 속도·회피 슬로모션)을 곱하고, 일시정지면 멈춘다.
        /// </summary>
        void Update()
        {
            if (animator == null) return;
            if (lowPosePending) SnapToLowestPose();
            double now = clock != null ? clock.Now : Time.unscaledTimeAsDouble;
            double dt = double.IsNaN(lastNow) ? 0 : now - lastNow;
            lastNow = now;
            if (dt < 0 || dt > 0.25) dt = 0;                       // 시간 되돌림·긴 멈춤은 건너뜀
            float speed = clock == null ? 1f : clock.IsPaused ? 0f : clock.IsRunning ? clock.SpeedMultiplier : 1f;
            UpdateCombatStance((float)dt * speed);
            animator.Update((float)dt * speed);
            followDt += (float)dt * speed;
            actionLeft -= (float)dt * speed;
        }

        float followDt;

        static readonly int RunHash = Animator.StringToHash(BodyMoves.Run);
        static readonly int CombatIdleHash = Animator.StringToHash(BodyMoves.CombatIdle);
        Vector3 lastMovePos;
        bool hasLastMovePos;
        /// <summary>전투 대기 자세인지 (작업 R16, 테스트용).</summary>
        public bool InCombatStance { get; private set; }
        /// <summary>몸(플레이어 뿌리) 수평 이동 빠르기 (m/s, 박자 시계 시간, 부드럽게).</summary>
        public float MoveSpeed { get; private set; }

        /// <summary>
        /// 작업 R16: 전투(멈추는 적·포위·무사 결투)에서 몸이 거의 안 움직이면 달리기 대신 대기 자세로, 움직이면 달리기를 이동 빠르기에 맞춰.
        /// 동작(장애물·피격)·처형 중에는 건드리지 않는다. 전투가 끝나면 달리기로 크로스페이드.
        /// </summary>
        void UpdateCombatStance(float dt)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            var at = transform.position;   // 플레이어 뿌리 (경로 + 전투 이동 CombatOffset). 머리 따라가기 출렁임은 빼고
            if (hasLastMovePos && dt > 1e-4f)
            {
                var d = at - lastMovePos; d.y = 0f;
                float v = d.magnitude / dt;
                if (v > 30f) v = 0f;   // 되감기·순간 이동
                MoveSpeed = Mathf.Lerp(MoveSpeed, v, 1f - Mathf.Exp(-GameConfig.Body.CombatSpeedSharpness * dt));
            }
            if (dt > 1e-4f || !hasLastMovePos) { lastMovePos = at; hasLastMovePos = true; }
            bool combat = director != null && director.State == StageState.Combat;
            bool still = MoveSpeed < GameConfig.Body.CombatStillSpeed;
            float len = seconds.TryGetValue(BodyMoves.Run, out var rs) ? rs : 1f;
            float runRate = BodyMoves.SpeedToFit(len, GameConfig.Body.RunCycleBeats, GameConfig.SecondsPerBeat);
            float runMps = GameConfig.MetersPerBeat / GameConfig.SecondsPerBeat;
            animator.SetFloat(RunSpeedParam, combat && !still ? runRate * Mathf.Clamp(MoveSpeed / runMps, GameConfig.Body.CombatRunMinRate, 1f) : runRate);
            if (animator.IsInTransition(0) || actionLeft > 0f) return;
            int cur = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            if (combat && still && cur == RunHash) SetLoopState(BodyMoves.CombatIdle);
            else if ((!combat || !still) && cur == CombatIdleHash) SetLoopState(BodyMoves.Run);
            InCombatStance = animator.GetCurrentAnimatorStateInfo(0).shortNameHash == CombatIdleHash || LastState == BodyMoves.CombatIdle;
        }

        void SetLoopState(string state)
        {
            if (!animator.HasState(0, Animator.StringToHash(state))) return;
            LastState = state;
            LastMirror = false;
            animator.SetBool(MirrorParam, false);
            animator.CrossFadeInFixedTime(state, GameConfig.Body.CrossFadeSeconds, 0);
        }

        bool lowPosePending;
        /// <summary>마지막으로 넘긴 낮은 자세의 정규화 시간 (작업 R19, 테스트용). 넘기지 않았으면 −1.</summary>
        public float LowPoseNormalizedTime { get; private set; } = -1f;
        /// <summary>마지막으로 넘긴 낮은 자세의 머리 높이 (몸 받침 기준 m, 테스트용).</summary>
        public float LowPoseHeadHeight { get; private set; } = float.NaN;

        /// <summary>
        /// 작업 R19: 대문 3인칭 컷이 시작되면 지금 동작(미끄러지기·숙이기)에서 머리가 가장 낮은 자리로 넘겨 달라고 한다.
        /// 다음 Update(판정 이벤트 뒤 크로스페이드가 걸린 다음)에서 처리한다.
        /// </summary>
        public void RequestLowestPose() => lowPosePending = true;

        void SnapToLowestPose()
        {
            lowPosePending = false;
            if (animator == null || animator.runtimeAnimatorController == null || head == null || LastState == BodyMoves.Run) return;
            int state = Animator.StringToHash(LastState);
            if (!animator.HasState(0, state)) return;
            Transform basis = bodyOffset != null ? bodyOffset : transform;
            float best = GameConfig.Body.LowPoseSearchFrom, bestY = float.PositiveInfinity;
            int n = Mathf.Max(2, GameConfig.Body.LowPoseSamples);
            for (int i = 0; i < n; i++)
            {
                float t = Mathf.Lerp(GameConfig.Body.LowPoseSearchFrom, GameConfig.Body.LowPoseSearchTo, i / (float)(n - 1));
                animator.Play(state, 0, t);
                animator.Update(0f);
                float y = basis.InverseTransformPoint(head.position).y;
                if (y < bestY) { bestY = y; best = t; }
            }
            animator.Play(state, 0, best);
            animator.Update(0f);
            LowPoseNormalizedTime = best;
            LowPoseHeadHeight = bestY;
        }

        /// <summary>머리 뼈 따라가기는 LateUpdate(Cinemachine Brain보다 먼저, 실행 순서 −50)에서, 시간은 박자 시계 시간으로.</summary>
        void LateUpdate()
        {
            float target = actionLeft > 0f ? 1f : 0f;
            // 동작이 시작되면 바로 1 (판정과 함께 카메라 궤적이 튀므로 몸도 같은 프레임에), 끝날 때만 부드럽게 달리기로
            actionBlend = target >= actionBlend ? target : Mathf.Lerp(actionBlend, target, 1f - Mathf.Exp(-GameConfig.Body.ActionBlendSharpness * followDt));
            if (Mathf.Abs(actionBlend - target) < 1e-3f) actionBlend = target;
            PlaceBody(out var motionPos);
            FollowHead(followDt, motionPos);
            followDt = 0f;
            UpdateVisibility();
        }

        /// <summary>
        /// 작업 R06: 1인칭에서 머리 없는 몸이 보이지 않게.
        /// (1) 실패·피격 동작 동안은 화면에 그리는 몸을 숨긴다(그림자는 원래 메시가 그대로) — 3인칭용 실패 동작이 몸을 크게 움직여 카메라가 몸 안에 들어가거나 목 단면이 보이므로.
        /// (2) 그 밖의 동작에서도 목 단면(목 뼈)이 화면 안(여유 포함)에 들어오면 숨긴다.
        /// </summary>
        void UpdateVisibility()
        {
            if (VisibleBody == null) return;
            bool hide = actionBlend > 0f && failAction && actionLeft > 0f;
            if (!hide && viewCamera != null && head != null)
            {
                // 아래 (2)(3)(5)는 동작 밖(포위 전투 카메라 회전·낮추기 등)에도 적용 — R06 후속: 4장 포위 전투에서 카메라가 몸 안을 봄
                var neck = animator.GetBoneTransform(HumanBodyBones.Neck) ?? head;
                var vp = viewCamera.WorldToViewportPoint(neck.position);
                float m = GameConfig.Body.NeckViewMargin;
                hide = vp.z > 0f && vp.x > -m && vp.x < 1f + m && vp.y > -m && vp.y < 1f + m;
                // (3) 카메라가 몸 앞 방향에서 크게 돌아감 (앞구르기 한 바퀴 등) → 거꾸로 된 내 몸이 화면을 덮지 않게
                var bodyFwd = bodyOffset != null ? bodyOffset.forward : transform.forward;
                if (Vector3.Angle(viewCamera.transform.forward, bodyFwd) > GameConfig.Body.HideBodyViewAngle) hide = true;
                // (5) 카메라가 목보다 낮음 = 몸통 안 (동작이 끝나 몸이 먼저 일어설 때, 포위 전투 동시 공격에 낮출 때 등)
                if (viewCamera.transform.position.y < neck.position.y) hide = true;
                if (actionBlend > 0f)
                {
                    // (4) 몸이 뒤집힘 (앞구르기에서 머리가 엉덩이보다 낮음) → 굴러 넘어오는 다리가 화면을 덮지 않게.
                    //     일어설 때 무릎이 화면을 덮으므로 머리가 충분히 올라올 때까지 숨긴 채로 둔다.
                    var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                    if (hips != null)
                    {
                        float up = head.position.y - hips.position.y;
                        if (up < GameConfig.Body.InvertedHeadAboveHips) inverted = true;
                        else if (up > GameConfig.Body.UprightHeadAboveHips) inverted = false;
                    }
                    if (inverted) hide = true;
                }
            }
            if (actionBlend <= 0f) inverted = false;
            BodyHidden = hide;
            VisibleBody.enabled = !hide;
        }

        /// <summary>
        /// 작업 R06: 카메라를 머리에 붙이는 동작(벽타기·미끄러지기·실패·피격, followFull)은 몸 받침을 카메라 연출 궤적(StageDirector.motion)과
        /// 같이 옮긴다 → 카메라가 몸을 떠나지 않는다. 높이는 동작 클립이 이미 내는 것(미끄러지기·실패로 쓰러짐)은 빼고, 실패로 뒤로 튕기는 것은 몸도 함께.
        /// 벽타기는 발을 벽 안쪽 면(옆으로 쏜 광선으로 잰 거리)에 두고 발 기준으로 벽 반대쪽으로 약 45° 눕힌다.
        /// 넘기·오르기·뛰어내리기·옆으로 피하기는 예전(T13)처럼 몸은 제자리, 카메라는 머리를 일부만 따라간다.
        /// </summary>
        void PlaceBody(out Vector3 motionPos)
        {
            motionPos = Vector3.zero;
            Vector3 pos = Vector3.zero;
            float lean = 0f;
            var run = director != null ? director.Current : null;
            followFull = failAction && actionLeft > 0f;
            if (run != null && run.HasResult && actionBlend > 0f && director.motion != null)
            {
                float p = director.TraversalProgress;
                float bell = Mathf.Sin(Mathf.Clamp01(p) * Mathf.PI);
                string action = run.HasCategory ? BodyMoves.ActionOf(run.Category) : null;
                bool fail = run.Result == Judgement.Fail;
                bool slide = action == "Slide" || (action == null && LastState.StartsWith("Slide"));
                followFull = fail || slide || action == "WallRun";
                if (followFull)
                {
                    motionPos = director.motion.localPosition;
                    if (action == "WallRun" && !fail)
                    {
                        if (wallRunFor != run) { wallRunFor = run; wallFootOffset = MeasureWall(run.Side); }
                        float deg = run.Result == Judgement.Perfect ? GameConfig.Body.WallRunLeanDegrees : GameConfig.Body.WallRunNormalLeanDegrees;
                        pos = new Vector3(run.Side * wallFootOffset * bell, motionPos.y, 0f);
                        lean = run.Side * deg * bell;
                    }
                    else
                    {
                        pos = motionPos;
                        pos.y = 0f;   // 높이는 클립이 낸다 (미끄러지기·쓰러짐)
                    }
                    motionPos *= actionBlend;
                    pos *= actionBlend;
                    lean *= actionBlend;
                }
            }
            LeanDegrees = lean;
            // 카메라 연출이 몸 방향을 돌리면(포위 전투에서 공격하는 적 쪽, 윈드밀 회전) 몸도 같이 돈다 → 카메라가 내 등·몸통 안을 보지 않는다
            float yaw = director != null && director.motion != null ? director.motion.localEulerAngles.y : 0f;
            BodyYaw = yaw;
            if (bodyOffset != null)
            {
                bodyOffset.localPosition = pos;
                bodyOffset.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, lean);
            }
        }

        /// <summary>
        /// 카메라 받침이 머리 뼈를 따라간다. 박자 시계 시간으로 한다 (작업 T15 수정: 실제 프레임 시간이면 무거운 씬에서
        /// 프레임마다 움직임이 달라진다). Update에서 하면 컷 카메라 검사 사이에 받침이 움직이므로 LateUpdate에서 한다.
        /// </summary>
        void FollowHead(float dt, Vector3 motionPos)
        {
            if (cameraRig == null || head == null) return;
            // 머리 움직임은 몸 받침 기준으로 재고(몸이 돌거나 기울어도 같은 값), 눈 위치를 몸 받침과 함께 옮기고 돌린다
            var now = bodyOffset != null ? bodyOffset.InverseTransformPoint(head.position) : transform.InverseTransformPoint(head.position);
            // 동작 중(R06): 머리를 다 따라가고(몸 받침 이동 포함), 카메라 연출 위치는 몸이 이미 옮겼으니 빼서 두 번 움직이지 않게
            float full = followFull ? actionBlend : 0f;
            float w = Mathf.Lerp(GameConfig.Body.HeadFollowWeight, GameConfig.Body.ActionHeadFollowWeight, full);
            var target = (now - headRest) * w;
            // 동작 중에는 머리를 그대로 따라감 (부드럽게 늦추면 몸이 먼저 일어설 때 카메라가 몸통 안에 남음). 달리기는 출렁임을 부드럽게.
            float k = Mathf.Lerp(1f - Mathf.Exp(-GameConfig.Body.HeadFollowSharpness * dt), 1f, full);
            rigOffset = Vector3.Lerp(rigOffset, target, k);
            var eye = new Vector3(0f, GameConfig.EyeHeight, GameConfig.Body.EyeForward) + rigOffset;
            if (bodyOffset != null) eye = bodyOffset.localPosition + bodyOffset.localRotation * eye;
            cameraRig.localPosition = eye - motionPos;
        }


        /// <summary>
        /// 머리·목(과 그 아래 뼈)에 가중치가 실린 삼각형을 뺀 메시 복사본을 만들어 화면에 그리고, 원래 메시는 그림자만 남긴다.
        /// </summary>
        public void BuildHeadlessCopy()
        {
            if (VisibleBody != null || body == null || body.sharedMesh == null || animator == null || !animator.isHuman) return;
            var headBones = new HashSet<Transform>();
            foreach (var b in new[] { HumanBodyBones.Head, HumanBodyBones.Neck })
            {
                var t = animator.GetBoneTransform(b);
                if (t == null) continue;
                foreach (var c in t.GetComponentsInChildren<Transform>(true)) headBones.Add(c);
            }
            var bones = body.bones;
            var isHead = new bool[bones.Length];
            for (int i = 0; i < bones.Length; i++) isHead[i] = bones[i] != null && headBones.Contains(bones[i]);

            var src = body.sharedMesh;
            var weights = src.boneWeights;
            var headW = new float[weights.Length];
            for (int v = 0; v < weights.Length; v++)
            {
                var w = weights[v];
                float h = 0f;
                if (w.boneIndex0 < isHead.Length && isHead[w.boneIndex0]) h += w.weight0;
                if (w.boneIndex1 < isHead.Length && isHead[w.boneIndex1]) h += w.weight1;
                if (w.boneIndex2 < isHead.Length && isHead[w.boneIndex2]) h += w.weight2;
                if (w.boneIndex3 < isHead.Length && isHead[w.boneIndex3]) h += w.weight3;
                headW[v] = h;
            }
            var mesh = Instantiate(src);
            mesh.name = src.name + "_NoHead";
            int removed = 0;
            for (int sm = 0; sm < mesh.subMeshCount; sm++)
            {
                var tris = mesh.GetTriangles(sm);
                var keep = new List<int>(tris.Length);
                for (int t = 0; t < tris.Length; t += 3)
                {
                    float th = GameConfig.Body.HeadWeightThreshold;
                    if (headW[tris[t]] >= th || headW[tris[t + 1]] >= th || headW[tris[t + 2]] >= th) { removed++; continue; }
                    keep.Add(tris[t]); keep.Add(tris[t + 1]); keep.Add(tris[t + 2]);
                }
                mesh.SetTriangles(keep, sm);
            }
            RemovedHeadTriangles = removed;

            var go = new GameObject(body.name + "_Visible");
            go.transform.SetParent(body.transform.parent, false);
            go.transform.localPosition = body.transform.localPosition;
            go.transform.localRotation = body.transform.localRotation;
            go.transform.localScale = body.transform.localScale;
            var copy = go.AddComponent<SkinnedMeshRenderer>();
            copy.sharedMesh = mesh;
            copy.bones = bones;
            copy.rootBone = body.rootBone;
            copy.sharedMaterials = body.sharedMaterials;
            copy.localBounds = body.localBounds;
            copy.updateWhenOffscreen = true;
            copy.shadowCastingMode = ShadowCastingMode.Off;   // 그림자는 원래 메시가 (머리 포함)
            VisibleBody = copy;
            body.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }
}
