using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>판정 하나 = 한 걸음 (GDD-beta B13.1 표). 순수 함수 검사.</summary>
    public class B32_StepMotionTests
    {
        static readonly Vector3 F = Vector3.forward, R = Vector3.right;

        static (Vector3 p, Vector3 e) M(CombatStepKind k, ArrowKey key, Judgement r, int duck = 1)
        {
            StageDirector.StepMotion(new CombatStep { Kind = k, Key = key }, r, F, R, duck, out var p, out var e);
            return (p, e);
        }

        static void Eq(Vector3 want, Vector3 got, string why) => Assert.Less(Vector3.Distance(want, got), 1e-4f, $"{why}: {want} ≠ {got}");

        [Test]
        public void B32_Config_MatchGddBetaB13()
        {
            Assert.AreEqual(5f, GameConfig.CombatArenaRadius);
            Assert.AreEqual(2.2f, GameConfig.FightDistanceSword);
            Assert.AreEqual(2.6f, GameConfig.FightDistanceSpear);
            Assert.AreEqual(5f, GameConfig.EnemySpawnMeters);
            Assert.AreEqual(0.35f, GameConfig.CombatMoveSeconds);
            Assert.AreEqual(0.25f, GameConfig.CombatFaceSmoothSeconds);
            Assert.AreEqual(240f, GameConfig.CombatFaceMaxDegPerSec);
            Assert.AreEqual(20f, GameConfig.CombatCircleDegPerSec);
        }

        [Test]
        public void B32_Parry_Up()
        {
            var a = M(CombatStepKind.Attack, ArrowKey.Up, Judgement.Perfect);
            Eq(F * 0.5f, a.p, "PERFECT 반 걸음 전진"); Eq(F * 0.5f, a.e, "적은 그만큼 밀림");
            var b = M(CombatStepKind.Attack, ArrowKey.Up, Judgement.Normal);
            Eq(-F * 0.5f, b.p, "NORMAL 0.5 m 밀려남"); Eq(-F * 0.5f, b.e, "적이 따라옴");
            var c = M(CombatStepKind.Attack, ArrowKey.Up, Judgement.Fail);
            Eq(-F * 0.4f, c.p, "FAIL 맞고 0.4 m 밀려남"); Eq(-F * 0.4f, c.e, "적이 따라옴");
        }

        [Test]
        public void B32_Dodge_LeftRight()
        {
            var l = M(CombatStepKind.Attack, ArrowKey.Left, Judgement.Perfect);
            Eq(-R * 1.2f, l.p, "← PERFECT 왼쪽 1.2 m"); Eq(R * 0.6f, l.e, "적은 반대로 0.6 m 지나침");
            var r = M(CombatStepKind.Attack, ArrowKey.Right, Judgement.Normal);
            Eq(R * 0.5f, r.p, "→ NORMAL 오른쪽 0.5 m"); Eq(-R * 0.2f, r.e, "적 0.2 m");
            // 각도: 싸움 거리 2.2 m에서 PERFECT 피하기는 약 40° 돈다
            var p = l.p; var e = F * 2.2f + l.e;
            float ang = Vector3.Angle(F, (e - p).normalized);
            Assert.That(ang, Is.InRange(35f, 45f), $"약 40° ({ang:0.0}°)");
        }

        [Test]
        public void B32_Duck_And_Strike()
        {
            var d = M(CombatStepKind.Attack, ArrowKey.Down, Judgement.Perfect, 1);
            Eq(F * 0.5f + R * 0.6f, d.p, "↓ PERFECT 앞 0.5 + 옆 0.6 파고듦");
            var d2 = M(CombatStepKind.Attack, ArrowKey.Down, Judgement.Perfect, -1);
            Eq(F * 0.5f - R * 0.6f, d2.p, "다음에는 반대쪽");
            Eq(Vector3.zero, M(CombatStepKind.Attack, ArrowKey.Down, Judgement.Normal).p, "NORMAL 제자리");
            var s = M(CombatStepKind.Strike, ArrowKey.Right, Judgement.Perfect);
            Eq(F * 1f, s.p, "내 공격 PERFECT 1 m 밀어붙임"); Eq(F * 1f, s.e, "적 1 m 밀림");
            var n = M(CombatStepKind.Strike, ArrowKey.Right, Judgement.Normal);
            Eq(-F * 0.3f, n.p, "NORMAL 0.3 m 밀려남"); Eq(-F * 0.3f, n.e, "적이 밀고 들어옴");
            var lk = M(CombatStepKind.Lock, ArrowKey.Up, Judgement.Perfect);
            Eq(F * 1f, lk.p, "칼 겨루기도 내 공격과 같음");
            Eq(Vector3.zero, M(CombatStepKind.Strike, ArrowKey.Up, Judgement.Fail).p, "헛침은 되감기가 대신");
            Eq(Vector3.zero, M(CombatStepKind.Finisher, ArrowKey.Up, Judgement.Perfect).p, "마무리는 처형 연출");
        }
    }
}
