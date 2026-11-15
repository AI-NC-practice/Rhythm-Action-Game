using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RhythmActionGame
{
    /// <summary>
    /// 판정 연출 (GDD 4.2): 원이 떠 있는 동안 화면 채도 −60%, FOV 70→58, 판정 지점 쪽으로 최대 25° 회전.
    /// 들어갈 때 0.15초, 판정 후 돌아올 때 0.2초.
    /// </summary>
    public class JudgeFx : MonoBehaviour
    {
        public JudgeSystem judge;
        public Camera cam;
        public Volume volume;

        ColorAdjustments color;
        Vignette vignette;
        float weight;          // 0 = 평소, 1 = 판정 연출 최대
        Quaternion turnTarget = Quaternion.identity;

        /// <summary>테스트에서 바꿔 끼우는 프레임 시간 (실제 초).</summary>
        public System.Func<float> DeltaTime = () => Time.unscaledDeltaTime;

        /// <summary>평소 FOV에 더할 값 (속도감 효과, T08).</summary>
        public float FovBonus;

        /// <summary>연출 층의 FOV 합 (CameraFx, GDD-beta B5). 최종 FOV는 45~85°로 자른다.</summary>
        public float FxFov;

        /// <summary>
        /// 다른 카메라 장치(무사 결투의 Cinemachine, GDD-beta B11.5)가 카메라를 맡는 중이면 true.
        /// 이때는 카메라 FOV·회전을 쓰지 않고 Turn과 FxFov만 계산해 둔다. 화면 효과(채도·노출·비네트)는 그대로.
        /// </summary>
        public bool CameraOverride;

        /// <summary>지금 판정 회전 (카메라 부모 기준, 판정 연출 세기만큼).</summary>
        public Quaternion Turn => Quaternion.Slerp(Quaternion.identity, turnTarget, weight);

        public float Weight => weight;
        public float Saturation => color != null ? color.saturation.value : 0f;
        /// <summary>판정 중 노출 (GDD-beta B1.4).</summary>
        public float Exposure => color != null ? color.postExposure.value : 0f;
        /// <summary>판정 중 비네트 세기 (GDD-beta B1.4).</summary>
        public float VignetteIntensity => vignette != null ? vignette.intensity.value : 0f;
        public float Fov => cam != null ? cam.fieldOfView : 0f;

        void Awake() => EnsureSetup();

        public void EnsureSetup()
        {
            if (volume == null)
            {
                volume = gameObject.GetComponent<Volume>();
                if (volume == null) volume = gameObject.AddComponent<Volume>();
            }
            volume.isGlobal = true;
            volume.priority = 10;
            if (volume.profile == null) volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            if (!volume.profile.TryGet(out color)) color = volume.profile.Add<ColorAdjustments>(true);
            color.saturation.overrideState = true;
            color.postExposure.overrideState = true;
            if (!volume.profile.TryGet(out vignette)) vignette = volume.profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }
            Apply();
        }

        void LateUpdate() => Step(DeltaTime());

        /// <summary>dt(실제 초)만큼 연출을 진행한다.</summary>
        public void Step(float dt)
        {
            if (color == null) EnsureSetup();
            var cue = judge != null ? judge.FirstActiveStageCue : null;   // 장단 노드는 판정 화면을 켜지 않음 (B18.2)
            float target = cue != null ? 1f : 0f;
            float duration = target > weight ? GameConfig.JudgeEnterSeconds : GameConfig.JudgeExitSeconds;
            weight = Mathf.MoveTowards(weight, target, dt / duration);

            if (cue != null && cue.WorldPoint.HasValue && cam != null && cam.transform.parent != null)
                turnTarget = LimitedTurn(cam.transform.parent, cue.WorldPoint.Value);
            Apply();
        }

        /// <summary>카메라 부모 기준으로 판정 지점을 보는 회전. 최대 25°로 제한.</summary>
        static Quaternion LimitedTurn(Transform rig, Vector3 point)
        {
            var dir = rig.InverseTransformPoint(point);
            if (dir.sqrMagnitude < 1e-6f) return Quaternion.identity;
            var look = Quaternion.LookRotation(dir.normalized, Vector3.up);
            return Quaternion.RotateTowards(Quaternion.identity, look, GameConfig.JudgeMaxTurnDegrees);
        }

        void Apply()
        {
            if (color != null)
            {
                color.saturation.value = Mathf.Lerp(0f, GameConfig.JudgeSaturation, weight);
                color.postExposure.value = Mathf.Lerp(0f, GameConfig.JudgeExposure, weight);
            }
            if (vignette != null) vignette.intensity.value = Mathf.Lerp(0f, GameConfig.JudgeVignette, weight);
            if (cam != null && !CameraOverride)
            {
                float baseFov = GameConfig.FovDefault + FovBonus;
                cam.fieldOfView = Mathf.Clamp(Mathf.Lerp(baseFov, GameConfig.FovZoom, weight) + FxFov, GameConfig.Fx.FovMin, GameConfig.Fx.FovMax);
                cam.transform.localRotation = Turn;
            }
        }
    }
}
