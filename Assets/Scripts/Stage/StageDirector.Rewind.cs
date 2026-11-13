using System;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>되감기 종류 (GDD-beta B9.2, B9.3).</summary>
    public enum RewindKind
    {
        Windmill,   // 윈드밀 FAIL → 3단계 첫 화살표 1마디 앞
        Boss,       // 무사 결투 FAIL → 그 단계 처음 (GDD-beta B11.4)
        Chapter,    // 체력 0 → 지금 챕터 시작 지점
    }

    /// <summary>
    /// 되감기 (GDD-beta B9). 되감기를 시작하면 남은 판정을 모두 지우고 박자 시계를 멈춘 채 0.8초 동안 연출을 보여 준 뒤,
    /// 곡과 박자 시계를 되감은 지점부터 다시 진행한다. 체력은 깎지 않는다(챕터 되감기는 체력 3으로 다시).
    /// </summary>
    public partial class StageDirector
    {
        /// <summary>체력 0일 때 게임 오버 대신 챕터 처음으로 되감기 (GDD-beta B9.3, 테스트 단계 기본값 true).</summary>
        public bool AllowDeathRewind = true;

        /// <summary>테스트에서 바꿔 끼우는 실제 시간 (초).</summary>
        public Func<float> RewindRealTime = () => Time.unscaledTime;

        public bool Rewinding { get; private set; }
        public RewindKind LastRewind { get; private set; }
        public int RewindCount { get; private set; }

        /// <summary>되감기가 시작될 때 (연출용).</summary>
        public event Action<RewindKind> RewindStarted;
        /// <summary>되감기가 끝나 다시 진행할 때 (되감은 박자).</summary>
        public event Action<RewindKind, double> RewindFinished;
        /// <summary>챕터 되감기: GameBootstrap이 그 지점부터 다시 시작한다 (달리기 박자).</summary>
        public event Action<double> ChapterRewindRequested;

        float rewindStartedAt;
        double rewindTargetBeat;
        double rewindFromRun;
        EnemyActor[] rewindActors;

        /// <summary>되감기 진행도 (0~1, 실제 시간 기준).</summary>
        public float RewindProgress => Rewinding ? Mathf.Clamp01((RewindRealTime() - rewindStartedAt) / GameConfig.RewindSeconds) : 0f;
        Action rewindResume;

        // ---------------- GDD-beta B12.2 피격 연출 뒤 되감기 ----------------
        /// <summary>결투·윈드밀 FAIL 뒤 되감기 전에 맞는 연출을 보여 주는 시간 (실제 초). 0이면 바로 되감는다.</summary>
        public float FailHitSeconds = GameConfig.FailHitSeconds;
        /// <summary>맞는 연출 중 (판정은 모두 지워졌고, 끝나면 되감기가 시작된다).</summary>
        public bool HitPending { get; private set; }
        /// <summary>플레이어가 맞음 (피격 연출용).</summary>
        public event Action<RewindKind> PlayerHit;

        float hitAt;
        RewindKind hitKind;
        double hitTarget;
        Action hitResume;

        /// <summary>FAIL: 먼저 맞는 연출(FailHitSeconds)을 보여 준 뒤 되감는다. 남은 판정은 바로 지운다.</summary>
        void HitThenRewind(RewindKind kind, double targetBeat, Action resume)
        {
            if (FailHitSeconds <= 0f) { BeginRewind(kind, targetBeat, resume); return; }
            HitPending = true;
            hitAt = RewindRealTime();
            hitKind = kind; hitTarget = targetBeat; hitResume = resume;
            judge.ClearAll();
            PlayerHit?.Invoke(kind);
        }

        void UpdateHitPending()
        {
            if (!HitPending || RewindRealTime() - hitAt < FailHitSeconds) return;
            HitPending = false;
            BeginRewind(hitKind, hitTarget, hitResume);
        }

        /// <summary>
        /// 되감기를 시작한다. targetBeat: 다시 시작할 시계 박자 (챕터 되감기는 달리기 박자). resume: 시계를 다시 돌린 뒤 할 일.
        /// </summary>
        void BeginRewind(RewindKind kind, double targetBeat, Action resume)
        {
            Rewinding = true;
            LastRewind = kind;
            RewindCount++;
            rewindStartedAt = RewindRealTime();
            rewindTargetBeat = targetBeat;
            rewindResume = resume;
            rewindFromRun = player != null ? player.RunBeat : 0;
            rewindActors = encounter != null ? encounter.Actors : null;
            judge.ClearAll();                       // 남은 원도 모두 사라짐 (B11)
            if (slowMotion != null) slowMotion.Cancel();
            clock.Pause();
            RewindStarted?.Invoke(kind);
        }

        void UpdateRewind()
        {
            if (!Rewinding) return;
            // 실제로 거꾸로 (GDD-beta B10.3): 챕터 되감기는 보이는 위치가 뒤로, 전투 되감기는 적의 휘두르기가 거꾸로
            float u = RewindProgress;
            float e = u * u * (3f - 2f * u);
            if (LastRewind == RewindKind.Chapter && player != null)
            {
                player.VisualOverride = rewindFromRun + (rewindTargetBeat - rewindFromRun) * e;
                player.Place();
            }
            else if (rewindActors != null)
                foreach (var a in rewindActors) if (a != null) a.RewindSwing(e);
            if (RewindRealTime() - rewindStartedAt < GameConfig.RewindSeconds) return;
            Rewinding = false;
            if (player != null) player.VisualOverride = null;
            var kind = LastRewind;
            if (kind == RewindKind.Chapter)
            {
                ChapterRewindRequested?.Invoke(rewindTargetBeat);   // GameBootstrap이 시계·무대를 다시 시작
            }
            else
            {
                clock.RestartAt(rewindTargetBeat);
                rewindResume?.Invoke();
            }
            RewindFinished?.Invoke(kind, rewindTargetBeat);
        }

        /// <summary>체력 0 (GDD-beta B9.3): 게임 오버 대신 지금 챕터 시작 지점으로 되감는다.</summary>
        void RewindToChapter()
        {
            double start = MapLayout.SectionOf((int)player.RunBeat).StartBeat;
            player.Running = false;
            BeginRewind(RewindKind.Chapter, start, null);
        }
    }
}
