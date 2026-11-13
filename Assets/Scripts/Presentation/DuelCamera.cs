using System;
using System.Collections.Generic;
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
        /// <summary>3인칭 컷 동안 보이게 그릴 플레이어 몸 (회색 캡슐과 칼 막대).</summary>
        public Renderer[] playerBody;

        public enum Mode { Off, Guard, Strike }

        public CinemachineBrain Brain { get; private set; }
        public CinemachineCamera GuardCam { get; private set; }
        public CinemachineCamera StrikeCam { get; private set; }
        /// <summary>3인칭 컷 카메라 A~D (GDD-beta B11.5).</summary>
        public readonly Dictionary<string, CinemachineCamera> CutCams = new Dictionary<string, CinemachineCamera>();
        /// <summary>지금 3인칭이어야 하는 컷 (A~D), 1인칭이면 null.</summary>
        public string Cut { get; private set; }
        /// <summary>지금 컷의 도는 각도 (도, −sweep/2 → +sweep/2).</summary>
        public float CutAngle { get; private set; }
        /// <summary>지금 컷 카메라가 바라보는 두 사람 중간점.</summary>
        public Vector3 CutLookAt { get; private set; }
        /// <summary>3인칭 카메라가 화면에 영향을 주는 중 (컷이거나 컷과 블렌드 중). 이때 플레이어 몸을 보이게 그린다.</summary>
        public bool ThirdPerson { get; private set; }
        public Mode Current { get; private set; } = Mode.Off;
        /// <summary>지금 줌 펀치 FOV (도, 0 또는 음수).</summary>
        public float PunchFov { get; private set; }
        /// <summary>줌 펀치 횟수 (테스트용).</summary>
        public int PunchCount { get; private set; }

        /// <summary>테스트에서 바꿔 끼우는 실제 시간 (초).</summary>
        public Func<float> RealTime = () => Time.unscaledTime;

        float punchAt = -999f;
        bool subscribed;
        CinemachineCamera desired;
        Vector3 cutMid, cutFwd;
        string cutFrozenFor;

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
            foreach (var c in GameConfig.DuelCuts)
            {
                var cc = MakeCam("DuelCut" + c.name, Vector3.zero, GameConfig.DuelGuardCamFov);
                cc.transform.SetParent(transform, false);   // 3인칭은 연출 층 밖 (세상 기준)
                CutCams[c.name] = cc;
            }
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
                return e != null && e.IsBossDuel;   // 마무리 뒤에도 처형 컷 D가 끝나는 72박까지 (B11.5)
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
            var e = director.CurrentEncounter;

            var next = NextStep();
            var mode = IsStrikeStep(next) ? Mode.Strike : Mode.Guard;
            bool starting = Current == Mode.Off;
            if (Current == Mode.Off)
            {
                GuardCam.gameObject.SetActive(true);
                StrikeCam.gameObject.SetActive(true);
                foreach (var c in CutCams.Values) c.gameObject.SetActive(true);
                if (judgeFx != null) judgeFx.CameraOverride = true;
                desired = null;
                Brain.enabled = true;
            }
            Current = mode;

            // 3인칭 컷 (B11.5): 판정 원이 떠 있으면 절대 3인칭이 아니다
            var clock = director.clock;
            double rel = clock != null ? clock.CurrentBeat - e.StartBeat : 0;
            bool finished = e.Done && e.Execution != ExecutionKind.None;
            Cut = BossDuel.ActiveCut(rel, clock != null ? clock.SpeedMultiplier : 1f, finished, out float progress);
            if (Cut != null && RingVisible(e)) Cut = null;
            if (starting) Cut = null;      // 첫 프레임은 1인칭: 이미 컷 A 구간이어도 다음 프레임에 0.4초 블렌드로 들어간다
            if (Cut == null) cutFrozenFor = null;

            var want = Cut != null ? CutCams[Cut] : mode == Mode.Strike ? StrikeCam : GuardCam;
            if (want != desired)
            {
                bool cutInvolved = Cut != null || IsCutCam(desired);
                Brain.DefaultBlend = Cut == "D"
                    ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f)                                   // 처형: 바로 자름
                    : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut,
                        cutInvolved ? GameConfig.DuelCutBlendSeconds : GameConfig.DuelCamBlendSeconds);
                want.Prioritize();
                desired = want;
            }

            // 1인칭 자세: 두 카메라 모두 지금 판정 회전을 따라가고, 공격 카메라는 베는 쪽으로 롤 (← 왼쪽, 그 밖 오른쪽)
            var turn = judgeFx != null ? judgeFx.Turn : Quaternion.identity;
            GuardCam.transform.localRotation = turn;
            float roll = next != null && next.Key == ArrowKey.Left ? GameConfig.DuelStrikeCamRoll : -GameConfig.DuelStrikeCamRoll;
            StrikeCam.transform.localRotation = turn * Quaternion.Euler(0f, 0f, roll);
            if (Cut != null) PlaceCut(e, Cut, progress);

            PunchFov = Punch(RealTime() - punchAt);
            float fx = (judgeFx != null ? judgeFx.FxFov : 0f) + PunchFov;
            SetFov(GuardCam, GameConfig.DuelGuardCamFov + fx);
            SetFov(StrikeCam, GameConfig.DuelStrikeCamFov + fx);
            Brain.ManualUpdate();

            var blend = Brain.ActiveBlend;
            ThirdPerson = IsCutCam(Brain.ActiveVirtualCamera) || (blend != null && (IsCutCam(blend.CamA) || IsCutCam(blend.CamB)));
            ShowBody(ThirdPerson);
        }

        bool IsCutCam(ICinemachineCamera c)
        {
            if (c == null) return false;
            foreach (var cc in CutCams.Values) if (ReferenceEquals(cc, c)) return true;
            return false;
        }

        /// <summary>결투 판정 원이 하나라도 떠 있는지.</summary>
        static bool RingVisible(StageDirector.Encounter e)
        {
            foreach (var c in e.Cues) if (c.Appeared && !c.Judged && !c.Cancelled) return true;
            return false;
        }

        /// <summary>
        /// 3인칭 컷 카메라 자리: 두 사람 중간점(컷이 시작될 때 고정)을 바라보며, 옆에서 시작해 sweep만큼 돈다.
        /// 반지름은 r0 → r1 (3.5~5 m), 높이는 컷마다 (D는 낮은 각도).
        /// </summary>
        void PlaceCut(StageDirector.Encounter e, string name, float p)
        {
            var def = Array.Find(GameConfig.DuelCuts, c => c.name == name);
            if (cutFrozenFor != name)
            {
                var a = director.player != null ? director.player.transform.position : Vector3.zero;
                var b = e.Actors != null && e.Actors.Length > 0 && e.Actors[0] != null ? e.Actors[0].transform.position : a + Vector3.forward * 3f;
                cutMid = (a + b) * 0.5f;
                cutFwd = b - a; cutFwd.y = 0f;
                if (cutFwd.sqrMagnitude < 1e-6f) cutFwd = Vector3.forward;
                cutFwd.Normalize();
                cutFrozenFor = name;
            }
            float t = Mathf.SmoothStep(0f, 1f, p);
            CutAngle = -def.sweep * 0.5f + def.sweep * t;
            float r = Mathf.Lerp(def.r0, def.r1, t);
            var side = Vector3.Cross(Vector3.up, cutFwd);                     // 두 사람을 잇는 선의 옆
            var dir = Quaternion.AngleAxis(CutAngle, Vector3.up) * side;
            CutLookAt = cutMid + Vector3.up * GameConfig.DuelCutLookHeight;
            var pos = cutMid + dir * r + Vector3.up * def.height;
            var cam = CutCams[name];
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(CutLookAt - pos, Vector3.up));
            SetFov(cam, GameConfig.DuelGuardCamFov);
        }

        void ShowBody(bool show)
        {
            if (playerBody == null) return;
            var mode = show ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            foreach (var r in playerBody) if (r != null && r.shadowCastingMode != mode) r.shadowCastingMode = mode;
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
            Cut = null; cutFrozenFor = null; desired = null;
            ThirdPerson = false;
            ShowBody(false);
            Brain.enabled = false;
            GuardCam.gameObject.SetActive(false);
            StrikeCam.gameObject.SetActive(false);
            foreach (var c in CutCams.Values) c.gameObject.SetActive(false);
            if (judgeFx != null) judgeFx.CameraOverride = false;
            cam.transform.localPosition = Vector3.zero;   // Brain이 옮긴 자리를 연출 층 기준으로 되돌린다
            cam.transform.localRotation = Quaternion.identity;
            PunchFov = 0f;
        }
    }
}
