using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>적 공격 하나 (GDD 4.5). attack: horizontal | overhead | thrust | upward. gap: 이전 공격과의 간격(박자, 0이면 기본값).</summary>
    [Serializable]
    public class ChartAttack
    {
        public string attack;
        public string key;
        public int gap;

        public ArrowKey Key => StageChart.ParseKey(key);
    }

    /// <summary>
    /// 차트 이벤트 하나. runBeat는 경로 위 위치(달리기 박자, 거리 = runBeat × 2.5 m).
    /// type: obstacle | enemy | surround | boss.
    /// </summary>
    [Serializable]
    public class ChartEvent
    {
        public int runBeat;
        public string type;
        public string kind;
        public string key;
        public int section;
        public bool onRoof;
        public ChartAttack[] attacks;
        /// <summary>사건 안 판정별 키 (GDD-beta B2.1). 없으면 key를 쓴다.</summary>
        public string[] keys;
        /// <summary>적 등장 방식 (GDD-beta B2.3): duel(기본) | charge | ambush | chase.</summary>
        public string approach;
        /// <summary>복선(foreshadow)이 보이는 길이 (박자, GDD-beta B2.3).</summary>
        public int span;

        public bool IsObstacle => type == StageChart.TypeObstacle;
        public bool IsEnemy => type == StageChart.TypeEnemy;
        /// <summary>멈춰서 싸우는 일반 적 (GDD 4.5 대치).</summary>
        public bool IsDuel => IsEnemy && (string.IsNullOrEmpty(approach) || approach == EventCatalog.Duel);
        /// <summary>달리면서 처리하는 사건 (장애물, 새 사건, 안 멈추는 적) — GDD-beta B2.</summary>
        public bool IsRunEvent => IsObstacle || (IsEnemy && !IsDuel);
        /// <summary>사건 목록(EventCatalog)의 키: 장애물은 kind, 안 멈추는 적은 approach.</summary>
        public string EventKind => IsEnemy ? approach : kind;
        /// <summary>복선: 판정 없이 먼 지붕에 적이 보임 (GDD-beta B2.3).</summary>
        public bool IsForeshadow => type == StageChart.TypeForeshadow;
        public bool IsSurround => type == StageChart.TypeSurround;
        public bool IsBoss => type == StageChart.TypeBoss;
        public ArrowKey Key => StageChart.ParseKey(key);
        public float Distance => runBeat * GameConfig.MetersPerBeat;
    }

    [Serializable]
    public class ChartData
    {
        public string name;
        public float bpm;
        public int runBeats;
        public ChartEvent[] events;

        public IEnumerable<ChartEvent> Obstacles => events.Where(e => e.IsObstacle);
        public IEnumerable<ChartEvent> Enemies => events.Where(e => e.IsDuel);
        /// <summary>달리면서 처리하는 사건 전부 (GDD-beta B2).</summary>
        public IEnumerable<ChartEvent> RunEvents => events.Where(e => e.IsRunEvent);
        public float PathLength => runBeats * GameConfig.MetersPerBeat;
    }

    /// <summary>스테이지 차트 (GDD 4.9). Resources/Charts/&lt;name&gt;.json 하나로 관리한다.</summary>
    public static class StageChart
    {
        public const string TypeObstacle = "obstacle";
        public const string TypeEnemy = "enemy";
        public const string TypeSurround = "surround";
        public const string TypeBoss = "boss";
        public const string TypeForeshadow = "foreshadow";
        public const string DefaultStage = "stage1";

        public static ChartData Load(string name = DefaultStage)
        {
            var asset = Resources.Load<TextAsset>("Charts/" + name);
            if (asset == null) throw new Exception($"차트 없음: Resources/Charts/{name}.json");
            var data = JsonUtility.FromJson<ChartData>(asset.text);
            Array.Sort(data.events, (a, b) => a.runBeat.CompareTo(b.runBeat));
            return data;
        }

        public static ArrowKey ParseKey(string key) =>
            string.IsNullOrEmpty(key) ? ArrowKey.Up : (ArrowKey)Enum.Parse(typeof(ArrowKey), key, true);

        /// <summary>장애물 끼리 (앞 장애물 끝 → 다음 장애물 시작) 간격의 최솟값 (박자).</summary>
        public static int MinObstacleGap(ChartData data)
        {
            var obs = data.Obstacles.Select(e => e.runBeat).OrderBy(b => b).ToArray();
            int min = int.MaxValue;
            for (int i = 1; i < obs.Length; i++) min = Math.Min(min, obs[i] - (obs[i - 1] + GameConfig.ObstacleBeats));
            return min;
        }
    }
}
