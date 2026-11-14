using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>배경 음악 (B10): 공유마당 국악 BGM #197을 120 BPM 격자에 맞춘 루프.</summary>
    public class B10_MusicTests
    {
        static AudioClip Clip() => AssetDatabase.LoadAssetAtPath<AudioClip>(GameConfig.Music.ClipPath);

        [Test]
        public void B10_Clip_InAssetsAudioMusic_LoopIs128Beats()
        {
            var clip = Clip();
            Assert.IsNotNull(clip, "Assets/Audio/Music/ 에 음악 파일");
            StringAssert.StartsWith("Assets/Audio/Music/", GameConfig.Music.ClipPath);
            Assert.AreEqual(120f, GameConfig.Bpm, "GDD 4.1 기준 BPM");
            Assert.AreEqual(GameConfig.Music.LoopBeats * GameConfig.SecondsPerBeat, clip.length, 0.002f, "루프 길이 = 128박 (64초)");
            Assert.AreEqual(0, GameConfig.Music.LoopBeats % (GameConfig.BeatsPerBar * 2), "12/8 한 마디(= 게임 2마디)의 정수배");
            Assert.AreEqual(2, clip.channels);

            var imp = (AudioImporter)AssetImporter.GetAtPath(GameConfig.Music.ClipPath);
            Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, imp.defaultSampleSettings.loadType, "되감기 위치를 정확히 찾도록 메모리에 풀기");
            Assert.AreEqual(AudioCompressionFormat.Vorbis, imp.defaultSampleSettings.compressionFormat, "빌드 용량은 Vorbis");
        }

        [Test]
        public void B10_MusicTimeAt_BeatTimesHalfSecond_WrapsEvery128Beats()
        {
            float len = GameConfig.Music.LoopBeats * GameConfig.SecondsPerBeat;
            Assert.AreEqual(0f, BeatClock.MusicTimeAt(0, len), 1e-5f);
            Assert.AreEqual(5f, BeatClock.MusicTimeAt(10, len), 1e-5f);
            Assert.AreEqual(0f, BeatClock.MusicTimeAt(128, len), 1e-4f, "128박마다 처음으로");
            Assert.AreEqual(1.25f, BeatClock.MusicTimeAt(130.5, len), 1e-4f);
            Assert.AreEqual(63.5f, BeatClock.MusicTimeAt(-1, len), 1e-4f, "음수 박도 루프 안으로");
            Assert.AreEqual(0f, BeatClock.MusicTimeAt(10, 0f), "클립 없음");
        }

        /// <summary>
        /// 곡의 강박이 게임 박자에 맞는지: 소리 시작 세기(고역 에너지의 증가)를 1초(= 게임 2박 = 곡의 점4분 박) 주기로 겹쳐 보면
        /// 가장 센 곳이 0초 ±20 ms에 있고 평균의 2배 이상이다. 즉 게임 짝수 박(0, 2, 4…)마다 강박이 온다.
        /// </summary>
        [Test]
        public void B10_Downbeat_OnEvenGameBeats_FromTimeZero()
        {
            var clip = Clip();
            Assert.IsTrue(clip.LoadAudioData());
            var data = new float[clip.samples * clip.channels];
            Assert.IsTrue(clip.GetData(data, 0));
            int rate = clip.frequency, ch = clip.channels;
            int hop = rate / 200;                                  // 5 ms
            int frames = clip.samples / hop;
            var energy = new double[frames];
            float prev = 0;
            for (int f = 0; f < frames; f++)
            {
                double e = 0;
                for (int i = f * hop; i < (f + 1) * hop; i++)
                {
                    float m = 0;
                    for (int c = 0; c < ch; c++) m += data[i * ch + c];
                    m /= ch;
                    float hp = m - prev; prev = m;                 // 고역 통과 (차분)
                    e += hp * hp;
                }
                energy[f] = System.Math.Log(1e-9 + e);
            }
            const int bins = 200;                                  // 1초 = 200 × 5 ms
            var prof = new double[bins];
            int cycles = frames / bins;
            for (int f = 1; f < cycles * bins; f++) prof[f % bins] += System.Math.Max(0, energy[f] - energy[f - 1]);
            double mean = 0; foreach (var v in prof) mean += v; mean /= bins;
            int peak = 0;
            for (int b = 1; b < bins; b++) if (prof[b] > prof[peak]) peak = b;
            int peakMs = (peak > bins / 2 ? peak - bins : peak) * 5;
            Debug.Log($"[B10] 1초 주기 강박 위치 {peakMs} ms, 세기 {prof[peak] / mean:F2} × 평균");
            Assert.LessOrEqual(Mathf.Abs(peakMs), 20, "강박이 0초(게임 짝수 박) ±20 ms에 옴");
            Assert.Greater(prof[peak] / mean, 2.0, "강박이 뚜렷함");
        }

        [Test]
        public void B10_Credit_CcBy_Attribution()
        {
            StringAssert.Contains("국악 BGM #197", GameConfig.Music.Credit);
            StringAssert.Contains("주식회사 아이티앤", GameConfig.Music.Credit);
            StringAssert.Contains("공유마당", GameConfig.Music.Credit);
            StringAssert.Contains("CC BY", GameConfig.Music.Credit);
            StringAssert.Contains(GameConfig.Music.Credit, UiText.AllGlyphs, "폰트 글자 검사에 포함");
        }
    }
}
