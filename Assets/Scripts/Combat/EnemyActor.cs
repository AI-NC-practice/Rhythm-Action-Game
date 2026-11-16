using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 적 하나. 공격할 때 무기를 휘두르고 트레일·잔상을 남기며, 쓰러질 때 결과별 동작을 한다.
    /// 상태 이름(State)으로 결과를 구분한다. 모델이 있으면(작업 T14) EnemyAnimator가 같은 신호로 클립을 재생하고,
    /// 보이는 무기(weaponVisual)는 손 뼈에 붙어 있다. weaponPivot은 휘두르기 진행(되감기 포함)을 담는 논리 축이다.
    /// </summary>
    public class EnemyActor : MonoBehaviour, IHitStoppable
    {
        /// <summary>Hit Stop 중 (GDD-beta B5): 무기 자세·쓰러지는 동작·잔상을 멈춘다. 전역 시간은 그대로.</summary>
        public bool Frozen { get; private set; }

        public void SetFrozen(bool frozen)
        {
            Frozen = frozen;
            var anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.speed = frozen ? 0f : 1f;
        }

        public string kind = "sword";
        public bool onRoof;
        /// <summary>작업 R07: 지붕 위 적이 발차기에 떨어질 자리 (빌드 때 보이는 메시로 고른 가장 낮은 면: 골목·마당·낮은 지붕). hasKickLanding이 false면 실행 중에 고른다.</summary>
        public bool hasKickLanding;
        public Vector3 kickLanding;

        /// <summary>
        /// 작업 R07 후속: 지붕 위 전투 자리 둘레의 보이는 면 높이 격자 (빌드 때 잼). arenaOrigin에서 x·z로 arenaStep(m) 간격 arenaN×arenaN,
        /// 면이 없으면 NaN. 전투 중 발을 지붕 면에 붙이고, 옆으로 갈 수 있는 거리를 정한다 (보이지 않는 충돌 상자 위 공중에 서지 않게).
        /// </summary>
        public Vector3 arenaOrigin;
        public float arenaStep = 0.5f;
        public int arenaN;
        public float[] arenaY = new float[0];

        public bool HasArena => arenaN > 0 && arenaY != null && arenaY.Length == arenaN * arenaN;

        /// <summary>(x, z)의 보이는 면 높이. 그 칸에 면이 없으면 가장 가까운(3칸 안) 면, 그것도 없으면 NaN.</summary>
        public float ArenaSurface(Vector3 p, bool exact = false)
        {
            if (!HasArena) return float.NaN;
            int ix = Mathf.RoundToInt((p.x - arenaOrigin.x) / arenaStep), iz = Mathf.RoundToInt((p.z - arenaOrigin.z) / arenaStep);
            for (int r = 0; r <= (exact ? 0 : 3); r++)
                for (int dz = -r; dz <= r; dz++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                        int x = ix + dx, z = iz + dz;
                        if (x < 0 || z < 0 || x >= arenaN || z >= arenaN) continue;
                        float y = arenaY[z * arenaN + x];
                        if (!float.IsNaN(y)) return y;
                    }
            return float.NaN;
        }
        /// <summary>무기 회전 축 (어깨 위치).</summary>
        public Transform weaponPivot;
        /// <summary>무기 끝 (판정 원과 카메라가 따라가는 점).</summary>
        public Transform weaponTip;
        public TrailRenderer trail;
        public Material ghostMaterial;
        /// <summary>모델 Animator (작업 T14). 없으면 회색 상자.</summary>
        public EnemyAnimator anim;
        /// <summary>보이는 무기 (손 뼈에 붙음). 없으면 weaponPivot 아래 막대가 보이는 무기.</summary>
        public Transform weaponVisual;

        public string State { get; private set; } = "Idle";
        public bool Defeated { get; private set; }

        /// <summary>잔상 간격 (초). GDD 6장 "0.1초 간격". 최대 3개.</summary>
        public const float GhostInterval = 0.1f;
        public const float GhostLifetime = 0.3f;
        public const int GhostMax = 3;

        readonly List<GameObject> ghosts = new List<GameObject>();
        float nextGhostAt;
        bool attacking;
        string lastAttack;
        float lastAttackP;
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
            pushRoutine = null; PushOffset = 0;
            MotionPosition = null; FacePoint = null;
            transform.SetPositionAndRotation(homePos, homeRot);
            gameObject.SetActive(true);
            Defeated = false;
            attacking = false;
            State = "Idle";
            if (weaponPivot != null) { weaponPivot.localRotation = Quaternion.identity; weaponPivot.gameObject.SetActive(true); }
            if (weaponVisual != null) weaponVisual.gameObject.SetActive(true);
            if (anim != null) anim.OnReset();
            if (trail != null) { trail.emitting = false; trail.Clear(); }
            foreach (var g in ghosts) if (g != null) Destroy(g);
            ghosts.Clear();
        }

        /// <summary>대치 (GDD-beta B10.2): 진행도 p(0~1)만큼 플레이어 쪽으로 다가가고 칼을 든다. 처음 자리에서 최대 0.5 m.</summary>
        public void Standoff(Vector3 playerPos, float p)
        {
            if (Defeated) return;
            SaveHome();
            var d = playerPos - homePos; d.y = 0;
            if (d.sqrMagnitude < 1e-6f) return;
            if (!MotionPosition.HasValue)   // 움직이는 전투(GDD-beta B13)에서는 자리를 전투 이동이 정한다. 칼만 든다
                transform.position = homePos + d.normalized * (GameConfig.Fx.StandoffApproachMeters * Mathf.Clamp01(p));
            if (attacking) return;                                                          // 휘두르는 중이면 자세는 공격이 정한다
            if (weaponPivot != null) weaponPivot.localRotation = Quaternion.Euler(-30f * Mathf.Clamp01(p), 0, 0);   // 칼을 든다
            State = "Standoff";
        }

        /// <summary>되감기 (GDD-beta B10.3): 마지막 휘두르기 자세를 거꾸로 돌린다. u: 0(그대로) → 1(처음 자세).</summary>
        public void RewindSwing(float u)
        {
            if (Defeated || weaponPivot == null || string.IsNullOrEmpty(lastAttack)) return;
            weaponPivot.localRotation = SwingPose(lastAttack, lastAttackP * (1f - Mathf.Clamp01(u)));
            if (anim != null) anim.OnRewind(u, lastAttack, lastAttackP);
            State = "Rewinding";
        }

        /// <summary>처음 자리에서 다가온 거리 (m).</summary>
        public float StandoffOffset { get { SaveHome(); var d = transform.position - homePos; d.y = 0; return d.magnitude; } }

        /// <summary>공격 진행도 p(0 = 원 등장, 1 = 목표 박자)에 맞춰 무기를 휘두른다.</summary>
        public void Attack(string attack, float p)
        {
            if (Defeated || Frozen) return;
            SaveHome();
            attacking = true;
            lastAttack = attack; lastAttackP = Mathf.Clamp01(p);
            State = "Attack_" + attack;
            if (trail != null) trail.emitting = true;
            if (weaponPivot != null) weaponPivot.localRotation = SwingPose(attack, Mathf.Clamp01(p));
            if (anim != null) anim.OnAttack(attack, p);
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
            if (anim != null) anim.OnEndAttack();
            if (!Defeated) State = playerResult == Judgement.Fail ? "Attack_Landed" : playerResult == Judgement.Perfect ? "Attack_Evaded" : "Attack_Grazed";
        }

        /// <summary>내 공격을 막으려고 칼을 가로로 든다 (무사 결투, GDD-beta B11.2). p: 원 등장 0 → 목표 1.</summary>
        public void Guard(float p)
        {
            if (Defeated || Frozen || attacking) return;
            if (weaponPivot != null) weaponPivot.localRotation = Quaternion.Euler(-60f * Mathf.Clamp01(p / 0.7f), 0, 70f * Mathf.Clamp01(p / 0.7f));
            if (pushRoutine == null) { State = "Guard"; if (anim != null) anim.OnGuard(); }      // 밀려나는 중이면 "Pushed" 유지
        }

        /// <summary>옆차기 (GDD-beta B13.4): 칼을 내리고 몸을 기울인 자세 (회색 상자). 다음 동작이 자세를 덮어쓴다.</summary>
        public void Kick()
        {
            if (Defeated) return;
            attacking = false;
            if (weaponPivot != null) weaponPivot.localRotation = Quaternion.Euler(20f, 0, -60f);
            State = "Kick";
        }

        /// <summary>막기 자세를 푼다.</summary>
        public void EndGuard()
        {
            if (Defeated || attacking) return;
            if (weaponPivot != null) weaponPivot.localRotation = Quaternion.identity;
            if (anim != null) anim.OnEndGuard();
        }

        Coroutine pushRoutine;
        Vector3 pushBase;
        Vector3 pushDir;

        /// <summary>
        /// 움직이는 전투(GDD-beta B13.1)에서 전투 이동이 정한 자리. 설정되면 매 프레임 이 자리(+ 밀림)에 서고 FacePoint를 바라본다.
        /// </summary>
        public Vector3? MotionPosition { get; set; }
        public Vector3? FacePoint { get; set; }

        void LateUpdate()
        {
            if (Defeated || !MotionPosition.HasValue) return;
            var pos = MotionPosition.Value + pushDir * PushOffset;
            transform.position = pos;
            if (FacePoint.HasValue)
            {
                var d = FacePoint.Value - pos; d.y = 0;
                if (d.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            }
        }
        /// <summary>밀려난 정도 (m, 테스트용).</summary>
        public float PushOffset { get; private set; }

        /// <summary>내 공격 PERFECT에 뒤로 밀린다 (B11.2): 빠르게 meters만큼 밀렸다가 seconds 동안 돌아온다.</summary>
        public void Pushback(Vector3 away, float meters, float seconds)
        {
            if (Defeated) return;
            if (pushRoutine != null) { StopCoroutine(pushRoutine); if (!MotionPosition.HasValue) transform.position = pushBase; }
            pushBase = transform.position;
            away.y = 0;
            pushDir = away.sqrMagnitude > 1e-6f ? away.normalized : -transform.forward;
            State = "Pushed";
            if (anim != null) anim.OnPushed();
            pushRoutine = StartCoroutine(PushRoutine(away.sqrMagnitude > 1e-6f ? away.normalized : -transform.forward, meters, seconds));
        }

        IEnumerator PushRoutine(Vector3 dir, float meters, float seconds)
        {
            for (float t = 0; t < seconds; t += Frozen ? 0f : Time.deltaTime)
            {
                float k = t / seconds;
                PushOffset = meters * (k < 0.25f ? k / 0.25f : 1f - Mathf.SmoothStep(0, 1, (k - 0.25f) / 0.75f));
                if (!MotionPosition.HasValue) transform.position = pushBase + dir * PushOffset;   // 움직이는 전투에서는 LateUpdate가 놓는다
                yield return null;
            }
            PushOffset = 0;
            if (!MotionPosition.HasValue) transform.position = pushBase;
            pushRoutine = null;
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
            if (MotionPosition.HasValue) { transform.position = MotionPosition.Value + pushDir * PushOffset; MotionPosition = null; FacePoint = null; }
            else if (pushRoutine != null) transform.position = pushBase;
            pushRoutine = null; PushOffset = 0;
            AlignForExecution(how, awayFromPlayer);
            if (anim != null) anim.OnDefeat(how);
            StartCoroutine(DefeatRoutine(how, awayFromPlayer.normalized));
        }

        /// <summary>처형이 시작되는 박에 적이 플레이어를 보게 하고, 일대일 처형(발차기·칼 뺏기·찌르기)은 거리를 맞춘다 (작업 T14).</summary>
        void AlignForExecution(ExecutionKind how, Vector3 awayFromPlayer)
        {
            var playerFeet = transform.position - awayFromPlayer;
            var flat = awayFromPlayer; flat.y = 0;
            if (flat.sqrMagnitude < 1e-6f) return;
            flat.Normalize();
            transform.rotation = Quaternion.LookRotation(-flat, Vector3.up);
            if (how == ExecutionKind.KickOffRoof || how == ExecutionKind.DisarmSlash || how == ExecutionKind.StabWithOwnSword)
            {
                var p = playerFeet + flat * GameConfig.EnemyBody.ExecutionDistance;
                p.y = transform.position.y;
                transform.position = p;
            }
        }

        /// <summary>지붕 발차기로 날아가는 수평 방향 (작업 R07, 카메라가 따라봄). 처형 전이면 zero.</summary>
        public Vector3 KickDirection { get; private set; }
        /// <summary>지붕 발차기로 떨어져 땅(아래 충돌면)에 닿았는지 (테스트용).</summary>
        public bool KickLanded { get; private set; }

        /// <summary>
        /// 작업 R07: 지붕 위 PERFECT 발차기 — 플레이어 반대쪽에서 지붕 가장자리 쪽으로 돌린 방향으로 뒤로 날아가(Mixamo Flying Back Death)
        /// 위로 조금 튄 뒤 중력으로 아래 면(땅·낮은 지붕)까지 떨어진다. 클립이 몸을 뒤로 넘기므로 코드 회전은 하지 않는다.
        /// </summary>
        IEnumerator KickOffRoofRoutine(Vector3 away)
        {
            var flat = away; flat.y = 0f;
            if (flat.sqrMagnitude < 1e-6f) flat = transform.forward * -1f;
            flat.Normalize();
            float side = KickSide();
            var start = transform.position;
            float landY = float.NaN;
            if (hasKickLanding)
            {
                var to = kickLanding - start; to.y = 0f;
                if (to.sqrMagnitude > 1f) { flat = to.normalized; landY = kickLanding.y; }
            }
            // 떨어질 자리 고르기 (빌드 때 고른 자리가 없을 때만): 지붕 가장자리 쪽 여러 방향·거리 중 가장 낮은 착지면(골목·마당). 같으면 기본 방향.
            var dir = Quaternion.AngleAxis(side * GameConfig.EnemyBody.KickOffRoofSideAngle, Vector3.up) * flat;
            float dist = GameConfig.EnemyBody.KickOffRoofDistance;
            float bestFloor = FloorBelow(start + dir * dist + Vector3.up * 0.5f, start.y) - 0.3f;   // 기본 방향을 조금 우대
            if (!float.IsNaN(landY)) { dir = flat; dist = new Vector2(kickLanding.x - start.x, kickLanding.z - start.z).magnitude; }
            else foreach (float sd in new[] { side, -side })
                foreach (float ang in new[] { 80f, 65f, 95f, 50f, 110f })
                    foreach (float dd in new[] { 7.5f, 9.5f, 6f })
                    {
                        var cand = Quaternion.AngleAxis(sd * ang, Vector3.up) * flat;
                        float f = FloorBelow(start + cand * dd + Vector3.up * 0.5f, start.y);
                        if (f < bestFloor - 0.5f) { bestFloor = f; dir = cand; dist = dd; }
                    }
            KickDirection = dir;
            KickLanded = false;
            transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);   // 등 쪽으로 날아감
            float T = GameConfig.EnemyBody.KickOffRoofFlightSeconds;
            float vy = float.IsNaN(landY) ? GameConfig.EnemyBody.KickOffRoofLift : LiftToClear(start, dir, dist, T);
            float y = start.y, t = 0f;
            while (t < 4f)
            {
                float dt = Frozen ? 0f : Time.deltaTime;
                t += dt;
                float k = Mathf.Clamp01(t / T);
                // 빌드 때 고른 자리로는 곧게(지붕들을 넘는 포물선), 아니면 처음에 빠르게
                var p = start + dir * (dist * (float.IsNaN(landY) ? 1f - (1f - k) * (1f - k) : k));
                vy -= GameConfig.EnemyBody.Gravity * dt;
                y += vy * dt;
                float floor = !float.IsNaN(landY) && k >= 1f ? landY : FloorBelow(new Vector3(p.x, Mathf.Max(y, start.y) + 0.5f, p.z), y, float.IsNaN(landY));
                if (vy < 0f && y <= floor) { y = floor; KickLanded = true; }
                transform.position = new Vector3(p.x, y, p.z);
                if (KickLanded && k >= 1f) yield break;
                yield return null;
            }
        }

        /// <summary>
        /// 떨어질 자리까지 가는 길에 있는 지붕(충돌면·길 옆 장식 윗면)을 모두 0.3 m 넘게 넘는 처음 위 속도 (m/s).
        /// 수평은 T초 동안 고르게 간다. 막 떠난 지붕(1.5 m 안)과 떨어질 자리 앞 1 m는 빼고 잰다.
        /// </summary>
        float LiftToClear(Vector3 start, Vector3 dir, float dist, float T)
        {
            float g = GameConfig.EnemyBody.Gravity, vy = GameConfig.EnemyBody.KickOffRoofLift;
            for (int i = 1; i < 24; i++)
            {
                float f = i / 24f, along = dist * f;
                if (along < 1.5f || dist - along < 1f) continue;
                var q = start + dir * along;
                float top = DressingInstancer.TopBelow(q, 999f);
                foreach (var h in Physics.RaycastAll(q + Vector3.up * 40f, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore))
                    if (!h.transform.IsChildOf(transform)) top = Mathf.Max(top, h.point.y);
                if (float.IsNegativeInfinity(top)) continue;
                float ti = f * T;
                vy = Mathf.Max(vy, (top + 0.3f - start.y + 0.5f * g * ti * ti) / ti);
            }
            return vy;
        }

        /// <summary>지붕 가장자리 쪽: 길 가운데에서 이 적이 벗어난 쪽 (가운데면 오른쪽).</summary>
        float KickSide()
        {
            var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.player : null;
            if (player == null) return 1f;
            float lat = Vector3.Dot(transform.position - player.transform.position, player.transform.right);
            return lat < -0.2f ? -1f : 1f;
        }

        /// <summary>발 아래 면 높이: 충돌면(자기 자신 제외)과 길 옆 장식의 윗면 중 높은 것. 없으면 맵 땅 높이 0.</summary>
        float FloorBelow(Vector3 from, float currentY, bool dressing = true)
        {
            float best = 0f;
            foreach (var h in Physics.RaycastAll(from, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.transform.IsChildOf(transform)) continue;
                if (h.point.y > currentY + 0.6f) continue;   // 위에 있는 면(막 떠난 지붕)은 무시
                best = Mathf.Max(best, h.point.y);
            }
            return dressing ? Mathf.Max(best, DressingInstancer.TopBelow(from, currentY + 0.6f)) : best;   // 충돌체 없는 길 옆 집·담 지붕 (경계 상자 윗면, 대략)
        }

        IEnumerator DefeatRoutine(ExecutionKind how, Vector3 away)
        {
            if (how == ExecutionKind.KickOffRoof) { yield return KickOffRoofRoutine(away); yield break; }
            var start = transform.position;
            var startRot = transform.rotation;
            if (how == ExecutionKind.DisarmSlash && weaponPivot != null) weaponPivot.gameObject.SetActive(false); // 칼을 빼앗김
            if (how == ExecutionKind.DisarmSlash && weaponVisual != null) weaponVisual.gameObject.SetActive(false);
            float dur = how == ExecutionKind.KickOffRoof || how == ExecutionKind.Windmill ? 1.0f : 0.6f;
            for (float t = 0; t < dur; t += Frozen ? 0f : Time.deltaTime)
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
            var holder = weaponVisual != null ? weaponVisual : weaponPivot;
            if (holder == null) return;
            ghosts.RemoveAll(g => g == null);
            while (ghosts.Count >= GhostMax) { Destroy(ghosts[0]); ghosts.RemoveAt(0); }
            var src = holder.GetComponentInChildren<MeshFilter>();
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
