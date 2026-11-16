using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R15: 무사 결투 FAIL 되감기 때 플레이어·무사가 단계 시작 자리로 거꾸로 이어 돌아감 (순간 이동 아님).
    /// 작업 R16: 멈춰 싸울 때(전투 중 이동 거의 없음) 내 몸은 달리기가 아니라 대기 자세, 전투가 끝나면 달리기로.
    /// </summary>
    public class R15_R16_CombatPlacePlayTests
    {
        double now;
        float real;
        GameBootstrap boot;
        StageDirector dir;
        PlayerBody body;

        IEnumerator Load(double start)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            body = Object.FindAnyObjectByType<PlayerBody>();
            now = 0; real = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.RewindRealTime = () => real;
            dir.FailHitSeconds = 0f;
            dir.slowMotion.RealTime = () => real;
            dir.MotionDeltaTime = () => 0.01f;   // 전투 이동도 박자 시계 걸음으로
            boot.presentation.timeFx.RealTime = () => Time.realtimeSinceStartup + (float)now;
            if (boot.bulletTime != null) boot.bulletTime.enabled = false;
            dir.AllowEventSlowMotion = false;
            boot.StartRun(start);
            yield return null;
        }

        [TearDown]
        public void TearDown() { if (dir != null) dir.slowMotion.Cancel(); Time.timeScale = 1f; }

        IEnumerator AdvanceTo(double t)
        {
            while (now < t - 1e-9) { now = System.Math.Min(t, now + 0.01); yield return null; }
        }

        /// <summary>그 원을 delay(초) 늦게 침. null = 안 침(FAIL).</summary>
        IEnumerator Hit(JudgeCue cue, double? delay)
        {
            yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) - 0.03);
            if (delay.HasValue)
            {
                now = boot.clock.TimeOfBeat(cue.TargetBeat) + delay.Value;
                boot.judge.Submit(cue.Key, now);
                yield return null;
            }
            else yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) + 0.26);
        }

        IEnumerator UntilCombat()
        {
            for (int i = 0; i < 4000 && dir.State != StageState.Combat; i++)
            {
                now += 0.02;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                yield return null;
            }
            Assert.AreEqual(StageState.Combat, dir.State);
        }

        [UnityTest]
        public IEnumerator R15_DuelFail_RewindsPlayerAndBossToPhaseStart_Continuously()
        {
            yield return Load(256);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            Assert.IsTrue(enc.IsBossDuel);
            double O = enc.StartBeat;
            var cues = enc.Cues.ToList();
            // 1단계 + 칼 겨루기까지 PERFECT, 2단계 시작(O+30) 자리를 테스트가 따로 적어 둔다
            for (int i = 0; i < 13; i++) yield return Hit(cues[i], 0.0);
            yield return AdvanceTo(boot.clock.TimeOfBeat(O + 29.9));
            yield return Hit(cues[13], 0.0);   // L30 (칼 겨루기 마지막, 자리 기록 박)
            var phaseP = boot.player.transform.position;
            var phaseE = dir.boss.transform.position;
            for (int i = 14; i < 15; i++) yield return Hit(cues[i], 0.0);
            // 2단계: 내 공격(칼 부딪힘)은 NORMAL로 쳐서 뒤로 밀려나게(B13.1), 나머지는 PERFECT, 마지막 적 공격에서 FAIL
            var clash = cues.Skip(15).Where(c => Step(c).Phase == BossDuel.PhaseClash).ToList();
            var failCue = clash.Last(c => Step(c).Kind == CombatStepKind.Attack);
            foreach (var c in clash)
            {
                if (c == failCue) break;
                bool strike = Step(c).Kind == CombatStepKind.Strike || Step(c).Kind == CombatStepKind.Lock;
                yield return Hit(c, strike ? 0.13 : 0.0);
            }
            Assert.IsNotNull(failCue, "2단계 FAIL 원");
            yield return AdvanceTo(boot.clock.TimeOfBeat(failCue.TargetBeat) - 0.1);
            yield return Hit(failCue, null);
            Assert.IsTrue(dir.Rewinding, "되감기 시작");
            Assert.AreEqual(RewindKind.Boss, dir.LastRewind);
            Assert.IsTrue(dir.MotionRewinding, "자리도 되감는 중");
            var fromP = boot.player.transform.position;
            var fromE = dir.boss.transform.position;
            float awayP = Vector3.Distance(Flat(fromP), Flat(dir.RewindTargetPlayer));
            float awayE = Vector3.Distance(Flat(fromE), Flat(dir.RewindTargetEnemy));
            Debug.Log($"[R15] 되감기 전 플레이어 {fromP} 무사 {fromE} → 목표 {dir.RewindTargetPlayer} {dir.RewindTargetEnemy} (거리 {awayP:0.00} / {awayE:0.00}), 테스트가 적은 단계 시작 {phaseP} {phaseE}");
            Assert.Greater(awayP + awayE, 0.3f, "FAIL 시점 자리가 단계 시작과 달라야 의미 있는 검사");
            // 되감기 동안: 프레임마다 조금씩 (순간 이동 아님), 거리가 줄어듦
            var lastP = fromP; var lastE = fromE;
            float maxJumpP = 0f, maxJumpE = 0f;
            float midDist = -1f;
            int steps = 20;
            for (int s = 1; s <= steps; s++)
            {
                real += GameConfig.RewindSeconds / steps;
                yield return null;
                var p = boot.player.transform.position; var e = dir.boss.transform.position;
                if (s < steps) { maxJumpP = Mathf.Max(maxJumpP, Vector3.Distance(Flat(p), Flat(lastP))); maxJumpE = Mathf.Max(maxJumpE, Vector3.Distance(Flat(e), Flat(lastE))); }
                if (s == steps / 2) midDist = Vector3.Distance(Flat(p), Flat(dir.RewindTargetPlayer)) + Vector3.Distance(Flat(e), Flat(dir.RewindTargetEnemy));
                lastP = p; lastE = e;
            }
            real += 0.05f;
            yield return null;
            yield return null;
            Assert.IsFalse(dir.Rewinding);
            Assert.IsFalse(dir.MotionRewinding);
            Assert.AreEqual(O + BossDuel.PhaseStart(BossDuel.PhaseClash), boot.clock.CurrentBeat, 0.05, "2단계 처음으로");
            var endP = boot.player.transform.position; var endE = dir.boss.transform.position;
            Debug.Log($"[R15] 되감기 뒤 플레이어 {endP} 무사 {endE}, 프레임당 최대 이동 {maxJumpP:0.000}/{maxJumpE:0.000}, 가운데 남은 거리 {midDist:0.00}");
            Assert.Less(Vector3.Distance(Flat(endP), Flat(dir.RewindTargetPlayer)), 0.1f, "플레이어가 단계 시작 자리로");
            Assert.Less(Vector3.Distance(Flat(endE), Flat(dir.RewindTargetEnemy)), 0.15f, "무사가 단계 시작 자리로");
            Assert.Less(Vector3.Distance(Flat(endP), Flat(phaseP)), 0.1f, "테스트가 적은 단계 시작 자리와도 일치");
            Assert.Less(Vector3.Distance(Flat(endE), Flat(phaseE)), 0.15f, "무사도");
            Assert.Less(maxJumpP, Mathf.Max(0.05f, awayP * 0.35f), "플레이어 순간 이동 없음");
            Assert.Less(maxJumpE, Mathf.Max(0.05f, awayE * 0.35f), "무사 순간 이동 없음");
            Assert.Less(midDist, (awayP + awayE) * 0.85f, "가운데쯤에는 이미 거꾸로 이동 중");
            Assert.Greater(midDist, (awayP + awayE) * 0.15f, "한 번에 건너뛰지 않음");
        }

        /// <summary>
        /// 윈드밀 FAIL 되감기 (4장 포위): 플레이어와 적 5명(뛰어든 적 포함)이 3단계 첫 원 1마디 앞 박의 자리로 프레임마다 조금씩 돌아감.
        /// 테스트가 그 박을 지나는 프레임에 직접 잰 자리와도 비교한다.
        /// </summary>
        [UnityTest]
        public IEnumerator R15_WindmillFail_RewindsPlayerAndEnemiesToCheckpoint()
        {
            yield return Load(181);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            Assert.IsTrue(enc.Event.IsSurround);
            var cues = enc.Cues.ToList();
            var wind = cues.Where(c => Step(c).Phase == 3).ToList();
            double cpBeat = wind[0].TargetBeat - GameConfig.RewindLeadBeats;
            // 1·2단계를 자동으로 치며 한 프레임씩: 코드가 되감기 자리를 적는 프레임에 테스트도 자리를 잰다
            Vector3 cpP = Vector3.zero; List<Vector3> cpE = null;
            for (int f = 0; f < 6000 && boot.clock.CurrentBeat < wind[0].TargetBeat - 0.3; f++)
            {
                var before = dir.RecordedCheckpoints;
                now += 0.01;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && Step(c).Phase != 3 && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                yield return null;
                if (cpE == null && dir.RecordedCheckpoints > before)
                {
                    cpP = boot.player.transform.position;
                    cpE = enc.Actors.Select(a => a != null ? a.transform.position : Vector3.zero).ToList();
                }
            }
            Assert.IsNotNull(cpE, "3단계 1마디 앞 자리 기록");
            Assert.IsNotNull(dir.LastCheckpointActors, "코드가 적 5명 자리를 적음");
            yield return Hit(wind[0], 0.0);
            yield return Hit(wind[1], null);   // 윈드밀 FAIL
            Assert.IsTrue(dir.Rewinding);
            Assert.AreEqual(RewindKind.Windmill, dir.LastRewind);
            Assert.IsTrue(dir.MotionRewinding, "자리도 되감는 중");
            var alive = Enumerable.Range(0, enc.Actors.Length).Where(i => enc.Actors[i] != null && !enc.Actors[i].Defeated).ToList();
            var lastP = boot.player.transform.position;
            var lastE = alive.Select(i => enc.Actors[i].transform.position).ToList();
            float away = alive.Select((i, n) => Vector3.Distance(Flat(lastE[n]), Flat(dir.LastCheckpointActors[i]))).DefaultIfEmpty(0f).Max();
            float maxJumpP = 0f, maxJumpE = 0f;
            for (int s = 1; s <= 20; s++)
            {
                real += GameConfig.RewindSeconds / 20f;
                yield return null;
                var p = boot.player.transform.position;
                var e = alive.Select(i => enc.Actors[i].transform.position).ToList();
                if (s < 20)
                {
                    maxJumpP = Mathf.Max(maxJumpP, Vector3.Distance(Flat(p), Flat(lastP)));
                    for (int n = 0; n < e.Count; n++) maxJumpE = Mathf.Max(maxJumpE, Vector3.Distance(Flat(e[n]), Flat(lastE[n])));
                }
                lastP = p; lastE = e;
            }
            real += 0.05f;
            yield return null;
            yield return null;
            Assert.IsFalse(dir.Rewinding);
            Assert.IsFalse(dir.MotionRewinding);
            float dP = Vector3.Distance(Flat(boot.player.transform.position), Flat(cpP));
            float dE = 0f;
            foreach (var i in alive) dE = Mathf.Max(dE, Vector3.Distance(Flat(enc.Actors[i].transform.position), Flat(cpE[i])));
            Debug.Log($"[R15] 윈드밀 되감기: 되감기 전 적 최대 거리 {away:0.00} m → 뒤 플레이어 차이 {dP:0.000} m, 적 {alive.Count}명 최대 차이 {dE:0.000} m, 프레임당 최대 {maxJumpP:0.000}/{maxJumpE:0.000} m");
            Assert.Less(dP, 0.1f, "플레이어가 3단계 1마디 앞 자리로");
            Assert.Less(dE, 0.3f, "적 5명(뛰어든 적 포함)도 그 자리로");
            Assert.Less(maxJumpP, 0.3f, "플레이어 순간 이동 없음");
            Assert.Less(maxJumpE, Mathf.Max(0.3f, away * 0.35f), "적 순간 이동 없음");
        }

        /// <summary>
        /// 챕터 되감기(체력 0, 2챕터 92박 전투 중): 플레이어는 경로를 따라 이어 뒤로(B24 테스트), 끝나면 무대를 챕터 시작으로 다시 세운다.
        /// 적은 챕터 시작 때 자리로 돌아가고, 자리가 바뀌는 순간(되감기 끝) 그 적은 화면 밖이거나 40 m 넘게 멀다(눈에 띄는 순간 이동 없음).
        /// </summary>
        [UnityTest]
        public IEnumerator R15_ChapterRewind_EnemiesBackToChapterStart_NotSeenJumping()
        {
            double chStart = MapLayout.SectionOf(92).StartBeat;
            yield return Load(chStart);
            for (int i = 0; i < 3; i++) { now += 0.01; yield return null; }
            var actors = Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None).ToList();
            var start = actors.ToDictionary(a => a, a => a.transform.position);
            yield return UntilCombat();
            Assert.AreEqual(92, dir.CurrentEncounter.Event.runBeat, "2챕터 92박 전투");
            for (int i = 0; i < 100; i++) { now += 0.01; yield return null; }
            var mid = actors.ToDictionary(a => a, a => a.transform.position);
            int movedBefore = actors.Count(a => Vector3.Distance(Flat(mid[a]), Flat(start[a])) > 1f);
            dir.Health.Damage(); dir.Health.Damage(); dir.Health.Damage();
            yield return null;
            Assert.IsTrue(dir.Rewinding);
            Assert.AreEqual(RewindKind.Chapter, dir.LastRewind);
            var cam = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            Dictionary<EnemyActor, Vector3> last = actors.ToDictionary(a => a, a => a.transform.position);
            int seenJumps = 0;
            for (int s = 1; s <= 24; s++)
            {
                real += GameConfig.RewindSeconds / 20f;
                yield return null;
                foreach (var a in actors)
                {
                    var p = a.transform.position;
                    if (Vector3.Distance(Flat(p), Flat(last[a])) > 1.5f)
                    {
                        var vp = cam.WorldToViewportPoint(last[a] + Vector3.up);
                        bool onScreen = vp.z > 0f && vp.z < 40f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f && a.gameObject.activeInHierarchy;
                        if (onScreen) { seenJumps++; Debug.Log($"[R15] 챕터 되감기: {a.name} 화면 안 {vp.z:0.0} m에서 {Vector3.Distance(p, last[a]):0.0} m 이동"); }
                    }
                    last[a] = p;
                }
            }
            Assert.IsFalse(dir.Rewinding);
            for (int i = 0; i < 3; i++) { now += 0.01; yield return null; }
            float maxBack = actors.Max(a => Vector3.Distance(Flat(a.transform.position), Flat(start[a])));
            Debug.Log($"[R15] 챕터 되감기: 전투 중 자리를 옮긴 적 {movedBefore}명, 되감은 뒤 챕터 시작 자리와 최대 차이 {maxBack:0.000} m, 화면 안 순간 이동 {seenJumps}");
            Assert.Greater(movedBefore, 0, "전투 중 자리를 옮긴 적이 있어야 의미 있는 검사");
            Assert.Less(maxBack, 0.3f, "적이 챕터 시작 때 자리로");
            Assert.AreEqual(0, seenJumps, "화면 안에서 보이는 순간 이동 없음");
        }

        static CombatStep Step(JudgeCue c) => (CombatStep)c.Tag;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        [UnityTest]
        public IEnumerator R16_CombatStill_NotRunning_ThenRunAfterCombat()
        {
            yield return Load(256);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            double O = enc.StartBeat;
            var cues = enc.Cues.ToList();
            // 등장·돌진이 끝나고 첫 원(O+6) 전: 나는 서서 기다림
            yield return AdvanceTo(boot.clock.TimeOfBeat(O + 5.5));
            var st = body.animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[R16] O+5.5: 상태 {body.LastState}, 이동 {body.MoveSpeed:0.00} m/s, 대기 {body.InCombatStance}");
            Assert.Less(body.MoveSpeed, GameConfig.Body.CombatStillSpeed, "멈춰 있음");
            Assert.AreEqual(BodyMoves.CombatIdle, body.LastState, "달리기 대신 대기 자세");
            Assert.IsFalse(st.IsName(BodyMoves.Run) && !body.animator.IsInTransition(0), "Animator가 달리기 상태가 아님");
            // 전투 중 판정마다: 이동이 끝나고 멈추면 다시 대기 자세 (처형·피격 같은 동작 중은 제외)
            int stillFrames = 0, runWhileStill = 0;
            for (int i = 0; i < 13; i++)
            {
                yield return Hit(cues[i], 0.0);
                for (int f = 0; f < 60; f++)
                {
                    now += 0.01; yield return null;
                    if (body.MoveSpeed < GameConfig.Body.CombatStillSpeed * 0.5f && !body.animator.IsInTransition(0) && body.ActionBlend <= 0f && f > 45)
                    {
                        stillFrames++;
                        if (body.animator.GetCurrentAnimatorStateInfo(0).IsName(BodyMoves.Run)) runWhileStill++;
                    }
                }
            }
            Debug.Log($"[R16] 전투 중 멈춘 프레임 {stillFrames}, 그중 달리기 {runWhileStill}");
            Assert.Greater(stillFrames, 20);
            Assert.AreEqual(0, runWhileStill, "멈춰 싸울 때 제자리 달리기 없음");
        }

        [UnityTest]
        public IEnumerator R16_NormalEnemy_StanceThenRunCrossfadeAfterCombat()
        {
            var first = StageChart.Load().events.Where(e => e.IsDuel).OrderBy(e => e.runBeat).First();   // 멈춰 싸우는 일반 적 (92박)
            yield return Load(first.runBeat - 6);
            yield return UntilCombat();
            bool sawIdle = false;
            for (int i = 0; i < 3000 && dir.State == StageState.Combat; i++)
            {
                now += 0.01;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                yield return null;
                if (body.LastState == BodyMoves.CombatIdle) sawIdle = true;
            }
            Assert.AreNotEqual(StageState.Combat, dir.State, "전투 끝");
            for (int i = 0; i < 200 && body.LastState != BodyMoves.Run; i++) { now += 0.01; yield return null; }
            for (int i = 0; i < 30; i++) { now += 0.01; yield return null; }
            Debug.Log($"[R16] 일반 적 {first.runBeat}박: 대기 자세 봄 {sawIdle}, 전투 뒤 상태 {body.LastState}");
            Assert.AreNotEqual(BodyMoves.CombatIdle, body.LastState, "전투가 끝나면 대기 자세에서 나옴");
            Assert.IsFalse(body.animator.GetCurrentAnimatorStateInfo(0).IsName(BodyMoves.CombatIdle), "달리기로 돌아옴");
        }
    }
}
