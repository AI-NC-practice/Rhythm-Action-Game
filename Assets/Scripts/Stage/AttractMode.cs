using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>
    /// 작업 R36 (A안): 게임 씬 안의 시작화면. 메뉴(TitleMenu)가 떠 있는 동안 주인공이 1챕터 시작선 뒤 진입로를 실제 달리기 속도로 달리고
    /// 3인칭 카메라가 뒤를 따라간다 (진입로 끝에 닿으면 어두워졌다 처음부터). 시작을 누르면 멈추지 않고 시작선까지 달리며 카메라가
    /// 1인칭 자리로 옮겨 가고, 시작선에 닿는 순간 GameBootstrap.StartRun(0) — 음악·박자·1챕터가 끊김 없이 시작된다.
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

        enum Phase { Off, Loop, FadeOut, FadeIn, Lead }
        Phase phase = Phase.Off;
        Camera cam;
        Image fade;
        float d, fadeT, leadT, leadDur, leadD0;

        public bool Active => phase != Phase.Off;
        public bool Leading => phase == Phase.Lead;
        /// <summary>진입로 위 거리 (시작선 = 0, 뒤는 음수, m). 테스트용.</summary>
        public float ApproachDistance => d;
        public Camera AttractCamera => cam;
        /// <summary>되풀이할 때 어두워진 정도 (테스트용).</summary>
        public float FadeAlpha => fade != null ? fade.color.a : 0f;
        /// <summary>테스트에서 프레임 시간을 정할 때.</summary>
        public System.Func<float> DeltaTime = () => Time.unscaledDeltaTime;

        static float RunSpeed => GameConfig.MetersPerBeat / GameConfig.SecondsPerBeat;

        /// <summary>시작선 뒤 진입로 위 자리 (경로 처음 방향을 뒤로 곧게 늘린 선, 높이는 시작선과 같음).</summary>
        public static Vector3 ApproachPoint(float dist, out Vector3 forward)
        {
            var p0 = MapLayout.PointAt(0f, out forward);
            return p0 + forward * dist;
        }

        /// <summary>시작화면 시작: 3인칭 카메라를 켜고 주인공을 진입로 처음에.</summary>
        public void Begin()
        {
            phase = Phase.Loop;
            d = -GameConfig.Attract.ApproachLength;
            var pc = boot.playerCamera;
            var go = new GameObject("AttractCamera", typeof(Camera));
            cam = go.GetComponent<Camera>();
            cam.fieldOfView = pc.fieldOfView;
            cam.nearClipPlane = pc.nearClipPlane;
            cam.farClipPlane = pc.farClipPlane;
            cam.clearFlags = pc.clearFlags;
            cam.backgroundColor = pc.backgroundColor;
            pc.enabled = false;
            if (fade == null) BuildFade();
            fade.color = new Color(0f, 0f, 0f, 0f);
            if (menu != null) menu.SetActive(true);
            if (hideDuringAttract != null) foreach (var g in hideDuringAttract) if (g != null) g.SetActive(false);
            if (body != null) { body.ThirdPersonCut = true; if (body.body != null) body.body.shadowCastingMode = ShadowCastingMode.On; }
            if (boot.player != null) boot.player.Running = false;
            PlacePlayer();
            PlaceCamera(0f);
        }

        /// <summary>시작을 눌렀다: 시작선까지 달리며 1인칭으로 (메뉴는 TitleMenu가 숨김).</summary>
        public void BeginStart()
        {
            if (phase == Phase.Off || phase == Phase.Lead) return;
            if (phase == Phase.FadeOut || phase == Phase.FadeIn) { if (fade != null) fade.color = new Color(0f, 0f, 0f, 0f); }
            phase = Phase.Lead;
            leadT = 0f;
            leadD0 = d;
            leadDur = Mathf.Clamp(-d / (RunSpeed * 1.6f), GameConfig.Attract.LeadMinSeconds, GameConfig.Attract.LeadMaxSeconds);
        }

        void BuildFade()
        {
            var cv = new GameObject("AttractFade", typeof(Canvas)).GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 2000;
            fade = new GameObject("Fade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fade.transform.SetParent(cv.transform, false);
            var rt = (RectTransform)fade.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            fade.raycastTarget = false;
        }

        void Update()
        {
            if (phase == Phase.Off) return;
            float dt = Mathf.Max(0f, DeltaTime());
            switch (phase)
            {
                case Phase.Loop:
                    d += RunSpeed * dt;
                    if (d >= -GameConfig.Attract.LoopEnd) { phase = Phase.FadeOut; fadeT = 0f; }
                    break;
                case Phase.FadeOut:
                    d += RunSpeed * dt;
                    fadeT += dt;
                    fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(fadeT / GameConfig.Attract.FadeSeconds));
                    if (fadeT >= GameConfig.Attract.FadeSeconds) { d = -GameConfig.Attract.ApproachLength; phase = Phase.FadeIn; fadeT = 0f; }
                    break;
                case Phase.FadeIn:
                    d += RunSpeed * dt;
                    fadeT += dt;
                    fade.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(fadeT / GameConfig.Attract.FadeSeconds));
                    if (fadeT >= GameConfig.Attract.FadeSeconds) phase = Phase.Loop;
                    break;
                case Phase.Lead:
                    leadT += dt;
                    float u = Mathf.Clamp01(leadT / leadDur);
                    // 처음·끝 속도가 달리기 속도인 3차 곡선 (시작선에 실제 달리기 속도로 도착 → StartRun으로 이어짐)
                    float m = RunSpeed * leadDur;
                    float u2 = u * u, u3 = u2 * u;
                    d = (2f * u3 - 3f * u2 + 1f) * leadD0 + (u3 - 2f * u2 + u) * m + (u3 - u2) * m;
                    if (u >= 1f) { Finish(); return; }
                    break;
            }
            PlacePlayer();
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
            if (body != null) { body.ThirdPersonCut = false; if (body.body != null) body.body.shadowCastingMode = ShadowCastingMode.ShadowsOnly; }
            if (hideDuringAttract != null) foreach (var g in hideDuringAttract) if (g != null) g.SetActive(true);
            if (menu != null) menu.SetActive(false);
            if (boot.player != null) boot.player.Running = true;
            boot.playerCamera.enabled = true;
            if (cam != null) Destroy(cam.gameObject);
            cam = null;
            if (fade != null) fade.color = new Color(0f, 0f, 0f, 0f);
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
