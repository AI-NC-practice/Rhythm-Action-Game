using UnityEngine;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>
    /// 작업 R35: 시작화면 지붕 달리기 (러닝머신). 주인공은 제자리(z=0)에서 달리기 동작만 하고, 기와지붕 조각들이 뒤(−z)로 흘러간다.
    /// 뒤로 RecycleBehind m 넘게 빠진 조각은 지우지 않고 맨 앞에 다시 붙인다 — 새로 만들거나 지우는 일 없이 지붕 몇 채로 끝없이 달린다.
    /// 조각 사이 틈은 짧게 뛰어넘는다. BeginFall이 불리면 옆으로 미끄러져 지붕에서 떨어지며 화면이 어두워지고 끝나면 알린다.
    /// SceneBuilder가 지붕 조각·주인공·카메라를 만들어 넣는다. 수치는 GameConfig.Title.
    /// </summary>
    public class TitleRunner : MonoBehaviour
    {
        public Transform player;
        public Animator animator;
        public Transform cam;
        /// <summary>가운데 줄 지붕 조각 (같은 모델, 길이 ridgeLength)과 발 높이 (용마루).</summary>
        public Transform[] ridge;
        public float ridgeLength;
        public float ridgeTop;
        /// <summary>양옆 낮은 지붕 줄 조각과 조각 간격.</summary>
        public Transform[] sides;
        public float sidePeriod;

        Image fade;
        bool falling;
        float fallT, vy, side;
        System.Action onFallen;

        /// <summary>뒤에서 앞으로 다시 붙인 횟수, 떨어지는 중, 떨어지기가 끝남 (테스트용).</summary>
        public int Recycled { get; private set; }
        public bool Falling => falling;
        public bool FallDone { get; private set; }
        /// <summary>지금 틈을 뛰어넘는 중 (테스트용, 발밑 검사에서 뺌).</summary>
        public bool Hopping { get; private set; }
        public float FadeAlpha => fade != null ? fade.color.a : 0f;
        /// <summary>테스트에서 프레임 시간을 정할 때.</summary>
        public System.Func<float> DeltaTime = () => Time.deltaTime;

        void Start()
        {
            if (animator != null) animator.Play(BodyMoves.Run, 0, 0f);
            var cv = new GameObject("TitleFade", typeof(Canvas)).GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 1000;
            fade = new GameObject("Fade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fade.transform.SetParent(cv.transform, false);
            var rt = (RectTransform)fade.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            fade.color = new Color(0f, 0f, 0f, 0f);
            fade.raycastTarget = false;
            UpdateCamera();
        }

        /// <summary>시작: 지붕에서 옆으로 떨어진다. 끝나면 done (게임 씬 불러오기).</summary>
        public void BeginFall(System.Action done)
        {
            if (falling) return;
            falling = true;
            fallT = 0f;
            vy = GameConfig.Title.FallLift;
            side = 1f;
            onFallen = done;
        }

        void Update()
        {
            float dt = Mathf.Max(0f, DeltaTime());
            float dz = GameConfig.Title.RunSpeed * dt;
            float period = ridgeLength + GameConfig.Title.RidgeGap;
            if (ridge != null)
                foreach (var r in ridge)
                {
                    if (r == null) continue;
                    var p = r.position; p.z -= dz;
                    if (p.z + ridgeLength * 0.5f < -GameConfig.Title.RecycleBehind) { p.z += ridge.Length * period; Recycled++; }
                    r.position = p;
                }
            if (sides != null && sides.Length > 0)
            {
                int perRow = Mathf.Max(1, GameConfig.Title.SideCount);
                foreach (var s in sides)
                {
                    if (s == null) continue;
                    var p = s.position; p.z -= dz;
                    if (p.z < -GameConfig.Title.RecycleBehind - sidePeriod) { p.z += perRow * sidePeriod; Recycled++; }
                    s.position = p;
                }
            }

            var pp = player.position;
            if (!falling)
            {
                pp.x = 0f;
                pp.y = ridgeTop + HopOffset();
            }
            else
            {
                fallT += dt;
                vy -= GameConfig.Title.FallGravity * dt;
                pp.x += side * GameConfig.Title.FallSideSpeed * dt;
                pp.y += vy * dt;
                Hopping = false;
                if (fade != null) fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(Mathf.InverseLerp(GameConfig.Title.FallSeconds * 0.35f, GameConfig.Title.FallSeconds, fallT)));
                if (fallT >= GameConfig.Title.FallSeconds && !FallDone)
                {
                    FallDone = true;
                    var cb = onFallen; onFallen = null;
                    cb?.Invoke();
                }
            }
            player.position = pp;
            UpdateCamera();
        }

        /// <summary>주인공 앞뒤 가장 가까운 틈까지 거리로 뛰는 높이 (포물선).</summary>
        float HopOffset()
        {
            Hopping = false;
            if (ridge == null) return 0f;
            float half = GameConfig.Title.RunSpeed * GameConfig.Title.HopSeconds * 0.5f;
            float best = float.PositiveInfinity;
            foreach (var r in ridge)
            {
                if (r == null) continue;
                float gapCenter = r.position.z + ridgeLength * 0.5f + GameConfig.Title.RidgeGap * 0.5f;
                if (Mathf.Abs(gapCenter) < Mathf.Abs(best)) best = gapCenter;
            }
            if (Mathf.Abs(best) >= half) return 0f;
            Hopping = true;
            float u = best / half;
            return GameConfig.Title.HopHeight * (1f - u * u);
        }

        void UpdateCamera()
        {
            if (cam == null || player == null) return;
            var p = player.position;
            float baseY = falling ? Mathf.Lerp(ridgeTop, p.y, GameConfig.Title.FallCamFollow) : ridgeTop;
            var anchor = new Vector3(0f, baseY, 0f);
            cam.position = anchor + GameConfig.Title.CamOffset;
            var look = falling ? new Vector3(p.x * 0.6f, p.y + 1.0f, GameConfig.Title.CamLook.z * 0.5f) : anchor + GameConfig.Title.CamLook;
            cam.rotation = Quaternion.LookRotation(look - cam.position, Vector3.up);
        }
    }
}
