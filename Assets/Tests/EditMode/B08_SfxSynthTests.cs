using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>합성 효과음과 장단 반주 (GDD-beta B7, 제안서 2장 소리 열).</summary>
    public class B08_SfxSynthTests
    {
        static SfxKind[] All => (SfxKind[])System.Enum.GetValues(typeof(SfxKind));

        static float ZeroCrossPerSecond(float[] d, float seconds)
        {
            int n = 0;
            int len = Mathf.Min(d.Length, Mathf.RoundToInt(seconds * SfxSynth.SampleRate));
            for (int i = 1; i < len; i++) if ((d[i - 1] < 0) != (d[i] < 0)) n++;
            return n / seconds;
        }

        [Test]
        public void B08_AllKinds_Synthesize_NormalizedAndFadeOut()
        {
            CollectionAssert.IsSupersetOf(All.Select(k => k.ToString()).ToArray(),
                new[] { "MetalClang", "LowBoom", "DullClank", "BodyHit", "Whoosh", "DustPuff", "EarRing", "Buk", "Jing", "UiTick" }, "GDD-beta B7 목록");
            foreach (var k in All)
            {
                var d = SfxSynth.Create(k);
                Assert.AreEqual(Mathf.RoundToInt(SfxSynth.Seconds(k) * SfxSynth.SampleRate), d.Length, k.ToString());
                Assert.AreEqual(0.9f, d.Max(x => Mathf.Abs(x)), 0.01f, k + " 최고값 0.9");
                Assert.Less(Mathf.Abs(d[d.Length - 1]), 0.01f, k + " 끝이 0 (딸깍 방지)");
                Assert.IsFalse(d.Any(float.IsNaN));
            }
        }

        [Test]
        public void B08_Timbre_LowIsLow_MetalIsHigh_JingRings()
        {
            float boom = ZeroCrossPerSecond(SfxSynth.Create(SfxKind.LowBoom), 0.2f);
            float clang = ZeroCrossPerSecond(SfxSynth.Create(SfxKind.MetalClang), 0.2f);
            float buk = ZeroCrossPerSecond(SfxSynth.Create(SfxKind.Buk), 0.2f);
            Assert.Less(boom, 300f, "저음 '쿵'은 낮은 소리");
            Assert.Less(buk, 300f, "북은 낮은 소리");
            Assert.Greater(clang, 2000f, "금속 '챙'은 높은 소리");
            Assert.Greater(SfxSynth.Seconds(SfxKind.Jing), 1.5f, "징은 길게 울림");
        }

        [Test]
        public void B08_Wav_Header16BitMono44k()
        {
            var w = SfxSynth.ToWav(new[] { 0f, 0.5f, -0.5f, 1f });
            Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(w, 0, 4));
            Assert.AreEqual("WAVE", System.Text.Encoding.ASCII.GetString(w, 8, 4));
            Assert.AreEqual(1, System.BitConverter.ToInt16(w, 20), "PCM");
            Assert.AreEqual(1, System.BitConverter.ToInt16(w, 22), "모노");
            Assert.AreEqual(44100, System.BitConverter.ToInt32(w, 24));
            Assert.AreEqual(16, System.BitConverter.ToInt16(w, 34));
            Assert.AreEqual(8, System.BitConverter.ToInt32(w, 40));
            Assert.AreEqual(44 + 8, w.Length);
            Assert.AreEqual(32767, System.BitConverter.ToInt16(w, 50));
        }

        [Test]
        public void B08_WavFiles_InAssetsAudioSfx()
        {
            foreach (var k in All)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Sfx/Sfx_{k}.wav");
                Assert.IsNotNull(clip, $"Assets/Audio/Sfx/Sfx_{k}.wav (SfxSynthTool이 생성)");
                Assert.AreEqual(1, clip.channels);
                Assert.AreEqual(SfxSynth.Seconds(k), clip.length, 0.01f);
            }
        }

        [Test]
        public void B08_SoundsPerSituation_MatchProposalColumn()
        {
            foreach (var p in GameConfig.Fx.All)
            {
                var s = new List<SfxKind>();
                FxLooks.Sounds(p.Name, s, out _, out _);
                Assert.Greater(s.Count, 0, p.Name + " 소리 있음");
            }
            void Check(FxPreset p, SfxKind[] expect, float db, float sec)
            {
                var s = new List<SfxKind>();
                FxLooks.Sounds(p.Name, s, out float d, out float t);
                CollectionAssert.IsSupersetOf(s, expect, p.Name);
                Assert.AreEqual(db, d, 1e-4f, p.Name + " 덕킹 dB");
                Assert.AreEqual(sec, t, 1e-4f, p.Name + " 덕킹 초");
            }
            Check(GameConfig.Fx.AttackWarn, new[] { SfxKind.Glint }, 0, 0);                                         // A "칭"
            Check(GameConfig.Fx.ParryPerfect, new[] { SfxKind.MetalClang, SfxKind.LowBoom }, -4f, 0.15f);           // B
            Check(GameConfig.Fx.DefendNormal, new[] { SfxKind.DullClank }, 0, 0);                                   // C "탕"
            Check(GameConfig.Fx.Hit, new[] { SfxKind.BodyHit, SfxKind.LowBoom, SfxKind.EarRing }, -8f, 0.3f);       // D
            Check(GameConfig.Fx.Execution, new[] { SfxKind.BodyHit, SfxKind.LowBoom, SfxKind.Jing }, -6f, 0.5f);    // E
            Check(GameConfig.Fx.WindmillFinish, new[] { SfxKind.Whoosh, SfxKind.LowBoom }, -6f, 0.5f);              // F3
            Check(GameConfig.Fx.LandNormal, new[] { SfxKind.DustPuff }, 0, 0);                                     // G
        }

        [Test]
        public void B08_BackingPattern_OneBar_AccentFirst()
        {
            var o = GameConfig.Fx.BackingOffsets;
            Assert.AreEqual(GameConfig.Fx.BackingHits.Length, o.Length);
            Assert.AreEqual(GameConfig.Fx.BackingVolumes.Length, o.Length);
            Assert.AreEqual(0f, o[0], "마디 첫 박에서 시작");
            for (int i = 1; i < o.Length; i++) Assert.Greater(o[i], o[i - 1]);
            Assert.Less(o[o.Length - 1], GameConfig.BeatsPerBar);
            Assert.AreEqual(GameConfig.Fx.BackingVolumes.Max(), GameConfig.Fx.BackingVolumes[0], "첫 박 강세");
            CollectionAssert.Contains(GameConfig.Fx.BackingHits[0], SfxKind.JangguKung);
            CollectionAssert.Contains(GameConfig.Fx.BackingHits[0], SfxKind.JangguDeok);
            Assert.AreEqual(0.0, JangguBacking.NextHitBeat(-1e-6, out int i0), 1e-9); Assert.AreEqual(0, i0);
            Assert.AreEqual(1.5, JangguBacking.NextHitBeat(1.0, out _), 1e-9);
            Assert.AreEqual(4.0, JangguBacking.NextHitBeat(3.5, out int i4), 1e-9); Assert.AreEqual(0, i4);
        }
    }
}
