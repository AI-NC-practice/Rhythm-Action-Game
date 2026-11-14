using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>히트사운드 규칙과 소리 (GDD-beta B14).</summary>
    public class B36_HitSoundTests
    {
        [Test]
        public void B36_Rules_ResultToSoundAndVolume_MatchGddBetaB14()
        {
            void Check(InputOutcome o, Judgement r, SfxKind expect, float vol)
            {
                Assert.AreEqual(vol, HitSoundRules.Pick(o, r, out var k), 1e-6f, $"{o}/{r} 음량");
                Assert.AreEqual(expect, k, $"{o}/{r} 소리");
            }
            Check(InputOutcome.Judged, Judgement.Perfect, SfxKind.HitPerfect, 1.0f);
            Check(InputOutcome.Judged, Judgement.Normal, SfxKind.HitNormal, 0.7f);
            Check(InputOutcome.Judged, Judgement.Fail, SfxKind.HitFail, 0.5f);
            Check(InputOutcome.NoCue, Judgement.Fail, SfxKind.HitEmpty, 0.2f);
            Check(InputOutcome.TooEarly, Judgement.Perfect, SfxKind.HitEmpty, 0.2f);
            Assert.AreEqual(4, GameConfig.HitSound.Voices);
        }

        /// <summary>저역(약 250 Hz 아래) 에너지 비율: 2 ms 이동 평균으로 거른 신호의 에너지 / 전체 에너지.</summary>
        static float LowRatio(float[] d)
        {
            int w = SfxSynth.SampleRate / 500;
            double sum = 0, all = 0, acc = 0;
            for (int i = 0; i < d.Length; i++)
            {
                acc += d[i];
                if (i >= w) acc -= d[i - w];
                float lp = (float)(acc / w);
                sum += lp * lp; all += d[i] * d[i];
            }
            return (float)(sum / System.Math.Max(1e-9, all));
        }

        [Test]
        public void B36_Synth_FourSounds_Timbre_And_WavFiles()
        {
            var kinds = new[] { SfxKind.HitPerfect, SfxKind.HitNormal, SfxKind.HitFail, SfxKind.HitEmpty };
            foreach (var k in kinds)
            {
                var d = SfxSynth.Create(k);
                float m = 0; foreach (var x in d) m = Mathf.Max(m, Mathf.Abs(x));
                Assert.AreEqual(0.9f, m, 1e-3f, k + " 최고값 0.9");
                Assert.AreEqual(0f, d[d.Length - 1], 1e-3f, k + " 끝이 0 (딸깍 방지)");
                Assert.IsTrue(File.Exists(SfxSynthToolPath(k)), k + " WAV가 Assets/Audio/Sfx에 있음");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(SfxSynthToolPath(k)));
            }
            float perfectLow = LowRatio(SfxSynth.Create(SfxKind.HitPerfect));
            float normalLow = LowRatio(SfxSynth.Create(SfxKind.HitNormal));
            Debug.Log($"[B36] 저역 비율 덩 {perfectLow:F3} / 딱 {normalLow:F3}");
            Assert.Greater(perfectLow, normalLow * 2f, "덩(PERFECT)은 궁편 저음을 포함, 딱(NORMAL)은 높은 소리");
            Assert.Less(SfxSynth.Seconds(SfxKind.HitEmpty), 0.05f, "틱은 아주 짧음");
            Assert.Less(SfxSynth.Seconds(SfxKind.HitNormal), SfxSynth.Seconds(SfxKind.HitPerfect), "딱은 덩보다 짧음");
        }

        static string SfxSynthToolPath(SfxKind k) => RhythmActionGame.EditorTools.SfxSynthTool.PathOf(k);

        [Test]
        public void B36_DspBuffer512()
        {
            var am = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset");
            Assert.IsNotEmpty(am);
            var so = new SerializedObject(am[0]);
            Assert.AreEqual(512, GameConfig.HitSound.DspBufferSize);
            Assert.AreEqual(512, so.FindProperty("m_RequestedDSPBufferSize").intValue, "오디오 지연을 줄이려고 DSP 버퍼 512");
        }
    }
}
