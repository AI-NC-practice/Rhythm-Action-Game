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
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetBool(MirrorParam, false);
            animator.CrossFadeInFixedTime(state, GameConfig.Body.CrossFadeSeconds, 0);
        }

        /// <summary>판정 결과 하나에 맞는 동작을 재생한다.</summary>
        public void Play(ObstacleCategory? category, ArrowKey key, Judgement r)
        {
            string action = category.HasValue ? BodyMoves.ActionOf(category.Value) : BodyMoves.ActionOfKey(key);
            string state = BodyMoves.State(action, r);
            bool mirror = BodyMoves.Mirror(action, key);
            LastState = state;
            LastMirror = mirror;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            float len = seconds.TryGetValue(state, out var s) ? s : 1f;
            animator.SetBool(MirrorParam, mirror);
            animator.SetFloat(ActionSpeedParam, BodyMoves.SpeedToFit(len, GameConfig.Body.ActionBeats, GameConfig.SecondsPerBeat));
            animator.CrossFadeInFixedTime(state, GameConfig.Body.CrossFadeSeconds, 0);
        }

        public void PlayRun()
        {
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

        /// <summary>
        /// Animator는 직접 돌린다: 박자 시계와 같은 시간(clock.Now)으로 진행해 음악·판정과 어긋나지 않는다.
        /// 속도 배율(판정 속도·회피 슬로모션)을 곱하고, 일시정지면 멈춘다.
        /// </summary>
        void Update()
        {
            if (animator == null) return;
            double now = clock != null ? clock.Now : Time.unscaledTimeAsDouble;
            double dt = double.IsNaN(lastNow) ? 0 : now - lastNow;
            lastNow = now;
            if (dt < 0 || dt > 0.25) dt = 0;                       // 시간 되돌림·긴 멈춤은 건너뜀
            float speed = clock == null ? 1f : clock.IsPaused ? 0f : clock.IsRunning ? clock.SpeedMultiplier : 1f;
            animator.Update((float)dt * speed);
            followDt += (float)dt * speed;
        }

        float followDt;

        /// <summary>머리 뼈 따라가기는 LateUpdate(Cinemachine Brain보다 먼저, 실행 순서 −50)에서, 시간은 박자 시계 시간으로.</summary>
        void LateUpdate()
        {
            FollowHead(followDt);
            followDt = 0f;
        }

        /// <summary>
        /// 카메라 받침이 머리 뼈를 따라간다. 박자 시계 시간으로 한다 (작업 T15 수정: 실제 프레임 시간이면 무거운 씬에서
        /// 프레임마다 움직임이 달라진다). Update에서 하면 컷 카메라 검사 사이에 받침이 움직이므로 LateUpdate에서 한다.
        /// </summary>
        void FollowHead(float dt)
        {
            if (cameraRig == null || head == null) return;
            var now = transform.InverseTransformPoint(head.position);
            var target = (now - headRest) * GameConfig.Body.HeadFollowWeight;
            float k = 1f - Mathf.Exp(-GameConfig.Body.HeadFollowSharpness * dt);
            rigOffset = Vector3.Lerp(rigOffset, target, k);
            cameraRig.localPosition = new Vector3(0f, GameConfig.EyeHeight, GameConfig.Body.EyeForward) + rigOffset;
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
