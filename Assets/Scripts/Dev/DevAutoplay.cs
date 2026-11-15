#if UNITY_EDITOR || RAG_DEV
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 개발자 자동 연주 (진단용, GDD에 없음 — 본 게임에서는 사라진다).
    /// 실행 인자 -autoplay 이면 판정 원이 목표 박에 닿는 프레임에 실제 시간으로 키를 넣고(SubmitRealtime, 키보드와 같은 경로),
    /// 판정마다 목표와의 시간 차를 로그에 남긴다. -devChapter N 이면 그 챕터에서 바로 시작한다 (DevMode).
    /// </summary>
    public class DevAutoplay : MonoBehaviour
    {
        GameBootstrap hooked;
        int lastSection = -1;
        int missLeft = -1;
        string shotDir;
        double nextShot = 3.5;
        double shotEnd = 9.0;
        readonly System.Collections.Generic.HashSet<JudgeCue> skipped = new System.Collections.Generic.HashSet<JudgeCue>();

        public static bool Requested => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-autoplay") >= 0;

        void Awake() => Application.runInBackground = true;   // 진단 실행 중 창이 포커스를 잃어도 멈추지 않게

        void Update()
        {
            var boot = GameBootstrap.Instance;
            if (boot == null || boot.judge == null || boot.clock == null) return;
            if (hooked != boot)
            {
                hooked = boot;
                boot.judge.CueJudged += (c, r) => Debug.Log($"[autoplay] 판정 목표 {c.TargetBeat:0.00}박 {c.Key} → {r} (시계 {boot.clock.CurrentBeat:0.00}박, 속도 {boot.clock.SpeedMultiplier:0.00})");
                boot.director.RewindStarted += k => Debug.Log($"[autoplay] 되감기 {k} at {boot.clock.CurrentBeat:0.00}박");
                boot.director.StateChanged += s => Debug.Log($"[autoplay] 상태 {s} at {boot.clock.CurrentBeat:0.00}박");
            }
            // -autoplayShots 폴더: 무사 결투 O+3.5 ~ O+9 사이 0.25박마다 화면을 저장한다 (진단용)
            var e0 = boot.director.CurrentEncounter;
            if (shotDir == null)
            {
                var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, "-autoplayShots"); shotDir = i >= 0 && i + 1 < a.Length ? a[i + 1] : "";
                // -autoplayShotsRange 시작 끝 (O 기준 박, 기본 3.5 ~ 9)
                int j = System.Array.IndexOf(a, "-autoplayShotsRange");
                if (j >= 0 && j + 2 < a.Length && double.TryParse(a[j + 1], out double s0) && double.TryParse(a[j + 2], out double s1)) { nextShot = s0; shotEnd = s1; }
            }
            if (shotDir.Length > 0 && e0 != null && e0.IsBossDuel)
            {
                double rel = boot.clock.CurrentBeat - e0.StartBeat;
                if (rel >= nextShot && rel < shotEnd) { ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(shotDir, $"duel_{rel:00.00}.png")); Debug.Log($"[autoplay] 화면 저장 {rel:0.00}박"); nextShot = System.Math.Floor(rel * 4 + 1) / 4; }
            }
            // 챕터(구간)가 바뀌면 달리기 박자와 시계 박자를 남긴다 (곡 위치를 챕터에 맞추는 데 씀, B10)
            if (boot.player != null)
            {
                var sec = MapLayout.SectionOf((int)boot.player.RunBeat);
                if (sec.Number != lastSection) { lastSection = sec.Number; Debug.Log($"[autoplay] 구간 {sec.Number} 시작: 달리기 {boot.player.RunBeat:0.00}박, 시계 {boot.clock.CurrentBeat:0.00}박"); }
            }
            var cue = boot.judge.FirstActiveCue;
            if (cue == null || boot.clock.CurrentBeat < cue.TargetBeat) return;
            // -autoplayMiss N: 무사 결투의 N번째 판정을 한 번 일부러 놓쳐 되감기를 일으킨다 (진단용)
            if (missLeft < 0) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, "-autoplayMiss"); missLeft = i >= 0 && i + 1 < a.Length && int.TryParse(a[i + 1], out int n) ? n : 0; }
            if (skipped.Contains(cue)) return;
            var enc = boot.director.CurrentEncounter;
            if (missLeft > 0 && enc != null && enc.IsBossDuel && enc.Cues.IndexOf(cue) == missLeft - 1) { missLeft = 0; skipped.Add(cue); Debug.Log($"[autoplay] 일부러 놓침 {cue.TargetBeat:0.00}박"); return; }
            double real = Time.realtimeSinceStartupAsDouble;
            double clockT = boot.clock.RealtimeToClock(real);
            Debug.Log($"[autoplay] 누름 {cue.Key} 목표 {cue.TargetBeat:0.00}박: 지금 박 {boot.clock.CurrentBeat:0.000}, 입력 시각→시계 {(clockT - boot.clock.TimeOfBeat(cue.TargetBeat)) * 1000:0}ms (목표 기준), Now {(boot.clock.Now - boot.clock.TimeOfBeat(cue.TargetBeat)) * 1000:0}ms");
            boot.judge.SubmitRealtime(cue.Key, real);
        }
    }
}
#endif
