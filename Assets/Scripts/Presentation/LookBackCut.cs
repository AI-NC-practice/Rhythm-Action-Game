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
            SetupBarricades();
        }

        /// <summary>쫓는 무리를 다시 달리는 자세로 (새 컷, 되감기).</summary>
        void ResetPursuers()
        {
            chargeCut = -1;
            if (tripped != null) for (int k = 0; k < tripped.Length; k++) tripped[k] = false;
            if (pursuers == null) return;
            foreach (var p in pursuers)
            {
                if (p == null) continue;
                var fr = p.GetComponent<EnemyFigureRun>();
                if (fr != null && fr.Down && p.gameObject.activeInHierarchy) fr.ResetFigure();
            }
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

        /// <summary>
        /// 작업 R29 (평가 2회차): 지붕 위 컷의 쫓는 무리 발 높이 (SceneBuilder가 보이는 메시로 잼). 컷 i의 시작 경로 거리 roofD0[i](지붕 아니면 NaN),
        /// roofStep m 간격 roofN칸, roofY[(i*무리 수 + k)*roofN + j] (NaN = 보이는 면 없음).
        /// </summary>
        public float[] roofD0;
        public int roofN;
        public float roofStep = 0.5f;
        public float[] roofY;

        float RoofY(int i, int k, float d)
        {
            if (roofD0 == null || roofY == null || i < 0 || i >= roofD0.Length || float.IsNaN(roofD0[i]) || roofN < 2) return float.NaN;
            int K = pursuers.Length;
            float f = (d - roofD0[i]) / roofStep;
            if (f < 0f || f > roofN - 1) return float.NaN;
            int j = Mathf.Min((int)f, roofN - 2);
            int b = (i * K + k) * roofN;
            if (b + j + 1 >= roofY.Length) return float.NaN;
            float a = roofY[b + j], c = roofY[b + j + 1];
            if (float.IsNaN(a) || float.IsNaN(c)) return float.NaN;
            if (Mathf.Abs(a - c) > 0.3f) return float.NaN;   // 지붕 경계의 계단 (한 칸 0.25 m 사이 0.3 m 넘게): 뛰어오르거나 내리는 중
            return Mathf.Lerp(a, c, f - j);
        }

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

        // ---------------- 작업 R42: 막는 것 (지게·짚단 / 지붕 위 기와 더미) ----------------

        /// <summary>
        /// 작업 R42 (사용자 피드백 2026-11-24 7번 "뒤돌아보면 바로 뒤에 적이 있는데 앞의 적과 싸울 때 안 덮친다 — 지게 같은 걸 쓰러뜨려 방해"):
        /// 컷마다 막는 것 (SceneBuilder가 만든 모델들, barricades[i]의 자식). 컷 자리 BarricadeBehind m 뒤 길가에 서 있다가(땅: 지게·짚단, 지붕: 기와 더미)
        /// 컷이 시작되면 길을 가로질러 쓰러지고, 쫓아오던 무리는 달려들다 거기 걸려 넘어진다 → 앞의 적과 싸우는 동안 뒤의 적이 덮치지 않는 이유.
        /// </summary>
        public Transform[] barricades;
        Pose[][] restPose, fallPose;
        bool[] tripped;
        int chargeCut = -1;
        float[] chargeFrom;
        double chargeV0;

        /// <summary>컷 i의 막는 것 자리 (경로 거리 m).</summary>
        public static float BarricadeDistance(int i) => (float)(GameConfig.Fx.LookBackRunBeats[i] * GameConfig.MetersPerBeat) - GameConfig.Fx.BarricadeBehind;
        /// <summary>쫓는 무리 k가 막는 것에 걸려 넘어졌는지 (테스트용).</summary>
        public bool PursuerTripped(int k) => tripped != null && k >= 0 && k < tripped.Length && tripped[k];
        /// <summary>컷 i의 막는 것이 쓰러진 정도 (0 서 있음 ~ 1 길을 가로질러 누움). 테스트용.</summary>
        public float BarricadeFall(int i) => barricadeFall != null && i >= 0 && i < barricadeFall.Length ? barricadeFall[i] : 0f;
        float[] barricadeFall;

        /// <summary>막는 것이 놓일 면 높이: 지붕 위 컷은 빌드 때 보이는 메시로 잰 높이(무리 k 옆 거리), 아니면 광선으로 길 바닥.</summary>
        float SurfaceY(int i, int k, Vector3 at, float fallbackY)
        {
            if (OnRoof(GameConfig.Fx.LookBackRunBeats[i]))
            {
                var d = BarricadeDistance(i);
                for (float dd = 0f; dd <= 1.5f; dd += 0.25f)
                {
                    float y = RoofY(i, k, d - dd);
                    if (!float.IsNaN(y)) return y;
                }
                return fallbackY;
            }
            float best = float.NegativeInfinity;
            foreach (var h in Physics.RaycastAll(at + Vector3.up * 1.5f, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
                if (h.point.y > best && h.point.y <= fallbackY + 0.3f && (barricades == null || !System.Array.Exists(barricades, b => b != null && h.transform.IsChildOf(b)))) best = h.point.y;
            return float.IsNegativeInfinity(best) ? fallbackY : best;
        }

        void SetupBarricades()
        {
            var beats = GameConfig.Fx.LookBackRunBeats;
            if (barricades == null || barricades.Length != beats.Length) return;
            restPose = new Pose[beats.Length][];
            fallPose = new Pose[beats.Length][];
            barricadeFall = new float[beats.Length];
            for (int i = 0; i < beats.Length; i++)
            {
                var root = barricades[i];
                if (root == null) continue;
                var at = MapLayout.PointAt(BarricadeDistance(i), out var fwd);
                var right = MapLayout.RightOf(fwd);
                var look = Quaternion.LookRotation(fwd, Vector3.up);
                int n = root.childCount;
                restPose[i] = new Pose[n];
                fallPose[i] = new Pose[n];
                bool roof = OnRoof(beats[i]);
                for (int j = 0; j < n; j++)
                {
                    if (roof)
                    {
                        // 기와 더미: 길 오른쪽 용마루 가에 쌓여 있다가 무리가 달려오는 길 위로 흩어진다
                        int k = j % Mathf.Max(1, pursuers != null ? pursuers.Length : 3);
                        float lat = GameConfig.Fx.PursuerLaterals[k % GameConfig.Fx.PursuerLaterals.Length] * GameConfig.Fx.PursuerRoofLateralScale;
                        var stackAt = at + right * GameConfig.Fx.BarricadeRoofStackSide;
                        float ys = SurfaceY(i, 1, stackAt, at.y);
                        restPose[i][j] = new Pose(new Vector3(stackAt.x, ys + j * GameConfig.Fx.BarricadeTileHeight, stackAt.z) + fwd * 0.05f * j, look * Quaternion.Euler(0f, 12f * j, 0f));
                        // 흩어지는 자리 = 그 거리의 보이는 지붕 칸 (지붕 사이 틈이면 앞뒤로 가까운 칸으로 옮김 — 예전엔 틈 위 3 m 공중에 뜸)
                        float back = 0.35f * j, yf = float.NaN;
                        for (float dd = 0f; dd <= 2f && float.IsNaN(yf); dd += 0.25f)
                            foreach (float sg in new[] { 1f, -1f })
                            {
                                float y = RoofY(i, k, BarricadeDistance(i) - back - sg * dd);
                                if (!float.IsNaN(y)) { yf = y; back += sg * dd; break; }
                            }
                        var fallAt = at + right * lat - fwd * back;
                        if (float.IsNaN(yf)) yf = SurfaceY(i, k, fallAt, at.y);
                        fallPose[i][j] = new Pose(new Vector3(fallAt.x, yf, fallAt.z), look * Quaternion.Euler(0f, 35f * j - 30f, 8f));
                    }
                    else
                    {
                        // 지게(왼쪽)·짚단(오른쪽): 길가에 서 있다가 길 가운데 쪽으로 넘어져 길을 가로막는다
                        float side = j % 2 == 0 ? -1f : 1f;
                        var standAt = at + right * side * GameConfig.Fx.BarricadeSide;
                        float y = SurfaceY(i, 0, standAt, at.y);
                        var rest = look;
                        restPose[i][j] = new Pose(new Vector3(standAt.x, y, standAt.z), rest);
                        var tip = Quaternion.FromToRotation(Vector3.up, -side * right);   // 위가 길 가운데 쪽으로
                        fallPose[i][j] = new Pose(new Vector3(standAt.x, y + GameConfig.Fx.BarricadeLyingLift, standAt.z), tip * rest);
                        // 쓰러진 모습의 실제 바닥(보이는 메시 경계)을 면 높이에 맞춘다 (예전: 짚단이 땅속 0.7 m·지게 0.4 m 박힘)
                        var piece = root.GetChild(j);
                        piece.SetPositionAndRotation(fallPose[i][j].position, fallPose[i][j].rotation);
                        var rs = piece.GetComponentsInChildren<Renderer>();
                        if (rs.Length > 0)
                        {
                            var bnd = rs[0].bounds; foreach (var r in rs) bnd.Encapsulate(r.bounds);
                            fallPose[i][j].position += Vector3.up * (y + GameConfig.Fx.BarricadeLyingLift - bnd.min.y);
                        }
                    }
                }
                PoseBarricade(i, 0f);
            }
        }

        void PoseBarricade(int i, float f)
        {
            if (barricades == null || restPose == null || i < 0 || i >= barricades.Length || barricades[i] == null || restPose[i] == null) return;
            barricadeFall[i] = f;
            float e = f * f;   // 처음엔 천천히, 넘어질수록 빨리
            var root = barricades[i];
            for (int j = 0; j < root.childCount && j < restPose[i].Length; j++)
                root.GetChild(j).SetPositionAndRotation(Vector3.Lerp(restPose[i][j].position, fallPose[i][j].position, e),
                    Quaternion.Slerp(restPose[i][j].rotation, fallPose[i][j].rotation, e));
        }

        void UpdateBarricades(double v)
        {
            if (restPose == null) return;
            var beats = GameConfig.Fx.LookBackRunBeats;
            for (int i = 0; i < beats.Length; i++)
            {
                float f = v < beats[i] ? 0f : Mathf.Clamp01((float)((v - beats[i]) / GameConfig.Fx.BarricadeFallBeats));   // 되감기로 컷 앞이 되면 다시 선다
                if (!Mathf.Approximately(f, barricadeFall[i])) PoseBarricade(i, f);
            }
        }

        /// <summary>작업 R29: 컷 1박 전 ~ 컷 뒤 1박 동안 플레이어 뒤 경로 위를 달려 따라온다 (조금씩 좁혀 옴). 그 밖에는 숨김.
        /// 작업 R42: 컷이 시작되면 막는 것 쪽으로 달려들다 걸려 넘어진다.</summary>
        void UpdatePursuers(double v, bool fighting)
        {
            if (pursuers == null || pursuers.Length == 0 || player == null) return;
            var beats = GameConfig.Fx.LookBackRunBeats;
            int near = -1;
            if (!fighting)
                for (int i = 0; i < beats.Length; i++)
                    if (v >= beats[i] - GameConfig.Fx.PursuerLeadBeats && v <= beats[i] + LengthOf(i) + 1.0) { near = i; break; }
            // 지붕 위 컷에도 쓴다 (R29 평가 1회차: 지붕 위 컷에 적이 1명뿐이거나 없었음). 발은 보이는 기와 윗면(아래 vis)에, 옆으로는 조금만.
            bool show = near >= 0;
            if (show != PursuersShown)
            {
                PursuersShown = show;
                foreach (var p in pursuers) if (p != null) p.gameObject.SetActive(show);
                if (show) ResetPursuers();
            }
            if (!show) return;
            if (tripped == null || tripped.Length != pursuers.Length) tripped = new bool[pursuers.Length];
            float since = (float)(v - (beats[near] - GameConfig.Fx.PursuerLeadBeats));
            float d0 = player.VisualDistance;
            bool charging = v >= beats[near] && barricades != null && near < barricades.Length && barricades[near] != null;
            if (!charging && chargeCut == near) ResetPursuers();   // 되감기로 컷 앞으로 돌아옴
            if (charging && chargeCut != near)
            {
                chargeCut = near; chargeV0 = v;
                if (chargeFrom == null || chargeFrom.Length != pursuers.Length) chargeFrom = new float[pursuers.Length];
                for (int k = 0; k < pursuers.Length; k++)
                    chargeFrom[k] = d0 - Mathf.Max(3f, GameConfig.Fx.PursuerGaps[k % GameConfig.Fx.PursuerGaps.Length] - GameConfig.Fx.PursuerGainPerBeat * since);
            }
            float stopAt = BarricadeDistance(near) - GameConfig.Fx.PursuerTripGap;
            for (int k = 0; k < pursuers.Length; k++)
            {
                var p = pursuers[k];
                if (p == null) continue;
                float gap = GameConfig.Fx.PursuerGaps[k % GameConfig.Fx.PursuerGaps.Length] - GameConfig.Fx.PursuerGainPerBeat * since;
                float lat = GameConfig.Fx.PursuerLaterals[k % GameConfig.Fx.PursuerLaterals.Length];
                float pd = Mathf.Max(0f, d0 - Mathf.Max(3f, gap));
                if (charging)
                {
                    // 막는 것으로 달려든다 (플레이어보다 빠르게) → 닿으면 걸려 넘어져 그 자리에
                    pd = chargeFrom[k] + (GameConfig.MetersPerBeat + GameConfig.Fx.PursuerChargeGainPerBeat) * (float)(v - chargeV0);
                    if (pd >= stopAt - 0.3f * k)
                    {
                        pd = stopAt - 0.3f * k;
                        // 지붕 위 컷: 넘어지는 자리가 지붕 사이 틈이면(보이는 지붕 칸 없음) 공중에 뜨므로 뒤쪽 가까운 지붕 칸에서 넘어진다
                        if (OnRoof(beats[near]) && roofY != null && roofY.Length > 0 && float.IsNaN(RoofY(near, k, pd)))
                            for (float dd = 0.25f; dd <= 2.5f; dd += 0.25f)
                                if (!float.IsNaN(RoofY(near, k, pd - dd))) { pd -= dd; break; }
                        if (!tripped[k]) { tripped[k] = true; var fr = p.GetComponent<EnemyFigureRun>(); if (fr != null) fr.PlayDefeat(); }
                    }
                }
                var at = MapLayout.PointAt(pd, out var fwd);
                if (at.y > GameConfig.Fx.PursuerRoofHeight) lat *= GameConfig.Fx.PursuerRoofLateralScale;   // 지붕 위: 용마루 가까이 (멀리 비키면 경사 아래 공중), 겹쳐 보이지 않을 만큼만
                at += MapLayout.RightOf(fwd) * lat;
                // 발은 그 자리 아래 가까운 면(지붕·담·땅)에. 1 m 넘게 아래뿐이면 지붕 사이 틈 — 플레이어처럼 길 높이로 뛰어넘는 중(Hopping)
                float best = float.NegativeInfinity;
                foreach (var h in Physics.RaycastAll(at + Vector3.up * 1.5f, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore))
                    if (!h.transform.IsChildOf(p) && h.point.y > best && h.point.y <= at.y + 0.3f) best = h.point.y;   // 길보다 높은 장애물 상자 위로는 올라가지 않음
                // 지붕 구간: 충돌 상자(지붕 길)는 보이는 기와보다 높다 → 보이는 장식 지붕 윗면(용마루)이 더 낮으면 그 높이
                float vis = DressingInstancer.TopBelow(at + Vector3.up * 1.5f, at.y + 0.3f);
                if (!float.IsNegativeInfinity(vis) && vis < best && best - vis < 3f) best = vis;
                bool hop = float.IsNegativeInfinity(best) || best < at.y - 1.0f || InObstacle(pd / GameConfig.MetersPerBeat);   // 장애물(지붕 뛰어넘기·오르기 등)은 플레이어처럼 넘어가는 중
                // 지붕 위 컷: 빌드 때 보이는 메시로 잰 발 높이 (없는 칸 = 지붕 사이 틈을 뛰어넘는 중)
                float roof = RoofY(near, k, pd);
                if (OnRoof(beats[near]) && roofY != null && roofY.Length > 0)
                {
                    if (float.IsNaN(roof)) hop = true;
                    else { best = roof; hop = !tripped[k] && InObstacle(pd / GameConfig.MetersPerBeat); }   // 넘어진 무리는 지붕 면 위에
                }
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
            UpdateBarricades(v);
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
