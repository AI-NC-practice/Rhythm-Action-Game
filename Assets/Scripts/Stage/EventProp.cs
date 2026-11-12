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
        /// <summary>판정별 박자 (사건 시작 기준).</summary>
        public float[] slotOffsets = new float[0];
        /// <summary>복선이 보이는 길이 (박자).</summary>
        public int span;
        /// <summary>움직이는 부품 (이름 순서는 EventProps가 정한다).</summary>
        public Transform[] parts = new Transform[0];
        /// <summary>함께 나오는 적 (없으면 null).</summary>
        public Transform enemy;

        /// <summary>회색 상자 상태 이름: Idle, Active, Done, Enemy_Down 등 (테스트용).</summary>
        public string State { get; private set; } = "Idle";
        public bool EnemyDown { get; private set; }

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
                    p = new Vector3(-side * Mathf.Lerp(3.2f, 1.0f, pop), enemyHome.y, slotZ(0));
                    if (u > 0 && r == Judgement.Perfect) { q = enemyHomeRot * Quaternion.Euler(0, 0, side * Mathf.Min(90f, u * 180f)); EnemyDown = true; }
                    break;
                case EventCatalog.Chase:
                    // 옆 지붕에서 나란히 달림 (플레이어보다 2 m 앞)
                    float cs = slotSides.Length > 0 ? -slotSides[0] : 1f;
                    p = new Vector3(cs * 3.5f, enemyHome.y, playerZ + 2f);
                    if (u > 0 && r == Judgement.Perfect) { q = enemyHomeRot * Quaternion.Euler(0, 0, cs * Mathf.Min(90f, u * 180f)); EnemyDown = true; }
                    break;
            }
            enemy.localPosition = p;
            enemy.localRotation = q;
            if (EnemyDown) State = "Enemy_Down";
        }
    }
}
