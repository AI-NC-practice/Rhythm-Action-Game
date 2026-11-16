using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>전투 소리 실제 녹음 (GDD-beta B17.5, 작업 B48): 녹음 파일, 출처, 연출별 소리, 박 맞춤 값.</summary>
    public class B48_CombatSoundTests
    {
        static readonly SfxKind[] Recorded =
        {
            SfxKind.SwordWhoosh, SfxKind.SwordSwish, SfxKind.ParryClang, SfxKind.BlockClang,
            SfxKind.Slash, SfxKind.Punch, SfxKind.Kick, SfxKind.BodyFall,
        };

        [Test]
        public void B48_RecordedFiles_ShortMono16bit_AndCredited()
        {
            string credits = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "docs/CREDITS.md"));
            StringAssert.Contains("Mixkit", credits);
            foreach (var k in Recorded)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), $"Assets/Audio/Sfx/Rec_{k}.wav");
                Assert.IsTrue(File.Exists(path), $"녹음 파일 Rec_{k}.wav");
                var b = File.ReadAllBytes(path);
                Assert.AreEqual(1, System.BitConverter.ToInt16(b, 22), $"{k} 모노");
                Assert.AreEqual(44100, System.BitConverter.ToInt32(b, 24), $"{k} 44.1 kHz");
                Assert.AreEqual(16, System.BitConverter.ToInt16(b, 34), $"{k} 16 bit");
                float seconds = System.BitConverter.ToInt32(b, 40) / 2f / 44100f;
                Assert.That(seconds, Is.InRange(0.2f, 1.05f), $"{k} 짧게 ({seconds:0.00}초)");
                StringAssert.Contains($"Rec_{k}", credits, "출처 표기");
                Assert.AreNotEqual(k, SfxSynth.Fallback(k), "녹음이 없을 때 합성 대체음");
            }
            foreach (var n in new[] { "2792", "1506", "2789", "2795", "2788", "2153", "2163", "2498" })
                StringAssert.Contains(n, credits, "Mixkit 번호");
        }

        static List<SfxKind> Sounds(string preset)
        {
            var l = new List<SfxKind>();
            FxLooks.Sounds(preset, l, out _, out _);
            return l;
        }

        [Test]
        public void B48_Presets_UseRealSwordAndHitSounds()
        {
            CollectionAssert.Contains(Sounds("ParryPerfect"), SfxKind.ParryClang, "막기 PERFECT = 칼 \"챙\"");
            CollectionAssert.Contains(Sounds("DefendNormal"), SfxKind.BlockClang);
            CollectionAssert.Contains(Sounds("StrikePerfect"), SfxKind.SwordSwish, "내 공격 휘두름");
            CollectionAssert.Contains(Sounds("StrikePerfect"), SfxKind.ParryClang);
            CollectionAssert.Contains(Sounds("Hit"), SfxKind.Punch, "맞음 = \"퍽\"");
            CollectionAssert.Contains(Sounds("Execution"), SfxKind.Slash, "처형 = 베임");
            CollectionAssert.Contains(Sounds("Execution"), SfxKind.BodyFall, "쓰러짐");
            CollectionAssert.Contains(Sounds("WindmillFinish"), SfxKind.BodyFall);
            CollectionAssert.Contains(Sounds("KickHit"), SfxKind.Kick);
            CollectionAssert.Contains(Sounds("BloodHit"), SfxKind.Slash);
            CollectionAssert.DoesNotContain(Sounds("AttackWarn"), SfxKind.Whoosh, "휘두름은 판정 반 박 전에 따로 (박 맞춤)");
            CollectionAssert.DoesNotContain(Sounds("SimultaneousPerfect"), SfxKind.Whoosh);
            foreach (var p in GameConfig.Fx.All)
                foreach (var k in Sounds(p.Name))
                    Assert.IsFalse(k == SfxKind.MetalClang || k == SfxKind.DullClank, $"{p.Name}: 합성 금속음 대신 녹음");
        }

        [Test]
        public void B48_SwingTiming_HalfBeatBefore_EnemyAttacksOnly()
        {
            Assert.AreEqual(0.5, GameConfig.CombatSound.SwingLeadBeats, 1e-9, "판정 반 박 전 \"쉭\"");
            Assert.IsTrue(CombatSwingSounds.IsEnemySwing(new JudgeCue { Tag = new CombatStep { Kind = CombatStepKind.Attack } }));
            Assert.IsFalse(CombatSwingSounds.IsEnemySwing(new JudgeCue { Tag = new CombatStep { Kind = CombatStepKind.Strike } }), "내 공격은 누를 때 휘두름");
            Assert.IsFalse(CombatSwingSounds.IsEnemySwing(new JudgeCue { Tag = new CombatStep { Kind = CombatStepKind.Finisher } }));
            Assert.IsFalse(CombatSwingSounds.IsEnemySwing(new JudgeCue { Tag = new CombatStep { Kind = CombatStepKind.Windmill } }));
            Assert.IsFalse(CombatSwingSounds.IsEnemySwing(new JudgeCue { Tag = new RhythmNote() }));
        }
    }
}
