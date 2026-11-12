using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 사건 소품을 사건 진행에 맞춰 움직인다 (GDD-beta B2). SceneBuilder가 만든 소품을 쓰고,
    /// 차트에 있는데 씬에 없는 소품(시험용 차트 등)은 시작할 때 코드로 만든다.
    /// </summary>
    public class EventPropDirector : MonoBehaviour
    {
        public StageDirector director;
        public PathFollower player;
        /// <summary>소품을 담는 곳 (SceneBuilder가 만든 "EventProps").</summary>
        public Transform propRoot;

        readonly List<EventProp> props = new List<EventProp>();
        readonly List<GameObject> spawned = new List<GameObject>();

        public IReadOnlyList<EventProp> Props => props;

        void OnEnable() { if (director != null) director.StageReset += OnStageReset; }
        void OnDisable() { if (director != null) director.StageReset -= OnStageReset; }

        static string Key(int runBeat, string kind) => runBeat + "_" + kind;

        /// <summary>차트에 맞게 소품 목록을 맞추고 모두 처음 상태로 돌린다.</summary>
        public void OnStageReset()
        {
            foreach (var g in spawned) if (g != null) Destroy(g);
            spawned.Clear();
            props.Clear();
            if (propRoot == null) propRoot = new GameObject("EventProps").transform;

            var existing = new Dictionary<string, EventProp>();
            foreach (var p in propRoot.GetComponentsInChildren<EventProp>(true)) existing[Key(p.runBeat, p.kind)] = p;

            var chart = director != null ? director.Chart : null;
            var wanted = new HashSet<string>();
            if (chart != null)
            {
                foreach (var e in chart.events)
                {
                    if (!EventProps.Needs(e)) continue;
                    string kind = e.IsForeshadow ? StageChart.TypeForeshadow : e.EventKind;
                    string key = Key(e.runBeat, kind);
                    wanted.Add(key);
                    if (!existing.TryGetValue(key, out var prop))
                    {
                        prop = EventProps.Build(e, propRoot);
                        spawned.Add(prop.gameObject);
                    }
                    props.Add(prop);
                }
            }
            // 이 차트에 없는 소품(씬 차트와 다른 시험용 차트)은 숨긴다
            foreach (var kv in existing)
                if (!wanted.Contains(kv.Key)) kv.Value.gameObject.SetActive(false);
            foreach (var p in props)
            {
                if (p.kind != StageChart.TypeForeshadow) p.gameObject.SetActive(true);
                p.ResetProp();
            }
        }

        StageDirector.ObstacleRun RunFor(EventProp p)
        {
            if (director == null) return null;
            if (director.Current != null && director.Current.Event.runBeat == p.runBeat) return director.Current;
            foreach (var r in director.PendingRuns) if (r.Event.runBeat == p.runBeat) return r;
            if (director.LastRun != null && director.LastRun.Event.runBeat == p.runBeat) return director.LastRun;
            return null;
        }

        void LateUpdate()
        {
            if (player == null) return;
            double v = player.VisualRunBeat;
            foreach (var p in props)
            {
                if (p == null) continue;
                // 화면 근처(앞 30박, 뒤 8박)만 움직인다
                if (p.kind != StageChart.TypeForeshadow && (v < p.runBeat - 30 || v > p.runBeat + p.beats + 8)) continue;
                p.Animate(v, RunFor(p));
            }
        }
    }
}
