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
    /// 포트폴리오용 플레이 GIF (평소 테스트에서는 빠짐: Explicit). 고정 프레임레이트(Time.captureFramerate)로 자동 연주(PERFECT)하며
    /// 화면을 찍어 바로 GIF로 저장한다 → harness/reports/Portfolio/*.gif. 실행: unity-verify.ps1 -Platform PlayMode -Filter Portfolio_
    /// </summary>
    [Explicit("포트폴리오 GIF 캡처 — 직접 지정할 때만")]
    public class Portfolio_GifCapture
    {
        const string Out = "harness/reports/Portfolio";
        const int W = 480, H = 270, Fps = 30, Every = 2;   // 15 fps GIF
        double now;
        GameBootstrap boot;
        StageDirector dir;

        [TearDown]
        public void TearDown() { AttractMode.AttractOnLoad = false; Time.captureFramerate = 0; Time.timeScale = 1f; if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel(); }

        static Color32[] Grab(Camera cam)
        {
            var rt = new RenderTexture(W, H, 24);
            var old = cam.targetTexture;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = old;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(W, H, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            RenderTexture.active = prev;
            var px = t.GetPixels32();
            Object.Destroy(t); rt.Release();
            return px;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Portfolio_1_Attract()
        {
            Directory.CreateDirectory(Out);
            Time.captureFramerate = Fps;
            AttractMode.AttractOnLoad = true;
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var attract = GameBootstrap.Instance.attract;
            attract.DeltaTime = () => 1f / Fps;
            for (int f = 0; f < 30; f++) yield return null;
            var gif = new GifWriter($"{Out}/1_attract_run.gif", W, H);
            for (int f = 0; f < 120; f++)
            {
                yield return null;
                if (f % Every == 0) gif.AddFrame(Grab(attract.AttractCamera), 100 * Every / Fps);
            }
            gif.Close();
        }

        IEnumerator Run(string file, double from, double to, double delay = 0.0)
        {
            Directory.CreateDirectory(Out);
            Time.captureFramerate = Fps;
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            now = 0;
            boot.clock.TimeSource = () => now;
            float realTime = Time.realtimeSinceStartup;
            dir.slowMotion.RealTime = () => realTime + (float)now;
            foreach (var fx in Object.FindObjectsByType<TimeFx>(FindObjectsSortMode.None)) fx.RealTime = () => realTime + (float)now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            boot.StartRun(from);
            yield return null;
            var cam = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            var gif = new GifWriter($"{Out}/{file}.gif", W, H);
            int f = 0;
            while (boot.player.VisualRunBeat < to && f < 600)
            {
                now += 1.0 / Fps;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) + delay - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
                yield return null;
                if (f++ % Every == 0) gif.AddFrame(Grab(cam), 100 * Every / Fps);
            }
            gif.Close();
        }

        [UnityTest, Timeout(600000)] public IEnumerator Portfolio_2_WallRun() => Run("2_wallrun_60", 57, 64.5);
        [UnityTest, Timeout(600000)] public IEnumerator Portfolio_3_SlideUnderEave() => Run("3_slide_eave_80", 77.5, 84);
        [UnityTest, Timeout(600000)] public IEnumerator Portfolio_4_LookBack() => Run("4_lookback_barricade", GameConfig.Fx.LookBackRunBeats[0] - 2, GameConfig.Fx.LookBackRunBeats[0] + LookBackCut.LengthOf(0) + 1);
        [UnityTest, Timeout(600000)] public IEnumerator Portfolio_5_ClosingGate() => Run("5_closing_gate_202", 199, 207);

        /// <summary>작은 GIF89a 인코더: 프레임마다 지역 팔레트(5비트 히스토그램 상위 256색), LZW, 무한 반복.</summary>
        class GifWriter
        {
            readonly BinaryWriter w;
            readonly int width, height;

            public GifWriter(string path, int width, int height)
            {
                this.width = width; this.height = height;
                w = new BinaryWriter(File.Create(path));
                w.Write(System.Text.Encoding.ASCII.GetBytes("GIF89a"));
                w.Write((ushort)width); w.Write((ushort)height);
                w.Write((byte)0); w.Write((byte)0); w.Write((byte)0);
                // NETSCAPE2.0 무한 반복
                w.Write(new byte[] { 0x21, 0xFF, 0x0B });
                w.Write(System.Text.Encoding.ASCII.GetBytes("NETSCAPE2.0"));
                w.Write(new byte[] { 0x03, 0x01, 0x00, 0x00, 0x00 });
            }

            public void AddFrame(Color32[] px, int delayCs)
            {
                // 팔레트: 5-5-5 칸 히스토그램 상위 256칸의 평균색
                var count = new int[32768]; var sr = new long[32768]; var sg = new long[32768]; var sb = new long[32768];
                foreach (var c in px) { int k = (c.r >> 3) << 10 | (c.g >> 3) << 5 | (c.b >> 3); count[k]++; sr[k] += c.r; sg[k] += c.g; sb[k] += c.b; }
                var top = Enumerable.Range(0, 32768).Where(k => count[k] > 0).OrderByDescending(k => count[k]).Take(256).ToArray();
                var pal = new byte[256 * 3];
                for (int i = 0; i < top.Length; i++) { int k = top[i]; pal[i * 3] = (byte)(sr[k] / count[k]); pal[i * 3 + 1] = (byte)(sg[k] / count[k]); pal[i * 3 + 2] = (byte)(sb[k] / count[k]); }
                var map = new short[32768];
                for (int i = 0; i < map.Length; i++) map[i] = -1;
                var idx = new byte[width * height];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        var c = px[(height - 1 - y) * width + x];   // ReadPixels는 아래부터
                        int k = (c.r >> 3) << 10 | (c.g >> 3) << 5 | (c.b >> 3);
                        if (map[k] < 0)
                        {
                            int best = 0, bd = int.MaxValue;
                            int r = (k >> 10) * 8 + 4, g = ((k >> 5) & 31) * 8 + 4, b = (k & 31) * 8 + 4;
                            for (int i = 0; i < top.Length; i++)
                            {
                                int dr = pal[i * 3] - r, dg = pal[i * 3 + 1] - g, db = pal[i * 3 + 2] - b;
                                int d = dr * dr * 3 + dg * dg * 4 + db * db * 2;
                                if (d < bd) { bd = d; best = i; }
                            }
                            map[k] = (short)best;
                        }
                        idx[y * width + x] = (byte)map[k];
                    }
                w.Write(new byte[] { 0x21, 0xF9, 0x04, 0x00 }); w.Write((ushort)delayCs); w.Write(new byte[] { 0x00, 0x00 });
                w.Write((byte)0x2C); w.Write((ushort)0); w.Write((ushort)0); w.Write((ushort)width); w.Write((ushort)height);
                w.Write((byte)0x87);   // 지역 팔레트 256색
                w.Write(pal);
                w.Write((byte)8);
                var data = Lzw(idx);
                for (int i = 0; i < data.Count; i += 255)
                {
                    int n = Mathf.Min(255, data.Count - i);
                    w.Write((byte)n); w.Write(data.GetRange(i, n).ToArray());
                }
                w.Write((byte)0);
            }

            static List<byte> Lzw(byte[] idx)
            {
                var outp = new List<byte>();
                int bitBuf = 0, bitCnt = 0, codeSize = 9, next = 258;
                void Emit(int code) { bitBuf |= code << bitCnt; bitCnt += codeSize; while (bitCnt >= 8) { outp.Add((byte)(bitBuf & 0xFF)); bitBuf >>= 8; bitCnt -= 8; } }
                var dict = new Dictionary<int, int>();
                Emit(256);
                int prefix = idx[0];
                for (int i = 1; i < idx.Length; i++)
                {
                    int k = idx[i], key = prefix << 8 | k;
                    if (dict.TryGetValue(key, out int code)) { prefix = code; continue; }
                    Emit(prefix);
                    if (next < 4096)
                    {
                        dict[key] = next++;
                        if (next > (1 << codeSize) && codeSize < 12) codeSize++;
                    }
                    else { Emit(256); dict.Clear(); next = 258; codeSize = 9; }
                    prefix = k;
                }
                Emit(prefix);
                Emit(257);
                if (bitCnt > 0) outp.Add((byte)(bitBuf & 0xFF));
                return outp;
            }

            public void Close() { w.Write((byte)0x3B); w.Close(); }
        }
    }
}
