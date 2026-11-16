using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 적 모델 + 전투 애니메이션 (작업 T14). Game 씬의 실제 차트(92박 칼 포졸, 128박 지붕 창 포졸)에서 시간을 주입해 검사한다.
    /// </summary>
    public class T14_EnemyCombatPlayTests
    {
        double now;
        float realTime;
        GameBootstrap boot;
        StageDirector dir;
        PlayerBody body;

        IEnumerator Load(double startRunBeat)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            body = Object.FindAnyObjectByType<PlayerBody>();
            now = 0; realTime = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.slowMotion.RealTime = () => realTime;
            boot.StartRun(startRunBeat);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel();
            Time.timeScale = 1f;
        }

        IEnumerator AdvanceTo(double t, double step = 0.02)
        {
            while (now < t - 1e-9) { now = System.Math.Min(t, now + step); yield return null; }
        }

        IEnumerator UntilCombat()
        {
            for (int i = 0; i < 2000 && dir.State != StageState.Combat; i++) { now += 0.02; yield return null; }
            Assert.AreEqual(StageState.Combat, dir.State);
        }

        IEnumerator Press(int i, double delay = 0.0)
        {
            var cue = dir.CurrentEncounter.Cues[i];
            yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) - 0.03);
            now = boot.clock.TimeOfBeat(cue.TargetBeat) + delay;
            boot.judge.Submit(cue.Key, now);
        }

        static bool IsUnder(Transform t, Transform parent)
        {
            for (var p = t; p != null; p = p.parent) if (p == parent) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator T14_Enemy_Model_WeaponInHand_ImpactOnTargetBeat_CrossfadeBetweenAttacks()
        {
            yield return Load(89);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var actor = enc.Actors[0];
            var ea = actor.anim;
            Assert.IsNotNull(ea, "EnemyAnimator");
            Assert.IsTrue(ea.animator.isHuman, "Humanoid 모델");
            Assert.IsNull(actor.transform.Find("Body"), "캡슐 없음");
            var hand = ea.animator.GetBoneTransform(HumanBodyBones.RightHand);
            Assert.IsTrue(IsUnder(actor.weaponVisual, hand), "무기가 오른손 뼈에 붙음");
            Assert.IsTrue(IsUnder(actor.weaponTip, actor.weaponVisual), "무기 끝이 무기에 붙음");

            for (int i = 0; i < 3; i++)
            {
                var cue = enc.Cues[i];
                var step = (CombatStep)cue.Tag;
                string state = EnemyMoves.AttackState(step.Attack);
                // 목표 박 바로 전까지 진행 → 목표 박에 정확히 맞춘 프레임
                yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) - 0.02);
                now = boot.clock.TimeOfBeat(cue.TargetBeat);
                yield return null;
                Assert.AreEqual(state, ea.CurrentState, $"공격 {i}: 상태");
                float hit = ea.HitSecondsOf(state);
                Assert.Greater(hit, 0.1f, "타격 순간");
                Assert.AreEqual(hit, ea.AttackClipTime, 0.02f, $"공격 {i}: 목표 박 = 클립의 타격 순간");
                var info = ea.animator.GetCurrentAnimatorStateInfo(0);
                if (!ea.animator.IsInTransition(0))
                {
                    Assert.IsTrue(info.IsName(state));
                    Assert.AreEqual(hit, info.normalizedTime * ea.LengthOf(state), 0.03f, $"공격 {i}: Animator 시간 = 타격 순간");
                }
                Assert.Less(Vector3.Distance(cue.WorldPoint.Value, actor.weaponTip.position), 0.05f, "판정 원이 손에 든 무기 끝을 따라감");
                boot.judge.Submit(cue.Key, now);
                yield return null;
                // 다음 공격: 크로스페이드로 이어짐
                if (i < 2)
                {
                    var next = (CombatStep)enc.Cues[i + 1].Tag;
                    bool sawFade = false;
                    for (int f = 0; f < 60 && !sawFade && now < boot.clock.TimeOfBeat(enc.Cues[i + 1].TargetBeat) - 0.1; f++)
                    {
                        now += 0.01; yield return null;                       // 히트 스톱이 풀리면 다음 공격이 시작된다
                        if (ea.CurrentState == EnemyMoves.AttackState(next.Attack) && ea.animator.IsInTransition(0)) sawFade = true;
                    }
                    Assert.AreEqual(EnemyMoves.AttackState(next.Attack), ea.CurrentState, "다음 공격 상태");
                    if (step.Attack != next.Attack) Assert.IsTrue(sawFade, "다른 공격으로 0.12초 크로스페이드");
                }
            }
        }

        [UnityTest]
        public IEnumerator T14_Disarm_PlayerAndEnemyStartSameBeat_Aligned()
        {
            yield return Load(89);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var actor = enc.Actors[0];
            for (int i = 0; i < 4; i++) { yield return Press(i); yield return null; }
            yield return Press(4);                                    // 마무리 PERFECT (같은 프레임에서 바로 검사)
            Assert.AreEqual(ExecutionKind.DisarmSlash, enc.Execution);
            Assert.AreEqual(EnemyMoves.DefeatState(ExecutionKind.DisarmSlash), actor.anim.CurrentState);
            Assert.AreEqual(BodyMoves.ExecState(ExecutionKind.DisarmSlash), body.LastState);
            Assert.AreEqual(actor.anim.StateStartBeat, body.ExecStartBeat, 1e-9, "플레이어·적 클립이 같은 박에 시작");
            var toPlayer = boot.player.transform.position - actor.transform.position; toPlayer.y = 0;
            Assert.Less(Vector3.Angle(actor.transform.forward, toPlayer), 2f, "적이 플레이어를 봄");
            Assert.AreEqual(GameConfig.EnemyBody.ExecutionDistance, toPlayer.magnitude, 0.02f, "처형 거리");
            Assert.IsFalse(actor.weaponVisual.gameObject.activeSelf, "칼을 빼앗김");
            yield return null; yield return null;
            Assert.IsTrue(body.animator.GetCurrentAnimatorStateInfo(0).IsName(BodyMoves.ExecState(ExecutionKind.DisarmSlash))
                || body.animator.GetNextAnimatorStateInfo(0).IsName(BodyMoves.ExecState(ExecutionKind.DisarmSlash)), "플레이어 칼 뺏기 동작");
        }

        [UnityTest]
        public IEnumerator T14_RoofKick_EnemyFallsOffRoof_PlayerKicks()
        {
            yield return Load(126);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var actor = enc.Actors[0];
            for (int i = 0; i < 4; i++) { yield return Press(i); yield return null; }
            yield return Press(4);
            Assert.AreEqual(ExecutionKind.KickOffRoof, enc.Execution);
            Assert.AreEqual(BodyMoves.ExecState(ExecutionKind.KickOffRoof), body.LastState, "플레이어 발차기");
            Assert.AreEqual(EnemyMoves.DefeatState(ExecutionKind.KickOffRoof), actor.anim.CurrentState, "적 떨어지는 동작");
            Assert.AreEqual(actor.anim.StateStartBeat, body.ExecStartBeat, 1e-9, "같은 박에 시작");
            float y0 = actor.transform.position.y;
            realTime += 0.5f; dir.slowMotion.Tick();                       // 처형 슬로모션 끝
            yield return new WaitForSecondsRealtime(1.1f);                 // 떨어지는 궤적은 실제 시간으로 진행
            Assert.Less(actor.transform.position.y, y0 - 2f, "지붕 아래로 떨어짐");
        }

        [UnityTest]
        public IEnumerator T14_HitPushback_Windmill_PlayerHit_States()
        {
            yield return Load(89);
            yield return UntilCombat();
            var actor = dir.CurrentEncounter.Actors[0];
            actor.Pushback(actor.transform.position - boot.player.transform.position, 0.5f, 0.3f);
            Assert.AreEqual(EnemyMoves.Hit, actor.anim.CurrentState, "밀림 = 피격 동작");
            var start = actor.transform.position;
            actor.Defeat(ExecutionKind.Windmill, actor.transform.position - boot.player.transform.position);
            Assert.AreEqual(EnemyMoves.DefeatState(ExecutionKind.Windmill), actor.anim.CurrentState, "윈드밀에 날아감 동작");
            yield return new WaitForSecondsRealtime(0.6f);                 // 날아가는 궤적은 실제 시간으로 진행
            Assert.Greater(Vector3.Distance(start, actor.transform.position), 1f, "바깥으로 날아감");

            // 내가 맞음: 다음 공격 무입력 FAIL → 피격 동작
            yield return Load(89);
            yield return UntilCombat();
            var cue = dir.CurrentEncounter.Cues[0];
            yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) + 0.3);
            Assert.AreEqual(BodyMoves.Hit, body.LastState, "플레이어 피격 동작");
        }
    }
}
