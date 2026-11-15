using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>실제 장구 히트사운드 (GDD-beta B17.1).</summary>
    public class B44_RecordedHitTests
    {
        static readonly string[] Names = { "Hit_Perfect", "Hit_Normal", "Hit_Fail", "Hit_Empty" };

        static float[] Data(AudioClip c)
        {
            Assert.IsTrue(c.LoadAudioData());
            var d = new float[c.samples * c.channels];
            Assert.IsTrue(c.GetData(d, 0));
            return d;
        }

        static float LowRatio(float[] d, int rate)
        {
            int w = rate / 500; double acc = 0, low = 0, all = 0;
            for (int i = 0; i < d.Length; i++)
            {
                acc += d[i]; if (i >= w) acc -= d[i - w];
                double lp = acc / w; low += lp * lp; all += d[i] * d[i];
            }
            return (float)(low / System.Math.Max(1e-9, all));
        }

        [Test]
        public void B44_RecordedHits_Exist_PCM_StartWithin10ms_Timbre()
        {
            Assert.AreEqual("Assets/Audio/Hit", GameConfig.HitSound.RecordedDir);
            Assert.AreEqual(-4.5f, GameConfig.HitSound.DuckDb, 1e-6f);   // GDD-beta B18.1
            Assert.AreEqual(0.08f, GameConfig.HitSound.DuckSeconds, 1e-6f);
            var clips = RhythmActionGame.EditorTools.MusicTool.LoadHitClips();
            for (int i = 0; i < Names.Length; i++)
            {
                Assert.IsNotNull(clips[i], Names[i] + " 녹음 파일");
                var imp = (AudioImporter)AssetImporter.GetAtPath($"{GameConfig.HitSound.RecordedDir}/{Names[i]}.wav");
                Assert.AreEqual(AudioCompressionFormat.PCM, imp.defaultSampleSettings.compressionFormat, "지연 없이 PCM");
                var d = Data(clips[i]);
                float peak = 0; foreach (var v in d) peak = Mathf.Max(peak, Mathf.Abs(v));
                Assert.Greater(peak, 0.5f, Names[i] + " 소리가 있음");
                int onset = System.Array.FindIndex(d, v => Mathf.Abs(v) > 0.1f * peak) / clips[i].channels;
                Assert.Less(onset / (float)clips[i].frequency, 0.01f, Names[i] + " 누르는 즉시 (소리 시작 10 ms 안)");
                Assert.Less(clips[i].length, 0.5f, Names[i] + " 짧음");
            }
            float pLow = LowRatio(Data(clips[0]), clips[0].frequency), nLow = LowRatio(Data(clips[1]), clips[1].frequency);
            Debug.Log($"[B44] 저역 비율 덩 {pLow:F3} / 덕 {nLow:F3}");
            Assert.Greater(pLow, nLow * 2f, "PERFECT 덩(궁편)은 낮고, NORMAL 덕(채편)은 높음");
        }
    }
}
