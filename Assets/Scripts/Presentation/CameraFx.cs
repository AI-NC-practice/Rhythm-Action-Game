using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>카메라 회전(yaw) 주인 우선순위 (제안서 4장). 높을수록 우선.</summary>
    public enum CameraOwner
    {
        Parkour = 0,
        JudgeTurn = 1,
        EventGaze = 2,
        SurroundTurn = 3,
        Execution = 4,
        Windmill = 5,
        /// <summary>작업 R18d: 뒤돌아보기 컷 (판정 원이 없는 틈에만, 가장 높음).</summary>
        LookBack = 6,
    }

    /// <summary>
    /// 카메라 연출 층 (GDD-beta B5, 제안서 1.2·4장). 기본 플레이 카메라 위(FxOffset 트랜스폼)에 "더하는" 값만 다룬다.
    /// 모든 층이 0이면 FxOffset은 원점·무회전이라 원래 카메라와 똑같다.
    /// - 층 합 규칙: 롤 ±10°, 고개 ±35°, FOV 45~85°(JudgeFx가 최종 적용), 흔들림 세기 합 ≤ 1·초당 1.5 감소
    /// - 회전(yaw) 주인은 하나: 높은 우선순위가 쓰는 동안 낮은 요청은 버린다
    /// </summary>
    public class CameraFx : MonoBehaviour
    {
        /// <summary>연출을 얹을 트랜스폼 (Motion과 PlayerCamera 사이).</summary>
        public Transform fxOffset;
        /// <summary>최종 FOV를 적용하는 곳 (FOV 층 합을 넘긴다).</summary>
        public JudgeFx judgeFx;

        /// <summary>테스트에서 바꿔 끼우는 프레임 시간 (실제 초).</summary>
        public System.Func<float> DeltaTime = () => Time.unscaledDeltaTime;

        class Impulse
        {
            public FxPreset P;
            public float Side;
            public float Age;
        }

        readonly List<Impulse> impulses = new List<Impulse>();
        float trauma;
        float clock;            // 실제 시간 누적 (흔들림 노이즈 입력)
        float shakeHz = 20f;

        CameraOwner? yawOwner;
        float yawValue, yawUntil;

        public float Trauma => trauma;
        public int ActiveCount => impulses.Count;
        public CameraOwner? YawOwner => yawOwner;
        public float Yaw => yawOwner.HasValue ? yawValue : 0f;

        /// <summary>마지막으로 계산한 층 합 (제한 적용 후).</summary>
        public Vector3 Position { get; private set; }
        public float Roll { get; private set; }
        public float Pitch { get; private set; }
        public float Fov { get; private set; }
        public float WobbleYaw { get; private set; }

        /// <summary>연출이 하나도 없고 모든 층이 0인지.</summary>
        public bool IsIdle => impulses.Count == 0 && trauma <= 1e-4f && !yawOwner.HasValue;

        /// <summary>연출 하나를 얹는다. side: 왼쪽 −1, 오른쪽 +1 (방향 없는 연출은 1).</summary>
        public void Add(FxPreset p, float side = 1f)
        {
            impulses.Add(new Impulse { P = p, Side = side });
            if (p.Trauma > 0)
            {
                trauma = Mathf.Min(GameConfig.Fx.TraumaMax, trauma + p.Trauma);
                shakeHz = p.ShakeHz;
            }
        }

        /// <summary>회전 주인 요청. 더 높은 주인이 쓰는 중이면 false.</summary>
        public bool RequestYaw(CameraOwner owner, float yawDegrees, float seconds)
        {
            if (yawOwner.HasValue && clock < yawUntil && owner < yawOwner.Value) return false;
            yawOwner = owner;
            yawValue = yawDegrees;
            yawUntil = clock + seconds;
            return true;
        }

        /// <summary>모든 연출을 즉시 지운다 (연출 끄기).</summary>
        public void ClearAll()
        {
            impulses.Clear();
            trauma = 0;
            yawOwner = null;
            Apply(Vector3.zero, 0, 0, 0, 0, 0);
        }

        /// <summary>시간 a에서의 층 세기 (0~1): Rise 동안 올라가 Hold 유지, Release 동안 내려옴.</summary>
        public static float Envelope(FxPreset p, float a)
        {
            if (a < 0) return 0;
            if (a < p.Rise) return p.Rise <= 0 ? 1f : a / p.Rise;
            a -= p.Rise;
            if (a < p.Hold) return 1f;
            a -= p.Hold;
            if (a >= p.Release) return 0f;
            float u = a / p.Release;
            return 1f - u * u * (3f - 2f * u);    // smoothstep으로 내려옴
        }

        void LateUpdate() => Step(DeltaTime());

        public void Step(float dt)
        {
            clock += dt;
            Vector3 pos = Vector3.zero;
            float roll = 0, pitch = 0, fov = 0, wobble = 0;
            for (int i = impulses.Count - 1; i >= 0; i--)
            {
                var im = impulses[i];
                im.Age += dt;
                if (im.Age >= im.P.Total) { impulses.RemoveAt(i); continue; }
                float e = Envelope(im.P, im.Age);
                pos += new Vector3(im.P.SideShift * im.Side, im.P.Height, -im.P.KickBack) * e;
                roll += im.P.Roll * im.Side * e;
                pitch += im.P.Pitch * e;
                fov += im.P.Fov * e;
                wobble += im.P.YawWobble * e * Mathf.Sin(im.Age * Mathf.PI * 2f * 6f);
            }
            trauma = Mathf.Max(0f, trauma - GameConfig.Fx.TraumaDecayPerSecond * dt);
            if (yawOwner.HasValue && clock >= yawUntil) yawOwner = null;

            // 흔들림: 세기² × 최대값, Perlin 노이즈
            float s = trauma * trauma;
            if (s > 0f)
            {
                float t = clock * shakeHz;
                pos += new Vector3(Noise(t, 0.1f), Noise(t, 1.7f), 0) * (GameConfig.Fx.ShakeMaxPosition * s);
                pitch += Noise(t, 3.3f) * GameConfig.Fx.ShakeMaxRotation * s;
                roll += Noise(t, 5.9f) * GameConfig.Fx.ShakeMaxRotation * s;
            }
            Apply(pos, Mathf.Clamp(roll, -GameConfig.Fx.RollLimit, GameConfig.Fx.RollLimit),
                Mathf.Clamp(pitch, -GameConfig.Fx.PitchLimit, GameConfig.Fx.PitchLimit), fov, wobble, Yaw);
        }

        static float Noise(float t, float seed) => Mathf.PerlinNoise(t, seed) * 2f - 1f;

        void Apply(Vector3 pos, float roll, float pitch, float fov, float wobble, float yaw)
        {
            Position = pos; Roll = roll; Pitch = pitch; Fov = fov; WobbleYaw = wobble;
            if (fxOffset != null)
            {
                fxOffset.localPosition = pos;
                fxOffset.localRotation = Quaternion.Euler(pitch, yaw + wobble, roll);
            }
            if (judgeFx != null) judgeFx.FxFov = fov;
        }
    }
}
