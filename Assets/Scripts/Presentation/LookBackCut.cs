using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 작업 R18d (GDD-beta B19.1): 뒤돌아보기 컷. 쫓는 적이 있는 구간(1·2·3·5)마다 한 번, 1박 동안 1인칭 카메라를 뒤로 돌려
    /// 뒤처진 추격 적을 보여 주고 돌아온다. 자리(달리기 박)는 판정 시각표에서 판정 원이 떠 있지 않고 장단 노드를 칠 일이 없는 틈으로 골랐다
    /// (GameConfig.Fx.LookBackRunBeats). 돌리는 쪽은 시작할 때 뒤에 있는 적이 가까운 쪽. 연출을 끄면(F1) 하지 않는다.
    /// </summary>
    public class LookBackCut : MonoBehaviour
    {
        public CameraFx cameraFx;
        public PresentationDirector presentation;

        PathFollower player;
        StageDirector director;
        double lastV = double.NaN;
        int active = -1;
        float sign = 1f;

        /// <summary>지금 컷 중 (테스트용)과 마지막 컷 번호·시작 박·돌린 각도(도).</summary>
        public bool Active => active >= 0;
        public int LastIndex { get; private set; } = -1;
        public float CurrentYaw { get; private set; }
        public int Count { get; private set; }

        void Start()
        {
            var boot = GameBootstrap.Instance;
            if (boot != null) { player = boot.player; director = boot.director; }
        }

        /// <summary>컷 i의 진행 (0~1)에서 카메라 각도 비율: 빠르게 돌아보고, 잠깐 보고, 돌아온다.</summary>
        public static float Profile(float u)
        {
            float a = GameConfig.Fx.LookBackTurnPart, h = 1f - 2f * a;
            if (u <= 0f || u >= 1f) return 0f;
            if (u < a) return Mathf.SmoothStep(0f, 1f, u / a);
            if (u < a + h) return 1f;
            return Mathf.SmoothStep(1f, 0f, (u - a - h) / a);
        }

        void LateUpdate()
        {
            if (player == null || cameraFx == null) return;
            double v = player.VisualRunBeat;
            var beats = GameConfig.Fx.LookBackRunBeats;
            bool enabledFx = presentation == null || presentation.Enabled;
            bool fighting = director != null && (director.CurrentEncounter != null || director.Rewinding);
            if (active < 0 && !double.IsNaN(lastV) && enabledFx && !fighting)
                for (int i = 0; i < beats.Length; i++)
                    if (lastV < beats[i] && v >= beats[i] && v < beats[i] + 0.5) { Begin(i); break; }
            lastV = v;
            if (active < 0) { CurrentYaw = 0f; return; }
            float u = (float)((v - beats[active]) / GameConfig.Fx.LookBackBeats);
            if (u >= 1f || u < 0f || fighting || !enabledFx) { active = -1; CurrentYaw = 0f; return; }
            CurrentYaw = sign * GameConfig.Fx.LookBackYaw * Profile(u);
            cameraFx.RequestYaw(CameraOwner.LookBack, CurrentYaw, 0.05f);
        }

        void Begin(int i)
        {
            active = i;
            LastIndex = i;
            Count++;
            // 뒤에 있는 적이 가까운 쪽으로 돈다 (오른쪽 뒤면 시계 방향)
            var pl = player.transform;
            float best = float.PositiveInfinity; sign = 1f;
            foreach (var t in Enemies())
            {
                var d = t.position - pl.position; d.y = 0f;
                if (d.sqrMagnitude < 1f || d.sqrMagnitude > 50f * 50f || Vector3.Dot(d, pl.forward) > 0f) continue;
                if (d.sqrMagnitude < best) { best = d.sqrMagnitude; sign = Vector3.Dot(d, pl.right) >= 0f ? 1f : -1f; }
            }
        }

        /// <summary>보이는 적의 발 자리들 (전투 적 + 사건 소품 적).</summary>
        public static System.Collections.Generic.IEnumerable<Transform> Enemies()
        {
            foreach (var a in Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))
            {
                if (a.Defeated || !a.gameObject.activeInHierarchy) continue;
                var lp = a.GetComponent<EnemyLeapIn>();
                if (lp != null && lp.Phase == "Waiting") continue;
                yield return a.transform;
            }
            var pd = Object.FindAnyObjectByType<EventPropDirector>();
            if (pd != null)
                foreach (var p in pd.Props)
                    if (p != null && p.enemy != null && p.enemy.gameObject.activeInHierarchy && p.kind != StageChart.TypeForeshadow) yield return p.enemy;
        }
    }
}
