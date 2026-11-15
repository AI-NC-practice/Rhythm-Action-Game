using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>배경 음악 (B10): 스테이지 1 곡 — 퓨전 국악 액션, 120 BPM, 96마디(384박).</summary>
    public class B10_MusicTests
    {
        static AudioClip Clip() => AssetDatabase.LoadAssetAtPath<AudioClip>(GameConfig.Music.ClipPath);
        static float LoopSeconds => GameConfig.Music.LoopBeats * GameConfig.SecondsPerBeat;

        [Test]
        public void B10_Clip_InAssetsAudioMusic_384Beats_ImportSettings()
        {
            var clip = Clip();
            Assert.IsNotNull(clip, "Assets/Audio/Music/ 에 음악 파일");
            StringAssert.StartsWith("Assets/Audio/Music/", GameConfig.Music.ClipPath);
            Assert.AreEqual(120f, GameConfig.Bpm, "GDD 4.1 기준 BPM");
            Assert.AreEqual(384, GameConfig.Music.LoopBeats, "96마디 = 스테이지 길이(모두 PERFECT일 때 약 384박)");
            Assert.AreEqual(0, GameConfig.Music.LoopBeats % GameConfig.BeatsPerBar, "마디 단위");
            Assert.That(clip.length, Is.InRange(GameConfig.Music.FirstBeatSeconds + LoopSeconds - 0.01f, GameConfig.Music.FirstBeatSeconds + LoopSeconds + 0.15f),
                "길이 = 첫 박 + 192초 (+ mp3 꼬리)");
            Assert.AreEqual(2, clip.channels);
            var imp = (AudioImporter)AssetImporter.GetAtPath(GameConfig.Music.ClipPath);
            Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, imp.defaultSampleSettings.loadType, "되감기 위치를 정확히 찾도록 메모리에 풀기");
            Assert.AreEqual(AudioCompressionFormat.Vorbis, imp.defaultSampleSettings.compressionFormat, "빌드 용량은 Vorbis");
        }

        [Test]
        public void B10_MusicTimeAt_FirstBeatPlusHalfSecondPerBeat_WrapsEvery384Beats()
        {
            float f = GameConfig.Music.FirstBeatSeconds;
            float len = f + LoopSeconds + 0.05f;               // mp3 꼬리가 붙은 클립 길이
            Assert.AreEqual(f, BeatClock.MusicTimeAt(0, len), 1e-5f);
            Assert.AreEqual(f + 5f, BeatClock.MusicTimeAt(10, len), 1e-4f);
            Assert.AreEqual(f, BeatClock.MusicTimeAt(384, len), 1e-3f, "384박마다 처음(첫 박)으로 — 클립 길이가 아니라 박 수로 반복");
            Assert.AreEqual(f + 1.25f, BeatClock.MusicTimeAt(386.5, len), 1e-3f);
            Assert.AreEqual(f + LoopSeconds - 0.5f, BeatClock.MusicTimeAt(-1, len), 1e-3f, "음수 박도 반복 안으로");
            Assert.AreEqual(0f, BeatClock.MusicTimeAt(10, 0f), "클립 없음");
            Assert.AreEqual(0.05f, GameConfig.Music.ResyncThresholdSeconds, 1e-6f);
            // 다시 맞추기 기준: 50 ms 넘게 어긋나면, 곡 끝↔처음 경계는 짧은 쪽으로
            Assert.IsFalse(BeatClock.NeedsResync(10.02f, 10f), "20 ms는 그대로 (끊김 방지)");
            Assert.IsTrue(BeatClock.NeedsResync(10.3f, 10f), "300 ms는 다시 맞춤");
            Assert.IsTrue(BeatClock.NeedsResync(9.9f, 10f));
            Assert.IsFalse(BeatClock.NeedsResync(LoopSeconds - 0.01f, 0.02f), "곡 끝(191.99초)과 처음(0.02초)은 30 ms 차이");
            Assert.IsTrue(BeatClock.NeedsResync(LoopSeconds + 0.04f, 0.0f - 0.0f + 0.5f), "꼬리로 넘어간 위치는 다시 맞춤");
        }

        /// <summary>
        /// 곡의 박이 게임 박자에 맞는지: 소리 시작 세기(고역 에너지 증가)를 0.5초(1박) 주기로 겹쳐 보면
        /// 가장 센 곳이 첫 박 위치(FirstBeatSeconds) ±10 ms에 있고 평균의 2배 이상이다. mp3 변환 지연을 실제 클립에서 확인한다.
        /// </summary>
        [Test]
        public void B10_Downbeat_OnEveryGameBeat_AtFirstBeatSeconds()
        {
            var clip = Clip();
            Assert.IsTrue(clip.LoadAudioData());
            var data = new float[clip.samples * clip.channels];
            Assert.IsTrue(clip.GetData(data, 0));
            int rate = clip.frequency, ch = clip.channels;
            int hop = System.Math.Max(1, rate / 1000);             // 약 1 ms (44.1 kHz면 0.998 ms → 아래에서 시각으로 칸을 정함)
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
            const int bins = 500;                                  // 1박 = 500 × 1 ms
            var prof = new double[bins];
            double loopMs = LoopSeconds * 1000.0;
            for (int f = 5; f < frames; f++)
            {
                double ms = (double)f * hop * 1000.0 / rate;
                if (ms >= loopMs) break;
                double on = System.Math.Max(0, energy[f] - energy[f - 5]);   // 약 5 ms 사이 증가
                prof[(int)System.Math.Round(ms) % bins] += on;
            }
            double mean = 0; foreach (var v in prof) mean += v; mean /= bins;
            int peak = 0;
            for (int b = 1; b < bins; b++) if (prof[b] > prof[peak]) peak = b;
            Debug.Log($"[B10] 1박 주기 박 위치 {peak} ms (설정 {GameConfig.Music.FirstBeatSeconds * 1000:F0} ms), 세기 {prof[peak] / mean:F2} × 평균");
            Assert.AreEqual(GameConfig.Music.FirstBeatSeconds * 1000f, peak, 10f, "박이 FirstBeatSeconds(mp3 지연) ±10 ms에 옴");
            Assert.Greater(prof[peak] / mean, 2.0, "박이 뚜렷함");
        }

        [Test]
        public void B10_Credit_Attribution_AuthorsAndLicenses()
        {
            StringAssert.Contains("공유마당", GameConfig.Music.Credit);
            StringAssert.Contains("한국저작권위원회", GameConfig.Music.Credit);
            StringAssert.Contains("태평소독주 시그널음악", GameConfig.Music.Credit);
            StringAssert.Contains("꽹과리", GameConfig.Music.Credit);
            StringAssert.Contains("CC BY", GameConfig.Music.Credit);
            StringAssert.Contains("(재)전주정보문화산업진흥원", GameConfig.Music.Credit);
            StringAssert.Contains("공공누리 1유형", GameConfig.Music.Credit);
            StringAssert.Contains(GameConfig.Music.Credit, UiText.AllGlyphs, "폰트 글자 검사에 포함");
        }
    }
}
