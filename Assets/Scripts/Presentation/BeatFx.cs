using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 화면이 박에 맞춰 숨 쉼 (GDD-beta B17.2). 박자 시계 박자로 매 프레임:
    /// - 박 펄스: 장구 레인 판정 표시·HUD가 살짝 커졌다 돌아오고, 화면 가장자리(비네트)가 어두워졌다 돌아온다. 마디 첫 박은 더 크게.
    ///   판정 원 테두리에는 걸지 않는다 (B52: 누르는 순간 원이 출렁여 맞추기 어려움).
    /// - 걸음·카메라: 달리는 동안 1박 = 한 걸음(박 순간 가장 낮음, 왼발·오른발 번갈아 좌우), 멈춰 있으면 박마다 살짝 내려앉는다.
    ///   카메라 계층 Motion → BeatBob(여기) → FxOffset(CameraFx) → PlayerCamera.
    /// 연출 끄기(F1)·일시정지·시계 멈춤이면 모두 0. 연출이라 판정·이동·점수는 바꾸지 않는다.
    /// </summary>
    public class BeatFx : MonoBehaviour
    {
        public BeatClock clock;
        public PathFollower follower;
        /// <summary>걸음 출렁임을 얹는 트랜스폼 (Motion과 FxOffset 사이).</summary>
        public Transform bob;
        public ScreenFx screenFx;
        public JudgeRingView ringView;
        public JangdanLaneView lane;
        public GameHud hud;
        public PresentationDirector presentation;

        /// <summary>테스트에서 바꿔 끼우는 프레임 시간 (실제 초).</summary>
        public System.Func<float> DeltaTime = () => Time.unscaledDeltaTime;

        /// <summary>지금 박 펄스 세기 (보통 박 0~1, 마디 첫 박 0~BarAccent).</summary>
        public float Pulse { get; private set; }
        /// <summary>달리는 정도 0~1 (부드럽게 바뀜).</summary>
        public float Run { get; private set; }
        public Vector3 BobPosition { get; private set; }
        public float BobRoll { get; private set; }
        /// <summary>연출이 켜져 있고 시계가 도는 중인지.</summary>
        public bool Active { get; private set; }

        void LateUpdate() => Step(DeltaTime());

        public void Step(float dt)
        {
            bool fxOn = presentation == null || presentation.Enabled;
            Active = fxOn && clock != null && clock.IsRunning && !clock.IsPaused;
            float runTarget = follower != null && follower.Running && !follower.VisualOverride.HasValue ? 1f : 0f;
            Run = Mathf.MoveTowards(Run, runTarget, GameConfig.BeatFx.RunBlendPerSecond * Mathf.Max(0f, dt));
            if (Active)
            {
                double beat = clock.CurrentBeat;
                Pulse = BeatPulse.Amount(beat);
                BobPosition = BeatPulse.Bob(beat, Run, out float roll);
                BobRoll = roll;
            }
            else
            {
                Pulse = 0f;
                BobPosition = Vector3.zero;
                BobRoll = 0f;
            }
            Apply();
        }

        void Apply()
        {
            if (bob != null)
            {
                bob.localPosition = BobPosition;
                bob.localRotation = Quaternion.Euler(0f, 0f, BobRoll);
            }
            if (screenFx != null) screenFx.BeatVignette = Pulse * GameConfig.BeatFx.Vignette;
            if (lane != null && lane.HitMarker != null)
                lane.HitMarker.rectTransform.localScale = Vector3.one * (1f + Pulse * GameConfig.BeatFx.LaneScale);
            if (hud != null && hud.Hearts != null)
            {
                var s = Vector3.one * (1f + Pulse * GameConfig.BeatFx.HudScale);
                foreach (var h in hud.Hearts) h.rectTransform.localScale = s;
                hud.SpeedText.rectTransform.localScale = s;
                hud.ScoreText.rectTransform.localScale = s;
                hud.ComboText.rectTransform.localScale = s;
            }
        }
    }
}
