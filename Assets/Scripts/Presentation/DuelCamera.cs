using System;
using Unity.Cinemachine;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 무사 결투 1인칭 카메라 (GDD-beta B11.5). 결투 동안에만 Cinemachine Brain이 카메라를 맡고,
    /// 다음 판정 종류에 따라 막기 카메라(−0.1 m, FOV 62°)와 공격 카메라(+0.4 m, FOV 54°, 롤 6°)를 0.3초 Ease In-Out으로 오간다.
    /// 두 가상 카메라는 연출 층(FxOffset) 아래에 있어 흔들림·밀림이 그대로 얹히고, FOV에는 연출 층 FOV와 줌 펀치를 더한다.
    /// 결투 밖에서는 Brain을 끄고 지금 카메라 구조(JudgeFx가 FOV·판정 회전)를 그대로 쓴다.
    /// </summary>
    [DefaultExecutionOrder(200)]   // CameraFx·JudgeFx(LateUpdate) 다음에 Brain을 돌린다
    public class DuelCamera : MonoBehaviour
    {
        public StageDirector director;
        public JudgeSystem judge;
        public JudgeFx judgeFx;
        public Camera cam;
        public Transform fxOffset;

        public enum Mode { Off, Guard, Strike }

        public CinemachineBrain Brain { get; private set; }
        public CinemachineCamera GuardCam { get; private set; }
        public CinemachineCamera StrikeCam { get; private set; }
        public Mode Current { get; private set; } = Mode.Off;
        /// <summary>지금 줌 펀치 FOV (도, 0 또는 음수).</summary>
        public float PunchFov { get; private set; }
        /// <summary>줌 펀치 횟수 (테스트용).</summary>
        public int PunchCount { get; private set; }

        /// <summary>테스트에서 바꿔 끼우는 실제 시간 (초).</summary>
        public Func<float> RealTime = () => Time.unscaledTime;

        float punchAt = -999f;
        bool subscribed;

        public bool Active => Current != Mode.Off;

        void Awake() => Build();

        void Build()
        {
            if (Brain != null || cam == null || fxOffset == null) return;
            Brain = cam.GetComponent<CinemachineBrain>();
            if (Brain == null) Brain = cam.gameObject.AddComponent<CinemachineBrain>();
            Brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            Brain.IgnoreTimeScale = true;                 // 히트 스톱·슬로모션과 무관하게 0.3초
            Brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, GameConfig.DuelCamBlendSeconds);
            Brain.enabled = false;
            GuardCam = MakeCam("DuelGuardCam", new Vector3(0f, GameConfig.DuelGuardCamHeight, 0f), GameConfig.DuelGuardCamFov);
            StrikeCam = MakeCam("DuelStrikeCam", new Vector3(0f, 0f, GameConfig.DuelStrikeCamForward), GameConfig.DuelStrikeCamFov);
        }

        CinemachineCamera MakeCam(string name, Vector3 local, float fov)
        {
            var go = new GameObject(name);
            go.transform.SetParent(fxOffset, false);
            go.transform.localPosition = local;
            var c = go.AddComponent<CinemachineCamera>();
            var lens = c.Lens;
            lens.FieldOfView = fov;
            lens.NearClipPlane = cam.nearClipPlane;
            lens.FarClipPlane = cam.farClipPlane;
            c.Lens = lens;
            go.SetActive(false);
            return c;
        }

        void OnEnable() => Subscribe();
        void OnDisable()
        {
            if (judge != null && subscribed) judge.CueJudged -= OnJudged;
            subscribed = false;
        }

        void Subscribe()
        {
            if (subscribed || judge == null) return;
            judge.CueJudged += OnJudged;
            subscribed = true;
        }

        bool InDuel
        {
            get
            {
                var e = director != null ? director.CurrentEncounter : null;
                return e != null && e.IsBossDuel && !e.Done;
            }
        }

        /// <summary>칼이 부딪침 (결투 중 FAIL이 아닌 판정): FOV −6° 줌 펀치 0.15초.</summary>
        void OnJudged(JudgeCue cue, Judgement r)
        {
            if (r == Judgement.Fail || !InDuel || !(cue.Tag is CombatStep)) return;
            punchAt = RealTime();
            PunchCount++;
        }

        /// <summary>줌 펀치 모양: 처음 1/3은 −6°를 유지하고 나머지 동안 0으로 돌아온다.</summary>
        public static float Punch(float age)
        {
            float d = GameConfig.DuelClashPunchSeconds;
            if (age < 0f || age >= d) return 0f;
            float hold = d / 3f;
            return age < hold ? GameConfig.DuelClashPunchFov : GameConfig.DuelClashPunchFov * (1f - (age - hold) / (d - hold));
        }

        /// <summary>다음에 판정할 결투 단계 (판정·취소되지 않은 것 중 가장 이른 것).</summary>
        CombatStep NextStep()
        {
            var e = director.CurrentEncounter;
            CombatStep best = null; double bestBeat = double.MaxValue;
            foreach (var c in e.Cues)
                if (!c.Judged && !c.Cancelled && c.TargetBeat < bestBeat && c.Tag is CombatStep s) { best = s; bestBeat = c.TargetBeat; }
            return best;
        }

        public static bool IsStrikeStep(CombatStep s) => s != null && (s.Kind == CombatStepKind.Strike || s.Kind == CombatStepKind.Lock);

        void LateUpdate()
        {
            Build();
            Subscribe();
            if (Brain == null) return;
            if (!InDuel) { Deactivate(); return; }

            var next = NextStep();
            var mode = IsStrikeStep(next) ? Mode.Strike : Mode.Guard;
            if (Current == Mode.Off)
            {
                GuardCam.gameObject.SetActive(true);
                StrikeCam.gameObject.SetActive(true);
                if (judgeFx != null) judgeFx.CameraOverride = true;
                Brain.enabled = true;
            }
            if (mode != Current) (mode == Mode.Strike ? StrikeCam : GuardCam).Prioritize();
            Current = mode;

            // 자세: 두 카메라 모두 지금 판정 회전을 따라가고, 공격 카메라는 베는 쪽으로 롤 (← 왼쪽, 그 밖 오른쪽)
            var turn = judgeFx != null ? judgeFx.Turn : Quaternion.identity;
            GuardCam.transform.localRotation = turn;
            float roll = next != null && next.Key == ArrowKey.Left ? GameConfig.DuelStrikeCamRoll : -GameConfig.DuelStrikeCamRoll;
            StrikeCam.transform.localRotation = turn * Quaternion.Euler(0f, 0f, roll);

            PunchFov = Punch(RealTime() - punchAt);
            float fx = (judgeFx != null ? judgeFx.FxFov : 0f) + PunchFov;
            SetFov(GuardCam, GameConfig.DuelGuardCamFov + fx);
            SetFov(StrikeCam, GameConfig.DuelStrikeCamFov + fx);
            Brain.ManualUpdate();
        }

        static void SetFov(CinemachineCamera c, float fov)
        {
            var lens = c.Lens;
            lens.FieldOfView = Mathf.Clamp(fov, GameConfig.Fx.FovMin, GameConfig.Fx.FovMax);
            c.Lens = lens;
        }

        void Deactivate()
        {
            if (Current == Mode.Off) return;
            Current = Mode.Off;
            Brain.enabled = false;
            GuardCam.gameObject.SetActive(false);
            StrikeCam.gameObject.SetActive(false);
            if (judgeFx != null) judgeFx.CameraOverride = false;
            cam.transform.localPosition = Vector3.zero;   // Brain이 옮긴 자리를 연출 층 기준으로 되돌린다
            cam.transform.localRotation = Quaternion.identity;
            PunchFov = 0f;
        }
    }
}
