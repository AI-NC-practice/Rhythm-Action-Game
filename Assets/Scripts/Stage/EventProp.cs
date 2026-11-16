using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 새 사건·안 멈추는 적·복선의 회색 상자 소품 하나 (GDD-beta B2.2, B2.3). 회색 상자 단계의 움직임만 한다.
    /// 루트는 사건 가운데(시작 + 길이/2) 경로 위에 놓이고 진행 방향을 본다. 로컬 z = 진행 방향(1박 = 2.5 m).
    /// EventPropDirector가 매 프레임 Animate를 부른다.
    /// </summary>
    public class EventProp : MonoBehaviour
    {
        public int runBeat;
        public string kind;
        public int beats;
        /// <summary>함께 나오는 적의 등장 방식 (예: rope_swing + charge). 없으면 빈 문자열.</summary>
        public string companion = "";
        /// <summary>판정별 키 방향 (← −1, → +1, 그 밖 0).</summary>
        public float[] slotSides = new float[0];
        /// <summary>추격 적이 나란히 달리는 옆 거리 (m). 작업 R03: 길 위 지붕에서는 지붕 가장자리보다 안쪽.</summary>
        public float chaseLateral = GameConfig.Map.ChaseLateral;
        /// <summary>판정별 박자 (사건 시작 기준).</summary>
        public float[] slotOffsets = new float[0];
        /// <summary>복선이 보이는 길이 (박자).</summary>
        public int span;
        /// <summary>움직이는 부품 (이름 순서는 EventProps가 정한다).</summary>
        public Transform[] parts = new Transform[0];
        /// <summary>함께 나오는 적 (없으면 null).</summary>
        public Transform enemy;

        /// <summary>
        /// 작업 R07: 추격 적이 달리는 자리의 발밑 높이·옆 거리 (빌드 때 보이는 메시로 잼). 경로 거리 chaseD0부터 chaseStep(m) 간격.
        /// 실제 경로(모퉁이 곡선·높이)를 따라 달리고 발을 지붕 비탈·땅에 붙인다. 비어 있으면 예전처럼 로컬 직선.
        /// </summary>
        public float chaseD0;
        public float chaseStep = 0.5f;
        public float[] chaseY = new float[0];
        public float[] chaseX = new float[0];
        /// <summary>그 자리에 면이 없어 뛰어넘거나(집과 집 사이 틈) 담·지붕을 오르내리는 중인지 (테스트용).</summary>
        public bool[] chaseGap = new bool[0];

        /// <summary>작업 R07: 매복 적이 튀어나온 끝자리(길 쪽 1 m)의 발 높이 (로컬). 지붕 비탈에서 발이 지붕 속으로 묻히지 않게. NaN이면 집 높이.</summary>
        public float ambushEndY = float.NaN;

        /// <summary>추격 적 프로필 범위 (사건 시작 기준 박자): 소품이 움직이는 범위 전체.</summary>
        public const float ChaseBeatsBefore = 31f, ChaseBeatsAfter = 10f;   // EventPropDirector가 앞 30박·뒤 8박 동안 움직인다

        /// <summary>회색 상자 상태 이름: Idle, Active, Done, Enemy_Down 등 (테스트용).</summary>
        public string State { get; private set; } = "Idle";
        public bool EnemyDown { get; private set; }
        /// <summary>추격 적이 집 사이 틈을 뛰어넘는 중 (작업 R07, 테스트용).</summary>
        public bool ChaseHopping { get; private set; }

        Vector3[] homePos;
        Quaternion[] homeRot;
        Vector3 enemyHome;
        Quaternion enemyHomeRot;

        static float M(double beats) => (float)(beats * GameConfig.MetersPerBeat);

        void SaveHome()
        {
            if (homePos != null) return;
            homePos = new Vector3[parts.Length];
            homeRot = new Quaternion[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null) continue;
                homePos[i] = parts[i].localPosition;
                homeRot[i] = parts[i].localRotation;
            }
            if (enemy != null) { enemyHome = enemy.localPosition; enemyHomeRot = enemy.localRotation; }
        }

        public void ResetProp()
        {
            SaveHome();
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null) continue;
                parts[i].localPosition = homePos[i];
                parts[i].localRotation = homeRot[i];
                parts[i].gameObject.SetActive(true);
            }
            if (enemy != null) { enemy.localPosition = enemyHome; enemy.localRotation = enemyHomeRot; enemy.gameObject.SetActive(true); }
            EnemyDown = false;
            State = "Idle";
            if (kind == StageChart.TypeForeshadow) gameObject.SetActive(false);
        }

        Judgement? Result(StageDirector.ObstacleRun run, int slot) =>
            run != null && run.Results != null && slot < run.Results.Length ? run.Results[slot] : null;

        /// <summary>보이는 달리기 박자 v에서의 소품 모양. run: 이 사건의 진행 정보(없으면 null).</summary>
        public void Animate(double v, StageDirector.ObstacleRun run)
        {
            SaveHome();
            double t = v - runBeat;                   // 사건 시작부터 지난 박자
            if (kind == StageChart.TypeForeshadow)
            {
                bool show = v >= runBeat && v < runBeat + span;
                if (gameObject.activeSelf != show) gameObject.SetActive(show);
                State = show ? "Active" : "Idle";
                return;
            }
            State = t < 0 ? "Idle" : t >= beats ? "Done" : "Active";
            float half = beats * 0.5f;
            // 판정 i 시점의 플레이어 위치 (로컬 z)
            float SlotZ(int i) => M(slotOffsets[i] - half);
            float playerZ = M(t - half);

            switch (kind)
            {
                case "tile_slip":
                    // 판정 ↓ 시점(@2)부터 기와가 빠져 처마 아래로 떨어진다
                    for (int i = 0; i < parts.Length; i++)
                    {
                        float fall = Mathf.Max(0f, (float)(t - slotOffsets[0]));
                        parts[i].localPosition = homePos[i] + new Vector3(0, -fall * fall * 1.5f, -fall * 0.8f);
                        parts[i].localRotation = homeRot[i] * Quaternion.Euler(fall * 60f * (i + 1), 0, fall * 25f);
                    }
                    break;
                case "rolling_jars":
                    // 독 i는 판정 i 시점에 플레이어 자리(키 반대쪽 차선)를 지나간다
                    for (int i = 0; i < parts.Length && i < slotOffsets.Length; i++)
                    {
                        float dz = (float)((slotOffsets[i] - t) * 4.0);       // 1박에 4 m씩 다가옴
                        parts[i].localPosition = new Vector3(homePos[i].x, homePos[i].y, SlotZ(i) + dz);
                        parts[i].localRotation = Quaternion.Euler(-(float)t * 200f, 0, 0);
                    }
                    break;
                case "closing_gate":
                    // 문짝 두 개가 판정(@2) + 0.5박에 거의 닫힌다
                    if (parts.Length >= 2)
                    {
                        float c = Mathf.Clamp01((float)(t / (slotOffsets[0] + 0.5)));
                        parts[0].localPosition = homePos[0] + new Vector3(c * 1.3f, 0, 0);
                        parts[1].localPosition = homePos[1] - new Vector3(c * 1.3f, 0, 0);
                    }
                    break;
                case "arrow_volley":
                    // 화살 i는 판정 i 시점에 플레이어 머리 옆을 지나간다 (키 반대쪽 담에서 날아옴)
                    for (int i = 0; i < parts.Length && i < slotOffsets.Length; i++)
                    {
                        float from = -slotSides[i] * 6f;
                        float k = (float)((t - (slotOffsets[i] - 0.5)) / 0.5);    // 판정 반 박 전에 출발
                        bool flying = k >= 0f && k <= 2f;
                        parts[i].gameObject.SetActive(flying);
                        if (flying) parts[i].localPosition = new Vector3(Mathf.LerpUnclamped(from, 0f, k), 1.6f, SlotZ(i));
                    }
                    break;
                case "awning_bounce":
                    if (parts.Length >= 1)
                    {
                        var r = Result(run, 0);
                        float u = (float)(t - slotOffsets[0]);
                        float sag = r.HasValue && u >= 0 && u < 0.5f ? Mathf.Sin(u / 0.5f * Mathf.PI) * (r == Judgement.Fail ? 0.6f : 0.3f) : 0f;
                        parts[0].localPosition = homePos[0] + new Vector3(0, -sag, 0);
                    }
                    break;
            }

            if (enemy != null) AnimateEnemy(t, run, SlotZ, playerZ);
        }

        /// <summary>
        /// 작업 R07: 추격 적을 실제 경로를 따라 플레이어 2 m 앞, 빌드 때 잰 옆 거리·발밑 높이에 놓는다 (공중에 뜨지 않게).
        /// 프로필이 없거나 범위 밖이면 false (예전 방식).
        /// </summary>
        bool ChaseOnPath(double t, float cs, float u, Judgement? r)
        {
            if (chaseY == null || chaseY.Length < 2 || chaseX == null || chaseX.Length != chaseY.Length) return false;
            float d = M(runBeat + t) + 2f;
            float f = (d - chaseD0) / chaseStep;
            if (f < 0f || f > chaseY.Length - 1) return false;
            int i = Mathf.Min((int)f, chaseY.Length - 2);
            float k = f - i;
            float y = Mathf.Lerp(chaseY[i], chaseY[i + 1], k);
            float lat = Mathf.Lerp(chaseX[i], chaseX[i + 1], k);
            ChaseHopping = chaseGap != null && chaseGap.Length == chaseY.Length && (chaseGap[i] || chaseGap[i + 1]);
            var pt = MapLayout.PointAt(d, out var fwd);
            var w = pt + MapLayout.RightOf(fwd) * (cs * lat);
            w.y = y;
            enemy.position = w;
            var rot = Quaternion.LookRotation(fwd, Vector3.up);   // 달리는 방향을 봄
            if (u > 0 && r == Judgement.Perfect) { rot = rot * Quaternion.Euler(0, 0, -cs * Mathf.Min(90f, u * 180f)); EnemyDown = true; }
            enemy.rotation = rot;
            return true;
        }

        void AnimateEnemy(double t, StageDirector.ObstacleRun run, System.Func<int, float> slotZ, float playerZ)
        {
            string how = companion != "" ? companion : kind;
            var r = Result(run, 0);
            float hitT = slotOffsets.Length > 0 ? slotOffsets[0] : 2f;
            float u = (float)(t - hitT);            // 판정 후 지난 박자
            Vector3 p = enemyHome;
            Quaternion q = enemyHomeRot;
            switch (how)
            {
                case EventCatalog.Charge:
                    // 앞에서 달려와 판정 시점에 플레이어와 만남. PERFECT 쓰러짐, NORMAL 스쳐 지나감, FAIL 그 자리
                    p.z = slotZ(0) + Mathf.Max(0f, (float)(hitT - t)) * 4f;
                    if (u > 0 && r.HasValue)
                    {
                        if (r == Judgement.Perfect) { q = enemyHomeRot * Quaternion.Euler(-Mathf.Min(90f, u * 180f), 0, 0); EnemyDown = true; }
                        else if (r == Judgement.Normal) p.z = slotZ(0) - u * 4f;
                    }
                    break;
                case EventCatalog.Ambush:
                    // 키 반대쪽 담 뒤에서 판정 1박 전에 튀어나옴
                    float side = slotSides.Length > 0 ? slotSides[0] : 1f;
                    float pop = Mathf.Clamp01((float)(t - (hitT - 1.0)));
                    p = new Vector3(-side * Mathf.Lerp(3.2f, 1.0f, pop), float.IsNaN(ambushEndY) ? enemyHome.y : Mathf.Lerp(enemyHome.y, ambushEndY, pop), slotZ(0));
                    if (u > 0 && r == Judgement.Perfect) { q = enemyHomeRot * Quaternion.Euler(0, 0, side * Mathf.Min(90f, u * 180f)); EnemyDown = true; }
                    break;
                case EventCatalog.Chase:
                    // 옆 지붕에서 나란히 달림 (플레이어보다 2 m 앞)
                    float cs = slotSides.Length > 0 ? -slotSides[0] : 1f;
                    if (ChaseOnPath(t, cs, u, r)) { if (EnemyDown) State = "Enemy_Down"; return; }
                    p = new Vector3(cs * chaseLateral, enemyHome.y, playerZ + 2f);
                    q = enemyHomeRot * Quaternion.Euler(0, 180f, 0);   // 작업 R07: 플레이어 쪽이 아니라 달리는 방향을 봄 (뒷걸음질로 달리지 않게)
                    if (u > 0 && r == Judgement.Perfect) { q = q * Quaternion.Euler(0, 0, -cs * Mathf.Min(90f, u * 180f)); EnemyDown = true; }
                    break;
            }
            enemy.localPosition = p;
            enemy.localRotation = q;
            if (EnemyDown) State = "Enemy_Down";
        }
    }
}
