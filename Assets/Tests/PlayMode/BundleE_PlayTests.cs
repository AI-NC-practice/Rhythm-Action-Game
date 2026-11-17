using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 묶음 E (사용자 빌드 플레이 피드백 2026-11-17): R27 칼 쥐기·같이 싸우기, R28 칼 든 대기, R29 뒤돌아보기, R30 매트릭스 회피,
    /// R31 윈드밀 컷·쓰러진 적·칼 날아감, R32 돌진 적 엇갈림, R33 벽타기 막는 짐, R34 처형 팔 가림, R26 콤보 단계. 미리보기 → harness/reports/BundleE/.
    /// </summary>
    public class BundleE_PlayTests
    {
        /// <summary>모든 LateUpdate 뒤(최종 자세)에 한 번 실행 (WaitForEndOfFrame은 배치 모드에서 안 됨).</summary>
        [DefaultExecutionOrder(32000)]
        class EndProbe : MonoBehaviour { public System.Action act; void LateUpdate() { var a = act; act = null; a?.Invoke(); } }

        const string Out = "harness/reports/BundleE";
        const int BodyLayer = 31;
        double now;
        GameBootstrap boot;
        StageDirector dir;
        PlayerBody body;
        Camera view;

        [TearDown]
        public void TearDown()
        {
            if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel();
            Time.timeScale = 1f;
        }

        IEnumerator Load(double startBeat, bool bullet = false)
        {
            Directory.CreateDirectory(Out);
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            body = Object.FindAnyObjectByType<PlayerBody>();
            now = 0;
            boot.clock.TimeSource = () => now;
            float realTime = Time.realtimeSinceStartup;
            dir.slowMotion.RealTime = () => realTime + (float)now;
            dir.RewindRealTime = () => realTime + (float)now;
            dir.MotionDeltaTime = () => 0.01f;
            dir.SpinDeltaTime = () => 0.01f;
            foreach (var fx in Object.FindObjectsByType<TimeFx>(FindObjectsSortMode.None)) fx.RealTime = () => realTime + (float)now;
            foreach (var cf in Object.FindObjectsByType<CameraFx>(FindObjectsSortMode.None)) cf.DeltaTime = () => 0.01f;
            if (boot.bulletTime != null) { boot.bulletTime.DeltaTime = () => 0.01f; boot.bulletTime.enabled = bullet; }
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.AllowEventSlowMotion = false;
            boot.StartRun(startBeat);
            yield return null;
            view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
        }

        /// <summary>자동 연주 한 프레임 (모두 PERFECT). 체력이 줄면 채운다.</summary>
        IEnumerator Step(System.Func<JudgeCue, bool> skip = null)
        {
            now += 0.01;
            foreach (var c in boot.judge.Cues.ToArray())
                if (!c.Judged && c.Appeared && (skip == null || !skip(c)) && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
            if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
            yield return null;
        }

        static void Shot(Camera cam, string path)
        {
            var big = new RenderTexture(640, 360, 24);
            var old = cam.targetTexture;
            cam.targetTexture = big;
            cam.Render();
            cam.targetTexture = old;
            var prev = RenderTexture.active;
            RenderTexture.active = big;
            var t = new Texture2D(640, 360, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
            RenderTexture.active = prev;
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.Destroy(t);
            big.Release();
        }

        Camera ThirdPerson(Vector3 target, Vector3 from)
        {
            var go = new GameObject("BundleE_TP");
            var c = go.AddComponent<Camera>();
            c.enabled = false;
            c.fieldOfView = 50f;
            go.transform.position = from;
            go.transform.LookAt(target);
            return c;
        }

        static float HandGap(Animator a)
        {
            var l = a.GetBoneTransform(HumanBodyBones.LeftHand); var r = a.GetBoneTransform(HumanBodyBones.RightHand);
            return l != null && r != null ? Vector3.Distance(l.position, r.position) : -1f;
        }

        /// <summary>적을 플레이어 쪽에서 40° 비스듬히 앞모습으로 찍는다 (모든 LateUpdate 뒤 최종 자세).</summary>
        IEnumerator FrontShot(Transform who, string path)
        {
            var probe = new GameObject("EndProbe").AddComponent<EndProbe>();
            probe.act = () =>
            {
                var p = who.position;
                var to = view.transform.position - p; to.y = 0f;
                if (to.sqrMagnitude < 1e-4f) to = who.forward;
                var from = p + Quaternion.Euler(0f, 40f, 0f) * to.normalized * 3.2f + Vector3.up * 1.5f;
                var tp = ThirdPerson(p + Vector3.up * 1.1f, from);
                Shot(tp, path);
                Object.Destroy(tp.gameObject);
            };
            yield return null;
            Object.Destroy(probe.gameObject);
        }

        /// <summary>플레이어(머리 있는 원래 몸, 평소 그림자만)를 앞에서 비스듬히 찍는다 — 1인칭용 몸은 잠시 끔.</summary>
        IEnumerator PlayerShot(string path)
        {
            var probe = new GameObject("EndProbe").AddComponent<EndProbe>();
            probe.act = () =>
            {
                var rs = body.animator.GetComponentsInChildren<Renderer>(true).Where(r => r != body.VisibleBody && r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly).ToArray();
                foreach (var r in rs) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                bool vb = body.VisibleBody.enabled; body.VisibleBody.enabled = false;
                var t = body.animator.transform;
                var fwd = t.forward; fwd.y = 0f; fwd.Normalize();
                var tp = ThirdPerson(t.position + Vector3.up * 1.1f, t.position + Quaternion.Euler(0f, 35f, 0f) * fwd * 2.8f + Vector3.up * 1.5f);
                Shot(tp, path);
                Object.Destroy(tp.gameObject);
                foreach (var r in rs) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                body.VisibleBody.enabled = vb;
            };
            yield return null;
            Object.Destroy(probe.gameObject);
        }

        static void Log(string line) { Debug.Log("[BundleE] " + line); File.AppendAllText($"{Out}/summary.txt", line + "\n"); }

        // ---------------- R27 + R28: 무사 결투 — 칼 쥐기, 막기·베기, 칼 든 대기 ----------------
        [UnityTest]
        public IEnumerator R27_R28_Duel_SwordInHand_BlockSlash_GuardStance()
        {
            yield return Load(256);
            var sword = GameObject.Find("Player").GetComponentsInChildren<Transform>(true).First(t => t.name == "Sword");
            var hand = body.animator.GetBoneTransform(HumanBodyBones.RightHand);
            Assert.IsTrue(sword.IsChildOf(hand), "칼이 오른손 뼈 아래 (손과 함께 움직임)");
            float gripGap = Vector3.Distance(sword.position, hand.position);
            Log($"R27 칼 손잡이-손 거리 {gripGap:0.000} m");
            Assert.Less(gripGap, 0.15f, "손잡이가 손바닥에");
            for (int i = 0; i < 4000 && dir.State != StageState.Combat; i++) yield return Step();
            Assert.AreEqual(StageState.Combat, dir.State);
            var enc = dir.CurrentEncounter;
            Assert.IsTrue(enc.IsBossDuel);
            yield return null;
            Assert.IsTrue(body.SwordVisible, "전투 중 1인칭에 내 칼이 보임");
            bool sawSlash = false, sawBlock = false, blockSwordOnScreen = false; float blockGuard = 0f; Vector3 bladeCam = Vector3.zero;
            int clash0 = body.ClashCount;
            float bossGapIdle = -1f, maxBossGap = 0f; int bossIdleFrames = 0;
            var bossAnim = dir.boss.GetComponentInChildren<Animator>();
            for (int f = 0; f < 3000 && boot.clock.CurrentBeat < enc.StartBeat + 24; f++)
            {
                yield return Step();
                if (!sawSlash && body.LastState == BodyMoves.SwordSlash) { sawSlash = true; for (int k = 0; k < 12; k++) yield return Step(); Shot(view, $"{Out}/R27_slash_fp.png"); }
                if (!sawBlock && body.LastState == BodyMoves.SwordBlock)
                {
                    sawBlock = true;
                    for (int k = 0; k < 12; k++) yield return Step();
                    var probe = new GameObject("EndProbe").AddComponent<EndProbe>();
                    probe.act = () =>
                    {
                        Shot(view, $"{Out}/R27_block_fp.png");
                        var sr = body.swordRenderers[0];
                        var gT = body.swordGrip; var tT = gT.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "WeaponTip");
                        var vp = view.WorldToViewportPoint(tT != null ? Vector3.Lerp(gT.position, tT.position, 0.3f) : sr.bounds.center);   // 칼자루 쪽 30% (칼끝은 화면 밖까지 가로지를 수 있음)
                        blockSwordOnScreen = vp.z > 0.05f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f && sr.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On && sr.enabled;
                        string V(Vector3 w) { var q = view.WorldToViewportPoint(w); return $"({q.x:0.00},{q.y:0.00},{q.z:0.00})"; }
                        var swT = body.swordGrip; var tipT = swT.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "WeaponTip");
                        bladeCam = view.transform.InverseTransformDirection(swT.forward);
                        Log($"R27 막기 화면 위치(최종 자세): 오른손 {V(body.animator.GetBoneTransform(HumanBodyBones.RightHand).position)} 칼자루 {V(swT.position)} 칼끝 {(tipT != null ? V(tipT.position) : "-")} 칼 방향(카메라 기준) {bladeCam}");
                        var only = new GameObject("SwordOnly").AddComponent<Camera>();
                        only.enabled = false; only.clearFlags = CameraClearFlags.SolidColor; only.backgroundColor = new Color(0.2f, 0.2f, 0.3f);
                        only.cullingMask = 1 << BodyLayer; only.fieldOfView = view.fieldOfView; only.nearClipPlane = view.nearClipPlane;
                        only.transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
                        var oldLayers = body.swordRenderers.Select(x => x.gameObject.layer).ToArray();
                        foreach (var x in body.swordRenderers) x.gameObject.layer = BodyLayer;
                        int vbl = body.VisibleBody.gameObject.layer; body.VisibleBody.gameObject.layer = BodyLayer;
                        Shot(only, $"{Out}/R27_block_swordonly.png");
                        for (int i2 = 0; i2 < oldLayers.Length; i2++) body.swordRenderers[i2].gameObject.layer = oldLayers[i2];
                        body.VisibleBody.gameObject.layer = vbl;
                        Object.Destroy(only.gameObject);
                    };
                    yield return null;
                    Object.Destroy(probe.gameObject);
                    blockGuard = body.SwordGuardWeight;
                }
                var en = dir.boss.GetComponentInChildren<EnemyAnimator>();
                if (en != null && en.CurrentState == EnemyMoves.Idle && bossAnim != null && !bossAnim.IsInTransition(0) && bossAnim.GetCurrentAnimatorStateInfo(0).IsName(EnemyMoves.Idle))
                {
                    float g = HandGap(bossAnim);
                    maxBossGap = Mathf.Max(maxBossGap, g);
                    if (bossGapIdle < 0f) bossGapIdle = g;
                    // 3인칭 미리보기: 무사 대기 자세 — 대기 상태일 때, 플레이어 쪽에서 비스듬히 (앞모습). 플레이어 대기 검사는 일반 전투 테스트에서 (R28 평가 1회차)
                    if (++bossIdleFrames == 10) yield return FrontShot(dir.boss.transform, $"{Out}/R28_boss_stance_tp.png");
                }
            }
            Log($"R27 베기 {sawSlash}, 막기 {sawBlock}; R28 무사 대기 두 손 거리 처음 {bossGapIdle:0.00} 최대 {maxBossGap:0.00} m");
            Log($"R27 막기 순간 내 칼 화면 안 {blockSwordOnScreen} (막기 자세 {blockGuard:0.00}), 칼끼리 부딪침 {body.ClashCount - clash0}번, 마지막 불꽃 자리-카메라 {Vector3.Distance(body.LastClashPoint, view.transform.position):0.00} m");
            Assert.IsTrue(blockSwordOnScreen, "막는 순간 1인칭 화면에 내 칼이 보임");
            Assert.Less(bladeCam.x, -0.6f, "막기: 칼날이 화면을 오른쪽에서 왼쪽으로 가로질러 막음 (손목을 꺾지 않는 가로막기)");
            Assert.Less(Mathf.Abs(bladeCam.z), 0.5f, "막기: 칼날이 앞뒤가 아니라 가로");
            Assert.Greater(body.ClashCount - clash0, 1, "막기·베기마다 칼끼리 부딪쳐 불꽃");
            Assert.Less(Vector3.Distance(body.LastClashPoint, view.transform.position), 2.0f, "불꽃은 눈앞 칼이 닿는 곳");
            Assert.IsTrue(sawSlash, "내 공격·칼 겨루기 성공 → 칼로 벰");
            Assert.IsTrue(sawBlock, "적 공격 ↑ 성공 → 칼로 막음");
            Assert.Greater(bossGapIdle, 0f, "무사 대기 상태를 봄");
            Assert.Less(maxBossGap, 0.7f, "무사 대기: 두 손으로 칼을 쥔 자세 (팔 벌림 아님)");
            Assert.GreaterOrEqual(bossIdleFrames, 10, "무사 대기 미리보기를 찍음");
            Assert.IsTrue(File.Exists($"{Out}/R27_slash_fp.png") && File.Exists($"{Out}/R27_block_fp.png"));
        }

        // ---------------- R28: 일반 병사 대기 + R34: 처형 뒤 팔 가림 ----------------
        [UnityTest]
        public IEnumerator R28_R34_NormalEnemy_Stance_ExecutionArmCoverShort()
        {
            var ev = StageChart.Load().events.Where(e => e.IsDuel).OrderBy(e => e.runBeat).First();
            yield return Load(ev.runBeat - 6);
            // 내 몸만 그리는 측정 카메라 (R06 방식)
            var pc = new GameObject("BundleE_Probe").AddComponent<Camera>();
            pc.enabled = false; pc.clearFlags = CameraClearFlags.SolidColor; pc.backgroundColor = new Color(1f, 0f, 1f, 1f);
            pc.cullingMask = 1 << BodyLayer;
            var rt = new RenderTexture(160, 90, 24); pc.targetTexture = rt;
            var tex = new Texture2D(160, 90, TextureFormat.RGBA32, false);
            body.VisibleBody.gameObject.layer = BodyLayer;
            float Cover()
            {
                pc.transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
                pc.fieldOfView = view.fieldOfView; pc.nearClipPlane = view.nearClipPlane; pc.aspect = 16f / 9f;
                pc.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 160, 90), 0, 0); RenderTexture.active = prev;
                int hit = 0; var px = tex.GetPixels32();
                foreach (var c in px) if (!(c.r > 240 && c.g < 15 && c.b > 240)) hit++;
                return hit / (float)px.Length;
            }
            for (int i = 0; i < 4000 && dir.State != StageState.Combat; i++) yield return Step();
            Assert.AreEqual(StageState.Combat, dir.State);
            var actor = dir.CurrentEncounter.Actors[0];
            var an = actor.GetComponentInChildren<Animator>();
            float maxGap = 0f; int idleFrames = 0;
            bool execStarted = false; double execBeat = double.NaN;
            float bigCoverSeconds = 0f, maxCover = 0f;
            bool shot = false, soldierShot = false, playerShot = false;
            int playerIdleFrames = 0; float playerMaxGap = 0f;
            for (int f = 0; f < 4000 && (dir.State == StageState.Combat || (execStarted && boot.clock.CurrentBeat < execBeat + 4)); f++)
            {
                yield return Step();
                var ea = actor.GetComponentInChildren<EnemyAnimator>();
                if (!actor.Defeated && ea != null && ea.CurrentState == EnemyMoves.Idle && !an.IsInTransition(0) && an.GetCurrentAnimatorStateInfo(0).IsName(EnemyMoves.Idle))
                {
                    idleFrames++; maxGap = Mathf.Max(maxGap, HandGap(an));
                    if (!soldierShot && idleFrames >= 10) { soldierShot = true; yield return FrontShot(actor.transform, $"{Out}/R28_soldier_stance_tp.png"); }
                }
                // R28 평가 1회차: 플레이어 전투 대기(CombatIdle)도 반드시 확인
                if (dir.State == StageState.Combat && body.LastState == BodyMoves.CombatIdle && !body.animator.IsInTransition(0) && body.animator.GetCurrentAnimatorStateInfo(0).IsName(BodyMoves.CombatIdle))
                {
                    playerIdleFrames++; playerMaxGap = Mathf.Max(playerMaxGap, HandGap(body.animator));
                    if (!playerShot && playerIdleFrames >= 10) { playerShot = true; yield return PlayerShot($"{Out}/R28_player_stance_tp.png"); }
                }
                if (!execStarted && body.LastState.StartsWith("Exec_")) { execStarted = true; execBeat = boot.clock.CurrentBeat; }
                if (execStarted && boot.clock.CurrentBeat < execBeat + 4)
                {
                    float c = Cover();
                    maxCover = Mathf.Max(maxCover, c);
                    if (c > 0.25f) bigCoverSeconds += 0.01f;
                    if (!shot && boot.clock.CurrentBeat > execBeat + 0.5) { shot = true; Shot(view, $"{Out}/R34_exec_fp.png"); }
                }
            }
            Log($"R28 병사 대기 프레임 {idleFrames}, 두 손 거리 최대 {maxGap:0.00} m; R34 처형 {body.LastState} 화면 25% 넘게 가린 시간 {bigCoverSeconds:0.00} s, 최대 가림 {maxCover:0.000}, 줄인 횟수 {body.ExecCappedCount}");
            Log($"R28 플레이어 전투 대기 프레임 {playerIdleFrames}, 두 손 거리 최대 {playerMaxGap:0.00} m");
            Assert.Greater(idleFrames, 0);
            Assert.Less(maxGap, 0.7f, "병사 대기: 두 손으로 칼 (팔 벌림 아님)");
            Assert.Greater(playerIdleFrames, 0, "플레이어 전투 대기(CombatIdle) 상태를 봄");
            Assert.Less(playerMaxGap, 0.7f, "플레이어 대기도 두 손으로 칼 (팔 벌림 아님)");
            Assert.IsTrue(File.Exists($"{Out}/R28_soldier_stance_tp.png") && File.Exists($"{Out}/R28_player_stance_tp.png"), "병사·플레이어 3인칭 미리보기");
            Assert.IsTrue(execStarted, "처형");
            Assert.Greater(body.ExecCappedCount, 0, "처형 동작을 줄여 끝냄");
            // 가림 측정은 R34_ExecutionArmCover_BeforeAfterCap (프레임 끝 측정·줄이기 전 기준선 비교, R34 평가 1회차)
            rt.Release();
            Object.Destroy(pc.gameObject);
        }

        // ---------------- R34: 처형 뒤 팔 가림 — 줄이기 전·후 비교 (R34 평가 1회차) ----------------
        /// <summary>모든 LateUpdate 뒤(최종 자세) 매 프레임 내 몸·칼만 그려 화면 가림 비율을 잰다 (R06 방식).</summary>
        [DefaultExecutionOrder(32000)]
        class CoverProbe : MonoBehaviour
        {
            public Camera view, probe; public RenderTexture rt; public Texture2D tex;
            public bool measuring; public float last; public System.Action<float> onMeasure;
            void LateUpdate()
            {
                if (!measuring) return;
                probe.transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
                probe.fieldOfView = view.fieldOfView; probe.nearClipPlane = view.nearClipPlane; probe.aspect = (float)rt.width / rt.height;
                probe.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); RenderTexture.active = prev;
                int hit = 0; var px = tex.GetPixels32();
                foreach (var c in px) if (!(c.r > 240 && c.g < 15 && c.b > 240)) hit++;
                last = hit / (float)px.Length;
                onMeasure?.Invoke(last);
            }
        }

        class ExecCover { public float over25, over10, max, meanVisible; public int frames, bodyVisible; public string state; public int capped; }

        IEnumerator MeasureExecution(bool cap, ExecCover res, string shotPath, double startBeat)
        {
            yield return Load(startBeat);
            body.CapExecutionEnabled = cap;
            body.VisibleBody.gameObject.layer = BodyLayer;
            if (body.swordRenderers != null) foreach (var r in body.swordRenderers) if (r != null) r.gameObject.layer = BodyLayer;
            var go = new GameObject("R34Probe");
            var cp = go.AddComponent<CoverProbe>();
            cp.view = view;
            var pcGo = new GameObject("R34ProbeCam"); pcGo.transform.SetParent(go.transform, false);
            cp.probe = pcGo.AddComponent<Camera>();
            cp.probe.enabled = false; cp.probe.clearFlags = CameraClearFlags.SolidColor; cp.probe.backgroundColor = new Color(1f, 0f, 1f, 1f);
            cp.probe.cullingMask = 1 << BodyLayer;
            cp.rt = new RenderTexture(192, 108, 24); cp.probe.targetTexture = cp.rt;
            cp.tex = new Texture2D(192, 108, TextureFormat.RGBA32, false);
            float shotAt = 0f;
            cp.onMeasure = c =>
            {
                res.frames++;
                if (body.VisibleBody.enabled) res.bodyVisible++;
                if (c > 0.25f) res.over25 += 0.01f;
                if (c > 0.10f) res.over10 += 0.01f;
                if (c > res.max) res.max = c;
                if (shotPath != null && c > shotAt + 0.03f) { shotAt = c; Shot(view, shotPath); }   // 가림이 가장 큰 프레임
            };
            for (int i = 0; i < 4000 && dir.State != StageState.Combat; i++) yield return Step();
            bool started = false; double execBeat = double.NaN;
            for (int f = 0; f < 6000; f++)
            {
                if (!started && body.LastState.StartsWith("Exec_")) { started = true; execBeat = boot.clock.CurrentBeat; }
                cp.measuring = started && boot.clock.CurrentBeat < execBeat + 4;
                if (started && boot.clock.CurrentBeat >= execBeat + 4) break;
                yield return Step();
            }
            cp.measuring = false;
            res.state = body.LastState; res.capped = body.ExecCappedCount;
            Assert.IsTrue(started, "처형");
            cp.rt.Release();
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator R34_ExecutionArmCover_BeforeAfterCap()
        {
            // 모든 일반 전투(결투 사건)의 처형을 줄이기 전·후로 잼 — 처형 종류(칼 뺏어 베기·찌르기·발차기)마다 팔 동작이 다르다
            var duels = StageChart.Load().events.Where(e => e.IsDuel).OrderBy(e => e.runBeat).ToList();
            var before = new ExecCover(); var after = new ExecCover();
            string Row(string n, ExecCover r) => $"| {n} | {r.state} | {r.frames} | {r.bodyVisible} | {r.max:0.000} | {r.over25:0.00} | {r.over10:0.00} | {r.capped} |";
            var md = "# R34 처형 팔 가림 (처형 시작 후 4박, 프레임 끝 측정, 내 몸·칼만 그림)\n\n| 전투 | 줄이기 | 측정 프레임 | 몸 보인 프레임 | 최대 가림 | 25% 넘은 시간(s) | 10% 넘은 시간(s) | 줄인 횟수 |\n|---|---|---|---|---|---|---|---|\n";
            float worstBefore = -1f;
            foreach (var ev in duels)
            {
                var b = new ExecCover(); var a = new ExecCover();
                yield return MeasureExecution(false, b, $"{Out}/R34_tmp_before.png", ev.runBeat - 6);
                b.state = $"{ev.runBeat}박 전 ({b.state})";
                if (b.max > worstBefore && File.Exists($"{Out}/R34_tmp_before.png")) { worstBefore = b.max; File.Copy($"{Out}/R34_tmp_before.png", $"{Out}/R34_exec_before_maxcover.png", true); }
                yield return MeasureExecution(true, a, $"{Out}/R34_tmp_after.png", ev.runBeat - 6);
                a.state = $"{ev.runBeat}박 후";
                if (b.max >= worstBefore - 1e-6f && File.Exists($"{Out}/R34_tmp_after.png")) File.Copy($"{Out}/R34_tmp_after.png", $"{Out}/R34_exec_fp.png", true);
                md += Row($"{ev.runBeat}박", b).Replace($"| {b.state} |", "| 전 |") + "\n" + Row($"{ev.runBeat}박", a).Replace($"| {a.state} |", "| 후 |") + "\n";
                before.over25 += b.over25; before.over10 += b.over10; before.max = Mathf.Max(before.max, b.max); before.frames += b.frames;
                after.over25 += a.over25; after.over10 += a.over10; after.max = Mathf.Max(after.max, a.max); after.frames += a.frames; after.capped += a.capped;
            }
            md += $"\n합계: 줄이기 전 >10% {before.over10:0.00}s, >25% {before.over25:0.00}s, 최대 {before.max:0.000} / 줄인 뒤 >10% {after.over10:0.00}s, >25% {after.over25:0.00}s, 최대 {after.max:0.000}\n";
            File.WriteAllText($"{Out}/R34_cover.md", md);
            Log($"R34 줄이기 전: 최대 {before.max:0.000}, >25% {before.over25:0.00}s, >10% {before.over10:0.00}s / 줄인 뒤: 최대 {after.max:0.000}, >25% {after.over25:0.00}s, >10% {after.over10:0.00}s");
            Assert.Greater(before.max, 0.05f, "측정이 실제로 내 몸을 그림 (줄이기 전 처형 팔이 화면에 보임)");
            Assert.Greater(after.capped, 0, "처형 동작을 줄여 끝냄");
            Assert.Less(after.over10, before.over10, "줄인 뒤 팔이 화면 10% 넘게 가리는 시간이 줄어듦");
            Assert.LessOrEqual(after.over25, 0.6f, "줄인 뒤 화면 25% 넘게 가리는 시간 0.6초 이하");
        }

        // ---------------- R29: 뒤돌아보기 천천히 + 쫓는 무리 ----------------
        [UnityTest]
        public IEnumerator R29_LookBack_Slow_PursuersVisible()
        {
            // 돌아오기가 돌아보기보다 느림
            float turnTime = GameConfig.Fx.LookBackTurnPart * (float)GameConfig.Fx.LookBackBeats * GameConfig.SecondsPerBeat;
            float backTime = GameConfig.Fx.LookBackReturnPart * (float)GameConfig.Fx.LookBackBeats * GameConfig.SecondsPerBeat;
            Assert.GreaterOrEqual(turnTime, 0.28f, "돌아보는 데 0.3초 안팎 (예전 0.15초)");
            Assert.Greater(backTime, turnTime, "돌아오기가 더 천천히");
            var lb0 = GameConfig.Fx.LookBackRunBeats;
            int seen = 0;
            for (int k = 0; k < lb0.Length; k++)
            {
                yield return Load(lb0[k] - 6);
                var lb = Object.FindAnyObjectByType<LookBackCut>();
                int visibleMid = 0, maxTogether = 0; bool shot = false;
                for (int f = 0; f < 3000 && boot.player.VisualRunBeat < lb0[k] + LookBackCut.LengthOf(k) + 1.3; f++)
                {
                    yield return Step();
                    if (lb.Active && Mathf.Abs(lb.CurrentYaw) > 150f)
                    {
                        int together = 0;
                        foreach (var p in LookBackCut.Enemies())   // 쫓는 무리 + 그 밖의 쫓는 적
                        {
                            if (p == null || !p.gameObject.activeInHierarchy) continue;
                            var vp = view.WorldToViewportPoint(p.position + Vector3.up * 1.1f);
                            if (vp.z > 0f && vp.z < 45f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f) { visibleMid++; together++; }
                        }
                        maxTogether = Mathf.Max(maxTogether, together);
                        Assert.IsTrue(lb.PursuersShown, $"컷 {k}: 쫓는 무리 (지붕 위 컷 포함, R29 평가 1회차)");
                        if (!shot) { shot = true; Shot(view, $"{Out}/R29_lookback_{k}.png"); }
                    }
                }
                Log($"R29 컷 {k} ({lb0[k]}박, 지붕 {LookBackCut.OnRoof(lb0[k])}): 돌아본 화면에 쫓는 적 보인 프레임·명 {visibleMid}, 한 화면 최대 {maxTogether}명");
                Assert.GreaterOrEqual(maxTogether, 3, $"컷 {k}: 돌아본 한 화면에 쫓는 적 3명 이상 (무리)");
                if (visibleMid > 0) seen++;
                Assert.IsFalse(lb.PursuersShown, "컷이 끝나면 숨김");
            }
            Assert.AreEqual(lb0.Length, seen, "모든 뒤돌아보기에서 쫓는 적이 보임");
        }

        // ---------------- R30: 매트릭스 회피 칼날 ----------------
        [UnityTest]
        public IEnumerator R30_DuelBullet_BladePassesInFrontOfFace()
        {
            yield return Load(256, bullet: true);
            for (int i = 0; i < 4000 && dir.State != StageState.Combat; i++) yield return Step();
            var enc = dir.CurrentEncounter;
            float minDist = float.PositiveInfinity; bool shot = false;
            for (int f = 0; f < 6000 && boot.clock.CurrentBeat < enc.StartBeat + GameConfig.BulletTime.BossBeats[0] + 2; f++)
            {
                yield return Step();
                var bt = boot.bulletTime.BladeTransform;
                if (bt != null)
                {
                    var tipT = bt.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "WeaponTip");
                    var a0 = bt.position; var a1 = tipT != null ? tipT.position : bt.position + bt.forward * GameConfig.EnemyBody.SwordLength;
                    var e = view.transform.position;
                    var ab = a1 - a0; float tt = Mathf.Clamp01(Vector3.Dot(e - a0, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                    minDist = Mathf.Min(minDist, Vector3.Distance(e, a0 + ab * tt));
                    if (!shot && boot.bulletTime.BladeSweeping) { for (int k = 0; k < 25; k++) yield return Step(); if (boot.bulletTime.BladeSweeping) { shot = true; Shot(view, $"{Out}/R30_blade_fp.png"); } }
                }
            }
            Log($"R30 칼날 지나간 횟수 {boot.bulletTime.BladeSweepCount}, 카메라와 최소 거리 {minDist:0.00} m");
            Assert.Greater(boot.bulletTime.BladeSweepCount, 0, "회피 슬로모션에서 칼날이 지나감");
            Assert.Less(minDist, 0.6f, "얼굴 앞 가까이");
            Assert.Greater(minDist, 0.05f, "카메라를 뚫지는 않음");
        }

        // ---------------- R31: 윈드밀 3인칭 컷 + 쓰러진 적 + 칼 날아감 ----------------
        [UnityTest]
        public IEnumerator R31_Windmill_FootVisible_FallenPose_WeaponFlies()
        {
            yield return Load(181, bullet: true);
            for (int i = 0; i < 4000 && dir.State != StageState.Combat; i++) yield return Step();
            var enc = dir.CurrentEncounter;
            Assert.IsTrue(enc.Event.IsSurround);
            int cutFrames = 0, footFrames = 0; bool shot = false, cutSeen = false;
            for (int f = 0; f < 8000 && !(enc.Done && cutSeen && !boot.bulletTime.WindmillCut); f++)
            {
                if (boot.bulletTime.WindmillCut) cutSeen = true;
                yield return Step();
                if (boot.bulletTime.WindmillCut)
                {
                    cutFrames++;
                    var foot = body.animator.GetBoneTransform(HumanBodyBones.RightFoot);
                    var foot2 = body.animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                    foreach (var ft in new[] { foot, foot2 })
                    {
                        var vp = view.WorldToViewportPoint(ft.position);
                        if (vp.z > 0f && vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.05f && vp.y < 0.95f) { footFrames++; break; }
                    }
                    if (!shot && cutFrames == 25) { shot = true; Shot(view, $"{Out}/R31_windmill_cut.png"); }
                }
            }
            float waitUntil = Time.realtimeSinceStartup + 8f;   // 칼이 슬로모션 속에서 떨어질 때까지 (실제 시간)
            while (Time.realtimeSinceStartup < waitUntil && enc.Actors.Any(a => a != null && a.weaponVisual != null && (!a.WeaponDropped || a.weaponVisual.position.y > a.transform.position.y + 0.2f))) yield return Step();
            for (int f = 0; f < 60; f++) yield return Step();
            // 쓰러진 적: 엉덩이가 낮음(누움), 칼이 손에서 빠져 바닥에
            int lying = 0, dropped = 0;
            var tp = ThirdPerson(dir.CurrentEncounter != null ? dir.CurrentEncounter.Actors[0].transform.position : enc.Actors[0].transform.position, boot.player.transform.position + Vector3.up * 4f - boot.player.transform.forward * 3f);
            foreach (var a in enc.Actors)
            {
                if (a == null) continue;
                var hips = a.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Hips);
                float hipUp = hips.position.y - a.transform.position.y;
                if (hipUp < 0.5f) lying++;
                if (a.WeaponDropped && a.weaponVisual != null && Mathf.Abs(a.weaponVisual.position.y - a.transform.position.y) < 0.4f) dropped++;
                Log($"R31 적 {a.name}: 칼 빠짐 {a.WeaponDropped}, 칼 높이 {(a.weaponVisual != null ? a.weaponVisual.position.y : float.NaN):0.00}, 적 발 높이 {a.transform.position.y:0.00}, 엉덩이 {hipUp:0.00}, 칼 부모 {(a.weaponVisual != null && a.weaponVisual.parent != null ? a.weaponVisual.parent.name : "-")}");
            }
            var center = enc.Actors.Where(a => a != null).Select(a => a.transform.position).Aggregate(Vector3.zero, (s, p) => s + p) / enc.Actors.Count(a => a != null);
            tp.transform.position = center + new Vector3(0f, 7f, -6f);
            tp.transform.LookAt(center);
            Shot(tp, $"{Out}/R31_fallen_tp.png");
            Object.Destroy(tp.gameObject);
            Log($"R31 윈드밀 컷 프레임 {cutFrames}, 발이 화면에 보인 프레임 {footFrames}; 누운 적 {lying}/{enc.Actors.Length}, 칼이 빠져 바닥에 {dropped}");
            Assert.Greater(cutFrames, 20, "윈드밀 3인칭 컷");
            Assert.Greater(footFrames, cutFrames / 3, "컷 동안 내 발이 화면에 보임");
            Assert.AreEqual(enc.Actors.Count(a => a != null), lying, "쓰러진 적이 모두 누워 있음 (선 자세로 눕지 않음)");
            Assert.AreEqual(enc.Actors.Count(a => a != null), dropped, "칼이 손에서 빠져 바닥에 떨어짐");
        }

        // ---------------- R32: 돌진 적과 엇갈림 ----------------
        [UnityTest]
        public IEnumerator R32_Charge_PassesBeside_NotThrough()
        {
            var ev = StageChart.Load().events.First(e => e.EventKind == EventCatalog.Charge && e.IsEnemy);
            yield return Load(ev.runBeat - 6);
            var prop = Object.FindObjectsByType<EventProp>(FindObjectsSortMode.None).First(p => p.runBeat == ev.runBeat);
            double hit = ev.runBeat + EventCatalog.Get(EventCatalog.Charge).Slots[0].Offset;
            float minGap = float.PositiveInfinity; bool shotFp = false, shotTp = false, sawSlash = false, sawClimb = false;
            for (int f = 0; f < 3000 && boot.player.VisualRunBeat < hit + 1.5; f++)
            {
                yield return Step();
                double v = boot.player.VisualRunBeat;
                if (v > hit - 0.4 && v < hit + 0.4)
                {
                    var a = view.transform.position; a.y = 0f;
                    var b = prop.enemy.position; b.y = 0f;
                    minGap = Mathf.Min(minGap, Vector3.Distance(a, b));
                }
                if (body.LastState == BodyMoves.SwordSlash) sawSlash = true;
                if (body.LastState.StartsWith("Climb")) sawClimb = true;
                if (!shotFp && v >= hit - 0.15) { shotFp = true; Shot(view, $"{Out}/R32_charge_fp.png"); }
                if (!shotTp && v >= hit - 0.05)
                {
                    shotTp = true;
                    var pl = boot.player.transform;
                    var tp = ThirdPerson(pl.position + Vector3.up * 1f, pl.position - pl.forward * 4f + Vector3.up * 2.5f);
                    Shot(tp, $"{Out}/R32_charge_tp.png");
                    Object.Destroy(tp.gameObject);
                }
            }
            Log($"R32 돌진 적({ev.runBeat}박)과 판정 ±0.4박 사이 수평 최소 거리 {minGap:0.00} m, 쓰러짐 {prop.EnemyDown}, 내 동작 {body.LastState}");
            Assert.Greater(minGap, 1.0f, "엇갈려 지나감 (몸이 겹치지 않음)");
            Assert.IsTrue(sawSlash, "오르기(점프) 대신 칼로 벰");
            Assert.IsFalse(sawClimb, "↑라도 오르기 동작이 아님");
        }

        // ---------------- R33: 벽타기 막는 짐 ----------------
        [UnityTest]
        public IEnumerator R33_WallRun_HasBlockerOnPath()
        {
            var walls = StageChart.Load().events.Where(e => e.kind == "wall_run").ToList();
            Assert.Greater(walls.Count, 0);
            yield return Load(walls[0].runBeat - 4);
            foreach (var w in walls)
            {
                var block = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Prop_wallrun_block_cart")).Select(t => t.position).ToList();
                var center = MapLayout.PointAt((w.runBeat + 2) * GameConfig.MetersPerBeat);
                float nearest = block.Count == 0 ? 99f : block.Min(p => Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(center.x, 0, center.z)));
                Log($"R33 벽타기 {w.runBeat}박: 길 가운데와 가장 가까운 막는 수레 {nearest:0.00} m");
                Assert.Less(nearest, 2.5f, $"{w.runBeat}박 벽타기 자리에 길을 막는 수레");
            }
            for (int k = 0; k < walls.Count; k++)
            {
                yield return Load(walls[k].runBeat - 1.5);
                for (int f = 0; f < 2000 && boot.player.VisualRunBeat < walls[k].runBeat + 0.6; f++) yield return Step();
                Shot(view, $"{Out}/R33_wallrun_{walls[k].runBeat}_fp.png");
            }
        }

        // ---------------- R27 보강: 손가락으로 칼자루를 쥐는지 (손 확대) ----------------
        /// <summary>오른손을 0.55 m 앞에서 찍고, 검지·약지 끝이 칼자루(손잡이 점)에서 가까운지 잰다.</summary>
        IEnumerator HandShot(Animator a, Transform grip, string name, List<string> lines)
        {
            yield return null;
            var hand = a.GetBoneTransform(HumanBodyBones.RightHand);
            var tipI = a.GetBoneTransform(HumanBodyBones.RightIndexDistal) ?? a.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);
            var tipR = a.GetBoneTransform(HumanBodyBones.RightRingDistal) ?? a.GetBoneTransform(HumanBodyBones.RightRingIntermediate);
            var root = a.transform;
            var tp = ThirdPerson(hand.position, hand.position + (root.right * 0.35f + root.forward * 0.35f + Vector3.up * 0.15f));
            tp.nearClipPlane = 0.02f;
            tp.fieldOfView = 35f;
            Shot(tp, $"{Out}/R27_hand_{name}.png");
            Object.Destroy(tp.gameObject);
            float dI = tipI != null && grip != null ? Vector3.Distance(tipI.position, grip.position) : -1f;
            float dR = tipR != null && grip != null ? Vector3.Distance(tipR.position, grip.position) : -1f;
            lines.Add($"{name}: 손가락 뼈 {(tipI != null ? "있음" : "없음")}, 검지 끝-칼자루 {dI:0.000} m, 약지 끝-칼자루 {dR:0.000} m");
        }

        [UnityTest]
        public IEnumerator R27_Hands_FingersWrapHilt()
        {
            var lines = new List<string>();
            yield return Load(256);
            for (int i = 0; i < 4000 && dir.State != StageState.Combat; i++) yield return Step();
            for (int i = 0; i < 100; i++) yield return Step();
            var bossA = dir.boss.GetComponentInChildren<Animator>();
            yield return HandShot(bossA, dir.boss.weaponVisual, "boss", lines);
            var pSword = GameObject.Find("Player").GetComponentsInChildren<Transform>(true).First(t => t.name == "Sword");
            yield return HandShot(body.animator, pSword, "player", lines);
            var ev = StageChart.Load().events.First(e => e.EventKind == EventCatalog.Charge && e.IsEnemy);
            yield return Load(ev.runBeat - 1);
            for (int i = 0; i < 30; i++) yield return Step();
            var prop = Object.FindObjectsByType<EventProp>(FindObjectsSortMode.None).First(p => p.runBeat == ev.runBeat);
            var fa = prop.enemy.GetComponentInChildren<Animator>();
            yield return HandShot(fa, prop.enemy.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "WeaponGrip"), "charge_soldier", lines);
            var spear = Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None).FirstOrDefault(a => a.kind == "spear");
            if (spear != null) yield return HandShot(spear.GetComponentInChildren<Animator>(), spear.weaponVisual, "spear", lines);
            foreach (var l in lines) Log("R27 손 " + l);
            Assert.IsTrue(lines.All(l => l.Contains("손가락 뼈 있음")), "모두 손가락 뼈가 있음");
            Assert.IsTrue(lines.All(l => { var m = System.Text.RegularExpressions.Regex.Match(l, @"검지 끝-칼자루 ([0-9.]+)"); return m.Success && float.Parse(m.Groups[1].Value) < 0.09f; }), "검지 끝이 칼자루를 감쌈 (9 cm 안)");
        }

        // ---------------- R26: 콤보 단계 ----------------
        [UnityTest]
        public IEnumerator R26_ComboTiers_ColorScalePop()
        {
            yield return Load(0);
            var hud = boot.hud;
            var b = boot.score.Board;
            var seen = new List<string>();
            int[] marks = { 4, 5, 10, 20, 50 };
            int n = 0;
            foreach (var m in marks)
            {
                while (n < m) { b.Add(Judgement.Perfect, true); n++; }
                yield return null;
                seen.Add($"{m}:{hud.ComboTier}/{hud.ComboText.color}/{hud.ComboText.rectTransform.localScale.x:0.00}");
                Assert.AreEqual(GameHud.TierOf(m), hud.ComboTier, $"콤보 {m} 단계");
                if (m >= 5) Assert.Greater(hud.ComboPop, 0f, $"콤보 {m}: 단계가 오르는 순간 튐");
            }
            Log("R26 " + string.Join(" | ", seen));
            int upsFirstPass = hud.ComboTierUps;
            // 튐이 끝난 뒤 안정된 단계별 상태 (실제 게임 씬, BeatFx 켜짐): 화면 글자 크기가 단계마다 커지고, 색이 다르고, 10↑부터 빛 테두리
            var b2 = new ScoreBoard();
            var stable = new List<(int tier, float scale, Color col, bool glow)>();
            int c2 = 0;
            foreach (var m in new[] { 3, 5, 10, 20, 50 })
            {
                b.Reset();
                for (int i = 0; i < m; i++) b.Add(Judgement.Perfect, true);
                float until = Time.realtimeSinceStartup + GameConfig.ComboPopSeconds + 0.3f;
                while (Time.realtimeSinceStartup < until) yield return null;
                var ol = hud.ComboText.GetComponent<UnityEngine.UI.Outline>();
                float shown = hud.ComboText.rectTransform.localScale.x / Mathf.Max(1e-4f, hud.ComboBeatScale);   // 박자 출렁임을 뺀 화면 크기
                stable.Add((hud.ComboTier, shown, hud.ComboText.color, ol != null && ol.enabled));
                c2++;
            }
            Log("R26 안정 상태 " + string.Join(" | ", stable.Select(x => $"단계{x.tier} 크기{x.scale:0.00} 색{x.col} 테두리{x.glow}")));
            for (int i = 1; i < stable.Count; i++)
            {
                Assert.Greater(stable[i].scale, stable[i - 1].scale + 0.03f, $"단계 {stable[i].tier}: 화면 글자가 더 큼 (BeatFx가 덮어쓰지 않음)");
                Assert.AreNotEqual(stable[i - 1].col, stable[i].col, $"단계 {stable[i].tier}: 색이 다름");
            }
            Assert.IsFalse(stable[0].glow, "5 미만은 테두리 없음");
            Assert.IsTrue(stable.Where(x => x.tier >= 2).All(x => x.glow), "10↑ 빛 테두리");
            Assert.AreEqual(4, upsFirstPass, "단계 4번 오름 (5·10·20·50)");
            var colors = Enumerable.Range(0, GameConfig.ComboTierColors.Length).Select(i => GameConfig.ComboTierColors[i]).Distinct().Count();
            Assert.AreEqual(GameConfig.ComboTierColors.Length, colors, "단계마다 다른 색");
        }
    }
}
