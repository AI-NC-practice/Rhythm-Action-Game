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
        /// <summary>작업 R29: 뒤에서 쫓아오는 무리 (SceneBuilder가 만든 적 모델, 평소 숨김).</summary>
        public Transform[] pursuers;

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
            float a = GameConfig.Fx.LookBackTurnPart, r = GameConfig.Fx.LookBackReturnPart, h = 1f - a - r;
            if (u <= 0f || u >= 1f) return 0f;
            if (u < a) return Mathf.SmoothStep(0f, 1f, u / a);
            if (u < a + h) return 1f;
            return Mathf.SmoothStep(1f, 0f, (u - a - h) / r);   // 작업 R29: 천천히 돌아옴
        }

        /// <summary>컷 i의 길이 (박).</summary>
        public static double LengthOf(int i) => i >= 0 && i < GameConfig.Fx.LookBackLengths.Length ? GameConfig.Fx.LookBackLengths[i] : GameConfig.Fx.LookBackBeats;

        bool[] hopping;
        /// <summary>달리기 박 rb가 장애물 사건 구간 안인지 (쫓는 무리도 플레이어 길을 따라 넘어간다).</summary>
        /// <summary>그 달리기 박의 길이 지붕 위인지 (길 높이).</summary>
        public static bool OnRoof(double runBeat) => MapLayout.PointAt((float)(runBeat * GameConfig.MetersPerBeat)).y > GameConfig.Fx.PursuerRoofHeight;

        bool InObstacle(float rb)
        {
            var ch = director != null ? director.Chart : null;
            if (ch == null) return false;
            foreach (var e in ch.events)
                if (e.IsObstacle && rb >= e.runBeat - 0.5f && rb <= e.runBeat + ChartCheck.Length(e) + 0.5f) return true;
            return false;
        }
        /// <summary>쫓는 무리 k가 지붕 사이 틈을 뛰어넘는 중 (테스트용, 발밑 검사에서 뺌).</summary>
        public bool PursuerHopping(int k) => hopping != null && k >= 0 && k < hopping.Length && hopping[k];

        /// <summary>쫓는 무리가 보이는 중 (테스트용).</summary>
        public bool PursuersShown { get; private set; }

        /// <summary>작업 R29: 컷 1박 전 ~ 컷 뒤 1박 동안 플레이어 뒤 경로 위를 달려 따라온다 (조금씩 좁혀 옴). 그 밖에는 숨김.</summary>
        void UpdatePursuers(double v, bool fighting)
        {
            if (pursuers == null || pursuers.Length == 0 || player == null) return;
            var beats = GameConfig.Fx.LookBackRunBeats;
            int near = -1;
            if (!fighting)
                for (int i = 0; i < beats.Length; i++)
                    if (v >= beats[i] - GameConfig.Fx.PursuerLeadBeats && v <= beats[i] + LengthOf(i) + 1.0) { near = i; break; }
            // 지붕 위 컷에서는 쓰지 않는다 — 플레이어 지붕 길(충돌 상자) 높이를 따라가면 보이는 기와보다 떠 보였다. 지붕 위 적(앞질러 착지 등)이 대신 보인다.
            bool show = near >= 0 && !OnRoof(beats[near]);
            if (show != PursuersShown)
            {
                PursuersShown = show;
                foreach (var p in pursuers) if (p != null) p.gameObject.SetActive(show);
            }
            if (!show) return;
            float since = (float)(v - (beats[near] - GameConfig.Fx.PursuerLeadBeats));
            float d0 = player.VisualDistance;
            for (int k = 0; k < pursuers.Length; k++)
            {
                var p = pursuers[k];
                if (p == null) continue;
                float gap = GameConfig.Fx.PursuerGaps[k % GameConfig.Fx.PursuerGaps.Length] - GameConfig.Fx.PursuerGainPerBeat * since;
                float lat = GameConfig.Fx.PursuerLaterals[k % GameConfig.Fx.PursuerLaterals.Length];
                var at = MapLayout.PointAt(Mathf.Max(0f, d0 - Mathf.Max(3f, gap)), out var fwd);
                if (at.y > GameConfig.Fx.PursuerRoofHeight) lat *= 0.1f;   // 지붕 위: 플레이어처럼 용마루 한 줄로 (옆으로 비키면 경사 아래 공중)
                at += MapLayout.RightOf(fwd) * lat;
                // 발은 그 자리 아래 가까운 면(지붕·담·땅)에. 1 m 넘게 아래뿐이면 지붕 사이 틈 — 플레이어처럼 길 높이로 뛰어넘는 중(Hopping)
                float best = float.NegativeInfinity;
                foreach (var h in Physics.RaycastAll(at + Vector3.up * 1.5f, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore))
                    if (!h.transform.IsChildOf(p) && h.point.y > best && h.point.y <= at.y + 0.3f) best = h.point.y;   // 길보다 높은 장애물 상자 위로는 올라가지 않음
                // 지붕 구간: 충돌 상자(지붕 길)는 보이는 기와보다 높다 → 보이는 장식 지붕 윗면(용마루)이 더 낮으면 그 높이
                float vis = DressingInstancer.TopBelow(at + Vector3.up * 1.5f, at.y + 0.3f);
                if (!float.IsNegativeInfinity(vis) && vis < best && best - vis < 3f) best = vis;
                bool hop = float.IsNegativeInfinity(best) || best < at.y - 1.0f || InObstacle((d0 - Mathf.Max(3f, gap)) / GameConfig.MetersPerBeat);   // 장애물(지붕 뛰어넘기·오르기 등)은 플레이어처럼 넘어가는 중
                if (!hop) at.y = best;
                if (hopping == null || hopping.Length != pursuers.Length) hopping = new bool[pursuers.Length];
                hopping[k] = hop;
                p.SetPositionAndRotation(at, Quaternion.LookRotation(fwd, Vector3.up));
            }
        }

        void LateUpdate()
        {
            if (player == null || cameraFx == null) return;
            double v = player.VisualRunBeat;
            var beats = GameConfig.Fx.LookBackRunBeats;
            bool enabledFx = presentation == null || presentation.Enabled;
            bool fighting = director != null && (director.CurrentEncounter != null || director.Rewinding);
            UpdatePursuers(v, fighting);
            if (active < 0 && !double.IsNaN(lastV) && enabledFx && !fighting)
                for (int i = 0; i < beats.Length; i++)
                    if (lastV < beats[i] && v >= beats[i] && v < beats[i] + 0.5) { Begin(i); break; }
            lastV = v;
            if (active < 0) { CurrentYaw = 0f; return; }
            float u = (float)((v - beats[active]) / LengthOf(active));
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
            var lb = Object.FindAnyObjectByType<LookBackCut>();
            if (lb != null && lb.pursuers != null && lb.PursuersShown)
                foreach (var p in lb.pursuers) if (p != null && p.gameObject.activeInHierarchy) yield return p;
            var pd = Object.FindAnyObjectByType<EventPropDirector>();
            if (pd != null)
                foreach (var p in pd.Props)
                    if (p != null && p.enemy != null && p.enemy.gameObject.activeInHierarchy && p.kind != StageChart.TypeForeshadow) yield return p.enemy;
        }
    }
}
