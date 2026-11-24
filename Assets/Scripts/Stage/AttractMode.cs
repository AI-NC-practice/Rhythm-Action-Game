using UnityEngine;
using UnityEngine.Rendering;

namespace RhythmActionGame
{
    /// <summary>
    /// 작업 R36 (A안): 게임 씬 안의 시작화면. 메뉴(TitleMenu)가 떠 있는 동안 주인공이 1챕터 시작선 뒤 진입로를 실제 달리기 속도로 달리고
    /// 3인칭 카메라가 뒤를 따라간다. 시작을 누르면 멈추지 않고 시작선까지 달리며 카메라가 1인칭 자리로 옮겨 가고,
    /// 시작선에 닿는 순간 GameBootstrap.StartRun(0) — 음악·박자·1챕터가 끊김 없이 시작된다.
    /// 작업 R37: 어두워졌다 처음으로 돌아가지 않는다. 진입로 집은 TilePeriod마다 똑같이 되풀이하고 시작화면 동안은 새벽 안개(Mist)가 짙어서,
    /// 주인공이 되풀이 구간 끝에 닿으면 TilePeriod만큼 뒤로 옮겨도 화면이 같다 (먼 배경 그림도 같이 옮김). 시작을 누르면 안개가 걷히며 실제 맵이 드러난다.
    /// 수치는 GameConfig.Attract. SceneBuilder가 붙인다.
    /// </summary>
    [DefaultExecutionOrder(20000)]
    public class AttractMode : MonoBehaviour
    {
        /// <summary>다음 Game 씬을 시작화면으로 열지. 빌드 실행(에디터·배치 모드·-startGame 아님)과 "타이틀로"가 켠다. 한 번 쓰면 꺼진다.</summary>
        public static bool AttractOnLoad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitFlag()
        {
            var args = System.Environment.GetCommandLineArgs();
            AttractOnLoad = !Application.isEditor && !Application.isBatchMode
                && System.Array.IndexOf(args, "-startGame") < 0 && System.Array.IndexOf(args, "-devChapter") < 0;
        }

        public GameBootstrap boot;
        public PlayerBody body;
        /// <summary>시작화면 메뉴 캔버스 (평소 꺼 둠)와 시작화면 동안 숨길 것 (HUD).</summary>
        public GameObject menu;
        public GameObject[] hideDuringAttract;
        /// <summary>먼 배경 그림 원통 (작업 R37: 되풀이할 때 같이 옮기고 안개에 묻는다). 없으면 이름으로 찾는다.</summary>
        public Renderer backdrop;

        enum Phase { Off, Loop, Lead }
        Phase phase = Phase.Off;
        Camera cam;
        float d, leadT, leadDur, leadD0;

        // 작업 R37: 평소 안개·배경 자리 (시작화면이 끝나면 그대로 돌린다)
        bool savedFog;
        FogMode savedFogMode;
        float savedFogStart, savedFogEnd;
        Vector3 backdropHome, backdropOffset, leadBackdropOffset;
        MaterialPropertyBlock mpb;
        static readonly int MistId = Shader.PropertyToID("_Mist");

        public bool Active => phase != Phase.Off;
        public bool Leading => phase == Phase.Lead;
        /// <summary>진입로 위 거리 (시작선 = 0, 뒤는 음수, m). 테스트용.</summary>
        public float ApproachDistance => d;
        public Camera AttractCamera => cam;
        /// <summary>되풀이한 횟수 (테스트용).</summary>
        public int Wraps { get; private set; }
        /// <summary>지금 안개 정도 (1 = 시작화면 안개, 0 = 평소). 테스트용.</summary>
        public float Mist { get; private set; }
        /// <summary>먼 배경 그림의 원래 자리 (테스트용).</summary>
        public Vector3 BackdropHome => backdropHome;
        /// <summary>테스트에서 프레임 시간을 정할 때.</summary>
        public System.Func<float> DeltaTime = () => Time.unscaledDeltaTime;

        static float RunSpeed => GameConfig.MetersPerBeat / GameConfig.SecondsPerBeat;
        static float LoopFar => -GameConfig.Attract.LoopNear - GameConfig.Attract.TilePeriod;

        /// <summary>시작선 뒤 진입로 위 자리 (경로 처음 방향을 뒤로 곧게 늘린 선, 높이는 시작선과 같음).</summary>
        public static Vector3 ApproachPoint(float dist, out Vector3 forward)
        {
            var p0 = MapLayout.PointAt(0f, out forward);
            return p0 + forward * dist;
        }

        /// <summary>시작화면 시작: 3인칭 카메라를 켜고 주인공을 진입로에, 새벽 안개.</summary>
        public void Begin()
        {
            phase = Phase.Loop;
            d = -GameConfig.Attract.ApproachLength;
            Wraps = 0;
            var pc = boot.playerCamera;
            var go = new GameObject("AttractCamera", typeof(Camera));
            cam = go.GetComponent<Camera>();
            cam.fieldOfView = pc.fieldOfView;
            cam.nearClipPlane = pc.nearClipPlane;
            cam.farClipPlane = pc.farClipPlane;
            cam.clearFlags = pc.clearFlags;
            cam.backgroundColor = pc.backgroundColor;
            pc.enabled = false;
            if (menu != null) menu.SetActive(true);
            if (hideDuringAttract != null) foreach (var g in hideDuringAttract) if (g != null) g.SetActive(false);
            if (body != null) { body.ThirdPersonCut = true; if (body.body != null) body.body.shadowCastingMode = ShadowCastingMode.On; }
            if (boot.player != null) boot.player.Running = false;
            savedFog = RenderSettings.fog; savedFogMode = RenderSettings.fogMode;
            savedFogStart = RenderSettings.fogStartDistance; savedFogEnd = RenderSettings.fogEndDistance;
            if (backdrop == null) { var b = GameObject.Find("Backdrop"); if (b != null) backdrop = b.GetComponent<Renderer>(); }
            if (backdrop != null) backdropHome = backdrop.transform.position;
            backdropOffset = Vector3.zero;
            SetMist(1f);
            PlacePlayer();
            PlaceCamera(0f);
        }

        /// <summary>시작을 눌렀다: 시작선까지 달리며 1인칭으로, 안개가 걷힌다 (메뉴는 TitleMenu가 숨김).</summary>
        public void BeginStart()
        {
            if (phase != Phase.Loop) return;
            phase = Phase.Lead;
            leadT = 0f;
            leadD0 = d;
            leadBackdropOffset = backdropOffset;
            leadDur = Mathf.Clamp(-d / (RunSpeed * GameConfig.Attract.LeadSpeedFactor), GameConfig.Attract.LeadMinSeconds, GameConfig.Attract.LeadMaxSeconds);
        }

        void Update()
        {
            if (phase == Phase.Off) return;
            float dt = Mathf.Max(0f, DeltaTime());
            switch (phase)
            {
                case Phase.Loop:
                    d += RunSpeed * dt;
                    if (d >= -GameConfig.Attract.LoopNear) Wrap();
                    break;
                case Phase.Lead:
                    leadT += dt;
                    float u = Mathf.Clamp01(leadT / leadDur);
                    // 처음·끝 속도가 달리기 속도인 3차 곡선 (시작선에 실제 달리기 속도로 도착 → StartRun으로 이어짐)
                    float m = RunSpeed * leadDur;
                    float u2 = u * u, u3 = u2 * u;
                    d = (2f * u3 - 3f * u2 + 1f) * leadD0 + (u3 - 2f * u2 + u) * m + (u3 - u2) * m;
                    // 안개가 걷히고 먼 배경 그림이 제자리로 (시작선 전에 다 끝남)
                    float clear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / GameConfig.Attract.MistClearAt));
                    backdropOffset = leadBackdropOffset * (1f - clear);
                    SetMist(1f - clear);
                    if (u >= 1f) { Finish(); return; }
                    break;
            }
            PlacePlayer();
        }

        /// <summary>되풀이: 주인공(과 카메라·먼 배경 그림)을 TilePeriod만큼 뒤로. 보이는 집·길·안개가 같아서 화면이 그대로다.</summary>
        void Wrap()
        {
            d -= GameConfig.Attract.TilePeriod;
            Wraps++;
        }

        /// <summary>테스트용: 진입로 위 거리를 바로 정한다 (되풀이 전후 같은 화면인지 비교).</summary>
        public void SetApproachDistanceForTest(float dist)
        {
            d = dist;
            PlacePlayer();
            PlaceCamera(0f);
        }

        void LateUpdate()
        {
            if (phase == Phase.Off) return;
            float w = phase == Phase.Lead ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(leadT / leadDur)) : 0f;
            // 카메라가 머리에 가까워지면 원래 몸(머리 있음)은 그림자만 — 머리카락이 화면을 덮지 않게. 1인칭용 몸(머리 없음)은 시작선까지 숨긴 채
            // (ThirdPersonCut) — 카메라가 아직 눈 자리에 다 오지 않아 목 단면이 보였다. 시작선에서 End가 1인칭 몸으로 돌린다.
            if (w > GameConfig.Attract.FirstPersonBodyAt && body != null && body.body != null && body.body.shadowCastingMode != ShadowCastingMode.ShadowsOnly)
                body.body.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            PlaceCamera(w);
        }

        void PlacePlayer()
        {
            if (boot == null || boot.player == null) return;
            var p = ApproachPoint(d, out var fwd);
            boot.player.transform.SetPositionAndRotation(p, Quaternion.LookRotation(fwd, Vector3.up));
            // 먼 배경 그림: 되풀이 구간에서는 주인공과 같이 움직인다(아주 먼 산처럼) → 되풀이로 뒤로 옮겨도 그대로
            if (phase == Phase.Loop) backdropOffset = p - ApproachPoint(LoopFar, out _);
            if (backdrop != null) backdrop.transform.position = backdropHome + backdropOffset;
        }

        void SetMist(float k)
        {
            Mist = k;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = Mathf.Lerp(savedFogStart, GameConfig.Attract.MistFogStart, k);
            RenderSettings.fogEndDistance = Mathf.Lerp(savedFogEnd, GameConfig.Attract.MistFogEnd, k);
            if (backdrop != null)
            {
                if (mpb == null) mpb = new MaterialPropertyBlock();
                backdrop.GetPropertyBlock(mpb);
                mpb.SetFloat(MistId, GameConfig.Attract.MistBackdrop * k);
                backdrop.SetPropertyBlock(mpb);
            }
        }

        /// <summary>3인칭 뒤 자리 → (w=1) 1인칭 카메라 자리로 섞는다.</summary>
        void PlaceCamera(float w)
        {
            if (cam == null || boot == null) return;
            var pl = boot.player.transform;
            var pos = pl.position + pl.rotation * GameConfig.Attract.CamOffset;
            var rot = Quaternion.LookRotation(pl.position + pl.rotation * GameConfig.Attract.CamLook - pos, Vector3.up);
            if (w > 0f && boot.playerCamera != null)
            {
                var fp = boot.playerCamera.transform;
                pos = Vector3.Lerp(pos, fp.position, w);
                rot = Quaternion.Slerp(rot, fp.rotation, w);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, boot.playerCamera.fieldOfView, w);
            }
            cam.transform.SetPositionAndRotation(pos, rot);
        }

        /// <summary>시작선 도착: 1인칭 카메라로 바꾸고 1챕터 시작.</summary>
        void Finish() => End(true);

        void End(bool start)
        {
            phase = Phase.Off;
            // 안개·먼 배경 그림을 평소대로 (시작을 누른 뒤 이미 다 걷혔지만, 중간에 끝나도 정확히 돌린다)
            backdropOffset = Vector3.zero;
            if (backdrop != null) backdrop.transform.position = backdropHome;
            SetMist(0f);
            RenderSettings.fog = savedFog; RenderSettings.fogMode = savedFogMode;
            RenderSettings.fogStartDistance = savedFogStart; RenderSettings.fogEndDistance = savedFogEnd;
            if (body != null) { body.ThirdPersonCut = false; if (body.body != null) body.body.shadowCastingMode = ShadowCastingMode.ShadowsOnly; }
            if (hideDuringAttract != null) foreach (var g in hideDuringAttract) if (g != null) g.SetActive(true);
            if (menu != null) menu.SetActive(false);
            if (boot.player != null) boot.player.Running = true;
            boot.playerCamera.enabled = true;
            if (cam != null) Destroy(cam.gameObject);
            cam = null;
            if (start) boot.StartRun(0);
        }

        /// <summary>시작화면 중에 다른 곳에서 StartRun이 불리면 (테스트·개발자 모드) 시작화면만 끝낸다.</summary>
        public void Cancel()
        {
            if (phase == Phase.Off) return;
            End(false);
        }
    }
}
