using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RhythmActionGame
{
    /// <summary>
    /// 아슬아슬 회피 슬로모션 (GDD-beta B17.4 2차). 노드가 다가오는 동안에는 느려지지 않는다.
    /// 회피 슬로모션 판정(대문 ↓, 포위 마지막 동시 공격, 무사 A37.5·A66)을 PERFECT·NORMAL로 피한 직후
    /// 0.06초에 걸쳐 박자 시계·음악·화면(Time.timeScale)을 0.3배로 늦추고, 실제 0.6초 머문 뒤 0.15초에 걸쳐 돌린다.
    /// 다음 판정 목표 0.6박 전에는 돌아오기 시작한다. FAIL이면 슬로모션이 없다.
    /// 슬로 동안 음악은 낮고(BeatClock.SetSlowFactor) 먹먹하게(저역 통과·작게), 화면은 회색(ScreenFx.SlowSaturation).
    /// 대문은 문 건너편 앞 3인칭 카메라로 주인공과 문을 함께 잡고, 칼은 숙이며 올려다보는 카메라 연출(NearMiss)을 건다.
    /// 처형·윈드밀 마무리 슬로모션(SlowMotion, GDD 4.2) 동안에도 음악 먹먹함·회색 화면을 건다.
    /// </summary>
    [DefaultExecutionOrder(1000)]   // 카메라 연출(CameraFx·BeatFx·DuelCamera) 뒤에 대문 컷 카메라를 놓는다
    public class BulletTimeFx : MonoBehaviour
    {
        public BeatClock clock;
        public JudgeSystem judge;
        public StageDirector director;
        /// <summary>처형·윈드밀 슬로모션 (먹먹함·회색만 함께).</summary>
        public SlowMotion slowMotion;
        /// <summary>음악 음량 배율을 넣는 곳.</summary>
        public SfxPlayer sfx;
        /// <summary>음악 AudioSource에 붙은 저역 통과 필터.</summary>
        public AudioLowPassFilter lowPass;
        /// <summary>회색 화면.</summary>
        public ScreenFx screenFx;
        /// <summary>칼 회피 카메라 연출 (NearMiss).</summary>
        public CameraFx cameraFx;
        /// <summary>대문 컷에 쓰는 플레이 카메라와 보이게 할 몸.</summary>
        public Camera cam;
        public Renderer[] playerBody;
        /// <summary>연출 끄기(F1)면 카메라 연출만 뺀다 (슬로·소리·회색은 게임 규칙처럼 그대로).</summary>
        public PresentationDirector presentation;

        /// <summary>테스트에서 바꿔 끼우는 프레임 시간 (실제 초).</summary>
        public Func<float> DeltaTime = () => Time.unscaledDeltaTime;

        enum Phase { None, In, Hold, Release }
        Phase phase;
        JudgeCue active;
        float t, releaseFrom, releaseT;
        bool ownsTimeScale;
        bool gateCut;
        Vector3 gateCamPos;
        Quaternion gateCamRot;

        /// <summary>지금 슬로모션 중인 회피 판정 (돌아오는 중 포함).</summary>
        public JudgeCue Active => active;
        public bool Releasing => phase == Phase.Release;
        /// <summary>대문 3인칭 컷 중인지.</summary>
        public bool GateCut => gateCut;
        /// <summary>슬로모션이 시작된·돌아온·(다음 판정이 가까워) 건너뛴 횟수 (테스트용).</summary>
        public int StartCount { get; private set; }
        public int ReleaseCount { get; private set; }
        public int SkipCount { get; private set; }
        /// <summary>먹먹함·회색 0~1.</summary>
        public float Muffle { get; private set; }
        /// <summary>대문 컷 카메라 자리 (테스트용).</summary>
        public Vector3 GateCamPosition => gateCamPos;

        public event Action<JudgeCue> Started;
        public event Action<JudgeCue> Released;

        void OnEnable()
        {
            if (clock != null) clock.Restarted += OnRestarted;
            if (director != null) director.StageReset += ResetState;
            if (judge != null) judge.CueJudged += OnJudged;
        }

        void OnDisable()
        {
            if (clock != null) clock.Restarted -= OnRestarted;
            if (director != null) director.StageReset -= ResetState;
            if (judge != null) judge.CueJudged -= OnJudged;
            ResetState();
        }

        void OnRestarted(double _) => ResetState();

        /// <summary>되감기·다시 시작: 바로 평소 속도로.</summary>
        public void ResetState()
        {
            active = null;
            phase = Phase.None;
            EndGateCut();
            if (clock != null && clock.IsRunning) clock.SetSlowFactor(1f, clock.Now);
            if (ownsTimeScale && Time.timeScale > 0f && (slowMotion == null || !slowMotion.IsActive)) Time.timeScale = 1f;
            ownsTimeScale = false;
            ApplyLook();
        }

        /// <summary>회피 슬로모션 판정인지: 전투 판정의 BulletTime, 사건의 BulletSlot.</summary>
        public static bool IsBulletCue(JudgeCue cue)
        {
            if (cue == null) return false;
            if (cue.Tag is CombatStep s) return s.BulletTime;
            if (cue.Tag is StageDirector.ObstacleRun r) return r.Def != null && r.Def.BulletSlot >= 0 && r.Cues.IndexOf(cue) == r.Def.BulletSlot;
            return false;
        }

        /// <summary>다음 판정 목표 − ClearBeats (이 박에는 돌아오기 시작해야 함). 남은 판정이 없으면 +∞.</summary>
        double LimitBeat(JudgeCue except)
        {
            double limit = double.PositiveInfinity;
            if (judge == null) return limit;
            foreach (var c in judge.Cues)
                if (!c.Judged && c != except) limit = Math.Min(limit, c.TargetBeat - GameConfig.BulletTime.ClearBeats);
            return limit;
        }

        void OnJudged(JudgeCue cue, Judgement r)
        {
            if (!isActiveAndEnabled || r == Judgement.Fail || !IsBulletCue(cue)) return;
            if (clock == null || !clock.IsRunning || active != null) return;
            if (clock.CurrentBeat >= LimitBeat(cue) - 0.05) { SkipCount++; return; }   // 다음 판정이 너무 가까움: 노드 우선
            active = cue;
            phase = Phase.In;
            t = 0f;
            StartCount++;
            bool fx = presentation == null || presentation.Enabled;
            if (cue.Tag is StageDirector.ObstacleRun run) { if (fx) BeginGateCut(run, cue); }
            else if (fx && cameraFx != null) cameraFx.Add(GameConfig.Fx.NearMiss);
            Started?.Invoke(cue);
        }

        void BeginGateCut(StageDirector.ObstacleRun run, JudgeCue cue)
        {
            if (cam == null) return;
            int slot = run.Cues.IndexOf(cue);
            double at = run.Event.runBeat + (slot >= 0 && slot < run.SlotOffsets.Count ? run.SlotOffsets[slot] : 0);
            var gate = MapLayout.PointAt((float)(at * GameConfig.MetersPerBeat), out var fwd);
            var right = Vector3.Cross(Vector3.up, fwd).normalized;
            gateCamPos = gate + fwd * GameConfig.BulletTime.GateCamAheadMeters + Vector3.up * GameConfig.BulletTime.GateCamHeight + right * GameConfig.BulletTime.GateCamSideMeters;
            var look = gate + Vector3.up * GameConfig.BulletTime.GateLookHeight - fwd * 0.5f;
            gateCamRot = Quaternion.LookRotation(look - gateCamPos, Vector3.up);
            gateCut = true;
            SetBodyVisible(true);
        }

        void EndGateCut()
        {
            if (!gateCut) return;
            gateCut = false;
            if (cam != null) { cam.transform.localPosition = Vector3.zero; cam.transform.localRotation = Quaternion.identity; }
            SetBodyVisible(false);
        }

        void SetBodyVisible(bool on)
        {
            if (playerBody == null) return;
            foreach (var r in playerBody) if (r != null) r.shadowCastingMode = on ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
        }

        void Update() => Tick(DeltaTime());

        public void Tick(float dt)
        {
            if (clock == null || !clock.IsRunning || clock.IsPaused) { ApplyLook(); return; }
            if (active != null)
            {
                dt = Mathf.Max(0f, dt);
                t += dt;
                double beat = clock.CurrentBeat;
                double now = clock.Now;
                bool mustGo = beat >= LimitBeat(active);
                if (phase == Phase.In)
                {
                    clock.SetSlowFactor(Mathf.Lerp(1f, GameConfig.BulletTime.Factor, Mathf.Clamp01(t / GameConfig.BulletTime.InSeconds)), now);
                    if (t >= GameConfig.BulletTime.InSeconds) phase = Phase.Hold;
                }
                if ((phase == Phase.In || phase == Phase.Hold) && (t >= GameConfig.BulletTime.HoldSeconds || mustGo))
                {
                    phase = Phase.Release;
                    releaseFrom = clock.SlowFactor;
                    releaseT = 0f;
                    ReleaseCount++;
                    Released?.Invoke(active);
                }
                else if (phase == Phase.Release)
                {
                    releaseT += dt;
                    float u = Mathf.Clamp01(releaseT / GameConfig.BulletTime.ReleaseSeconds);
                    clock.SetSlowFactor(Mathf.Lerp(releaseFrom, 1f, u), now);
                    if (u >= 1f) { active = null; phase = Phase.None; EndGateCut(); }
                }
            }
            ApplyTimeScale();
            ApplyLook();
        }

        void LateUpdate()
        {
            if (gateCut && cam != null) cam.transform.SetPositionAndRotation(gateCamPos, gateCamRot);
        }

        void ApplyTimeScale()
        {
            if (Time.timeScale <= 0f || (slowMotion != null && slowMotion.IsActive)) return;   // 일시정지·처형 슬로모션이 우선
            float f = clock.SlowFactor;
            if (f < 1f - 1e-4f) { Time.timeScale = f; ownsTimeScale = true; }
            else if (ownsTimeScale) { Time.timeScale = 1f; ownsTimeScale = false; }
        }

        /// <summary>먹먹함·회색: 회피 슬로 배율 또는 처형 슬로모션.</summary>
        void ApplyLook()
        {
            float bullet = clock != null ? Mathf.Clamp01((1f - clock.SlowFactor) / (1f - GameConfig.BulletTime.Factor)) : 0f;
            float story = slowMotion != null && slowMotion.IsActive ? 1f : 0f;
            Muffle = Mathf.Max(bullet, story);
            if (lowPass != null)
                lowPass.cutoffFrequency = GameConfig.BulletTime.LowPassOpenHz * Mathf.Pow(GameConfig.BulletTime.LowPassMuffledHz / GameConfig.BulletTime.LowPassOpenHz, Muffle);
            if (sfx != null) sfx.MusicFxGain = Mathf.Lerp(1f, GameConfig.BulletTime.MusicGain, Muffle);
            if (screenFx != null) screenFx.SlowSaturation = GameConfig.BulletTime.Saturation * Muffle;
        }
    }
}
