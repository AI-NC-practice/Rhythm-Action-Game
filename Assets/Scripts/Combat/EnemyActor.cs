using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 적 하나 (회색 상자 단계). 공격할 때 무기를 휘두르고 트레일·잔상을 남기며, 쓰러질 때 결과별 동작을 한다.
    /// 상태 이름(State)으로 결과를 구분한다. 모델과 Animator 연결은 T14.
    /// </summary>
    public class EnemyActor : MonoBehaviour
    {
        public string kind = "sword";
        public bool onRoof;
        /// <summary>무기 회전 축 (어깨 위치).</summary>
        public Transform weaponPivot;
        /// <summary>무기 끝 (판정 원과 카메라가 따라가는 점).</summary>
        public Transform weaponTip;
        public TrailRenderer trail;
        public Material ghostMaterial;

        public string State { get; private set; } = "Idle";
        public bool Defeated { get; private set; }

        /// <summary>잔상 간격 (초). GDD 6장 "0.1초 간격". 최대 3개.</summary>
        public const float GhostInterval = 0.1f;
        public const float GhostLifetime = 0.3f;
        public const int GhostMax = 3;

        readonly List<GameObject> ghosts = new List<GameObject>();
        float nextGhostAt;
        bool attacking;
        Vector3 homePos;
        Quaternion homeRot;
        bool homeSaved;

        public int LiveGhosts { get { ghosts.RemoveAll(g => g == null); return ghosts.Count; } }
        public bool TrailEmitting => trail != null && trail.emitting;

        void Awake() => SaveHome();

        void SaveHome()
        {
            if (homeSaved) return;
            homePos = transform.position;
            homeRot = transform.rotation;
            homeSaved = true;
            if (trail != null) trail.emitting = false;
        }

        /// <summary>처음 자리로 돌아간다 (다시 하기).</summary>
        public void ResetActor()
        {
            SaveHome();
            StopAllCoroutines();
            transform.SetPositionAndRotation(homePos, homeRot);
            gameObject.SetActive(true);
            Defeated = false;
            attacking = false;
            State = "Idle";
            if (weaponPivot != null) { weaponPivot.localRotation = Quaternion.identity; weaponPivot.gameObject.SetActive(true); }
            if (trail != null) { trail.emitting = false; trail.Clear(); }
            foreach (var g in ghosts) if (g != null) Destroy(g);
            ghosts.Clear();
        }

        /// <summary>공격 진행도 p(0 = 원 등장, 1 = 목표 박자)에 맞춰 무기를 휘두른다.</summary>
        public void Attack(string attack, float p)
        {
            if (Defeated) return;
            SaveHome();
            attacking = true;
            State = "Attack_" + attack;
            if (trail != null) trail.emitting = true;
            if (weaponPivot != null) weaponPivot.localRotation = SwingPose(attack, Mathf.Clamp01(p));
        }

        /// <summary>공격 종류별 무기 궤적 (어깨 기준 회전).</summary>
        public static Quaternion SwingPose(string attack, float p)
        {
            // 앞 70%는 천천히 들어 올리고, 마지막 30%에 빠르게 휘두른다
            float wind = Mathf.Clamp01(p / 0.7f);
            float strike = Mathf.Clamp01((p - 0.7f) / 0.3f);
            switch (attack)
            {
                case "horizontal": return Quaternion.Euler(0, Mathf.Lerp(0, -100, wind) + 200 * strike, 0);
                case "overhead": return Quaternion.Euler(Mathf.Lerp(0, -140, wind) + 200 * strike, 0, 0);
                case "upward": return Quaternion.Euler(Mathf.Lerp(0, 60, wind) - 160 * strike, 0, 0);
                case "thrust": return Quaternion.Euler(Mathf.Lerp(0, 10, wind) - 10 * strike, Mathf.Lerp(0, 25, wind) - 25 * strike, 0);
                default: return Quaternion.identity;
            }
        }

        /// <summary>공격 판정이 끝남. FAIL이면 플레이어가 맞았다.</summary>
        public void EndAttack(Judgement playerResult)
        {
            attacking = false;
            if (trail != null) trail.emitting = false;
            if (weaponPivot != null) weaponPivot.localRotation = Quaternion.identity;
            if (!Defeated) State = playerResult == Judgement.Fail ? "Attack_Landed" : playerResult == Judgement.Perfect ? "Attack_Evaded" : "Attack_Grazed";
        }

        /// <summary>쓰러진다. 결과별 동작 (회색 상자: 이동·회전).</summary>
        public void Defeat(ExecutionKind how, Vector3 awayFromPlayer)
        {
            if (Defeated) return;
            Defeated = true;
            attacking = false;
            if (trail != null) trail.emitting = false;
            State = "Defeated_" + how;
            StopAllCoroutines();
            StartCoroutine(DefeatRoutine(how, awayFromPlayer.normalized));
        }

        IEnumerator DefeatRoutine(ExecutionKind how, Vector3 away)
        {
            var start = transform.position;
            var startRot = transform.rotation;
            if (how == ExecutionKind.DisarmSlash && weaponPivot != null) weaponPivot.gameObject.SetActive(false); // 칼을 빼앗김
            float dur = how == ExecutionKind.KickOffRoof || how == ExecutionKind.Windmill ? 1.0f : 0.6f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                switch (how)
                {
                    case ExecutionKind.KickOffRoof:   // 뒤로 날아가 지붕 아래로 떨어짐
                        transform.position = start + away * (4f * k) + Vector3.up * (1.5f * Mathf.Sin(k * Mathf.PI) - 6f * k * k);
                        transform.rotation = startRot * Quaternion.Euler(-120f * k, 0, 0);
                        break;
                    case ExecutionKind.Windmill:      // 바깥으로 날아감
                        transform.position = start + away * (6f * k) + Vector3.up * (2f * Mathf.Sin(k * Mathf.PI));
                        transform.rotation = startRot * Quaternion.Euler(-90f * k, 360f * k, 0);
                        break;
                    default:                           // 그 자리에서 쓰러짐
                        transform.position = start + away * (0.6f * k) - Vector3.up * (0.6f * k);
                        transform.rotation = startRot * Quaternion.Euler(-85f * Mathf.SmoothStep(0, 1, k), 0, 0);
                        break;
                }
                yield return null;
            }
        }

        void Update()
        {
            if (!attacking || weaponTip == null) return;
            if (Time.unscaledTime >= nextGhostAt)
            {
                SpawnGhost();
                nextGhostAt = Time.unscaledTime + GhostInterval;
            }
        }

        /// <summary>무기 모양을 반투명하게 복제해 남긴다 (잔상).</summary>
        public void SpawnGhost()
        {
            if (weaponPivot == null) return;
            ghosts.RemoveAll(g => g == null);
            while (ghosts.Count >= GhostMax) { Destroy(ghosts[0]); ghosts.RemoveAt(0); }
            var src = weaponPivot.GetComponentInChildren<MeshFilter>();
            if (src == null) return;
            var g = new GameObject("Afterimage", typeof(MeshFilter), typeof(MeshRenderer));
            g.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
            g.transform.localScale = src.transform.lossyScale;
            g.GetComponent<MeshFilter>().sharedMesh = src.sharedMesh;
            var mr = g.GetComponent<MeshRenderer>();
            mr.sharedMaterial = ghostMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ghosts.Add(g);
            Destroy(g, GhostLifetime);
        }
    }
}
