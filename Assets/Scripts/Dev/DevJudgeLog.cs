#if UNITY_EDITOR || RAG_DEV
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 개발자 판정 기록 (진단용, GDD에 없음 — 본 게임에서는 사라진다).
    /// 키를 누를 때마다 가장 가까운 판정 원과의 시간 차(ms, 음수 = 이름)와 결과를, 판정·되감기·상태 변화를 Player.log에 남긴다.
    /// </summary>
    public class DevJudgeLog : MonoBehaviour
    {
        GameBootstrap hooked;

        void Update()
        {
            var boot = GameBootstrap.Instance;
            if (boot == null || boot.judge == null || boot.clock == null || boot.director == null || hooked == boot) return;
            hooked = boot;
            var clock = boot.clock;
            var dir = boot.director;
            boot.judge.InputReceived += (key, cue, outcome) =>
            {
                string near = cue != null ? $"원 {cue.TargetBeat:0.00}박 {cue.Key}, 차이 {(boot.judge.LastInputTime - clock.TimeOfBeat(cue.TargetBeat)) * 1000:0}ms" : "떠 있는 원 없음";
                Debug.Log($"[판정기록] 키 {key} → {outcome} ({near}, 시계 {clock.CurrentBeat:0.00}박{Rel(dir, clock)})");
            };
            boot.judge.CueJudged += (c, r) => Debug.Log($"[판정기록] 판정 {c.TargetBeat:0.00}박 {c.Key} = {r}{Kind(c)} (속도 {clock.SpeedMultiplier:0.00})");
            dir.RewindStarted += k => Debug.Log($"[판정기록] 되감기 {k} (시계 {clock.CurrentBeat:0.00}박{Rel(dir, clock)})");
            dir.StateChanged += s => Debug.Log($"[판정기록] 상태 {s} (시계 {clock.CurrentBeat:0.00}박)");
        }

        static string Rel(StageDirector d, BeatClock c)
        {
            var e = d.CurrentEncounter;
            return e != null && e.IsBossDuel ? $", 결투 O+{c.CurrentBeat - e.StartBeat:0.00}" : "";
        }

        static string Kind(JudgeCue c) => c.Tag is CombatStep s ? $" [{s.Kind}]" : "";
    }
}
#endif
