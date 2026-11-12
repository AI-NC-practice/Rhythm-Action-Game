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

        public bool IsObstacle => type == StageChart.TypeObstacle;
        public bool IsEnemy => type == StageChart.TypeEnemy;
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
        public IEnumerable<ChartEvent> Enemies => events.Where(e => e.IsEnemy);
        public float PathLength => runBeats * GameConfig.MetersPerBeat;
    }

    /// <summary>스테이지 차트 (GDD 4.9). Resources/Charts/&lt;name&gt;.json 하나로 관리한다.</summary>
    public static class StageChart
    {
        public const string TypeObstacle = "obstacle";
        public const string TypeEnemy = "enemy";
        public const string TypeSurround = "surround";
        public const string TypeBoss = "boss";
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
