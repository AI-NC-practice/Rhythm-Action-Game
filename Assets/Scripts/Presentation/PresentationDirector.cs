using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>연출 요청 하나 (GDD-beta B5). 게임 코드는 "무슨 일이 일어났다"만 알린다.</summary>
    public struct PresentationEvent
    {
        public FxPreset Preset;
        /// <summary>방향 (왼쪽 −1, 오른쪽 +1). 방향 없는 연출은 1.</summary>
        public float Side;
        /// <summary>Hit Stop으로 멈출 대상.</summary>
        public IList<IHitStoppable> Targets;
        /// <summary>월드 위치 (VFX용, B06).</summary>
        public Vector3? Position;

        public static PresentationEvent Of(FxPreset p, float side = 1f, IList<IHitStoppable> targets = null, Vector3? position = null) =>
            new PresentationEvent { Preset = p, Side = side == 0 ? 1f : side, Targets = targets, Position = position };
    }

    /// <summary>
    /// 연출 시스템 진입점 (GDD-beta B5, 제안서 1·6장). Play(event) 하나로 카메라 층·Hit Stop·슬로모션을 한 번에 건다.
    /// 연출은 게임 상태(판정·점수·체력·이동)를 바꾸지 않는다.
    /// Enabled=false면 모든 연출을 끄고 층을 지운다. 단, 처형·윈드밀 슬로모션(GDD 4.2 규칙)은 그대로 둔다.
    /// 개발자 모드 F1로 켜고 끈다.
    /// </summary>
    public class PresentationDirector : MonoBehaviour
    {
        public CameraFx cameraFx;
        public TimeFx timeFx;

        public static PresentationDirector Instance { get; private set; }

        bool enabledFx = true;
        public bool Enabled
        {
            get => enabledFx;
            set
            {
                enabledFx = value;
                if (!value && cameraFx != null) cameraFx.ClearAll();
                EnabledChanged?.Invoke(value);
            }
        }

        public event Action<bool> EnabledChanged;
        /// <summary>연출이 실제로 재생될 때 (B06 화면 효과·VFX, B08 소리가 붙는다).</summary>
        public event Action<PresentationEvent> Played;

        /// <summary>마지막으로 재생한 연출 이름 (테스트·디버그용).</summary>
        public string LastPlayed { get; private set; }

        /// <summary>새 씬에서 시작할 때의 켜짐 상태 (개발자 모드 F1이 씬을 넘어 유지되게).</summary>
        public static bool DefaultEnabled = true;

        void Awake()
        {
            Instance = this;
            enabledFx = DefaultEnabled;
        }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>
        /// 연출을 재생한다. 슬로모션이 있는 연출이면 시간 연출에 요청하고, 받아들여졌는지를 돌려준다
        /// (받아들여지지 않아도 카메라 층과 Hit Stop은 건다).
        /// </summary>
        public bool Play(PresentationEvent e)
        {
            bool slow = false;
            if (e.Preset.SlowMo != SlowMoMode.None) slow = RequestSlowMotion(e.Preset.SlowMo);
            if (!enabledFx) return slow;
            if (e.Preset.HitStop > 0 && timeFx != null && !slow) timeFx.HitStop(e.Preset.HitStop, e.Targets);
            if (cameraFx != null) cameraFx.Add(e.Preset, e.Side);
            LastPlayed = e.Preset.Name;
            Played?.Invoke(e);
            return slow;
        }

        public bool Play(FxPreset p, float side = 1f, IList<IHitStoppable> targets = null, Vector3? position = null) =>
            Play(PresentationEvent.Of(p, side, targets, position));

        /// <summary>
        /// 슬로모션 요청 (GDD-beta B4). Always는 연출을 꺼도 그대로(GDD 규칙), Candidate는 연출이 켜져 있고 빈도 제한을 통과해야 한다.
        /// </summary>
        public bool RequestSlowMotion(SlowMoMode mode)
        {
            if (timeFx == null) return false;
            if (mode == SlowMoMode.Candidate && !enabledFx) return false;
            return timeFx.RequestSlowMotion(mode);
        }

        /// <summary>스테이지를 다시 시작할 때.</summary>
        public void ResetAll()
        {
            if (cameraFx != null) cameraFx.ClearAll();
            if (timeFx != null) timeFx.ResetHistory();
        }
    }
}
